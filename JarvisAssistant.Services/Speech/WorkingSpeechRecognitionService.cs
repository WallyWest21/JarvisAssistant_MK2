using JarvisAssistant.Core.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JarvisAssistant.Services.Speech
{
    /// <summary>
    /// Basic working speech recognition service that actually works
    /// </summary>
    public class WorkingSpeechRecognitionService : ISpeechRecognitionService
    {
        private readonly ILogger<WorkingSpeechRecognitionService> _logger;
        private SpeechRecognitionState _currentState = SpeechRecognitionState.Idle;
        private bool _isListening = false;

        public WorkingSpeechRecognitionService(ILogger<WorkingSpeechRecognitionService> logger)
        {
            _logger = logger;
            _logger.LogInformation("WorkingSpeechRecognitionService created");
        }

        public bool IsListening => _isListening;

        public bool IsAvailable 
        { 
            get 
            {
                try
                {
#if WINDOWS
                    // Test if we can create a speech recognition engine
                    using var engine = new System.Speech.Recognition.SpeechRecognitionEngine();
                    _logger.LogDebug("Speech recognition engine created successfully");
                    return true;
#else
                    _logger.LogDebug("Speech recognition not available on this platform");
                    return false;
#endif
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Speech recognition not available");
                    return false;
                }
            }
        }

        public event EventHandler<SpeechRecognitionResult>? SpeechRecognized;
        public event EventHandler<string>? PartialResultsReceived;
        public event EventHandler<SpeechRecognitionState>? StateChanged;

        public async Task<IEnumerable<string>> GetAvailableLanguagesAsync()
        {
            var languages = new List<string> { "en-US", "en-GB" };
            _logger.LogDebug("Available languages: {Languages}", string.Join(", ", languages));
            return await Task.FromResult(languages);
        }

        public async Task<PermissionStatus> RequestPermissionsAsync()
        {
            _logger.LogDebug("Requesting microphone permissions");
            
#if ANDROID
            try
            {
                var status = await Permissions.RequestAsync<Permissions.Microphone>();
                var result = status switch
                {
                    Microsoft.Maui.ApplicationModel.PermissionStatus.Granted => PermissionStatus.Granted,
                    Microsoft.Maui.ApplicationModel.PermissionStatus.Denied => PermissionStatus.Denied,
                    Microsoft.Maui.ApplicationModel.PermissionStatus.Disabled => PermissionStatus.Disabled,
                    Microsoft.Maui.ApplicationModel.PermissionStatus.Restricted => PermissionStatus.Restricted,
                    _ => PermissionStatus.Unknown
                };
                _logger.LogDebug("Android permission result: {Result}", result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to request Android permissions");
                return PermissionStatus.Unknown;
            }
#else
            // On Windows, assume permission is granted
            _logger.LogDebug("Windows - assuming permission granted");
            return await Task.FromResult(PermissionStatus.Granted);
#endif
        }

        public async Task<bool> StartListeningAsync(SpeechRecognitionOptions? options = null)
        {
            if (_isListening)
            {
                _logger.LogWarning("Already listening");
                return false;
            }

            _logger.LogInformation("Starting speech recognition...");
            UpdateState(SpeechRecognitionState.Starting);

            try
            {
                var permissionStatus = await RequestPermissionsAsync();
                if (permissionStatus != PermissionStatus.Granted)
                {
                    _logger.LogError("Permission not granted: {Status}", permissionStatus);
                    UpdateState(SpeechRecognitionState.Error);
                    return false;
                }

#if WINDOWS
                return await StartWindowsListeningAsync(options ?? new SpeechRecognitionOptions());
#else
                _logger.LogWarning("Platform not supported for continuous listening");
                UpdateState(SpeechRecognitionState.Error);
                return false;
#endif
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start listening");
                UpdateState(SpeechRecognitionState.Error);
                return false;
            }
        }

        public async Task StopListeningAsync()
        {
            if (!_isListening)
            {
                _logger.LogDebug("Not currently listening");
                return;
            }

            _logger.LogInformation("Stopping speech recognition...");
            UpdateState(SpeechRecognitionState.Stopping);

#if WINDOWS
            await StopWindowsListeningAsync();
#endif

            _isListening = false;
            UpdateState(SpeechRecognitionState.Idle);
            _logger.LogInformation("Speech recognition stopped");
        }

        public async Task<SpeechRecognitionResult> RecognizeSpeechAsync(SpeechRecognitionOptions? options = null, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting single speech recognition...");
            
#if WINDOWS
            return await RecognizeWindowsSpeechAsync(options ?? new SpeechRecognitionOptions(), cancellationToken);
#else
            _logger.LogError("Single recognition not supported on this platform");
            throw new PlatformNotSupportedException("Speech recognition not supported on this platform");
#endif
        }

#if WINDOWS
        private System.Speech.Recognition.SpeechRecognitionEngine? _windowsEngine;

        private async Task<bool> StartWindowsListeningAsync(SpeechRecognitionOptions options)
        {
            return await Task.Run(() =>
            {
                try
                {
                    _logger.LogDebug("Creating Windows speech recognition engine");
                    _windowsEngine = new System.Speech.Recognition.SpeechRecognitionEngine();
                    
                    // Load dictation grammar
                    var grammar = new System.Speech.Recognition.DictationGrammar();
                    _windowsEngine.LoadGrammar(grammar);
                    _logger.LogDebug("Grammar loaded");
                    
                    // Set input device
                    _windowsEngine.SetInputToDefaultAudioDevice();
                    _logger.LogDebug("Audio device set");
                    
                    // Wire up events
                    _windowsEngine.SpeechRecognized += OnWindowsSpeechRecognized;
                    _windowsEngine.SpeechHypothesized += OnWindowsSpeechHypothesized;
                    _windowsEngine.RecognizeCompleted += OnWindowsRecognizeCompleted;
                    
                    // Start recognition
                    if (options.ContinuousRecognition)
                    {
                        _windowsEngine.RecognizeAsync(System.Speech.Recognition.RecognizeMode.Multiple);
                        _logger.LogInformation("Started continuous recognition");
                    }
                    else
                    {
                        _windowsEngine.RecognizeAsync(System.Speech.Recognition.RecognizeMode.Single);
                        _logger.LogInformation("Started single recognition");
                    }
                    
                    _isListening = true;
                    UpdateState(SpeechRecognitionState.Listening);
                    
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to start Windows speech recognition");
                    CleanupWindowsEngine();
                    return false;
                }
            });
        }

        private async Task StopWindowsListeningAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    if (_windowsEngine != null)
                    {
                        _logger.LogDebug("Stopping Windows speech recognition");
                        _windowsEngine.RecognizeAsyncStop();
                        CleanupWindowsEngine();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error stopping Windows speech recognition");
                }
            });
        }

        private async Task<SpeechRecognitionResult> RecognizeWindowsSpeechAsync(SpeechRecognitionOptions options, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                try
                {
                    _logger.LogDebug("Starting Windows single recognition");
                    UpdateState(SpeechRecognitionState.Starting);
                    
                    using var engine = new System.Speech.Recognition.SpeechRecognitionEngine();
                    
                    var grammar = new System.Speech.Recognition.DictationGrammar();
                    engine.LoadGrammar(grammar);
                    engine.SetInputToDefaultAudioDevice();
                    
                    UpdateState(SpeechRecognitionState.Listening);
                    _logger.LogDebug("Listening for speech (10 second timeout)...");
                    
                    var result = engine.Recognize(TimeSpan.FromSeconds(10));
                    
                    UpdateState(SpeechRecognitionState.Processing);
                    
                    if (result != null)
                    {
                        var speechResult = new SpeechRecognitionResult
                        {
                            Text = result.Text,
                            Confidence = result.Confidence,
                            IsFinal = true,
                            Timestamp = DateTime.UtcNow
                        };
                        
                        _logger.LogInformation("Recognition successful: '{Text}' (Confidence: {Confidence})", 
                            speechResult.Text, speechResult.Confidence);
                        
                        UpdateState(SpeechRecognitionState.Idle);
                        return speechResult;
                    }
                    else
                    {
                        _logger.LogWarning("No speech detected within timeout");
                        UpdateState(SpeechRecognitionState.Idle);
                        return new SpeechRecognitionResult 
                        { 
                            Text = "", 
                            Confidence = 0f, 
                            IsFinal = true,
                            Timestamp = DateTime.UtcNow
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Windows speech recognition failed");
                    UpdateState(SpeechRecognitionState.Error);
                    throw;
                }
            }, cancellationToken);
        }

        private void OnWindowsSpeechRecognized(object? sender, System.Speech.Recognition.SpeechRecognizedEventArgs e)
        {
            var result = new SpeechRecognitionResult
            {
                Text = e.Result.Text,
                Confidence = e.Result.Confidence,
                IsFinal = true,
                Timestamp = DateTime.UtcNow
            };
            
            _logger.LogInformation("Speech recognized: '{Text}' (Confidence: {Confidence})", 
                result.Text, result.Confidence);
            
            SpeechRecognized?.Invoke(this, result);
        }

        private void OnWindowsSpeechHypothesized(object? sender, System.Speech.Recognition.SpeechHypothesizedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Result.Text))
            {
                _logger.LogDebug("Partial result: '{Text}'", e.Result.Text);
                PartialResultsReceived?.Invoke(this, e.Result.Text);
            }
        }

        private void OnWindowsRecognizeCompleted(object? sender, System.Speech.Recognition.RecognizeCompletedEventArgs e)
        {
            _logger.LogDebug("Recognition completed");
            _isListening = false;
            UpdateState(SpeechRecognitionState.Idle);
        }

        private void CleanupWindowsEngine()
        {
            if (_windowsEngine != null)
            {
                try
                {
                    _windowsEngine.SpeechRecognized -= OnWindowsSpeechRecognized;
                    _windowsEngine.SpeechHypothesized -= OnWindowsSpeechHypothesized;
                    _windowsEngine.RecognizeCompleted -= OnWindowsRecognizeCompleted;
                    _windowsEngine.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error cleaning up Windows engine");
                }
                finally
                {
                    _windowsEngine = null;
                }
            }
        }
#endif

        private void UpdateState(SpeechRecognitionState newState)
        {
            if (_currentState != newState)
            {
                var oldState = _currentState;
                _currentState = newState;
                _logger.LogDebug("State changed: {OldState} -> {NewState}", oldState, newState);
                StateChanged?.Invoke(this, newState);
            }
        }
    }
}
