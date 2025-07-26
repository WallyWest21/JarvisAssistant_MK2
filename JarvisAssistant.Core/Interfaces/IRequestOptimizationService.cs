using JarvisAssistant.Core.Models;

namespace JarvisAssistant.Core.Interfaces
{
    /// <summary>
    /// Provides methods for intelligent request batching and optimization.
    /// </summary>
    public interface IRequestOptimizationService
    {
        /// <summary>
        /// Batches similar requests together for efficient processing.
        /// </summary>
        /// <param name="requests">Collection of requests to batch.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Batched processing results.</returns>
        Task<BatchProcessingResult> BatchRequestsAsync(IEnumerable<OptimizedRequest> requests, CancellationToken cancellationToken = default);

        /// <summary>
        /// Optimizes a single request based on context and performance settings.
        /// </summary>
        /// <param name="request">Request to optimize.</param>
        /// <param name="context">Optimization context.</param>
        /// <returns>Optimized request with applied settings.</returns>
        Task<OptimizedRequest> OptimizeRequestAsync(ChatRequest request, OptimizationContext context);

        /// <summary>
        /// Caches response for similar future requests.
        /// </summary>
        /// <param name="request">Original request.</param>
        /// <param name="response">Response to cache.</param>
        /// <param name="cacheSettings">Cache configuration.</param>
        Task CacheResponseAsync(ChatRequest request, ChatResponse response, ResponseCacheSettings cacheSettings);

        /// <summary>
        /// Attempts to retrieve cached response for a request.
        /// </summary>
        /// <param name="request">Request to check cache for.</param>
        /// <returns>Cached response if available, null otherwise.</returns>
        Task<ChatResponse?> GetCachedResponseAsync(ChatRequest request);

        /// <summary>
        /// Compresses network traffic for large requests/responses.
        /// </summary>
        /// <param name="data">Data to compress.</param>
        /// <returns>Compressed data with metadata.</returns>
        Task<CompressedData> CompressDataAsync(byte[] data);

        /// <summary>
        /// Decompresses network traffic.
        /// </summary>
        /// <param name="compressedData">Compressed data to decompress.</param>
        /// <returns>Original data.</returns>
        Task<byte[]> DecompressDataAsync(CompressedData compressedData);

        /// <summary>
        /// Optimizes embedding generation for batch processing.
        /// </summary>
        /// <param name="texts">Texts to generate embeddings for.</param>
        /// <param name="batchSize">Optimal batch size.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Generated embeddings with performance metrics.</returns>
        Task<EmbeddingBatchResult> OptimizeEmbeddingGenerationAsync(IEnumerable<string> texts, int batchSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// Enables parallel processing for independent requests.
        /// </summary>
        /// <param name="requests">Independent requests to process.</param>
        /// <param name="maxConcurrency">Maximum concurrent operations.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Parallel processing results.</returns>
        Task<ParallelProcessingResult> ProcessInParallelAsync(IEnumerable<OptimizedRequest> requests, int maxConcurrency, CancellationToken cancellationToken = default);
    }
}
