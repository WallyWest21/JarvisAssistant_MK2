using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Services.LLM;
using JarvisAssistant.Services.Hubs;

namespace JarvisAssistant.Services.Extensions
{
    /// <summary>
    /// Extension methods for registering LLM services in the dependency injection container.
    /// </summary>
    public static class LLMServiceExtensions
    {
        /// <summary>
        /// Adds the Ollama LLM service and related components to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="ollamaBaseUrl">The base URL for the Ollama server. Defaults to http://100.108.155.28:11434</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddOllamaLLMService(this IServiceCollection services, string? ollamaBaseUrl = null)
        {
            // Register HttpClient for OllamaClient
            services.AddHttpClient<OllamaClient>(client =>
            {
                client.BaseAddress = new Uri(ollamaBaseUrl ?? "http://100.108.155.28:11434");
                client.Timeout = TimeSpan.FromMinutes(5);
            });

            // Register OllamaClient with interface
            services.AddSingleton<IOllamaClient, OllamaClient>();
            services.AddSingleton<OllamaClient>();

            // Register PersonalityService with interface
            services.AddSingleton<IPersonalityService, PersonalityService>();
            services.AddSingleton<PersonalityService>();

            // Register the main LLM service
            services.AddScoped<ILLMService, OllamaLLMService>();

            // Register streaming service (removed SignalR server dependency for MAUI compatibility)
            // Note: SignalR client connection should be managed in the consuming application
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
    }

    /// <summary>
    /// Configuration options for the Ollama LLM service.
    /// </summary>
    public class OllamaLLMOptions
    {
        /// <summary>
        /// Gets or sets the base URL for the Ollama server.
        /// </summary>
        public string BaseUrl { get; set; } = "http://100.108.155.28:11434";

        /// <summary>
        /// Gets or sets the request timeout duration.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets the default model to use for general queries.
        /// </summary>
        public string DefaultModel { get; set; } = "llama3.2";

        /// <summary>
        /// Gets or sets the model to use for code-related queries.
        /// </summary>
        public string CodeModel { get; set; } = "deepseek-coder";

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
    }
}
