using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JarvisAssistant.Core.Services;
using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CorePermissionStatus = JarvisAssistant.Core.Services.PermissionStatus;

namespace JarvisAssistant.MAUI.ViewModels
{
    /// <summary>
    /// ViewModel for voice-based interaction with Jarvis Assistant
    /// </summary>
    public partial class VoiceViewModel : BaseViewModel, IDisposable
    {
        private readonly ISpeechRecognitionService _speechRecognitionService;
        private readonly ILLMService _llmService;
        private readonly IVoiceService _voiceService;
        private readonly ILogger<VoiceViewModel> _logger;
        private bool _disposed;

        [ObservableProperty]
        private bool _isVoiceModeActive;

        [ObservableProperty]
        private string _recognizedText = string.Empty;

        [ObservableProperty]
        private string _partialText = string.Empty;

        [ObservableProperty]
        private SpeechRecognitionState _recognitionState = SpeechRecognitionState.Idle;

        [ObservableProperty]
        private bool _isContinuousMode = true;

        [ObservableProperty]
        private string _selectedLanguage = "en-US";

        [ObservableProperty]
        private List<string> _availableLanguages = new();

        [ObservableProperty]
        private bool _isSpeechRecognitionAvailable;

        [ObservableProperty]
        private string _statusMessage = "Ready";

        public VoiceViewModel(
            ISpeechRecognitionService speechRecognitionService,
            ILLMService llmService,
            IVoiceService voiceService,
            ILogger<VoiceViewModel> logger)
        {
            _speechRecognitionService = speechRecognitionService;
            _llmService = llmService;
            _voiceService = voiceService;
            _logger = logger;

            // Subscribe to speech recognition events
            _speechRecognitionService.SpeechRecognized += OnSpeechRecognized;
            _speechRecognitionService.PartialResultsReceived += OnPartialResultsReceived;
            _speechRecognitionService.StateChanged += OnStateChanged;

            // Initialize
            Task.Run(InitializeAsync);
        }

        private async Task InitializeAsync()
        {
            try
            {
                IsSpeechRecognitionAvailable = _speechRecognitionService.IsAvailable;
                
                if (IsSpeechRecognitionAvailable)
                {
                    var languages = await _speechRecognitionService.GetAvailableLanguagesAsync();
                    AvailableLanguages = languages.ToList();
                    StatusMessage = "Speech recognition ready";
                }
                else
                {
                    StatusMessage = "Speech recognition not available";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize voice view model");
                StatusMessage = "Failed to initialize speech recognition";
            }
        }

        [RelayCommand]
        private async Task ToggleVoiceModeAsync()
        {
            try
            {
                if (IsVoiceModeActive)
                {
                    await StopVoiceModeAsync();
                }
                else
                {
                    await StartVoiceModeAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to toggle voice mode");
                await ShowErrorAsync("Failed to toggle voice mode", ex.Message);
            }
        }

        private async Task StartVoiceModeAsync()
        {
            StatusMessage = "Requesting permissions...";
            
            // Request permissions first
            var permissionStatus = await _speechRecognitionService.RequestPermissionsAsync();
            if (permissionStatus != CorePermissionStatus.Granted)
            {
                StatusMessage = "Permission denied";
                await ShowErrorAsync("Permission Required", "Microphone permission is required for voice mode.");
                return;
            }

            var options = new SpeechRecognitionOptions
            {
                Language = SelectedLanguage,
                ContinuousRecognition = IsContinuousMode,
                EnablePartialResults = true,
                MaxAlternatives = 3,
                SilenceTimeout = TimeSpan.FromSeconds(2)
            };

            StatusMessage = "Starting speech recognition...";
            var started = await _speechRecognitionService.StartListeningAsync(options);
            
            if (started)
            {
                IsVoiceModeActive = true;
                RecognizedText = string.Empty;
                PartialText = string.Empty;
                StatusMessage = "Voice mode active - speak now";
            }
            else
            {
                StatusMessage = "Failed to start";
                await ShowErrorAsync("Failed to Start", "Could not start speech recognition.");
            }
        }

        private async Task StopVoiceModeAsync()
        {
            StatusMessage = "Stopping...";
            await _speechRecognitionService.StopListeningAsync();
            IsVoiceModeActive = false;
            PartialText = string.Empty;
            StatusMessage = "Voice mode stopped";
        }

        private async void OnSpeechRecognized(object? sender, SpeechRecognitionResult result)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                RecognizedText = result.Text;
                PartialText = string.Empty;

                _logger.LogInformation("Speech recognized: {Text} (Confidence: {Confidence})", 
                    result.Text, result.Confidence);

                StatusMessage = $"Recognized: {result.Text.Substring(0, Math.Min(30, result.Text.Length))}...";

                // Process the recognized speech with LLM
                if (!string.IsNullOrWhiteSpace(result.Text))
                {
                    await ProcessSpeechWithLLMAsync(result.Text);
                }
            });
        }

