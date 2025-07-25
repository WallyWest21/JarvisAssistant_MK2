using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.RegularExpressions;

namespace JarvisAssistant.Services
{
    /// <summary>
    /// Service for intelligently chunking text content for embedding and search.
    /// </summary>
    public class TextChunkingService : ITextChunker
    {
        private readonly ILogger<TextChunkingService> _logger;

        // Sentence boundary patterns
        private static readonly Regex SentenceEndPattern = new(@"[.!?]+(?=\s+[A-Z]|\s*$)", RegexOptions.Compiled);
        private static readonly Regex ParagraphPattern = new(@"\n\s*\n", RegexOptions.Compiled);
        private static readonly Regex WordBoundaryPattern = new(@"\s+", RegexOptions.Compiled);

        /// <summary>
        /// Initializes a new instance of the <see cref="TextChunkingService"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public TextChunkingService(ILogger<TextChunkingService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public IEnumerable<TextChunk> ChunkText(string text, ChunkingConfig config)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("Empty text provided for chunking");
                yield break;
            }

            var validation = ValidateConfig(config);
            if (validation.Any())
            {
                throw new ArgumentException($"Invalid chunking configuration: {string.Join(", ", validation)}");
            }

            _logger.LogDebug("Chunking text of {Length} characters using {Strategy} strategy", 
                text.Length, config.Strategy);

            var chunks = config.Strategy switch
            {
                ChunkingStrategy.Sentence => ChunkBySentence(text, config),
                ChunkingStrategy.Paragraph => ChunkByParagraph(text, config),
                ChunkingStrategy.FixedSize => ChunkByFixedSize(text, config),
                ChunkingStrategy.Word => ChunkByWord(text, config),
                ChunkingStrategy.Custom => ChunkByCustomSeparators(text, config),
                ChunkingStrategy.Semantic => ChunkBySemantic(text, config),
                _ => throw new ArgumentException($"Unsupported chunking strategy: {config.Strategy}")
            };

            var chunkIndex = 0;
            foreach (var chunk in chunks)
            {
                if (chunk.Content.Length >= config.MinChunkSize)
                {
                    chunk.Index = chunkIndex++;
                    yield return chunk;
                }
            }

