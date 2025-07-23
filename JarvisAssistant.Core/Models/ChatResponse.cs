namespace JarvisAssistant.Core.Models
{
    /// <summary>
    /// Represents a response from an LLM service.
    /// </summary>
    public class ChatResponse
    {
        /// <summary>
        /// Gets or sets the unique identifier for this response.
        /// </summary>
        public string ResponseId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Gets or sets the response message content.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of the response (e.g., "assistant", "error", "partial").
        /// </summary>
        public string Type { get; set; } = "assistant";

        /// <summary>
        /// Gets or sets additional metadata associated with the response.
        /// </summary>
        public Dictionary<string, object>? Metadata { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the response was generated.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets or sets a value indicating whether this is a complete response or a partial/streaming response.
        /// </summary>
        public bool IsComplete { get; set; } = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatResponse"/> class.
        /// </summary>
        public ChatResponse()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatResponse"/> class with the specified message.
        /// </summary>
        /// <param name="message">The response message.</param>
        /// <param name="type">The response type.</param>
        public ChatResponse(string message, string type = "assistant")
        {
            Message = message;
            Type = type;
        }
    }
}
