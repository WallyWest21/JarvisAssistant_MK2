using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JarvisAssistant.UnitTests.Voice
{
    /// <summary>
    /// Mock implementation of Android KeyEvent for unit testing.
    /// </summary>
    public class MockKeyEvent
    {
        public MockKeyEventAction Action { get; set; }
        public MockKeycode KeyCode { get; set; }
    }

    /// <summary>
    /// Mock implementation of Android KeyEventActions for unit testing.
    /// </summary>
    public enum MockKeyEventAction
    {
        Down,
        Up
    }

    /// <summary>
    /// Mock implementation of Android Keycode for unit testing.
    /// </summary>
    public enum MockKeycode
    {
        Search,
        VoiceAssist,
        MediaRecord, // Replaces Microphone which doesn't exist
        MediaPlay,
        MediaPause,
        MediaPlayPause,
        Back,
        Home,
        Menu
    }

    /// <summary>
    /// Mock wrapper for GoogleTVVoiceHandler that abstracts Android dependencies for testing.
    /// </summary>
    public class TestableGoogleTVVoiceHandler : IDisposable
    {
        private readonly IVoiceModeManager _voiceModeManager;
        private readonly IVoiceCommandProcessor _commandProcessor;
        private readonly IPlatformService _platformService;
        private readonly ILogger _logger;
        private bool _isInitialized;
        private bool _disposed;

        public TestableGoogleTVVoiceHandler(
            IVoiceModeManager voiceModeManager,
            IVoiceCommandProcessor commandProcessor,
            IPlatformService platformService,
            ILogger logger)
        {
            _voiceModeManager = voiceModeManager ?? throw new ArgumentNullException(nameof(voiceModeManager));
            _commandProcessor = commandProcessor ?? throw new ArgumentNullException(nameof(commandProcessor));
            _platformService = platformService ?? throw new ArgumentNullException(nameof(platformService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool IsActive => _isInitialized && !_disposed;
        public bool IsListening => _voiceModeManager.IsVoiceModeActive;

        public event EventHandler<VoiceButtonPressedEventArgs>? VoiceButtonPressed;
        public event EventHandler<ContinuousListeningStateChangedEventArgs>? ListeningStateChanged;

        public Task InitializeAsync()
        {
            _isInitialized = true;
            return Task.CompletedTask;
        }

        public bool HandleKeyEvent(MockKeycode keyCode, MockKeyEvent keyEvent)
        {
            if (!_isInitialized || _disposed)
            {
                return false;
            }

            // Only handle key down events to avoid double processing
            if (keyEvent.Action != MockKeyEventAction.Down)
            {
                return false;
            }

            switch (keyCode)
            {
                case MockKeycode.Search:
                case MockKeycode.VoiceAssist:
                case MockKeycode.MediaRecord:
                    OnVoiceButtonPressed(new VoiceButtonPressedEventArgs
                    {
                        ButtonType = MapKeycodeToButtonType(keyCode),
                        Timestamp = DateTime.UtcNow
                    });
                    return true;

                case MockKeycode.MediaPlay:
                case MockKeycode.MediaPause:
                case MockKeycode.MediaPlayPause:
                    return false; // Not handled for voice input

                default:
                    return false;
            }
        }

        public async Task<bool> TriggerVoiceListeningAsync(VoiceCommandSource source = VoiceCommandSource.RemoteControl)
        {
            if (!_isInitialized || _disposed)
            {
                return false;
            }

            if (!_voiceModeManager.IsVoiceModeActive)
            {
                return await _voiceModeManager.EnableVoiceModeAsync();
            }

            return true;
        }

        public Task ConfigureContinuousListeningAsync(bool enabled, float sensitivity = 0.7f)
        {
            if (!_isInitialized || _disposed)
            {
                return Task.CompletedTask;
            }

            OnListeningStateChanged(enabled);
            return Task.CompletedTask;
        }

        public Dictionary<string, object> GetStatistics()
        {
            return new Dictionary<string, object>
            {
                ["is_initialized"] = _isInitialized,
                ["is_active"] = IsActive,
                ["is_listening"] = IsListening,
                ["voice_mode_state"] = _voiceModeManager.CurrentState.ToString(),
                ["activation_mode"] = _voiceModeManager.ActivationMode.ToString(),
                ["wake_word_enabled"] = _voiceModeManager.IsWakeWordDetectionEnabled,
                ["platform"] = _platformService.CurrentPlatform.ToString(),
                ["is_google_tv"] = _platformService.IsGoogleTV()
            };
        }

        private VoiceButtonType MapKeycodeToButtonType(MockKeycode keyCode)
        {
            return keyCode switch
            {
                MockKeycode.Search => VoiceButtonType.Search,
                MockKeycode.VoiceAssist => VoiceButtonType.VoiceAssist,
                MockKeycode.MediaRecord => VoiceButtonType.Microphone,
                _ => VoiceButtonType.Unknown
            };
        }

        private void OnVoiceButtonPressed(VoiceButtonPressedEventArgs eventArgs)
        {
            VoiceButtonPressed?.Invoke(this, eventArgs);
        }

        private void OnListeningStateChanged(bool isListening)
        {
            ListeningStateChanged?.Invoke(this, new ContinuousListeningStateChangedEventArgs
            {
                IsListening = isListening,
                Timestamp = DateTime.UtcNow
            });
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// Unit tests for the GoogleTVVoiceHandler class using testable wrapper.
    /// </summary>
    public class GoogleTVVoiceHandlerTests : IDisposable
    {
        private readonly Mock<IVoiceModeManager> _mockVoiceModeManager;
        private readonly Mock<IVoiceCommandProcessor> _mockCommandProcessor;
        private readonly Mock<IPlatformService> _mockPlatformService;
        private readonly Mock<ILogger> _mockLogger;
        private readonly TestableGoogleTVVoiceHandler _handler;

        public GoogleTVVoiceHandlerTests()
        {
            _mockVoiceModeManager = new Mock<IVoiceModeManager>();
            _mockCommandProcessor = new Mock<IVoiceCommandProcessor>();
            _mockPlatformService = new Mock<IPlatformService>();
            _mockLogger = new Mock<ILogger>();

            _handler = new TestableGoogleTVVoiceHandler(
                _mockVoiceModeManager.Object,
                _mockCommandProcessor.Object,
                _mockPlatformService.Object,
                _mockLogger.Object);
        }

        [Fact]
        public void Constructor_WithValidParameters_InitializesCorrectly()
        {
            // Assert
            Assert.NotNull(_handler);
            Assert.False(_handler.IsActive);
        }

        [Fact]
        public void Constructor_WithNullVoiceModeManager_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TestableGoogleTVVoiceHandler(
                null!,
                _mockCommandProcessor.Object,
                _mockPlatformService.Object,
                _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullCommandProcessor_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TestableGoogleTVVoiceHandler(
                _mockVoiceModeManager.Object,
                null!,
                _mockPlatformService.Object,
                _mockLogger.Object));
        }

        [Fact]
        public void Constructor_WithNullPlatformService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TestableGoogleTVVoiceHandler(
                _mockVoiceModeManager.Object,
                _mockCommandProcessor.Object,
                null!,
                _mockLogger.Object));
        }

        [Fact]
        public void IsListening_ReflectsVoiceModeManagerState()
        {
            // Arrange
            _mockVoiceModeManager.Setup(x => x.IsVoiceModeActive).Returns(true);

            // Assert
            Assert.True(_handler.IsListening);

            // Arrange
            _mockVoiceModeManager.Setup(x => x.IsVoiceModeActive).Returns(false);

            // Assert
            Assert.False(_handler.IsListening);
        }

        [Theory]
        [InlineData(MockKeycode.Search, true)]
        [InlineData(MockKeycode.VoiceAssist, true)]
        [InlineData(MockKeycode.MediaRecord, true)]
        [InlineData(MockKeycode.Back, false)]
        [InlineData(MockKeycode.Home, false)]
        [InlineData(MockKeycode.Menu, false)]
        public async Task HandleKeyEvent_WithVariousKeyCodes_ReturnsExpectedResult(MockKeycode keyCode, bool expectedHandled)
        {
            // Arrange
            await _handler.InitializeAsync();
            var mockKeyEvent = new MockKeyEvent
            {
                Action = MockKeyEventAction.Down,
                KeyCode = keyCode
            };

            // Act
            var result = _handler.HandleKeyEvent(keyCode, mockKeyEvent);

            // Assert
            Assert.Equal(expectedHandled, result);
        }

        [Fact]
        public async Task HandleKeyEvent_WithKeyUpAction_ReturnsFalse()
        {
            // Arrange
            await _handler.InitializeAsync();
            var mockKeyEvent = new MockKeyEvent
            {
                Action = MockKeyEventAction.Up,
                KeyCode = MockKeycode.Search
            };

            // Act
            var result = _handler.HandleKeyEvent(MockKeycode.Search, mockKeyEvent);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void HandleKeyEvent_WhenNotInitialized_ReturnsFalse()
        {
            // Arrange
            var mockKeyEvent = new MockKeyEvent
            {
                Action = MockKeyEventAction.Down,
                KeyCode = MockKeycode.Search
            };

            // Act - Handler is not initialized
            var result = _handler.HandleKeyEvent(MockKeycode.Search, mockKeyEvent);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void VoiceButtonPressed_Event_CanBeSubscribed()
        {
            // Arrange
            VoiceButtonPressedEventArgs? eventArgs = null;
            _handler.VoiceButtonPressed += (sender, args) => eventArgs = args;

            // This test verifies that the event can be subscribed to
            // In a real scenario, the event would be raised by key handling

            // Assert
            Assert.Null(eventArgs); // Event hasn't been raised yet
        }

        [Fact]
        public void ListeningStateChanged_Event_CanBeSubscribed()
        {
            // Arrange
            ContinuousListeningStateChangedEventArgs? eventArgs = null;
            _handler.ListeningStateChanged += (sender, args) => eventArgs = args;

            // This test verifies that the event can be subscribed to

            // Assert
            Assert.Null(eventArgs); // Event hasn't been raised yet
        }

        [Fact]
        public async Task TriggerVoiceListeningAsync_WhenNotInitialized_ReturnsFalse()
        {
            // Act
            var result = await _handler.TriggerVoiceListeningAsync();

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task TriggerVoiceListeningAsync_WhenVoiceModeInactive_EnablesVoiceMode()
        {
            // Arrange
            await _handler.InitializeAsync();
            _mockVoiceModeManager.Setup(x => x.IsVoiceModeActive).Returns(false);
            _mockVoiceModeManager.Setup(x => x.EnableVoiceModeAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.TriggerVoiceListeningAsync();

            // Assert
            Assert.True(result);
            _mockVoiceModeManager.Verify(x => x.EnableVoiceModeAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ConfigureContinuousListeningAsync_WhenNotInitialized_DoesNotThrow()
        {
            // Act & Assert - Should not throw
            await _handler.ConfigureContinuousListeningAsync(true, 0.8f);
        }

        [Fact]
        public async Task GetStatistics_ReturnsValidStatistics()
        {
            // Arrange
            await _handler.InitializeAsync();
            _mockPlatformService.Setup(x => x.CurrentPlatform).Returns(PlatformType.AndroidTV);
            _mockPlatformService.Setup(x => x.IsGoogleTV()).Returns(true);
            _mockVoiceModeManager.Setup(x => x.CurrentState).Returns(VoiceModeState.Listening);
            _mockVoiceModeManager.Setup(x => x.ActivationMode).Returns(VoiceActivationMode.AlwaysOn);
            _mockVoiceModeManager.Setup(x => x.IsWakeWordDetectionEnabled).Returns(true);

            // Act
            var stats = _handler.GetStatistics();

            // Assert
            Assert.NotNull(stats);
            Assert.Contains("is_initialized", stats.Keys);
            Assert.Contains("is_active", stats.Keys);
            Assert.Contains("is_listening", stats.Keys);
            Assert.Contains("voice_mode_state", stats.Keys);
            Assert.Contains("activation_mode", stats.Keys);
            Assert.Contains("wake_word_enabled", stats.Keys);
            Assert.Contains("platform", stats.Keys);
            Assert.Contains("is_google_tv", stats.Keys);
        }

        [Fact]
        public async Task GetStatistics_ReturnsCorrectValues()
        {
            // Arrange
            await _handler.InitializeAsync();
            _mockPlatformService.Setup(x => x.CurrentPlatform).Returns(PlatformType.AndroidTV);
            _mockPlatformService.Setup(x => x.IsGoogleTV()).Returns(true);
            _mockVoiceModeManager.Setup(x => x.CurrentState).Returns(VoiceModeState.Listening);
            _mockVoiceModeManager.Setup(x => x.ActivationMode).Returns(VoiceActivationMode.AlwaysOn);
            _mockVoiceModeManager.Setup(x => x.IsWakeWordDetectionEnabled).Returns(true);

            // Act
            var stats = _handler.GetStatistics();

            // Assert
            Assert.Equal(true, stats["is_initialized"]);
            Assert.Equal(true, stats["is_active"]);
            Assert.Equal("Listening", stats["voice_mode_state"]);
            Assert.Equal("AlwaysOn", stats["activation_mode"]);
            Assert.Equal(true, stats["wake_word_enabled"]);
            Assert.Equal("AndroidTV", stats["platform"]);
            Assert.Equal(true, stats["is_google_tv"]);
        }

        [Fact]
        public void Dispose_DoesNotThrow()
        {
            // Act & Assert - Should not throw
            _handler.Dispose();
        }

        [Fact]
        public void Dispose_MultipleCalls_DoesNotThrow()
        {
            // Act & Assert - Multiple dispose calls should not throw
            _handler.Dispose();
            _handler.Dispose();
            _handler.Dispose();
        }

        public void Dispose()
        {
            _handler?.Dispose();
        }
    }

    /// <summary>
    /// Unit tests for voice-related event arguments and enums.
    /// </summary>
    public class VoiceEventArgsTests
    {
        [Fact]
        public void VoiceButtonPressedEventArgs_DefaultValues_AreSet()
        {
            // Act
            var eventArgs = new VoiceButtonPressedEventArgs();

            // Assert
            Assert.Equal(VoiceButtonType.Unknown, eventArgs.ButtonType);
            Assert.True(DateTime.UtcNow.Subtract(eventArgs.Timestamp).TotalSeconds < 1);
            Assert.NotNull(eventArgs.AdditionalData);
            Assert.Empty(eventArgs.AdditionalData);
        }

        [Fact]
        public void VoiceButtonPressedEventArgs_CanSetProperties()
        {
            // Arrange
            var timestamp = DateTime.UtcNow.AddMinutes(-1);
            var additionalData = new Dictionary<string, object> { ["key"] = "value" };

            // Act
            var eventArgs = new VoiceButtonPressedEventArgs
            {
                ButtonType = VoiceButtonType.Search,
                Timestamp = timestamp,
                AdditionalData = additionalData
            };

            // Assert
            Assert.Equal(VoiceButtonType.Search, eventArgs.ButtonType);
            Assert.Equal(timestamp, eventArgs.Timestamp);
            Assert.Equal(additionalData, eventArgs.AdditionalData);
        }

        [Fact]
        public void ContinuousListeningStateChangedEventArgs_DefaultValues_AreSet()
        {
            // Act
            var eventArgs = new ContinuousListeningStateChangedEventArgs();

            // Assert
            Assert.False(eventArgs.IsListening);
            Assert.True(DateTime.UtcNow.Subtract(eventArgs.Timestamp).TotalSeconds < 1);
            Assert.Null(eventArgs.ErrorMessage);
        }

        [Fact]
        public void ContinuousListeningStateChangedEventArgs_CanSetProperties()
        {
            // Arrange
            var timestamp = DateTime.UtcNow.AddMinutes(-1);
            var errorMessage = "Test error";

            // Act
            var eventArgs = new ContinuousListeningStateChangedEventArgs
            {
                IsListening = true,
                Timestamp = timestamp,
                ErrorMessage = errorMessage
            };

            // Assert
            Assert.True(eventArgs.IsListening);
            Assert.Equal(timestamp, eventArgs.Timestamp);
            Assert.Equal(errorMessage, eventArgs.ErrorMessage);
        }

        [Theory]
        [InlineData(VoiceButtonType.Search)]
        [InlineData(VoiceButtonType.VoiceAssist)]
        [InlineData(VoiceButtonType.Microphone)]
        [InlineData(VoiceButtonType.Media)]
        [InlineData(VoiceButtonType.Unknown)]
        public void VoiceButtonType_AllValues_AreValid(VoiceButtonType buttonType)
        {
            // Act & Assert - Should not throw
            var eventArgs = new VoiceButtonPressedEventArgs
            {
                ButtonType = buttonType
            };

            Assert.Equal(buttonType, eventArgs.ButtonType);
        }
    }

    /// <summary>
    /// Event arguments for voice button press events.
    /// </summary>
    public class VoiceButtonPressedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the type of voice button that was pressed.
        /// </summary>
        public VoiceButtonType ButtonType { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the button was pressed.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets additional data about the button press.
        /// </summary>
        public Dictionary<string, object> AdditionalData { get; set; } = new();
    }

    /// <summary>
    /// Event arguments for continuous listening state change events.
    /// </summary>
    public class ContinuousListeningStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets a value indicating whether continuous listening is active.
        /// </summary>
        public bool IsListening { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the state changed.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets optional error information if the state change was due to an error.
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Represents the type of voice button pressed.
    /// </summary>
    public enum VoiceButtonType
    {
        /// <summary>
        /// Unknown or other button type.
        /// </summary>
        Unknown,

        /// <summary>
        /// Search button (typically center button or search key).
        /// </summary>
        Search,

        /// <summary>
        /// Voice assist button.
        /// </summary>
        VoiceAssist,

        /// <summary>
        /// Dedicated microphone button.
        /// </summary>
        Microphone,

        /// <summary>
        /// Media control button.
        /// </summary>
        Media
    }
}
