using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace JarvisAssistant.Services
{
    /// <summary>
    /// Manages voice mode functionality with platform-specific behavior.
    /// </summary>
    public class VoiceModeManager : IVoiceModeManager, IDisposable
    {
        private readonly IPlatformService _platformService;
        private readonly IVoiceService _voiceService;
        private readonly IVoiceCommandProcessor _commandProcessor;
        private readonly ILogger<VoiceModeManager> _logger;

        private bool _isVoiceModeActive;
        private VoiceModeState _currentState = VoiceModeState.Inactive;
        private bool _isWakeWordDetectionEnabled;
        private float _wakeWordSensitivity = 0.7f;
        private string[] _wakeWords = { "hey jarvis", "jarvis" };
        private bool _isListening;
        private bool _isProcessingCommand;
        private CancellationTokenSource? _listeningCancellationTokenSource;
        private Timer? _voiceActivityTimer;
        private float _currentAudioLevel;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="VoiceModeManager"/> class.
        /// </summary>
        /// <param name="platformService">The platform service for platform detection.</param>
        /// <param name="voiceService">The voice service for speech operations.</param>
        /// <param name="commandProcessor">The voice command processor.</param>
        /// <param name="logger">The logger instance.</param>
        public VoiceModeManager(
            IPlatformService platformService,
            IVoiceService voiceService,
            IVoiceCommandProcessor commandProcessor,
            ILogger<VoiceModeManager> logger)
        {
            _platformService = platformService ?? throw new ArgumentNullException(nameof(platformService));
            _voiceService = voiceService ?? throw new ArgumentNullException(nameof(voiceService));
            _commandProcessor = commandProcessor ?? throw new ArgumentNullException(nameof(commandProcessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Configure based on platform
            ConfigureForPlatform();

            // Subscribe to command processor events
            _commandProcessor.CommandReceived += OnCommandReceived;
            _commandProcessor.CommandProcessed += OnCommandProcessed;
        }

        /// <inheritdoc/>
        public bool IsVoiceModeActive => _isVoiceModeActive;

        /// <inheritdoc/>
        public bool CanToggleVoiceMode => ActivationMode == VoiceActivationMode.Toggle;

        /// <inheritdoc/>
        public VoiceActivationMode ActivationMode { get; private set; }

        /// <inheritdoc/>
        public VoiceModeState CurrentState
        {
            get => _currentState;
            private set
            {
                if (_currentState != value)
                {
                    var previousState = _currentState;
                    _currentState = value;
                    OnStateChanged(previousState, value);
                }
            }
        }

        /// <inheritdoc/>
        public bool IsWakeWordDetectionEnabled => _isWakeWordDetectionEnabled;

        /// <inheritdoc/>
        public event EventHandler<VoiceModeStateChangedEventArgs>? StateChanged;

        /// <inheritdoc/>
        public event EventHandler<WakeWordDetectedEventArgs>? WakeWordDetected;

        /// <inheritdoc/>
        public event EventHandler<VoiceActivityDetectedEventArgs>? VoiceActivityDetected;

        /// <inheritdoc/>
        public async Task<bool> EnableVoiceModeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (_isVoiceModeActive)
                {
                    _logger.LogDebug("Voice mode is already active");
                    return true;
                }

                _logger.LogInformation("Enabling voice mode for platform: {Platform}", _platformService.CurrentPlatform);

                if (!_platformService.SupportsVoiceInput())
                {
                    _logger.LogWarning("Voice input is not supported on this platform");
                    CurrentState = VoiceModeState.Error;
                    return false;
                }

                _isVoiceModeActive = true;
                CurrentState = VoiceModeState.Listening;

                // Start listening based on activation mode
                if (ActivationMode == VoiceActivationMode.AlwaysOn || ActivationMode == VoiceActivationMode.Toggle)
                {
                    await StartContinuousListeningAsync(cancellationToken);
                }

                _logger.LogInformation("Voice mode enabled successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enable voice mode");
                CurrentState = VoiceModeState.Error;
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DisableVoiceModeAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_isVoiceModeActive)
                {
                    _logger.LogDebug("Voice mode is already inactive");
                    return true;
                }

                // TV platforms should always have voice mode active
                if (ActivationMode == VoiceActivationMode.AlwaysOn)
                {
                    _logger.LogWarning("Cannot disable voice mode on always-on platform");
                    return false;
                }

                _logger.LogInformation("Disabling voice mode");

                await StopListeningAsync();
                _isVoiceModeActive = false;
                CurrentState = VoiceModeState.Inactive;

                _logger.LogInformation("Voice mode disabled successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to disable voice mode");
                CurrentState = VoiceModeState.Error;
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ToggleVoiceModeAsync(CancellationToken cancellationToken = default)
        {
            if (!CanToggleVoiceMode)
            {
                _logger.LogWarning("Voice mode cannot be toggled on this platform");
                return _isVoiceModeActive;
            }

            if (_isVoiceModeActive)
            {
                return await DisableVoiceModeAsync(cancellationToken);
            }
            else
            {
                return await EnableVoiceModeAsync(cancellationToken);
            }
        }

        /// <inheritdoc/>
        public async Task<string?> ListenForCommandAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Starting push-to-talk listening with timeout: {Timeout}", timeout);

                CurrentState = VoiceModeState.Listening;

                using var timeoutCts = new CancellationTokenSource(timeout);
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                // Simulate listening for voice input
                // In a real implementation, this would capture audio and perform speech recognition
                await Task.Delay(100, combinedCts.Token);

                // For demonstration, return null (no command detected)
                return null;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Push-to-talk listening was cancelled or timed out");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during push-to-talk listening");
                CurrentState = VoiceModeState.Error;
                return null;
            }
            finally
            {
                if (_isVoiceModeActive)
                {
                    CurrentState = VoiceModeState.Listening;
                }
                else
                {
                    CurrentState = VoiceModeState.Inactive;
                }
            }
        }

        /// <inheritdoc/>
        public async Task ConfigureWakeWordDetectionAsync(bool enabled, float sensitivity = 0.7f, string[]? wakeWords = null)
        {
            _logger.LogInformation("Configuring wake word detection - Enabled: {Enabled}, Sensitivity: {Sensitivity}", enabled, sensitivity);

            _isWakeWordDetectionEnabled = enabled;
            _wakeWordSensitivity = Math.Clamp(sensitivity, 0.0f, 1.0f);

            if (wakeWords != null && wakeWords.Length > 0)
            {
                _wakeWords = wakeWords.Select(w => w.ToLowerInvariant()).ToArray();
                _logger.LogDebug("Updated wake words: {WakeWords}", string.Join(", ", _wakeWords));
            }

            // Restart listening if currently active to apply new settings
            if (_isVoiceModeActive && _isListening)
            {
                await StopListeningAsync();
                await StartContinuousListeningAsync(CancellationToken.None);
            }
        }

        /// <inheritdoc/>
        public float GetCurrentAudioLevel()
        {
            return _currentAudioLevel;
        }

        private void ConfigureForPlatform()
        {
            var platform = _platformService.CurrentPlatform;

            if (_platformService.IsGoogleTV() || platform == PlatformType.AndroidTV)
            {
                // TV platforms should always have voice mode active
                ActivationMode = VoiceActivationMode.AlwaysOn;
                _isWakeWordDetectionEnabled = true;
                _logger.LogInformation("Configured for Google TV/Android TV - Always-on voice mode");
            }
            else if (_platformService.SupportsVoiceInput())
            {
                // Mobile and desktop platforms can toggle voice mode
                ActivationMode = VoiceActivationMode.Toggle;
                _isWakeWordDetectionEnabled = true;
                _logger.LogInformation("Configured for {Platform} - Toggle voice mode", platform);
            }
            else
            {
                // Platform doesn't support voice input
                ActivationMode = VoiceActivationMode.Disabled;
                _isWakeWordDetectionEnabled = false;
                _logger.LogWarning("Voice input not supported on platform: {Platform}", platform);
            }

            // Auto-enable for always-on platforms
            if (ActivationMode == VoiceActivationMode.AlwaysOn)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(1000); // Give time for initialization
                    await EnableVoiceModeAsync();
                });
            }
        }

        private async Task StartContinuousListeningAsync(CancellationToken cancellationToken)
        {
            if (_isListening)
            {
                return;
            }

            _logger.LogDebug("Starting continuous voice listening");

            _listeningCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _isListening = true;

            // Start voice activity monitoring
            StartVoiceActivityMonitoring();

            // Start the listening loop
            _ = Task.Run(async () => await ContinuousListeningLoopAsync(_listeningCancellationTokenSource.Token), _listeningCancellationTokenSource.Token);
        }

        private async Task StopListeningAsync()
        {
            if (!_isListening)
            {
                return;
            }

            _logger.LogDebug("Stopping voice listening");

            _isListening = false;
            _listeningCancellationTokenSource?.Cancel();
            _listeningCancellationTokenSource?.Dispose();
            _listeningCancellationTokenSource = null;

            // Stop voice activity monitoring
            StopVoiceActivityMonitoring();

            await Task.Delay(100); // Give time for cleanup
        }

        private async Task ContinuousListeningLoopAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Starting continuous listening loop");

            try
            {
                while (!cancellationToken.IsCancellationRequested && _isListening)
                {
                    try
                    {
                        // Simulate audio capture and processing
                        await Task.Delay(100, cancellationToken);

                        // Update audio level (simulated)
                        _currentAudioLevel = (float)(new Random().NextDouble() * 0.3); // Low level background

                        // Simulate wake word detection
                        if (_isWakeWordDetectionEnabled && ShouldSimulateWakeWordDetection())
                        {
                            await HandleWakeWordDetected("hey jarvis", 0.85f, cancellationToken);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in continuous listening loop");
                        await Task.Delay(1000, cancellationToken); // Brief pause before retry
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
            finally
            {
                _logger.LogDebug("Continuous listening loop ended");
            }
        }

        private bool ShouldSimulateWakeWordDetection()
        {
            // For demonstration purposes, randomly trigger wake word detection
            // In a real implementation, this would analyze audio for wake words
            return new Random().Next(0, 10000) < 5; // Very low probability for demonstration
        }

        private async Task HandleWakeWordDetected(string wakeWord, float confidence, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Wake word detected: {WakeWord} (confidence: {Confidence:P1})", wakeWord, confidence);

            var eventArgs = new WakeWordDetectedEventArgs
            {
                WakeWord = wakeWord,
                Confidence = confidence,
                Timestamp = DateTime.UtcNow
            };

            OnWakeWordDetected(eventArgs);

            // Start processing mode
            CurrentState = VoiceModeState.Processing;

            try
            {
                // Listen for command after wake word
                var command = await ListenForCommandAfterWakeWordAsync(cancellationToken);
                if (!string.IsNullOrWhiteSpace(command))
                {
                    await ProcessVoiceCommandAsync(command, VoiceCommandSource.WakeWord, cancellationToken);
                }
            }
            finally
            {
                // Return to listening state
                if (_isVoiceModeActive)
                {
                    CurrentState = VoiceModeState.Listening;
                }
            }
        }

        private async Task<string?> ListenForCommandAfterWakeWordAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Listening for command after wake word detection");

                // Simulate command listening with timeout
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                await Task.Delay(1000, combinedCts.Token);

                // For demonstration, return a sample command occasionally
                if (new Random().Next(0, 10) < 3)
                {
                    var sampleCommands = new[] { "what's my status", "generate code", "help me", "analyze this" };
                    return sampleCommands[new Random().Next(sampleCommands.Length)];
                }

                return null;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Command listening timed out or was cancelled");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listening for command after wake word");
                return null;
            }
        }

        private async Task ProcessVoiceCommandAsync(string commandText, VoiceCommandSource source, CancellationToken cancellationToken)
        {
            try
            {
                _isProcessingCommand = true;
                _logger.LogInformation("Processing voice command: {Command} from {Source}", commandText, source);

                var result = await _commandProcessor.ProcessTextCommandAsync(commandText, source, null, cancellationToken);

                if (result.Success && result.ShouldSpeak && !string.IsNullOrWhiteSpace(result.Response))
                {
                    // Generate speech response
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var audioData = await _voiceService.GenerateSpeechAsync(result.Response, null, cancellationToken);
                            _logger.LogDebug("Generated speech response of {Size} bytes", audioData.Length);
                            // In a real implementation, this would play the audio
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to generate speech response");
                        }
                    }, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing voice command: {Command}", commandText);
            }
            finally
            {
                _isProcessingCommand = false;
            }
        }

        private void StartVoiceActivityMonitoring()
        {
            _voiceActivityTimer = new Timer(OnVoiceActivityTimer, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(100));
        }

        private void StopVoiceActivityMonitoring()
        {
            _voiceActivityTimer?.Dispose();
            _voiceActivityTimer = null;
        }

        private void OnVoiceActivityTimer(object? state)
        {
            try
            {
                // Simulate voice activity detection
                var isActive = _currentAudioLevel > 0.1f;
                
                VoiceActivityDetected?.Invoke(this, new VoiceActivityDetectedEventArgs
                {
                    IsActive = isActive,
                    AudioLevel = _currentAudioLevel,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in voice activity monitoring");
            }
        }

        private void OnCommandReceived(object? sender, VoiceCommandReceivedEventArgs e)
        {
            _logger.LogDebug("Voice command received: {Command}", e.Command.ToLogString());
        }

        private void OnCommandProcessed(object? sender, VoiceCommandProcessedEventArgs e)
        {
            _logger.LogDebug("Voice command processed: {Command} -> {Success} in {Duration}ms", 
                e.Command.Text, e.Result.Success, e.ProcessingTime.TotalMilliseconds);
        }

        private void OnStateChanged(VoiceModeState previousState, VoiceModeState newState)
        {
            _logger.LogDebug("Voice mode state changed: {Previous} -> {New}", previousState, newState);

            StateChanged?.Invoke(this, new VoiceModeStateChangedEventArgs
            {
                PreviousState = previousState,
                NewState = newState,
                Timestamp = DateTime.UtcNow
            });
        }

        private void OnWakeWordDetected(WakeWordDetectedEventArgs eventArgs)
        {
            WakeWordDetected?.Invoke(this, eventArgs);
        }

        /// <summary>
        /// Disposes the voice mode manager and releases resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            try
            {
                StopListeningAsync().Wait(1000);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during disposal");
            }

            _listeningCancellationTokenSource?.Dispose();
            _voiceActivityTimer?.Dispose();

            if (_commandProcessor != null)
            {
                _commandProcessor.CommandReceived -= OnCommandReceived;
                _commandProcessor.CommandProcessed -= OnCommandProcessed;
            }
        }
    }
}
