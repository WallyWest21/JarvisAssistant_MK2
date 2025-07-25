using JarvisAssistant.Core.Models;

namespace JarvisAssistant.Core.Interfaces
{
    /// <summary>
    /// Provides methods for managing a knowledge base with vector search capabilities.
    /// </summary>
    public interface IKnowledgeBaseService
    {
        /// <summary>
        /// Uploads and processes a document into the knowledge base.
        /// </summary>
        /// <param name="document">The document to upload and process.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the processed document information.</returns>
        Task<DocumentProcessingResult> UploadDocumentAsync(DocumentUpload document, CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs semantic search across the knowledge base using vector embeddings.
        /// </summary>
        /// <param name="query">The search query.</param>
        /// <param name="options">Search options including filters and limits.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the search results.</returns>
        Task<KnowledgeSearchResult> SearchAsync(string query, SearchOptions? options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs keyword-based search as a fallback when semantic search is unavailable.
        /// </summary>
        /// <param name="query">The search query.</param>
        /// <param name="options">Search options including filters and limits.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the search results.</returns>
        Task<KnowledgeSearchResult> KeywordSearchAsync(string query, SearchOptions? options = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all documents in the knowledge base with optional filtering.
        /// </summary>
        /// <param name="filter">Optional filter criteria.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the list of documents.</returns>
        Task<IEnumerable<KnowledgeDocument>> GetDocumentsAsync(DocumentFilter? filter = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a specific document by its ID.
        /// </summary>
        /// <param name="documentId">The unique identifier of the document.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the document or null if not found.</returns>
        Task<KnowledgeDocument?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a document from the knowledge base.
        /// </summary>
        /// <param name="documentId">The unique identifier of the document to delete.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates if the deletion was successful.</returns>
        Task<bool> DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the total number of documents in the knowledge base.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the document count.</returns>
        Task<int> GetDocumentCountAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets storage statistics for the knowledge base.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains storage statistics.</returns>
        Task<KnowledgeBaseStats> GetStatsAsync(CancellationToken cancellationToken = default);
    }
}
