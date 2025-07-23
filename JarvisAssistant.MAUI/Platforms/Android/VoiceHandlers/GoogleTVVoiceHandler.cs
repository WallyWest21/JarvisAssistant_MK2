using Android.Content;
using Android.Views;
using AndroidX.AppCompat.App;
using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models;
using Microsoft.Extensions.Logging;

namespace JarvisAssistant.MAUI.Platforms.Android.VoiceHandlers
{
    /// <summary>
    /// Handles Google TV remote control voice input and provides continuous listening capabilities.
    /// </summary>
    public class GoogleTVVoiceHandler : IDisposable
    {
        private readonly IVoiceModeManager _voiceModeManager;
        private readonly IVoiceCommandProcessor _commandProcessor;
        private readonly IPlatformService _platformService;
        private readonly ILogger<GoogleTVVoiceHandler> _logger;
        private bool _isInitialized;
        private bool _disposed;
        private AppCompatActivity? _mainActivity;

        /// <summary>
        /// Initializes a new instance of the <see cref="GoogleTVVoiceHandler"/> class.
        /// </summary>
        /// <param name="voiceModeManager">The voice mode manager.</param>
        /// <param name="commandProcessor">The voice command processor.</param>
        /// <param name="platformService">The platform service.</param>
        /// <param name="logger">The logger instance.</param>
        public GoogleTVVoiceHandler(
            IVoiceModeManager voiceModeManager,
            IVoiceCommandProcessor commandProcessor,
            IPlatformService platformService,
            ILogger<GoogleTVVoiceHandler> logger)
        {
            _voiceModeManager = voiceModeManager ?? throw new ArgumentNullException(nameof(voiceModeManager));
            _commandProcessor = commandProcessor ?? throw new ArgumentNullException(nameof(commandProcessor));
            _platformService = platformService ?? throw new ArgumentNullException(nameof(platformService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets a value indicating whether this handler is active.
        /// </summary>
        public bool IsActive => _isInitialized && !_disposed;

        /// <summary>
        /// Gets a value indicating whether the handler is currently listening for voice input.
        /// </summary>
        public bool IsListening => _voiceModeManager.IsVoiceModeActive;

        /// <summary>
        /// Event raised when a voice button press is detected.
        /// </summary>
        public event EventHandler<VoiceButtonPressedEventArgs>? VoiceButtonPressed;

        /// <summary>
        /// Event raised when continuous listening state changes.
        /// </summary>
        public event EventHandler<ContinuousListeningStateChangedEventArgs>? ListeningStateChanged;

        /// <summary>
        /// Initializes the Google TV voice handler with the main activity.
        /// </summary>
        /// <param name="mainActivity">The main activity instance.</param>
        /// <returns>A task that represents the asynchronous initialization operation.</returns>
        public async Task InitializeAsync(AppCompatActivity mainActivity)
        {
            if (_isInitialized)
            {
                _logger.LogWarning("Google TV voice handler is already initialized");
                return;
            }

            try
            {
                _mainActivity = mainActivity ?? throw new ArgumentNullException(nameof(mainActivity));

                if (!_platformService.IsGoogleTV())
                {
                    _logger.LogWarning("Google TV voice handler initialized on non-Google TV platform");
                    return;
                }

                _logger.LogInformation("Initializing Google TV voice handler");

                // Subscribe to voice mode manager events
                _voiceModeManager.StateChanged += OnVoiceModeStateChanged;
                _voiceModeManager.WakeWordDetected += OnWakeWordDetected;

                // Enable voice mode for Google TV (always-on)
                await _voiceModeManager.EnableVoiceModeAsync();

                _isInitialized = true;
                _logger.LogInformation("Google TV voice handler initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Google TV voice handler");
                throw;
            }
        }

        /// <summary>
        /// Handles key events from the Google TV remote control.
        /// </summary>
        /// <param name="keyCode">The key code of the pressed key.</param>
        /// <param name="keyEvent">The key event details.</param>
        /// <returns>True if the key event was handled, false otherwise.</returns>
        public bool HandleKeyEvent(Keycode keyCode, KeyEvent keyEvent)
        {
            if (!_isInitialized || _disposed)
            {
                return false;
            }

            try
            {
                _logger.LogDebug("Handling key event: {KeyCode}, Action: {Action}", keyCode, keyEvent.Action);

                // Only handle key down events to avoid double processing
                if (keyEvent.Action != KeyEventActions.Down)
                {
                    return false;
                }

                switch (keyCode)
                {
                    case Keycode.Search:
                        return HandleSearchButton();

                    case Keycode.VoiceAssist:
                        return HandleVoiceAssistButton();

                    // Some Android TV remotes may use MediaRecord for microphone functionality
                    // Note: Keycode.Microphone doesn't exist in Android API
                    case Keycode.MediaRecord:
                        return HandleMicrophoneButton();

                    case Keycode.MediaPlay:
                    case Keycode.MediaPause:
                    case Keycode.MediaPlayPause:
                        // These could also trigger voice mode depending on user preference
                        return HandleMediaButton(keyCode);

                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling key event: {KeyCode}", keyCode);
                return false;
            }
        }

        /// <summary>
        /// Manually triggers voice listening mode.
        /// </summary>
        /// <param name="source">The source of the voice activation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task<bool> TriggerVoiceListeningAsync(VoiceCommandSource source = VoiceCommandSource.RemoteControl)
        {
            if (!_isInitialized || _disposed)
            {
                _logger.LogWarning("Cannot trigger voice listening - handler not initialized");
                return false;
            }

            try
            {
                _logger.LogInformation("Triggering voice listening from source: {Source}", source);

                if (!_voiceModeManager.IsVoiceModeActive)
                {
                    return await _voiceModeManager.EnableVoiceModeAsync();
                }

                // Voice mode is already active, trigger a single command listen
                var command = await _voiceModeManager.ListenForCommandAsync(TimeSpan.FromSeconds(10));
                if (!string.IsNullOrWhiteSpace(command))
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _commandProcessor.ProcessTextCommandAsync(command, source);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing voice command: {Command}", command);
                        }
                    });
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering voice listening");
                return false;
            }
        }

        /// <summary>
        /// Configures the continuous listening behavior for Google TV.
        /// </summary>
        /// <param name="enabled">Whether continuous listening should be enabled.</param>
        /// <param name="sensitivity">The wake word detection sensitivity (0.0 to 1.0).</param>
        /// <returns>A task that represents the asynchronous configuration operation.</returns>
        public async Task ConfigureContinuousListeningAsync(bool enabled, float sensitivity = 0.7f)
        {
            if (!_isInitialized || _disposed)
            {
                _logger.LogWarning("Cannot configure continuous listening - handler not initialized");
                return;
            }

            try
            {
                _logger.LogInformation("Configuring continuous listening - Enabled: {Enabled}, Sensitivity: {Sensitivity}", 
                    enabled, sensitivity);

                await _voiceModeManager.ConfigureWakeWordDetectionAsync(enabled, sensitivity, new[] { "hey jarvis", "jarvis" });

                OnListeningStateChanged(enabled);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error configuring continuous listening");
            }
        }

        /// <summary>
        /// Gets the current voice handler statistics.
        /// </summary>
        /// <returns>A dictionary containing handler statistics.</returns>
        public Dictionary<string, object> GetStatistics()
        {
            var stats = new Dictionary<string, object>
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

            return stats;
        }

        private bool HandleSearchButton()
        {
            _logger.LogDebug("Search button pressed on Google TV remote");

            OnVoiceButtonPressed(new VoiceButtonPressedEventArgs
            {
                ButtonType = VoiceButtonType.Search,
                Timestamp = DateTime.UtcNow
            });

            // Trigger voice listening
            _ = Task.Run(async () => await TriggerVoiceListeningAsync(VoiceCommandSource.RemoteControl));

            return true; // Indicate that we handled this key event
        }

        private bool HandleVoiceAssistButton()
        {
            _logger.LogDebug("Voice assist button pressed on Google TV remote");

            OnVoiceButtonPressed(new VoiceButtonPressedEventArgs
            {
                ButtonType = VoiceButtonType.VoiceAssist,
                Timestamp = DateTime.UtcNow
            });

            // Trigger voice listening
            _ = Task.Run(async () => await TriggerVoiceListeningAsync(VoiceCommandSource.RemoteControl));

            return true; // Indicate that we handled this key event
        }

        private bool HandleMicrophoneButton()
        {
            _logger.LogDebug("Microphone button pressed on Google TV remote");

            OnVoiceButtonPressed(new VoiceButtonPressedEventArgs
            {
                ButtonType = VoiceButtonType.Microphone,
                Timestamp = DateTime.UtcNow
            });

            // Trigger voice listening
            _ = Task.Run(async () => await TriggerVoiceListeningAsync(VoiceCommandSource.RemoteControl));

            return true; // Indicate that we handled this key event
        }

        private bool HandleMediaButton(Keycode keyCode)
        {
            _logger.LogDebug("Media button pressed on Google TV remote: {KeyCode}", keyCode);

            // For now, we don't handle media buttons for voice input
            // This could be configurable in the future
            return false;
        }

        private void OnVoiceModeStateChanged(object? sender, VoiceModeStateChangedEventArgs e)
        {
            _logger.LogDebug("Voice mode state changed: {Previous} -> {New}", e.PreviousState, e.NewState);

            if (e.NewState == VoiceModeState.Listening)
            {
                OnListeningStateChanged(true);
            }
            else if (e.NewState == VoiceModeState.Inactive)
            {
                OnListeningStateChanged(false);
            }
        }

        private void OnWakeWordDetected(object? sender, WakeWordDetectedEventArgs e)
        {
            _logger.LogInformation("Wake word detected: {WakeWord} (confidence: {Confidence:P1})", 
                e.WakeWord, e.Confidence);

            // Wake word detection is handled by the voice mode manager
            // We just log it here for Google TV specific tracking
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

        /// <summary>
        /// Disposes the Google TV voice handler and releases resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            try
            {
                if (_voiceModeManager != null)
                {
                    _voiceModeManager.StateChanged -= OnVoiceModeStateChanged;
                    _voiceModeManager.WakeWordDetected -= OnWakeWordDetected;
                }

                _logger.LogInformation("Google TV voice handler disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Google TV voice handler disposal");
            }
        }
    }

    /// <summary>
    /// Represents the type of voice button pressed.
    /// </summary>
    public enum VoiceButtonType
    {
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
        Media,

        /// <summary>
        /// Unknown or other button type.
        /// </summary>
        Unknown
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
}
