using JarvisAssistant.SpeechTest.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#if ANDROID
using AndroidX.Activity.Result;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Android.Speech;
#endif

namespace JarvisAssistant.SpeechTest.Services
{
    /// <summary>
    /// Android implementation using Android.Speech APIs
    /// </summary>
    public class AndroidSpeechRecognitionService : ISpeechRecognitionService
    {
        private readonly ILogger<AndroidSpeechRecognitionService> _logger;
        private SpeechRecognitionState _currentState = SpeechRecognitionState.Idle;
        private bool _isListening = false;

#if ANDROID
        private Android.Speech.SpeechRecognizer? _speechRecognizer;
        private SpeechRecognitionListener? _listener;
        private TaskCompletionSource<SpeechRecognitionResult>? _recognitionTcs;
        private readonly object _lockObject = new object();
        private Android.Content.Context? _context;
#endif

        public AndroidSpeechRecognitionService(ILogger<AndroidSpeechRecognitionService> logger)
        {
            _logger = logger;
            _logger.LogInformation("AndroidSpeechRecognitionService created");
            
#if ANDROID
            _context = Platform.CurrentActivity ?? Android.App.Application.Context;
            if (_context == null)
            {
                _logger.LogError("No Android context available");
            }
#endif
        }

        public bool IsListening => _isListening;

