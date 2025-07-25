using JarvisAssistant.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace JarvisAssistant.Services
{
    /// <summary>
    /// Implementation of rate limiting service with sliding window algorithm.
    /// </summary>
    public class RateLimitService : IRateLimitService, IDisposable
    {
        private readonly ILogger<RateLimitService> _logger;
        private readonly ConcurrentDictionary<string, RateLimitData> _rateLimits = new();
        private readonly Timer _cleanupTimer;
        private readonly int _maxRequestsPerMinute;
        private readonly int _maxCharactersPerMinute;
        private bool _disposed = false;

        /// <summary>
        /// Initializes a new instance of the RateLimitService.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="maxRequestsPerMinute">Maximum requests per minute.</param>
        /// <param name="maxCharactersPerMinute">Maximum characters per minute.</param>
        public RateLimitService(ILogger<RateLimitService> logger, int maxRequestsPerMinute = 100, int maxCharactersPerMinute = 50000)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _maxRequestsPerMinute = maxRequestsPerMinute;
            _maxCharactersPerMinute = maxCharactersPerMinute;

            // Clean up old data every 5 minutes
            _cleanupTimer = new Timer(CleanupOldData, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));

            _logger.LogInformation("Rate limit service initialized with {RequestLimit} requests/min, {CharLimit} chars/min", 
                maxRequestsPerMinute, maxCharactersPerMinute);
        }

        /// <inheritdoc/>
        public async Task<bool> CanMakeRequestAsync(string apiKey)
        {
            if (_disposed || string.IsNullOrWhiteSpace(apiKey))
                return false;

            try
            {
                var rateLimitData = _rateLimits.GetOrAdd(apiKey, _ => new RateLimitData());
                var now = DateTime.UtcNow;

                lock (rateLimitData)
                {
                    // Remove requests older than 1 minute
                    rateLimitData.Requests.RemoveAll(r => now - r.Timestamp > TimeSpan.FromMinutes(1));

                    // Check request count limit
                    if (rateLimitData.Requests.Count >= _maxRequestsPerMinute)
                    {
                        _logger.LogWarning("Rate limit exceeded for API key: too many requests ({Count}/{Max})", 
                            rateLimitData.Requests.Count, _maxRequestsPerMinute);
                        return false;
                    }

                    // Check character count limit
                    var charactersInLastMinute = rateLimitData.Requests
                        .Where(r => now - r.Timestamp <= TimeSpan.FromMinutes(1))
                        .Sum(r => r.CharacterCount);

                    if (charactersInLastMinute >= _maxCharactersPerMinute)
                    {
                        _logger.LogWarning("Rate limit exceeded for API key: too many characters ({Count}/{Max})", 
                            charactersInLastMinute, _maxCharactersPerMinute);
                        return false;
                    }

                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking rate limit");
                return false; // Fail safe - deny request on error
            }
        }

        /// <inheritdoc/>
        public async Task RecordRequestAsync(string apiKey, int characterCount)
        {
            if (_disposed || string.IsNullOrWhiteSpace(apiKey))
                return;

            try
            {
                var rateLimitData = _rateLimits.GetOrAdd(apiKey, _ => new RateLimitData());
                var now = DateTime.UtcNow;

                lock (rateLimitData)
                {
                    rateLimitData.Requests.Add(new RequestRecord
                    {
                        Timestamp = now,
                        CharacterCount = characterCount
                    });

                    rateLimitData.TotalRequests++;
                    rateLimitData.TotalCharacters += characterCount;
                    rateLimitData.LastRequestTime = now;
                }

                _logger.LogDebug("Recorded request: {Characters} characters for API key", characterCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording request for rate limiting");
            }
        }

        /// <inheritdoc/>
        public async Task<TimeSpan?> GetWaitTimeAsync(string apiKey)
        {
            if (_disposed || string.IsNullOrWhiteSpace(apiKey))
                return null;

            try
            {
                if (!_rateLimits.TryGetValue(apiKey, out var rateLimitData))
                    return null;

                var now = DateTime.UtcNow;

                lock (rateLimitData)
                {
                    // Remove requests older than 1 minute
                    rateLimitData.Requests.RemoveAll(r => now - r.Timestamp > TimeSpan.FromMinutes(1));

                    if (rateLimitData.Requests.Count == 0)
                        return null;

                    // Find the oldest request
                    var oldestRequest = rateLimitData.Requests.MinBy(r => r.Timestamp);
                    if (oldestRequest == null)
                        return null;

                    // Calculate when the oldest request will be outside the 1-minute window
                    var waitUntil = oldestRequest.Timestamp.Add(TimeSpan.FromMinutes(1));
                    var waitTime = waitUntil - now;

                    return waitTime > TimeSpan.Zero ? waitTime : null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating wait time");
                return TimeSpan.FromSeconds(60); // Conservative fallback
            }
        }

        /// <inheritdoc/>
        public Dictionary<string, object> GetStatistics(string apiKey)
        {
            var defaultStats = new Dictionary<string, object>
            {
                ["total_requests"] = 0,
                ["total_characters"] = 0,
                ["requests_last_minute"] = 0,
                ["characters_last_minute"] = 0,
                ["last_request_time"] = (DateTime?)null,
                ["max_requests_per_minute"] = _maxRequestsPerMinute,
                ["max_characters_per_minute"] = _maxCharactersPerMinute,
                ["rate_limited"] = false
            };

            if (_disposed || string.IsNullOrWhiteSpace(apiKey) || !_rateLimits.TryGetValue(apiKey, out var rateLimitData))
                return defaultStats;

            try
            {
                var now = DateTime.UtcNow;

                lock (rateLimitData)
                {
                    // Remove old requests
                    rateLimitData.Requests.RemoveAll(r => now - r.Timestamp > TimeSpan.FromMinutes(1));

                    var requestsLastMinute = rateLimitData.Requests.Count;
                    var charactersLastMinute = rateLimitData.Requests.Sum(r => r.CharacterCount);
                    var isRateLimited = requestsLastMinute >= _maxRequestsPerMinute || 
                                       charactersLastMinute >= _maxCharactersPerMinute;

                    return new Dictionary<string, object>
                    {
                        ["total_requests"] = rateLimitData.TotalRequests,
                        ["total_characters"] = rateLimitData.TotalCharacters,
                        ["requests_last_minute"] = requestsLastMinute,
                        ["characters_last_minute"] = charactersLastMinute,
                        ["last_request_time"] = rateLimitData.LastRequestTime,
                        ["max_requests_per_minute"] = _maxRequestsPerMinute,
                        ["max_characters_per_minute"] = _maxCharactersPerMinute,
                        ["rate_limited"] = isRateLimited,
                        ["requests_remaining"] = Math.Max(0, _maxRequestsPerMinute - requestsLastMinute),
                        ["characters_remaining"] = Math.Max(0, _maxCharactersPerMinute - charactersLastMinute)
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting rate limit statistics");
                return defaultStats;
            }
        }

        /// <inheritdoc/>
        public async Task ResetAsync(string apiKey)
        {
            if (_disposed || string.IsNullOrWhiteSpace(apiKey))
                return;

            try
            {
                if (_rateLimits.TryRemove(apiKey, out _))
                {
                    _logger.LogInformation("Reset rate limiting data for API key");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting rate limiting data");
            }
        }

        private void CleanupOldData(object? state)
        {
            if (_disposed)
                return;

            try
            {
                var now = DateTime.UtcNow;
                var keysToRemove = new List<string>();

                foreach (var kvp in _rateLimits)
                {
                    lock (kvp.Value)
                    {
                        // Remove old requests
                        kvp.Value.Requests.RemoveAll(r => now - r.Timestamp > TimeSpan.FromMinutes(1));

                        // If no recent activity, remove the entire entry
                        if (kvp.Value.LastRequestTime.HasValue && 
                            now - kvp.Value.LastRequestTime.Value > TimeSpan.FromHours(1))
                        {
                            keysToRemove.Add(kvp.Key);
                        }
                    }
                }

                foreach (var key in keysToRemove)
                {
                    _rateLimits.TryRemove(key, out _);
                }

                if (keysToRemove.Count > 0)
                {
                    _logger.LogDebug("Cleaned up {Count} inactive rate limit entries", keysToRemove.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during rate limit cleanup");
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _cleanupTimer?.Dispose();
            _rateLimits.Clear();
        }

        private class RateLimitData
        {
            public List<RequestRecord> Requests { get; } = new();
            public long TotalRequests { get; set; }
            public long TotalCharacters { get; set; }
            public DateTime? LastRequestTime { get; set; }
        }

        private class RequestRecord
        {
            public DateTime Timestamp { get; set; }
            public int CharacterCount { get; set; }
        }
    }
}
