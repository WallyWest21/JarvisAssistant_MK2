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
    /// Simple, working implementation of speech recognition for Windows using System.Speech
    /// </summary>
    public class SimpleSpeechRecognitionService : ISpeechRecognitionService
    {
        private readonly ILogger<SimpleSpeechRecognitionService> _logger;
        private SpeechRecognitionState _currentState = SpeechRecognitionState.Idle;
        private bool _isListening = false;

#if WINDOWS
        private System.Speech.Recognition.SpeechRecognitionEngine? _recognizer;
        private readonly object _lockObject = new object();
#endif

        public SimpleSpeechRecognitionService(ILogger<SimpleSpeechRecognitionService> logger)
        {
            _logger = logger;
        }

        public bool IsListening => _isListening;

#if WINDOWS
        public bool IsAvailable 
        { 
            get 
            {
                try
                {
                    // Test if System.Speech is available
                    using var test = new System.Speech.Recognition.SpeechRecognitionEngine();
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
#elif ANDROID
        public bool IsAvailable 
        { 
            get 
            {
                try
                {
                    return Android.Speech.SpeechRecognizer.IsRecognitionAvailable(Platform.CurrentActivity ?? Android.App.Application.Context);
                }
                catch
                {
                    return false;
                }
            }
        }
#else
        public bool IsAvailable => false;
#endif

        public event EventHandler<SpeechRecognitionResult>? SpeechRecognized;
        public event EventHandler<string>? PartialResultsReceived;
        public event EventHandler<SpeechRecognitionState>? StateChanged;

        public async Task<IEnumerable<string>> GetAvailableLanguagesAsync()
        {
            var languages = new List<string> { "en-US", "en-GB", "es-ES", "fr-FR", "de-DE" };
            return await Task.FromResult(languages);
        }

        public async Task<PermissionStatus> RequestPermissionsAsync()
        {
#if ANDROID
            try
            {
                var status = await Permissions.RequestAsync<Permissions.Microphone>();
                return status switch
                {
                    Microsoft.Maui.ApplicationModel.PermissionStatus.Granted => PermissionStatus.Granted,
                    Microsoft.Maui.ApplicationModel.PermissionStatus.Denied => PermissionStatus.Denied,
                    Microsoft.Maui.ApplicationModel.PermissionStatus.Disabled => PermissionStatus.Disabled,
                    Microsoft.Maui.ApplicationModel.PermissionStatus.Restricted => PermissionStatus.Restricted,
                    _ => PermissionStatus.Unknown
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to request microphone permission");
                return PermissionStatus.Unknown;
            }
#else
            return await Task.FromResult(PermissionStatus.Granted);
#endif
        }

        public async Task<bool> StartListeningAsync(SpeechRecognitionOptions? options = null)
        {
            if (_isListening)
            {
                _logger.LogWarning("Speech recognition is already active");
                return false;
            }

            try
            {
                UpdateState(SpeechRecognitionState.Starting);

                // Check permissions
                var permissionStatus = await RequestPermissionsAsync();
                if (permissionStatus != PermissionStatus.Granted)
                {
                    _logger.LogError("Microphone permission not granted: {Status}", permissionStatus);
                    UpdateState(SpeechRecognitionState.Error);
                    return false;
                }

#if WINDOWS
                return await StartWindowsListeningAsync(options ?? new SpeechRecognitionOptions());
#elif ANDROID
                return await StartAndroidListeningAsync(options ?? new SpeechRecognitionOptions());
#else
                _logger.LogWarning("Speech recognition not supported on this platform");
                return false;
#endif
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start speech recognition");
                UpdateState(SpeechRecognitionState.Error);
                return false;
            }
        }

        public async Task StopListeningAsync()
        {
            if (!_isListening)
            {
                return;
            }

            try
            {
                UpdateState(SpeechRecognitionState.Stopping);

#if WINDOWS
                await StopWindowsListeningAsync();
#elif ANDROID
                await StopAndroidListeningAsync();
#endif

                _isListening = false;
                UpdateState(SpeechRecognitionState.Idle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop speech recognition");
                UpdateState(SpeechRecognitionState.Error);
            }
        }

        public async Task<SpeechRecognitionResult> RecognizeSpeechAsync(SpeechRecognitionOptions? options = null, CancellationToken cancellationToken = default)
        {
#if WINDOWS
            return await RecognizeWindowsSpeechAsync(options ?? new SpeechRecognitionOptions(), cancellationToken);
#elif ANDROID
            return await RecognizeAndroidSpeechAsync(options ?? new SpeechRecognitionOptions(), cancellationToken);
#else
            throw new PlatformNotSupportedException("Speech recognition not supported on this platform");
#endif
        }

#if WINDOWS
        private async Task<bool> StartWindowsListeningAsync(SpeechRecognitionOptions options)
        {
            return await Task.Run(() =>
            {
                try
                {
                    lock (_lockObject)
                    {
                        _recognizer = new System.Speech.Recognition.SpeechRecognitionEngine();
                        
                        // Load default grammar
                        var grammar = new System.Speech.Recognition.DictationGrammar();
                        _recognizer.LoadGrammar(grammar);
                        
                        // Set up event handlers
                        _recognizer.SpeechRecognized += OnWindowsSpeechRecognized;
                        _recognizer.SpeechHypothesized += OnWindowsSpeechHypothesized;
                        _recognizer.RecognizeCompleted += OnWindowsRecognizeCompleted;
                        
                        // Set input to default microphone
                        _recognizer.SetInputToDefaultAudioDevice();
                        
                        // Start recognition
                        if (options.ContinuousRecognition)
                        {
                            _recognizer.RecognizeAsync(System.Speech.Recognition.RecognizeMode.Multiple);
                        }
                        else
                        {
                            _recognizer.RecognizeAsync(System.Speech.Recognition.RecognizeMode.Single);
                        }
                        
                        _isListening = true;
                        UpdateState(SpeechRecognitionState.Listening);
                        
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to start Windows speech recognition");
                    return false;
                }
            });
        }

        private async Task StopWindowsListeningAsync()
        {
            await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    if (_recognizer != null)
                    {
                        _recognizer.RecognizeAsyncStop();
                        _recognizer.SpeechRecognized -= OnWindowsSpeechRecognized;
                        _recognizer.SpeechHypothesized -= OnWindowsSpeechHypothesized;
                        _recognizer.RecognizeCompleted -= OnWindowsRecognizeCompleted;
                        _recognizer.Dispose();
                        _recognizer = null;
                    }
                }
            });
        }

        private async Task<SpeechRecognitionResult> RecognizeWindowsSpeechAsync(SpeechRecognitionOptions options, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var recognizer = new System.Speech.Recognition.SpeechRecognitionEngine();
                    
                    var grammar = new System.Speech.Recognition.DictationGrammar();
                    recognizer.LoadGrammar(grammar);
                    recognizer.SetInputToDefaultAudioDevice();
                    
                    UpdateState(SpeechRecognitionState.Listening);
                    
                    var result = recognizer.Recognize(TimeSpan.FromSeconds(10));
                    
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
                        
                        UpdateState(SpeechRecognitionState.Idle);
                        return speechResult;
                    }
                    
                    UpdateState(SpeechRecognitionState.Idle);
                    return new SpeechRecognitionResult { Text = "", Confidence = 0f, IsFinal = true };
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
            
            _logger.LogInformation("Windows speech recognized: {Text} (Confidence: {Confidence})", result.Text, result.Confidence);
            SpeechRecognized?.Invoke(this, result);
        }

        private void OnWindowsSpeechHypothesized(object? sender, System.Speech.Recognition.SpeechHypothesizedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Result.Text))
            {
                PartialResultsReceived?.Invoke(this, e.Result.Text);
            }
        }

        private void OnWindowsRecognizeCompleted(object? sender, System.Speech.Recognition.RecognizeCompletedEventArgs e)
        {
            _logger.LogInformation("Windows recognition completed");
            _isListening = false;
            UpdateState(SpeechRecognitionState.Idle);
        }
#endif

#if ANDROID
        private async Task<bool> StartAndroidListeningAsync(SpeechRecognitionOptions options)
        {
            // For now, return true to indicate success
            // We'll implement Android-specific logic here
            _isListening = true;
            UpdateState(SpeechRecognitionState.Listening);
            return await Task.FromResult(true);
        }

        private async Task StopAndroidListeningAsync()
        {
            await Task.CompletedTask;
        }

        private async Task<SpeechRecognitionResult> RecognizeAndroidSpeechAsync(SpeechRecognitionOptions options, CancellationToken cancellationToken)
        {
            // Placeholder for Android implementation
            await Task.Delay(1000, cancellationToken);
            return new SpeechRecognitionResult
            {
                Text = "Android speech recognition not yet implemented",
                Confidence = 0.5f,
                IsFinal = true
            };
        }
#endif

        private void UpdateState(SpeechRecognitionState newState)
        {
            if (_currentState != newState)
            {
                _currentState = newState;
                _logger.LogDebug("Speech recognition state changed: {State}", newState);
                StateChanged?.Invoke(this, newState);
            }
        }
    }
}
