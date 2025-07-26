using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models;
using JarvisAssistant.Services;
using JarvisAssistant.Services.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JarvisAssistant.UnitTests.Integration
{
    /// <summary>
    /// Integration tests for ElevenLabs voice service and supporting services.
    /// </summary>
    public class ElevenLabsVoiceIntegrationTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private bool _disposed = false;

        public ElevenLabsVoiceIntegrationTests()
        {
            var services = new ServiceCollection();
            
            // Add logging
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
            
            // Add ElevenLabs service with test configuration
            services.AddElevenLabsVoiceService(config =>
            {
                config.ApiKey = "test-api-key-for-integration-test";
                config.VoiceId = "test-voice-id";
                config.EnableCaching = true;
                config.EnableRateLimiting = true;
                config.EnableFallback = true;
                config.MaxCacheSizeMB = 1; // Small cache for testing
                config.MaxRetryAttempts = 1; // Faster tests
                config.TimeoutSeconds = 2; // Very short timeout to trigger fallback quickly
            });

            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public void ServiceRegistration_AllServicesRegistered_Successfully()
        {
            // Act & Assert - Verify all required services are registered
            Assert.NotNull(_serviceProvider.GetService<IVoiceService>());
            Assert.NotNull(_serviceProvider.GetService<IAudioCacheService>());
            Assert.NotNull(_serviceProvider.GetService<IRateLimitService>());
            Assert.NotNull(_serviceProvider.GetService<ElevenLabsConfig>());
            Assert.NotNull(_serviceProvider.GetService<HttpClient>());
        }

        [Fact]
        public void ServiceRegistration_VoiceServiceIsElevenLabsType()
        {
            // Act
            var voiceService = _serviceProvider.GetService<IVoiceService>();

            // Assert
            Assert.IsType<ElevenLabsVoiceService>(voiceService);
        }

        [Fact]
        public void ServiceRegistration_ConfigurationIsValid()
        {
            // Act
            var config = _serviceProvider.GetRequiredService<ElevenLabsConfig>();

            // Assert
            Assert.True(config.IsValid());
            Assert.Equal("test-api-key-for-integration-test", config.ApiKey);
            Assert.Equal("test-voice-id", config.VoiceId);
        }

        [Fact]
        public async Task CacheService_Integration_WorksCorrectly()
        {
            // Arrange
            var cacheService = _serviceProvider.GetRequiredService<IAudioCacheService>();
            var text = "Integration test message";
            var voiceId = "test-voice";
            var settings = new { stability = 0.75f };
            var audioData = new byte[] { 1, 2, 3, 4, 5 };

            // Act
            var cached = await cacheService.CacheAudioAsync(text, voiceId, settings, audioData);
            var retrieved = await cacheService.GetCachedAudioAsync(text, voiceId, settings);

            // Assert
            Assert.True(cached);
            Assert.NotNull(retrieved);
            Assert.Equal(audioData, retrieved);
        }

        [Fact]
        public async Task RateLimitService_Integration_WorksCorrectly()
        {
            // Arrange
            var rateLimitService = _serviceProvider.GetRequiredService<IRateLimitService>();
            var apiKey = "test-integration-key";

            // Act
            var canMake1 = await rateLimitService.CanMakeRequestAsync(apiKey);
            await rateLimitService.RecordRequestAsync(apiKey, 100);
            var canMake2 = await rateLimitService.CanMakeRequestAsync(apiKey);

            // Assert
            Assert.True(canMake1);
            Assert.True(canMake2); // Should still be within limits
        }

        [Fact]
        public async Task VoiceService_WithFallback_HandlesFailureGracefully()
        {
            // Arrange
            var voiceService = _serviceProvider.GetRequiredService<IVoiceService>();

            // Act - This will fail with invalid API key but should fall back to stub service
            var result = await voiceService.GenerateSpeechAsync("Test message");

            // Assert - Should get some result from fallback (stub service)
            Assert.NotNull(result);
        }

        [Fact]
        public async Task VoiceService_StreamingFallback_WorksCorrectly()
        {
            // Arrange
            var voiceService = _serviceProvider.GetRequiredService<IVoiceService>();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            // Act - This will fail with invalid API key but should fall back to stub service
            var chunks = new List<byte[]>();
            try
            {
                await foreach (var chunk in voiceService.StreamSpeechAsync("Test streaming message").WithCancellation(timeout.Token))
                {
                    chunks.Add(chunk);
                    if (chunks.Count > 10) break; // Prevent infinite streaming in tests
                }
            }
            catch (OperationCanceledException)
            {
                // Test timed out - this might indicate a problem with fallback
                Assert.True(chunks.Count > 0, "Expected at least some chunks before timeout, but got none. Fallback mechanism may not be working.");
            }

            // Assert
            Assert.NotEmpty(chunks);
        }

        [Fact]
        public async Task GetElevenLabsStatistics_ReturnsValidData()
        {
            // Act
            var stats = await _serviceProvider.GetElevenLabsStatisticsAsync();

            // Assert
            Assert.NotNull(stats);
            Assert.True(stats.ContainsKey("cache_total_entries"));
            Assert.True(stats.ContainsKey("rate_limit_total_requests"));
        }

        [Fact]
        public async Task ValidateElevenLabsService_WithInvalidKey_ReturnsFalse()
        {
            // Act
            var isValid = await _serviceProvider.ValidateElevenLabsServiceAsync();

            // Assert - Should be false since we're using a test API key
            // The service should gracefully handle invalid API keys and return false
            Assert.False(isValid);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _serviceProvider?.Dispose();
        }
    }

    /// <summary>
    /// Integration tests for service extension methods.
    /// </summary>
    public class ElevenLabsServiceExtensionsTests
    {
        [Fact]
        public void AddJarvisVoiceService_ConfiguresCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            services.AddJarvisVoiceService("test-api-key");
            using var provider = services.BuildServiceProvider();

            // Assert
            var config = provider.GetRequiredService<ElevenLabsConfig>();
            Assert.Equal("test-api-key", config.ApiKey);
            Assert.Equal("91AxxCADnelg9FDuKsIS", config.VoiceId); // Updated voice ID for Jarvis
            Assert.True(config.EnableStreaming);
            Assert.True(config.EnableCaching);
            Assert.Equal(0.9f, config.DefaultVoiceSettings.SpeakingRate); // Measured pace
        }

        [Fact]
        public void AddVoiceServiceWithFallback_WithApiKey_UsesElevenLabs()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            services.AddVoiceServiceWithFallback("test-api-key");
            using var provider = services.BuildServiceProvider();

            // Assert
            var voiceService = provider.GetRequiredService<IVoiceService>();
            Assert.IsType<ElevenLabsVoiceService>(voiceService);
        }

        [Fact]
        public void AddVoiceServiceWithFallback_WithoutApiKey_UsesStubService()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            services.AddVoiceServiceWithFallback(null);
            using var provider = services.BuildServiceProvider();

            // Assert
            var voiceService = provider.GetRequiredService<IVoiceService>();
            Assert.IsType<StubVoiceService>(voiceService);
        }

        [Fact]
        public void ReplaceVoiceServiceWithElevenLabs_ReplacesExistingService()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IVoiceService, StubVoiceService>(); // Add existing service

            // Act
            services.ReplaceVoiceServiceWithElevenLabs("test-api-key");
            using var provider = services.BuildServiceProvider();

            // Assert
            var voiceService = provider.GetRequiredService<IVoiceService>();
            Assert.IsType<ElevenLabsVoiceService>(voiceService);
        }

        [Fact]
        public void AddElevenLabsVoiceServiceFromEnvironment_WithoutApiKey_ThrowsException()
        {
            // Arrange
            Environment.SetEnvironmentVariable("ELEVENLABS_API_KEY", null);
            var services = new ServiceCollection();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => 
                services.AddElevenLabsVoiceServiceFromEnvironment());
        }

        [Fact]
        public void AddElevenLabsVoiceService_WithCustomConfiguration_AppliesCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            services.AddElevenLabsVoiceService(config =>
            {
                config.ApiKey = "custom-key";
                config.VoiceId = "custom-voice";
                config.EnableCaching = false;
                config.MaxRetryAttempts = 5;
            });

            using var provider = services.BuildServiceProvider();

            // Assert
            var config = provider.GetRequiredService<ElevenLabsConfig>();
            Assert.Equal("custom-key", config.ApiKey);
            Assert.Equal("custom-voice", config.VoiceId);
            Assert.False(config.EnableCaching);
            Assert.Equal(5, config.MaxRetryAttempts);
        }
    }

    /// <summary>
    /// Tests for VoiceSettings and ElevenLabsConfig models.
    /// </summary>
    public class VoiceConfigurationTests
    {
        [Fact]
        public void VoiceSettings_CreateJarvisSettings_HasCorrectValues()
        {
            // Act
            var settings = VoiceSettings.CreateJarvisSettings();

            // Assert
            Assert.Equal(0.75f, settings.Stability);
            Assert.Equal(0.85f, settings.SimilarityBoost);
            Assert.Equal(0.0f, settings.Style);
            Assert.Equal(0.9f, settings.SpeakingRate);
        }

        [Theory]
        [InlineData("excited", 0.6f, 0.8f, 0.3f, 1.1f)]
        [InlineData("concerned", 0.8f, 0.9f, 0.1f, 0.8f)]
        [InlineData("calm", 0.85f, 0.9f, 0.0f, 0.85f)]
        [InlineData("unknown", 0.75f, 0.85f, 0.0f, 0.9f)] // Should default to Jarvis settings
        public void VoiceSettings_CreateEmotionalSettings_ReturnsCorrectValues(
            string emotion, float expectedStability, float expectedSimilarity, 
            float expectedStyle, float expectedRate)
        {
            // Act
            var settings = VoiceSettings.CreateEmotionalSettings(emotion);

            // Assert
            Assert.Equal(expectedStability, settings.Stability);
            Assert.Equal(expectedSimilarity, settings.SimilarityBoost);
            Assert.Equal(expectedStyle, settings.Style);
            Assert.Equal(expectedRate, settings.SpeakingRate);
        }

        [Fact]
        public void ElevenLabsConfig_IsValid_WithValidConfig_ReturnsTrue()
        {
            // Arrange
            var config = new ElevenLabsConfig
            {
                ApiKey = "valid-key",
                VoiceId = "valid-voice",
                BaseUrl = "https://api.elevenlabs.io",
                TimeoutSeconds = 30,
                MaxRetryAttempts = 3
            };

            // Act
            var isValid = config.IsValid();

            // Assert
            Assert.True(isValid);
        }

        [Theory]
        [InlineData(null, "voice", "https://api.elevenlabs.io", 30, 3, false)]
        [InlineData("", "voice", "https://api.elevenlabs.io", 30, 3, false)]
        [InlineData("key", null, "https://api.elevenlabs.io", 30, 3, false)]
        [InlineData("key", "", "https://api.elevenlabs.io", 30, 3, false)]
        [InlineData("key", "voice", "", 30, 3, false)]
        [InlineData("key", "voice", "https://api.elevenlabs.io", 0, 3, false)]
        [InlineData("key", "voice", "https://api.elevenlabs.io", 30, -1, false)]
        public void ElevenLabsConfig_IsValid_WithInvalidConfig_ReturnsFalse(
            string? apiKey, string? voiceId, string baseUrl, int timeoutSeconds, int maxRetryAttempts, bool expected)
        {
            // Arrange
            var config = new ElevenLabsConfig
            {
                ApiKey = apiKey,
                VoiceId = voiceId,
                BaseUrl = baseUrl,
                TimeoutSeconds = timeoutSeconds,
                MaxRetryAttempts = maxRetryAttempts
            };

            // Act
            var isValid = config.IsValid();

            // Assert
            Assert.Equal(expected, isValid);
        }

        [Fact]
        public void ElevenLabsConfig_GetTextToSpeechUrl_ReturnsCorrectUrl()
        {
            // Arrange
            var config = new ElevenLabsConfig
            {
                BaseUrl = "https://api.elevenlabs.io",
                ApiVersion = "v1",
                VoiceId = "test-voice"
            };

            // Act
            var url = config.GetTextToSpeechUrl();

            // Assert
            Assert.Equal("https://api.elevenlabs.io/v1/text-to-speech/test-voice", url);
        }

        [Fact]
        public void ElevenLabsConfig_GetStreamingUrl_ReturnsCorrectUrl()
        {
            // Arrange
            var config = new ElevenLabsConfig
            {
                BaseUrl = "https://api.elevenlabs.io",
                ApiVersion = "v1",
                VoiceId = "test-voice"
            };

            // Act
            var url = config.GetStreamingUrl();

            // Assert
            Assert.Equal("https://api.elevenlabs.io/v1/text-to-speech/test-voice/stream", url);
        }
    }

    /// <summary>
    /// Test-specific extension methods for service configuration.
    /// </summary>
    public static class TestServiceExtensions
    {
        /// <summary>
        /// Adds voice service with fallback for testing purposes.
        /// When no API key is provided, uses the test StubVoiceService.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <param name="apiKey">Optional ElevenLabs API key.</param>
        /// <returns>Service collection for chaining.</returns>
        public static IServiceCollection AddVoiceServiceWithFallback(
            this IServiceCollection services,
            string? apiKey = null)
        {
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                // API key provided, use ElevenLabs with fallback
                return services.AddJarvisVoiceService(apiKey);
            }
            else
            {
                // No API key, use test stub service
                services.AddSingleton<IVoiceService, StubVoiceService>();
                return services;
            }
        }
    }
}
