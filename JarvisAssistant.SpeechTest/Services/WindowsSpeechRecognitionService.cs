using JarvisAssistant.SpeechTest.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MauiPermissionStatus = Microsoft.Maui.ApplicationModel.PermissionStatus;

namespace JarvisAssistant.SpeechTest.Services
{
    /// <summary>
    /// Windows implementation using System.Speech
    /// </summary>
    public class WindowsSpeechRecognitionService : ISpeechRecognitionService
    {
        private readonly ILogger<WindowsSpeechRecognitionService> _logger;
        private SpeechRecognitionState _currentState = SpeechRecognitionState.Idle;
        private bool _isListening = false;

#if WINDOWS
        private System.Speech.Recognition.SpeechRecognitionEngine? _engine;
        private readonly object _lockObject = new object();
#endif

        public WindowsSpeechRecognitionService(ILogger<WindowsSpeechRecognitionService> logger)
        {
            _logger = logger;
            _logger.LogInformation("WindowsSpeechRecognitionService created");
        }

        public bool IsListening => _isListening;

        public bool IsAvailable
        {
            get
            {
#if WINDOWS
                try
                {
                    using var testEngine = new System.Speech.Recognition.SpeechRecognitionEngine();
                    _logger.LogDebug("System.Speech engine test successful");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "System.Speech engine not available");
                    return false;
                }
#else
                return false;
#endif
            }
        }

        public event EventHandler<SpeechRecognitionResult>? SpeechRecognized;
        public event EventHandler<string>? PartialResultsReceived;
        public event EventHandler<SpeechRecognitionState>? StateChanged;

        public async Task<IEnumerable<string>> GetAvailableLanguagesAsync()
        {
#if WINDOWS
            try
            {
                // Get installed recognizers
                var recognizers = System.Speech.Recognition.SpeechRecognitionEngine.InstalledRecognizers();
                var languages = recognizers.Select(r => r.Culture.Name).Distinct().ToList();
                _logger.LogInformation("Available languages: {Languages}", string.Join(", ", languages));
                return await Task.FromResult(languages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get available languages");
                return new[] { "en-US" };
            }
#else
            return await Task.FromResult(new[] { "en-US" });
#endif
        }

        public async Task<Core.PermissionStatus> RequestPermissionsAsync()
        {
            _logger.LogInformation("Checking Windows microphone permissions");
            
            // On Windows, we assume permission is granted if we can create the engine
            if (IsAvailable)
            {
                return await Task.FromResult(Core.PermissionStatus.Granted);
            }
            
            return await Task.FromResult(Core.PermissionStatus.Unknown);
        }

        public async Task<bool> StartListeningAsync(SpeechRecognitionOptions? options = null)
        {
            if (_isListening)
            {
                _logger.LogWarning("Already listening");
                return false;
            }

            options ??= new SpeechRecognitionOptions();
            _logger.LogInformation("Starting Windows speech recognition with options: {Options}", 
                System.Text.Json.JsonSerializer.Serialize(options));

            UpdateState(SpeechRecognitionState.Starting);

#if WINDOWS
            return await Task.Run(() =>
            {
                try
                {
                    lock (_lockObject)
                    {
                        _engine = new System.Speech.Recognition.SpeechRecognitionEngine();
                        
                        // Load grammar
                        var grammar = new System.Speech.Recognition.DictationGrammar();
                        _engine.LoadGrammar(grammar);
                        _logger.LogDebug("Grammar loaded");
                        
                        // Set input device
                        _engine.SetInputToDefaultAudioDevice();
                        _logger.LogDebug("Audio device set");
                        
                        // Configure timeouts
                        _engine.InitialSilenceTimeout = options.SilenceTimeout;
                        _engine.BabbleTimeout = TimeSpan.FromSeconds(5);
                        _engine.EndSilenceTimeout = options.SilenceTimeout;
                        
                        // Wire up events
                        _engine.SpeechRecognized += OnSpeechRecognized;
                        _engine.SpeechHypothesized += OnSpeechHypothesized;
                        _engine.RecognizeCompleted += OnRecognizeCompleted;
                        _engine.AudioLevelUpdated += OnAudioLevelUpdated;
                        _engine.SpeechDetected += OnSpeechDetected;
                        
                        // Start recognition
                        if (options.ContinuousRecognition)
                        {
                            _engine.RecognizeAsync(System.Speech.Recognition.RecognizeMode.Multiple);
                            _logger.LogInformation("Started continuous recognition");
                        }
                        else
                        {
                            _engine.RecognizeAsync(System.Speech.Recognition.RecognizeMode.Single);
                            _logger.LogInformation("Started single recognition");
                        }
                        
                        _isListening = true;
                        UpdateState(SpeechRecognitionState.Listening);
                        
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to start Windows speech recognition");
                    UpdateState(SpeechRecognitionState.Error);
                    CleanupEngine();
                    return false;
                }
            });
#else
            UpdateState(SpeechRecognitionState.Error);
            return await Task.FromResult(false);
#endif
        }

        public async Task StopListeningAsync()
        {
            if (!_isListening)
            {
                return;
            }

            _logger.LogInformation("Stopping Windows speech recognition");
            UpdateState(SpeechRecognitionState.Stopping);

#if WINDOWS
            await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    try
                    {
                        if (_engine != null)
                        {
                            _engine.RecognizeAsyncStop();
                            CleanupEngine();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error stopping recognition");
                    }
                }
            });
#endif

            _isListening = false;
            UpdateState(SpeechRecognitionState.Idle);
        }