            _logger.LogDebug("Created {ChunkCount} chunks from text", chunkIndex);
        }

        /// <inheritdoc/>
        public int EstimateOptimalChunkSize(string text, int maxTokens = 8192)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 1000; // Default chunk size

            // Rough token-to-character ratio (1 token ≈ 4 characters for English)
            var estimatedTokensPerChar = 0.25;
            var maxChars = (int)(maxTokens / estimatedTokensPerChar * 0.8); // Leave 20% buffer

            // Analyze text characteristics
            var avgSentenceLength = EstimateAverageSentenceLength(text);
            var avgParagraphLength = EstimateAverageParagraphLength(text);

            // Prefer paragraph-based chunks if paragraphs are reasonable size
            if (avgParagraphLength > 0 && avgParagraphLength <= maxChars * 0.6)
            {
                return Math.Min((int)(avgParagraphLength * 2), maxChars);
            }

            // Otherwise, aim for 3-5 sentences per chunk
            var optimalSize = (int)(avgSentenceLength * 4);
            return Math.Min(Math.Max(optimalSize, 500), maxChars);
        }

        /// <inheritdoc/>
        public List<string> ValidateConfig(ChunkingConfig config)
        {
            var errors = new List<string>();

            if (config.MaxChunkSize <= 0)
                errors.Add("MaxChunkSize must be greater than zero");

            if (config.MinChunkSize < 0)
                errors.Add("MinChunkSize cannot be negative");

            if (config.MinChunkSize >= config.MaxChunkSize)
                errors.Add("MinChunkSize must be less than MaxChunkSize");

            if (config.OverlapSize < 0)
                errors.Add("OverlapSize cannot be negative");

            if (config.OverlapSize >= config.MaxChunkSize)
                errors.Add("OverlapSize must be less than MaxChunkSize");

            if (config.Strategy == ChunkingStrategy.Custom && !config.CustomSeparators.Any())
                errors.Add("Custom separators must be provided when using Custom strategy");

            return errors;
        }

        /// <inheritdoc/>
        public ChunkingStats GetChunkingStats(string text, ChunkingConfig config)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new ChunkingStats();
            }

            var chunks = ChunkText(text, config).ToList();
            
            if (!chunks.Any())
            {
                return new ChunkingStats();
            }

            var chunkSizes = chunks.Select(c => c.Length).ToList();
            var totalOverlap = chunks.Count > 1 ? (chunks.Count - 1) * config.OverlapSize : 0;

            return new ChunkingStats
            {
                TotalChunks = chunks.Count,
                AverageChunkSize = chunkSizes.Average(),
                MinChunkSize = chunkSizes.Min(),
                MaxChunkSize = chunkSizes.Max(),
                TotalOverlapCharacters = totalOverlap,
                EstimatedMemoryUsage = chunks.Sum(c => c.Content.Length * 2), // Rough UTF-16 estimate
                Efficiency = (double)(text.Length) / (text.Length + totalOverlap)
            };
        }

        #region Private Chunking Methods

        private IEnumerable<TextChunk> ChunkBySentence(string text, ChunkingConfig config)
        {
            var sentences = SplitIntoSentences(text);
            return CreateOverlappingChunks(sentences, config, text);
        }

        private IEnumerable<TextChunk> ChunkByParagraph(string text, ChunkingConfig config)
        {
            var paragraphs = ParagraphPattern.Split(text)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim())
                .ToList();

            return CreateOverlappingChunks(paragraphs, config, text);
        }

        private IEnumerable<TextChunk> ChunkByFixedSize(string text, ChunkingConfig config)
        {
            var chunks = new List<TextChunk>();
            var currentPosition = 0;

            while (currentPosition < text.Length)
            {
                var chunkSize = Math.Min(config.MaxChunkSize, text.Length - currentPosition);
                var chunkText = text.Substring(currentPosition, chunkSize);

                // Try to break at word boundary if we're not at the end
                if (currentPosition + chunkSize < text.Length)
                {
                    var lastSpaceIndex = chunkText.LastIndexOf(' ');
                    if (lastSpaceIndex > chunkSize * 0.8) // Only break if we don't lose too much content
                    {
                        chunkSize = lastSpaceIndex + 1;
                        chunkText = text.Substring(currentPosition, chunkSize);
                    }
                }

                chunks.Add(new TextChunk
                {
                    Content = chunkText.Trim(),
                    StartPosition = currentPosition,
                    EndPosition = currentPosition + chunkSize - 1
                });

                currentPosition += chunkSize - config.OverlapSize;
            }

            return chunks;
        }

        private IEnumerable<TextChunk> ChunkByWord(string text, ChunkingConfig config)
        {
            var words = WordBoundaryPattern.Split(text)
                .Where(w => !string.IsNullOrWhiteSpace(w))
                .ToList();

            var wordsPerChunk = EstimateWordsPerChunk(words, config.MaxChunkSize);
            return CreateWordBasedChunks(words, wordsPerChunk, config, text);
        }

        private IEnumerable<TextChunk> ChunkByCustomSeparators(string text, ChunkingConfig config)
        {
            var segments = new List<string> { text };

            foreach (var separator in config.CustomSeparators)
            {
                var newSegments = new List<string>();
                foreach (var segment in segments)
                {
                    newSegments.AddRange(segment.Split(new[] { separator }, StringSplitOptions.None));
                }
                segments = newSegments;
            }

            var validSegments = segments.Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .ToList();

            return CreateOverlappingChunks(validSegments, config, text);
        }

        private IEnumerable<TextChunk> ChunkBySemantic(string text, ChunkingConfig config)
        {
            // For semantic chunking, we'll use a combination of paragraph and sentence boundaries
            // with intelligent section detection

            var sections = DetectSections(text);
            if (sections.Count <= 1)
            {
                // Fall back to paragraph chunking if no clear sections
                return ChunkByParagraph(text, config);
            }

            var chunks = new List<TextChunk>();
            var currentPosition = 0;

            foreach (var section in sections)
            {
                if (section.Length <= config.MaxChunkSize)
                {
                    // Section fits in one chunk
                    chunks.Add(new TextChunk
                    {
                        Content = section.Trim(),
                        StartPosition = currentPosition,
                        EndPosition = currentPosition + section.Length - 1,
                        Metadata = { ["Type"] = "Section" }
                    });
                }
                else
                {
                    // Section needs to be split further
                    var subChunks = ChunkByParagraph(section, config);
                    foreach (var subChunk in subChunks)
                    {
                        subChunk.StartPosition += currentPosition;
                        subChunk.EndPosition += currentPosition;
                        subChunk.Metadata["Type"] = "SubSection";
                        chunks.Add(subChunk);
                    }
                }

                currentPosition += section.Length;
            }

            return chunks;
        }

        #endregion

        #region Helper Methods

        private List<string> SplitIntoSentences(string text)
        {
            var sentences = new List<string>();
            var matches = SentenceEndPattern.Matches(text);
            var lastIndex = 0;

            foreach (Match match in matches)
            {
                var sentence = text.Substring(lastIndex, match.Index + match.Length - lastIndex).Trim();
                if (!string.IsNullOrWhiteSpace(sentence))
                {
                    sentences.Add(sentence);
                }
                lastIndex = match.Index + match.Length;
            }

            // Add remaining text as last sentence
            if (lastIndex < text.Length)
            {
                var lastSentence = text.Substring(lastIndex).Trim();
                if (!string.IsNullOrWhiteSpace(lastSentence))
                {
                    sentences.Add(lastSentence);
                }
            }

            return sentences;
        }

        private IEnumerable<TextChunk> CreateOverlappingChunks(List<string> segments, ChunkingConfig config, string originalText)
        {
            var chunks = new List<TextChunk>();
            var currentChunk = new StringBuilder();
            var currentSegments = new List<string>();
            var currentPosition = 0;

            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                var potentialLength = currentChunk.Length + segment.Length + (currentChunk.Length > 0 ? 1 : 0);

                if (potentialLength <= config.MaxChunkSize || currentChunk.Length == 0)
                {
                    if (currentChunk.Length > 0)
                        currentChunk.Append(' ');
                    currentChunk.Append(segment);
                    currentSegments.Add(segment);
                }
                else
                {
                    // Create chunk from current content
                    var chunkContent = currentChunk.ToString().Trim();
                    if (!string.IsNullOrWhiteSpace(chunkContent))
                    {
                        chunks.Add(new TextChunk
                        {
                            Content = chunkContent,
                            StartPosition = FindPositionInText(originalText, chunkContent, currentPosition),
                            EndPosition = FindPositionInText(originalText, chunkContent, currentPosition) + chunkContent.Length - 1
                        });
                    }

                    // Start new chunk with overlap
                    var overlapSegments = GetOverlapSegments(currentSegments, config.OverlapSize);
                    currentChunk.Clear();
                    currentSegments.Clear();

                    foreach (var overlapSegment in overlapSegments)
                    {
                        if (currentChunk.Length > 0)
                            currentChunk.Append(' ');
                        currentChunk.Append(overlapSegment);
                        currentSegments.Add(overlapSegment);
                    }

                    // Add current segment
                    if (currentChunk.Length > 0)
                        currentChunk.Append(' ');
                    currentChunk.Append(segment);
                    currentSegments.Add(segment);
                }
            }

            // Add final chunk
            var finalContent = currentChunk.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(finalContent))
            {
                chunks.Add(new TextChunk
                {
                    Content = finalContent,
                    StartPosition = FindPositionInText(originalText, finalContent, currentPosition),
                    EndPosition = FindPositionInText(originalText, finalContent, currentPosition) + finalContent.Length - 1
                });
            }

            return chunks;
        }

        private List<string> GetOverlapSegments(List<string> segments, int overlapSize)
        {
            var overlapSegments = new List<string>();
            var currentLength = 0;

            for (int i = segments.Count - 1; i >= 0 && currentLength < overlapSize; i--)
            {
                var segment = segments[i];
                if (currentLength + segment.Length <= overlapSize)
                {
                    overlapSegments.Insert(0, segment);
                    currentLength += segment.Length;
                }
                else
                {
                    break;
                }
            }

            return overlapSegments;
        }

        private int FindPositionInText(string text, string chunk, int startSearch)
        {
            var index = text.IndexOf(chunk, startSearch, StringComparison.Ordinal);
            return index >= 0 ? index : startSearch;
        }

        private IEnumerable<TextChunk> CreateWordBasedChunks(List<string> words, int wordsPerChunk, ChunkingConfig config, string originalText)
        {
            var chunks = new List<TextChunk>();
            var currentPosition = 0;

            for (int i = 0; i < words.Count; i += wordsPerChunk - config.OverlapSize / 10) // Rough overlap in words
            {
                var chunkWords = words.Skip(i).Take(wordsPerChunk).ToList();
                var chunkContent = string.Join(" ", chunkWords);

                chunks.Add(new TextChunk
                {
                    Content = chunkContent,
                    StartPosition = FindPositionInText(originalText, chunkContent, currentPosition),
                    EndPosition = FindPositionInText(originalText, chunkContent, currentPosition) + chunkContent.Length - 1
                });

                currentPosition += chunkContent.Length;
            }

            return chunks;
        }

        private int EstimateWordsPerChunk(List<string> words, int maxChunkSize)
        {
            if (!words.Any()) return 1;

            var avgWordLength = words.Average(w => w.Length);
            var estimatedWordsPerChunk = (int)(maxChunkSize / (avgWordLength + 1)); // +1 for space
            return Math.Max(1, estimatedWordsPerChunk);
        }

        private double EstimateAverageSentenceLength(string text)
        {
            var sentences = SplitIntoSentences(text);
            return sentences.Any() ? sentences.Average(s => s.Length) : 0;
        }

        private double EstimateAverageParagraphLength(string text)
        {
            var paragraphs = ParagraphPattern.Split(text)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();
            return paragraphs.Any() ? paragraphs.Average(p => p.Length) : 0;
        }

        private List<string> DetectSections(string text)
        {
            var sections = new List<string>();

            // Look for common section indicators (headings, etc.)
            var headingPatterns = new[]
            {
                new Regex(@"^#{1,6}\s+.+$", RegexOptions.Multiline), // Markdown headings
                new Regex(@"^[A-Z][A-Z\s]+$", RegexOptions.Multiline), // ALL CAPS headings
                new Regex(@"^\d+\.\s+[A-Z].+$", RegexOptions.Multiline), // Numbered sections
                new Regex(@"^[A-Z][a-z]+(?:\s+[A-Z][a-z]+)*:$", RegexOptions.Multiline) // Title case with colon
            };

            var sectionBreaks = new List<int> { 0 }; // Start with beginning of text

            foreach (var pattern in headingPatterns)
            {
                var matches = pattern.Matches(text);
                foreach (Match match in matches)
                {
                    if (!sectionBreaks.Contains(match.Index))
                    {
                        sectionBreaks.Add(match.Index);
                    }
                }
            }

            sectionBreaks.Add(text.Length); // End with end of text
            sectionBreaks.Sort();

            // Create sections from break points
            for (int i = 0; i < sectionBreaks.Count - 1; i++)
            {
                var start = sectionBreaks[i];
                var end = sectionBreaks[i + 1];
                var section = text.Substring(start, end - start).Trim();
                
                if (!string.IsNullOrWhiteSpace(section))
                {
                    sections.Add(section);
                }
            }

            return sections.Any() ? sections : new List<string> { text };
        }

        #endregion
    }
}
