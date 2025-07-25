using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace JarvisAssistant.Services.VectorStores
{
    /// <summary>
    /// In-memory vector store implementation for development and testing.
    /// </summary>
    public class InMemoryVectorStore : IVectorStore
    {
        private readonly ILogger<InMemoryVectorStore> _logger;
        private readonly ConcurrentDictionary<Guid, DocumentChunk> _vectors;
        private readonly ConcurrentDictionary<string, bool> _collections;
        private readonly IEmbeddingService _embeddingService;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryVectorStore"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="embeddingService">The embedding service for similarity calculations.</param>
        public InMemoryVectorStore(ILogger<InMemoryVectorStore> logger, IEmbeddingService embeddingService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
            _vectors = new ConcurrentDictionary<Guid, DocumentChunk>();
            _collections = new ConcurrentDictionary<string, bool>();
        }

        /// <inheritdoc/>
        public async Task<bool> StoreVectorAsync(DocumentChunk chunk, CancellationToken cancellationToken = default)
        {
            try
            {
                if (chunk == null)
                {
                    _logger.LogWarning("Null chunk provided for storage");
                    return false;
                }

                if (chunk.Embedding == null || chunk.Embedding.Length == 0)
                {
                    _logger.LogWarning("Chunk {ChunkId} has no embedding vector", chunk.Id);
                    return false;
                }

                _vectors.AddOrUpdate(chunk.Id, chunk, (key, existingChunk) => chunk);
                
                _logger.LogDebug("Stored vector for chunk {ChunkId} with {Dimensions} dimensions", 
                    chunk.Id, chunk.Embedding.Length);

                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store vector for chunk {ChunkId}", chunk.Id);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> StoreBatchAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default)
        {
            try
            {
                var chunkList = chunks?.ToList();
                if (chunkList == null || !chunkList.Any())
                {
                    _logger.LogWarning("Empty chunk list provided for batch storage");
                    return false;
                }

                _logger.LogInformation("Storing batch of {Count} vectors", chunkList.Count);

                var storedCount = 0;
                foreach (var chunk in chunkList)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    if (await StoreVectorAsync(chunk, cancellationToken))
                    {
                        storedCount++;
                    }
                }

                _logger.LogInformation("Successfully stored {StoredCount} out of {TotalCount} vectors", 
                    storedCount, chunkList.Count);

                return storedCount == chunkList.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store vector batch");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<VectorSearchResult>> SearchSimilarAsync(
            float[] queryEmbedding, 
            int limit = 10, 
            float threshold = 0.0f, 
            Dictionary<string, object>? filter = null, 
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (queryEmbedding == null || queryEmbedding.Length == 0)
                {
                    _logger.LogWarning("Empty query embedding provided for search");
                    return Enumerable.Empty<VectorSearchResult>();
                }

                _logger.LogDebug("Searching for similar vectors with threshold {Threshold}, limit {Limit}", 
                    threshold, limit);

                var results = new List<VectorSearchResult>();

                await Task.Run(() =>
                {
                    foreach (var kvp in _vectors)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var chunk = kvp.Value;
                        
                        // Apply metadata filters if provided
                        if (filter != null && !MatchesFilter(chunk, filter))
                            continue;

                        if (chunk.Embedding != null && chunk.Embedding.Length > 0)
                        {
                            var similarity = _embeddingService.CalculateSimilarity(queryEmbedding, chunk.Embedding);
                            
                            if (similarity >= threshold)
                            {
                                results.Add(new VectorSearchResult
                                {
                                    Chunk = chunk,
                                    SimilarityScore = similarity,
                                    DistanceMetric = "cosine",
                                    VectorMetadata = new Dictionary<string, object>
                                    {
                                        ["stored_at"] = chunk.CreatedAt,
                                        ["vector_dimensions"] = chunk.Embedding.Length
                                    }
                                });
                            }
                        }
                    }
                }, cancellationToken);

                // Sort by similarity score (highest first) and take top results
                var sortedResults = results
                    .OrderByDescending(r => r.SimilarityScore)
                    .Take(limit)
                    .ToList();

                _logger.LogDebug("Found {ResultCount} similar vectors", sortedResults.Count);
                return sortedResults;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to search similar vectors");
                return Enumerable.Empty<VectorSearchResult>();
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteDocumentVectorsAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Deleting vectors for document {DocumentId}", documentId);

                var keysToRemove = new List<Guid>();

                await Task.Run(() =>
                {
                    foreach (var kvp in _vectors)
                    {
                        if (kvp.Value.DocumentId == documentId)
                        {
                            keysToRemove.Add(kvp.Key);
                        }
                    }
                }, cancellationToken);

                var removedCount = 0;
                foreach (var key in keysToRemove)
                {
                    if (_vectors.TryRemove(key, out _))
                    {
                        removedCount++;
                    }
                }

                _logger.LogInformation("Removed {RemovedCount} vectors for document {DocumentId}", 
                    removedCount, documentId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete vectors for document {DocumentId}", documentId);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<long> GetVectorCountAsync(CancellationToken cancellationToken = default)
        {
            return await Task.FromResult(_vectors.Count);
        }

        /// <inheritdoc/>
        public async Task<bool> CreateCollectionAsync(string collectionName, int embeddingDimensions, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(collectionName))
                {
                    _logger.LogWarning("Empty collection name provided");
                    return false;
                }

                _collections.AddOrUpdate(collectionName, true, (key, value) => true);
                
                _logger.LogInformation("Created collection {CollectionName} with {Dimensions} dimensions", 
                    collectionName, embeddingDimensions);

                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create collection {CollectionName}", collectionName);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // For in-memory store, we're healthy if we can access our data structures
                var vectorCount = _vectors.Count;
                var collectionCount = _collections.Count;
                
                _logger.LogDebug("Vector store health check: {VectorCount} vectors, {CollectionCount} collections", 
                    vectorCount, collectionCount);

                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Vector store health check failed");
                return false;
            }
        }

        /// <summary>
        /// Gets statistics about the in-memory vector store.
        /// </summary>
        /// <returns>A dictionary containing store statistics.</returns>
        public Dictionary<string, object> GetStatistics()
        {
            var stats = new Dictionary<string, object>
            {
                ["total_vectors"] = _vectors.Count,
                ["total_collections"] = _collections.Count,
                ["memory_usage_estimate"] = EstimateMemoryUsage(),
                ["unique_documents"] = _vectors.Values.Select(v => v.DocumentId).Distinct().Count()
            };

            if (_vectors.Any())
            {
                var vectorLengths = _vectors.Values
                    .Where(v => v.Embedding != null)
                    .Select(v => v.Embedding!.Length)
                    .ToList();

                if (vectorLengths.Any())
                {
                    stats["avg_vector_dimensions"] = vectorLengths.Average();
                    stats["min_vector_dimensions"] = vectorLengths.Min();
                    stats["max_vector_dimensions"] = vectorLengths.Max();
                }
            }

            return stats;
        }

        /// <summary>
        /// Clears all stored vectors and collections.
        /// </summary>
        public void Clear()
        {
            _vectors.Clear();
            _collections.Clear();
            _logger.LogInformation("Cleared all vectors and collections from memory store");
        }

        #region Private Methods

        private bool MatchesFilter(DocumentChunk chunk, Dictionary<string, object> filter)
        {
            foreach (var filterItem in filter)
            {
                var key = filterItem.Key;
                var expectedValue = filterItem.Value;

                // Check document-level properties
                switch (key.ToLowerInvariant())
                {
                    case "document_id":
                        if (!chunk.DocumentId.ToString().Equals(expectedValue?.ToString(), StringComparison.OrdinalIgnoreCase))
                            return false;
                        break;

                    case "chunk_index":
                        if (chunk.ChunkIndex != Convert.ToInt32(expectedValue))
                            return false;
                        break;

                    default:
                        // Check chunk metadata
                        if (chunk.Metadata.TryGetValue(key, out var actualValue))
                        {
                            if (!actualValue?.Equals(expectedValue) == true)
                                return false;
                        }
                        else
                        {
                            return false; // Required metadata key not found
                        }
                        break;
                }
            }

            return true;
        }

        private long EstimateMemoryUsage()
        {
            long totalSize = 0;

            foreach (var chunk in _vectors.Values)
            {
                // Estimate memory usage for each chunk
                totalSize += chunk.Content.Length * 2; // UTF-16 string
                totalSize += chunk.Embedding?.Length * sizeof(float) ?? 0; // Float array
                totalSize += 64; // Estimated overhead for other properties
            }

            return totalSize;
        }

        #endregion
    }
}
