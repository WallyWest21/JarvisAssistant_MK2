using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models;
using JarvisAssistant.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Contrib.HttpClient;
using NUnit.Framework;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JarvisAssistant.ElevenLabs.IntegrationTests
{
    [TestFixture]
    public class ElevenLabsIntegrationTests
    {
        private ServiceProvider _serviceProvider = null!;
        private Mock<HttpMessageHandler> _handlerMock = null!;
        private ElevenLabsConfig _config = null!;

        [SetUp]
        public void Setup()
        {
            var services = new ServiceCollection();
            _handlerMock = new Mock<HttpMessageHandler>();

            // Configure test configuration
            _config = new ElevenLabsConfig
            {
                ApiKey = "test-api-key",
                VoiceId = "EXAVITQu4vr4xnSDxMaL", // British accent voice
                ModelId = "eleven_multilingual_v2",
                BaseUrl = "https://api.elevenlabs.io",
                EnableCaching = true,
                EnableRateLimiting = true,
                EnableFallback = true,
                EnableStreaming = true
            };

            var httpClient = _handlerMock.CreateClient();
            httpClient.BaseAddress = new Uri(_config.BaseUrl);

            // Register services
            services.AddSingleton(httpClient);
            services.AddSingleton(_config);
            services.AddLogging();
            
            // Use existing service implementations
            services.AddSingleton<IAudioCacheService, AudioCacheService>();
            services.AddSingleton<IRateLimitService, RateLimitService>();
            services.AddSingleton<IVoiceService, StubVoiceService>(); // Fallback service
            services.AddTransient<ElevenLabsVoiceService>();

            _serviceProvider = services.BuildServiceProvider();
        }

        [TearDown]
        public void TearDown()
        {
            _serviceProvider?.Dispose();
        }

        [Test]
        public async Task GenerateSpeechAsync_WithValidApiKey_ReturnsAudioData()
        {
            // Arrange
            var service = _serviceProvider.GetRequiredService<ElevenLabsVoiceService>();
            var expectedAudio = new byte[] { 0x49, 0x44, 0x33, 0x04 }; // Mock MP3 header
            var testText = "Hello Sir, Jarvis is now online and ready to assist you.";

            _handlerMock.SetupRequest(HttpMethod.Post, $"{_config.BaseUrl}/v1/text-to-speech/{_config.VoiceId}")
                .ReturnsResponse(HttpStatusCode.OK, new ByteArrayContent(expectedAudio));

            // Act
            var result = await service.GenerateSpeechAsync(testText);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.EqualTo(expectedAudio));
        }

        [Test]
        public async Task StreamSpeechAsync_WithValidApiKey_StreamsAudioChunks()
        {
            // Arrange
            var service = _serviceProvider.GetRequiredService<ElevenLabsVoiceService>();
            var testText = "Good morning Sir. The system diagnostics are complete.";
            var mockAudioData = new byte[] { 0x49, 0x44, 0x33, 0x04, 0x00, 0x00, 0x00, 0x00 };
            
            _handlerMock.SetupRequest(HttpMethod.Post, $"{_config.BaseUrl}/v1/text-to-speech/{_config.VoiceId}/stream")
                .ReturnsResponse(HttpStatusCode.OK, new ByteArrayContent(mockAudioData));

            // Act
            var chunks = new System.Collections.Generic.List<byte[]>();
            await foreach (var chunk in service.StreamSpeechAsync(testText))
            {
                chunks.Add(chunk);
            }

            // Assert
            Assert.That(chunks, Is.Not.Empty);
            var totalBytes = chunks.SelectMany(c => c).ToArray();
            Assert.That(totalBytes.Length, Is.GreaterThan(0));
        }

        [Test]
        public async Task GenerateSpeechAsync_WithApiError_UsesFallbackService()
        {
            // Arrange
            var service = _serviceProvider.GetRequiredService<ElevenLabsVoiceService>();
            var testText = "Testing fallback mechanism.";

            _handlerMock.SetupRequest(HttpMethod.Post, $"{_config.BaseUrl}/v1/text-to-speech/{_config.VoiceId}")
                .ReturnsResponse(HttpStatusCode.InternalServerError);

            // Act
            var result = await service.GenerateSpeechAsync(testText);

            // Assert - StubVoiceService returns empty array
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.EqualTo(0));
        }

        [Test]
        public async Task GetQuotaInfoAsync_WithValidApiKey_ReturnsQuotaInformation()
        {
            // Arrange
            var service = _serviceProvider.GetRequiredService<ElevenLabsVoiceService>();
            var mockQuotaResponse = new ElevenLabsQuotaResponse
            {
                CharacterCount = 5000,
                CharacterLimit = 10000,
                CanExtendCharacterLimit = true,
                AllowedToExtendCharacterLimit = false,
                NextCharacterCountResetUnix = DateTimeOffset.UtcNow.AddDays(30).ToUnixTimeSeconds()
            };

            var jsonResponse = JsonSerializer.Serialize(mockQuotaResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            _handlerMock.SetupRequest(HttpMethod.Get, $"{_config.BaseUrl}/v1/user/subscription")
                .ReturnsResponse(HttpStatusCode.OK, new StringContent(jsonResponse, Encoding.UTF8, "application/json"));

            // Act
            var result = await service.GetQuotaInfoAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.CharacterCount, Is.EqualTo(5000));
            Assert.That(result.CharacterLimit, Is.EqualTo(10000));
            Assert.That(result.CharactersRemaining, Is.EqualTo(5000));
            Assert.That(result.QuotaUsedPercentage, Is.EqualTo(50.0).Within(0.1));
        }

        [Test]
        public async Task IsHealthyAsync_WithValidConfiguration_ReturnsTrue()
        {
            // Arrange
            var service = _serviceProvider.GetRequiredService<ElevenLabsVoiceService>();

            _handlerMock.SetupRequest(HttpMethod.Get, $"{_config.BaseUrl}/v1/user")
                .ReturnsResponse(HttpStatusCode.OK, new StringContent("{\"user_id\":\"test\"}"));

            // Act
            var result = await service.IsHealthyAsync();

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task IsHealthyAsync_WithInvalidApiKey_ReturnsFalse()
        {
            // Arrange
            var service = _serviceProvider.GetRequiredService<ElevenLabsVoiceService>();

            _handlerMock.SetupRequest(HttpMethod.Get, $"{_config.BaseUrl}/v1/user")
                .ReturnsResponse(HttpStatusCode.Unauthorized);

            // Act
            var result = await service.IsHealthyAsync();

            // Assert
            Assert.That(result, Is.False);
        }
    }
}
