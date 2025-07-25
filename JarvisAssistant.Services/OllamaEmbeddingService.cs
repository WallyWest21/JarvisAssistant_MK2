using JarvisAssistant.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace JarvisAssistant.Services
{
    /// <summary>
    /// Embedding service that uses Ollama for generating text embeddings.
    /// </summary>
    public class OllamaEmbeddingService : IEmbeddingService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OllamaEmbeddingService> _logger;
        private readonly string _baseUrl;
        private readonly string _modelName;

        /// <summary>
        /// Initializes a new instance of the <see cref="OllamaEmbeddingService"/> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client for making requests.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="baseUrl">The Ollama base URL (default: http://localhost:11434).</param>
        /// <param name="modelName">The embedding model name (default: nomic-embed-text).</param>
        public OllamaEmbeddingService(
            HttpClient httpClient, 
            ILogger<OllamaEmbeddingService> logger,
            string baseUrl = "http://localhost:11434",
            string modelName = "nomic-embed-text")
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _baseUrl = baseUrl.TrimEnd('/');
            _modelName = modelName;

            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.Timeout = TimeSpan.FromMinutes(5); // Embeddings can take time for large batches
        }

        /// <inheritdoc/>
        public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("Empty text provided for embedding generation");
                return Array.Empty<float>();
            }

            try
            {
                _logger.LogDebug("Generating embedding for text of length {Length}", text.Length);

                var request = new
                {
                    model = _modelName,
                    prompt = text
                };

                var jsonContent = JsonSerializer.Serialize(request);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/embeddings", httpContent, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Failed to generate embedding. Status: {StatusCode}, Error: {Error}", 
                        response.StatusCode, errorContent);
                    throw new HttpRequestException($"Embedding request failed: {response.StatusCode}");
                }

                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var embeddingResponse = JsonSerializer.Deserialize<EmbeddingResponse>(responseContent);

                if (embeddingResponse?.Embedding == null)
                {
                    _logger.LogError("Received null embedding from Ollama");
                    throw new InvalidOperationException("Received null embedding from Ollama");
                }

                _logger.LogDebug("Successfully generated embedding with {Dimensions} dimensions", 
                    embeddingResponse.Embedding.Length);

                return embeddingResponse.Embedding;
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                _logger.LogError(ex, "Failed to generate embedding for text");
                throw new InvalidOperationException($"Failed to generate embedding: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<float[][]> GenerateEmbeddingsBatchAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
        {
            var textList = texts?.ToList();
            if (textList == null || !textList.Any())
            {
                _logger.LogWarning("Empty text list provided for batch embedding generation");
                return Array.Empty<float[]>();
            }

            _logger.LogInformation("Generating embeddings for batch of {Count} texts", textList.Count);

            var embeddings = new List<float[]>();
            var semaphore = new SemaphoreSlim(5, 5); // Limit concurrent requests to avoid overwhelming Ollama

            var tasks = textList.Select(async text =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    return await GenerateEmbeddingAsync(text, cancellationToken);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var results = await Task.WhenAll(tasks);
            
            _logger.LogInformation("Successfully generated {Count} embeddings in batch", results.Length);
            return results;
        }

        /// <inheritdoc/>
        public float CalculateSimilarity(float[] embedding1, float[] embedding2)
        {
            if (embedding1 == null || embedding2 == null)
                throw new ArgumentNullException("Embeddings cannot be null");

            if (embedding1.Length != embedding2.Length)
                throw new ArgumentException("Embeddings must have the same dimensions");

            if (embedding1.Length == 0)
                return 0f;

            // Calculate cosine similarity
            double dotProduct = 0;
            double magnitude1 = 0;
            double magnitude2 = 0;

            for (int i = 0; i < embedding1.Length; i++)
            {
                dotProduct += embedding1[i] * embedding2[i];
                magnitude1 += embedding1[i] * embedding1[i];
                magnitude2 += embedding2[i] * embedding2[i];
            }

            if (magnitude1 == 0 || magnitude2 == 0)
                return 0f;

            var similarity = dotProduct / (Math.Sqrt(magnitude1) * Math.Sqrt(magnitude2));
            return (float)Math.Max(-1, Math.Min(1, similarity)); // Clamp to [-1, 1]
        }

        /// <inheritdoc/>
        public int GetEmbeddingDimensions()
        {
            // The nomic-embed-text model typically returns 768-dimensional embeddings
            // This should be configured based on the actual model being used
            return 768;
        }

        /// <inheritdoc/>
        public string GetModelName()
        {
            return _modelName;
        }

        /// <summary>
        /// Checks if the Ollama service is available and the embedding model is loaded.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates if the service is available.</returns>
        public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if Ollama is running
                var response = await _httpClient.GetAsync("/api/tags", cancellationToken);
                if (!response.IsSuccessStatusCode)
                    return false;

                // Check if our embedding model is available
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var modelsResponse = JsonSerializer.Deserialize<ModelsResponse>(content);
                
                var modelExists = modelsResponse?.Models?.Any(m => 
                    m.Name.StartsWith(_modelName, StringComparison.OrdinalIgnoreCase)) == true;

                if (!modelExists)
                {
                    _logger.LogWarning("Embedding model {ModelName} is not available in Ollama", _modelName);
                }

                return modelExists;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check Ollama availability");
                return false;
            }
        }

        /// <summary>
        /// Attempts to pull the embedding model if it's not available.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates if the model was successfully pulled.</returns>
        public async Task<bool> TryPullModelAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Attempting to pull embedding model {ModelName}", _modelName);

                var request = new { name = _modelName };
                var jsonContent = JsonSerializer.Serialize(request);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("/api/pull", httpContent, cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Successfully initiated pull for model {ModelName}", _modelName);
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Failed to pull model {ModelName}. Status: {StatusCode}, Error: {Error}", 
                        _modelName, response.StatusCode, errorContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to pull embedding model {ModelName}", _modelName);
                return false;
            }
        }

        #region Response Models

        private class EmbeddingResponse
        {
            public float[]? Embedding { get; set; }
        }

        private class ModelsResponse
        {
            public List<ModelInfo>? Models { get; set; }
        }

        private class ModelInfo
        {
            public string Name { get; set; } = string.Empty;
            public string Size { get; set; } = string.Empty;
            public DateTime ModifiedAt { get; set; }
        }

        #endregion
    }
}
