using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models;
using JarvisAssistant.MAUI.Models;
using Microsoft.Extensions.Logging;

namespace JarvisAssistant.MAUI.ViewModels
{
    public partial class ChatViewModel : ObservableObject
    {
        private readonly ILLMService? _llmService;
        private readonly IVoiceService? _voiceService;
        private readonly IVoiceModeManager? _voiceModeManager;
        private readonly IVoiceCommandProcessor? _voiceCommandProcessor;
        private readonly ILogger<ChatViewModel>? _logger;

        // Use ObservableProperty attributes for proper UI binding
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

        public ChatViewModel(
            ILLMService? llmService = null,
            IVoiceService? voiceService = null,
            IVoiceModeManager? voiceModeManager = null,
            IVoiceCommandProcessor? voiceCommandProcessor = null,
            ILogger<ChatViewModel>? logger = null)
        {
            _llmService = llmService;
            _voiceService = voiceService;
            _voiceModeManager = voiceModeManager;
            _voiceCommandProcessor = voiceCommandProcessor;
            _logger = logger;

            // Platform-specific voice mode behavior
            InitializePlatformBehavior();

            // Check service availability and update status
            CheckServiceAvailability();

            // Add welcome message
            AddWelcomeMessage();
        }

        private void InitializePlatformBehavior()
        {
            // TV platform: Always active voice mode, no toggle
            if (DeviceInfo.Idiom == DeviceIdiom.TV)
            {
                isVoiceModeActive = true;
                showVoiceToggle = false;
            }
            // Desktop/Mobile: Show toggle, default off
            else
            {
                isVoiceModeActive = false;
                showVoiceToggle = true;
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
                    isConnected = false;
                    statusMessage = "LLM Service Offline";
                    _logger?.LogWarning("LLM Service is not available");
                }
                else if (!voiceAvailable)
                {
                    statusMessage = "Voice Services Limited";
                    showVoiceToggle = false;
                    _logger?.LogWarning("Voice services are not available");
                }
                else
                {
                    isConnected = true;
                    statusMessage = "All Systems Online";
                }
            }
            catch (Exception ex)
            {
                isConnected = false;
                statusMessage = "Service Check Failed";
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

            var welcomeMessage = new ChatMessage(welcomeText, false, messageType);
            Messages.Add(welcomeMessage);
        }

        [RelayCommand]
        private async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(currentMessage) || isSending)
                return;

            var userMessage = currentMessage.Trim();
            currentMessage = string.Empty;
            isSending = true;

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

                // Create request
                var request = new ChatRequest
                {
                    Message = userMessage,
                    Type = "user",
                    ConversationId = "main-chat"
                };

                // Add thinking indicator
                var thinkingMessage = new ChatMessage("Analyzing...", false, MessageType.System)
                {
                    IsStreaming = true
                };
                Messages.Add(thinkingMessage);

                // Send to LLM service using correct method name
                var response = await _llmService.SendMessageAsync(request);

                // Remove thinking indicator
                Messages.Remove(thinkingMessage);

                // Add response
                var responseMessage = new ChatMessage(response.Message, false)
                {
                    Type = response.Type == "error" ? MessageType.Error : MessageType.Text
                };
                Messages.Add(responseMessage);

                // Auto-scroll to bottom
                WeakReferenceMessenger.Default.Send("ScrollToBottom");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error sending message");
                
