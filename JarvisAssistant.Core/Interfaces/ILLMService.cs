using JarvisAssistant.Core.Models;

namespace JarvisAssistant.Core.Interfaces
{
    /// <summary>
    /// Provides methods for interacting with Large Language Model services.
    /// </summary>
    public interface ILLMService
    {
        /// <summary>
        /// Sends a message to the LLM service and returns the complete response.
        /// </summary>
        /// <param name="request">The chat request containing the message and context.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the chat response.</returns>
        Task<ChatResponse> SendMessageAsync(ChatRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a message to the LLM service and streams the response as it's generated.
        /// </summary>
        /// <param name="request">The chat request containing the message and context.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>An async enumerable that yields partial responses as they are received.</returns>
        IAsyncEnumerable<ChatResponse> StreamResponseAsync(ChatRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the name or identifier of the currently active LLM model.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the active model name.</returns>
        Task<string> GetActiveModelAsync();
    }
}
