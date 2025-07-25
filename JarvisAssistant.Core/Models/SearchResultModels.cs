namespace JarvisAssistant.Core.Models
{
    /// <summary>
    /// Represents the result of a knowledge base search operation.
    /// </summary>
    public class KnowledgeSearchResult
    {
        /// <summary>
        /// Gets or sets the search query that was executed.
        /// </summary>
        public string Query { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the search results.
        /// </summary>
        public List<SearchResultItem> Results { get; set; } = new();

        /// <summary>
        /// Gets or sets the total number of results found (may be larger than the returned results due to pagination).
        /// </summary>
        public int TotalResults { get; set; }

        /// <summary>
        /// Gets or sets the time taken to execute the search in milliseconds.
        /// </summary>
        public long SearchTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the type of search that was performed.
        /// </summary>
        public SearchType SearchType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this was a fallback search.
        /// </summary>
        public bool IsFallbackSearch { get; set; }

        /// <summary>
        /// Gets or sets suggested alternative queries or corrections.
        /// </summary>
        public List<string> Suggestions { get; set; } = new();

        /// <summary>
        /// Gets or sets faceted search information (counts by category).
        /// </summary>
        public Dictionary<string, Dictionary<string, int>> Facets { get; set; } = new();
    }

    /// <summary>
    /// Represents a single search result item.
    /// </summary>
    public class SearchResultItem
    {
        /// <summary>
        /// Gets or sets the unique identifier of the document chunk.
        /// </summary>
        public Guid ChunkId { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the parent document.
        /// </summary>
        public Guid DocumentId { get; set; }

        /// <summary>
        /// Gets or sets the name of the source document.
        /// </summary>
        public string DocumentName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of the source document.
        /// </summary>
        public DocumentType DocumentType { get; set; }

        /// <summary>
        /// Gets or sets the relevance score (0.0 to 1.0, higher is more relevant).
        /// </summary>
        public float Score { get; set; }

        /// <summary>
        /// Gets or sets the similarity score (alias for Score for backward compatibility).
        /// </summary>
        public float SimilarityScore
        {
            get => Score;
            set => Score = value;
        }

        /// <summary>
        /// Gets or sets the matching text content.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the chunk content (alias for Content for backward compatibility).
        /// </summary>
        public string ChunkContent
        {
            get => Content;
            set => Content = value;
        }

        /// <summary>
        /// Gets or sets the highlighted content with search terms emphasized.
        /// </summary>
        public string? HighlightedContent { get; set; }

        /// <summary>
        /// Gets or sets the position of this chunk within the document.
        /// </summary>
        public int ChunkIndex { get; set; }

        /// <summary>
        /// Gets or sets the character offset within the document.
        /// </summary>
        public int CharacterOffset { get; set; }

        /// <summary>
        /// Gets or sets the context surrounding the match (for better understanding).
        /// </summary>
        public string? Context { get; set; }

        /// <summary>
        /// Gets or sets metadata associated with the document.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// Gets or sets tags associated with the document.
        /// </summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>
        /// Gets or sets the source of the document.
        /// </summary>
        public string? Source { get; set; }

        /// <summary>
        /// Gets or sets the author of the document.
        /// </summary>
        public string? Author { get; set; }

        /// <summary>
        /// Gets or sets the upload date of the document.
        /// </summary>
        public DateTime UploadedAt { get; set; }

        /// <summary>
        /// Gets or sets the file size of the source document.
        /// </summary>
        public long FileSize { get; set; }
    }

    /// <summary>
    /// Represents a vector search result with similarity scoring.
    /// </summary>
    public class VectorSearchResult
    {
        /// <summary>
        /// Gets or sets the document chunk that was found.
        /// </summary>
        public DocumentChunk Chunk { get; set; } = new();

        /// <summary>
        /// Gets or sets the similarity score (0.0 to 1.0, higher is more similar).
        /// </summary>
        public float SimilarityScore { get; set; }

        /// <summary>
        /// Gets or sets the distance metric used for comparison.
        /// </summary>
        public string DistanceMetric { get; set; } = "cosine";

        /// <summary>
        /// Gets or sets additional metadata from the vector store.
        /// </summary>
        public Dictionary<string, object> VectorMetadata { get; set; } = new();
    }

    /// <summary>
    /// Represents the type of search performed.
    /// </summary>
    public enum SearchType
    {
        /// <summary>
        /// Semantic search using vector embeddings.
        /// </summary>
        Semantic = 0,

        /// <summary>
        /// Keyword-based text search.
        /// </summary>
        Keyword = 1,

        /// <summary>
        /// Hybrid search combining semantic and keyword approaches.
        /// </summary>
        Hybrid = 2,

        /// <summary>
        /// Exact phrase matching.
        /// </summary>
        Exact = 3
    }

    /// <summary>
    /// Represents configuration for text chunking strategies.
    /// </summary>
    public class ChunkingConfig
    {
        /// <summary>
        /// Gets or sets the maximum number of characters per chunk.
        /// </summary>
        public int MaxChunkSize { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the number of characters to overlap between chunks.
        /// </summary>
        public int OverlapSize { get; set; } = 200;

        /// <summary>
        /// Gets or sets the chunking strategy to use.
        /// </summary>
        public ChunkingStrategy Strategy { get; set; } = ChunkingStrategy.Sentence;

        /// <summary>
        /// Gets or sets custom separators for chunking (used with Custom strategy).
        /// </summary>
        public List<string> CustomSeparators { get; set; } = new();

        /// <summary>
        /// Gets or sets a value indicating whether to preserve formatting in chunks.
        /// </summary>
        public bool PreserveFormatting { get; set; } = false;

        /// <summary>
        /// Gets or sets the minimum chunk size to avoid creating very small chunks.
        /// </summary>
        public int MinChunkSize { get; set; } = 100;
    }

    /// <summary>
    /// Represents different strategies for chunking text.
    /// </summary>
    public enum ChunkingStrategy
    {
        /// <summary>
        /// Split text by sentences, attempting to preserve sentence boundaries.
        /// </summary>
        Sentence = 0,

        /// <summary>
        /// Split text by paragraphs, using double line breaks.
        /// </summary>
        Paragraph = 1,

        /// <summary>
        /// Split text by a fixed number of characters.
        /// </summary>
        FixedSize = 2,

        /// <summary>
        /// Split text by words, maintaining word boundaries.
        /// </summary>
        Word = 3,

        /// <summary>
        /// Use custom separators defined in the configuration.
        /// </summary>
        Custom = 4,

        /// <summary>
        /// Intelligent chunking based on document structure (headings, sections, etc.).
        /// </summary>
        Semantic = 5
    }
}
