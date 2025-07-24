using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using JarvisAssistant.Core.Models;
using JarvisAssistant.Services;
using JarvisAssistant.Services.LLM;

namespace JarvisAssistant.UnitTests.Services
{
    public class OllamaLLMServiceTests
    {
        private readonly Mock<IOllamaClient> _mockOllamaClient;
        private readonly Mock<IPersonalityService> _mockPersonalityService;
        private readonly Mock<ILogger<OllamaLLMService>> _mockLogger;
        private readonly OllamaLLMService _llmService;

        public OllamaLLMServiceTests()
        {
            _mockOllamaClient = new Mock<IOllamaClient>();
            _mockPersonalityService = new Mock<IPersonalityService>();
            _mockLogger = new Mock<ILogger<OllamaLLMService>>();
            
            _llmService = new OllamaLLMService(
                _mockOllamaClient.Object,
                _mockPersonalityService.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task SendMessageAsync_WithValidRequest_ReturnsFormattedResponse()
        {
            // Arrange
            var request = new ChatRequest("Hello, how are you?", "conv123");
            var rawResponse = "I am functioning normally.";
            var formattedResponse = "Indeed, Sir. I am functioning quite normally.";
            var systemPrompt = "You are JARVIS...";

            _mockPersonalityService.Setup(x => x.GetSystemPrompt(It.IsAny<QueryType>()))
                .Returns(systemPrompt);

            _mockOllamaClient.Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<QueryType>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(rawResponse);

            _mockPersonalityService.Setup(x => x.FormatResponseAsync(rawResponse, It.IsAny<QueryType>(), false))
                .ReturnsAsync(formattedResponse);

            // Act
            var result = await _llmService.SendMessageAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Message.Should().Be(formattedResponse);
            result.Type.Should().Be("assistant");
            result.IsComplete.Should().BeTrue();
            result.Metadata.Should().ContainKey("queryType");
            result.Metadata.Should().ContainKey("conversationId");
        }

        [Fact]
        public async Task SendMessageAsync_WithNullRequest_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _llmService.SendMessageAsync(null!));
        }

        [Fact]
        public async Task SendMessageAsync_WithEmptyMessage_ThrowsArgumentException()
        {
            // Arrange
            var request = new ChatRequest("", "conv123");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => _llmService.SendMessageAsync(request));
            exception.ParamName.Should().Be("request");
        }

        [Fact]
        public async Task SendMessageAsync_WithOllamaError_ReturnsErrorResponse()
        {
            // Arrange
            var request = new ChatRequest("Test message", "conv123");
            _mockPersonalityService.Setup(x => x.GetSystemPrompt(It.IsAny<QueryType>()))
                .Returns("System prompt");

            _mockOllamaClient.Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<QueryType>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Ollama connection failed"));

            // Act
            var result = await _llmService.SendMessageAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Type.Should().Be("error");
            result.Message.Should().Contain("complication");
            result.Metadata.Should().ContainKey("error");
        }

        [Fact]
        public async Task SendMessageAsync_WithEmptyOllamaResponse_UsesDefaultErrorMessage()
        {
            // Arrange
            var request = new ChatRequest("Test message", "conv123");
            _mockPersonalityService.Setup(x => x.GetSystemPrompt(It.IsAny<QueryType>()))
                .Returns("System prompt");

            _mockOllamaClient.Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<QueryType>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(string.Empty);

            _mockPersonalityService.Setup(x => x.FormatResponseAsync(It.IsAny<string>(), It.IsAny<QueryType>(), false))
                .ReturnsAsync((string response, QueryType _, bool _) => response);

            // Act
            var result = await _llmService.SendMessageAsync(request);

            // Assert
            result.Message.Should().Contain("processing difficulty");
        }

        [Theory]
        [InlineData("write a function", QueryType.Code)]
        [InlineData("how to fix this error", QueryType.Error)]
        [InlineData("explain the architecture", QueryType.Technical)]
        [InlineData("calculate the sum", QueryType.Mathematical)]
        [InlineData("write a story", QueryType.Creative)]
        [InlineData("hello there", QueryType.General)]
        public async Task SendMessageAsync_DetectsCorrectQueryType(string message, QueryType expectedQueryType)
        {
            // Arrange
            var request = new ChatRequest(message, "conv123");
            QueryType detectedQueryType = QueryType.General;

            _mockPersonalityService.Setup(x => x.GetSystemPrompt(It.IsAny<QueryType>()))
                .Returns("System prompt")
                .Callback<QueryType>(qt => detectedQueryType = qt);

            _mockOllamaClient.Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<QueryType>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("Response");

            _mockPersonalityService.Setup(x => x.FormatResponseAsync(It.IsAny<string>(), It.IsAny<QueryType>(), false))
                .ReturnsAsync("Formatted response");

            // Act
            await _llmService.SendMessageAsync(request);

            // Assert
            detectedQueryType.Should().Be(expectedQueryType);
        }

        [Fact]
        public async Task SendMessageAsync_WithExplicitQueryTypeInContext_UsesProvidedQueryType()
        {
            // Arrange
            var request = new ChatRequest("hello", "conv123")
            {
                Context = new Dictionary<string, object> { ["queryType"] = "Code" }
            };

            QueryType usedQueryType = QueryType.General;
            _mockPersonalityService.Setup(x => x.GetSystemPrompt(It.IsAny<QueryType>()))
                .Returns("System prompt")
                .Callback<QueryType>(qt => usedQueryType = qt);

            _mockOllamaClient.Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<QueryType>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("Response");

            _mockPersonalityService.Setup(x => x.FormatResponseAsync(It.IsAny<string>(), It.IsAny<QueryType>(), false))
                .ReturnsAsync("Formatted response");

            // Act
            await _llmService.SendMessageAsync(request);

            // Assert
            usedQueryType.Should().Be(QueryType.Code);
        }

