using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace JarvisAssistant.Services
{
    /// <summary>
    /// Fallback LLM service for when Ollama is not available.
    /// Provides appropriate offline responses.
    /// </summary>
    public class FallbackLLMService : ILLMService
    {
        private readonly ILogger<FallbackLLMService>? _logger;

        public FallbackLLMService(ILogger<FallbackLLMService>? logger = null)
        {
            _logger = logger;
        }

        public Task<ChatResponse> SendMessageAsync(ChatRequest request, CancellationToken cancellationToken = default)
        {
            _logger?.LogWarning("FallbackLLMService called - Ollama service not available");

            var response = new ChatResponse
            {
                Message = "I apologize, but my language processing services are currently offline. " +
                         "This may be because:\n\n" +
                         "• Ollama is not running\n" +
                         "• Network connectivity issues\n" +
                         "• Service configuration problems\n\n" +
                         "Please check your Ollama installation and try again.",
                Type = "error",
                Timestamp = DateTimeOffset.UtcNow,
                IsComplete = true
            };

            return Task.FromResult(response);
        }

        public async IAsyncEnumerable<ChatResponse> StreamResponseAsync(ChatRequest request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            _logger?.LogWarning("FallbackLLMService streaming called - Ollama service not available");

            var response = new ChatResponse
            {
                Message = "I apologize, but my language processing services are currently offline. " +
                         "Please check your Ollama installation and try again.",
                Type = "error",
                Timestamp = DateTimeOffset.UtcNow,
                IsComplete = true
            };

            yield return response;
            await Task.CompletedTask;
        }

        public Task<string> GetActiveModelAsync()
        {
            return Task.FromResult("offline");
        }
    }
}
