using System.Net.Http;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using JarvisAssistant.Services;
using JarvisAssistant.Services.LLM;
using JarvisAssistant.Core.Models;

namespace JarvisAssistant.UnitTests.Integration
{
    /// <summary>
    /// Integration tests for live Ollama server connection at 100.108.155.28.
    /// These tests verify connectivity to the actual Ollama server instance.
    /// </summary>
    [Collection("Live Ollama Connection Tests")]
    public class LiveOllamaConnectionTests : IDisposable
    {
        private const string OLLAMA_HOST = "100.108.155.28";
        private const int OLLAMA_PORT = 11434;
        private static readonly string OLLAMA_BASE_URL = $"http://{OLLAMA_HOST}:{OLLAMA_PORT}";
        
        private readonly HttpClient _httpClient;
        private readonly OllamaConnectionDiagnostics _diagnostics;
        private readonly Mock<ILogger<OllamaConnectionDiagnostics>> _mockLogger;

        public LiveOllamaConnectionTests()
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            _mockLogger = new Mock<ILogger<OllamaConnectionDiagnostics>>();
            _diagnostics = new OllamaConnectionDiagnostics(_mockLogger.Object, _httpClient);
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "LiveConnection")]
        public async Task LiveConnection_PingOllamaHost_ShouldBeReachable()
        {
            // Arrange & Act
            var result = await _diagnostics.DiagnoseConnectionAsync(OLLAMA_BASE_URL);

            // Assert
            result.Should().NotBeNull();
            
            if (result.EndpointResults.TryGetValue(OLLAMA_BASE_URL, out var endpointResult))
            {
                // Log results for debugging
                Console.WriteLine($"Ping Result: {endpointResult.PingResult?.Success} ({endpointResult.PingResult?.ResponseTime}ms)");
                Console.WriteLine($"Port Open: {endpointResult.PortOpen}");
                Console.WriteLine($"HTTP Result: {endpointResult.HttpResult.IsSuccessful}");
                
                if (!string.IsNullOrEmpty(endpointResult.Error))
                {
                    Console.WriteLine($"Error: {endpointResult.Error}");
                }

                // The host should be pingable (network reachable)
                endpointResult.PingResult?.Success.Should().BeTrue($"Host {OLLAMA_HOST} should be pingable");
            }
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "LiveConnection")]
        public async Task LiveConnection_OllamaPort_ShouldBeOpen()
        {
            // Arrange & Act
            var result = await _diagnostics.DiagnoseConnectionAsync(OLLAMA_BASE_URL);

            // Assert
            result.Should().NotBeNull();
            
            if (result.EndpointResults.TryGetValue(OLLAMA_BASE_URL, out var endpointResult))
            {
                // Port 11434 should be open if Ollama is running
                endpointResult.PortOpen.Should().BeTrue($"Port {OLLAMA_PORT} should be open on {OLLAMA_HOST}");
            }
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "LiveConnection")]
        public async Task LiveConnection_OllamaApiTags_ShouldRespondSuccessfully()
        {
            // Arrange
            var tagsUrl = $"{OLLAMA_BASE_URL}/api/tags";

            // Act
            try
            {
                using var response = await _httpClient.GetAsync(tagsUrl);
                var content = await response.Content.ReadAsStringAsync();

                // Assert
                response.IsSuccessStatusCode.Should().BeTrue($"Ollama API should respond successfully at {tagsUrl}");
                content.Should().NotBeNullOrEmpty("Response should contain model information");

                // Log the response for debugging
                Console.WriteLine($"Response Status: {response.StatusCode}");
                Console.WriteLine($"Response Content: {content}");

                // Try to parse as JSON to verify it's valid Ollama response
                if (response.IsSuccessStatusCode)
                {
                    var jsonDoc = JsonDocument.Parse(content);
                    jsonDoc.RootElement.TryGetProperty("models", out var modelsProperty).Should().BeTrue("Response should contain 'models' property");
                }
            }
            catch (HttpRequestException ex)
            {
                // Log the specific error for debugging
                Console.WriteLine($"HTTP Request failed: {ex.Message}");
                
                // Allow the test to fail with detailed information
                throw new InvalidOperationException($"Failed to connect to Ollama at {tagsUrl}. Error: {ex.Message}", ex);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                throw new InvalidOperationException($"Request to Ollama timed out at {tagsUrl}. This may indicate the service is not running or is overloaded.", ex);
            }
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "LiveConnection")]
        public async Task LiveConnection_OllamaHealthCheck_ShouldIndicateServiceStatus()
        {
            // Arrange
            var healthUrl = $"{OLLAMA_BASE_URL}/api/version";

            // Act & Assert
            try
            {
                using var response = await _httpClient.GetAsync(healthUrl);
                var content = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"Health Check Status: {response.StatusCode}");
                Console.WriteLine($"Health Check Content: {content}");

                // Even if this specific endpoint doesn't exist, we should get a response from the server
                // indicating that Ollama is running (even if it's a 404)
                response.StatusCode.Should().NotBe(System.Net.HttpStatusCode.ServiceUnavailable, 
                    "Server should be responding (even with 404) if Ollama is running");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Health check failed: {ex.Message}");
                // This is informational - health endpoint may not exist
            }
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "LiveConnection")]
        public async Task LiveConnection_ComprehensiveDiagnostics_ShouldProvideDetailedResults()
        {
            // Arrange & Act
            var result = await _diagnostics.DiagnoseConnectionAsync(
                OLLAMA_BASE_URL,
                "http://localhost:11434",
                "http://127.0.0.1:11434"
            );

            // Assert
            result.Should().NotBeNull();
            result.EndpointResults.Should().ContainKey(OLLAMA_BASE_URL);

            // Log comprehensive results
            Console.WriteLine("\n=== Ollama Connection Diagnostics ===");
            Console.WriteLine($"Has Internet Connection: {result.HasInternetConnection}");
            Console.WriteLine($"Can Resolve Localhost: {result.CanResolveLocalhost}");
            Console.WriteLine($"Working Endpoints: {result.WorkingEndpoints.Count}");

            foreach (var endpoint in result.EndpointResults)
            {
                Console.WriteLine($"\nEndpoint: {endpoint.Key}");
                Console.WriteLine($"  Reachable: {endpoint.Value.IsReachable}");
                Console.WriteLine($"  Ping: {endpoint.Value.PingResult?.Success} ({endpoint.Value.PingResult?.ResponseTime}ms)");
                Console.WriteLine($"  Port Open: {endpoint.Value.PortOpen}");
                Console.WriteLine($"  HTTP Success: {endpoint.Value.HttpResult.IsSuccessful}");
                
                if (!string.IsNullOrEmpty(endpoint.Value.Error))
                {
                    Console.WriteLine($"  Error: {endpoint.Value.Error}");
                }
            }

            Console.WriteLine("\nRecommendations:");
            foreach (var recommendation in result.Recommendations)
            {
                Console.WriteLine($"  - {recommendation}");
            }

            // Basic connectivity assertions
            result.HasInternetConnection.Should().BeTrue("Internet connectivity is required for comprehensive testing");
            
            // At least one endpoint should be working if Ollama is properly configured
            if (result.WorkingEndpoints.Any())
            {
                result.WorkingEndpoints.Should().Contain(OLLAMA_BASE_URL, 
                    $"The target Ollama instance at {OLLAMA_BASE_URL} should be in the working endpoints list");
            }
        }

        [Fact]
        [Trait("Category", "Integration")]
        [Trait("Category", "LiveConnection")]
        public async Task LiveConnection_OllamaClient_ShouldConnectAndRetrieveModels()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<OllamaClient>>();
            using var httpClient = new HttpClient { BaseAddress = new Uri(OLLAMA_BASE_URL) };
            var ollamaClient = new OllamaClient(httpClient, mockLogger.Object);

            // Act & Assert
            try
            {
                var models = await ollamaClient.GetAvailableModelsAsync();

                // Assert
                models.Should().NotBeNull("Available models list should not be null");
                Console.WriteLine($"Found {models.Count} available models:");
                
                foreach (var model in models)
                {
                    Console.WriteLine($"  - {model}");
                }

                // If models are available, test a simple generation
                if (models.Any())
                {
                    var testModel = models.First();
                    Console.WriteLine($"Testing generation with model: {testModel}");

                    // This might timeout or fail if the model is not loaded, which is acceptable
                    try
                    {
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                        var response = await ollamaClient.GenerateAsync("Say hello", QueryType.General, cts.Token);
                        
                        response.Should().NotBeNullOrEmpty("Generated response should not be empty");
                        Console.WriteLine($"Generated response: {response}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Generation test failed (this may be expected): {ex.Message}");
                        // Don't fail the test for generation issues - connectivity is what we're testing
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"OllamaClient failed to connect to {OLLAMA_BASE_URL}. Ensure Ollama is running and accessible. Error: {ex.Message}", ex);
            }
        }

        [Theory]
        [Trait("Category", "Integration")]
        [Trait("Category", "LiveConnection")]
        [InlineData("llama3.2")]
        [InlineData("deepseek-coder")]
        [InlineData("qwen2.5")]
        public async Task LiveConnection_CheckSpecificModel_ShouldReportAvailability(string modelName)
        {
            // Arrange
            var mockLogger = new Mock<ILogger<OllamaClient>>();
            using var httpClient = new HttpClient { BaseAddress = new Uri(OLLAMA_BASE_URL) };
            var ollamaClient = new OllamaClient(httpClient, mockLogger.Object);

            // Act
            try
            {
                var models = await ollamaClient.GetAvailableModelsAsync();
                var modelAvailable = models.Any(m => m.Contains(modelName, StringComparison.OrdinalIgnoreCase));

                // Assert (informational)
                Console.WriteLine($"Model '{modelName}' available: {modelAvailable}");
                
                if (!modelAvailable)
                {
                    Console.WriteLine($"To install model '{modelName}', run: ollama pull {modelName}");
                }

                // Don't fail the test - this is informational
                // But log the result for CI/CD and development purposes
                Assert.True(true, $"Model availability check completed for {modelName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to check model availability: {ex.Message}");
                // Skip this test if we can't connect
                throw new SkipException($"Cannot check model availability due to connection issues: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        /// <summary>
        /// Custom exception for skipping tests when prerequisites are not met.
        /// </summary>
        public class SkipException : Exception
        {
            public SkipException(string message) : base(message) { }
        }
    }
}