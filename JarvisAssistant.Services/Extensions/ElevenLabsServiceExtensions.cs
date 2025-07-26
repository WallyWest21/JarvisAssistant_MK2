using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JarvisAssistant.Services.Extensions
{
    /// <summary>
    /// Extension methods for configuring ElevenLabs voice services.
    /// </summary>
    public static class ElevenLabsServiceExtensions
    {
        /// <summary>
        /// Adds ElevenLabs voice service with all supporting services to the DI container.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <param name="configureOptions">Configuration action for ElevenLabs settings.</param>
        /// <returns>Service collection for chaining.</returns>
        public static IServiceCollection AddElevenLabsVoiceService(
            this IServiceCollection services,
            Action<ElevenLabsConfig>? configureOptions = null)
        {
            // Register configuration
            var config = new ElevenLabsConfig();
            configureOptions?.Invoke(config);
            services.AddSingleton(config);

            // Register supporting services
            services.AddSingleton<IAudioCacheService>(serviceProvider =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<AudioCacheService>>();
                return new AudioCacheService(logger, config.MaxCacheSizeMB, config.CacheExpiryHours);
            });

            services.AddSingleton<IRateLimitService>(serviceProvider =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<RateLimitService>>();
                return new RateLimitService(logger, config.MaxRequestsPerMinute);
            });

            // Register HTTP client for ElevenLabs
            services.AddHttpClient<ElevenLabsVoiceService>(client =>
            {
                client.BaseAddress = new Uri(config.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);
            });

            // Register intelligent multi-tier fallback service (Modern Windows TTS + Legacy SAPI + Stub)
            services.AddSingleton<IVoiceService>(serviceProvider =>
            {
                var httpClient = serviceProvider.GetRequiredService<HttpClient>();
                var logger = serviceProvider.GetRequiredService<ILogger<ElevenLabsVoiceService>>();
                var cacheService = serviceProvider.GetRequiredService<IAudioCacheService>();
                var rateLimitService = serviceProvider.GetRequiredService<IRateLimitService>();
                var fallbackLogger = serviceProvider.GetRequiredService<ILogger<IntelligentFallbackVoiceService>>();
                
                // Create intelligent fallback service with multiple free TTS options
                var fallbackService = new IntelligentFallbackVoiceService(fallbackLogger);

                // Return ElevenLabs service with intelligent fallback
                return new ElevenLabsVoiceService(
                    httpClient,
                    config,
                    logger,
                    cacheService,
                    rateLimitService,
                    fallbackService);
            });

            return services;
        }

        /// <summary>
        /// Adds ElevenLabs voice service with API key from configuration.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <param name="apiKey">ElevenLabs API key.</param>
        /// <param name="voiceId">Optional voice ID (uses default British accent if not specified).</param>
        /// <returns>Service collection for chaining.</returns>
        public static IServiceCollection AddElevenLabsVoiceService(
            this IServiceCollection services,
            string apiKey,
            string? voiceId = null)
        {
            return services.AddElevenLabsVoiceService(config =>
            {
                config.ApiKey = apiKey;
                if (!string.IsNullOrWhiteSpace(voiceId))
                {
                    config.VoiceId = voiceId;
                }
            });
        }

        /// <summary>
        /// Adds ElevenLabs voice service optimized for Jarvis assistant.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <param name="apiKey">ElevenLabs API key.</param>
        /// <returns>Service collection for chaining.</returns>
        public static IServiceCollection AddJarvisVoiceService(
            this IServiceCollection services,
            string apiKey)
        {
            return services.AddElevenLabsVoiceService(config =>
            {
                config.ApiKey = apiKey;
                config.VoiceId = "91AxxCADnelg9FDuKsIS"; // Updated voice ID for Jarvis
                config.DefaultVoiceSettings = VoiceSettings.CreateJarvisSettings();
                config.ModelId = "eleven_multilingual_v2";
                config.AudioFormat = "mp3_44100_128";
                config.AudioQuality = 8; // High quality for professional sound
                config.EnableStreaming = true;
                config.EnableCaching = true;
                config.EnableRateLimiting = true;
                config.EnableFallback = true;
                config.MaxCacheSizeMB = 150; // Larger cache for better performance
                config.TimeoutSeconds = 45; // Longer timeout for high-quality synthesis
                config.DefaultVoiceSettings.SpeakingRate = 0.9f; // Measured pace
            });
        }

        /// <summary>
        /// Configures ElevenLabs service with environment variables.
        /// Looks for ELEVENLABS_API_KEY and ELEVENLABS_VOICE_ID environment variables.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <returns>Service collection for chaining.</returns>
        public static IServiceCollection AddElevenLabsVoiceServiceFromEnvironment(
            this IServiceCollection services)
        {
            var apiKey = Environment.GetEnvironmentVariable("ELEVENLABS_API_KEY");
            var voiceId = Environment.GetEnvironmentVariable("ELEVENLABS_VOICE_ID");

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    "ELEVENLABS_API_KEY environment variable is required but not set. " +
                    "Please set this environment variable with your ElevenLabs API key.");
            }

            return services.AddJarvisVoiceService(apiKey);
        }

        /// <summary>
        /// Replaces the existing voice service registration with ElevenLabs.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <param name="apiKey">ElevenLabs API key.</param>
        /// <returns>Service collection for chaining.</returns>
        public static IServiceCollection ReplaceVoiceServiceWithElevenLabs(
            this IServiceCollection services,
            string apiKey)
        {
            // Remove existing IVoiceService registrations
            var voiceServiceDescriptors = services.Where(d => d.ServiceType == typeof(IVoiceService)).ToList();
            foreach (var descriptor in voiceServiceDescriptors)
            {
                services.Remove(descriptor);
            }

            // Add ElevenLabs service
            return services.AddJarvisVoiceService(apiKey);
        }

        /// <summary>
        /// Adds voice service with automatic fallback chain: ElevenLabs -> Stub.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <param name="apiKey">ElevenLabs API key (optional - if null, only stub service is used).</param>
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
#if WINDOWS
                // No API key, use Windows SAPI service (free TTS) on Windows
                services.AddSingleton<IVoiceService, JarvisAssistant.Services.WindowsSapiVoiceService>();
