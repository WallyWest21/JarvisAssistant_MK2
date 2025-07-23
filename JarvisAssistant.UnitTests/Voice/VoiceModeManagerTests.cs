using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models;
using JarvisAssistant.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JarvisAssistant.UnitTests.Voice
{
    /// <summary>
    /// Unit tests for the VoiceModeManager class.
    /// </summary>
    public class VoiceModeManagerTests : IDisposable
    {
        private readonly Mock<IPlatformService> _mockPlatformService;
        private readonly Mock<IVoiceService> _mockVoiceService;
        private readonly Mock<IVoiceCommandProcessor> _mockCommandProcessor;
        private readonly Mock<ILogger<VoiceModeManager>> _mockLogger;
        private readonly VoiceModeManager _voiceModeManager;

        public VoiceModeManagerTests()
        {
            _mockPlatformService = new Mock<IPlatformService>();
            _mockVoiceService = new Mock<IVoiceService>();
            _mockCommandProcessor = new Mock<IVoiceCommandProcessor>();
            _mockLogger = new Mock<ILogger<VoiceModeManager>>();

            _voiceModeManager = new VoiceModeManager(
                _mockPlatformService.Object,
                _mockVoiceService.Object,
                _mockCommandProcessor.Object,
                _mockLogger.Object);
        }

        [Fact]
        public void Constructor_WithValidParameters_InitializesCorrectly()
        {
            // Assert
            Assert.NotNull(_voiceModeManager);
            Assert.False(_voiceModeManager.IsVoiceModeActive);
            Assert.Equal(VoiceModeState.Inactive, _voiceModeManager.CurrentState);
        }

        [Fact]
        public void Constructor_WithNullPlatformService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new VoiceModeManager(
                null!,
                _mockVoiceService.Object,
                _mockCommandProcessor.Object,
                _mockLogger.Object));
        }

        [Theory]
        [InlineData(PlatformType.AndroidTV, VoiceActivationMode.AlwaysOn)]
        [InlineData(PlatformType.Android, VoiceActivationMode.Toggle)]
        [InlineData(PlatformType.Windows, VoiceActivationMode.Toggle)]
        [InlineData(PlatformType.iOS, VoiceActivationMode.Toggle)]
        public void ActivationMode_BasedOnPlatform_ConfiguredCorrectly(PlatformType platform, VoiceActivationMode expectedMode)
        {
            // Arrange
            _mockPlatformService.Setup(x => x.CurrentPlatform).Returns(platform);
            _mockPlatformService.Setup(x => x.IsGoogleTV()).Returns(platform == PlatformType.AndroidTV);
            _mockPlatformService.Setup(x => x.SupportsVoiceInput()).Returns(true);

            // Act
            var manager = new VoiceModeManager(
                _mockPlatformService.Object,
                _mockVoiceService.Object,
                _mockCommandProcessor.Object,
                _mockLogger.Object);

            // Assert
            Assert.Equal(expectedMode, manager.ActivationMode);
        }

        [Fact]
        public void IsGoogleTV_WhenTrue_SetsAlwaysOnMode()
        {
            // Arrange
            _mockPlatformService.Setup(x => x.IsGoogleTV()).Returns(true);
            _mockPlatformService.Setup(x => x.SupportsVoiceInput()).Returns(true);

            // Act
            var manager = new VoiceModeManager(
                _mockPlatformService.Object,
                _mockVoiceService.Object,
                _mockCommandProcessor.Object,
                _mockLogger.Object);

            // Assert
            Assert.Equal(VoiceActivationMode.AlwaysOn, manager.ActivationMode);
            Assert.False(manager.CanToggleVoiceMode);
            Assert.True(manager.IsWakeWordDetectionEnabled);
        }

        [Fact]
        public async Task EnableVoiceModeAsync_WhenPlatformSupportsVoice_ReturnsTrue()
        {
            // Arrange
            _mockPlatformService.Setup(x => x.SupportsVoiceInput()).Returns(true);

            // Act
            var result = await _voiceModeManager.EnableVoiceModeAsync();

            // Assert
            Assert.True(result);
            Assert.True(_voiceModeManager.IsVoiceModeActive);
            Assert.Equal(VoiceModeState.Listening, _voiceModeManager.CurrentState);
        }

        [Fact]
        public async Task EnableVoiceModeAsync_WhenPlatformDoesNotSupportVoice_ReturnsFalse()
        {
            // Arrange
            _mockPlatformService.Setup(x => x.SupportsVoiceInput()).Returns(false);

            // Act
            var result = await _voiceModeManager.EnableVoiceModeAsync();

            // Assert
            Assert.False(result);
            Assert.False(_voiceModeManager.IsVoiceModeActive);
            Assert.Equal(VoiceModeState.Error, _voiceModeManager.CurrentState);
        }

        [Fact]
        public async Task EnableVoiceModeAsync_WhenAlreadyActive_ReturnsTrue()
        {
            // Arrange
            _mockPlatformService.Setup(x => x.SupportsVoiceInput()).Returns(true);
            await _voiceModeManager.EnableVoiceModeAsync();

            // Act
            var result = await _voiceModeManager.EnableVoiceModeAsync();

            // Assert
            Assert.True(result);
            Assert.True(_voiceModeManager.IsVoiceModeActive);
        }

        [Fact]
        public async Task DisableVoiceModeAsync_WhenToggleModeAndActive_ReturnsTrue()
        {
            // Arrange
            _mockPlatformService.Setup(x => x.SupportsVoiceInput()).Returns(true);
            _mockPlatformService.Setup(x => x.IsGoogleTV()).Returns(false);
            
            await _voiceModeManager.EnableVoiceModeAsync();

            // Act
            var result = await _voiceModeManager.DisableVoiceModeAsync();

            // Assert
            Assert.True(result);
            Assert.False(_voiceModeManager.IsVoiceModeActive);
            Assert.Equal(VoiceModeState.Inactive, _voiceModeManager.CurrentState);
        }

        [Fact]
        public async Task DisableVoiceModeAsync_WhenAlwaysOnMode_ReturnsFalse()
        {
            // Arrange
            _mockPlatformService.Setup(x => x.SupportsVoiceInput()).Returns(true);
            _mockPlatformService.Setup(x => x.IsGoogleTV()).Returns(true);

            var manager = new VoiceModeManager(
                _mockPlatformService.Object,
                _mockVoiceService.Object,
                _mockCommandProcessor.Object,
                _mockLogger.Object);

            await manager.EnableVoiceModeAsync();

            // Act
            var result = await manager.DisableVoiceModeAsync();

            // Assert
            Assert.False(result);
            Assert.True(manager.IsVoiceModeActive);
        }

        [Fact]
        public async Task ToggleVoiceModeAsync_WhenCanToggle_TogglesState()
        {
            // Arrange - Create a new manager with proper mock setup
            _mockPlatformService.Setup(x => x.SupportsVoiceInput()).Returns(true);
            _mockPlatformService.Setup(x => x.IsGoogleTV()).Returns(false);
            _mockPlatformService.Setup(x => x.CurrentPlatform).Returns(PlatformType.Windows);

            var manager = new VoiceModeManager(
                _mockPlatformService.Object,
                _mockVoiceService.Object,
                _mockCommandProcessor.Object,
                _mockLogger.Object);

            // Act - Toggle on
            var result1 = await manager.ToggleVoiceModeAsync();

            // Assert
            Assert.True(result1);
            Assert.True(manager.IsVoiceModeActive);

            // Act - Toggle off
            var result2 = await manager.ToggleVoiceModeAsync();

            // Assert
            Assert.True(result2);
            Assert.False(manager.IsVoiceModeActive);
        }

        [Fact]
        public async Task ToggleVoiceModeAsync_WhenCannotToggle_ReturnsCurrentState()
        {
            // Arrange
            _mockPlatformService.Setup(x => x.SupportsVoiceInput()).Returns(true);
            _mockPlatformService.Setup(x => x.IsGoogleTV()).Returns(true);

            var manager = new VoiceModeManager(
                _mockPlatformService.Object,
                _mockVoiceService.Object,
                _mockCommandProcessor.Object,
                _mockLogger.Object);

            // Act
            var result = await manager.ToggleVoiceModeAsync();

            // Assert - Should return current state since toggle is not allowed
            Assert.Equal(manager.IsVoiceModeActive, result);
        }

        [Fact]
        public async Task ListenForCommandAsync_WithTimeout_ReturnsNullWhenNoCommand()
        {
            // Arrange
            var timeout = TimeSpan.FromMilliseconds(100);

            // Act
            var result = await _voiceModeManager.ListenForCommandAsync(timeout);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ConfigureWakeWordDetectionAsync_UpdatesSettings()
        {
            // Arrange
            var enabled = true;
            var sensitivity = 0.8f;
            var wakeWords = new[] { "test jarvis", "hello jarvis" };

            // Act
            await _voiceModeManager.ConfigureWakeWordDetectionAsync(enabled, sensitivity, wakeWords);

            // Assert
            Assert.Equal(enabled, _voiceModeManager.IsWakeWordDetectionEnabled);
        }

        [Fact]
        public void GetCurrentAudioLevel_ReturnsValidRange()
        {
            // Act
            var level = _voiceModeManager.GetCurrentAudioLevel();

            // Assert
            Assert.InRange(level, 0f, 1f);
        }

        [Fact]
        public void StateChanged_Event_RaisedWhenStateChanges()
        {
            // Arrange
            VoiceModeStateChangedEventArgs? eventArgs = null;
            _voiceModeManager.StateChanged += (sender, args) => eventArgs = args;

            _mockPlatformService.Setup(x => x.SupportsVoiceInput()).Returns(true);

            // Act
            _ = _voiceModeManager.EnableVoiceModeAsync();

            // Give some time for the state change
            Thread.Sleep(100);

            // Assert
            Assert.NotNull(eventArgs);
            Assert.Equal(VoiceModeState.Inactive, eventArgs.PreviousState);
            Assert.Equal(VoiceModeState.Listening, eventArgs.NewState);
        }

        [Fact]
        public void WakeWordDetected_Event_CanBeSubscribed()
        {
            // Arrange
            var eventRaised = false;
            _voiceModeManager.WakeWordDetected += (sender, args) => eventRaised = true;

            // Act - This is primarily testing that the event can be subscribed to
            // In a real implementation, this would be triggered by actual wake word detection

            // Assert
            Assert.False(eventRaised); // Event hasn't been raised yet, but can be subscribed
        }

        [Fact]
        public void VoiceActivityDetected_Event_CanBeSubscribed()
        {
            // Arrange
            var eventRaised = false;
            _voiceModeManager.VoiceActivityDetected += (sender, args) => eventRaised = true;

            // Act - This is primarily testing that the event can be subscribed to

            // Assert
            Assert.False(eventRaised); // Event hasn't been raised yet, but can be subscribed
        }

        public void Dispose()
        {
            _voiceModeManager?.Dispose();
        }
    }
}