        private void OnPartialResultsReceived(object? sender, string partialText)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                PartialText = partialText;
                StatusMessage = $"Listening: {partialText.Substring(0, Math.Min(20, partialText.Length))}...";
            });
        }

        private void OnStateChanged(object? sender, SpeechRecognitionState state)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                RecognitionState = state;
                
                StatusMessage = state switch
                {
                    SpeechRecognitionState.Idle => "Ready",
                    SpeechRecognitionState.Starting => "Starting...",
                    SpeechRecognitionState.Listening => "Listening...",
                    SpeechRecognitionState.Processing => "Processing...",
                    SpeechRecognitionState.Stopping => "Stopping...",
                    SpeechRecognitionState.Error => "Error occurred",
                    _ => StatusMessage
                };
            });
        }

        private async Task ProcessSpeechWithLLMAsync(string speechText)
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Processing with AI...";

                // Send to LLM
                var response = await _llmService.SendMessageAsync(new ChatRequest
                {
                    Message = speechText,
                    ConversationId = Guid.NewGuid().ToString() // You might want to maintain conversation context
                });

                // Convert response to speech
                if (response?.Message != null)
                {
                    StatusMessage = "Generating speech response...";
                    await _voiceService.GenerateSpeechAsync(response.Message);
                    StatusMessage = "Response delivered";
                }
                else
                {
                    StatusMessage = "No response from AI";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process speech with LLM");
                StatusMessage = "Processing failed";
                await ShowErrorAsync("Processing Error", "Failed to process your speech.");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task RecognizeSingleUtteranceAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Starting single recognition...";

                var options = new SpeechRecognitionOptions
                {
                    Language = SelectedLanguage,
                    ContinuousRecognition = false,
                    EnablePartialResults = true
                };

                var result = await _speechRecognitionService.RecognizeSpeechAsync(options);
                
                RecognizedText = result.Text;
                StatusMessage = $"Single recognition complete: {result.Confidence:P} confidence";
                
                if (result.Alternatives.Any())
                {
                    _logger.LogInformation("Alternatives: {Alternatives}", 
                        string.Join(", ", result.Alternatives.Select(a => $"{a.Text} ({a.Confidence:P})")));
                }

                await ProcessSpeechWithLLMAsync(result.Text);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to recognize speech");
                StatusMessage = "Recognition failed";
                await ShowErrorAsync("Recognition Error", "Failed to recognize speech.");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task TestSpeechRecognitionAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Testing speech recognition...";

                // Test permissions
                var permissionStatus = await _speechRecognitionService.RequestPermissionsAsync();
                _logger.LogInformation("Permission status: {Status}", permissionStatus);

                // Test availability
                var isAvailable = _speechRecognitionService.IsAvailable;
                _logger.LogInformation("Speech recognition available: {Available}", isAvailable);

                // Test languages
                var languages = await _speechRecognitionService.GetAvailableLanguagesAsync();
                _logger.LogInformation("Available languages: {Languages}", string.Join(", ", languages));

                StatusMessage = $"Test complete - Available: {isAvailable}, Permission: {permissionStatus}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Speech recognition test failed");
                StatusMessage = "Test failed";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ShowErrorAsync(string title, string message)
        {
            // Implementation depends on your dialog service
            // This is a placeholder
            _logger.LogError("Error: {Title} - {Message}", title, message);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _speechRecognitionService.SpeechRecognized -= OnSpeechRecognized;
                _speechRecognitionService.PartialResultsReceived -= OnPartialResultsReceived;
                _speechRecognitionService.StateChanged -= OnStateChanged;
                
                _disposed = true;
            }
        }
    }
}
