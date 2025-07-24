using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Contrib.HttpClient;
using Moq.Protected;
using JarvisAssistant.Core.Models;
using JarvisAssistant.Services.LLM;
using JarvisAssistant.Services.Extensions;
using static Moq.Protected.ItExpr;

namespace JarvisAssistant.UnitTests.Services.LLM
{
    public class OllamaClientTests : IDisposable
    {
        private readonly Mock<ILogger<OllamaClient>> _mockLogger;
        private readonly Mock<HttpMessageHandler> _mockHttpHandler;
        private readonly HttpClient _httpClient;
        private readonly OllamaClient _ollamaClient;

        public OllamaClientTests()
        {
            _mockLogger = new Mock<ILogger<OllamaClient>>();
            _mockHttpHandler = new Mock<HttpMessageHandler>();
            
            // Create HttpClient with the mock handler
            _httpClient = _mockHttpHandler.CreateClient();
            _httpClient.BaseAddress = new Uri("http://localhost:11434");
            
            // Create OllamaClient with pre-configured HttpClient
            _ollamaClient = new OllamaClient(_httpClient, _mockLogger.Object);
        }

        [Fact]
        public async Task GenerateAsync_WithValidRequest_ReturnsResponse()
        {
            // Arrange
            var expectedResponse = new { response = "Test response from Ollama", done = true };
            var responseJson = JsonSerializer.Serialize(expectedResponse);
            
            _mockHttpHandler.SetupRequest(HttpMethod.Post, "http://localhost:11434/api/generate")
                .ReturnsResponse(HttpStatusCode.OK, responseJson, "application/json");

            // Act
            var result = await _ollamaClient.GenerateAsync("Test prompt", QueryType.General);

            // Assert
            result.Should().Be("Test response from Ollama");
        }

        [Fact]
        public async Task GenerateAsync_WithCodeQueryType_UsesCorrectModel()
        {
            // Arrange
            var expectedResponse = new { response = "Code response", done = true };
            var responseJson = JsonSerializer.Serialize(expectedResponse);
            
            _mockHttpHandler.SetupRequest(HttpMethod.Post, "http://localhost:11434/api/generate")
                .ReturnsResponse(HttpStatusCode.OK, responseJson, "application/json");

            // Act
            var result = await _ollamaClient.GenerateAsync("Write a function", QueryType.Code);

            // Assert
            result.Should().Be("Code response");
            
            // Verify the request was made with the correct model
            _mockHttpHandler.VerifyRequest(HttpMethod.Post, "http://localhost:11434/api/generate", Times.Once());
        }

        [Fact]
        public async Task GenerateAsync_WithHttpError_ThrowsInvalidOperationException()
        {
            // Arrange
            _mockHttpHandler.SetupRequest(HttpMethod.Post, "http://localhost:11434/api/generate")
                .ReturnsResponse(HttpStatusCode.InternalServerError, "Internal Server Error");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _ollamaClient.GenerateAsync("Test prompt"));
            
            exception.Message.Should().Contain("Ollama API returned InternalServerError");
        }

        [Fact]
        public async Task GenerateAsync_WithNetworkError_ThrowsInvalidOperationException()
        {
            // Arrange - Create properly configured HTTP mock
            var mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            
            var httpClient = new HttpClient(mockHandler.Object)
            {
                BaseAddress = new Uri("http://localhost:11434"),
                Timeout = TimeSpan.FromMinutes(5)
            };
            
            // Create options that exactly match the HttpClient configuration
            var options = Options.Create(new OllamaLLMOptions
            {
                BaseUrl = "http://localhost:11434",  // Matches httpClient.BaseAddress
                Timeout = TimeSpan.FromMinutes(5)    // Matches httpClient.Timeout
            });
            
            var ollamaClient = new OllamaClient(httpClient, _mockLogger.Object, options);

            // Setup the mock to throw HttpRequestException AFTER creating the client
            mockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Connection refused"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => ollamaClient.GenerateAsync("Test prompt"));
            
            exception.Message.Should().Contain("Unable to connect to Ollama server");
        }

