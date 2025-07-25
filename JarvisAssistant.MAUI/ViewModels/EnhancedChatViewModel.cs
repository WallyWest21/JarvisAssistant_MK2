using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models;
using JarvisAssistant.MAUI.Models;
using Microsoft.Extensions.Logging;

namespace JarvisAssistant.MAUI.ViewModels
{
    /// <summary>
    /// Enhanced Chat ViewModel with Knowledge Base integration.
    /// </summary>
    public partial class EnhancedChatViewModel : BaseViewModel
    {
        private readonly ILLMService? _llmService;
        private readonly IVoiceService? _voiceService;
        private readonly IVoiceModeManager? _voiceModeManager;
        private readonly IVoiceCommandProcessor? _voiceCommandProcessor;
        private readonly IKnowledgeBaseService? _knowledgeBaseService;
        private readonly ILogger<ChatViewModel>? _logger;

        // Core chat properties
        [ObservableProperty]
        private string currentMessage = string.Empty;

        [ObservableProperty]
        private bool isVoiceModeActive;

        [ObservableProperty]
        private bool isSending;

        [ObservableProperty]
        private bool isListening;

        [ObservableProperty]
        private bool isConnected = true;

        [ObservableProperty]
        private string statusMessage = "Ready";

        [ObservableProperty]
        private double voiceActivityLevel;

        [ObservableProperty]
        private string voiceCommandFeedback = string.Empty;

        [ObservableProperty]
        private bool showVoiceToggle = true;

        public ObservableCollection<ChatMessage> Messages { get; } = new();

        [ObservableProperty]
        private bool isKnowledgeBaseEnabled = true;

        [ObservableProperty]
        private bool useKnowledgeBaseSearch = true;

        [ObservableProperty]
        private float knowledgeSearchThreshold = 0.3f;

        [ObservableProperty]
        private int maxKnowledgeResults = 5;

        [ObservableProperty]
        private ObservableCollection<SearchResultItem> lastKnowledgeResults = new();

        [ObservableProperty]
        private bool showKnowledgeReferences = true;

        [ObservableProperty]
        private string knowledgeSearchStatus = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnhancedChatViewModel"/> class.
        /// </summary>
        public EnhancedChatViewModel(
            ILLMService? llmService = null,
            IVoiceService? voiceService = null,
            IVoiceModeManager? voiceModeManager = null,
            IVoiceCommandProcessor? voiceCommandProcessor = null,
            IKnowledgeBaseService? knowledgeBaseService = null,
            ILogger<ChatViewModel>? logger = null)
        {
            _llmService = llmService;
            _voiceService = voiceService;
            _voiceModeManager = voiceModeManager;
            _voiceCommandProcessor = voiceCommandProcessor;
            _knowledgeBaseService = knowledgeBaseService;
            _logger = logger;
            
            Title = "Enhanced Chat";
            
            // Platform-specific voice mode behavior
            InitializePlatformBehavior();

            // Check service availability and update status
            CheckServiceAvailability();
            CheckKnowledgeBaseAvailability();

            // Add welcome message
            AddWelcomeMessage();
        }

        private void InitializePlatformBehavior()
        {
            // TV platform: Always active voice mode, no toggle
            if (DeviceInfo.Idiom == DeviceIdiom.TV)
            {
                IsVoiceModeActive = true;
                ShowVoiceToggle = false;
            }
            // Desktop/Mobile: Show toggle, default off
            else
            {
                IsVoiceModeActive = false;
                ShowVoiceToggle = true;
            }
        }

        private void CheckServiceAvailability()
        {
            try
            {
                bool llmAvailable = _llmService != null;
                bool voiceAvailable = _voiceService != null && _voiceModeManager != null;

                if (!llmAvailable)
                {
                    IsConnected = false;
                    StatusMessage = "LLM Service Offline";
                    _logger?.LogWarning("LLM Service is not available");
                }
                else if (!voiceAvailable)
                {
                    StatusMessage = "Voice Services Limited";
                    ShowVoiceToggle = false;
                    _logger?.LogWarning("Voice services are not available");
                }
                else
                {
                    IsConnected = true;
                    StatusMessage = "All Systems Online";
                }
            }
            catch (Exception ex)
            {
                IsConnected = false;
                StatusMessage = "Service Check Failed";
                _logger?.LogError(ex, "Error checking service availability");
            }
        }

