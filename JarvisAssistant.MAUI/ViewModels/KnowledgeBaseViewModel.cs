using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace JarvisAssistant.MAUI.ViewModels
{
    /// <summary>
    /// ViewModel for the knowledge base management interface.
    /// </summary>
    public partial class KnowledgeBaseViewModel : BaseViewModel
    {
        private readonly IKnowledgeBaseService? _knowledgeBaseService;
        private readonly ILogger<KnowledgeBaseViewModel>? _logger;

        [ObservableProperty]
        private ObservableCollection<KnowledgeDocument> _documents = new();

        [ObservableProperty]
        private ObservableCollection<SearchResultItem> _searchResults = new();

        [ObservableProperty]
        private string _searchQuery = string.Empty;

        [ObservableProperty]
        private KnowledgeDocument? _selectedDocument;

        [ObservableProperty]
        private SearchResultItem? _selectedSearchResult;

        [ObservableProperty]
        private KnowledgeBaseStats? _statistics;

        [ObservableProperty]
        private bool _isSearching;

        [ObservableProperty]
        private bool _isUploading;

        [ObservableProperty]
        private string _uploadStatus = string.Empty;

        [ObservableProperty]
        private double _uploadProgress;

        [ObservableProperty]
        private string _searchStatus = string.Empty;

        [ObservableProperty]
        private int _totalDocuments;

        [ObservableProperty]
        private long _totalChunks;

        [ObservableProperty]
        private DocumentType _selectedDocumentTypeFilter = DocumentType.Unknown;

        [ObservableProperty]
        private string _selectedSourceFilter = string.Empty;

        [ObservableProperty]
        private string _selectedAuthorFilter = string.Empty;

        [ObservableProperty]
        private bool _highlightMatches = true;

        [ObservableProperty]
        private float _similarityThreshold = 0.1f;

        [ObservableProperty]
        private int _maxResults = 10;

        [ObservableProperty]
        private string _documentPreview = string.Empty;

        [ObservableProperty]
        private bool _isDocumentPreviewVisible;

        /// <summary>
        /// Initializes a new instance of the <see cref="KnowledgeBaseViewModel"/> class.
        /// </summary>
        /// <param name="knowledgeBaseService">The knowledge base service.</param>
        /// <param name="logger">The logger instance.</param>
        public KnowledgeBaseViewModel(
            IKnowledgeBaseService? knowledgeBaseService = null,
            ILogger<KnowledgeBaseViewModel>? logger = null)
        {
            _knowledgeBaseService = knowledgeBaseService;
            _logger = logger;
            Title = "Knowledge Base";
        }

        /// <summary>
        /// Gets the available document types for filtering.
        /// </summary>
        public static DocumentType[] DocumentTypes => Enum.GetValues<DocumentType>();

        /// <summary>
        /// Loads documents from the knowledge base.
        /// </summary>
        [RelayCommand]
        public async Task LoadDocumentsAsync()
        {
            if (_knowledgeBaseService == null)
            {
                await HandleOfflineStateAsync("Knowledge base service is not available");
                return;
            }

            await ExecuteSafelyAsync(async () =>
            {
                _logger?.LogInformation("Loading documents from knowledge base");

                var filter = CreateDocumentFilter();
                var documents = await _knowledgeBaseService.GetDocumentsAsync(filter);

                Documents.Clear();
                foreach (var document in documents)
                {
                    Documents.Add(document);
                }

                TotalDocuments = Documents.Count;
                _logger?.LogInformation("Loaded {Count} documents", Documents.Count);
            });
        }

        /// <summary>
        /// Performs a search in the knowledge base.
        /// </summary>
        [RelayCommand]
        public async Task SearchAsync()
        {
            if (_knowledgeBaseService == null)
            {
                await HandleOfflineStateAsync("Knowledge base service is not available");
                return;
            }

            if (string.IsNullOrWhiteSpace(SearchQuery))
            {
                SearchStatus = "Please enter a search query";
                return;
            }

            IsSearching = true;
            SearchStatus = "Searching...";

            await ExecuteSafelyAsync(async () =>
            {
                _logger?.LogInformation("Searching for: {Query}", SearchQuery);

                var searchOptions = new SearchOptions
                {
                    Limit = MaxResults,
                    SimilarityThreshold = SimilarityThreshold,
                    HighlightMatches = HighlightMatches,
                    IncludeContent = true
                };

                // Apply filters
                if (SelectedDocumentTypeFilter != DocumentType.Unknown)
                {
                    searchOptions.DocumentTypes.Add(SelectedDocumentTypeFilter);
                }

                if (!string.IsNullOrWhiteSpace(SelectedSourceFilter))
                {
                    searchOptions.Sources.Add(SelectedSourceFilter);
                }

                if (!string.IsNullOrWhiteSpace(SelectedAuthorFilter))
                {
                    searchOptions.Authors.Add(SelectedAuthorFilter);
                }

                var searchResult = await _knowledgeBaseService.SearchAsync(SearchQuery, searchOptions);

                SearchResults.Clear();
                foreach (var result in searchResult.Results)
                {
                    SearchResults.Add(result);
                }

                SearchStatus = $"Found {searchResult.TotalResults} results in {searchResult.SearchTimeMs}ms";
                
                if (searchResult.IsFallbackSearch)
                {
                    SearchStatus += " (used keyword search)";
                }

                _logger?.LogInformation("Search completed: {ResultCount} results", searchResult.TotalResults);
            });

            IsSearching = false;
        }

        /// <summary>
        /// Clears the current search results.
        /// </summary>
        [RelayCommand]
        public async Task ClearSearchAsync()
        {
            SearchResults.Clear();
            SearchQuery = string.Empty;
            SearchStatus = string.Empty;
            SelectedSearchResult = null;
            await Task.CompletedTask;
        }

        /// <summary>
        /// Uploads a document to the knowledge base.
        /// </summary>
        [RelayCommand]
        public async Task UploadDocumentAsync()
        {
            if (_knowledgeBaseService == null)
            {
                await HandleOfflineStateAsync("Knowledge base service is not available");
                return;
            }

            try
            {
                // This would typically open a file picker dialog
                // For now, we'll simulate the upload process
                IsUploading = true;
                UploadStatus = "Selecting file...";
                UploadProgress = 0;

                // Simulate file selection and upload
                await Task.Delay(500); // Simulate file picker
                
                UploadStatus = "File selected. Processing...";
                UploadProgress = 25;

                // This is where you would implement actual file picking and upload
                // For example, using Microsoft.Maui.Essentials.FilePicker
                
                UploadStatus = "Upload completed";
                UploadProgress = 100;
                
                await Task.Delay(1000);
                
                // Refresh the document list
                await LoadDocumentsAsync();
                await LoadStatisticsAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to upload document");
                UploadStatus = $"Upload failed: {ex.Message}";
            }
            finally
            {
                IsUploading = false;
                UploadProgress = 0;
                
                // Clear status after a delay
                await Task.Delay(3000);
                UploadStatus = string.Empty;
            }
        }

        /// <summary>
        /// Deletes the selected document from the knowledge base.
        /// </summary>
        [RelayCommand]
        public async Task DeleteDocumentAsync()
        {
            if (_knowledgeBaseService == null || SelectedDocument == null)
                return;

            await ExecuteSafelyAsync(async () =>
            {
                _logger?.LogInformation("Deleting document: {DocumentName}", SelectedDocument.FileName);

                var success = await _knowledgeBaseService.DeleteDocumentAsync(SelectedDocument.Id);
                
                if (success)
                {
                    Documents.Remove(SelectedDocument);
                    SelectedDocument = null;
                    TotalDocuments = Documents.Count;
                    
                    await LoadStatisticsAsync();
                    _logger?.LogInformation("Document deleted successfully");
                }
                else
                {
                    await HandleErrorAsync(new Exception("Failed to delete document"));
                }
            });
        }

        /// <summary>
        /// Shows a preview of the selected document.
        /// </summary>
        [RelayCommand]
        public async Task ShowDocumentPreviewAsync()
        {
            if (SelectedDocument == null)
                return;

            DocumentPreview = SelectedDocument.ContentPreview ?? "No preview available";
            IsDocumentPreviewVisible = true;
            await Task.CompletedTask;
        }

        /// <summary>
        /// Hides the document preview.
        /// </summary>
        [RelayCommand]
        public async Task HideDocumentPreviewAsync()
        {
            IsDocumentPreviewVisible = false;
            await Task.CompletedTask;
        }

        /// <summary>
        /// Loads statistics about the knowledge base.
        /// </summary>
        [RelayCommand]
        public async Task LoadStatisticsAsync()
        {
            if (_knowledgeBaseService == null)
                return;

            await ExecuteSafelyAsync(async () =>
            {
                _logger?.LogInformation("Loading knowledge base statistics");

                Statistics = await _knowledgeBaseService.GetStatsAsync();
                TotalDocuments = Statistics.TotalDocuments;
                TotalChunks = Statistics.TotalChunks;

                _logger?.LogInformation("Statistics loaded: {Documents} documents, {Chunks} chunks", 
                    Statistics.TotalDocuments, Statistics.TotalChunks);
            });
        }

        /// <summary>
        /// Refreshes all data in the view.
        /// </summary>
        public override async Task RefreshAsync()
        {
            await LoadDocumentsAsync();
            await LoadStatisticsAsync();
        }

        /// <summary>
        /// Exports search results to a file.
        /// </summary>
        [RelayCommand]
        public async Task ExportSearchResultsAsync()
        {
            if (!SearchResults.Any())
            {
                SearchStatus = "No search results to export";
                return;
            }

            await ExecuteSafelyAsync(async () =>
            {
                // This would typically open a file save dialog
                // For now, we'll just simulate the export
                _logger?.LogInformation("Exporting {Count} search results", SearchResults.Count);
                
                SearchStatus = "Export completed";
                await Task.Delay(2000);
                SearchStatus = string.Empty;
            });
        }

        /// <inheritdoc/>
        public override async Task OnAppearingAsync()
        {
            await base.OnAppearingAsync();
            await LoadDocumentsAsync();
            await LoadStatisticsAsync();
        }

        #region Private Methods

        private DocumentFilter CreateDocumentFilter()
        {
            var filter = new DocumentFilter();

            if (SelectedDocumentTypeFilter != DocumentType.Unknown)
            {
                filter.DocumentTypes.Add(SelectedDocumentTypeFilter);
            }

            if (!string.IsNullOrWhiteSpace(SelectedSourceFilter))
            {
                filter.Sources.Add(SelectedSourceFilter);
            }

            if (!string.IsNullOrWhiteSpace(SelectedAuthorFilter))
            {
                filter.Authors.Add(SelectedAuthorFilter);
            }

            return filter;
        }

        private async Task HandleOfflineStateAsync(string message)
        {
            _logger?.LogWarning("Knowledge base operation failed: {Message}", message);
            SearchStatus = message;
            UploadStatus = message;
            
            // Clear status after a delay
            await Task.Delay(3000);
            SearchStatus = string.Empty;
            UploadStatus = string.Empty;
        }

        #endregion

        #region Property Changed Handlers

        partial void OnSelectedDocumentChanged(KnowledgeDocument? value)
        {
            // Update UI when document selection changes
            IsDocumentPreviewVisible = false;
            DocumentPreview = string.Empty;
        }

        partial void OnSearchQueryChanged(string value)
        {
            // Auto-search could be implemented here with a delay
            if (string.IsNullOrWhiteSpace(value))
            {
                SearchResults.Clear();
                SearchStatus = string.Empty;
            }
        }

        partial void OnSelectedDocumentTypeFilterChanged(DocumentType value)
        {
            // Reload documents when filter changes
            _ = Task.Run(async () => await LoadDocumentsAsync());
        }

        #endregion
    }
}
