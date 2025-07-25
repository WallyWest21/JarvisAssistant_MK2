using System.ComponentModel.DataAnnotations;

namespace JarvisAssistant.Core.Models
{
    /// <summary>
    /// Represents a document upload request.
    /// </summary>
    public class DocumentUpload
    {
        /// <summary>
        /// Gets or sets the file name of the document.
        /// </summary>
        [Required]
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the binary content of the document.
        /// </summary>
        [Required]
        public byte[] Content { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Gets or sets the MIME type of the document.
        /// </summary>
        public string? ContentType { get; set; }

        /// <summary>
        /// Gets or sets optional metadata associated with the document.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// Gets or sets tags for categorizing the document.
        /// </summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>
        /// Gets or sets the source or origin of the document.
        /// </summary>
        public string? Source { get; set; }

        /// <summary>
        /// Gets or sets the author of the document.
        /// </summary>
        public string? Author { get; set; }

        /// <summary>
        /// Gets or sets an optional description of the document.
        /// </summary>
        public string? Description { get; set; }
    }

    /// <summary>
    /// Represents the result of document processing.
    /// </summary>
    public class DocumentProcessingResult
    {
        /// <summary>
        /// Gets or sets the unique identifier of the processed document.
        /// </summary>
        public Guid DocumentId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the processing was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the error message if processing failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the number of text chunks created from the document.
        /// </summary>
        public int ChunkCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of characters extracted from the document.
        /// </summary>
        public int TotalCharacters { get; set; }

        /// <summary>
        /// Gets or sets the processing time in milliseconds.
        /// </summary>
        public long ProcessingTimeMs { get; set; }

        /// <summary>
        /// Gets or sets any warnings encountered during processing.
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// Gets or sets the document type that was detected.
        /// </summary>
        public DocumentType DetectedType { get; set; }
    }

    /// <summary>
    /// Represents a document stored in the knowledge base.
    /// </summary>
    public class KnowledgeDocument
    {
        /// <summary>
        /// Gets or sets the unique identifier of the document.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the original file name.
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the MIME type of the document.
        /// </summary>
        public string? ContentType { get; set; }

        /// <summary>
        /// Gets or sets the file size in bytes.
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// Gets or sets the document type.
        /// </summary>
        public DocumentType Type { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the document was uploaded.
        /// </summary>
        public DateTime UploadedAt { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the document was last modified.
        /// </summary>
        public DateTime? ModifiedAt { get; set; }

        /// <summary>
        /// Gets or sets the number of text chunks created from this document.
        /// </summary>
        public int ChunkCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of characters in the document.
        /// </summary>
        public int TotalCharacters { get; set; }

        /// <summary>
        /// Gets or sets metadata associated with the document.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// Gets or sets tags for categorizing the document.
        /// </summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>
        /// Gets or sets the source or origin of the document.
        /// </summary>
        public string? Source { get; set; }

        /// <summary>
        /// Gets or sets the author of the document.
        /// </summary>
        public string? Author { get; set; }

        /// <summary>
        /// Gets or sets the description of the document.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the extracted text content preview (first 500 characters).
        /// </summary>
        public string? ContentPreview { get; set; }
    }

    /// <summary>
    /// Represents a chunk of text from a document with its embedding.
    /// </summary>
    public class DocumentChunk
    {
        /// <summary>
        /// Gets or sets the unique identifier of the chunk.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the identifier of the parent document.
        /// </summary>
        public Guid DocumentId { get; set; }

        /// <summary>
        /// Gets or sets the text content of the chunk.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the embedding vector for this chunk.
        /// </summary>
        public float[] Embedding { get; set; } = Array.Empty<float>();

        /// <summary>
        /// Gets or sets the position of this chunk within the document (0-based).
        /// </summary>
        public int ChunkIndex { get; set; }

        /// <summary>
        /// Gets or sets the character offset of this chunk within the original document.
        /// </summary>
        public int CharacterOffset { get; set; }

        /// <summary>
        /// Gets or sets the number of characters in this chunk.
        /// </summary>
        public int CharacterCount { get; set; }

        /// <summary>
        /// Gets or sets metadata specific to this chunk.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// Gets or sets the date and time when this chunk was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Represents the supported document types.
    /// </summary>
    public enum DocumentType
    {
        /// <summary>
        /// Unknown or unsupported document type.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Plain text file (.txt).
        /// </summary>
        Text = 1,

        /// <summary>
        /// Portable Document Format (.pdf).
        /// </summary>
        Pdf = 2,

        /// <summary>
        /// Microsoft Word document (.docx).
        /// </summary>
        WordDocument = 3,

        /// <summary>
        /// Image file with OCR text extraction (.jpg, .png, .bmp, .tiff).
        /// </summary>
        Image = 4,

        /// <summary>
        /// Rich Text Format (.rtf).
        /// </summary>
        RichText = 5,

        /// <summary>
        /// Markdown file (.md).
        /// </summary>
        Markdown = 6,

        /// <summary>
        /// HTML file (.html, .htm).
        /// </summary>
        Html = 7
    }
}