        public bool IsAvailable
        {
            get
            {
#if ANDROID
                try
                {
                    return _context != null && 
                           Android.Speech.SpeechRecognizer.IsRecognitionAvailable(_context);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking speech recognition availability");
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
#if ANDROID
            try
            {
                // Get available locales from system
                var availableLocales = Java.Util.Locale.GetAvailableLocales();
                var languages = availableLocales
                    .Where(l => !string.IsNullOrEmpty(l.Language))
                    .Select(l => $"{l.Language}-{l.Country}")
                    .Where(l => !string.IsNullOrEmpty(l) && l != "-")
                    .Distinct()
                    .OrderBy(l => l)
                    .ToList();
                
                _logger.LogInformation("Available Android languages: {Count}", languages.Count);
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
            _logger.LogInformation("Checking Android microphone permissions");
            
#if ANDROID
            try
            {
                if (_context == null)
                {
                    _logger.LogError("No Android context available for permission check");
                    return Core.PermissionStatus.Unknown;
                }

                var permission = Android.Manifest.Permission.RecordAudio;
                var status = ContextCompat.CheckSelfPermission(_context, permission);
                
                if (status == Android.Content.PM.Permission.Granted)
                {
                    _logger.LogInformation("Microphone permission already granted");
                    return Core.PermissionStatus.Granted;
                }
                
                // Check if we need to show rationale
                if (_context is AndroidX.AppCompat.App.AppCompatActivity activity)
                {
                    if (ActivityCompat.ShouldShowRequestPermissionRationale(activity, permission))
                    {
                        _logger.LogInformation("Should show permission rationale");
                        return Core.PermissionStatus.Denied;
                    }
                    
                    // Request permission
                    var tcs = new TaskCompletionSource<Core.PermissionStatus>();
                    
                    // Note: In a real implementation, you'd use the MAUI permissions system
                    // or handle the ActivityResult callback properly
                    ActivityCompat.RequestPermissions(activity, new[] { permission }, 1001);
                    
                    // For now, return that permission is required
                    return Core.PermissionStatus.Denied;
                }
                
                return Core.PermissionStatus.Unknown;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permissions");
                return Core.PermissionStatus.Unknown;
            }
#else
            return await Task.FromResult(Core.PermissionStatus.Unknown);
#endif
        }

        public async Task<bool> StartListeningAsync(SpeechRecognitionOptions? options = null)
        {
            if (_isListening)
            {
                _logger.LogWarning("Already listening");
                return false;
            }

            options ??= new SpeechRecognitionOptions();
            _logger.LogInformation("Starting Android speech recognition with options: {Options}", 
                System.Text.Json.JsonSerializer.Serialize(options));

            UpdateState(SpeechRecognitionState.Starting);

#if ANDROID
            return await Task.Run(() =>
            {
                try
                {
                    if (_context == null)
                    {
                        _logger.LogError("No Android context available");
                        UpdateState(SpeechRecognitionState.Error);
                        return false;
                    }

                    lock (_lockObject)
                    {
                        // Check permissions first
                        var permission = ContextCompat.CheckSelfPermission(_context, Android.Manifest.Permission.RecordAudio);
                        if (permission != Android.Content.PM.Permission.Granted)
                        {
                            _logger.LogError("Microphone permission not granted");
                            UpdateState(SpeechRecognitionState.Error);
                            return false;
                        }

                        // Create speech recognizer
                        _speechRecognizer = Android.Speech.SpeechRecognizer.CreateSpeechRecognizer(_context);
                        if (_speechRecognizer == null)
                        {
                            _logger.LogError("Failed to create speech recognizer");
                            UpdateState(SpeechRecognitionState.Error);
                            return false;
                        }

                        // Create and set listener
                        _listener = new SpeechRecognitionListener(_logger, this, options.ContinuousRecognition);
                        _speechRecognizer.SetRecognitionListener(_listener);

                        // Create intent
                        var intent = new Android.Content.Intent(Android.Speech.RecognizerIntent.ActionRecognizeSpeech);
                        intent.PutExtra(Android.Speech.RecognizerIntent.ExtraLanguageModel, 
                                       Android.Speech.RecognizerIntent.LanguageModelFreeForm);
                        intent.PutExtra(Android.Speech.RecognizerIntent.ExtraLanguage, options.Language);
                        intent.PutExtra(Android.Speech.RecognizerIntent.ExtraMaxResults, options.MaxAlternatives);
                        intent.PutExtra(Android.Speech.RecognizerIntent.ExtraPartialResults, true);
                        
                        // Configure additional settings
                        if (options.SilenceTimeout.TotalMilliseconds > 0)
                        {
                            intent.PutExtra(Android.Speech.RecognizerIntent.ExtraSpeechInputCompleteSilenceLengthMillis,
                                           (long)options.SilenceTimeout.TotalMilliseconds);
                        }

                        // Start listening
                        _speechRecognizer.StartListening(intent);
                        _isListening = true;
                        UpdateState(SpeechRecognitionState.Listening);
                        
                        _logger.LogInformation("Android speech recognition started");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to start Android speech recognition");
                    UpdateState(SpeechRecognitionState.Error);
                    CleanupRecognizer();
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

            _logger.LogInformation("Stopping Android speech recognition");
            UpdateState(SpeechRecognitionState.Stopping);

#if ANDROID
            await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    try
                    {
                        if (_speechRecognizer != null)
                        {
                            _speechRecognizer.StopListening();
                            CleanupRecognizer();
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
            _logger.LogInformation("Starting single Android speech recognition");

#if ANDROID
            if (_context == null)
            {
                throw new InvalidOperationException("No Android context available");
            }

            _recognitionTcs = new TaskCompletionSource<SpeechRecognitionResult>();

            try
            {
                UpdateState(SpeechRecognitionState.Starting);

                // Check permissions
                var permission = ContextCompat.CheckSelfPermission(_context, Android.Manifest.Permission.RecordAudio);
                if (permission != Android.Content.PM.Permission.Granted)
                {
                    throw new UnauthorizedAccessException("Microphone permission not granted");
                }

                lock (_lockObject)
                {
                    // Create speech recognizer
                    _speechRecognizer = Android.Speech.SpeechRecognizer.CreateSpeechRecognizer(_context);
                    if (_speechRecognizer == null)
                    {
                        throw new InvalidOperationException("Failed to create speech recognizer");
                    }

                    // Create listener for single recognition
                    _listener = new SpeechRecognitionListener(_logger, this, false);
                    _speechRecognizer.SetRecognitionListener(_listener);

                    // Create intent
                    var intent = new Android.Content.Intent(Android.Speech.RecognizerIntent.ActionRecognizeSpeech);
                    intent.PutExtra(Android.Speech.RecognizerIntent.ExtraLanguageModel, 
                                   Android.Speech.RecognizerIntent.LanguageModelFreeForm);
                    intent.PutExtra(Android.Speech.RecognizerIntent.ExtraLanguage, options.Language);
                    intent.PutExtra(Android.Speech.RecognizerIntent.ExtraMaxResults, options.MaxAlternatives);
                    intent.PutExtra(Android.Speech.RecognizerIntent.ExtraPartialResults, true);

                    UpdateState(SpeechRecognitionState.Listening);
                    _logger.LogInformation("Listening for speech...");

                    // Start listening
                    _speechRecognizer.StartListening(intent);
                }

                // Wait for result with timeout
                var timeout = options.MaxListeningTime ?? TimeSpan.FromSeconds(15);
                using var timeoutCts = new CancellationTokenSource(timeout);
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                var result = await _recognitionTcs.Task.WaitAsync(combinedCts.Token);
                
                UpdateState(SpeechRecognitionState.Idle);
                return result;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Speech recognition cancelled by user");
                UpdateState(SpeechRecognitionState.Idle);
                throw;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Speech recognition timed out");
                UpdateState(SpeechRecognitionState.Idle);
                return new SpeechRecognitionResult
                {
                    Text = "",
                    Confidence = 0f,
                    IsFinal = true,
                    Timestamp = DateTime.UtcNow,
                    Metadata = new Dictionary<string, object>
                    {
                        ["Platform"] = "Android",
                        ["Result"] = "Timeout"
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Android speech recognition failed");
                UpdateState(SpeechRecognitionState.Error);
                throw;
            }
            finally
            {
                CleanupRecognizer();
                _recognitionTcs = null;
            }
#else
            throw new PlatformNotSupportedException("Android speech recognition not available on this platform");
#endif
        }

        public async Task<DiagnosticResult> RunDiagnosticsAsync()
        {
            var result = new DiagnosticResult();
            
            _logger.LogInformation("Running Android speech recognition diagnostics");

            try
            {
                // Test availability
                result.IsAvailable = IsAvailable;
                if (result.IsAvailable)
                {
                    result.Info.Add("✓ Android speech recognition available");
                }
                else
                {
                    result.Errors.Add("✗ Android speech recognition not available");
                }

                // Test permissions
                result.PermissionStatus = await RequestPermissionsAsync();
                result.Info.Add($"Permission status: {result.PermissionStatus}");

                // Get languages
                var languages = await GetAvailableLanguagesAsync();
                result.AvailableLanguages = languages.ToList();
                result.Info.Add($"Available languages: {languages.Count()}");

                // System info
#if ANDROID
                result.SystemInfo["Platform"] = "Android";
                result.SystemInfo["API_Level"] = Android.OS.Build.VERSION.SdkInt.ToString();
                result.SystemInfo["Device"] = $"{Android.OS.Build.Manufacturer} {Android.OS.Build.Model}";
                result.SystemInfo["OS"] = $"Android {Android.OS.Build.VERSION.Release}";

                if (_context != null)
                {
                    result.SystemInfo["Context"] = _context.GetType().Name;
                    result.Info.Add($"Context: {_context.GetType().Name}");

                    // Check package manager
                    var packageManager = _context.PackageManager;
                    if (packageManager != null)
                    {
                        var hasFeature = packageManager.HasSystemFeature(Android.Content.PM.PackageManager.FeatureMicrophone);
                        result.SystemInfo["HasMicrophone"] = hasFeature.ToString();
                        if (hasFeature)
                        {
                            result.Info.Add("✓ Microphone feature available");
                        }
                        else
                        {
                            result.Warnings.Add("⚠ Microphone feature not detected");
                        }
                    }

                    // Test speech recognizer creation
                    try
                    {
                        using var testRecognizer = Android.Speech.SpeechRecognizer.CreateSpeechRecognizer(_context);
                        if (testRecognizer != null)
                        {
                            result.Info.Add("✓ Speech recognizer creation successful");
                        }
                        else
                        {
                            result.Errors.Add("✗ Speech recognizer creation failed");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"✗ Speech recognizer test failed: {ex.Message}");
                    }
                }
                else
                {
                    result.Errors.Add("✗ No Android context available");
                }
#else
                result.SystemInfo["Platform"] = "Non-Android";
                result.Errors.Add("Android speech recognition only available on Android");
#endif

                _logger.LogInformation("Android diagnostics completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Android diagnostics failed");
                result.Errors.Add($"Diagnostics failed: {ex.Message}");
            }

            return result;
        }

#if ANDROID
        internal void OnSpeechRecognized(SpeechRecognitionResult result)
        {
            _logger.LogInformation("Speech recognized: '{Text}' (Confidence: {Confidence:P})", 
                result.Text, result.Confidence);
            
            SpeechRecognized?.Invoke(this, result);
            
            // Complete single recognition task if waiting
            if (_recognitionTcs != null && !_recognitionTcs.Task.IsCompleted)
            {
                _recognitionTcs.SetResult(result);
            }
        }

        internal void OnPartialResult(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                _logger.LogDebug("Partial result: '{Text}'", text);
                PartialResultsReceived?.Invoke(this, text);
            }
        }

        internal void OnRecognitionError(Android.Speech.SpeechRecognizerError error, string message)
        {
            _logger.LogError("Recognition error: {Error} - {Message}", error, message);
            
            if (_recognitionTcs != null && !_recognitionTcs.Task.IsCompleted)
            {
                _recognitionTcs.SetException(new InvalidOperationException($"Recognition error: {error} - {message}"));
            }
            
            _isListening = false;
            UpdateState(SpeechRecognitionState.Error);
        }

        internal void OnRecognitionComplete()
        {
            _logger.LogDebug("Recognition completed");
            _isListening = false;
            UpdateState(SpeechRecognitionState.Idle);
        }

        private void CleanupRecognizer()
        {
            if (_speechRecognizer != null)
            {
                try
                {
                    _speechRecognizer.Destroy();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error destroying speech recognizer");
                }
                finally
                {
                    _speechRecognizer = null;
                    _listener = null;
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

        public void Dispose()
        {
#if ANDROID
            CleanupRecognizer();
#endif
        }
    }

#if ANDROID
    /// <summary>
    /// Custom recognition listener for Android speech recognition
    /// </summary>
    internal class SpeechRecognitionListener : Java.Lang.Object, Android.Speech.IRecognitionListener
    {
        private readonly ILogger _logger;
        private readonly AndroidSpeechRecognitionService _service;
        private readonly bool _isContinuous;

        public SpeechRecognitionListener(ILogger logger, AndroidSpeechRecognitionService service, bool isContinuous)
        {
            _logger = logger;
            _service = service;
            _isContinuous = isContinuous;
        }

        public void OnBeginningOfSpeech()
        {
            _logger.LogDebug("Beginning of speech detected");
        }

        public void OnBufferReceived(byte[]? buffer)
        {
            _logger.LogTrace("Audio buffer received: {Size} bytes", buffer?.Length ?? 0);
        }

        public void OnEndOfSpeech()
        {
            _logger.LogDebug("End of speech detected");
        }

        public void OnError(Android.Speech.SpeechRecognizerError error)
        {
            var message = error switch
            {
                Android.Speech.SpeechRecognizerError.Audio => "Audio recording error",
                Android.Speech.SpeechRecognizerError.Client => "Client side error",
                Android.Speech.SpeechRecognizerError.InsufficientPermissions => "Insufficient permissions",
                Android.Speech.SpeechRecognizerError.Network => "Network error",
                Android.Speech.SpeechRecognizerError.NetworkTimeout => "Network timeout",
                Android.Speech.SpeechRecognizerError.NoMatch => "No speech input",
                Android.Speech.SpeechRecognizerError.RecognizerBusy => "Recognizer busy",
                Android.Speech.SpeechRecognizerError.Server => "Server error",
                Android.Speech.SpeechRecognizerError.SpeechTimeout => "Speech timeout",
                _ => "Unknown error"
            };

            _service.OnRecognitionError(error, message);
        }

        public void OnEvent(int eventType, Android.OS.Bundle? @params)
        {
            _logger.LogTrace("Recognition event: {EventType}", eventType);
        }

        public void OnPartialResults(Android.OS.Bundle? partialResults)
        {
            if (partialResults?.GetStringArrayList(Android.Speech.SpeechRecognizer.ResultsRecognition) is { } results &&
                results.Count > 0 && !string.IsNullOrEmpty(results[0]))
            {
                _service.OnPartialResult(results[0]);
            }
        }

        public void OnReadyForSpeech(Android.OS.Bundle? @params)
        {
            _logger.LogDebug("Ready for speech");
        }

        public void OnResults(Android.OS.Bundle? results)
        {
            if (results?.GetStringArrayList(Android.Speech.SpeechRecognizer.ResultsRecognition) is { } texts &&
                results.GetFloatArray(Android.Speech.SpeechRecognizer.ConfidenceScores) is { } scores)
            {
                if (texts.Count > 0 && !string.IsNullOrEmpty(texts[0]))
                {
                    var confidence = scores.Length > 0 ? scores[0] : 0.5f;
                    
                    var result = new SpeechRecognitionResult
                    {
                        Text = texts[0],
                        Confidence = confidence,
                        IsFinal = true,
                        Timestamp = DateTime.UtcNow,
                        Metadata = new Dictionary<string, object>
                        {
                            ["Platform"] = "Android",
                            ["ResultCount"] = texts.Count
                        }
                    };

                    // Add alternatives
                    var alternatives = new List<SpeechRecognitionAlternative>();
                    for (int i = 1; i < Math.Min(texts.Count, scores.Length); i++)
                    {
                        if (!string.IsNullOrEmpty(texts[i]))
                        {
                            alternatives.Add(new SpeechRecognitionAlternative
                            {
                                Text = texts[i],
                                Confidence = scores[i]
                            });
                        }
                    }
                    result.Alternatives = alternatives;

                    _service.OnSpeechRecognized(result);
                }
            }

            _service.OnRecognitionComplete();
        }

        public void OnRmsChanged(float rmsdB)
        {
            _logger.LogTrace("RMS changed: {RmsDb}", rmsdB);
        }
    }
#endif
}
