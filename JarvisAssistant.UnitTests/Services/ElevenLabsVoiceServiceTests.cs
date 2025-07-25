using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models;
using JarvisAssistant.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace JarvisAssistant.UnitTests.Services
{
    /// <summary>
    /// Unit tests for ElevenLabs voice service.
    /// </summary>
    public class ElevenLabsVoiceServiceTests : IDisposable
    {
        private readonly Mock<ILogger<ElevenLabsVoiceService>> _mockLogger;
        private readonly Mock<IAudioCacheService> _mockCacheService;
        private readonly Mock<IRateLimitService> _mockRateLimitService;
        private readonly Mock<IVoiceService> _mockFallbackService;
        private readonly Mock<HttpMessageHandler> _mockHttpHandler;
        private readonly HttpClient _httpClient;
        private readonly ElevenLabsConfig _config;
        private readonly ElevenLabsVoiceService _service;
        private bool _disposed = false;

        public ElevenLabsVoiceServiceTests()
        {
            _mockLogger = new Mock<ILogger<ElevenLabsVoiceService>>();
            _mockCacheService = new Mock<IAudioCacheService>();
            _mockRateLimitService = new Mock<IRateLimitService>();
            _mockFallbackService = new Mock<IVoiceService>();
            _mockHttpHandler = new Mock<HttpMessageHandler>();

            _httpClient = new HttpClient(_mockHttpHandler.Object);

            _config = new ElevenLabsConfig
            {
                ApiKey = "test-api-key",
                VoiceId = "test-voice-id",
                BaseUrl = "https://api.elevenlabs.io",
                ModelId = "eleven_multilingual_v2",
                TimeoutSeconds = 30,
                MaxRetryAttempts = 3,
                EnableCaching = true,
                EnableRateLimiting = true,
                EnableFallback = true
            };

            _service = new ElevenLabsVoiceService(
                _httpClient,
                _config,
                _mockLogger.Object,
                _mockCacheService.Object,
                _mockRateLimitService.Object,
                _mockFallbackService.Object);
        }

        [Fact]
        public async Task GenerateSpeechAsync_WithValidText_ReturnsAudioData()
        {
            // Arrange
            var text = "Hello, Sir. How may I assist you today?";
            var expectedAudio = Encoding.UTF8.GetBytes("fake-audio-data");

            _mockCacheService.Setup(x => x.GetCachedAudioAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
                .ReturnsAsync((byte[]?)null);

            _mockRateLimitService.Setup(x => x.CanMakeRequestAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            SetupHttpResponse(HttpStatusCode.OK, expectedAudio);

            // Act
            var result = await _service.GenerateSpeechAsync(text);

            // Assert
            Assert.Equal(expectedAudio, result);
            _mockCacheService.Verify(x => x.CacheAudioAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<object>(), 
                expectedAudio), Times.Once);
        }

        [Fact]
        public async Task GenerateSpeechAsync_WithCachedAudio_ReturnsCachedData()
        {
            // Arrange
            var text = "System status: All systems operational";
            var cachedAudio = Encoding.UTF8.GetBytes("cached-audio-data");

            _mockCacheService.Setup(x => x.GetCachedAudioAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
                .ReturnsAsync(cachedAudio);

            // Act
            var result = await _service.GenerateSpeechAsync(text);

            // Assert
            Assert.Equal(cachedAudio, result);
            _mockHttpHandler.Protected().Verify(
                "SendAsync",
                Times.Never(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }

        [Fact]
        public async Task GenerateSpeechAsync_WhenRateLimited_UsesFallbackService()
        {
            // Arrange
            var text = "Rate limit test";
            var fallbackAudio = Encoding.UTF8.GetBytes("fallback-audio");

            _mockCacheService.Setup(x => x.GetCachedAudioAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
                .ReturnsAsync((byte[]?)null);

            _mockRateLimitService.Setup(x => x.CanMakeRequestAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            _mockFallbackService.Setup(x => x.GenerateSpeechAsync(text, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fallbackAudio);

            // Act
            var result = await _service.GenerateSpeechAsync(text);

            // Assert
            Assert.Equal(fallbackAudio, result);
            _mockFallbackService.Verify(x => x.GenerateSpeechAsync(text, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GenerateSpeechAsync_WithServerError_UsesFallbackService()
        {
            // Arrange
            var text = "Error test";
            var fallbackAudio = Encoding.UTF8.GetBytes("fallback-audio");

            _mockCacheService.Setup(x => x.GetCachedAudioAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
                .ReturnsAsync((byte[]?)null);

            _mockRateLimitService.Setup(x => x.CanMakeRequestAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            _mockFallbackService.Setup(x => x.GenerateSpeechAsync(text, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(fallbackAudio);

            SetupHttpResponse(HttpStatusCode.InternalServerError, Array.Empty<byte>());

            // Act
            var result = await _service.GenerateSpeechAsync(text);

            // Assert
            Assert.Equal(fallbackAudio, result);
            _mockFallbackService.Verify(x => x.GenerateSpeechAsync(text, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GenerateSpeechAsync_WithEmptyText_ReturnsEmptyArray()
        {
            // Act
            var result = await _service.GenerateSpeechAsync("");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GenerateSpeechAsync_WithNullText_ReturnsEmptyArray()
        {
            // Act
            var result = await _service.GenerateSpeechAsync(null!);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task StreamSpeechAsync_WithValidText_ReturnsAudioChunks()
        {
            // Arrange
            var text = "Streaming test message";
            var audioData = Encoding.UTF8.GetBytes("streaming-audio-data-chunk");

            _mockRateLimitService.Setup(x => x.CanMakeRequestAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            SetupStreamingHttpResponse(HttpStatusCode.OK, audioData);

            // Act
            var chunks = new List<byte[]>();
            await foreach (var chunk in _service.StreamSpeechAsync(text))
            {
                chunks.Add(chunk);
            }

            // Assert
            Assert.NotEmpty(chunks);
            var totalData = chunks.SelectMany(c => c).ToArray();
            Assert.Equal(audioData, totalData);
        }

        [Fact]
        public async Task StreamSpeechAsync_WhenRateLimited_UsesFallbackService()
        {
            // Arrange
            var text = "Streaming rate limit test";
            var fallbackChunks = new List<byte[]> { Encoding.UTF8.GetBytes("fallback-chunk") };

            _mockRateLimitService.Setup(x => x.CanMakeRequestAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            _mockFallbackService.Setup(x => x.StreamSpeechAsync(text, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(fallbackChunks.ToAsyncEnumerable());

            // Act
            var chunks = new List<byte[]>();
            await foreach (var chunk in _service.StreamSpeechAsync(text))
            {
                chunks.Add(chunk);
            }

            // Assert
            Assert.Single(chunks);
            Assert.Equal(fallbackChunks[0], chunks[0]);
        }

        [Fact]
        public async Task RecognizeSpeechAsync_AlwaysUsesFallbackService()
        {
            // Arrange
            var audioData = Encoding.UTF8.GetBytes("audio-data");
            var expectedText = "recognized text";

            _mockFallbackService.Setup(x => x.RecognizeSpeechAsync(audioData, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedText);

            // Act
            var result = await _service.RecognizeSpeechAsync(audioData);

            // Assert
            Assert.Equal(expectedText, result);
            _mockFallbackService.Verify(x => x.RecognizeSpeechAsync(audioData, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Theory]
        [InlineData("System error detected", "concerned")]
        [InlineData("Task completed successfully", "excited")]
        [InlineData("All systems normal", "calm")]
        [InlineData("Regular message", "default")]
        public async Task GenerateSpeechAsync_AdjustsVoiceSettingsBasedOnContent(string text, string expectedEmotion)
        {
            // Arrange
            _mockCacheService.Setup(x => x.GetCachedAudioAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
                .ReturnsAsync((byte[]?)null);

            _mockRateLimitService.Setup(x => x.CanMakeRequestAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            var capturedRequest = "";
            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((request, token) =>
                {
                    if (request.Content != null)
                    {
                        capturedRequest = request.Content.ReadAsStringAsync().Result;
                    }
                })
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(Encoding.UTF8.GetBytes("audio-data"))
                });

            // Act
            await _service.GenerateSpeechAsync(text);

            // Assert
            Assert.NotEmpty(capturedRequest);
            var requestData = JsonSerializer.Deserialize<JsonElement>(capturedRequest);
            Assert.True(requestData.TryGetProperty("voice_settings", out var voiceSettings));
            
            // Verify that voice settings were included (specific values depend on emotion)
            Assert.True(voiceSettings.TryGetProperty("stability", out var stability));
            Assert.True(stability.GetSingle() > 0);
        }

        [Fact]
        public async Task GenerateSpeechAsync_EnhancesTextForJarvis()
        {
            // Arrange
            var originalText = "System status is normal, Sir";

            _mockCacheService.Setup(x => x.GetCachedAudioAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
                .ReturnsAsync((byte[]?)null);

            _mockRateLimitService.Setup(x => x.CanMakeRequestAsync(It.IsAny<string>()))
                .ReturnsAsync(true);

            var capturedRequest = "";
            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((request, token) =>
                {
                    if (request.Content != null)
                    {
                        capturedRequest = request.Content.ReadAsStringAsync().Result;
                    }
                })
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(Encoding.UTF8.GetBytes("audio-data"))
                });

            // Act
            await _service.GenerateSpeechAsync(originalText);

            // Assert
            Assert.NotEmpty(capturedRequest);
            
            // Parse the JSON to get the actual text value
            var requestData = JsonSerializer.Deserialize<JsonElement>(capturedRequest);
            Assert.True(requestData.TryGetProperty("text", out var textElement));
            
            var enhancedText = textElement.GetString();
            Assert.NotNull(enhancedText);
            
            // Verify that "Sir" was enhanced with the break tag
            Assert.Contains("Sir<break time=\"500ms\"/>", enhancedText);
            
            // Also verify that "status" was enhanced with emphasis
            Assert.Contains("<emphasis level=\"moderate\">status</emphasis>", enhancedText);
        }

        [Fact]
        public async Task GetQuotaInfoAsync_WithValidResponse_ReturnsQuotaInfo()
        {
            // Arrange
            var quotaResponse = new ElevenLabsQuotaResponse
            {
                CharacterCount = 1000,
                CharacterLimit = 10000,
                CharactersRemaining = 9000,
                NextResetUnix = DateTimeOffset.UtcNow.AddDays(1).ToUnixTimeSeconds()
            };

            var jsonResponse = JsonSerializer.Serialize(quotaResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            SetupHttpResponse(HttpStatusCode.OK, Encoding.UTF8.GetBytes(jsonResponse), "/v1/user/subscription");

            // Act
            var result = await _service.GetQuotaInfoAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(quotaResponse.CharacterCount, result.CharacterCount);
            Assert.Equal(quotaResponse.CharacterLimit, result.CharacterLimit);
            Assert.Equal(quotaResponse.CharactersRemaining, result.CharactersRemaining);
        }

        [Fact]
        public async Task GetAvailableVoicesAsync_WithValidResponse_ReturnsVoices()
        {
            // Arrange
            var voicesResponse = new ElevenLabsVoicesResponse
            {
                Voices = new List<ElevenLabsVoice>
                {
                    new() { VoiceId = "voice1", Name = "British Male", Category = "professional" },
                    new() { VoiceId = "voice2", Name = "American Female", Category = "generated" }
                }
            };

            var jsonResponse = JsonSerializer.Serialize(voicesResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            SetupHttpResponse(HttpStatusCode.OK, Encoding.UTF8.GetBytes(jsonResponse), "/v1/voices");

            // Act
            var result = await _service.GetAvailableVoicesAsync();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal("voice1", result[0].VoiceId);
            Assert.Equal("British Male", result[0].Name);
        }

        [Fact]
        public void Constructor_WithInvalidConfig_ThrowsArgumentException()
        {
            // Arrange
            var invalidConfig = new ElevenLabsConfig { ApiKey = null }; // Invalid config

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new ElevenLabsVoiceService(
                _httpClient,
                invalidConfig,
                _mockLogger.Object,
                _mockCacheService.Object,
                _mockRateLimitService.Object,
                _mockFallbackService.Object));
        }

        [Fact]
        public async Task GenerateSpeechAsync_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            _service.Dispose();

            // Act & Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(() => _service.GenerateSpeechAsync("test"));
        }

        private void SetupHttpResponse(HttpStatusCode statusCode, byte[] content, string? endpoint = null)
        {
            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    endpoint != null ? 
                        ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains(endpoint)) :
                        ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(statusCode)
                {
                    Content = new ByteArrayContent(content)
                });
        }

        private void SetupStreamingHttpResponse(HttpStatusCode statusCode, byte[] content)
        {
            var stream = new MemoryStream(content);
            
            _mockHttpHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("/stream")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(statusCode)
                {
                    Content = new StreamContent(stream)
                });
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _service?.Dispose();
            _httpClient?.Dispose();
        }
    }
}
