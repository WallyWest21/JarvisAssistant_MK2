namespace JarvisAssistant.Core.Interfaces
{
    /// <summary>
    /// Provides methods for voice-related operations including speech generation and recognition.
    /// </summary>
    public interface IVoiceService
    {
        /// <summary>
        /// Generates speech audio from the provided text.
        /// </summary>
        /// <param name="text">The text to convert to speech.</param>
        /// <param name="voiceId">Optional voice identifier to use for speech generation.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the audio data as a byte array.</returns>
        Task<byte[]> GenerateSpeechAsync(string text, string? voiceId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates speech audio from the provided text and streams it as it's generated.
        /// </summary>
        /// <param name="text">The text to convert to speech.</param>
        /// <param name="voiceId">Optional voice identifier to use for speech generation.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>An async enumerable that yields audio chunks as they are generated.</returns>
        IAsyncEnumerable<byte[]> StreamSpeechAsync(string text, string? voiceId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Recognizes speech from audio data and converts it to text.
        /// </summary>
        /// <param name="audioData">The audio data to process for speech recognition.</param>
        /// <param name="language">Optional language code for speech recognition (e.g., "en-US").</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the recognized text.</returns>
        Task<string> RecognizeSpeechAsync(byte[] audioData, string? language = null, CancellationToken cancellationToken = default);
    }
}
