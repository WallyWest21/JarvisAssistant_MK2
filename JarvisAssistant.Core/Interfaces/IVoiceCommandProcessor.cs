using JarvisAssistant.Core.Models;

namespace JarvisAssistant.Core.Interfaces
{
    /// <summary>
    /// Provides voice command classification and processing capabilities.
    /// </summary>
    public interface IVoiceCommandProcessor
    {
        /// <summary>
        /// Event raised when a voice command is received for processing.
        /// </summary>
        event EventHandler<VoiceCommandReceivedEventArgs> CommandReceived;

        /// <summary>
        /// Event raised when a voice command has been processed.
        /// </summary>
        event EventHandler<VoiceCommandProcessedEventArgs> CommandProcessed;

        /// <summary>
        /// Gets a value indicating whether the processor is currently handling commands.
        /// </summary>
        bool IsProcessing { get; }

        /// <summary>
        /// Gets the supported command types for this processor.
        /// </summary>
        IReadOnlyList<VoiceCommandType> SupportedCommands { get; }

        /// <summary>
        /// Classifies a voice command text into a command type with confidence score.
        /// </summary>
        /// <param name="commandText">The recognized text from voice input.</param>
        /// <param name="context">Optional context information for classification.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the classified command.</returns>
        Task<VoiceCommand> ClassifyCommandAsync(string commandText, Dictionary<string, object>? context = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Processes a classified voice command and returns the result.
        /// </summary>
        /// <param name="command">The voice command to process.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the processing result.</returns>
        Task<VoiceCommandResult> ProcessCommandAsync(VoiceCommand command, CancellationToken cancellationToken = default);

        /// <summary>
        /// Processes raw voice command text through classification and execution.
        /// </summary>
        /// <param name="commandText">The recognized text from voice input.</param>
        /// <param name="source">The source of the voice command.</param>
        /// <param name="context">Optional context information.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the processing result.</returns>
        Task<VoiceCommandResult> ProcessTextCommandAsync(string commandText, VoiceCommandSource source, Dictionary<string, object>? context = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Registers a custom command handler for a specific command type.
        /// </summary>
        /// <param name="commandType">The command type to handle.</param>
        /// <param name="handler">The handler function for the command.</param>
        void RegisterCommandHandler(VoiceCommandType commandType, Func<VoiceCommand, CancellationToken, Task<VoiceCommandResult>> handler);

        /// <summary>
        /// Unregisters a command handler for a specific command type.
        /// </summary>
        /// <param name="commandType">The command type to unregister.</param>
        void UnregisterCommandHandler(VoiceCommandType commandType);

        /// <summary>
        /// Gets the available command patterns for a specific command type.
        /// </summary>
        /// <param name="commandType">The command type to get patterns for.</param>
        /// <returns>A list of text patterns that match the command type.</returns>
        IReadOnlyList<string> GetCommandPatterns(VoiceCommandType commandType);

        /// <summary>
        /// Updates the command patterns for a specific command type.
        /// </summary>
        /// <param name="commandType">The command type to update patterns for.</param>
        /// <param name="patterns">The new patterns to use for classification.</param>
        void UpdateCommandPatterns(VoiceCommandType commandType, IEnumerable<string> patterns);

        /// <summary>
        /// Gets processing statistics for voice commands.
        /// </summary>
        /// <returns>A dictionary containing processing statistics.</returns>
        Dictionary<string, object> GetProcessingStatistics();

        /// <summary>
        /// Clears processing statistics and resets counters.
        /// </summary>
        void ClearStatistics();
    }

    /// <summary>
    /// Event arguments for voice command received events.
    /// </summary>
    public class VoiceCommandReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the voice command that was received.
        /// </summary>
        public VoiceCommand Command { get; init; } = new();

        /// <summary>
        /// Gets the timestamp when the command was received.
        /// </summary>
        public DateTime ReceivedAt { get; init; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Event arguments for voice command processed events.
    /// </summary>
    public class VoiceCommandProcessedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the voice command that was processed.
        /// </summary>
        public VoiceCommand Command { get; init; } = new();

        /// <summary>
        /// Gets the result of processing the command.
        /// </summary>
        public VoiceCommandResult Result { get; init; } = new();

        /// <summary>
        /// Gets the timestamp when processing was completed.
        /// </summary>
        public DateTime ProcessedAt { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Gets the time taken to process the command.
        /// </summary>
        public TimeSpan ProcessingTime { get; init; }
    }
}
