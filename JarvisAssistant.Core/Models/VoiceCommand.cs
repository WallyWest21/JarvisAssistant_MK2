namespace JarvisAssistant.Core.Models
{
    /// <summary>
    /// Represents the source of a voice command.
    /// </summary>
    public enum VoiceCommandSource
    {
        /// <summary>
        /// Command came from wake word detection.
        /// </summary>
        WakeWord,

        /// <summary>
        /// Command came from push-to-talk activation.
        /// </summary>
        PushToTalk,

        /// <summary>
        /// Command came from always-on listening mode.
        /// </summary>
        AlwaysOn,

        /// <summary>
        /// Command came from manual activation.
        /// </summary>
        Manual,

        /// <summary>
        /// Command came from remote control (e.g., Google TV remote).
        /// </summary>
        RemoteControl
    }

    /// <summary>
    /// Represents the classification of a voice command.
    /// </summary>
    public enum VoiceCommandType
    {
        /// <summary>
        /// Command not recognized or classified.
        /// </summary>
        Unknown,

        /// <summary>
        /// Request for system status information.
        /// </summary>
        Status,

        /// <summary>
        /// Request to generate code.
        /// </summary>
        GenerateCode,

        /// <summary>
        /// Request to analyze something.
        /// </summary>
        Analyze,

        /// <summary>
        /// Request to open or navigate to something.
        /// </summary>
        Navigate,

        /// <summary>
        /// Request to search for information.
        /// </summary>
        Search,

        /// <summary>
        /// Request to configure or change settings.
        /// </summary>
        Settings,

        /// <summary>
        /// Request for help or assistance.
        /// </summary>
        Help,

        /// <summary>
        /// Request to stop or cancel current operation.
        /// </summary>
        Stop,

        /// <summary>
        /// Request to exit or close the application.
        /// </summary>
        Exit,

        /// <summary>
        /// Request to repeat last action or response.
        /// </summary>
        Repeat,

        /// <summary>
        /// General conversation or chat request.
        /// </summary>
        Chat
    }

    /// <summary>
    /// Represents a voice command with its metadata and processing information.
    /// </summary>
    public class VoiceCommand
    {
        /// <summary>
        /// Gets or sets the recognized text from the voice input.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the source of the voice command.
        /// </summary>
        public VoiceCommandSource Source { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the command was received.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the confidence level of the speech recognition (0.0 to 1.0).
        /// </summary>
        public float RecognitionConfidence { get; set; }

        /// <summary>
        /// Gets or sets the classified type of the command.
        /// </summary>
        public VoiceCommandType CommandType { get; set; } = VoiceCommandType.Unknown;

        /// <summary>
        /// Gets or sets the confidence level of the command classification (0.0 to 1.0).
        /// </summary>
        public float ClassificationConfidence { get; set; }

        /// <summary>
        /// Gets or sets the extracted parameters from the command.
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();

        /// <summary>
        /// Gets or sets the raw audio data that generated this command.
        /// </summary>
        public byte[]? AudioData { get; set; }

        /// <summary>
        /// Gets or sets the duration of the audio input in milliseconds.
        /// </summary>
        public int AudioDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the language detected in the command.
        /// </summary>
        public string? DetectedLanguage { get; set; }

        /// <summary>
        /// Gets or sets whether this command requires confirmation before execution.
        /// </summary>
        public bool RequiresConfirmation { get; set; }

        /// <summary>
        /// Gets or sets any error that occurred during command processing.
        /// </summary>
        public string? ProcessingError { get; set; }

        /// <summary>
        /// Gets a value indicating whether the command was successfully recognized and classified.
        /// </summary>
        public bool IsValid => !string.IsNullOrWhiteSpace(Text) && 
                              RecognitionConfidence > 0.5f && 
                              CommandType != VoiceCommandType.Unknown;

        /// <summary>
        /// Gets a formatted string representation of the command for logging.
        /// </summary>
        public string ToLogString()
        {
            return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] {Source} -> {CommandType}: \"{Text}\" " +
                   $"(Recognition: {RecognitionConfidence:P1}, Classification: {ClassificationConfidence:P1})";
        }

        /// <summary>
        /// Creates a copy of the voice command with updated processing information.
        /// </summary>
        /// <param name="commandType">The classified command type.</param>
        /// <param name="classificationConfidence">The confidence of the classification.</param>
        /// <param name="parameters">Extracted parameters from the command.</param>
        /// <returns>A new VoiceCommand instance with updated information.</returns>
        public VoiceCommand WithClassification(VoiceCommandType commandType, float classificationConfidence, Dictionary<string, object>? parameters = null)
        {
            return new VoiceCommand
            {
                Text = Text,
                Source = Source,
                Timestamp = Timestamp,
                RecognitionConfidence = RecognitionConfidence,
                CommandType = commandType,
                ClassificationConfidence = classificationConfidence,
                Parameters = parameters ?? new Dictionary<string, object>(),
                AudioData = AudioData,
                AudioDurationMs = AudioDurationMs,
                DetectedLanguage = DetectedLanguage,
                RequiresConfirmation = RequiresConfirmation,
                ProcessingError = ProcessingError
            };
        }
    }

    /// <summary>
    /// Represents the result of processing a voice command.
    /// </summary>
    public class VoiceCommandResult
    {
        /// <summary>
        /// Gets or sets whether the command was successfully processed.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the response message to be spoken back to the user.
        /// </summary>
        public string? Response { get; set; }

        /// <summary>
        /// Gets or sets any data or results from the command execution.
        /// </summary>
        public object? Data { get; set; }

        /// <summary>
        /// Gets or sets any error that occurred during command processing.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets whether the response should be spoken aloud.
        /// </summary>
        public bool ShouldSpeak { get; set; } = true;

        /// <summary>
        /// Gets or sets any follow-up actions that should be performed.
        /// </summary>
        public List<string> FollowUpActions { get; set; } = new();

        /// <summary>
        /// Gets or sets the time taken to process the command in milliseconds.
        /// </summary>
        public int ProcessingTimeMs { get; set; }

        /// <summary>
        /// Creates a successful command result.
        /// </summary>
        /// <param name="response">The response message.</param>
        /// <param name="data">Optional data from the command execution.</param>
        /// <param name="shouldSpeak">Whether the response should be spoken.</param>
        /// <returns>A successful VoiceCommandResult.</returns>
        public static VoiceCommandResult CreateSuccess(string response, object? data = null, bool shouldSpeak = true)
        {
            return new VoiceCommandResult
            {
                Success = true,
                Response = response,
                Data = data,
                ShouldSpeak = shouldSpeak
            };
        }

        /// <summary>
        /// Creates a failed command result.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="shouldSpeak">Whether the error should be spoken.</param>
        /// <returns>A failed VoiceCommandResult.</returns>
        public static VoiceCommandResult CreateError(string errorMessage, bool shouldSpeak = true)
        {
            return new VoiceCommandResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                Response = errorMessage,
                ShouldSpeak = shouldSpeak
            };
        }
    }
}