        [Fact]
        public async Task StreamResponseAsync_WithValidRequest_YieldsResponseChunks()
        {
            // Arrange
            var request = new ChatRequest("Test streaming", "conv123");
            var greeting = "Certainly, Sir.";
            var streamChunks = new[] { "Hello", " ", "world" };

            _mockPersonalityService.Setup(x => x.GetSystemPrompt(It.IsAny<QueryType>()))
                .Returns("System prompt");

            _mockPersonalityService.Setup(x => x.GetContextualGreeting(It.IsAny<QueryType>()))
                .Returns(greeting);

            _mockOllamaClient.Setup(x => x.StreamGenerateAsync(It.IsAny<string>(), It.IsAny<QueryType>(), It.IsAny<CancellationToken>()))
                .Returns(streamChunks.ToAsyncEnumerable());

            _mockPersonalityService.Setup(x => x.FormatResponseAsync(It.IsAny<string>(), It.IsAny<QueryType>(), true))
                .ReturnsAsync((string response, QueryType _, bool _) => response);

            _mockPersonalityService.Setup(x => x.FormatResponseAsync(It.IsAny<string>(), It.IsAny<QueryType>(), false))
                .ReturnsAsync((string response, QueryType _, bool _) => response);

            // Act
            var responses = new List<ChatResponse>();
            await foreach (var response in _llmService.StreamResponseAsync(request))
            {
                responses.Add(response);
            }

            // Assert
            responses.Should().HaveCount(5); // Greeting + 3 chunks + final response
            responses[0].Message.Should().Be(greeting);
            responses[0].IsComplete.Should().BeFalse();
            responses[1].Message.Should().Be("Hello");
            responses[2].Message.Should().Be(" ");
            responses[3].Message.Should().Be("world");
            responses[4].IsComplete.Should().BeTrue();
        }

        [Fact]
        public async Task StreamResponseAsync_WithNullRequest_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await foreach (var _ in _llmService.StreamResponseAsync(null!))
                {
                    // Should not reach here
                }
            });
        }

        [Fact]
        public async Task StreamResponseAsync_WithEmptyMessage_ThrowsArgumentException()
        {
            // Arrange
            var request = new ChatRequest("", "conv123");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await foreach (var _ in _llmService.StreamResponseAsync(request))
                {
                    // Should not reach here
                }
            });
            exception.ParamName.Should().Be("request");
        }

        [Fact]
        public async Task StreamResponseAsync_WithOllamaError_YieldsErrorResponse()
        {
            // Arrange
            var request = new ChatRequest("Test message", "conv123");
            _mockPersonalityService.Setup(x => x.GetSystemPrompt(It.IsAny<QueryType>()))
                .Returns("System prompt");

            _mockPersonalityService.Setup(x => x.GetContextualGreeting(It.IsAny<QueryType>()))
                .Returns("Greeting");

            _mockOllamaClient.Setup(x => x.StreamGenerateAsync(It.IsAny<string>(), It.IsAny<QueryType>(), It.IsAny<CancellationToken>()))
                .Throws(new Exception("Streaming failed"));

            // Act
            var responses = new List<ChatResponse>();
            await foreach (var response in _llmService.StreamResponseAsync(request))
            {
                responses.Add(response);
            }

            // Assert
            responses.Should().HaveCount(1);
            responses[0].Type.Should().Be("error");
            responses[0].Message.Should().Contain("complication");
        }

        [Fact]
        public async Task GetActiveModelAsync_WithAvailableModels_ReturnsActiveModel()
        {
            // Arrange
            var availableModels = new List<string> { "llama3.2", "deepseek-coder" };
            _mockOllamaClient.Setup(x => x.GetAvailableModelsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(availableModels);

            // Act
            var result = await _llmService.GetActiveModelAsync();

            // Assert
            result.Should().Be("llama3.2"); // Default active model
        }

        [Fact]
        public async Task GetActiveModelAsync_WithNoAvailableModels_ReturnsErrorMessage()
        {
            // Arrange
            _mockOllamaClient.Setup(x => x.GetAvailableModelsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string>());

            // Act
            var result = await _llmService.GetActiveModelAsync();

            // Assert
            result.Should().Be("No models available");
        }

        [Fact]
        public async Task GetActiveModelAsync_WithException_ReturnsErrorMessage()
        {
            // Arrange
            _mockOllamaClient.Setup(x => x.GetAvailableModelsAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Connection failed"));

            // Act
            var result = await _llmService.GetActiveModelAsync();

            // Assert
            result.Should().StartWith("Error:");
        }

        [Fact]
        public void Constructor_WithNullOllamaClient_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new OllamaLLMService(
                null!,
                _mockPersonalityService.Object,
                _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullPersonalityService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new OllamaLLMService(
                _mockOllamaClient.Object,
                null!,
                _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new OllamaLLMService(
                _mockOllamaClient.Object,
                _mockPersonalityService.Object,
                null!));
        }
    }

    // Extension method to create async enumerable from array
    public static class AsyncEnumerableExtensions
    {
        public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> enumerable)
        {
            foreach (var item in enumerable)
            {
                await Task.Yield(); // Add async behavior
                yield return item;
            }
        }
    }
}
