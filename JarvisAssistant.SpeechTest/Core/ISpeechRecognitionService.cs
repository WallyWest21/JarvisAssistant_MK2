using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace JarvisAssistant.SpeechTest.Core
{
    /// <summary>
    /// Interface for cross-platform speech recognition service
    /// </summary>
    public interface ISpeechRecognitionService
    {
        /// <summary>
        /// Indicates if speech recognition is currently active
        /// </summary>
        bool IsListening { get; }

        /// <summary>
        /// Indicates if speech recognition is available on the current platform
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Event raised when speech is recognized
        /// </summary>
        event EventHandler<SpeechRecognitionResult>? SpeechRecognized;

        /// <summary>
        /// Event raised when partial speech results are available
        /// </summary>
        event EventHandler<string>? PartialResultsReceived;

        /// <summary>
        /// Event raised when speech recognition state changes
        /// </summary>
        event EventHandler<SpeechRecognitionState>? StateChanged;

        /// <summary>
        /// Starts continuous speech recognition
        /// </summary>
        Task<bool> StartListeningAsync(SpeechRecognitionOptions? options = null);

        /// <summary>
        /// Stops speech recognition
        /// </summary>
        Task StopListeningAsync();

        /// <summary>
        /// Recognizes speech for a single utterance
        /// </summary>
        Task<SpeechRecognitionResult> RecognizeSpeechAsync(SpeechRecognitionOptions? options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Requests necessary permissions for speech recognition
        /// </summary>
        Task<PermissionStatus> RequestPermissionsAsync();

        /// <summary>
        /// Gets available languages for speech recognition
        /// </summary>
        Task<IEnumerable<string>> GetAvailableLanguagesAsync();

        /// <summary>
        /// Runs diagnostic tests
        /// </summary>
        Task<DiagnosticResult> RunDiagnosticsAsync();
    }

    /// <summary>
    /// Speech recognition result
    /// </summary>
    public class SpeechRecognitionResult
    {
        public string Text { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public bool IsFinal { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public TimeSpan Duration { get; set; }
        public List<SpeechRecognitionAlternative> Alternatives { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Alternative recognition results
    /// </summary>
    public class SpeechRecognitionAlternative
    {
        public string Text { get; set; } = string.Empty;
        public float Confidence { get; set; }
    }

    /// <summary>
    /// Speech recognition options
    /// </summary>
    public class SpeechRecognitionOptions
    {
        public string Language { get; set; } = "en-US";
        public int MaxAlternatives { get; set; } = 3;
        public bool EnablePartialResults { get; set; } = true;
        public bool EnableProfanityFilter { get; set; } = true;
        public TimeSpan? MaxListeningTime { get; set; }
        public TimeSpan SilenceTimeout { get; set; } = TimeSpan.FromSeconds(2);
        public bool ContinuousRecognition { get; set; } = false;
        public Dictionary<string, object> PlatformSpecificOptions { get; set; } = new();
    }

    /// <summary>
    /// Speech recognition state
    /// </summary>
    public enum SpeechRecognitionState
    {
        Idle,
        Starting,
        Listening,
        Processing,
        Stopping,
        Error
    }

    /// <summary>
    /// Permission status
    /// </summary>
    public enum PermissionStatus
    {
        Unknown,
        Denied,
        Disabled,
        Granted,
        Restricted
    }

    /// <summary>
    /// Diagnostic result for troubleshooting
    /// </summary>
    public class DiagnosticResult
    {
        public bool IsAvailable { get; set; }
        public PermissionStatus PermissionStatus { get; set; }
        public List<string> AvailableLanguages { get; set; } = new();
        public Dictionary<string, string> SystemInfo { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<string> Info { get; set; } = new();
        public DateTime TestTimestamp { get; set; } = DateTime.UtcNow;
    }
}
