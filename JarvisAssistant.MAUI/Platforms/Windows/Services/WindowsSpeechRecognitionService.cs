#if WINDOWS
using JarvisAssistant.Core.Services;
using JarvisAssistant.Services.Speech;
using Microsoft.Extensions.Logging;
using Windows.Media.SpeechRecognition;
using Windows.Globalization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CorePermissionStatus = JarvisAssistant.Core.Services.PermissionStatus;
using CoreSpeechRecognitionResult = JarvisAssistant.Core.Services.SpeechRecognitionResult;

namespace JarvisAssistant.MAUI.Platforms.Windows.Services
{
    public class WindowsSpeechRecognitionService : SpeechRecognitionServiceBase
    {
        private SpeechRecognizer? _speechRecognizer;
        private readonly SemaphoreSlim _recognizerLock = new(1, 1);

        public WindowsSpeechRecognitionService(ILogger<WindowsSpeechRecognitionService> logger) 
            : base(logger)
        {
        }

        public override bool IsAvailable => true;

        public override async Task<IEnumerable<string>> GetAvailableLanguagesAsync()
        {
            try
            {
                var languages = SpeechRecognizer.SupportedTopicLanguages
                    .Select(l => l.LanguageTag)
                    .ToList();
                
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
                // For Windows, we'll assume permission is granted if speech recognition is available
                // In a real implementation, you might want to check privacy settings or request permissions through Windows APIs
                return await Task.FromResult(CorePermissionStatus.Granted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to request microphone permission");
                return CorePermissionStatus.Unknown;
            }
        }

        protected override async Task<bool> StartListeningCoreAsync(SpeechRecognitionOptions options)
        {
            await _recognizerLock.WaitAsync();
            try
            {
                _speechRecognizer = new SpeechRecognizer(new Language(options.Language));
                
                // Configure recognizer
                _speechRecognizer.Timeouts.InitialSilenceTimeout = options.SilenceTimeout;
                _speechRecognizer.Timeouts.EndSilenceTimeout = options.SilenceTimeout;
                
                // Set up event handlers
                _speechRecognizer.StateChanged += OnStateChanged;
                _speechRecognizer.HypothesisGenerated += OnHypothesisGenerated;
                
                if (options.ContinuousRecognition)
                {
                    _speechRecognizer.ContinuousRecognitionSession.ResultGenerated += OnContinuousRecognitionResultGenerated;
                    _speechRecognizer.ContinuousRecognitionSession.Completed += OnContinuousRecognitionCompleted;
                    
                    // Compile constraints
                    var compilationResult = await _speechRecognizer.CompileConstraintsAsync();
                    if (compilationResult.Status != SpeechRecognitionResultStatus.Success)
                    {
                        _logger.LogError("Failed to compile speech recognition constraints: {Status}", compilationResult.Status);
                        return false;
                    }
                    
                    // Start continuous recognition
                    await _speechRecognizer.ContinuousRecognitionSession.StartAsync();
                }
                else
                {
                    // For single recognition, we'll handle it in RecognizeSpeechAsync
                    var compilationResult = await _speechRecognizer.CompileConstraintsAsync();
                    if (compilationResult.Status != SpeechRecognitionResultStatus.Success)
                    {
                        _logger.LogError("Failed to compile speech recognition constraints: {Status}", compilationResult.Status);
                        return false;
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start Windows speech recognition");
                CleanupRecognizer();
                return false;
            }
            finally
            {
                _recognizerLock.Release();
            }
        }

        protected override async Task StopListeningCoreAsync()
        {
            await _recognizerLock.WaitAsync();
            try
            {
                if (_speechRecognizer?.ContinuousRecognitionSession != null)
                {
                    await _speechRecognizer.ContinuousRecognitionSession.StopAsync();
                }
                
                CleanupRecognizer();
            }
            finally
            {
                _recognizerLock.Release();
            }
        }

        public override async Task<CoreSpeechRecognitionResult> RecognizeSpeechAsync(
            SpeechRecognitionOptions? options = null, 
            CancellationToken cancellationToken = default)
        {
            options ??= new SpeechRecognitionOptions();
            
            await _recognizerLock.WaitAsync(cancellationToken);
            try
            {
                UpdateState(SpeechRecognitionState.Starting);
                
                using var recognizer = new SpeechRecognizer(new Language(options.Language));
                
                recognizer.Timeouts.InitialSilenceTimeout = options.SilenceTimeout;
                recognizer.Timeouts.EndSilenceTimeout = options.SilenceTimeout;
                
                var compilationResult = await recognizer.CompileConstraintsAsync();
                if (compilationResult.Status != SpeechRecognitionResultStatus.Success)
                {
                    throw new InvalidOperationException($"Failed to compile constraints: {compilationResult.Status}");
                }
                
                UpdateState(SpeechRecognitionState.Listening);
                
                var recognitionResult = await recognizer.RecognizeAsync();
                
                UpdateState(SpeechRecognitionState.Processing);
                
                var result = new CoreSpeechRecognitionResult
                {
                    Text = recognitionResult.Text ?? string.Empty,
                    Confidence = (float)recognitionResult.Confidence,
                    IsFinal = true,
                    Duration = TimeSpan.Zero // Duration not available in Windows Speech Recognition
                };
                
                // Add alternatives if available
                if (recognitionResult.GetAlternates((uint)options.MaxAlternatives) is { } alternates)
                {
                    result.Alternatives = alternates
                        .Select(a => new SpeechRecognitionAlternative
                        {
                            Text = a.Text,
                            Confidence = (float)a.Confidence
                        })
                        .ToList();
                }
                
                UpdateState(SpeechRecognitionState.Idle);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Speech recognition failed");
                UpdateState(SpeechRecognitionState.Error);
                throw;
            }
            finally
            {
                _recognizerLock.Release();
            }
        }

        private void OnStateChanged(SpeechRecognizer sender, SpeechRecognizerStateChangedEventArgs args)
        {
            _logger.LogDebug("Speech recognizer state changed: {State}", args.State);
            
            var state = args.State switch
            {
                SpeechRecognizerState.Idle => SpeechRecognitionState.Idle,
                SpeechRecognizerState.Capturing => SpeechRecognitionState.Listening,
                SpeechRecognizerState.Processing => SpeechRecognitionState.Processing,
                _ => _currentState
            };
            
            UpdateState(state);
        }

        private void OnHypothesisGenerated(SpeechRecognizer sender, SpeechRecognitionHypothesisGeneratedEventArgs args)
        {
            if (!string.IsNullOrEmpty(args.Hypothesis.Text))
            {
                OnPartialResultsReceived(args.Hypothesis.Text);
            }
        }

        private void OnContinuousRecognitionResultGenerated(
            SpeechContinuousRecognitionSession sender, 
            SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            var result = new CoreSpeechRecognitionResult
            {
                Text = args.Result.Text ?? string.Empty,
                Confidence = (float)args.Result.Confidence,
                IsFinal = true,
                Duration = TimeSpan.Zero // Duration not available in Windows Speech Recognition
            };
            
            OnSpeechRecognized(result);
        }

        private void OnContinuousRecognitionCompleted(
            SpeechContinuousRecognitionSession sender, 
            SpeechContinuousRecognitionCompletedEventArgs args)
        {
            _logger.LogInformation("Continuous recognition completed: {Status}", args.Status);
            
            if (args.Status == SpeechRecognitionResultStatus.UserCanceled)
            {
                UpdateState(SpeechRecognitionState.Idle);
            }
            else if (args.Status != SpeechRecognitionResultStatus.Success)
            {
                UpdateState(SpeechRecognitionState.Error);
            }
        }

        private void CleanupRecognizer()
        {
            if (_speechRecognizer != null)
            {
                _speechRecognizer.StateChanged -= OnStateChanged;
                _speechRecognizer.HypothesisGenerated -= OnHypothesisGenerated;
                
                if (_speechRecognizer.ContinuousRecognitionSession != null)
                {
                    _speechRecognizer.ContinuousRecognitionSession.ResultGenerated -= OnContinuousRecognitionResultGenerated;
                    _speechRecognizer.ContinuousRecognitionSession.Completed -= OnContinuousRecognitionCompleted;
                }
                
                _speechRecognizer.Dispose();
                _speechRecognizer = null;
            }
        }
    }
}
#endif