        private void AddWelcomeMessage()
        {
            string welcomeText;
            MessageType messageType;

            if (_llmService == null)
            {
                welcomeText = "Greetings. I am JARVIS - Just A Rather Very Intelligent System. " +
                             "However, my language processing services are currently offline. " +
                             "You may send messages, but I cannot respond until the LLM service is restored.";
                messageType = MessageType.Error;
            }
            else if (_voiceService == null || _voiceModeManager == null)
            {
                welcomeText = "Greetings. I am JARVIS - Just A Rather Very Intelligent System. " +
                             "Text chat is available, but voice services are currently limited.";
                messageType = MessageType.System;
            }
            else
            {
                welcomeText = "Greetings. I am JARVIS - Just A Rather Very Intelligent System. " +
                             "All systems are online. How may I assist you today?";
                messageType = MessageType.System;
            }

            // Add knowledge base info to welcome message
            if (_knowledgeBaseService != null)
            {
                welcomeText += " My knowledge base is also available for enhanced responses.";
            }

            var welcomeMessage = new ChatMessage(welcomeText, false, messageType);
            Messages.Add(welcomeMessage);
        }

        /// <summary>
        /// Enhanced send message command with knowledge base integration.
        /// </summary>
        [RelayCommand]
        public async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(CurrentMessage) || IsSending)
                return;

            var userMessage = CurrentMessage.Trim();
            CurrentMessage = string.Empty;
            IsSending = true;
            LastKnowledgeResults.Clear();