        public async Task<SpeechRecognitionResult> RecognizeSpeechAsync(SpeechRecognitionOptions? options = null, CancellationToken cancellationToken = default)
        {
            options ??= new SpeechRecognitionOptions();
            _logger.LogInformation("Starting single speech recognition");

#if WINDOWS
            return await Task.Run(() =>
            {
                try
                {
                    UpdateState(SpeechRecognitionState.Starting);
                    
                    using var engine = new System.Speech.Recognition.SpeechRecognitionEngine();
                    
                    var grammar = new System.Speech.Recognition.DictationGrammar();
                    engine.LoadGrammar(grammar);
                    engine.SetInputToDefaultAudioDevice();
                    
                    // Configure timeouts
                    engine.InitialSilenceTimeout = options.SilenceTimeout;
                    engine.EndSilenceTimeout = options.SilenceTimeout;
                    
                    UpdateState(SpeechRecognitionState.Listening);
                    _logger.LogInformation("Listening for speech (timeout: {Timeout})...", options.SilenceTimeout);
                    
                    var timeout = options.MaxListeningTime ?? TimeSpan.FromSeconds(15);
                    var result = engine.Recognize(timeout);
                    
                    UpdateState(SpeechRecognitionState.Processing);
                    
                    if (result != null)
                    {
                        var speechResult = new SpeechRecognitionResult
                        {
                            Text = result.Text,
                            Confidence = result.Confidence,
                            IsFinal = true,
                            Timestamp = DateTime.UtcNow,
                            Metadata = new Dictionary<string, object>
                            {
                                ["Platform"] = "Windows",
                                ["Engine"] = "System.Speech",
                                ["Grammar"] = result.Grammar?.Name ?? "Unknown"
                            }
                        };
                        
                        // Add alternatives if available - get the default alternates collection
                        var alternates = result.Alternates;
                        var alternatesList = new List<SpeechRecognitionAlternative>();
                        
                        foreach (var alternate in alternates.Take(options.MaxAlternatives))
                        {
                            alternatesList.Add(new SpeechRecognitionAlternative
                            {
                                Text = alternate.Text,
                                Confidence = alternate.Confidence
                            });
                        }
                        speechResult.Alternatives = alternatesList;
                        
                        _logger.LogInformation("Recognition successful: '{Text}' (Confidence: {Confidence:P})", 
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
                            Timestamp = DateTime.UtcNow,
                            Metadata = new Dictionary<string, object>
                            {
                                ["Platform"] = "Windows",
                                ["Engine"] = "System.Speech",
                                ["Result"] = "Timeout"
                            }
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
#else
            throw new PlatformNotSupportedException("Windows speech recognition not available on this platform");
#endif
        }

        public async Task<DiagnosticResult> RunDiagnosticsAsync()
        {
            var result = new DiagnosticResult();
            
            _logger.LogInformation("Running Windows speech recognition diagnostics");

            try
            {
                // Test availability
                result.IsAvailable = IsAvailable;
                if (result.IsAvailable)
                {
                    result.Info.Add("✓ System.Speech engine available");
                }
                else
                {
                    result.Errors.Add("✗ System.Speech engine not available");
                }

                // Test permissions
                result.PermissionStatus = await RequestPermissionsAsync();
                result.Info.Add($"Permission status: {result.PermissionStatus}");

                // Get languages
                var languages = await GetAvailableLanguagesAsync();
                result.AvailableLanguages = languages.ToList();
                result.Info.Add($"Available languages: {string.Join(", ", languages)}");

                // System info
#if WINDOWS
                result.SystemInfo["Platform"] = "Windows";
                result.SystemInfo["Engine"] = "System.Speech";
                result.SystemInfo["OS"] = Environment.OSVersion.ToString();
                
                try
                {
                    var recognizers = System.Speech.Recognition.SpeechRecognitionEngine.InstalledRecognizers();
                    result.SystemInfo["InstalledRecognizers"] = recognizers.Count.ToString();
                    result.Info.Add($"Installed recognizers: {recognizers.Count}");
                    
                    foreach (var recognizer in recognizers)
                    {
                        result.Info.Add($"  - {recognizer.Description} ({recognizer.Culture.Name})");
                    }
                }
                catch (Exception ex)
                {
                    result.Warnings.Add($"Could not enumerate recognizers: {ex.Message}");
                }

                // Test audio device
                try
                {
                    using var testEngine = new System.Speech.Recognition.SpeechRecognitionEngine();
                    testEngine.SetInputToDefaultAudioDevice();
                    result.Info.Add("✓ Default audio device accessible");
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"✗ Default audio device error: {ex.Message}");
                }
#else
                result.SystemInfo["Platform"] = "Non-Windows";
                result.Errors.Add("System.Speech only available on Windows");
#endif

                _logger.LogInformation("Diagnostics completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Diagnostics failed");
                result.Errors.Add($"Diagnostics failed: {ex.Message}");
            }

            return result;
        }

#if WINDOWS
        private void OnSpeechRecognized(object? sender, System.Speech.Recognition.SpeechRecognizedEventArgs e)
        {
            var result = new SpeechRecognitionResult
            {
                Text = e.Result.Text,
                Confidence = e.Result.Confidence,
                IsFinal = true,
                Timestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["Platform"] = "Windows",
                    ["Engine"] = "System.Speech",
                    ["Grammar"] = e.Result.Grammar?.Name ?? "Unknown"
                }
            };
            
            _logger.LogInformation("Speech recognized: '{Text}' (Confidence: {Confidence:P})", 
                result.Text, result.Confidence);
            
            SpeechRecognized?.Invoke(this, result);
        }

        private void OnSpeechHypothesized(object? sender, System.Speech.Recognition.SpeechHypothesizedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Result.Text))
            {
                _logger.LogDebug("Partial result: '{Text}'", e.Result.Text);
                PartialResultsReceived?.Invoke(this, e.Result.Text);
            }
        }

        private void OnRecognizeCompleted(object? sender, System.Speech.Recognition.RecognizeCompletedEventArgs e)
        {
            _logger.LogDebug("Recognition completed. Result: {Result}, Error: {Error}", 
                e.Result?.ToString() ?? "None", e.Error?.Message ?? "None");
            
            if (!_isListening)
            {
                return;
            }
            
            _isListening = false;
            UpdateState(SpeechRecognitionState.Idle);
        }

        private void OnAudioLevelUpdated(object? sender, System.Speech.Recognition.AudioLevelUpdatedEventArgs e)
        {
            _logger.LogTrace("Audio level: {Level}", e.AudioLevel);
        }

        private void OnSpeechDetected(object? sender, System.Speech.Recognition.SpeechDetectedEventArgs e)
        {
            _logger.LogDebug("Speech detected at position: {Position}", e.AudioPosition);
        }

        private void CleanupEngine()
        {
            if (_engine != null)
            {
                try
                {
                    _engine.SpeechRecognized -= OnSpeechRecognized;
                    _engine.SpeechHypothesized -= OnSpeechHypothesized;
                    _engine.RecognizeCompleted -= OnRecognizeCompleted;
                    _engine.AudioLevelUpdated -= OnAudioLevelUpdated;
                    _engine.SpeechDetected -= OnSpeechDetected;
                    _engine.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disposing engine");
                }
                finally
                {
                    _engine = null;
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
