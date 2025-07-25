namespace JarvisAssistant.Core.Models
{
    /// <summary>
    /// Represents search options for knowledge base queries.
    /// </summary>
    public class SearchOptions
    {
        /// <summary>
        /// Gets or sets the maximum number of results to return.
        /// </summary>
        public int Limit { get; set; } = 10;

        /// <summary>
        /// Gets or sets the minimum similarity threshold for semantic search (0.0 to 1.0).
        /// </summary>
        public float SimilarityThreshold { get; set; } = 0.0f;

        /// <summary>
        /// Gets or sets the document types to filter by.
        /// </summary>
        public List<DocumentType> DocumentTypes { get; set; } = new();

        /// <summary>
        /// Gets or sets the tags to filter by.
        /// </summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>
        /// Gets or sets the sources to filter by.
        /// </summary>
        public List<string> Sources { get; set; } = new();

        /// <summary>
        /// Gets or sets the authors to filter by.
        /// </summary>
        public List<string> Authors { get; set; } = new();

        /// <summary>
        /// Gets or sets the date range for filtering documents.
        /// </summary>
        public DateRange? DateRange { get; set; }

        /// <summary>
        /// Gets or sets additional metadata filters.
        /// </summary>
        public Dictionary<string, object> MetadataFilters { get; set; } = new();

        /// <summary>
        /// Gets or sets a value indicating whether to include document content in results.
        /// </summary>
        public bool IncludeContent { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to highlight matching text in results.
        /// </summary>
        public bool HighlightMatches { get; set; } = true;
    }

    /// <summary>
    /// Represents a date range for filtering.
    /// </summary>
    public class DateRange
    {
        /// <summary>
        /// Gets or sets the start date (inclusive).
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Gets or sets the end date (inclusive).
        /// </summary>
        public DateTime? EndDate { get; set; }
    }

    /// <summary>
    /// Represents document filter criteria.
    /// </summary>
    public class DocumentFilter
    {
        /// <summary>
        /// Gets or sets the document types to filter by.
        /// </summary>
        public List<DocumentType> DocumentTypes { get; set; } = new();

        /// <summary>
        /// Gets or sets the tags to filter by.
        /// </summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>
        /// Gets or sets the sources to filter by.
        /// </summary>
        public List<string> Sources { get; set; } = new();

        /// <summary>
        /// Gets or sets the authors to filter by.
        /// </summary>
        public List<string> Authors { get; set; } = new();

        /// <summary>
        /// Gets or sets the date range for filtering documents.
        /// </summary>
        public DateRange? DateRange { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of documents to return.
        /// </summary>
        public int? Limit { get; set; }

        /// <summary>
        /// Gets or sets the number of documents to skip (for pagination).
        /// </summary>
        public int Offset { get; set; } = 0;

        /// <summary>
        /// Gets or sets the sort order for results.
        /// </summary>
        public SortOrder SortOrder { get; set; } = SortOrder.UploadedDateDescending;
    }

    /// <summary>
    /// Represents sort order options for document lists.
    /// </summary>
    public enum SortOrder
    {
        /// <summary>
        /// Sort by upload date, newest first.
        /// </summary>
        UploadedDateDescending = 0,

        /// <summary>
        /// Sort by upload date, oldest first.
        /// </summary>
        UploadedDateAscending = 1,

        /// <summary>
        /// Sort by file name alphabetically.
        /// </summary>
        FileNameAscending = 2,

        /// <summary>
        /// Sort by file size, largest first.
        /// </summary>
        FileSizeDescending = 3,

        /// <summary>
        /// Sort by relevance score (for search results).
        /// </summary>
        Relevance = 4
    }

    /// <summary>
    /// Represents knowledge base storage statistics.
    /// </summary>
    public class KnowledgeBaseStats
    {
        /// <summary>
        /// Gets or sets the total number of documents stored.
        /// </summary>
        public int TotalDocuments { get; set; }

        /// <summary>
        /// Gets or sets the total number of text chunks stored.
        /// </summary>
        public long TotalChunks { get; set; }

        /// <summary>
        /// Gets or sets the total storage size in bytes.
        /// </summary>
        public long TotalStorageBytes { get; set; }

        /// <summary>
        /// Gets or sets the total number of embeddings stored.
        /// </summary>
        public long TotalEmbeddings { get; set; }

        /// <summary>
        /// Gets or sets statistics by document type.
        /// </summary>
        public Dictionary<DocumentType, DocumentTypeStats> DocumentTypeStats { get; set; } = new();

        /// <summary>
        /// Gets or sets the date and time when these statistics were calculated.
        /// </summary>
        public DateTime CalculatedAt { get; set; }

        /// <summary>
        /// Gets or sets the average processing time per document in milliseconds.
        /// </summary>
        public double AverageProcessingTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the average number of chunks per document.
        /// </summary>
        public double AverageChunksPerDocument { get; set; }
    }

    /// <summary>
    /// Represents statistics for a specific document type.
    /// </summary>
    public class DocumentTypeStats
    {
        /// <summary>
        /// Gets or sets the document type.
        /// </summary>
        public DocumentType Type { get; set; }

        /// <summary>
        /// Gets or sets the number of documents of this type.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Gets or sets the total storage size in bytes for this type.
        /// </summary>
        public long TotalBytes { get; set; }

        /// <summary>
        /// Gets or sets the average file size in bytes for this type.
        /// </summary>
        public double AverageFileSize { get; set; }

        /// <summary>
        /// Gets or sets the total number of chunks for this type.
        /// </summary>
        public long TotalChunks { get; set; }
    }
}
