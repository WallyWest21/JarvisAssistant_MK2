using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JarvisAssistant.SpeechTest.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace JarvisAssistant.SpeechTest.ViewModels
{
    public partial class SpeechTestViewModel : ObservableObject
    {
        private readonly ISpeechRecognitionService _speechService;
        private readonly ILogger<SpeechTestViewModel> _logger;

        [ObservableProperty]
        private string _status = "Ready";

        [ObservableProperty]
        private string _lastRecognizedText = "";

        [ObservableProperty]
        private string _partialText = "";

        [ObservableProperty]
        private bool _isListening = false;

        [ObservableProperty]
        private bool _isAvailable = false;

        [ObservableProperty]
        private string _permissionStatus = "Unknown";

        [ObservableProperty]
        private float _lastConfidence = 0f;

        [ObservableProperty]
        private string _currentState = "Idle";

        [ObservableProperty]
        private bool _continuousMode = false;

        [ObservableProperty]
        private string _selectedLanguage = "en-US";

        public ObservableCollection<string> AvailableLanguages { get; } = new();
        public ObservableCollection<string> LogMessages { get; } = new();
        public ObservableCollection<DiagnosticResultItem> DiagnosticResults { get; } = new();

        public SpeechTestViewModel(ISpeechRecognitionService speechService, ILogger<SpeechTestViewModel> logger)
        {
            _speechService = speechService;
            _logger = logger;

            // Wire up events
            _speechService.SpeechRecognized += OnSpeechRecognized;
            _speechService.PartialResultsReceived += OnPartialResultsReceived;
            _speechService.StateChanged += OnStateChanged;

            // Initialize
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                AddLogMessage("Initializing speech recognition service...");
                
                // Check availability
                IsAvailable = _speechService.IsAvailable;
                AddLogMessage($"Service available: {IsAvailable}");

                // Check permissions
                var permissionResult = await _speechService.RequestPermissionsAsync();
                PermissionStatus = permissionResult.ToString();
                AddLogMessage($"Permission status: {PermissionStatus}");

                // Get available languages
                var languages = await _speechService.GetAvailableLanguagesAsync();
                AvailableLanguages.Clear();
                foreach (var language in languages)
                {
                    AvailableLanguages.Add(language);
                }
                AddLogMessage($"Available languages: {AvailableLanguages.Count}");

                Status = IsAvailable ? "Ready" : "Not Available";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize");
                AddLogMessage($"Initialization error: {ex.Message}");
                Status = "Error";
            }
        }

        [RelayCommand]
        private async Task StartSingleRecognitionAsync()
        {
            try
            {
                AddLogMessage("Starting single recognition...");
                Status = "Listening (Single)";

                var options = new SpeechRecognitionOptions
                {
                    Language = SelectedLanguage,
                    ContinuousRecognition = false,
                    MaxAlternatives = 3,
                    SilenceTimeout = TimeSpan.FromSeconds(3),
                    MaxListeningTime = TimeSpan.FromSeconds(10)
                };

                var result = await _speechService.RecognizeSpeechAsync(options);
                
                if (!string.IsNullOrEmpty(result.Text))
                {
                    LastRecognizedText = result.Text;
                    LastConfidence = result.Confidence;
                    AddLogMessage($"Recognition result: '{result.Text}' (Confidence: {result.Confidence:P})");
                    
                    if (result.Alternatives?.Count > 0)
                    {
                        foreach (var alt in result.Alternatives)
                        {
                            AddLogMessage($"  Alternative: '{alt.Text}' ({alt.Confidence:P})");
                        }
                    }
                }
                else
                {
                    AddLogMessage("No speech detected");
                }

                Status = "Ready";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Single recognition failed");
                AddLogMessage($"Single recognition error: {ex.Message}");
                Status = "Error";
            }
        }

        [RelayCommand]
        private async Task StartContinuousRecognitionAsync()
        {
            try
            {
                if (IsListening)
                {
                    AddLogMessage("Already listening");
                    return;
                }

                AddLogMessage("Starting continuous recognition...");
                Status = "Listening (Continuous)";

                var options = new SpeechRecognitionOptions
                {
                    Language = SelectedLanguage,
                    ContinuousRecognition = true,
                    MaxAlternatives = 3,
                    SilenceTimeout = TimeSpan.FromSeconds(2)
                };

                var success = await _speechService.StartListeningAsync(options);
                if (success)
                {
                    IsListening = true;
                    AddLogMessage("Continuous recognition started");
                }
                else
                {
                    AddLogMessage("Failed to start continuous recognition");
                    Status = "Error";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Continuous recognition start failed");
                AddLogMessage($"Continuous recognition error: {ex.Message}");
                Status = "Error";
            }
        }

        [RelayCommand]
        private async Task StopRecognitionAsync()
        {
            try
            {
                if (!IsListening)
                {
                    AddLogMessage("Not currently listening");
                    return;
                }

                AddLogMessage("Stopping recognition...");
                await _speechService.StopListeningAsync();
                IsListening = false;
                Status = "Ready";
                AddLogMessage("Recognition stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stop recognition failed");
                AddLogMessage($"Stop recognition error: {ex.Message}");
                Status = "Error";
            }
        }

        [RelayCommand]
        private async Task RunDiagnosticsAsync()
        {
            try
            {
                AddLogMessage("Running diagnostics...");
                Status = "Running Diagnostics";

                var diagnostics = await _speechService.RunDiagnosticsAsync();
                
                DiagnosticResults.Clear();
                
                // Add general info
                DiagnosticResults.Add(new DiagnosticResultItem 
                { 
                    Type = "Info", 
                    Message = $"Service Available: {diagnostics.IsAvailable}" 
                });
                DiagnosticResults.Add(new DiagnosticResultItem 
                { 
                    Type = "Info", 
                    Message = $"Permission Status: {diagnostics.PermissionStatus}" 
                });
                DiagnosticResults.Add(new DiagnosticResultItem 
                { 
                    Type = "Info", 
                    Message = $"Available Languages: {diagnostics.AvailableLanguages?.Count ?? 0}" 
                });

                // Add system info
                foreach (var kvp in diagnostics.SystemInfo)
                {
                    DiagnosticResults.Add(new DiagnosticResultItem 
                    { 
                        Type = "System", 
                        Message = $"{kvp.Key}: {kvp.Value}" 
                    });
                }

                // Add info messages
                foreach (var info in diagnostics.Info)
                {
                    DiagnosticResults.Add(new DiagnosticResultItem 
                    { 
                        Type = "Info", 
                        Message = info 
                    });
                }

                // Add warnings
                foreach (var warning in diagnostics.Warnings)
                {
                    DiagnosticResults.Add(new DiagnosticResultItem 
                    { 
                        Type = "Warning", 
                        Message = warning 
                    });
                }

                // Add errors
                foreach (var error in diagnostics.Errors)
                {
                    DiagnosticResults.Add(new DiagnosticResultItem 
                    { 
                        Type = "Error", 
                        Message = error 
                    });
                }

                AddLogMessage($"Diagnostics completed. Found {diagnostics.Errors.Count} errors, {diagnostics.Warnings.Count} warnings");
                Status = "Ready";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Diagnostics failed");
                AddLogMessage($"Diagnostics error: {ex.Message}");
                Status = "Error";
            }
        }

        [RelayCommand]
        private async Task RequestPermissionsAsync()
        {
            try
            {
                AddLogMessage("Requesting permissions...");
                var result = await _speechService.RequestPermissionsAsync();
                PermissionStatus = result.ToString();
                AddLogMessage($"Permission request result: {result}");
                
                // Refresh availability
                IsAvailable = _speechService.IsAvailable;
                Status = IsAvailable ? "Ready" : "Not Available";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Permission request failed");
                AddLogMessage($"Permission request error: {ex.Message}");
                Status = "Error";
            }
        }

        [RelayCommand]
        private void ClearLogs()
        {
            LogMessages.Clear();
            AddLogMessage("Logs cleared");
        }

        [RelayCommand]
        private void ClearResults()
        {
            LastRecognizedText = "";
            PartialText = "";
            LastConfidence = 0f;
            AddLogMessage("Results cleared");
        }

        private void OnSpeechRecognized(object? sender, SpeechRecognitionResult e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                LastRecognizedText = e.Text;
                LastConfidence = e.Confidence;
                PartialText = ""; // Clear partial text when we get final result
                
                AddLogMessage($"Final: '{e.Text}' (Confidence: {e.Confidence:P})");
                
                if (e.Alternatives?.Count > 0)
                {
                    AddLogMessage($"Alternatives: {e.Alternatives.Count}");
                    foreach (var alt in e.Alternatives)
                    {
                        AddLogMessage($"  '{alt.Text}' ({alt.Confidence:P})");
                    }
                }
            });
        }

        private void OnPartialResultsReceived(object? sender, string e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                PartialText = e;
                AddLogMessage($"Partial: '{e}'");
            });
        }

        private void OnStateChanged(object? sender, SpeechRecognitionState e)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                CurrentState = e.ToString();
                AddLogMessage($"State: {e}");
                
                // Update listening status
                IsListening = e == SpeechRecognitionState.Listening;
                
                // Update status
                Status = e switch
                {
                    SpeechRecognitionState.Idle => "Ready",
                    SpeechRecognitionState.Starting => "Starting...",
                    SpeechRecognitionState.Listening => IsListening && ContinuousMode ? "Listening (Continuous)" : "Listening",
                    SpeechRecognitionState.Processing => "Processing...",
                    SpeechRecognitionState.Stopping => "Stopping...",
                    SpeechRecognitionState.Error => "Error",
                    _ => e.ToString()
                };
            });
        }

        private void AddLogMessage(string message)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                LogMessages.Add($"[{timestamp}] {message}");
                
                // Keep only last 100 messages
                while (LogMessages.Count > 100)
                {
                    LogMessages.RemoveAt(0);
                }
            });
        }
    }

    public class DiagnosticResultItem
    {
        public string Type { get; set; } = "";
        public string Message { get; set; } = "";
    }
}
