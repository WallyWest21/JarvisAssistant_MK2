using Xunit;
using JarvisAssistant.Core.Models;
using System.ComponentModel;

namespace JarvisAssistant.UnitTests.Core.Models
{
    public class ChatMessageCoreTests
    {
        [Fact]
        public void Constructor_ShouldSetDefaultValues()
        {
            // Act
            var message = new ChatMessageCore();

            // Assert
            Assert.NotNull(message.Id);
            Assert.Empty(message.Content);
            Assert.False(message.IsFromUser);
            Assert.True(message.IsFromJarvis);
            Assert.Equal(MessageType.Text, message.Type);
            Assert.False(message.IsStreaming);
            Assert.True((DateTime.Now - message.Timestamp).TotalSeconds < 1);
        }

        [Fact]
        public void Constructor_WithParameters_ShouldSetValues()
        {
            // Arrange
            const string content = "Test message";
            const bool isFromUser = true;
            const MessageType type = MessageType.Code;

            // Act
            var message = new ChatMessageCore(content, isFromUser, type);

            // Assert
            Assert.Equal(content, message.Content);
            Assert.Equal(isFromUser, message.IsFromUser);
            Assert.Equal(!isFromUser, message.IsFromJarvis);
            Assert.Equal(type, message.Type);
        }

        [Fact]
        public void IsFromUser_WhenSet_ShouldUpdateRelatedProperties()
        {
            // Arrange
            var message = new ChatMessageCore();
            var propertyChangedEvents = new List<string>();
            message.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName!);

            // Act
            message.IsFromUser = true;

            // Assert
            Assert.True(message.IsFromUser);
            Assert.False(message.IsFromJarvis);
            Assert.Contains(nameof(ChatMessageCore.IsFromUser), propertyChangedEvents);
            Assert.Contains(nameof(ChatMessageCore.IsFromJarvis), propertyChangedEvents);
        }

        [Fact]
        public void Type_WhenSet_ShouldUpdateRelatedProperties()
        {
            // Arrange
            var message = new ChatMessageCore();
            var propertyChangedEvents = new List<string>();
            message.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName!);

            // Act
            message.Type = MessageType.Error;

            // Assert
            Assert.Equal(MessageType.Error, message.Type);
            Assert.True(message.IsError);
            Assert.Contains(nameof(ChatMessageCore.Type), propertyChangedEvents);
            Assert.Contains(nameof(ChatMessageCore.IsCodeBlock), propertyChangedEvents);
            Assert.Contains(nameof(ChatMessageCore.IsError), propertyChangedEvents);
            Assert.Contains(nameof(ChatMessageCore.IsVoiceCommand), propertyChangedEvents);
        }

        [Theory]
        [InlineData(MessageType.Code, true, false, false)]
        [InlineData(MessageType.Error, false, true, false)]
        [InlineData(MessageType.Voice, false, false, true)]
        [InlineData(MessageType.Text, false, false, false)]
        [InlineData(MessageType.System, false, false, false)]
        public void TypeProperties_ShouldReturnCorrectValues(MessageType type, bool isCode, bool isError, bool isVoice)
        {
            // Arrange
            var message = new ChatMessageCore { Type = type };

            // Assert
            Assert.Equal(isCode, message.IsCodeBlock);
            Assert.Equal(isError, message.IsError);
            Assert.Equal(isVoice, message.IsVoiceCommand);
        }

        [Fact]
        public void FormattedTime_ShouldReturnCorrectFormat()
        {
            // Arrange
            var testTime = new DateTime(2023, 12, 25, 14, 30, 45);
            var message = new ChatMessageCore { Timestamp = testTime };

            // Assert
            Assert.Equal("14:30", message.FormattedTime);
        }

        [Fact]
        public void PropertyChanged_ShouldBeRaisedForAllProperties()
        {
            // Arrange
            var message = new ChatMessageCore();
            var propertyChangedEvents = new List<string>();
            message.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName!);

            // Act
            message.Id = "new-id";
            message.Content = "new content";
            message.IsFromUser = true;
            message.Timestamp = DateTime.Now.AddMinutes(-1);
            message.Type = MessageType.Error;
            message.IsStreaming = true;

            // Assert
            Assert.Contains(nameof(ChatMessageCore.Id), propertyChangedEvents);
            Assert.Contains(nameof(ChatMessageCore.Content), propertyChangedEvents);
            Assert.Contains(nameof(ChatMessageCore.IsFromUser), propertyChangedEvents);
            Assert.Contains(nameof(ChatMessageCore.Timestamp), propertyChangedEvents);
            Assert.Contains(nameof(ChatMessageCore.Type), propertyChangedEvents);
            Assert.Contains(nameof(ChatMessageCore.IsStreaming), propertyChangedEvents);
        }

        [Fact]
        public void Timestamp_WhenSet_ShouldUpdateFormattedTime()
        {
            // Arrange
            var message = new ChatMessageCore();
            var propertyChangedEvents = new List<string>();
            message.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName!);
            var newTime = new DateTime(2023, 12, 25, 15, 45, 30);

            // Act
            message.Timestamp = newTime;

            // Assert
            Assert.Equal(newTime, message.Timestamp);
            Assert.Equal("15:45", message.FormattedTime);
            Assert.Contains(nameof(ChatMessageCore.Timestamp), propertyChangedEvents);
            Assert.Contains(nameof(ChatMessageCore.FormattedTime), propertyChangedEvents);
        }
    }
}