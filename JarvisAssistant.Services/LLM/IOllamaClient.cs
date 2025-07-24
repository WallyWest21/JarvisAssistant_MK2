using JarvisAssistant.Core.Models;

namespace JarvisAssistant.Services.LLM
{
    /// <summary>
    /// Interface for Ollama client operations.
    /// </summary>
    public interface IOllamaClient
    {
        /// <summary>
        /// Generates a complete response from Ollama.
        /// </summary>
        /// <param name="prompt">The prompt to send to the model.</param>
        /// <param name="queryType">The type of query to determine model selection.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The generated response.</returns>
        Task<string> GenerateAsync(string prompt, QueryType queryType = QueryType.General, CancellationToken cancellationToken = default);

        /// <summary>
        /// Streams a response from Ollama as it's generated.
        /// </summary>
        /// <param name="prompt">The prompt to send to the model.</param>
        /// <param name="queryType">The type of query to determine model selection.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>An async enumerable of response chunks.</returns>
        IAsyncEnumerable<string> StreamGenerateAsync(string prompt, QueryType queryType = QueryType.General, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the list of available models from Ollama.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of available model names.</returns>
        Task<List<string>> GetAvailableModelsAsync(CancellationToken cancellationToken = default);
    }
}