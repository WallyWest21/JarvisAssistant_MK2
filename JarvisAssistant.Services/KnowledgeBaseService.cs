using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace JarvisAssistant.Services
{
    /// <summary>
    /// Main knowledge base service that orchestrates document processing, embedding generation, and search.
    /// </summary>
    public class KnowledgeBaseService : IKnowledgeBaseService
    {
        private readonly IDocumentProcessor _documentProcessor;
        private readonly ITextChunker _textChunker;
        private readonly IEmbeddingService _embeddingService;
        private readonly IVectorStore _vectorStore;
        private readonly ILogger<KnowledgeBaseService> _logger;
        
        // In-memory storage for document metadata (would typically be in a database)
        private readonly ConcurrentDictionary<Guid, KnowledgeDocument> _documents;
        private readonly object _statsLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="KnowledgeBaseService"/> class.
        /// </summary>
        /// <param name="documentProcessor">The document processor for extracting text.</param>
        /// <param name="textChunker">The text chunker for splitting documents.</param>
        /// <param name="embeddingService">The embedding service for generating vectors.</param>
        /// <param name="vectorStore">The vector store for storing and searching embeddings.</param>
        /// <param name="logger">The logger instance.</param>
        public KnowledgeBaseService(
            IDocumentProcessor documentProcessor,
            ITextChunker textChunker,
            IEmbeddingService embeddingService,
            IVectorStore vectorStore,
            ILogger<KnowledgeBaseService> logger)
        {
            _documentProcessor = documentProcessor ?? throw new ArgumentNullException(nameof(documentProcessor));
            _textChunker = textChunker ?? throw new ArgumentNullException(nameof(textChunker));
            _embeddingService = embeddingService ?? throw new ArgumentNullException(nameof(embeddingService));
            _vectorStore = vectorStore ?? throw new ArgumentNullException(nameof(vectorStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _documents = new ConcurrentDictionary<Guid, KnowledgeDocument>();
        }

        /// <inheritdoc/>
        public async Task<DocumentProcessingResult> UploadDocumentAsync(DocumentUpload document, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var documentId = Guid.NewGuid();
            
            var result = new DocumentProcessingResult
            {
                DocumentId = documentId,
                Success = false
            };

            try
            {
                _logger.LogInformation("Starting upload and processing of document: {FileName}", document.FileName);

                // Validate document
                if (!_documentProcessor.CanProcess(document.FileName))
                {
                    result.ErrorMessage = $"Document type not supported: {Path.GetExtension(document.FileName)}";
                    return result;
                }

                // Extract text from document
                var extractedText = await _documentProcessor.ExtractTextAsync(
                    document.Content, document.FileName, cancellationToken);

                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    result.ErrorMessage = "No text could be extracted from the document";
                    return result;
                }

                result.TotalCharacters = extractedText.Length;

                // Create knowledge document metadata
                var knowledgeDoc = new KnowledgeDocument
                {
                    Id = documentId,
                    FileName = document.FileName,
                    ContentType = document.ContentType,
                    FileSize = document.Content.Length,
                    Type = DetermineDocumentType(document.FileName),
                    UploadedAt = DateTime.UtcNow,
                    TotalCharacters = extractedText.Length,
                    Metadata = new Dictionary<string, object>(document.Metadata),
                    Tags = new List<string>(document.Tags),
                    Source = document.Source,
                    Author = document.Author,
                    Description = document.Description,
                    ContentPreview = extractedText.Length > 500 ? extractedText.Substring(0, 500) + "..." : extractedText
                };

                // Configure chunking based on document type and content
                var chunkingConfig = CreateChunkingConfig(knowledgeDoc.Type, extractedText);

                // Chunk the text
                var textChunks = _textChunker.ChunkText(extractedText, chunkingConfig).ToList();
                result.ChunkCount = textChunks.Count;
                knowledgeDoc.ChunkCount = textChunks.Count;

                if (!textChunks.Any())
                {
                    result.ErrorMessage = "No valid chunks could be created from the document";
                    return result;
                }

                _logger.LogInformation("Created {ChunkCount} chunks from document {FileName}", 
                    textChunks.Count, document.FileName);

                // Generate embeddings for chunks
                var documentChunks = new List<DocumentChunk>();
                var embeddingTasks = textChunks.Select(async (chunk, index) =>
                {
                    try
                    {
                        var embedding = await _embeddingService.GenerateEmbeddingAsync(chunk.Content, cancellationToken);
                        
                        return new DocumentChunk
                        {
                            Id = Guid.NewGuid(),
                            DocumentId = documentId,
                            Content = chunk.Content,
                            Embedding = embedding,
                            ChunkIndex = index,
                            CharacterOffset = chunk.StartPosition,
                            CharacterCount = chunk.Length,
                            Metadata = new Dictionary<string, object>(chunk.Metadata)
                            {
                                ["document_name"] = document.FileName,
                                ["document_type"] = knowledgeDoc.Type.ToString(),
                                ["chunk_strategy"] = chunkingConfig.Strategy.ToString()
                            },
                            CreatedAt = DateTime.UtcNow
                        };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to generate embedding for chunk {Index} in document {FileName}", 
                            index, document.FileName);
                        return null;
                    }
                });

                var embeddingResults = await Task.WhenAll(embeddingTasks);
                documentChunks.AddRange(embeddingResults.Where(chunk => chunk != null).Cast<DocumentChunk>());

                if (!documentChunks.Any())
                {
                    result.ErrorMessage = "Failed to generate embeddings for any chunks";
                    return result;
                }

                // Store vectors in vector store
                var storageSuccess = await _vectorStore.StoreBatchAsync(documentChunks, cancellationToken);
                if (!storageSuccess)
                {
                    result.ErrorMessage = "Failed to store vectors in vector store";
                    return result;
                }

                // Store document metadata
                _documents.TryAdd(documentId, knowledgeDoc);

                stopwatch.Stop();
                result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
                result.Success = true;

                _logger.LogInformation("Successfully processed document {FileName} in {ElapsedMs}ms. " +
                    "Created {ChunkCount} chunks with embeddings.", 
                    document.FileName, result.ProcessingTimeMs, result.ChunkCount);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
                result.ErrorMessage = ex.Message;
                
                _logger.LogError(ex, "Failed to process document {FileName}", document.FileName);
                return result;
            }
        }

        /// <inheritdoc/>
        public async Task<KnowledgeSearchResult> SearchAsync(string query, SearchOptions? options = null, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                _logger.LogInformation("Performing semantic search for query: {Query}", query);

                options ??= new SearchOptions();
                
                // Generate embedding for search query
                var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query, cancellationToken);
                
                // Build metadata filter from search options
                var filter = BuildMetadataFilter(options);
                
                // Search for similar vectors
                var vectorResults = await _vectorStore.SearchSimilarAsync(
                    queryEmbedding, 
                    options.Limit,
                    options.SimilarityThreshold,
                    filter,
                    cancellationToken);

                // Convert to search result items
                var searchItems = new List<SearchResultItem>();
                
                foreach (var vectorResult in vectorResults)
                {
                    var chunk = vectorResult.Chunk;
                    var document = _documents.TryGetValue(chunk.DocumentId, out var doc) ? doc : null;
                    
                    if (document != null && PassesDocumentFilter(document, options))
                    {
                        var item = new SearchResultItem
                        {
                            ChunkId = chunk.Id,
                            DocumentId = chunk.DocumentId,
                            DocumentName = document.FileName,
                            DocumentType = document.Type,
                            Score = vectorResult.SimilarityScore,
                            Content = chunk.Content,
                            ChunkIndex = chunk.ChunkIndex,
                            CharacterOffset = chunk.CharacterOffset,
                            Metadata = new Dictionary<string, object>(document.Metadata),
                            Tags = new List<string>(document.Tags),
                            Source = document.Source,
                            Author = document.Author,
                            UploadedAt = document.UploadedAt,
                            FileSize = document.FileSize
                        };

                        // Add highlighting if requested
                        if (options.HighlightMatches)
                        {
                            item.HighlightedContent = HighlightText(chunk.Content, query);
                        }

                        // Add context from surrounding chunks if available
                        item.Context = await GetChunkContextAsync(chunk, cancellationToken);

                        searchItems.Add(item);
                    }
                }

                stopwatch.Stop();

                var result = new KnowledgeSearchResult
                {
                    Query = query,
                    Results = searchItems,
                    TotalResults = searchItems.Count,
                    SearchTimeMs = stopwatch.ElapsedMilliseconds,
                    SearchType = SearchType.Semantic,
                    IsFallbackSearch = false
                };

                _logger.LogInformation("Semantic search completed in {ElapsedMs}ms. Found {ResultCount} results.", 
                    result.SearchTimeMs, result.TotalResults);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Semantic search failed for query: {Query}", query);
                
                // Try fallback to keyword search
                _logger.LogInformation("Attempting fallback to keyword search");
                var fallbackResult = await KeywordSearchAsync(query, options, cancellationToken);
                fallbackResult.IsFallbackSearch = true;
                return fallbackResult;
            }
        }

        /// <inheritdoc/>
        public async Task<KnowledgeSearchResult> KeywordSearchAsync(string query, SearchOptions? options = null, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                _logger.LogInformation("Performing keyword search for query: {Query}", query);

                options ??= new SearchOptions();
                var searchTerms = ExtractSearchTerms(query);
                var results = new List<SearchResultItem>();

                // Search through all stored documents
                foreach (var kvp in _documents)
                {
                    var document = kvp.Value;
                    
                    if (!PassesDocumentFilter(document, options))
                        continue;

                    // Get all chunks for this document from vector store
                    var documentChunks = await GetDocumentChunksAsync(document.Id, cancellationToken);
                    
                    foreach (var chunk in documentChunks)
                    {
                        var score = CalculateKeywordScore(chunk.Content, searchTerms);
                        
                        if (score > 0)
                        {
                            var item = new SearchResultItem
                            {
                                ChunkId = chunk.Id,
                                DocumentId = document.Id,
                                DocumentName = document.FileName,
                                DocumentType = document.Type,
                                Score = score,
                                Content = chunk.Content,
                                ChunkIndex = chunk.ChunkIndex,
                                CharacterOffset = chunk.CharacterOffset,
                                Metadata = new Dictionary<string, object>(document.Metadata),
                                Tags = new List<string>(document.Tags),
                                Source = document.Source,
                                Author = document.Author,
                                UploadedAt = document.UploadedAt,
                                FileSize = document.FileSize
                            };

                            if (options.HighlightMatches)
                            {
                                item.HighlightedContent = HighlightText(chunk.Content, query);
                            }

                            results.Add(item);
                        }
                    }
                }

                // Sort by score and take top results
                var sortedResults = results
                    .OrderByDescending(r => r.Score)
                    .Take(options.Limit)
                    .ToList();

                stopwatch.Stop();

                var result = new KnowledgeSearchResult
                {
                    Query = query,
                    Results = sortedResults,
                    TotalResults = sortedResults.Count,
                    SearchTimeMs = stopwatch.ElapsedMilliseconds,
                    SearchType = SearchType.Keyword,
                    IsFallbackSearch = false
                };

                _logger.LogInformation("Keyword search completed in {ElapsedMs}ms. Found {ResultCount} results.", 
                    result.SearchTimeMs, result.TotalResults);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Keyword search failed for query: {Query}", query);
                
                stopwatch.Stop();
                return new KnowledgeSearchResult
                {
                    Query = query,
                    Results = new List<SearchResultItem>(),
                    TotalResults = 0,
                    SearchTimeMs = stopwatch.ElapsedMilliseconds,
                    SearchType = SearchType.Keyword,
                    IsFallbackSearch = false
                };
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<KnowledgeDocument>> GetDocumentsAsync(DocumentFilter? filter = null, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask; // For async interface compliance
            
            var documents = _documents.Values.AsEnumerable();
            
            if (filter != null)
            {
                documents = ApplyDocumentFilter(documents, filter);
            }

            return documents.ToList();
        }

        /// <inheritdoc/>
        public async Task<KnowledgeDocument?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask; // For async interface compliance
            return _documents.TryGetValue(documentId, out var document) ? document : null;
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Deleting document {DocumentId}", documentId);

                // Remove from vector store
                var vectorDeleteSuccess = await _vectorStore.DeleteDocumentVectorsAsync(documentId, cancellationToken);
                
                // Remove from document metadata store
                var metadataDeleteSuccess = _documents.TryRemove(documentId, out _);

                var success = vectorDeleteSuccess && metadataDeleteSuccess;
                
                _logger.LogInformation("Document {DocumentId} deletion {Status}", 
                    documentId, success ? "successful" : "failed");

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete document {DocumentId}", documentId);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<int> GetDocumentCountAsync(CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask; // For async interface compliance
            return _documents.Count;
        }

        /// <inheritdoc/>
        public async Task<KnowledgeBaseStats> GetStatsAsync(CancellationToken cancellationToken = default)
        {
            var vectorCount = await _vectorStore.GetVectorCountAsync(cancellationToken);
            
            var documentTypes = _documents.Values
                .GroupBy(d => d.Type)
                .ToDictionary(
                    g => g.Key,
                    g => new DocumentTypeStats
                    {
                        Type = g.Key,
                        Count = g.Count(),
                        TotalBytes = g.Sum(d => d.FileSize),
                        AverageFileSize = g.Average(d => d.FileSize),
                        TotalChunks = g.Sum(d => d.ChunkCount)
                    });

            var stats = new KnowledgeBaseStats
            {
                TotalDocuments = _documents.Count,
                TotalChunks = vectorCount,
                TotalStorageBytes = _documents.Values.Sum(d => d.FileSize),
                TotalEmbeddings = vectorCount,
                DocumentTypeStats = documentTypes,
                CalculatedAt = DateTime.UtcNow,
                AverageChunksPerDocument = _documents.Any() ? _documents.Values.Average(d => d.ChunkCount) : 0
            };

            return stats;
        }

        #region Private Helper Methods

        private DocumentType DetermineDocumentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => DocumentType.Pdf,
                ".docx" or ".docm" => DocumentType.WordDocument,
                ".txt" => DocumentType.Text,
                ".md" or ".markdown" => DocumentType.Markdown,
                ".html" or ".htm" or ".xhtml" => DocumentType.Html,
                ".rtf" => DocumentType.RichText,
                ".jpg" or ".jpeg" or ".png" or ".bmp" or ".tiff" or ".gif" => DocumentType.Image,
                _ => DocumentType.Unknown
            };
        }

        private ChunkingConfig CreateChunkingConfig(DocumentType documentType, string content)
        {
            return documentType switch
            {
                DocumentType.Pdf => new ChunkingConfig
                {
                    MaxChunkSize = 1000,
                    OverlapSize = 100,
                    Strategy = ChunkingStrategy.Sentence,
                    MinChunkSize = 100
                },
                DocumentType.WordDocument => new ChunkingConfig
                {
                    MaxChunkSize = 1200,
                    OverlapSize = 150,
                    Strategy = ChunkingStrategy.Paragraph,
                    MinChunkSize = 150
                },
                DocumentType.Html => new ChunkingConfig
                {
                    MaxChunkSize = 800,
                    OverlapSize = 80,
                    Strategy = ChunkingStrategy.Semantic,
                    MinChunkSize = 100
                },
                DocumentType.Markdown => new ChunkingConfig
                {
                    MaxChunkSize = 1000,
                    OverlapSize = 100,
                    Strategy = ChunkingStrategy.Semantic,
                    MinChunkSize = 100
                },
                _ => new ChunkingConfig
                {
                    MaxChunkSize = 1000,
                    OverlapSize = 100,
                    Strategy = ChunkingStrategy.Sentence,
                    MinChunkSize = 100
                }
            };
        }

        private Dictionary<string, object>? BuildMetadataFilter(SearchOptions options)
        {
            var filter = new Dictionary<string, object>(options.MetadataFilters);

            if (options.DocumentTypes.Any())
            {
                filter["document_type"] = string.Join(",", options.DocumentTypes.Select(dt => dt.ToString()));
            }

            return filter.Any() ? filter : null;
        }

        private bool PassesDocumentFilter(KnowledgeDocument document, SearchOptions options)
        {
            if (options.DocumentTypes.Any() && !options.DocumentTypes.Contains(document.Type))
                return false;

            if (options.Tags.Any() && !options.Tags.Any(tag => document.Tags.Contains(tag)))
                return false;

            if (options.Sources.Any() && !string.IsNullOrEmpty(document.Source) && 
                !options.Sources.Contains(document.Source))
                return false;

            if (options.Authors.Any() && !string.IsNullOrEmpty(document.Author) && 
                !options.Authors.Contains(document.Author))
                return false;

            if (options.DateRange != null)
            {
                if (options.DateRange.StartDate.HasValue && document.UploadedAt < options.DateRange.StartDate.Value)
                    return false;

                if (options.DateRange.EndDate.HasValue && document.UploadedAt > options.DateRange.EndDate.Value)
                    return false;
            }

            return true;
        }

        private IEnumerable<KnowledgeDocument> ApplyDocumentFilter(IEnumerable<KnowledgeDocument> documents, DocumentFilter filter)
        {
            var filtered = documents;

            if (filter.DocumentTypes.Any())
                filtered = filtered.Where(d => filter.DocumentTypes.Contains(d.Type));

            if (filter.Tags.Any())
                filtered = filtered.Where(d => filter.Tags.Any(tag => d.Tags.Contains(tag)));

            if (filter.Sources.Any())
                filtered = filtered.Where(d => !string.IsNullOrEmpty(d.Source) && filter.Sources.Contains(d.Source));

            if (filter.Authors.Any())
                filtered = filtered.Where(d => !string.IsNullOrEmpty(d.Author) && filter.Authors.Contains(d.Author));

            if (filter.DateRange != null)
            {
                if (filter.DateRange.StartDate.HasValue)
                    filtered = filtered.Where(d => d.UploadedAt >= filter.DateRange.StartDate.Value);

                if (filter.DateRange.EndDate.HasValue)
                    filtered = filtered.Where(d => d.UploadedAt <= filter.DateRange.EndDate.Value);
            }

            // Apply sorting
            filtered = filter.SortOrder switch
            {
                SortOrder.UploadedDateDescending => filtered.OrderByDescending(d => d.UploadedAt),
                SortOrder.UploadedDateAscending => filtered.OrderBy(d => d.UploadedAt),
                SortOrder.FileNameAscending => filtered.OrderBy(d => d.FileName),
                SortOrder.FileSizeDescending => filtered.OrderByDescending(d => d.FileSize),
                _ => filtered.OrderByDescending(d => d.UploadedAt)
            };

            // Apply pagination
            if (filter.Offset > 0)
                filtered = filtered.Skip(filter.Offset);

            if (filter.Limit.HasValue)
                filtered = filtered.Take(filter.Limit.Value);

            return filtered;
        }

        private List<string> ExtractSearchTerms(string query)
        {
            return query.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(term => term.Length > 2) // Ignore very short terms
                .Select(term => term.ToLowerInvariant().Trim('.', ',', '!', '?', ';', ':'))
                .Distinct()
                .ToList();
        }

        private float CalculateKeywordScore(string content, List<string> searchTerms)
        {
            if (string.IsNullOrWhiteSpace(content) || !searchTerms.Any())
                return 0f;

            var contentLower = content.ToLowerInvariant();
            var score = 0f;

            foreach (var term in searchTerms)
            {
                var termOccurrences = CountOccurrences(contentLower, term);
                score += termOccurrences * (1f / (1f + Math.Abs(term.Length - 5))); // Prefer medium-length terms
            }

            // Normalize by content length
            return score / (content.Length / 1000f + 1f);
        }

        private int CountOccurrences(string text, string pattern)
        {
            int count = 0;
            int index = 0;
            
            while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) != -1)
            {
                count++;
                index += pattern.Length;
            }
            
            return count;
        }

        private string HighlightText(string content, string query)
        {
            if (string.IsNullOrWhiteSpace(content) || string.IsNullOrWhiteSpace(query))
                return content;

            var terms = ExtractSearchTerms(query);
            var highlighted = content;

            foreach (var term in terms)
            {
                highlighted = System.Text.RegularExpressions.Regex.Replace(
                    highlighted,
                    System.Text.RegularExpressions.Regex.Escape(term),
                    $"**{term}**",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }

            return highlighted;
        }

        private async Task<string?> GetChunkContextAsync(DocumentChunk chunk, CancellationToken cancellationToken)
        {
            // This would typically fetch surrounding chunks from the vector store
            // For now, return null as context retrieval would require more complex vector store queries
            await Task.CompletedTask;
            return null;
        }

        private async Task<List<DocumentChunk>> GetDocumentChunksAsync(Guid documentId, CancellationToken cancellationToken)
        {
            // This is a simplified implementation - in a real scenario, we'd query the vector store
            // with document_id filter to get all chunks for a document
            var filter = new Dictionary<string, object> { ["document_id"] = documentId.ToString() };
            
            // Create a dummy query embedding to search (not ideal, but works for this implementation)
            var dummyEmbedding = new float[_embeddingService.GetEmbeddingDimensions()];
            
            var results = await _vectorStore.SearchSimilarAsync(
                dummyEmbedding, 
                limit: 1000, // Get all chunks
                threshold: -1f, // Accept all similarities
                filter: filter, 
                cancellationToken);

            return results.Select(r => r.Chunk).ToList();
        }

        #endregion
    }
}
