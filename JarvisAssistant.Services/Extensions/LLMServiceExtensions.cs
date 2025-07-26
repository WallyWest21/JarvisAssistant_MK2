using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Services.LLM;
using JarvisAssistant.Services.Hubs;
using System.Net.NetworkInformation;

namespace JarvisAssistant.Services.Extensions
{
    /// <summary>
    /// Extension methods for registering LLM services in the dependency injection container.
    /// </summary>
    public static class LLMServiceExtensions
    {
        /// <summary>
        /// Adds the Ollama LLM service with automatic endpoint detection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="ollamaBaseUrl">Override URL. If null, will attempt auto-detection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddOllamaLLMService(this IServiceCollection services, string? ollamaBaseUrl = null)
        {
            // Register configuration options
            services.Configure<OllamaLLMOptions>(options =>
            {
                options.BaseUrl = ollamaBaseUrl ?? DetectOllamaEndpoint();
            });

            // Register HttpClient for OllamaClient with configuration
            services.AddHttpClient<OllamaClient>((serviceProvider, client) =>
            {
                var options = Microsoft.Extensions.Options.Options.Create(new OllamaLLMOptions { BaseUrl = ollamaBaseUrl ?? DetectOllamaEndpoint() });
                client.BaseAddress = new Uri(options.Value.BaseUrl);
                client.Timeout = options.Value.Timeout;
            });

            // Register OllamaClient with interface
            services.AddSingleton<IOllamaClient, OllamaClient>();
            services.AddSingleton<OllamaClient>();

            // Register PersonalityService with interface
            services.AddSingleton<IPersonalityService, PersonalityService>();
            services.AddSingleton<PersonalityService>();

            // Register the main LLM service with fallback
            services.AddScoped<ILLMService>(serviceProvider =>
            {
                var logger = serviceProvider.GetService<ILogger<OllamaLLMService>>();
                try
                {
                    var ollamaClient = serviceProvider.GetRequiredService<IOllamaClient>();
                    var personalityService = serviceProvider.GetRequiredService<IPersonalityService>();
                    return new OllamaLLMService(ollamaClient, personalityService, logger!);
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, "Failed to initialize OllamaLLMService, falling back to FallbackLLMService");
                    var fallbackLogger = serviceProvider.GetService<ILogger<FallbackLLMService>>();
                    return new FallbackLLMService(fallbackLogger);
                }
            });

            // Register streaming service
            services.AddSingleton<StreamingResponseService>();

            return services;
        }

        /// <summary>
        /// Configures the Ollama LLM service with custom settings.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Action to configure LLM options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection ConfigureOllamaLLMService(this IServiceCollection services, Action<OllamaLLMOptions> configureOptions)
        {
            services.Configure(configureOptions);
            return services;
        }

        /// <summary>
        /// Attempts to detect the appropriate Ollama endpoint.
        /// </summary>
        /// <returns>The detected or default Ollama endpoint URL.</returns>
        private static string DetectOllamaEndpoint()
        {
            // List of potential Ollama endpoints to try
            var endpoints = new List<string>();

#if ANDROID
            // Android emulator specific endpoints
            endpoints.AddRange(new[]
            {
                "http://10.0.2.2:11434",           // Android emulator host mapping
                "http://100.108.155.28:11434",     // Direct IP (might work on device)
                "http://192.168.1.100:11434",      // Common local network range
                "http://192.168.0.100:11434"       // Alternative local network range
            });
#else
            // Standard endpoints for other platforms
            endpoints.AddRange(new[]
            {
                "http://localhost:11434",           // Local development
                "http://127.0.0.1:11434",          // Local loopback
                "http://100.108.155.28:11434",     // Original configured endpoint
                "http://host.docker.internal:11434" // Docker development
            });
#endif

            foreach (var endpoint in endpoints)
            {
                if (IsEndpointReachable(endpoint))
                {
                    return endpoint;
                }
            }

            // Platform-specific defaults
#if ANDROID
            return "http://10.0.2.2:11434"; // Default for Android emulator
#else
            return "http://localhost:11434"; // Default for other platforms
#endif
        }

        /// <summary>
        /// Checks if an endpoint is reachable by attempting a quick connection test.
        /// </summary>
        /// <param name="endpoint">The endpoint URL to test.</param>
        /// <returns>True if the endpoint appears to be reachable.</returns>
        private static bool IsEndpointReachable(string endpoint)
        {
            try
            {
                var uri = new Uri(endpoint);
                using var ping = new Ping();
                var reply = ping.Send(uri.Host, 1000); // 1 second timeout
                return reply?.Status == IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Configuration options for the Ollama LLM service.
    /// </summary>
    public class OllamaLLMOptions
    {
        /// <summary>
        /// Gets or sets the base URL for the Ollama server.
        /// </summary>
        public string BaseUrl { get; set; } = "http://localhost:11434";

        /// <summary>
        /// Gets or sets the request timeout duration.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets the default model to use for general queries.
        /// </summary>
        public string DefaultModel { get; set; } = "llama3.2:latest";

        /// <summary>
        /// Gets or sets the model to use for code-related queries.
        /// </summary>
        public string CodeModel { get; set; } = "deepseek-coder:latest";

        /// <summary>
        /// Gets or sets whether to enable personality formatting.
        /// </summary>
        public bool EnablePersonality { get; set; } = true;

        /// <summary>
        /// Gets or sets the temperature setting for model responses (0.0 to 1.0).
        /// </summary>
        public double Temperature { get; set; } = 0.7;

        /// <summary>
        /// Gets or sets the top-p setting for model responses (0.0 to 1.0).
        /// </summary>
        public double TopP { get; set; } = 0.9;

        /// <summary>
        /// Gets or sets the maximum number of tokens in responses.
        /// </summary>
        public int MaxTokens { get; set; } = 2048;

        /// <summary>
        /// Gets or sets the maximum number of retry attempts for failed requests.
        /// </summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets the delay between retry attempts.
        /// </summary>
        public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Gets or sets alternative endpoints to try if the primary fails.
        /// </summary>
        public List<string> AlternativeEndpoints { get; set; } = new()
        {
            "http://localhost:11434",
            "http://127.0.0.1:11434",
            "http://host.docker.internal:11434"
        };
    }
}