            try
            {
                // Add user message to chat
                var userChatMessage = new ChatMessage(userMessage, true);
                Messages.Add(userChatMessage);

                // Check if LLM service is available
                if (_llmService == null)
                {
                    var offlineMessage = new ChatMessage(
                        "I apologize, but my language processing services are currently offline. Please check your connection and try again later.",
                        false,
                        MessageType.Error);
                    Messages.Add(offlineMessage);
                    return;
                }

                // Search knowledge base if enabled
                KnowledgeSearchResult? knowledgeResults = null;
                if (UseKnowledgeBaseSearch && _knowledgeBaseService != null)
                {
                    knowledgeResults = await SearchKnowledgeBaseAsync(userMessage);
                }

                // Add thinking indicator
                var thinkingMessage = new ChatMessage("Analyzing...", false, MessageType.System)
                {
                    IsStreaming = true
                };
                Messages.Add(thinkingMessage);

                // Create enhanced request with knowledge context
                var request = await CreateEnhancedChatRequestAsync(userMessage, knowledgeResults);

                // Send to LLM service
                var response = await _llmService.SendMessageAsync(request);

                // Remove thinking indicator
                Messages.Remove(thinkingMessage);

                // Create enhanced response with citations
                var responseMessage = CreateEnhancedResponseMessage(response, knowledgeResults);
                Messages.Add(responseMessage);

                // Auto-scroll to bottom
                WeakReferenceMessenger.Default.Send("ScrollToBottom");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error sending enhanced message");
                
                var errorMessage = new ChatMessage(
                    "I apologize, but I encountered an error processing your request. Please try again.",
                    false,
                    MessageType.Error);
                Messages.Add(errorMessage);
            }
            finally
            {
                IsSending = false;
                KnowledgeSearchStatus = string.Empty;
            }
        }

        /// <summary>
        /// Searches the knowledge base for relevant information.
        /// </summary>
        private async Task<KnowledgeSearchResult?> SearchKnowledgeBaseAsync(string query)
        {
            if (_knowledgeBaseService == null)
                return null;

            try
            {
                KnowledgeSearchStatus = "Searching knowledge base...";
                
                var searchOptions = new SearchOptions
                {
                    Limit = MaxKnowledgeResults,
                    SimilarityThreshold = KnowledgeSearchThreshold,
                    IncludeContent = true,
                    HighlightMatches = false
                };

                var results = await _knowledgeBaseService.SearchAsync(query, searchOptions);
                
                if (results.Results.Any())
                {
                    LastKnowledgeResults.Clear();
                    foreach (var result in results.Results)
                    {
                        LastKnowledgeResults.Add(result);
                    }
                    
                    KnowledgeSearchStatus = $"Found {results.TotalResults} relevant documents";
                    _logger?.LogInformation("Knowledge base search found {Count} results", results.TotalResults);
                }
                else
                {
                    KnowledgeSearchStatus = "No relevant documents found";
                    _logger?.LogInformation("Knowledge base search found no results for query: {Query}", query);
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error searching knowledge base");
                KnowledgeSearchStatus = "Knowledge search failed";
                return null;
            }
        }

        /// <summary>
        /// Creates an enhanced chat request with knowledge context.
        /// </summary>
        private async Task<ChatRequest> CreateEnhancedChatRequestAsync(string userMessage, KnowledgeSearchResult? knowledgeResults)
        {
            var request = new ChatRequest
            {
                Message = userMessage,
                Type = "user",
                ConversationId = "main-chat"
            };

            // Add knowledge context if available
            if (knowledgeResults?.Results.Any() == true)
            {
                var contextBuilder = new StringBuilder();
                contextBuilder.AppendLine("RELEVANT KNOWLEDGE BASE INFORMATION:");
                contextBuilder.AppendLine("Use the following information to enhance your response, but do not mention that you're using a knowledge base unless specifically asked.");
                contextBuilder.AppendLine();

                int citationNumber = 1;
                foreach (var result in knowledgeResults.Results.Take(MaxKnowledgeResults))
                {
                    contextBuilder.AppendLine($"[Citation {citationNumber}] Document: {result.DocumentName}");
                    if (!string.IsNullOrEmpty(result.Source))
                    {
                        contextBuilder.AppendLine($"Source: {result.Source}");
                    }
                    contextBuilder.AppendLine($"Relevance: {result.SimilarityScore:P1}");
                    contextBuilder.AppendLine($"Content: {result.ChunkContent}");
                    contextBuilder.AppendLine();
                    citationNumber++;
                }

                contextBuilder.AppendLine("END OF KNOWLEDGE BASE INFORMATION");
                contextBuilder.AppendLine();
                contextBuilder.AppendLine($"User Question: {userMessage}");

                request.Message = contextBuilder.ToString();
                
                // Add metadata about knowledge context
                request.Metadata = new Dictionary<string, object>
                {
                    ["hasKnowledgeContext"] = true,
                    ["knowledgeResultCount"] = knowledgeResults.Results.Count(),
                    ["searchTimeMs"] = knowledgeResults.SearchTimeMs
                };
            }

            return request;
        }

        /// <summary>
        /// Creates an enhanced response message with citations.
        /// </summary>
        private ChatMessage CreateEnhancedResponseMessage(ChatResponse response, KnowledgeSearchResult? knowledgeResults)
        {
            var messageType = response.Type == "error" ? MessageType.Error : MessageType.Text;
            var responseText = response.Message;

            // Add citations if knowledge was used and references should be shown
            if (ShowKnowledgeReferences && knowledgeResults?.Results.Any() == true)
            {
                var citationsBuilder = new StringBuilder();
                citationsBuilder.AppendLine();
                citationsBuilder.AppendLine("📚 **Sources:**");
                
                int citationNumber = 1;
                foreach (var result in knowledgeResults.Results.Take(3)) // Show max 3 citations
                {
                    citationsBuilder.AppendLine($"{citationNumber}. {result.DocumentName}");
                    if (!string.IsNullOrEmpty(result.Source))
                    {
                        citationsBuilder.AppendLine($"   Source: {result.Source}");
                    }
                    citationsBuilder.AppendLine($"   Relevance: {result.SimilarityScore:P1}");
                    citationNumber++;
                }

                responseText += citationsBuilder.ToString();
            }

            var chatMessage = new ChatMessage(responseText, false)
            {
                Type = messageType
            };

            // Add knowledge metadata to the message
            if (knowledgeResults?.Results.Any() == true)
            {
                chatMessage.Metadata = new Dictionary<string, object>
                {
                    ["hasKnowledgeContext"] = true,
                    ["knowledgeResultCount"] = knowledgeResults.Results.Count(),
                    ["searchTimeMs"] = knowledgeResults.SearchTimeMs,
                    ["usedFallback"] = knowledgeResults.IsFallbackSearch
                };
            }

            return chatMessage;
        }

        /// <summary>
        /// Toggles knowledge base search on/off.
        /// </summary>
        [RelayCommand]
        public async Task ToggleKnowledgeBaseSearchAsync()
        {
            UseKnowledgeBaseSearch = !UseKnowledgeBaseSearch;
            
            var statusMessage = UseKnowledgeBaseSearch 
                ? "Knowledge base search enabled" 
                : "Knowledge base search disabled";
            
            var systemMessage = new ChatMessage(statusMessage, false, MessageType.System);
            Messages.Add(systemMessage);
            
            _logger?.LogInformation("Knowledge base search toggled: {Enabled}", UseKnowledgeBaseSearch);
            await Task.CompletedTask;
        }

        /// <summary>
        /// Shows the last knowledge search results.
        /// </summary>
        [RelayCommand]
        public async Task ShowLastKnowledgeResultsAsync()
        {
            if (!LastKnowledgeResults.Any())
            {
                var noResultsMessage = new ChatMessage(
                    "No recent knowledge base search results to display.", 
                    false, 
                    MessageType.System);
                Messages.Add(noResultsMessage);
                return;
            }

            var resultsBuilder = new StringBuilder();
            resultsBuilder.AppendLine("📋 **Recent Knowledge Search Results:**");
            resultsBuilder.AppendLine();

            int resultNumber = 1;
            foreach (var result in LastKnowledgeResults.Take(5))
            {
                resultsBuilder.AppendLine($"**{resultNumber}. {result.DocumentName}**");
                resultsBuilder.AppendLine($"Relevance: {result.SimilarityScore:P1}");
                if (!string.IsNullOrEmpty(result.Source))
                {
                    resultsBuilder.AppendLine($"Source: {result.Source}");
                }
                resultsBuilder.AppendLine($"Content: {result.ChunkContent.Substring(0, Math.Min(200, result.ChunkContent.Length))}...");
                resultsBuilder.AppendLine();
                resultNumber++;
            }

            var resultsMessage = new ChatMessage(resultsBuilder.ToString(), false, MessageType.System);
            Messages.Add(resultsMessage);
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// Manually triggers a knowledge base search.
        /// </summary>
        [RelayCommand]
        public async Task SearchKnowledgeAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return;

            try
            {
                var results = await SearchKnowledgeBaseAsync(query);
                
                if (results?.Results.Any() == true)
                {
                    var searchBuilder = new StringBuilder();
                    searchBuilder.AppendLine($"🔍 **Knowledge Search Results for: \"{query}\"**");
                    searchBuilder.AppendLine();

                    int resultNumber = 1;
                    foreach (var result in results.Results.Take(MaxKnowledgeResults))
                    {
                        searchBuilder.AppendLine($"**{resultNumber}. {result.DocumentName}**");
                        searchBuilder.AppendLine($"Score: {result.SimilarityScore:P1} | Type: {result.DocumentType}");
                        if (!string.IsNullOrEmpty(result.Source))
                        {
                            searchBuilder.AppendLine($"Source: {result.Source}");
                        }
                        searchBuilder.AppendLine($"Content: {result.ChunkContent}");
                        searchBuilder.AppendLine();
                        resultNumber++;
                    }

                    searchBuilder.AppendLine($"Found {results.TotalResults} total results in {results.SearchTimeMs}ms");
                    
                    var searchMessage = new ChatMessage(searchBuilder.ToString(), false, MessageType.System);
                    Messages.Add(searchMessage);
                }
                else
                {
                    var noResultsMessage = new ChatMessage(
                        $"No knowledge base results found for: \"{query}\"", 
                        false, 
                        MessageType.System);
                    Messages.Add(noResultsMessage);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in manual knowledge search");
                var errorMessage = new ChatMessage(
                    "Failed to search knowledge base. Please try again.", 
                    false, 
                    MessageType.Error);
                Messages.Add(errorMessage);
            }
        }

        /// <summary>
        /// Checks knowledge base service availability.
        /// </summary>
        private void CheckKnowledgeBaseAvailability()
        {
            IsKnowledgeBaseEnabled = _knowledgeBaseService != null;
            
            if (!IsKnowledgeBaseEnabled)
            {
                UseKnowledgeBaseSearch = false;
                _logger?.LogWarning("Knowledge base service is not available");
            }
            else
            {
                _logger?.LogInformation("Knowledge base service is available");
            }
        }

        /// <summary>
        /// Processes voice commands with knowledge base integration.
        /// </summary>
        [RelayCommand]
        public async Task ProcessVoiceCommandAsync()
        {
            if (IsListening || !IsVoiceModeActive)
                return;

            // Check if voice services are available
            if (_voiceModeManager == null || _voiceCommandProcessor == null)
            {
                VoiceCommandFeedback = "Voice services unavailable.";
                await Task.Delay(2000);
                VoiceCommandFeedback = string.Empty;
                return;
            }

            IsListening = true;
            VoiceCommandFeedback = "Listening...";

            try
            {
                // Start voice recognition using voice mode manager
                var voiceResult = await _voiceModeManager.ListenForCommandAsync(TimeSpan.FromSeconds(5));
                
                if (!string.IsNullOrEmpty(voiceResult))
                {
                    VoiceCommandFeedback = $"Processing: \"{voiceResult}\"";
                    
                    // Check for knowledge base commands
                    if (voiceResult.Contains("search knowledge", StringComparison.OrdinalIgnoreCase) ||
                        voiceResult.Contains("find documents", StringComparison.OrdinalIgnoreCase))
                    {
                        // Extract search query from voice command
                        var searchQuery = ExtractSearchQuery(voiceResult);
                        if (!string.IsNullOrEmpty(searchQuery))
                        {
                            await SearchKnowledgeAsync(searchQuery);
                        }
                    }
                    else if (voiceResult.Contains("toggle knowledge", StringComparison.OrdinalIgnoreCase))
                    {
                        await ToggleKnowledgeBaseSearchAsync();
                    }
                    else
                    {
                        // Regular chat message
                        CurrentMessage = voiceResult;
                        await SendMessageAsync();
                    }
                }
                else
                {
                    VoiceCommandFeedback = "No speech detected.";
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error processing voice command");
                VoiceCommandFeedback = "Voice recognition failed.";
            }
            finally
            {
                IsListening = false;
                await Task.Delay(2000);
                VoiceCommandFeedback = string.Empty;
            }
        }

        /// <summary>
        /// Toggles voice mode on/off.
        /// </summary>
        [RelayCommand]
        public async Task ToggleVoiceModeAsync()
        {
            if (_voiceModeManager == null)
            {
                StatusMessage = "Voice services not available";
                return;
            }

            IsVoiceModeActive = !IsVoiceModeActive;
            
            var statusMessage = IsVoiceModeActive 
                ? "Voice mode activated" 
                : "Voice mode deactivated";
            
            var systemMessage = new ChatMessage(statusMessage, false, MessageType.System);
            Messages.Add(systemMessage);
            
            _logger?.LogInformation("Voice mode toggled: {Active}", IsVoiceModeActive);
            await Task.CompletedTask;
        }

        /// <summary>
        /// Extracts search query from voice command.
        /// </summary>
        private string ExtractSearchQuery(string voiceCommand)
        {
            // Simple extraction - look for patterns like "search knowledge for X" or "find documents about Y"
            var patterns = new[]
            {
                @"search knowledge for (.+)",
                @"find documents about (.+)",
                @"search for (.+)",
                @"look up (.+)"
            };

            foreach (var pattern in patterns)
            {
                var match = System.Text.RegularExpressions.Regex.Match(
                    voiceCommand, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                
                if (match.Success && match.Groups.Count > 1)
                {
                    return match.Groups[1].Value.Trim();
                }
            }

            return string.Empty;
        }

        /// <inheritdoc/>
        public override async Task OnAppearingAsync()
        {
            await base.OnAppearingAsync();
            UpdateKnowledgeStatus();
        }

        /// <summary>
        /// Updates the knowledge status display.
        /// </summary>
        private void UpdateKnowledgeStatus()
        {
            if (!IsKnowledgeBaseEnabled)
            {
                KnowledgeSearchStatus = "Knowledge base unavailable";
            }
            else if (!UseKnowledgeBaseSearch)
            {
                KnowledgeSearchStatus = "Knowledge search disabled";
            }
            else
            {
                KnowledgeSearchStatus = "Knowledge search ready";
            }
        }
    }
}
