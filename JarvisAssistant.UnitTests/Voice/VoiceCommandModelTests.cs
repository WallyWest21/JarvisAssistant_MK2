using JarvisAssistant.Core.Models;
using Xunit;

namespace JarvisAssistant.UnitTests.Voice
{
    /// <summary>
    /// Unit tests for voice command models and related classes.
    /// </summary>
    public class VoiceCommandModelTests
    {
        [Fact]
        public void VoiceCommand_DefaultConstructor_SetsDefaultValues()
        {
            // Act
            var command = new VoiceCommand();

            // Assert
            Assert.Equal(string.Empty, command.Text);
            Assert.Equal(VoiceCommandSource.WakeWord, command.Source);
            Assert.True(DateTime.UtcNow.Subtract(command.Timestamp).TotalSeconds < 1);
            Assert.Equal(0f, command.RecognitionConfidence);
            Assert.Equal(VoiceCommandType.Unknown, command.CommandType);
            Assert.Equal(0f, command.ClassificationConfidence);
            Assert.NotNull(command.Parameters);
            Assert.Empty(command.Parameters);
            Assert.Null(command.AudioData);
            Assert.Equal(0, command.AudioDurationMs);
            Assert.Null(command.DetectedLanguage);
            Assert.False(command.RequiresConfirmation);
            Assert.Null(command.ProcessingError);
        }

        [Fact]
        public void VoiceCommand_IsValid_WithValidData_ReturnsTrue()
        {
            // Arrange
            var command = new VoiceCommand
            {
                Text = "test command",
                RecognitionConfidence = 0.8f,
                CommandType = VoiceCommandType.Status
            };

            // Act & Assert
            Assert.True(command.IsValid);
        }

        [Theory]
        [InlineData("", 0.8f, VoiceCommandType.Status)] // Empty text
        [InlineData("test", 0.4f, VoiceCommandType.Status)] // Low confidence
        [InlineData("test", 0.8f, VoiceCommandType.Unknown)] // Unknown type
        [InlineData(null, 0.8f, VoiceCommandType.Status)] // Null text
        public void VoiceCommand_IsValid_WithInvalidData_ReturnsFalse(string? text, float confidence, VoiceCommandType type)
        {
            // Arrange
            var command = new VoiceCommand
            {
                Text = text ?? string.Empty,
                RecognitionConfidence = confidence,
                CommandType = type
            };

            // Act & Assert
            Assert.False(command.IsValid);
        }

        [Fact]
        public void VoiceCommand_ToLogString_ReturnsFormattedString()
        {
            // Arrange
            var timestamp = new DateTime(2024, 1, 15, 10, 30, 45, DateTimeKind.Utc);
            var command = new VoiceCommand
            {
                Text = "test command",
                Source = VoiceCommandSource.WakeWord,
                CommandType = VoiceCommandType.Status,
                RecognitionConfidence = 0.85f,
                ClassificationConfidence = 0.92f,
                Timestamp = timestamp
            };

            // Act
            var logString = command.ToLogString();

            // Assert
            Assert.Contains("[2024-01-15 10:30:45]", logString);
            Assert.Contains("WakeWord", logString);
            Assert.Contains("Status", logString);
            Assert.Contains("test command", logString);
            Assert.Contains("85.0%", logString);
            Assert.Contains("92.0%", logString);
        }

        [Fact]
        public void VoiceCommand_WithClassification_CreatesNewInstanceWithUpdatedInfo()
        {
            // Arrange
            var originalCommand = new VoiceCommand
            {
                Text = "test command",
                Source = VoiceCommandSource.Manual,
                RecognitionConfidence = 0.8f
            };

            var parameters = new Dictionary<string, object> { ["param1"] = "value1" };

            // Act
            var updatedCommand = originalCommand.WithClassification(
                VoiceCommandType.Status, 
                0.9f, 
                parameters);

            // Assert
            Assert.NotSame(originalCommand, updatedCommand);
            Assert.Equal(originalCommand.Text, updatedCommand.Text);
            Assert.Equal(originalCommand.Source, updatedCommand.Source);
            Assert.Equal(originalCommand.RecognitionConfidence, updatedCommand.RecognitionConfidence);
            Assert.Equal(VoiceCommandType.Status, updatedCommand.CommandType);
            Assert.Equal(0.9f, updatedCommand.ClassificationConfidence);
            Assert.Equal(parameters, updatedCommand.Parameters);
        }

        [Fact]
        public void VoiceCommand_WithClassification_WithNullParameters_UsesEmptyDictionary()
        {
            // Arrange
            var originalCommand = new VoiceCommand
            {
                Text = "test command",
                RecognitionConfidence = 0.8f
            };

            // Act
            var updatedCommand = originalCommand.WithClassification(VoiceCommandType.Status, 0.9f, null);

            // Assert
            Assert.NotNull(updatedCommand.Parameters);
            Assert.Empty(updatedCommand.Parameters);
        }

        [Fact]
        public void VoiceCommandResult_CreateSuccess_CreatesSuccessfulResult()
        {
            // Arrange
            var response = "Operation completed successfully";
            var data = new { result = "success" };

            // Act
            var result = VoiceCommandResult.CreateSuccess(response, data, true);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(response, result.Response);
            Assert.Equal(data, result.Data);
            Assert.True(result.ShouldSpeak);
            Assert.Null(result.ErrorMessage);
            Assert.NotNull(result.FollowUpActions);
            Assert.Empty(result.FollowUpActions);
        }

