using JarvisAssistant.Core.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace JarvisAssistant.Services.Speech
{
    /// <summary>
    /// Base implementation of speech recognition service
    /// </summary>
    public abstract class SpeechRecognitionServiceBase : ISpeechRecognitionService
    {
        protected readonly ILogger<SpeechRecognitionServiceBase> _logger;
        protected SpeechRecognitionState _currentState = SpeechRecognitionState.Idle;
        protected CancellationTokenSource? _listeningCancellationTokenSource;

        public SpeechRecognitionServiceBase(ILogger<SpeechRecognitionServiceBase> logger)
        {
            _logger = logger;
        }

        public abstract bool IsAvailable { get; }
        
        public bool IsListening => _currentState == SpeechRecognitionState.Listening;

        public event EventHandler<SpeechRecognitionResult>? SpeechRecognized;
        public event EventHandler<string>? PartialResultsReceived;
        public event EventHandler<SpeechRecognitionState>? StateChanged;

        public abstract Task<IEnumerable<string>> GetAvailableLanguagesAsync();
        public abstract Task<PermissionStatus> RequestPermissionsAsync();

        public virtual async Task<bool> StartListeningAsync(SpeechRecognitionOptions? options = null)
        {
            if (IsListening)
            {
                _logger.LogWarning("Speech recognition is already active");
                return false;
            }

            try
            {
                UpdateState(SpeechRecognitionState.Starting);
                
                var permissionStatus = await RequestPermissionsAsync();
                if (permissionStatus != PermissionStatus.Granted)
                {
                    _logger.LogError("Microphone permission not granted: {Status}", permissionStatus);
                    UpdateState(SpeechRecognitionState.Error);
                    return false;
                }

                _listeningCancellationTokenSource = new CancellationTokenSource();
                
                if (options?.MaxListeningTime.HasValue == true)
                {
                    _listeningCancellationTokenSource.CancelAfter(options.MaxListeningTime.Value);
                }

                var started = await StartListeningCoreAsync(options ?? new SpeechRecognitionOptions());
                
                if (started)
                {
                    UpdateState(SpeechRecognitionState.Listening);
                }
                else
                {
                    UpdateState(SpeechRecognitionState.Error);
                }

                return started;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start speech recognition");
                UpdateState(SpeechRecognitionState.Error);
                return false;
            }
        }

        public virtual async Task StopListeningAsync()
        {
            if (!IsListening)
            {
                return;
            }

            try
            {
                UpdateState(SpeechRecognitionState.Stopping);
                
                _listeningCancellationTokenSource?.Cancel();
                
                await StopListeningCoreAsync();
                
                UpdateState(SpeechRecognitionState.Idle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop speech recognition");
                UpdateState(SpeechRecognitionState.Error);
            }
            finally
            {
                _listeningCancellationTokenSource?.Dispose();
                _listeningCancellationTokenSource = null;
            }
        }

        public abstract Task<SpeechRecognitionResult> RecognizeSpeechAsync(
            SpeechRecognitionOptions? options = null, 
            CancellationToken cancellationToken = default);

        protected abstract Task<bool> StartListeningCoreAsync(SpeechRecognitionOptions options);
        protected abstract Task StopListeningCoreAsync();

        protected void UpdateState(SpeechRecognitionState newState)
        {
            if (_currentState != newState)
            {
                _currentState = newState;
                StateChanged?.Invoke(this, newState);
            }
        }

        protected void OnSpeechRecognized(SpeechRecognitionResult result)
        {
            SpeechRecognized?.Invoke(this, result);
        }

        protected void OnPartialResultsReceived(string partialText)
        {
            PartialResultsReceived?.Invoke(this, partialText);
        }
    }
}
