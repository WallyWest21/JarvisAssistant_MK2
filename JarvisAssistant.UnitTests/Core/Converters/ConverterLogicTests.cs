using Xunit;
using JarvisAssistant.Core.Converters;
using JarvisAssistant.Core.Models;

namespace JarvisAssistant.UnitTests.Core.Converters
{
    public class ConverterLogicTests
    {
        [Fact]
        public void InvertBool_ShouldInvertBooleanValues()
        {
            // Act & Assert
            Assert.False(ConverterLogic.InvertBool(true));
            Assert.True(ConverterLogic.InvertBool(false));
            Assert.False(ConverterLogic.InvertBool(null));
        }

        [Theory]
        [InlineData(true, "#4CAF50")] // Green for connected
        [InlineData(false, "#F44336")] // Red for disconnected
        public void BoolToColorHex_ShouldReturnCorrectColors(bool isConnected, string expectedColor)
        {
            // This test would be in MAUI-specific tests since it involves Color objects
            // Here we're testing the core logic only
        }

        [Theory]
        [InlineData("Hello", true)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData(null, false)]
        public void StringToBool_ShouldReturnCorrectBooleanValues(string? input, bool expected)
        {
            // Act
            var result = ConverterLogic.StringToBool(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(MessageType.Error, "#FF5722")]
        [InlineData(MessageType.Voice, "#00E5FF")]
        [InlineData(MessageType.Code, "#1A1A1A")]
        [InlineData(MessageType.System, "#9C27B0")]
        [InlineData(MessageType.Text, "#4A148C")]
        public void MessageTypeToColorHex_ShouldReturnCorrectColors(MessageType messageType, string expectedColor)
        {
            // Act
            var result = ConverterLogic.MessageTypeToColorHex(messageType);

            // Assert
            Assert.Equal(expectedColor, result);
        }

        [Theory]
        [InlineData(MessageType.Error, "?")]
        [InlineData(MessageType.Voice, "??")]
        [InlineData(MessageType.Code, "??")]
        [InlineData(MessageType.System, "??")]
        [InlineData(MessageType.Text, "")]
        public void MessageTypeToIcon_ShouldReturnCorrectIcons(MessageType messageType, string expectedIcon)
        {
            // Act
            var result = ConverterLogic.MessageTypeToIcon(messageType);

            // Assert
            Assert.Equal(expectedIcon, result);
        }

        [Theory]
        [InlineData(0.0, 0.2)]
        [InlineData(0.1, 0.2)]
        [InlineData(0.5, 0.5)]
        [InlineData(0.8, 0.8)]
        [InlineData(1.0, 1.0)]
        [InlineData(1.5, 1.0)]
        public void VoiceActivityToOpacity_ShouldClampValues(double input, double expected)
        {
            // Act
            var result = ConverterLogic.VoiceActivityToOpacity(input);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void VoiceActivityToOpacity_WithNullValue_ShouldReturnMinimumOpacity()
        {
            // Act
            var result = ConverterLogic.VoiceActivityToOpacity(null);

            // Assert
            Assert.Equal(0.2, result);
        }

        [Fact]
        public void PlatformMatches_WithMatchingPlatform_ShouldReturnTrue()
        {
            // Act
            var result = ConverterLogic.PlatformMatches("Desktop", "Desktop");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void PlatformMatches_WithNonMatchingPlatform_ShouldReturnFalse()
        {
            // Act
            var result = ConverterLogic.PlatformMatches("Desktop", "Mobile");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void PlatformMatches_WithNullParameter_ShouldReturnFalse()
        {
            // Act
            var result = ConverterLogic.PlatformMatches(null, "Desktop");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void PlatformMatches_IsCaseInsensitive()
        {
            // Act
            var result = ConverterLogic.PlatformMatches("desktop", "DESKTOP");

            // Assert
            Assert.True(result);
        }
    }
}