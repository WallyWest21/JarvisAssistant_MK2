namespace JarvisAssistant.Core.Models
{
    /// <summary>
    /// Represents a request to send a chat message to an LLM service.
    /// </summary>
    public class ChatRequest
    {
        /// <summary>
        /// Gets or sets the message content to be sent.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of the message (e.g., "user", "system", "assistant").
        /// </summary>
        public string Type { get; set; } = "user";

        /// <summary>
        /// Gets or sets the unique identifier for the conversation this message belongs to.
        /// </summary>
        public string ConversationId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional context or metadata for the request.
        /// </summary>
        public Dictionary<string, object>? Context { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatRequest"/> class.
        /// </summary>
        public ChatRequest()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatRequest"/> class with the specified message.
        /// </summary>
        /// <param name="message">The message content.</param>
        /// <param name="conversationId">The conversation identifier.</param>
        public ChatRequest(string message, string conversationId = "")
        {
            Message = message;
            ConversationId = conversationId;
        }
    }
}
