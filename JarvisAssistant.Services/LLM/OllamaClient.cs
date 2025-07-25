using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using JarvisAssistant.Core.Models;
using JarvisAssistant.Services.Extensions;

namespace JarvisAssistant.Services.LLM
{
    /// <summary>
    /// Client for interacting with Ollama API for LLM operations.
    /// </summary>
    public class OllamaClient : IOllamaClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OllamaClient> _logger;
        private readonly IOptions<OllamaLLMOptions> _options;
        private readonly JsonSerializerOptions _jsonOptions;

        private static readonly Dictionary<QueryType, string> ModelMapping = new()
        {
            { QueryType.General, "llama3.2:latest" },
            { QueryType.Code, "deepseek-coder:latest" },
            { QueryType.Technical, "llama3.2:latest" },
            { QueryType.Creative, "llama3.2:latest" },
            { QueryType.Mathematical, "llama3.2:latest" },
            { QueryType.Error, "deepseek-coder:latest" }
        };

        public OllamaClient(HttpClient httpClient, ILogger<OllamaClient> logger, IOptions<OllamaLLMOptions>? options = null)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? Microsoft.Extensions.Options.Options.Create(new OllamaLLMOptions());

            // Configure HttpClient with options only if not already configured
            // Check if BaseAddress and Timeout are still at their default values
            try
            {
                if (_httpClient.BaseAddress == null)
                {
                    _httpClient.BaseAddress = new Uri(_options.Value.BaseUrl);
                }
                
                if (_httpClient.Timeout == TimeSpan.FromSeconds(100)) // Default HttpClient timeout
                {
                    _httpClient.Timeout = _options.Value.Timeout;
                }
            }
            catch (InvalidOperationException)
            {
                // HttpClient has already been used, skip configuration
                // This can happen in test scenarios where the HttpClient is pre-configured
            }

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            _logger.LogInformation("OllamaClient initialized with base URL: {BaseUrl}", _httpClient.BaseAddress);
        }

        /// <summary>
        /// Generates a complete response from Ollama.
        /// </summary>
        /// <param name="prompt">The prompt to send to the model.</param>
        /// <param name="queryType">The type of query to determine model selection.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The generated response.</returns>
        public async Task<string> GenerateAsync(string prompt, QueryType queryType = QueryType.General, CancellationToken cancellationToken = default)
        {
            var retryCount = 0;
            var maxRetries = _options.Value.MaxRetryAttempts;

            while (retryCount <= maxRetries)
            {
                try
                {
                    var model = GetModelForQueryType(queryType, _options.Value);
                    var request = new
                    {
                        model = model,
                        prompt = prompt,
                        stream = false,
                        options = new
                        {
                            temperature = _options.Value.Temperature,
                            top_p = _options.Value.TopP,
                            max_tokens = _options.Value.MaxTokens
                        }
                    };

                    var json = JsonSerializer.Serialize(request, _jsonOptions);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    _logger.LogInformation("Sending request to Ollama with model: {Model} (attempt {Attempt}/{MaxAttempts})", 
                        model, retryCount + 1, maxRetries + 1);

                    var response = await _httpClient.PostAsync("/api/generate", content, cancellationToken);
                    
                    if (!response.IsSuccessStatusCode)
                    {
                        var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                        
                        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                        {
                            _logger.LogError("Ollama API endpoint not found (404). Please check if Ollama is running and accessible at: {BaseUrl}", _httpClient.BaseAddress);
                            throw new InvalidOperationException($"Ollama service not found at {_httpClient.BaseAddress}. Please ensure Ollama is running and accessible. Error: HTTP 404 - {errorContent}");
                        }
                        
                        _logger.LogError("Ollama API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                        throw new InvalidOperationException($"Ollama API returned {response.StatusCode}: {errorContent}");
                    }

                    var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                    
                    try
                    {
                        var ollamaResponse = JsonSerializer.Deserialize<OllamaResponse>(responseJson, _jsonOptions);
                        _logger.LogInformation("Successfully received response from Ollama");
                        return ollamaResponse?.Response ?? string.Empty;
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "Failed to parse JSON response from Ollama: {ResponseContent}", responseJson);
                        throw new JsonException($"Invalid JSON response from Ollama server: {ex.Message}", ex);
                    }
                }
                catch (HttpRequestException ex) when (ex.Message.Contains("404") || ex.Message.Contains("connection") || ex.Message.Contains("refused"))
                {
                    if (retryCount < maxRetries && await TryAlternativeEndpoint(retryCount))
                    {
                        retryCount++;
                        _logger.LogWarning("Retrying with alternative endpoint (attempt {Attempt}/{MaxAttempts})", retryCount + 1, maxRetries + 1);
                        await Task.Delay(_options.Value.RetryDelay, cancellationToken);
                        continue;
                    }

                    _logger.LogError(ex, "Failed to connect to Ollama server after all retry attempts");
                    throw new InvalidOperationException($"Unable to connect to Ollama server. Please ensure Ollama is running and accessible. Tried {retryCount + 1} attempts. Last error: {ex.Message}", ex);
                }
                catch (HttpRequestException ex)
                {
                    // Handle other HTTP request exceptions as connection errors for testing compatibility
                    _logger.LogError(ex, "HTTP request error during Ollama generation");
                    throw new InvalidOperationException($"Unable to connect to Ollama server. Please ensure Ollama is running and accessible. Error: {ex.Message}", ex);
                }
                catch (TaskCanceledException ex) when (ContainsTimeoutException(ex))
                {
                    _logger.LogError(ex, "Ollama request timed out");
                    throw new TimeoutException("The request to Ollama timed out. The model may be taking longer than expected to respond.", ex);
                }
                catch (TaskCanceledException ex) when (ex.CancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Ollama request was cancelled");
                    throw new OperationCanceledException("Operation was canceled", ex, ex.CancellationToken);
                }
                catch (TaskCanceledException ex)
                {
                    // Generic TaskCanceledException - treat as timeout if no specific cancellation token
                    _logger.LogError(ex, "Ollama request was cancelled (likely timeout)");
                    throw new TimeoutException("The request to Ollama timed out. The model may be taking longer than expected to respond.", ex);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error during Ollama generation");
                    throw;
                }
            }

            throw new InvalidOperationException("Maximum retry attempts exceeded");
        }

        /// <summary>
        /// Streams a response from Ollama as it's generated.
        /// </summary>
        /// <param name="prompt">The prompt to send to the model.</param>
        /// <param name="queryType">The type of query to determine model selection.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>An async enumerable of response chunks.</returns>
        public async IAsyncEnumerable<string> StreamGenerateAsync(string prompt, QueryType queryType = QueryType.General, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var model = GetModelForQueryType(queryType, _options.Value);
            var requestData = new
            {
                model = model,
                prompt = prompt,
                stream = true,
                options = new
                {
                    temperature = _options.Value.Temperature,
                    top_p = _options.Value.TopP,
                    max_tokens = _options.Value.MaxTokens
                }
            };

            var json = JsonSerializer.Serialize(requestData, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Starting streaming request to Ollama with model: {Model}", model);

            // Get the stream chunks first, then yield them
            var chunks = new List<string>();
            
            HttpResponseMessage? response = null;
            Stream? stream = null;
            StreamReader? reader = null;
            Exception? streamException = null;

            try
            {
                var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/generate")
                {
                    Content = content
                };

                response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        _logger.LogError("Ollama API endpoint not found (404) during streaming. Please check if Ollama is running at: {BaseUrl}", _httpClient.BaseAddress);
                        streamException = new InvalidOperationException($"Ollama service not found at {_httpClient.BaseAddress}. Please ensure Ollama is running and accessible. Error: HTTP 404 - {errorContent}");
                    }
                    else
                    {
                        _logger.LogError("Ollama API error during streaming: {StatusCode} - {Content}", response.StatusCode, errorContent);
                        streamException = new HttpRequestException($"Ollama API returned {response.StatusCode}: {errorContent}");
                    }
                }
                else
                {
                    stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                    reader = new StreamReader(stream);

                    string? line;
                    while ((line = await reader.ReadLineAsync()) != null && !cancellationToken.IsCancellationRequested)
                    {
                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        var chunkResult = ParseStreamChunk(line);
                        if (chunkResult.Success && chunkResult.Response != null)
                        {
                            chunks.Add(chunkResult.Response);
                        }

                        // Check if this is the final chunk
                        if (chunkResult.IsDone)
                        {
                            _logger.LogInformation("Ollama streaming completed");
                            break;
                        }
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to connect to Ollama server during streaming");
                streamException = new InvalidOperationException($"Unable to connect to Ollama server for streaming. Please ensure Ollama is running and accessible at {_httpClient.BaseAddress}. Error: {ex.Message}", ex);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Ollama streaming request timed out");
                streamException = new TimeoutException("The streaming request to Ollama timed out.", ex);
            }
            finally
            {
                reader?.Dispose();
                stream?.Dispose();
                response?.Dispose();
            }

            // Now yield the chunks (outside of try-catch)
            if (streamException != null)
            {
                throw streamException;
            }

            foreach (var chunk in chunks)
            {
                yield return chunk;
            }
        }

        /// <summary>
        /// Tries to switch to an alternative endpoint if available.
        /// </summary>
        /// <param name="attemptNumber">The current attempt number.</param>
        /// <returns>True if an alternative endpoint was set.</returns>
        private Task<bool> TryAlternativeEndpoint(int attemptNumber)
        {
            var alternatives = _options.Value.AlternativeEndpoints;
            if (attemptNumber < alternatives.Count)
            {
                var alternativeUrl = alternatives[attemptNumber];
                _logger.LogInformation("Trying alternative endpoint: {Endpoint}", alternativeUrl);
                
                try
                {
                    _httpClient.BaseAddress = new Uri(alternativeUrl);
                    return Task.FromResult(true);
                }
                catch (InvalidOperationException)
                {
                    // HttpClient has already been used, can't change BaseAddress
                    // This can happen in test scenarios or when the client has been used before
                    _logger.LogWarning("Cannot switch to alternative endpoint {Endpoint} - HttpClient already in use", alternativeUrl);
                    return Task.FromResult(false);
                }
            }

            return Task.FromResult(false);
        }

        /// <summary>
        /// Parses a streaming chunk from Ollama.
        /// </summary>
        /// <param name="line">The JSON line to parse.</param>
        /// <returns>The parsed chunk result.</returns>
        private StreamChunkResult ParseStreamChunk(string line)
        {
            try
            {
                var chunk = JsonSerializer.Deserialize<OllamaStreamResponse>(line, _jsonOptions);
                return new StreamChunkResult
                {
                    Success = true,
                    Response = chunk?.Response,
                    IsDone = chunk?.Done ?? false
                };
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to parse Ollama stream chunk: {Line}", line);
                return new StreamChunkResult { Success = false };
            }
        }

        /// <summary>
        /// Gets the appropriate model name for the given query type.
        /// </summary>
        /// <param name="queryType">The type of query.</param>
        /// <returns>The model name to use.</returns>
        private static string GetModelForQueryType(QueryType queryType, OllamaLLMOptions options)
        {
            // Use configured models if available
            return queryType switch
            {
                QueryType.Code => options.CodeModel,
                QueryType.Error => options.CodeModel,
                _ => options.DefaultModel
            };
        }

        /// <summary>
        /// Gets the list of available models from Ollama.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of available model names.</returns>
        public async Task<List<string>> GetAvailableModelsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/tags", cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        _logger.LogError("Ollama models endpoint not found (404). Service may not be running at: {BaseUrl}", _httpClient.BaseAddress);
                    }
                    else
                    {
                        _logger.LogError("Failed to get Ollama models: {StatusCode}", response.StatusCode);
                    }
                    return new List<string>();
                }

                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var modelsResponse = JsonSerializer.Deserialize<OllamaModelsResponse>(responseJson, _jsonOptions);

                return modelsResponse?.Models?.Select(m => m.Name).ToList() ?? new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Ollama models from {BaseUrl}", _httpClient.BaseAddress);
                return new List<string>();
            }
        }

        /// <summary>
        /// Checks if an exception or its inner exceptions contain a TimeoutException.
        /// </summary>
        private static bool ContainsTimeoutException(Exception ex)
        {
            var current = ex;
            while (current != null)
            {
                if (current is TimeoutException)
                    return true;
                current = current.InnerException;
            }
            return false;
        }
    }

    // Response DTOs for Ollama API
    internal class OllamaResponse
    {
        public string? Response { get; set; }
        public bool Done { get; set; }
    }

    internal class OllamaStreamResponse
    {
        public string? Response { get; set; }
        public bool Done { get; set; }
    }

    internal class OllamaModelsResponse
    {
        public List<OllamaModel>? Models { get; set; }
    }

    internal class OllamaModel
    {
        public string Name { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string Digest { get; set; } = string.Empty;
    }

    // Helper class for stream chunk parsing
    internal class StreamChunkResult
    {
        public bool Success { get; set; }
        public string? Response { get; set; }
        public bool IsDone { get; set; }
    }
}