                var errorMessage = new ChatMessage(
                    "I apologize, but I encountered an error processing your request. Please try again.",
                    false,
                    MessageType.Error);
                Messages.Add(errorMessage);
            }
            finally
            {
                isSending = false;
            }
        }

        [RelayCommand]
        private async Task ProcessVoiceCommandAsync()
        {
            if (isListening || !isVoiceModeActive)
                return;

            // Check if voice services are available
            if (_voiceModeManager == null || _voiceCommandProcessor == null)
            {
                voiceCommandFeedback = "Voice services unavailable.";
                await Task.Delay(2000);
                voiceCommandFeedback = string.Empty;
                return;
            }

            isListening = true;
            voiceCommandFeedback = "Listening...";

            try
            {
                // Start voice recognition using voice mode manager
                var voiceResult = await _voiceModeManager.ListenForCommandAsync(TimeSpan.FromSeconds(5));
                
                if (!string.IsNullOrEmpty(voiceResult))
                {
                    voiceCommandFeedback = $"Processing: \"{voiceResult}\"";
                    
                    // Process as voice command first
                    var voiceCommand = new VoiceCommand
                    {
                        Text = voiceResult,
                        Timestamp = DateTime.UtcNow,
                        Source = VoiceCommandSource.Manual
                    };

                    var commandResult = await _voiceCommandProcessor.ProcessCommandAsync(voiceCommand);
                    
                    if (commandResult.Success)
                    {
                        // Command was handled by voice processor
                        var commandMessage = new ChatMessage(
                            $"Voice Command: {voiceResult}\nResult: {commandResult.Response}",
                            false,
                            MessageType.Voice);
                        Messages.Add(commandMessage);
                    }
                    else
                    {
                        // Treat as regular chat message
                        currentMessage = voiceResult;
                        await SendMessageAsync();
                    }
                }
                else
                {
                    voiceCommandFeedback = "No speech detected.";
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error processing voice command");
                voiceCommandFeedback = "Voice recognition error.";
            }
            finally
            {
                isListening = false;
                
                // Clear feedback after delay
                await Task.Delay(2000);
                voiceCommandFeedback = string.Empty;
            }
        }

        [RelayCommand]
        private async Task ToggleVoiceModeAsync()
        {
            if (DeviceInfo.Idiom == DeviceIdiom.TV)
                return; // TV always has voice mode active

            // Check if voice services are available
            if (_voiceModeManager == null)
            {
                statusMessage = "Voice services unavailable";
                await Task.Delay(2000);
                statusMessage = isConnected ? "Ready" : "LLM Service Offline";
                return;
            }

            isVoiceModeActive = !isVoiceModeActive;

            try
            {
                if (isVoiceModeActive)
                {
                    await _voiceModeManager.EnableVoiceModeAsync();
                    statusMessage = "Voice mode activated";
                }
                else
                {
                    await _voiceModeManager.DisableVoiceModeAsync();
                    statusMessage = "Voice mode deactivated";
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error toggling voice mode");
                isVoiceModeActive = !isVoiceModeActive; // Revert on error
            }
        }

        [RelayCommand]
        private async Task RefreshConversationAsync()
        {
            try
            {
                statusMessage = "Refreshing...";
                
                // Check service availability again
                CheckServiceAvailability();
                
                await Task.Delay(1000);
                
                statusMessage = isConnected ? "Conversation refreshed" : "LLM Service Offline";
                await Task.Delay(2000);
                statusMessage = isConnected ? "Ready" : "LLM Service Offline";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error refreshing conversation");
                statusMessage = "Refresh failed";
            }
        }

        [RelayCommand]
        private void ClearConversation()
        {
            Messages.Clear();
            AddWelcomeMessage();
        }

        // Platform-specific input handling
        public void HandleKeyboardShortcut(string shortcut)
        {
            switch (shortcut.ToLower())
            {
                case "ctrl+enter":
                    if (DeviceInfo.Platform == DevicePlatform.WinUI || DeviceInfo.Platform == DevicePlatform.MacCatalyst)
                    {
                        _ = SendMessageAsync();
                    }
                    break;
                case "ctrl+r":
                    _ = RefreshConversationAsync();
                    break;
                case "ctrl+shift+v":
                    _ = ToggleVoiceModeAsync();
                    break;
            }
        }

        // Update voice activity level for visual indicator
        public void UpdateVoiceActivityLevel(double level)
        {
            VoiceActivityLevel = Math.Max(0, Math.Min(1, level));
        }
    }
}
