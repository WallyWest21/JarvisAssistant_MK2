namespace JarvisAssistant.Core.Interfaces
{
    /// <summary>
    /// Represents the different voice activation modes available.
    /// </summary>
    public enum VoiceActivationMode
    {
        /// <summary>
        /// Voice mode is always active (used for TV platforms).
        /// </summary>
        AlwaysOn,

        /// <summary>
        /// Voice mode can be toggled on/off by user (used for mobile/desktop).
        /// </summary>
        Toggle,

        /// <summary>
        /// Voice mode is activated by push-to-talk button.
        /// </summary>
        PushToTalk,

        /// <summary>
        /// Voice mode is disabled for this platform.
        /// </summary>
        Disabled
    }

    /// <summary>
    /// Represents the current state of voice mode.
    /// </summary>
    public enum VoiceModeState
    {
        /// <summary>
        /// Voice mode is inactive and not listening.
        /// </summary>
        Inactive,

        /// <summary>
        /// Voice mode is active and listening for wake word.
        /// </summary>
        Listening,

        /// <summary>
        /// Voice mode detected wake word and is processing commands.
        /// </summary>
        Processing,

        /// <summary>
        /// Voice mode encountered an error.
        /// </summary>
        Error
    }

    /// <summary>
    /// Provides platform-specific voice mode management capabilities.
    /// </summary>
    public interface IVoiceModeManager
    {
        /// <summary>
        /// Gets a value indicating whether voice mode is currently active.
        /// </summary>
        /// <value>True if voice mode is active and listening, false otherwise.</value>
        bool IsVoiceModeActive { get; }

        /// <summary>
        /// Gets a value indicating whether the user can toggle voice mode on/off.
        /// </summary>
        /// <value>True if voice mode can be toggled, false if it's always on or disabled.</value>
        bool CanToggleVoiceMode { get; }

        /// <summary>
        /// Gets the current voice activation mode for the platform.
        /// </summary>
        /// <value>The activation mode configured for the current platform.</value>
        VoiceActivationMode ActivationMode { get; }

        /// <summary>
        /// Gets the current state of voice mode.
        /// </summary>
        /// <value>The current voice mode state.</value>
        VoiceModeState CurrentState { get; }

        /// <summary>
        /// Gets a value indicating whether wake word detection is enabled.
        /// </summary>
        /// <value>True if wake word detection is active, false otherwise.</value>
        bool IsWakeWordDetectionEnabled { get; }

        /// <summary>
        /// Event raised when voice mode state changes.
        /// </summary>
        event EventHandler<VoiceModeStateChangedEventArgs> StateChanged;

        /// <summary>
        /// Event raised when wake word is detected.
        /// </summary>
        event EventHandler<WakeWordDetectedEventArgs> WakeWordDetected;

        /// <summary>
        /// Event raised when voice activity is detected.
        /// </summary>
        event EventHandler<VoiceActivityDetectedEventArgs> VoiceActivityDetected;

        /// <summary>
        /// Enables voice mode and starts listening for wake words or commands.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates success.</returns>
        Task<bool> EnableVoiceModeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Disables voice mode and stops all voice listening activities.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates success.</returns>
        Task<bool> DisableVoiceModeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Toggles voice mode on or off based on current state.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates the new state.</returns>
        Task<bool> ToggleVoiceModeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Starts listening for a single voice command (push-to-talk mode).
        /// </summary>
        /// <param name="timeout">Maximum time to wait for voice input.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the recognized text.</returns>
        Task<string?> ListenForCommandAsync(TimeSpan timeout, CancellationToken cancellationToken = default);

        /// <summary>
        /// Configures wake word detection settings.
        /// </summary>
        /// <param name="enabled">Whether to enable wake word detection.</param>
        /// <param name="sensitivity">Detection sensitivity (0.0 to 1.0).</param>
        /// <param name="wakeWords">Array of wake words to listen for.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task ConfigureWakeWordDetectionAsync(bool enabled, float sensitivity = 0.7f, string[]? wakeWords = null);

        /// <summary>
        /// Gets the current audio level for voice activity detection.
        /// </summary>
        /// <returns>Audio level between 0.0 and 1.0.</returns>
        float GetCurrentAudioLevel();
    }

    /// <summary>
    /// Event arguments for voice mode state changes.
    /// </summary>
    public class VoiceModeStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the previous voice mode state.
        /// </summary>
        public VoiceModeState PreviousState { get; init; }

        /// <summary>
        /// Gets the new voice mode state.
        /// </summary>
        public VoiceModeState NewState { get; init; }

        /// <summary>
        /// Gets the timestamp when the state change occurred.
        /// </summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Gets optional error information if the state change was due to an error.
        /// </summary>
        public string? ErrorMessage { get; init; }
    }

    /// <summary>
    /// Event arguments for wake word detection.
    /// </summary>
    public class WakeWordDetectedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the detected wake word.
        /// </summary>
        public string WakeWord { get; init; } = string.Empty;

        /// <summary>
        /// Gets the confidence level of the detection (0.0 to 1.0).
        /// </summary>
        public float Confidence { get; init; }

        /// <summary>
        /// Gets the timestamp when the wake word was detected.
        /// </summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Gets the audio data that contained the wake word.
        /// </summary>
        public byte[]? AudioData { get; init; }
    }

    /// <summary>
    /// Event arguments for voice activity detection.
    /// </summary>
    public class VoiceActivityDetectedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets a value indicating whether voice activity is currently detected.
        /// </summary>
        public bool IsActive { get; init; }

        /// <summary>
        /// Gets the current audio level (0.0 to 1.0).
        /// </summary>
        public float AudioLevel { get; init; }

        /// <summary>
        /// Gets the timestamp of the voice activity detection.
        /// </summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    }
}
