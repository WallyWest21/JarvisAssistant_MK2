using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JarvisAssistant.Core.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using CorePermissionStatus = JarvisAssistant.Core.Services.PermissionStatus;

namespace JarvisAssistant.MAUI.ViewModels
{
    /// <summary>
    /// Simple test ViewModel for speech recognition functionality
    /// </summary>
    public partial class SimpleSpeechTestViewModel : ObservableObject
    {
        private readonly ISpeechRecognitionService _speechService;
        private readonly ILogger<SimpleSpeechTestViewModel> _logger;

        [ObservableProperty]
        private string _statusText = "Ready to test speech recognition";

        [ObservableProperty]
        private string _recognizedText = "";

        [ObservableProperty]
        private bool _isListening = false;

        [ObservableProperty]
        private bool _isAvailable = false;

        public SimpleSpeechTestViewModel(ISpeechRecognitionService speechService, ILogger<SimpleSpeechTestViewModel> logger)
        {
            _speechService = speechService;
            _logger = logger;
            
            Initialize();
        }

        private void Initialize()
        {
            try
            {
                IsAvailable = _speechService.IsAvailable;
                StatusText = IsAvailable ? "Speech recognition available" : "Speech recognition not available";
                
                // Subscribe to events
                _speechService.SpeechRecognized += OnSpeechRecognized;
                _speechService.StateChanged += OnStateChanged;
                
                _logger.LogInformation("SimpleSpeechTestViewModel initialized. Available: {Available}", IsAvailable);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize speech test view model");
                StatusText = $"Error: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task TestSingleRecognitionAsync()
        {
            try
            {
                StatusText = "Testing single recognition...";
                _logger.LogInformation("Starting single recognition test");

                // Check availability first
                if (!_speechService.IsAvailable)
                {
                    StatusText = "Speech recognition not available on this platform";
                    return;
                }

                // Check permissions
                var permissionStatus = await _speechService.RequestPermissionsAsync();
                if (permissionStatus != CorePermissionStatus.Granted)
                {
                    StatusText = $"Permission denied: {permissionStatus}";
                    return;
                }

                StatusText = "Ready to listen... Speak now!";
                
                var result = await _speechService.RecognizeSpeechAsync();
                
                RecognizedText = result.Text;
                StatusText = string.IsNullOrEmpty(result.Text) 
                    ? "No speech detected" 
                    : $"Recognized: {result.Text} (Confidence: {result.Confidence:P})";
                
                _logger.LogInformation("Single recognition result: {Text} (Confidence: {Confidence})", result.Text, result.Confidence);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Single recognition test failed");
                StatusText = $"Error: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task ToggleContinuousRecognitionAsync()
        {
            try
            {
                if (IsListening)
                {
                    StatusText = "Stopping continuous recognition...";
                    await _speechService.StopListeningAsync();
                }
                else
                {
                    StatusText = "Starting continuous recognition...";
                    
                    var options = new SpeechRecognitionOptions
                    {
                        ContinuousRecognition = true,
                        Language = "en-US"
                    };
                    
                    var started = await _speechService.StartListeningAsync(options);
                    
                    if (!started)
                    {
                        StatusText = "Failed to start continuous recognition";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Continuous recognition toggle failed");
                StatusText = $"Error: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task CheckPermissionsAsync()
        {
            try
            {
                StatusText = "Checking permissions...";
                
                var status = await _speechService.RequestPermissionsAsync();
                StatusText = $"Permission status: {status}";
                
                _logger.LogInformation("Permission status: {Status}", status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Permission check failed");
                StatusText = $"Error: {ex.Message}";
            }
        }

        private void OnSpeechRecognized(object? sender, SpeechRecognitionResult result)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                RecognizedText = result.Text;
                StatusText = $"Recognized: {result.Text}";
                _logger.LogInformation("Speech recognized: {Text}", result.Text);
            });
        }

        private void OnStateChanged(object? sender, SpeechRecognitionState state)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                IsListening = state == SpeechRecognitionState.Listening;
                StatusText = $"State: {state}";
                _logger.LogDebug("Speech recognition state: {State}", state);
            });
        }
    }
}
