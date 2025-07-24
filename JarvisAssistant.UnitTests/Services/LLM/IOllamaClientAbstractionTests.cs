using FluentAssertions;
using Moq;
using JarvisAssistant.Services.LLM;
using JarvisAssistant.Core.Models;

namespace JarvisAssistant.UnitTests.Services.LLM
{
    /// <summary>
    /// Tests for IOllamaClient interface abstraction to verify it can be properly mocked and tested.
    /// </summary>
    public class IOllamaClientAbstractionTests
    {
        [Fact]
        public void IOllamaClient_CanBeCreatedAsMock()
        {
            // Arrange & Act
            var mockOllamaClient = new Mock<IOllamaClient>();

            // Assert
            mockOllamaClient.Should().NotBeNull();
            mockOllamaClient.Object.Should().NotBeNull();
        }

        [Fact]
        public async Task IOllamaClient_GenerateAsync_CanBeMocked()
        {
            // Arrange
            var mockOllamaClient = new Mock<IOllamaClient>();
            var expectedResponse = "Mocked response from Ollama";
            
            mockOllamaClient.Setup(x => x.GenerateAsync(
                It.IsAny<string>(), 
                It.IsAny<QueryType>(), 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await mockOllamaClient.Object.GenerateAsync("Test prompt", QueryType.General);

            // Assert
            result.Should().Be(expectedResponse);
            mockOllamaClient.Verify(x => x.GenerateAsync("Test prompt", QueryType.General, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task IOllamaClient_StreamGenerateAsync_CanBeMocked()
        {
            // Arrange
            var mockOllamaClient = new Mock<IOllamaClient>();
            var expectedChunks = new[] { "Hello", " ", "World", "!" };
            
            mockOllamaClient.Setup(x => x.StreamGenerateAsync(
                It.IsAny<string>(), 
                It.IsAny<QueryType>(), 
                It.IsAny<CancellationToken>()))
                .Returns(expectedChunks.ToAsyncEnumerable());

            // Act
            var chunks = new List<string>();
            await foreach (var chunk in mockOllamaClient.Object.StreamGenerateAsync("Test prompt", QueryType.General))
            {
                chunks.Add(chunk);
            }

            // Assert
            chunks.Should().BeEquivalentTo(expectedChunks);
            mockOllamaClient.Verify(x => x.StreamGenerateAsync("Test prompt", QueryType.General, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task IOllamaClient_GetAvailableModelsAsync_CanBeMocked()
        {
            // Arrange
            var mockOllamaClient = new Mock<IOllamaClient>();
            var expectedModels = new List<string> { "llama3.2", "deepseek-coder", "qwen2.5" };
            
            mockOllamaClient.Setup(x => x.GetAvailableModelsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedModels);

            // Act
            var result = await mockOllamaClient.Object.GetAvailableModelsAsync();

            // Assert
            result.Should().BeEquivalentTo(expectedModels);
            mockOllamaClient.Verify(x => x.GetAvailableModelsAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task IOllamaClient_AllMethods_CanBeUsedInTestableService()
        {
            // Arrange
            var mockOllamaClient = new Mock<IOllamaClient>();
            
            mockOllamaClient.Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<QueryType>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("Generated response");
            
            mockOllamaClient.Setup(x => x.GetAvailableModelsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string> { "test-model" });
            
            mockOllamaClient.Setup(x => x.StreamGenerateAsync(It.IsAny<string>(), It.IsAny<QueryType>(), It.IsAny<CancellationToken>()))
                .Returns(new[] { "chunk1", "chunk2" }.ToAsyncEnumerable());

            var testableService = new TestableServiceUsingIOllamaClient(mockOllamaClient.Object);

            // Act
            var generateResult = await testableService.GenerateResponseAsync("test prompt");
            var modelsResult = await testableService.GetModelsAsync();
            var streamResult = await testableService.GetStreamedResponseAsync("test prompt");

            // Assert
            generateResult.Should().Be("Generated response");
            modelsResult.Should().ContainSingle("test-model");
            streamResult.Should().Be("chunk1chunk2");

            // Verify all methods were called
            mockOllamaClient.Verify(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<QueryType>(), It.IsAny<CancellationToken>()), Times.Once);
            mockOllamaClient.Verify(x => x.GetAvailableModelsAsync(It.IsAny<CancellationToken>()), Times.Once);
            mockOllamaClient.Verify(x => x.StreamGenerateAsync(It.IsAny<string>(), It.IsAny<QueryType>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    /// <summary>
    /// Example testable service that uses IOllamaClient interface for demonstration.
    /// </summary>
    public class TestableServiceUsingIOllamaClient
    {
        private readonly IOllamaClient _ollamaClient;

        public TestableServiceUsingIOllamaClient(IOllamaClient ollamaClient)
        {
            _ollamaClient = ollamaClient;
        }

        public async Task<string> GenerateResponseAsync(string prompt)
        {
            return await _ollamaClient.GenerateAsync(prompt, QueryType.General);
        }

        public async Task<List<string>> GetModelsAsync()
        {
            return await _ollamaClient.GetAvailableModelsAsync();
        }

        public async Task<string> GetStreamedResponseAsync(string prompt)
        {
            var result = "";
            await foreach (var chunk in _ollamaClient.StreamGenerateAsync(prompt, QueryType.General))
            {
                result += chunk;
            }
            return result;
        }
    }
}

/// <summary>
/// Extension methods to support async enumerable in tests.
/// </summary>
public static class AsyncEnumerableExtensions
{
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> enumerable)
    {
        foreach (var item in enumerable)
        {
            yield return item;
        }
    }
}