#else
                // No API key, use stub service on non-Windows platforms
                services.AddSingleton<IVoiceService, JarvisAssistant.Services.StubVoiceService>();
#endif
                return services;
            }
        }

        /// <summary>
        /// Adds voice service with specified fallback type when no API key is provided.
        /// </summary>
        /// <typeparam name="TFallback">Type of fallback voice service to use.</typeparam>
        /// <param name="services">Service collection.</param>
        /// <param name="apiKey">Optional ElevenLabs API key.</param>
        /// <returns>Service collection for chaining.</returns>
        public static IServiceCollection AddVoiceServiceWithFallback<TFallback>(
            this IServiceCollection services,
            string? apiKey = null)
            where TFallback : class, IVoiceService
        {
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                // API key provided, use ElevenLabs with fallback
                return services.AddJarvisVoiceService(apiKey);
            }
            else
            {
                // No API key, use specified fallback service
                services.AddSingleton<IVoiceService, TFallback>();
                return services;
            }
        }

        /// <summary>
        /// Validates ElevenLabs configuration and API connectivity.
        /// </summary>
        /// <param name="serviceProvider">Service provider.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if service is properly configured and accessible.</returns>
        public static async Task<bool> ValidateElevenLabsServiceAsync(
            this IServiceProvider serviceProvider,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var voiceService = serviceProvider.GetService<IVoiceService>();
                if (voiceService is not ElevenLabsVoiceService elevenLabsService)
                {
                    return false; // Not using ElevenLabs service
                }

                // Use the health check instead of generating speech to avoid fallback
                return await elevenLabsService.IsHealthyAsync(cancellationToken);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets ElevenLabs service statistics if available.
        /// </summary>
        /// <param name="serviceProvider">Service provider.</param>
        /// <returns>Dictionary containing service statistics or empty if not available.</returns>
        public static async Task<Dictionary<string, object>> GetElevenLabsStatisticsAsync(
            this IServiceProvider serviceProvider)
        {
            var stats = new Dictionary<string, object>();

            try
            {
                var voiceService = serviceProvider.GetService<IVoiceService>();
                if (voiceService is ElevenLabsVoiceService elevenLabsService)
                {
                    // Get quota info
                    var quotaInfo = await elevenLabsService.GetQuotaInfoAsync();
                    if (quotaInfo != null)
                    {
                        stats["quota_used_percentage"] = quotaInfo.QuotaUsedPercentage;
                        stats["characters_remaining"] = quotaInfo.CharactersRemaining;
                        stats["character_limit"] = quotaInfo.CharacterLimit;
                        stats["next_reset"] = quotaInfo.NextResetTime;
                    }
                }

                // Get cache statistics
                var cacheService = serviceProvider.GetService<IAudioCacheService>();
                if (cacheService != null)
                {
                    var cacheStats = cacheService.GetStatistics();
                    foreach (var kvp in cacheStats)
                    {
                        stats[$"cache_{kvp.Key}"] = kvp.Value;
                    }
                }

                // Get rate limit statistics
                var rateLimitService = serviceProvider.GetService<IRateLimitService>();
                var config = serviceProvider.GetService<ElevenLabsConfig>();
                if (rateLimitService != null && config != null && !string.IsNullOrWhiteSpace(config.ApiKey))
                {
                    var rateLimitStats = rateLimitService.GetStatistics(config.ApiKey);
                    foreach (var kvp in rateLimitStats)
                    {
                        stats[$"rate_limit_{kvp.Key}"] = kvp.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                stats["error"] = ex.Message;
            }

            return stats;
        }
    }
}
