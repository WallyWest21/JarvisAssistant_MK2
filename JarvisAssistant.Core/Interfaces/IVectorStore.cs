using JarvisAssistant.Core.Models;

namespace JarvisAssistant.Core.Interfaces
{
    /// <summary>
    /// Provides methods for storing and retrieving vector embeddings.
    /// </summary>
    public interface IVectorStore
    {
        /// <summary>
        /// Stores a vector embedding with associated metadata.
        /// </summary>
        /// <param name="chunk">The document chunk to store.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates if the storage was successful.</returns>
        Task<bool> StoreVectorAsync(DocumentChunk chunk, CancellationToken cancellationToken = default);

        /// <summary>
        /// Stores multiple vector embeddings in batch.
        /// </summary>
        /// <param name="chunks">The document chunks to store.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates if the storage was successful.</returns>
        Task<bool> StoreBatchAsync(IEnumerable<DocumentChunk> chunks, CancellationToken cancellationToken = default);

        /// <summary>
        /// Searches for similar vectors using cosine similarity.
        /// </summary>
        /// <param name="queryEmbedding">The query embedding vector.</param>
        /// <param name="limit">The maximum number of results to return.</param>
        /// <param name="threshold">The minimum similarity threshold (0.0 to 1.0).</param>
        /// <param name="filter">Optional metadata filter criteria.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the similar chunks with scores.</returns>
        Task<IEnumerable<VectorSearchResult>> SearchSimilarAsync(
            float[] queryEmbedding, 
            int limit = 10, 
            float threshold = 0.0f, 
            Dictionary<string, object>? filter = null, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes all vectors associated with a specific document.
        /// </summary>
        /// <param name="documentId">The unique identifier of the document.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates if the deletion was successful.</returns>
        Task<bool> DeleteDocumentVectorsAsync(Guid documentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the total number of vectors stored.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the vector count.</returns>
        Task<long> GetVectorCountAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates or updates a collection for storing vectors.
        /// </summary>
        /// <param name="collectionName">The name of the collection.</param>
        /// <param name="embeddingDimensions">The dimension size of embeddings to be stored.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates if the collection was created successfully.</returns>
        Task<bool> CreateCollectionAsync(string collectionName, int embeddingDimensions, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if the vector store is available and can accept connections.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates if the store is healthy.</returns>
        Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
    }
}