        [Fact]
        public async Task StreamGenerateAsync_WithValidRequest_YieldsResponseChunks()
        {
            // Arrange
            var streamData = new[]
            {
                new { response = "Hello", done = false },
                new { response = " ", done = false },
                new { response = "world", done = false },
                new { response = "", done = true }
            };

            var streamContent = string.Join("\n", streamData.Select(x => JsonSerializer.Serialize(x)));
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(streamContent));

            _mockHttpHandler.SetupRequest(HttpMethod.Post, "http://localhost:11434/api/generate")
                .ReturnsResponse(HttpStatusCode.OK, stream, "application/json");

            // Act
            var chunks = new List<string>();
            await foreach (var chunk in _ollamaClient.StreamGenerateAsync("Test prompt"))
            {
                chunks.Add(chunk);
            }

            // Assert - expect 4 chunks including the empty one from done=true
            chunks.Should().HaveCount(4);
            chunks[0].Should().Be("Hello");
            chunks[1].Should().Be(" ");
            chunks[2].Should().Be("world");
            chunks[3].Should().Be("");
        }

        [Fact]
        public async Task GetAvailableModelsAsync_WithValidResponse_ReturnsModelList()
        {
            // Arrange
            var modelsResponse = new
            {
                models = new[]
                {
                    new { name = "llama3.2", size = "4.7GB", digest = "abc123" },
                    new { name = "deepseek-coder", size = "6.9GB", digest = "def456" }
                }
            };

            var responseJson = JsonSerializer.Serialize(modelsResponse);
            
            _mockHttpHandler.SetupRequest(HttpMethod.Get, "http://localhost:11434/api/tags")
                .ReturnsResponse(HttpStatusCode.OK, responseJson, "application/json");

            // Act
            var result = await _ollamaClient.GetAvailableModelsAsync();

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain("llama3.2");
            result.Should().Contain("deepseek-coder");
        }

        [Fact]
        public async Task GetAvailableModelsAsync_WithError_ReturnsEmptyList()
        {
            // Arrange
            _mockHttpHandler.SetupRequest(HttpMethod.Get, "http://localhost:11434/api/tags")
                .ReturnsResponse(HttpStatusCode.InternalServerError);

            // Act
            var result = await _ollamaClient.GetAvailableModelsAsync();

            // Assert
            result.Should().BeEmpty();
        }

        [Theory]
        [InlineData(QueryType.General, "llama3.2")]
        [InlineData(QueryType.Code, "deepseek-coder")]
        [InlineData(QueryType.Technical, "llama3.2")]
        [InlineData(QueryType.Error, "deepseek-coder")]
        [InlineData(QueryType.Mathematical, "llama3.2")]
        [InlineData(QueryType.Creative, "llama3.2")]
        public async Task GenerateAsync_WithDifferentQueryTypes_UsesCorrectModel(QueryType queryType, string expectedModel)
        {
            // Arrange
            var expectedResponse = new { response = "Test response", done = true };
            var responseJson = JsonSerializer.Serialize(expectedResponse);
            
            var httpRequestCapture = new List<HttpRequestMessage>();
            _mockHttpHandler.SetupRequest(HttpMethod.Post, "http://localhost:11434/api/generate")
                .ReturnsResponse(HttpStatusCode.OK, responseJson, "application/json")
                .Callback<HttpRequestMessage, CancellationToken>((request, ct) => httpRequestCapture.Add(request));

            // Act
            var result = await _ollamaClient.GenerateAsync("Test prompt", queryType);

            // Assert
            result.Should().Be("Test response");
            
            // Verify the correct model was used in the request
            httpRequestCapture.Should().HaveCount(1);
            var requestContent = await httpRequestCapture[0].Content!.ReadAsStringAsync();
            requestContent.Should().Contain($"\"model\":\"{expectedModel}\"");
        }

        [Fact]
        public void Constructor_WithNullHttpClient_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new OllamaClient(null!, _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new OllamaClient(_httpClient, null!));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _httpClient?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
