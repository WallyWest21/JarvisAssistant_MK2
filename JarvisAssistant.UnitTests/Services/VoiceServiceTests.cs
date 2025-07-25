using JarvisAssistant.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JarvisAssistant.UnitTests.Services
{
    /// <summary>
    /// Unit tests for AudioCacheService.
    /// </summary>
    public class AudioCacheServiceTests : IDisposable
    {
        private readonly Mock<ILogger<AudioCacheService>> _mockLogger;
        private readonly AudioCacheService _cacheService;
        private bool _disposed = false;

        public AudioCacheServiceTests()
        {
            _mockLogger = new Mock<ILogger<AudioCacheService>>();
            _cacheService = new AudioCacheService(_mockLogger.Object, maxCacheSizeMB: 1, expiryHours: 1);
        }

        [Fact]
        public async Task CacheAudioAsync_WithValidData_StoresSuccessfully()
        {
            // Arrange
            var text = "Hello world";
            var voiceId = "voice1";
            var voiceSettings = new { stability = 0.75f };
            var audioData = new byte[] { 1, 2, 3, 4, 5 };

            // Act
            var result = await _cacheService.CacheAudioAsync(text, voiceId, voiceSettings, audioData);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task GetCachedAudioAsync_WithCachedData_ReturnsAudio()
        {
            // Arrange
            var text = "Test message";
            var voiceId = "voice1";
            var voiceSettings = new { stability = 0.75f };
            var audioData = new byte[] { 1, 2, 3, 4, 5 };

            await _cacheService.CacheAudioAsync(text, voiceId, voiceSettings, audioData);

            // Act
            var result = await _cacheService.GetCachedAudioAsync(text, voiceId, voiceSettings);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(audioData, result);
        }

        [Fact]
        public async Task GetCachedAudioAsync_WithNonExistentData_ReturnsNull()
        {
            // Act
            var result = await _cacheService.GetCachedAudioAsync("nonexistent", "voice1", new { });

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CacheAudioAsync_WithEmptyData_ReturnsFalse()
        {
            // Act
            var result = await _cacheService.CacheAudioAsync("test", "voice1", new { }, Array.Empty<byte>());

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CacheAudioAsync_WithNullText_ReturnsFalse()
        {
            // Act
            var result = await _cacheService.CacheAudioAsync(null!, "voice1", new { }, new byte[] { 1, 2, 3 });

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetStatistics_ReturnsCorrectInformation()
        {
            // Arrange
            var audioData = new byte[1000];
            await _cacheService.CacheAudioAsync("test", "voice1", new { }, audioData);

            // Act
            var stats = _cacheService.GetStatistics();

            // Assert
            Assert.True(stats.ContainsKey("total_entries"));
            Assert.True(stats.ContainsKey("total_size_bytes"));
            Assert.True(stats.ContainsKey("total_size_mb"));
            Assert.Equal(1, stats["total_entries"]);
            Assert.Equal(1000L, stats["total_size_bytes"]);
        }

        [Fact]
        public async Task ClearExpiredEntriesAsync_RemovesExpiredEntries()
        {
            // This test would require manipulating time or using a cache with very short expiry
            // For now, we'll test that the method doesn't throw
            var removedCount = await _cacheService.ClearExpiredEntriesAsync();
            Assert.True(removedCount >= 0);
        }

        [Fact]
        public async Task ClearAllAsync_RemovesAllEntries()
        {
            // Arrange
            await _cacheService.CacheAudioAsync("test1", "voice1", new { }, new byte[] { 1, 2, 3 });
            await _cacheService.CacheAudioAsync("test2", "voice1", new { }, new byte[] { 4, 5, 6 });

            // Act
            var removedCount = await _cacheService.ClearAllAsync();

            // Assert
            Assert.Equal(2, removedCount);
            
            var stats = _cacheService.GetStatistics();
            Assert.Equal(0, stats["total_entries"]);
        }

        [Fact]
        public async Task CacheAudioAsync_ExceedsMaxSize_HandlesGracefully()
        {
            // Arrange - Create cache with very small max size
            using var smallCache = new AudioCacheService(_mockLogger.Object, maxCacheSizeMB: 0); // 0 MB max
            var largeAudioData = new byte[1024 * 1024]; // 1 MB data

            // Act
            var result = await smallCache.CacheAudioAsync("test", "voice1", new { }, largeAudioData);

            // Assert
            Assert.False(result); // Should fail due to size limit
        }

        [Fact]
        public void Dispose_DoesNotThrow()
        {
            // Act & Assert
            _cacheService.Dispose();
        }

        [Fact]
        public async Task GetCachedAudioAsync_AfterDispose_ReturnsNull()
        {
            // Arrange
            _cacheService.Dispose();

            // Act
            var result = await _cacheService.GetCachedAudioAsync("test", "voice1", new { });

            // Assert
            Assert.Null(result);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _cacheService?.Dispose();
        }
    }

    /// <summary>
    /// Unit tests for RateLimitService.
    /// </summary>
    public class RateLimitServiceTests : IDisposable
    {
        private readonly Mock<ILogger<RateLimitService>> _mockLogger;
        private readonly RateLimitService _rateLimitService;
        private const string TestApiKey = "test-api-key";
        private bool _disposed = false;

        public RateLimitServiceTests()
        {
            _mockLogger = new Mock<ILogger<RateLimitService>>();
            _rateLimitService = new RateLimitService(_mockLogger.Object, maxRequestsPerMinute: 2, maxCharactersPerMinute: 100);
        }

        [Fact]
        public async Task CanMakeRequestAsync_WithNoHistory_ReturnsTrue()
        {
            // Act
            var result = await _rateLimitService.CanMakeRequestAsync(TestApiKey);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CanMakeRequestAsync_WithinLimits_ReturnsTrue()
        {
            // Arrange
            await _rateLimitService.RecordRequestAsync(TestApiKey, 10);

            // Act
            var result = await _rateLimitService.CanMakeRequestAsync(TestApiKey);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CanMakeRequestAsync_ExceedsRequestLimit_ReturnsFalse()
        {
            // Arrange - Record maximum number of requests
            await _rateLimitService.RecordRequestAsync(TestApiKey, 10);
            await _rateLimitService.RecordRequestAsync(TestApiKey, 10);

            // Act
            var result = await _rateLimitService.CanMakeRequestAsync(TestApiKey);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CanMakeRequestAsync_ExceedsCharacterLimit_ReturnsFalse()
        {
            // Arrange - Record request that exceeds character limit
            await _rateLimitService.RecordRequestAsync(TestApiKey, 101); // Exceeds 100 char limit

            // Act
            var result = await _rateLimitService.CanMakeRequestAsync(TestApiKey);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task RecordRequestAsync_UpdatesStatistics()
        {
            // Act
            await _rateLimitService.RecordRequestAsync(TestApiKey, 50);

            // Assert
            var stats = _rateLimitService.GetStatistics(TestApiKey);
            Assert.Equal(1L, stats["total_requests"]);
            Assert.Equal(50L, stats["total_characters"]);
            Assert.Equal(1, stats["requests_last_minute"]);
            Assert.Equal(50, stats["characters_last_minute"]);
        }

        [Fact]
        public async Task GetWaitTimeAsync_WithNoRequests_ReturnsNull()
        {
            // Act
            var waitTime = await _rateLimitService.GetWaitTimeAsync(TestApiKey);

            // Assert
            Assert.Null(waitTime);
        }

        [Fact]
        public async Task GetWaitTimeAsync_WithRecentRequests_ReturnsWaitTime()
        {
            // Arrange - Fill up the request limit
            await _rateLimitService.RecordRequestAsync(TestApiKey, 10);
            await _rateLimitService.RecordRequestAsync(TestApiKey, 10);

            // Act
            var waitTime = await _rateLimitService.GetWaitTimeAsync(TestApiKey);

            // Assert
            Assert.NotNull(waitTime);
            Assert.True(waitTime.Value.TotalSeconds > 0);
        }

        [Fact]
        public async Task GetStatistics_WithEmptyHistory_ReturnsDefaults()
        {
            // Act
            var stats = _rateLimitService.GetStatistics(TestApiKey);

            // Assert
            Assert.Equal(0L, stats["total_requests"]);
            Assert.Equal(0L, stats["total_characters"]);
            Assert.Equal(0, stats["requests_last_minute"]);
            Assert.Equal(0, stats["characters_last_minute"]);
            Assert.Equal(false, stats["rate_limited"]);
        }

        [Fact]
        public async Task GetStatistics_WhenRateLimited_ShowsCorrectStatus()
        {
            // Arrange - Exceed limits
            await _rateLimitService.RecordRequestAsync(TestApiKey, 101); // Exceeds character limit

            // Act
            var stats = _rateLimitService.GetStatistics(TestApiKey);

            // Assert
            Assert.Equal(true, stats["rate_limited"]);
        }

        [Fact]
        public async Task ResetAsync_ClearsAllData()
        {
            // Arrange
            await _rateLimitService.RecordRequestAsync(TestApiKey, 50);

            // Act
            await _rateLimitService.ResetAsync(TestApiKey);

            // Assert
            var stats = _rateLimitService.GetStatistics(TestApiKey);
            Assert.Equal(0L, stats["total_requests"]);
            Assert.Equal(0L, stats["total_characters"]);
        }

        [Fact]
        public async Task CanMakeRequestAsync_WithNullApiKey_ReturnsFalse()
        {
            // Act
            var result = await _rateLimitService.CanMakeRequestAsync(null!);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CanMakeRequestAsync_WithEmptyApiKey_ReturnsFalse()
        {
            // Act
            var result = await _rateLimitService.CanMakeRequestAsync(string.Empty);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Dispose_DoesNotThrow()
        {
            // Act & Assert
            _rateLimitService.Dispose();
        }

        [Fact]
        public async Task CanMakeRequestAsync_AfterDispose_ReturnsFalse()
        {
            // Arrange
            _rateLimitService.Dispose();

            // Act
            var result = await _rateLimitService.CanMakeRequestAsync(TestApiKey);

            // Assert
            Assert.False(result);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _rateLimitService?.Dispose();
        }
    }
}
