using JarvisAssistant.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace JarvisAssistant.Services
{
    /// <summary>
    /// In-memory audio cache implementation with size and time-based expiration.
    /// </summary>
    public class AudioCacheService : IAudioCacheService, IDisposable
    {
        private readonly ILogger<AudioCacheService> _logger;
        private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
        private readonly Timer _cleanupTimer;
        private readonly int _maxCacheSizeBytes;
        private readonly TimeSpan _expiryTime;
        private long _currentCacheSizeBytes = 0;
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the AudioCacheService.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="maxCacheSizeMB">Maximum cache size in megabytes.</param>
        /// <param name="expiryHours">Cache entry expiry time in hours.</param>
        public AudioCacheService(ILogger<AudioCacheService> logger, int maxCacheSizeMB = 100, int expiryHours = 24)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _maxCacheSizeBytes = maxCacheSizeMB * 1024 * 1024;
            _expiryTime = TimeSpan.FromHours(expiryHours);

            // Run cleanup every hour
            _cleanupTimer = new Timer(async _ => await ClearExpiredEntriesAsync(), null, 
                TimeSpan.FromHours(1), TimeSpan.FromHours(1));

            _logger.LogInformation("Audio cache initialized with max size: {MaxSize}MB, expiry: {Expiry}h", 
                maxCacheSizeMB, expiryHours);
        }

        /// <inheritdoc/>
        public async Task<byte[]?> GetCachedAudioAsync(string text, string voiceId, object voiceSettings)
        {
            if (_disposed || string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(voiceId))
                return null;

            try
            {
                var cacheKey = GenerateCacheKey(text, voiceId, voiceSettings);
                
                if (_cache.TryGetValue(cacheKey, out var entry))
                {
                    if (entry.IsExpired)
                    {
                        // Remove expired entry
                        if (_cache.TryRemove(cacheKey, out var removedEntry))
                        {
                            Interlocked.Add(ref _currentCacheSizeBytes, -removedEntry.AudioData.Length);
                        }
                        return null;
                    }

                    // Update last accessed time
                    entry.LastAccessed = DateTime.UtcNow;
                    
                    _logger.LogDebug("Cache hit for key: {Key}, size: {Size} bytes", 
                        cacheKey[..Math.Min(16, cacheKey.Length)], entry.AudioData.Length);
                    
                    return entry.AudioData;
                }

                _logger.LogDebug("Cache miss for text: {Text}", text[..Math.Min(50, text.Length)]);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cached audio");
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> CacheAudioAsync(string text, string voiceId, object voiceSettings, byte[] audioData)
        {
            if (_disposed || string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(voiceId) || 
                audioData == null || audioData.Length == 0)
                return false;

            try
            {
                var cacheKey = GenerateCacheKey(text, voiceId, voiceSettings);
                
                // Check if adding this entry would exceed cache size
                if (_currentCacheSizeBytes + audioData.Length > _maxCacheSizeBytes)
                {
                    // Try to free up space by removing old entries
                    await EvictOldEntriesAsync(audioData.Length);
                    
                    // Check again after cleanup
                    if (_currentCacheSizeBytes + audioData.Length > _maxCacheSizeBytes)
                    {
                        _logger.LogWarning("Cannot cache audio: would exceed max cache size");
                        return false;
                    }
                }

                var entry = new CacheEntry
                {
                    AudioData = audioData,
                    CachedAt = DateTime.UtcNow,
                    LastAccessed = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.Add(_expiryTime)
                };

                var added = _cache.TryAdd(cacheKey, entry);
                if (added)
                {
                    Interlocked.Add(ref _currentCacheSizeBytes, audioData.Length);
                    
                    _logger.LogDebug("Cached audio for key: {Key}, size: {Size} bytes, total cache size: {TotalSize} bytes", 
                        cacheKey[..Math.Min(16, cacheKey.Length)], audioData.Length, _currentCacheSizeBytes);
                }

                return added;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error caching audio");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<int> ClearExpiredEntriesAsync()
        {
            if (_disposed)
                return 0;

            try
            {
                var expiredKeys = new List<string>();
                var now = DateTime.UtcNow;

                foreach (var kvp in _cache)
                {
                    if (kvp.Value.ExpiresAt < now)
                    {
                        expiredKeys.Add(kvp.Key);
                    }
                }

                var removedCount = 0;
                foreach (var key in expiredKeys)
                {
                    if (_cache.TryRemove(key, out var entry))
                    {
                        Interlocked.Add(ref _currentCacheSizeBytes, -entry.AudioData.Length);
                        removedCount++;
                    }
                }

                if (removedCount > 0)
                {
                    _logger.LogInformation("Removed {Count} expired cache entries", removedCount);
                }

                return removedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing expired cache entries");
                return 0;
            }
        }

        /// <inheritdoc/>
        public Dictionary<string, object> GetStatistics()
        {
            return new Dictionary<string, object>
            {
                ["total_entries"] = _cache.Count,
                ["total_size_bytes"] = _currentCacheSizeBytes,
                ["total_size_mb"] = _currentCacheSizeBytes / (1024.0 * 1024.0),
                ["max_size_bytes"] = _maxCacheSizeBytes,
                ["max_size_mb"] = _maxCacheSizeBytes / (1024.0 * 1024.0),
                ["cache_usage_percent"] = _maxCacheSizeBytes > 0 ? (_currentCacheSizeBytes * 100.0 / _maxCacheSizeBytes) : 0,
                ["expiry_time_hours"] = _expiryTime.TotalHours
            };
        }

        /// <inheritdoc/>
        public async Task<int> ClearAllAsync()
        {
            if (_disposed)
                return 0;

            try
            {
                var count = _cache.Count;
                _cache.Clear();
                Interlocked.Exchange(ref _currentCacheSizeBytes, 0);
                
                _logger.LogInformation("Cleared all {Count} cache entries", count);
                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing all cache entries");
                return 0;
            }
        }

        private string GenerateCacheKey(string text, string voiceId, object voiceSettings)
        {
            // Create a hash of the text, voice ID, and settings for consistent caching
            var combined = $"{text}|{voiceId}|{System.Text.Json.JsonSerializer.Serialize(voiceSettings)}";
            
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
            return Convert.ToBase64String(hashBytes);
        }

        private async Task EvictOldEntriesAsync(int requiredSpace)
        {
            try
            {
                // Sort by last accessed time (oldest first)
                var sortedEntries = _cache.ToList()
                    .OrderBy(kvp => kvp.Value.LastAccessed)
                    .ToList();

                var freedSpace = 0;
                var removedCount = 0;

                foreach (var kvp in sortedEntries)
                {
                    if (freedSpace >= requiredSpace)
                        break;

                    if (_cache.TryRemove(kvp.Key, out var entry))
                    {
                        freedSpace += entry.AudioData.Length;
                        Interlocked.Add(ref _currentCacheSizeBytes, -entry.AudioData.Length);
                        removedCount++;
                    }
                }

                if (removedCount > 0)
                {
                    _logger.LogInformation("Evicted {Count} old cache entries to free {Size} bytes", 
                        removedCount, freedSpace);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evicting old cache entries");
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _cleanupTimer?.Dispose();
            _cache.Clear();
            Interlocked.Exchange(ref _currentCacheSizeBytes, 0);
        }

        private class CacheEntry
        {
            public byte[] AudioData { get; set; } = Array.Empty<byte>();
            public DateTime CachedAt { get; set; }
            public DateTime LastAccessed { get; set; }
            public DateTime ExpiresAt { get; set; }

            public bool IsExpired => DateTime.UtcNow > ExpiresAt;
        }
    }
}
