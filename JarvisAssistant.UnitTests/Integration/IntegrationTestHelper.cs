using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Services.Extensions;

namespace JarvisAssistant.UnitTests.Integration
{
    /// <summary>
    /// Helper class for managing integration test configuration and setup.
    /// </summary>
    public static class IntegrationTestHelper
    {
        private const string DefaultOllamaUrl = "http://100.108.155.28:11434";
        private static readonly TimeSpan HealthCheckTimeout = TimeSpan.FromSeconds(5);
        private static readonly string[] RequiredModels = { "llama3.2", "deepseek-coder" };

        /// <summary>
        /// Checks if Ollama is available and responsive with required models.
        /// </summary>
        /// <param name="ollamaUrl">The Ollama URL to check. If null, uses the default URL.</param>
        /// <returns>True if Ollama is available with required models, false otherwise.</returns>
        public static async Task<bool> IsOllamaAvailableAsync(string? ollamaUrl = null)
        {
            var url = ollamaUrl ?? DefaultOllamaUrl;
            
            try
            {
                using var httpClient = new HttpClient { Timeout = HealthCheckTimeout };
                var response = await httpClient.GetAsync($"{url}/api/tags");
                
                if (!response.IsSuccessStatusCode)
                    return false;

                var jsonContent = await response.Content.ReadAsStringAsync();
                var modelsResponse = JsonSerializer.Deserialize<OllamaModelsResponse>(jsonContent);
                
                if (modelsResponse?.Models == null)
                    return false;

                var availableModels = modelsResponse.Models.Select(m => m.Name).ToList();
                
                // Check if all required models are available
                var missingModels = RequiredModels.Except(availableModels).ToList();
                
                if (missingModels.Any())
                {
                    Console.WriteLine($"Missing required models: {string.Join(", ", missingModels)}");
                    Console.WriteLine($"Available models: {string.Join(", ", availableModels)}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ollama availability check failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Skips a test if Ollama is not available by returning true if the test should be skipped.
        /// </summary>
        /// <param name="ollamaUrl">The Ollama URL to check. If null, uses the default URL.</param>
        /// <returns>True if the test should be skipped, false if it should continue.</returns>
        public static async Task<bool> ShouldSkipIfOllamaNotAvailableAsync(string? ollamaUrl = null)
        {
            var isAvailable = await IsOllamaAvailableAsync(ollamaUrl);
            if (!isAvailable)
            {
                // Write to test output that we're skipping
                Console.WriteLine("SKIPPED: Ollama instance is not available or missing required models. Please ensure Ollama is running with llama3.2 and deepseek-coder models.");
            }
            return !isAvailable;
        }

        /// <summary>
        /// Creates a service provider configured for integration testing.
        /// </summary>
        /// <param name="ollamaUrl">The Ollama URL to use. If null, uses the default URL.</param>
        /// <returns>A configured service provider.</returns>
        public static ServiceProvider CreateTestServiceProvider(string? ollamaUrl = null)
        {
            var services = new ServiceCollection();
            
            // Add logging for tests
            services.AddLogging(builder => 
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });
            
            // Add LLM services with custom URL if provided
            if (ollamaUrl != null)
            {
                services.AddOllamaLLMService(ollamaUrl);
            }
            else
            {
                services.AddOllamaLLMService();
            }
            
            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Gets the LLM service from a service provider, checking Ollama availability first.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="ollamaUrl">The Ollama URL to check. If null, uses the default URL.</param>
        /// <returns>The LLM service if Ollama is available.</returns>
        public static async Task<ILLMService> GetLLMServiceAsync(ServiceProvider serviceProvider, string? ollamaUrl = null)
        {
            if (await ShouldSkipIfOllamaNotAvailableAsync(ollamaUrl))
            {
                throw new InvalidOperationException("Ollama is not available");
            }
            return serviceProvider.GetRequiredService<ILLMService>();
        }
    }

    /// <summary>
    /// Response model for Ollama models API.
    /// </summary>
    internal class OllamaModelsResponse
    {
        public List<OllamaModel>? Models { get; set; }
    }

    /// <summary>
    /// Model information from Ollama API.
    /// </summary>
    internal class OllamaModel
    {
        public string Name { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string Digest { get; set; } = string.Empty;
    }
}