        [Fact]
        public void VoiceCommandResult_CreateError_CreatesErrorResult()
        {
            // Arrange
            var errorMessage = "Something went wrong";

            // Act
            var result = VoiceCommandResult.CreateError(errorMessage, false);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(errorMessage, result.ErrorMessage);
            Assert.Equal(errorMessage, result.Response);
            Assert.False(result.ShouldSpeak);
            Assert.Null(result.Data);
        }

        [Fact]
        public void VoiceCommandResult_DefaultConstructor_SetsDefaultValues()
        {
            // Act
            var result = new VoiceCommandResult();

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.Response);
            Assert.Null(result.Data);
            Assert.Null(result.ErrorMessage);
            Assert.True(result.ShouldSpeak);
            Assert.NotNull(result.FollowUpActions);
            Assert.Empty(result.FollowUpActions);
            Assert.Equal(0, result.ProcessingTimeMs);
        }

        [Fact]
        public void VoiceCommandResult_CanSetProperties()
        {
            // Arrange
            var followUpActions = new List<string> { "action1", "action2" };

            // Act
            var result = new VoiceCommandResult
            {
                Success = true,
                Response = "Test response",
                Data = 42,
                ErrorMessage = "Test error",
                ShouldSpeak = false,
                FollowUpActions = followUpActions,
                ProcessingTimeMs = 150
            };

            // Assert
            Assert.True(result.Success);
            Assert.Equal("Test response", result.Response);
            Assert.Equal(42, result.Data);
            Assert.Equal("Test error", result.ErrorMessage);
            Assert.False(result.ShouldSpeak);
            Assert.Equal(followUpActions, result.FollowUpActions);
            Assert.Equal(150, result.ProcessingTimeMs);
        }

        [Theory]
        [InlineData(VoiceCommandSource.WakeWord)]
        [InlineData(VoiceCommandSource.PushToTalk)]
        [InlineData(VoiceCommandSource.AlwaysOn)]
        [InlineData(VoiceCommandSource.Manual)]
        [InlineData(VoiceCommandSource.RemoteControl)]
        public void VoiceCommandSource_AllValues_AreValid(VoiceCommandSource source)
        {
            // Act
            var command = new VoiceCommand { Source = source };

            // Assert
            Assert.Equal(source, command.Source);
        }

        [Theory]
        [InlineData(VoiceCommandType.Unknown)]
        [InlineData(VoiceCommandType.Status)]
        [InlineData(VoiceCommandType.GenerateCode)]
        [InlineData(VoiceCommandType.Analyze)]
        [InlineData(VoiceCommandType.Navigate)]
        [InlineData(VoiceCommandType.Search)]
        [InlineData(VoiceCommandType.Settings)]
        [InlineData(VoiceCommandType.Help)]
        [InlineData(VoiceCommandType.Stop)]
        [InlineData(VoiceCommandType.Exit)]
        [InlineData(VoiceCommandType.Repeat)]
        [InlineData(VoiceCommandType.Chat)]
        public void VoiceCommandType_AllValues_AreValid(VoiceCommandType type)
        {
            // Act
            var command = new VoiceCommand { CommandType = type };

            // Assert
            Assert.Equal(type, command.CommandType);
        }

        [Fact]
        public void VoiceCommand_Parameters_CanStoreVariousTypes()
        {
            // Arrange
            var command = new VoiceCommand();

            // Act
            command.Parameters["string"] = "test";
            command.Parameters["int"] = 42;
            command.Parameters["bool"] = true;
            command.Parameters["double"] = 3.14;
            command.Parameters["object"] = new { name = "test" };

            // Assert
            Assert.Equal("test", command.Parameters["string"]);
            Assert.Equal(42, command.Parameters["int"]);
            Assert.Equal(true, command.Parameters["bool"]);
            Assert.Equal(3.14, command.Parameters["double"]);
            Assert.NotNull(command.Parameters["object"]);
        }

        [Fact]
        public void VoiceCommand_AudioData_CanBeSetAndRetrieved()
        {
            // Arrange
            var audioData = new byte[] { 1, 2, 3, 4, 5 };
            var command = new VoiceCommand();

            // Act
            command.AudioData = audioData;

            // Assert
            Assert.Equal(audioData, command.AudioData);
            Assert.Equal(5, command.AudioData.Length);
        }

        [Fact]
        public void VoiceCommand_DetectedLanguage_CanBeSetAndRetrieved()
        {
            // Arrange
            var language = "en-US";
            var command = new VoiceCommand();

            // Act
            command.DetectedLanguage = language;

            // Assert
            Assert.Equal(language, command.DetectedLanguage);
        }

        [Fact]
        public void VoiceCommand_RequiresConfirmation_DefaultsFalse()
        {
            // Act
            var command = new VoiceCommand();

            // Assert
            Assert.False(command.RequiresConfirmation);
        }

        [Fact]
        public void VoiceCommand_ProcessingError_CanBeSetAndRetrieved()
        {
            // Arrange
            var error = "Processing failed";
            var command = new VoiceCommand();

            // Act
            command.ProcessingError = error;

            // Assert
            Assert.Equal(error, command.ProcessingError);
        }
    }
}
