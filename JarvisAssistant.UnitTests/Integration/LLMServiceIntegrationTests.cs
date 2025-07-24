using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models;
using JarvisAssistant.Services.Extensions;

namespace JarvisAssistant.UnitTests.Integration
{
    /// <summary>
    /// Integration tests for the LLM service components.
    /// These tests require a running Ollama instance and should be run as part of integration testing.
    /// </summary>
    public class LLMServiceIntegrationTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly ILLMService _llmService;

        public LLMServiceIntegrationTests()
        {
            _serviceProvider = IntegrationTestHelper.CreateTestServiceProvider();
            _llmService = _serviceProvider.GetRequiredService<ILLMService>();
        }

        [Fact]
        public async Task SendMessageAsync_WithRealOllamaInstance_ReturnsJarvisResponse()
        {
            // Skip if Ollama is not available
            if (await IntegrationTestHelper.ShouldSkipIfOllamaNotAvailableAsync())
                return;

            // Arrange
            var request = new ChatRequest("Hello, who are you?", "integration-test");

            // Act
            var response = await _llmService.SendMessageAsync(request);

            // Assert
            response.Should().NotBeNull();
            response.Message.Should().NotBeEmpty();
            response.Message.ToLowerInvariant().Should().Contain("jarvis");
            response.Type.Should().Be("assistant");
            response.IsComplete.Should().BeTrue();
        }

        [Fact]
        public async Task SendMessageAsync_WithCodeQuery_UsesCodeModel()
        {
            // Skip if Ollama is not available
            if (await IntegrationTestHelper.ShouldSkipIfOllamaNotAvailableAsync())
                return;

            // Arrange
            var request = new ChatRequest("Write a simple Hello World function in C#", "integration-test")
            {
                Context = new Dictionary<string, object> { ["queryType"] = "Code" }
            };

            // Act
            var response = await _llmService.SendMessageAsync(request);

            // Assert
            response.Should().NotBeNull();
            response.Message.Should().NotBeEmpty();
            response.Message.ToLowerInvariant().Should().Contain("sir");
            response.Metadata.Should().ContainKey("queryType");
            response.Metadata!["queryType"].Should().Be("Code");
        }

        [Fact]
        public async Task StreamResponseAsync_WithRealOllamaInstance_StreamsJarvisResponse()
        {
            // Skip if Ollama is not available
            if (await IntegrationTestHelper.ShouldSkipIfOllamaNotAvailableAsync())
                return;

            // Arrange
            var request = new ChatRequest("Tell me about artificial intelligence", "integration-test");
            var responses = new List<ChatResponse>();

            // Act
            await foreach (var response in _llmService.StreamResponseAsync(request))
            {
                responses.Add(response);
                
                // Break after receiving a few chunks to avoid long test runs
                if (responses.Count >= 10)
                    break;
            }

            // Assert
            responses.Should().NotBeEmpty();
            responses[0].Message.Should().NotBeEmpty(); // First response should be greeting
            responses.Should().Contain(r => !r.IsComplete); // Should have streaming chunks
            responses.All(r => r.Type == "assistant" || r.Type == "error").Should().BeTrue();
        }

        [Fact]
        public async Task GetActiveModelAsync_WithRealOllamaInstance_ReturnsValidModel()
        {
            // Skip if Ollama is not available
            if (await IntegrationTestHelper.ShouldSkipIfOllamaNotAvailableAsync())
                return;

            // Act
            var activeModel = await _llmService.GetActiveModelAsync();

            // Assert
            activeModel.Should().NotBeEmpty();
            activeModel.Should().NotStartWith("Error:");
            activeModel.Should().NotBe("No models available");
        }

        [Theory]
        [InlineData("Write a function to sort an array", QueryType.Code)]
        [InlineData("Explain quantum computing", QueryType.Technical)]
        [InlineData("How are you today?", QueryType.General)]
        [InlineData("I'm getting a null reference exception", QueryType.Error)]
        [InlineData("Calculate the square root of 144", QueryType.Mathematical)]
        [InlineData("Write a short poem about AI", QueryType.Creative)]
        public async Task SendMessageAsync_WithDifferentQueryTypes_ReturnsAppropriateResponses(string message, QueryType expectedQueryType)
        {
            // Skip if Ollama is not available
            if (await IntegrationTestHelper.ShouldSkipIfOllamaNotAvailableAsync())
                return;

            // Arrange
            var request = new ChatRequest(message, "integration-test");

            // Act
            var response = await _llmService.SendMessageAsync(request);

            // Assert
            response.Should().NotBeNull();
            response.Message.Should().NotBeEmpty();
            response.Metadata.Should().ContainKey("queryType");
            response.Metadata!["queryType"].ToString().Should().Be(expectedQueryType.ToString());
        }

        [Fact]
        public async Task SendMessageAsync_PersonalityConsistency_MaintainsJarvisCharacter()
        {
            // Skip if Ollama is not available
            if (await IntegrationTestHelper.ShouldSkipIfOllamaNotAvailableAsync())
                return;

            // Arrange
            var requests = new[]
            {
                new ChatRequest("What's the weather like?", "personality-test-1"),
                new ChatRequest("Help me debug this code error", "personality-test-2"),
                new ChatRequest("Explain machine learning", "personality-test-3")
            };

            var responses = new List<ChatResponse>();

            // Act
            foreach (var request in requests)
            {
                var response = await _llmService.SendMessageAsync(request);
                responses.Add(response);
            }

            // Assert
            responses.Should().HaveCount(3);
            responses.Should().AllSatisfy(response =>
            {
                response.Message.Should().NotBeEmpty();
                // Check for British/sophisticated language patterns
                var message = response.Message.ToLowerInvariant();
                var hasBritishElements = message.Contains("sir") || 
                                       message.Contains("rather") || 
                                       message.Contains("quite") || 
                                       message.Contains("indeed") ||
                                       message.Contains("certainly");
                hasBritishElements.Should().BeTrue("Response should contain British/sophisticated language elements");
            });
        }

        [Fact]
        public async Task StreamResponseAsync_CancellationToken_CancelsGracefully()
        {
            // Skip if Ollama is not available
            if (await IntegrationTestHelper.ShouldSkipIfOllamaNotAvailableAsync())
                return;

            // Arrange
            var request = new ChatRequest("Write a very long explanation about the universe", "cancellation-test");
            using var cts = new CancellationTokenSource();
            var responses = new List<ChatResponse>();

            // Act
            cts.CancelAfter(TimeSpan.FromSeconds(2)); // Cancel after 2 seconds

            var cancellationOccurred = false;
            try
            {
                await foreach (var response in _llmService.StreamResponseAsync(request, cts.Token))
                {
                    responses.Add(response);
                }
            }
            catch (OperationCanceledException)
            {
                cancellationOccurred = true;
            }

            // Assert
            cancellationOccurred.Should().BeTrue("Operation should have been cancelled");
            responses.Should().NotBeEmpty("Should have received at least some responses before cancellation");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _serviceProvider?.Dispose();
            }
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
