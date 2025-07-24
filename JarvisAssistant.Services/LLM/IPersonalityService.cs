using JarvisAssistant.Core.Models;

namespace JarvisAssistant.Services.LLM
{
    /// <summary>
    /// Interface for personality service operations.
    /// </summary>
    public interface IPersonalityService
    {
        /// <summary>
        /// Formats a response with Jarvis personality based on the query type.
        /// </summary>
        /// <param name="originalResponse">The original response from the LLM.</param>
        /// <param name="queryType">The type of query to determine personality style.</param>
        /// <param name="isStreaming">Whether this is part of a streaming response.</param>
        /// <returns>The formatted response with Jarvis personality.</returns>
        Task<string> FormatResponseAsync(string originalResponse, QueryType queryType, bool isStreaming = false);

        /// <summary>
        /// Gets the system prompt for the specified query type.
        /// </summary>
        /// <param name="queryType">The type of query.</param>
        /// <returns>The appropriate system prompt.</returns>
        string GetSystemPrompt(QueryType queryType);

        /// <summary>
        /// Gets a contextual greeting based on the query type.
        /// </summary>
        /// <param name="queryType">The type of query.</param>
        /// <returns>An appropriate greeting.</returns>
        string GetContextualGreeting(QueryType queryType);

        /// <summary>
        /// Determines if the user should be addressed as "Sir" or "Madam".
        /// </summary>
        /// <param name="context">Optional context to determine appropriate addressing.</param>
        /// <returns>The appropriate form of address.</returns>
        string GetAppropriateAddress(string? context = null);
    }
}