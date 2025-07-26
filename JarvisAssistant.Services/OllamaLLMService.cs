using Microsoft.Extensions.Logging;
using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models;
using JarvisAssistant.Services.LLM;

namespace JarvisAssistant.Services
{
    /// <summary>
    /// LLM service implementation using Ollama with Jarvis personality.
    /// </summary>
    public class OllamaLLMService : ILLMService
    {
        private readonly IOllamaClient _ollamaClient;
        private readonly IPersonalityService _personalityService;
        private readonly ILogger<OllamaLLMService> _logger;
        private string _activeModel = "llama3.2:latest";

        public OllamaLLMService(
            IOllamaClient ollamaClient,
            IPersonalityService personalityService,
            ILogger<OllamaLLMService> logger)
        {
            _ollamaClient = ollamaClient ?? throw new ArgumentNullException(nameof(ollamaClient));
            _personalityService = personalityService ?? throw new ArgumentNullException(nameof(personalityService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Sends a message to the LLM service and returns the complete response.
        /// </summary>
        /// <param name="request">The chat request containing the message and context.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the chat response.</returns>
        public async Task<ChatResponse> SendMessageAsync(ChatRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.Message))
                throw new ArgumentException("Message cannot be empty", nameof(request));

            try
            {
                _logger.LogInformation("Processing chat request for conversation: {ConversationId}", request.ConversationId);

                // Determine query type from context or message content
                var queryType = DetermineQueryType(request);
                
                // Get system prompt and prepare the full prompt
                var systemPrompt = _personalityService.GetSystemPrompt(queryType);
                var fullPrompt = $"{systemPrompt}\n\nUser: {request.Message}\nJARVIS:";

                // Generate response from Ollama
                var rawResponse = await _ollamaClient.GenerateAsync(fullPrompt, queryType, cancellationToken);

                if (string.IsNullOrWhiteSpace(rawResponse))
                {
                    _logger.LogWarning("Received empty response from Ollama");
                    rawResponse = "I apologize, Sir, but I seem to be experiencing a temporary processing difficulty.";
                }

                // Apply Jarvis personality formatting
                var formattedResponse = await _personalityService.FormatResponseAsync(rawResponse, queryType, isStreaming: false);

                var response = new ChatResponse(formattedResponse, "assistant")
                {
                    ResponseId = Guid.NewGuid().ToString(),
                    IsComplete = true,
                    Metadata = new Dictionary<string, object>
                    {
                        ["queryType"] = queryType.ToString(),
                        ["model"] = _activeModel,
                        ["conversationId"] = request.ConversationId
                    }
                };

                _logger.LogInformation("Successfully generated response for conversation: {ConversationId}", request.ConversationId);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat request for conversation: {ConversationId}", request.ConversationId);
                
                var errorResponse = new ChatResponse(
                    "I'm afraid I've encountered an unexpected complication, Sir. Please allow me a moment to resolve this matter.",
                    "error")
                {
                    ResponseId = Guid.NewGuid().ToString(),
                    IsComplete = true,
                    Metadata = new Dictionary<string, object>
                    {
                        ["error"] = ex.Message,
                        ["conversationId"] = request.ConversationId
                    }
                };

                return errorResponse;
            }
        }

        /// <summary>
        /// Sends a message to the LLM service and streams the response as it's generated.
        /// </summary>
        /// <param name="request">The chat request containing the message and context.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>An async enumerable that yields partial responses as they are received.</returns>
        public async IAsyncEnumerable<ChatResponse> StreamResponseAsync(ChatRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.Message))
                throw new ArgumentException("Message cannot be empty", nameof(request));

            var responseId = Guid.NewGuid().ToString();
            var fullResponse = new System.Text.StringBuilder();
            var responses = new List<ChatResponse>();
            Exception? streamException = null;

