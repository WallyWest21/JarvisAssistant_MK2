using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models;
using JarvisAssistant.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JarvisAssistant.UnitTests.Voice
{
    /// <summary>
    /// Unit tests for the VoiceCommandProcessor class.
    /// </summary>
    public class VoiceCommandProcessorTests
    {
        private readonly Mock<ILogger<VoiceCommandProcessor>> _mockLogger;
        private readonly VoiceCommandProcessor _processor;

        public VoiceCommandProcessorTests()
        {
            _mockLogger = new Mock<ILogger<VoiceCommandProcessor>>();
            _processor = new VoiceCommandProcessor(_mockLogger.Object);
        }

        [Fact]
        public void Constructor_WithValidLogger_InitializesCorrectly()
        {
            // Assert
            Assert.NotNull(_processor);
            Assert.False(_processor.IsProcessing);
            Assert.NotEmpty(_processor.SupportedCommands);
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new VoiceCommandProcessor(null!));
        }

        [Theory]
        [InlineData("what's my status", VoiceCommandType.Status)]
        [InlineData("show status", VoiceCommandType.Status)]
        [InlineData("status check", VoiceCommandType.Status)]
        [InlineData("generate code", VoiceCommandType.GenerateCode)]
        [InlineData("create a function", VoiceCommandType.GenerateCode)]
        [InlineData("write some code", VoiceCommandType.GenerateCode)]
        [InlineData("analyze this", VoiceCommandType.Analyze)]
        [InlineData("examine the code", VoiceCommandType.Analyze)]
        [InlineData("open settings", VoiceCommandType.Settings)]
        [InlineData("help", VoiceCommandType.Help)]
        [InlineData("stop", VoiceCommandType.Stop)]
        [InlineData("exit", VoiceCommandType.Exit)]
        [InlineData("repeat that", VoiceCommandType.Repeat)]
        [InlineData("search for something", VoiceCommandType.Search)]
        [InlineData("find something", VoiceCommandType.Search)]
        [InlineData("go to dashboard", VoiceCommandType.Navigate)]
        [InlineData("what is the weather", VoiceCommandType.Chat)]
        public async Task ClassifyCommandAsync_WithValidCommands_ClassifiesCorrectly(string commandText, VoiceCommandType expectedType)
        {
            // Act
            var result = await _processor.ClassifyCommandAsync(commandText);

            // Assert
            Assert.Equal(expectedType, result.CommandType);
            Assert.True(result.ClassificationConfidence > 0);
            Assert.Equal(commandText, result.Text);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task ClassifyCommandAsync_WithInvalidInput_ReturnsUnknownType(string? commandText)
        {
            // Act
            var result = await _processor.ClassifyCommandAsync(commandText ?? "");

            // Assert
            Assert.Equal(VoiceCommandType.Unknown, result.CommandType);
            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task ClassifyCommandAsync_WithParameterExtraction_ExtractsParameters()
        {
            // Arrange
            var commandText = "search for machine learning";

            // Act
            var result = await _processor.ClassifyCommandAsync(commandText);

            // Assert
            Assert.Equal(VoiceCommandType.Search, result.CommandType);
            Assert.Contains("param1", result.Parameters.Keys);
        }

        [Fact]
        public async Task ProcessCommandAsync_WithValidCommand_ReturnsSuccessResult()
        {
            // Arrange
            var command = new VoiceCommand
            {
                Text = "what's my status",
                CommandType = VoiceCommandType.Status,
                ClassificationConfidence = 0.9f,
                RecognitionConfidence = 0.8f,
                Source = VoiceCommandSource.WakeWord
            };

            // Act
            var result = await _processor.ProcessCommandAsync(command);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Response);
            Assert.True(result.ShouldSpeak);
            Assert.True(result.ProcessingTimeMs > 0);
        }

        [Fact]
        public async Task ProcessCommandAsync_WithInvalidCommand_ReturnsErrorResult()
        {
            // Arrange
            var command = new VoiceCommand
            {
                Text = "",
                CommandType = VoiceCommandType.Unknown,
                ClassificationConfidence = 0.1f,
                RecognitionConfidence = 0.1f
            };

            // Act
            var result = await _processor.ProcessCommandAsync(command);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("didn't understand", result.Response ?? "");
            Assert.True(result.ShouldSpeak);
        }

        [Fact]
        public async Task ProcessCommandAsync_WithUnsupportedCommand_ReturnsErrorResult()
        {
            // Arrange
            var command = new VoiceCommand
            {
                Text = "unsupported command",
                CommandType = (VoiceCommandType)999, // Non-existent command type
                ClassificationConfidence = 0.9f,
                RecognitionConfidence = 0.8f
            };

            // Act
            var result = await _processor.ProcessCommandAsync(command);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("don't know how to handle", result.Response ?? "");
        }

        [Fact]
        public async Task ProcessTextCommandAsync_WithValidText_ProcessesSuccessfully()
        {
            // Arrange
            var commandText = "what's my status";
            var source = VoiceCommandSource.WakeWord;

            // Act
            var result = await _processor.ProcessTextCommandAsync(commandText, source);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.Response);
        }

        [Fact]
        public void RegisterCommandHandler_WithValidHandler_RegistersSuccessfully()
        {
            // Arrange
            var commandType = VoiceCommandType.Chat;
            var handlerCalled = false;
            
            Task<VoiceCommandResult> CustomHandler(VoiceCommand cmd, CancellationToken ct)
            {
                handlerCalled = true;
                return Task.FromResult(VoiceCommandResult.CreateSuccess("Custom response"));
            }

            // Act
            _processor.RegisterCommandHandler(commandType, CustomHandler);

            // Process a command to test the handler
            var command = new VoiceCommand
            {
                Text = "test",
                CommandType = commandType,
                ClassificationConfidence = 0.9f,
                RecognitionConfidence = 0.8f
            };

            var result = _processor.ProcessCommandAsync(command).Result;

            // Assert
            Assert.True(handlerCalled);
            Assert.Equal("Custom response", result.Response);
        }

        [Fact]
        public void UnregisterCommandHandler_WithExistingHandler_RemovesHandler()
        {
            // Arrange
            var commandType = VoiceCommandType.Chat;
            
            Task<VoiceCommandResult> CustomHandler(VoiceCommand cmd, CancellationToken ct)
            {
                return Task.FromResult(VoiceCommandResult.CreateSuccess("Custom response"));
            }

            _processor.RegisterCommandHandler(commandType, CustomHandler);

            // Act
            _processor.UnregisterCommandHandler(commandType);

            // Try to process a command - should use default handler or fail
            var command = new VoiceCommand
            {
                Text = "test",
                CommandType = commandType,
                ClassificationConfidence = 0.9f,
                RecognitionConfidence = 0.8f
            };

            var result = _processor.ProcessCommandAsync(command).Result;

            // Assert - Should not use the custom handler response
            Assert.NotEqual("Custom response", result.Response);
        }

        [Fact]
        public void GetCommandPatterns_WithValidCommandType_ReturnsPatterns()
        {
            // Act
            var patterns = _processor.GetCommandPatterns(VoiceCommandType.Status);

            // Assert
            Assert.NotEmpty(patterns);
            Assert.Contains(patterns, p => p.Contains("status"));
        }

        [Fact]
        public void GetCommandPatterns_WithInvalidCommandType_ReturnsEmptyList()
        {
            // Act
            var patterns = _processor.GetCommandPatterns((VoiceCommandType)999);

            // Assert
            Assert.Empty(patterns);
        }

        [Fact]
        public void UpdateCommandPatterns_WithValidPatterns_UpdatesSuccessfully()
        {
            // Arrange
            var commandType = VoiceCommandType.Status;
            var newPatterns = new[] { "custom status pattern", "another status pattern" };

            // Act
            _processor.UpdateCommandPatterns(commandType, newPatterns);
            var retrievedPatterns = _processor.GetCommandPatterns(commandType);

            // Assert
            Assert.Equal(newPatterns.Length, retrievedPatterns.Count);
            Assert.Contains("custom status pattern", retrievedPatterns);
            Assert.Contains("another status pattern", retrievedPatterns);
        }

        [Fact]
        public void GetProcessingStatistics_ReturnsValidStatistics()
        {
            // Act
            var stats = _processor.GetProcessingStatistics();

            // Assert
            Assert.NotNull(stats);
            Assert.Contains("total_classifications", stats.Keys);
            Assert.Contains("total_processed", stats.Keys);
            Assert.Contains("successful_processed", stats.Keys);
            Assert.Contains("failed_processed", stats.Keys);
            Assert.Contains("average_processing_time_ms", stats.Keys);
        }

        [Fact]
        public void ClearStatistics_ResetsAllStatistics()
        {
            // Arrange - Process some commands to generate statistics
            var command = new VoiceCommand
            {
                Text = "status",
                CommandType = VoiceCommandType.Status,
                ClassificationConfidence = 0.9f,
                RecognitionConfidence = 0.8f
            };

            _processor.ProcessCommandAsync(command).Wait();

            // Act
            _processor.ClearStatistics();
            var stats = _processor.GetProcessingStatistics();

            // Assert
            Assert.Equal(0, stats["total_classifications"]);
            Assert.Equal(0, stats["total_processed"]);
            Assert.Equal(0, stats["successful_processed"]);
            Assert.Equal(0, stats["failed_processed"]);
        }

        [Fact]
        public void CommandReceived_Event_RaisedWhenCommandProcessed()
        {
            // Arrange
            VoiceCommandReceivedEventArgs? eventArgs = null;
            _processor.CommandReceived += (sender, args) => eventArgs = args;

            var command = new VoiceCommand
            {
                Text = "test command",
                CommandType = VoiceCommandType.Status,
                ClassificationConfidence = 0.9f,
                RecognitionConfidence = 0.8f
            };

            // Act
            _processor.ProcessCommandAsync(command).Wait();

            // Assert
            Assert.NotNull(eventArgs);
            Assert.Equal("test command", eventArgs.Command.Text);
        }

        [Fact]
        public void CommandProcessed_Event_RaisedWhenCommandCompleted()
        {
            // Arrange
            VoiceCommandProcessedEventArgs? eventArgs = null;
            _processor.CommandProcessed += (sender, args) => eventArgs = args;

            var command = new VoiceCommand
            {
                Text = "test command",
                CommandType = VoiceCommandType.Status,
                ClassificationConfidence = 0.9f,
                RecognitionConfidence = 0.8f
            };

            // Act
            _processor.ProcessCommandAsync(command).Wait();

            // Assert
            Assert.NotNull(eventArgs);
            Assert.Equal("test command", eventArgs.Command.Text);
            Assert.NotNull(eventArgs.Result);
            Assert.True(eventArgs.ProcessingTime.TotalMilliseconds > 0);
        }

        [Fact]
        public async Task ProcessCommandAsync_WithCancellation_CancelsGracefully()
        {
            // Arrange
            var command = new VoiceCommand
            {
                Text = "test command",
                CommandType = VoiceCommandType.Status,
                ClassificationConfidence = 0.9f,
                RecognitionConfidence = 0.8f
            };

            using var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            // Act
            var result = await _processor.ProcessCommandAsync(command, cts.Token);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("cancelled", result.Response ?? "");
        }

        [Theory]
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
        public void SupportedCommands_ContainsAllDefaultCommands(VoiceCommandType commandType)
        {
            // Assert
            Assert.Contains(commandType, _processor.SupportedCommands);
        }
    }
}
