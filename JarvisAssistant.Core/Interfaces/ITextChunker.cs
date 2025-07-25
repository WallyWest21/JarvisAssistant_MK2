using JarvisAssistant.Core.Models;

namespace JarvisAssistant.Core.Interfaces
{
    /// <summary>
    /// Provides methods for intelligently chunking text content.
    /// </summary>
    public interface ITextChunker
    {
        /// <summary>
        /// Chunks the given text into smaller segments based on the specified configuration.
        /// </summary>
        /// <param name="text">The text to chunk.</param>
        /// <param name="config">The chunking configuration to use.</param>
        /// <returns>An enumerable of text chunks with metadata.</returns>
        IEnumerable<TextChunk> ChunkText(string text, ChunkingConfig config);

        /// <summary>
        /// Estimates the optimal chunk size based on the content and target embedding model.
        /// </summary>
        /// <param name="text">The text to analyze.</param>
        /// <param name="maxTokens">The maximum number of tokens supported by the embedding model.</param>
        /// <returns>The recommended chunk size in characters.</returns>
        int EstimateOptimalChunkSize(string text, int maxTokens = 8192);

        /// <summary>
        /// Validates that a chunking configuration is valid and sensible.
        /// </summary>
        /// <param name="config">The configuration to validate.</param>
        /// <returns>A list of validation errors, empty if valid.</returns>
        List<string> ValidateConfig(ChunkingConfig config);

        /// <summary>
        /// Gets statistics about how the text would be chunked with the given configuration.
        /// </summary>
        /// <param name="text">The text to analyze.</param>
        /// <param name="config">The chunking configuration to use.</param>
        /// <returns>Statistics about the chunking operation.</returns>
        ChunkingStats GetChunkingStats(string text, ChunkingConfig config);
    }

    /// <summary>
    /// Represents a chunk of text with position information.
    /// </summary>
    public class TextChunk
    {
        /// <summary>
        /// Gets or sets the text content of the chunk.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the starting character position in the original text.
        /// </summary>
        public int StartPosition { get; set; }

        /// <summary>
        /// Gets or sets the ending character position in the original text.
        /// </summary>
        public int EndPosition { get; set; }

        /// <summary>
        /// Gets or sets the chunk index (0-based).
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets metadata about this chunk.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// Gets the length of the chunk content.
        /// </summary>
        public int Length => Content.Length;
    }

    /// <summary>
    /// Represents statistics about a chunking operation.
    /// </summary>
    public class ChunkingStats
    {
        /// <summary>
        /// Gets or sets the total number of chunks that would be created.
        /// </summary>
        public int TotalChunks { get; set; }

        /// <summary>
        /// Gets or sets the average chunk size in characters.
        /// </summary>
        public double AverageChunkSize { get; set; }

        /// <summary>
        /// Gets or sets the minimum chunk size.
        /// </summary>
        public int MinChunkSize { get; set; }

        /// <summary>
        /// Gets or sets the maximum chunk size.
        /// </summary>
        public int MaxChunkSize { get; set; }

        /// <summary>
        /// Gets or sets the total overlap characters across all chunks.
        /// </summary>
        public int TotalOverlapCharacters { get; set; }

        /// <summary>
        /// Gets or sets the estimated memory usage in bytes.
        /// </summary>
        public long EstimatedMemoryUsage { get; set; }

        /// <summary>
        /// Gets or sets the chunking efficiency (useful content vs. overhead).
        /// </summary>
        public double Efficiency { get; set; }
    }
}