            try
            {
                _logger.LogInformation("Starting streaming response for conversation: {ConversationId}", request.ConversationId);

                // Determine query type from context or message content
                var queryType = DetermineQueryType(request);
                
                // Get system prompt and prepare the full prompt
                var systemPrompt = _personalityService.GetSystemPrompt(queryType);
                var fullPrompt = $"{systemPrompt}\n\nUser: {request.Message}\nJARVIS:";

                // Start with a contextual greeting
                var greeting = _personalityService.GetContextualGreeting(queryType);
                responses.Add(new ChatResponse(greeting, "assistant")
                {
                    ResponseId = responseId,
                    IsComplete = false,
                    Metadata = new Dictionary<string, object>
                    {
                        ["queryType"] = queryType.ToString(),
                        ["model"] = _activeModel,
                        ["conversationId"] = request.ConversationId,
                        ["streamChunk"] = true
                    }
                });

                fullResponse.Append(greeting);

                // Stream the response from Ollama
                await foreach (var chunk in _ollamaClient.StreamGenerateAsync(fullPrompt, queryType, cancellationToken))
                {
                    if (string.IsNullOrEmpty(chunk))
                        continue;

                    // Apply light personality formatting for streaming chunks
                    var formattedChunk = await _personalityService.FormatResponseAsync(chunk, queryType, isStreaming: true);
                    fullResponse.Append(formattedChunk);

                    responses.Add(new ChatResponse(formattedChunk, "assistant")
                    {
                        ResponseId = responseId,
                        IsComplete = false,
                        Metadata = new Dictionary<string, object>
                        {
                            ["queryType"] = queryType.ToString(),
                            ["model"] = _activeModel,
                            ["conversationId"] = request.ConversationId,
                            ["streamChunk"] = true
                        }
                    });
                }

                // Final response with complete formatting
                var finalFormattedResponse = await _personalityService.FormatResponseAsync(fullResponse.ToString(), queryType, isStreaming: false);
                
                responses.Add(new ChatResponse(string.Empty, "assistant")
                {
                    ResponseId = responseId,
                    IsComplete = true,
                    Metadata = new Dictionary<string, object>
                    {
                        ["queryType"] = queryType.ToString(),
                        ["model"] = _activeModel,
                        ["conversationId"] = request.ConversationId,
                        ["finalResponse"] = finalFormattedResponse
                    }
                });

                _logger.LogInformation("Successfully completed streaming response for conversation: {ConversationId}", request.ConversationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during streaming response for conversation: {ConversationId}", request.ConversationId);
                
                streamException = ex;
            }

            // If there was an error, yield an error response instead of normal responses
            if (streamException != null)
            {
                yield return new ChatResponse(
                    "I'm afraid there appears to be a complication with the streaming process, Sir.",
                    "error")
                {
                    ResponseId = responseId,
                    IsComplete = true,
                    Metadata = new Dictionary<string, object>
                    {
                        ["error"] = streamException.Message,
                        ["conversationId"] = request.ConversationId
                    }
                };
            }
            else
            {
                // Yield normal responses only if no error occurred
                foreach (var response in responses)
                {
                    yield return response;
                }
            }
        }

        /// <summary>
        /// Gets the name or identifier of the currently active LLM model.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the active model name.</returns>
        public async Task<string> GetActiveModelAsync()
        {
            try
            {
                var availableModels = await _ollamaClient.GetAvailableModelsAsync();
                
                if (availableModels.Contains(_activeModel))
                {
                    return _activeModel;
                }

                // If current model is not available, return the first available model
                if (availableModels.Count > 0)
                {
                    _activeModel = availableModels[0];
                    _logger.LogInformation("Switched to available model: {Model}", _activeModel);
                    return _activeModel;
                }

                _logger.LogWarning("No models available in Ollama");
                return "No models available";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active model information");
                return $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Determines the query type based on the request content and context.
        /// </summary>
        /// <param name="request">The chat request.</param>
        /// <returns>The determined query type.</returns>
        private QueryType DetermineQueryType(ChatRequest request)
        {
            // Check if query type is explicitly provided in context
            if (request.Context?.TryGetValue("queryType", out var queryTypeObj) == true)
            {
                if (queryTypeObj is string queryTypeString && Enum.TryParse<QueryType>(queryTypeString, true, out var explicitQueryType))
                {
                    return explicitQueryType;
                }
            }

            // Analyze message content for query type hints
            var message = request.Message.ToLowerInvariant();

            // Code-related keywords
            if (ContainsAny(message, "code", "function", "class", "method", "variable", "programming", "syntax", "debug", "compile", "script", "algorithm"))
            {
                return QueryType.Code;
            }

            // Error-related keywords
            if (ContainsAny(message, "error", "exception", "bug", "crash", "fail", "problem", "issue", "fix", "troubleshoot"))
            {
                return QueryType.Error;
            }

            // Technical keywords
            if (ContainsAny(message, "technical", "system", "architecture", "database", "server", "network", "api", "protocol", "configuration"))
            {
                return QueryType.Technical;
            }

            // Mathematical keywords
            if (ContainsAny(message, "calculate", "math", "equation", "formula", "statistics", "probability", "algebra", "geometry"))
            {
                return QueryType.Mathematical;
            }

            // Creative keywords
            if (ContainsAny(message, "creative", "story", "write", "poem", "creative", "artistic", "design", "imagine"))
            {
                return QueryType.Creative;
            }

            // Default to general
            return QueryType.General;
        }

        /// <summary>
        /// Helper method to check if a string contains any of the specified keywords.
        /// </summary>
        /// <param name="text">The text to search.</param>
        /// <param name="keywords">The keywords to search for.</param>
        /// <returns>True if any keyword is found.</returns>
        private static bool ContainsAny(string text, params string[] keywords)
        {
            return keywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }
    }
}
