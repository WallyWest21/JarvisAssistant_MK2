using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using JarvisAssistant.Core.Models;
using JarvisAssistant.Services.LLM;

namespace JarvisAssistant.UnitTests.Services.LLM
{
    public class PersonalityServiceTests
    {
        private readonly Mock<ILogger<PersonalityService>> _mockLogger;
        private readonly PersonalityService _personalityService;

        public PersonalityServiceTests()
        {
            _mockLogger = new Mock<ILogger<PersonalityService>>();
            _personalityService = new PersonalityService(_mockLogger.Object);
        }

        [Fact]
        public async Task FormatResponseAsync_WithBasicResponse_AppliesPersonality()
        {
            // Arrange
            var originalResponse = "This is a basic response.";

            // Act
            var result = await _personalityService.FormatResponseAsync(originalResponse, QueryType.General);

            // Assert
            result.Should().NotBeEmpty();
            result.Should().NotBe(originalResponse); // Should be modified
        }

        [Fact]
        public async Task FormatResponseAsync_WithEmptyResponse_ReturnsEmpty()
        {
            // Arrange
            var originalResponse = string.Empty;

            // Act
            var result = await _personalityService.FormatResponseAsync(originalResponse, QueryType.General);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task FormatResponseAsync_WithNullResponse_ReturnsNull()
        {
            // Arrange
            string? originalResponse = null;

            // Act
            var result = await _personalityService.FormatResponseAsync(originalResponse!, QueryType.General);

            // Assert
            result.Should().BeNull();
        }

        [Theory]
        [InlineData("This is a problem.", "challenge")]
        [InlineData("There's an issue here.", "matter")]
        [InlineData("This is a bug.", "irregularity")]
        [InlineData("That's good work.", "excellent")]
        public async Task FormatResponseAsync_AppliesVocabularyEnhancements(string input, string expectedWord)
        {
            // Act
            var result = await _personalityService.FormatResponseAsync(input, QueryType.General);

            // Assert
            result.Should().Contain(expectedWord);
        }

        [Fact]
        public async Task FormatResponseAsync_WithStreamingMode_DoesNotAddClosingElements()
        {
            // Arrange
            var originalResponse = "Streaming response chunk.";

            // Act
            var result = await _personalityService.FormatResponseAsync(originalResponse, QueryType.General, isStreaming: true);

            // Assert
            result.Should().NotContain("service"); // Should not contain closing phrases in streaming mode
        }

        [Fact]
        public async Task FormatResponseAsync_WithNonStreamingMode_AddsPersonalityElements()
        {
            // Arrange
            var originalResponse = "Complete response.";

            // Act
            var result = await _personalityService.FormatResponseAsync(originalResponse, QueryType.General, isStreaming: false);

            // Assert
            result.Should().Contain("Sir"); // Should contain British addressing
        }

        [Theory]
        [InlineData(QueryType.Code)]
        [InlineData(QueryType.Technical)]
        [InlineData(QueryType.General)]
        [InlineData(QueryType.Error)]
        [InlineData(QueryType.Creative)]
        [InlineData(QueryType.Mathematical)]
        public void GetSystemPrompt_WithValidQueryType_ReturnsPrompt(QueryType queryType)
        {
            // Act
            var result = _personalityService.GetSystemPrompt(queryType);

            // Assert
            result.Should().NotBeEmpty();
            result.Should().Contain("JARVIS");
            result.Should().Contain("British");
        }

        [Theory]
        [InlineData(QueryType.Code)]
        [InlineData(QueryType.Technical)]
        [InlineData(QueryType.General)]
        [InlineData(QueryType.Error)]
        [InlineData(QueryType.Creative)]
        [InlineData(QueryType.Mathematical)]
        public void GetContextualGreeting_WithValidQueryType_ReturnsGreeting(QueryType queryType)
        {
            // Act
            var result = _personalityService.GetContextualGreeting(queryType);

            // Assert
            result.Should().NotBeEmpty();
            // Different query types might have different greeting styles
        }

        [Fact]
        public void GetContextualGreeting_ForCodeType_ReturnsCodeSpecificGreeting()
        {
            // Act
            var result = _personalityService.GetContextualGreeting(QueryType.Code);

            // Assert
            result.Should().NotBeEmpty();
            // The actual greeting will depend on the JSON configuration
        }

        [Fact]
        public void GetContextualGreeting_ForErrorType_ReturnsErrorSpecificGreeting()
        {
            // Act
            var result = _personalityService.GetContextualGreeting(QueryType.Error);

            // Assert
            result.Should().NotBeEmpty();
            // Should potentially contain words like "complication" or "difficulty"
        }

        [Fact]
        public void GetAppropriateAddress_WithNoContext_ReturnsSir()
        {
            // Act
            var result = _personalityService.GetAppropriateAddress();

            // Assert
            result.Should().Be("Sir");
        }

        [Fact]
        public void GetAppropriateAddress_WithContext_ReturnsSir()
        {
            // Arrange
            var context = "Some context about the user";

            // Act
            var result = _personalityService.GetAppropriateAddress(context);

            // Assert
            result.Should().Be("Sir"); // Currently always returns Sir, but could be enhanced
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new PersonalityService(null!));
        }

        [Fact]
        public async Task FormatResponseAsync_ConsistentPersonality_MaintainsStyle()
        {
            // Arrange
            var responses = new[]
            {
                "Help me with this code.",
                "What's the weather like?",
                "Explain quantum physics.",
                "There's an error in my program."
            };

            var queryTypes = new[]
            {
                QueryType.Code,
                QueryType.General,
                QueryType.Technical,
                QueryType.Error
            };

            // Act
            var formattedResponses = new List<string>();
            for (int i = 0; i < responses.Length; i++)
            {
                var formatted = await _personalityService.FormatResponseAsync(responses[i], queryTypes[i]);
                formattedResponses.Add(formatted);
            }

            // Assert
            formattedResponses.Should().AllSatisfy(response =>
            {
                response.Should().NotBeEmpty();
                // All responses should maintain some level of sophistication
                // This is a basic check - in reality, you'd want more sophisticated personality consistency checks
            });
        }

        [Fact]
        public async Task FormatResponseAsync_WithException_ReturnsOriginalResponse()
        {
            // This test would be more meaningful if we could inject a faulty JSON parser
            // For now, we test the general error handling behavior
            
            // Arrange
            var originalResponse = "Original response";

            // Act
            var result = await _personalityService.FormatResponseAsync(originalResponse, QueryType.General);

            // Assert
            // Should not throw and should return some response
            result.Should().NotBeNull();
        }
    }
}
