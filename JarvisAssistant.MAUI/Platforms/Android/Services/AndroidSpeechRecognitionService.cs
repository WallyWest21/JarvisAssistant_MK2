#if ANDROID
using Android.Content;
using Android.OS;
using Android.Speech;
using JarvisAssistant.Core.Services;
using JarvisAssistant.Services.Speech;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Android;
using Android.Content.PM;
using CorePermissionStatus = JarvisAssistant.Core.Services.PermissionStatus;

namespace JarvisAssistant.MAUI.Platforms.Android.Services
{
    public class AndroidSpeechRecognitionService : SpeechRecognitionServiceBase
    {
        private SpeechRecognizer? _speechRecognizer;
        private Intent? _speechIntent;
        private TaskCompletionSource<SpeechRecognitionResult>? _recognitionTaskSource;
        private readonly Context _context;

        public AndroidSpeechRecognitionService(ILogger<AndroidSpeechRecognitionService> logger) 
            : base(logger)
        {
            _context = Platform.CurrentActivity ?? throw new InvalidOperationException("Current activity is null");
        }

        public override bool IsAvailable => SpeechRecognizer.IsRecognitionAvailable(_context);

        public override async Task<IEnumerable<string>> GetAvailableLanguagesAsync()
        {
            try
            {
                var languages = new List<string> { "en-US", "en-GB", "es-ES", "fr-FR", "de-DE", "it-IT", "pt-BR", "zh-CN", "ja-JP", "ko-KR" };
                return await Task.FromResult(languages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get available languages");
                return Enumerable.Empty<string>();
            }
        }

        public override async Task<CorePermissionStatus> RequestPermissionsAsync()
        {
            try
            {
                // Check current permission status
                var status = ContextCompat.CheckSelfPermission(_context, Manifest.Permission.RecordAudio);
                
                if (status == Permission.Granted)
                {
                    return CorePermissionStatus.Granted;
                }

                // Request permission
                var permissionResult = await Permissions.RequestAsync<Permissions.Microphone>();
                
                return permissionResult switch
                {
                    Microsoft.Maui.ApplicationModel.PermissionStatus.Granted => CorePermissionStatus.Granted,
                    Microsoft.Maui.ApplicationModel.PermissionStatus.Denied => CorePermissionStatus.Denied,
                    Microsoft.Maui.ApplicationModel.PermissionStatus.Disabled => CorePermissionStatus.Disabled,
                    Microsoft.Maui.ApplicationModel.PermissionStatus.Restricted => CorePermissionStatus.Restricted,
                    _ => CorePermissionStatus.Unknown
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to request microphone permission");
                return CorePermissionStatus.Unknown;
            }
        }

        protected override async Task<bool> StartListeningCoreAsync(SpeechRecognitionOptions options)
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    _speechRecognizer = SpeechRecognizer.CreateSpeechRecognizer(_context);
                    _speechRecognizer?.SetRecognitionListener(new SpeechRecognitionListener(this, options));
                    
                    _speechIntent = CreateSpeechIntent(options);
                    _speechRecognizer?.StartListening(_speechIntent);
                });
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start Android speech recognition");
                CleanupRecognizer();
                return false;
            }
        }

        protected override async Task StopListeningCoreAsync()
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _speechRecognizer?.StopListening();
                CleanupRecognizer();
            });
        }

        public override async Task<SpeechRecognitionResult> RecognizeSpeechAsync(
            SpeechRecognitionOptions? options = null, 
            CancellationToken cancellationToken = default)
        {
            options ??= new SpeechRecognitionOptions();
            
            _recognitionTaskSource = new TaskCompletionSource<SpeechRecognitionResult>();
            
            using (cancellationToken.Register(() => _recognitionTaskSource.TrySetCanceled()))
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    _speechRecognizer = SpeechRecognizer.CreateSpeechRecognizer(_context);
                    _speechRecognizer?.SetRecognitionListener(new SpeechRecognitionListener(this, options, _recognitionTaskSource));
                    
                    _speechIntent = CreateSpeechIntent(options);
                    _speechRecognizer?.StartListening(_speechIntent);
                });
                
                return await _recognitionTaskSource.Task;
            }
        }

        private Intent CreateSpeechIntent(SpeechRecognitionOptions options)
        {
            var intent = new Intent(RecognizerIntent.ActionRecognizeSpeech);
            intent.PutExtra(RecognizerIntent.ExtraLanguageModel, RecognizerIntent.LanguageModelFreeForm);
            intent.PutExtra(RecognizerIntent.ExtraLanguage, options.Language);
            intent.PutExtra(RecognizerIntent.ExtraMaxResults, options.MaxAlternatives);
            intent.PutExtra(RecognizerIntent.ExtraPartialResults, options.EnablePartialResults);
            intent.PutExtra(RecognizerIntent.ExtraSpeechInputCompleteSilenceLengthMillis, (long)options.SilenceTimeout.TotalMilliseconds);
            
            // Note: ExtraProfanityFilter may not be available on all Android versions
            // We'll set it only if the profanity filter is explicitly enabled
            if (options.EnableProfanityFilter)
            {
                try
                {
                    intent.PutExtra("android.speech.extra.PROFANITY_FILTER", true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not set profanity filter - not supported on this device");
                }
            }
            
            return intent;
        }

        private void CleanupRecognizer()
        {
            _speechRecognizer?.Destroy();
            _speechRecognizer = null;
            _speechIntent = null;
        }

        private class SpeechRecognitionListener : Java.Lang.Object, IRecognitionListener
        {
            private readonly AndroidSpeechRecognitionService _service;
            private readonly SpeechRecognitionOptions _options;
            private readonly TaskCompletionSource<SpeechRecognitionResult>? _taskSource;
            private readonly DateTime _startTime;

            public SpeechRecognitionListener(
                AndroidSpeechRecognitionService service, 
                SpeechRecognitionOptions options,
                TaskCompletionSource<SpeechRecognitionResult>? taskSource = null)
            {
                _service = service;
                _options = options;
                _taskSource = taskSource;
                _startTime = DateTime.UtcNow;
            }

            public void OnBeginningOfSpeech()
            {
                _service._logger.LogDebug("Speech recognition started");
                _service.UpdateState(SpeechRecognitionState.Listening);
            }

            public void OnBufferReceived(byte[]? buffer)
            {
                // Audio buffer received
            }

            public void OnEndOfSpeech()
            {
                _service._logger.LogDebug("Speech recognition ended");
                _service.UpdateState(SpeechRecognitionState.Processing);
            }

            public void OnError(SpeechRecognizerError error)
            {
                _service._logger.LogError("Speech recognition error: {Error}", error);
                _service.UpdateState(SpeechRecognitionState.Error);
                
                _taskSource?.TrySetException(new Exception($"Speech recognition error: {error}"));
            }

            public void OnEvent(int eventType, Bundle? @params)
            {
                // Handle additional events if needed
            }

            public void OnPartialResults(Bundle? partialResults)
            {
                if (!_options.EnablePartialResults || partialResults == null)
                    return;

                var matches = partialResults.GetStringArrayList(SpeechRecognizer.ResultsRecognition);
                if (matches?.Count > 0)
                {
                    _service.OnPartialResultsReceived(matches[0] ?? string.Empty);
                }
            }

            public void OnReadyForSpeech(Bundle? @params)
            {
                _service._logger.LogDebug("Ready for speech");
            }

            public void OnResults(Bundle? results)
            {
                if (results == null)
                {
                    _taskSource?.TrySetResult(new SpeechRecognitionResult());
                    return;
                }

                var matches = results.GetStringArrayList(SpeechRecognizer.ResultsRecognition);
                var scores = results.GetFloatArray(SpeechRecognizer.ConfidenceScores);
                
                var result = new SpeechRecognitionResult
                {
                    Text = matches?.FirstOrDefault() ?? string.Empty,
                    Confidence = scores?.FirstOrDefault() ?? 0f,
                    IsFinal = true,
                    Duration = DateTime.UtcNow - _startTime
                };
                
                // Add alternatives
                if (matches != null && scores != null)
                {
                    for (int i = 1; i < Math.Min(matches.Count, _options.MaxAlternatives); i++)
                    {
                        result.Alternatives.Add(new SpeechRecognitionAlternative
                        {
                            Text = matches[i] ?? string.Empty,
                            Confidence = i < scores.Length ? scores[i] : 0f
                        });
                    }
                }
                
                if (_taskSource != null)
                {
                    _taskSource.TrySetResult(result);
                }
                else
                {
                    _service.OnSpeechRecognized(result);
                }
                
                _service.UpdateState(SpeechRecognitionState.Idle);
            }

            public void OnRmsChanged(float rmsdB)
            {
                // Volume level changed
            }
        }
    }
}
#endif
