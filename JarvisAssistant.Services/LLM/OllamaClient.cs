using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using JarvisAssistant.Core.Models;

namespace JarvisAssistant.Services.LLM
{
    /// <summary>
    /// Client for interacting with Ollama API for LLM operations.
    /// </summary>
    public class OllamaClient : IOllamaClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OllamaClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        private static readonly Dictionary<QueryType, string> ModelMapping = new()
        {
            { QueryType.General, "llama3.2" },
            { QueryType.Code, "deepseek-coder" },
            { QueryType.Technical, "llama3.2" },
            { QueryType.Creative, "llama3.2" },
            { QueryType.Mathematical, "llama3.2" },
            { QueryType.Error, "deepseek-coder" }
        };

        public OllamaClient(HttpClient httpClient, ILogger<OllamaClient> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Configure HttpClient with Ollama base URL
            _httpClient.BaseAddress = new Uri("http://100.108.155.28:11434");
            _httpClient.Timeout = TimeSpan.FromMinutes(5);

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
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
            try
            {
                var model = GetModelForQueryType(queryType);
                var request = new
                {
                    model = model,
                    prompt = prompt,
                    stream = false,
                    options = new
                    {
                        temperature = 0.7,
                        top_p = 0.9,
                        max_tokens = 2048
                    }
                };

                var json = JsonSerializer.Serialize(request, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _logger.LogInformation("Sending request to Ollama with model: {Model}", model);

                var response = await _httpClient.PostAsync("/api/generate", content, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Ollama API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                    throw new HttpRequestException($"Ollama API returned {response.StatusCode}: {errorContent}");
                }

                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var ollamaResponse = JsonSerializer.Deserialize<OllamaResponse>(responseJson, _jsonOptions);

                return ollamaResponse?.Response ?? string.Empty;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to connect to Ollama server");
                throw new InvalidOperationException("Unable to connect to Ollama server. Please ensure it's running and accessible.", ex);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Ollama request timed out");
                throw new TimeoutException("The request to Ollama timed out. The model may be taking longer than expected to respond.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during Ollama generation");
                throw;
            }
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
            var model = GetModelForQueryType(queryType);
            var requestData = new
            {
                model = model,
                prompt = prompt,
                stream = true,
                options = new
                {
                    temperature = 0.7,
                    top_p = 0.9,
                    max_tokens = 2048
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
                    _logger.LogError("Ollama API error: {StatusCode} - {Content}", response.StatusCode, errorContent);
                    streamException = new HttpRequestException($"Ollama API returned {response.StatusCode}: {errorContent}");
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
                streamException = new InvalidOperationException("Unable to connect to Ollama server for streaming. Please ensure it's running and accessible.", ex);
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
        private static string GetModelForQueryType(QueryType queryType)
        {
            return ModelMapping.TryGetValue(queryType, out var model) ? model : ModelMapping[QueryType.General];
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
                    _logger.LogError("Failed to get Ollama models: {StatusCode}", response.StatusCode);
                    return new List<string>();
                }

                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var modelsResponse = JsonSerializer.Deserialize<OllamaModelsResponse>(responseJson, _jsonOptions);

                return modelsResponse?.Models?.Select(m => m.Name).ToList() ?? new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Ollama models");
                return new List<string>();
            }
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
