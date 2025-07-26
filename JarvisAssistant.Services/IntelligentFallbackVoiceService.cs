using JarvisAssistant.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace JarvisAssistant.Services
{
    /// <summary>
    /// Intelligent multi-tier fallback voice service that manages multiple TTS services
    /// in order of preference and quality. Automatically switches between services
    /// based on availability and performance.
    /// </summary>
    public class IntelligentFallbackVoiceService : IVoiceService, IDisposable
    {
        private readonly ILogger<IntelligentFallbackVoiceService> _logger;
        private readonly List<IVoiceService> _fallbackServices;
        private readonly Dictionary<Type, DateTime> _lastFailures;
        private readonly Dictionary<Type, int> _failureCount;
        private readonly TimeSpan _cooldownPeriod = TimeSpan.FromMinutes(5);
        private readonly int _maxFailures = 3;
        private bool _disposed = false;

        public IntelligentFallbackVoiceService(ILogger<IntelligentFallbackVoiceService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fallbackServices = new List<IVoiceService>();
            _lastFailures = new Dictionary<Type, DateTime>();
            _failureCount = new Dictionary<Type, int>();

            InitializeFallbackServices();
        }

        /// <summary>
        /// Initializes fallback services in order of preference.
        /// </summary>
        private void InitializeFallbackServices()
        {
            // First priority: Direct Windows TTS (no WAV files, no beeping, Windows only)
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    var directTts = new DirectWindowsVoiceService();
                    _fallbackServices.Add(directTts);
                    _logger.LogInformation("Added Direct Windows TTS as fallback option 1 (no beeping)");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to initialize Direct Windows TTS service");
                }

                try
                {
                    // Second priority: Modern Windows TTS (best quality, free, Windows only)
                    var modernTts = new ModernWindowsTtsService(_logger as ILogger<ModernWindowsTtsService> ?? 
                        Microsoft.Extensions.Logging.Abstractions.NullLogger<ModernWindowsTtsService>.Instance);
                    _fallbackServices.Add(modernTts);
                    _logger.LogInformation("Added Modern Windows TTS as fallback option 2");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to initialize Modern Windows TTS service");
                }

                // Note: Removed WindowsSapiVoiceService as it causes beeping due to WAV file generation
                // DirectWindowsVoiceService replaces it with direct audio output
            }
            else
            {
                _logger.LogInformation("Windows TTS services not available on this platform");
            }

            // Always add stub service as final fallback (cross-platform)
            try
            {
                var stubService = new StubVoiceService();
                _fallbackServices.Add(stubService);
                _logger.LogInformation("Added Stub service as fallback option {Option}", _fallbackServices.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize Stub service");
            }

            if (_fallbackServices.Count == 0)
            {
                _logger.LogError("No fallback services could be initialized!");
            }
            else
            {
                _logger.LogInformation("Initialized {ServiceCount} fallback services", _fallbackServices.Count);
            }
        }

        /// <inheritdoc/>
        public async Task<byte[]> GenerateSpeechAsync(string text, string? voiceId = null, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(IntelligentFallbackVoiceService));

            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<byte>();

            var availableServices = GetAvailableServices();
            
            if (availableServices.Count == 0)
            {
                _logger.LogError("No fallback services are available");
                return Array.Empty<byte>();
            }

            foreach (var service in availableServices)
            {
                var serviceType = service.GetType();
                
                try
                {
                    _logger.LogDebug("Attempting speech generation with {ServiceType}", serviceType.Name);
                    
                    var result = await service.GenerateSpeechAsync(text, voiceId, cancellationToken);
                    
                    if (result.Length > 0)
                    {
                        // Success - reset failure count
                        _failureCount[serviceType] = 0;
                        _lastFailures.Remove(serviceType);
                        
                        _logger.LogInformation("Successfully generated speech using {ServiceType}", serviceType.Name);
                        return result;
                    }
                    
                    _logger.LogWarning("{ServiceType} returned empty audio data", serviceType.Name);
                }
                catch (Exception ex)
                {
                    RecordFailure(serviceType, ex);
                    _logger.LogWarning(ex, "Service {ServiceType} failed, trying next fallback", serviceType.Name);
                }
            }

            _logger.LogError("All fallback services failed to generate speech");
            return Array.Empty<byte>();
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<byte[]> StreamSpeechAsync(string text, string? voiceId = null, 
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(IntelligentFallbackVoiceService));

            if (string.IsNullOrWhiteSpace(text))
                yield break;

            var availableServices = GetAvailableServices();
            
            if (availableServices.Count == 0)
            {
                _logger.LogError("No fallback services are available for streaming");
                yield break;
            }

            foreach (var service in availableServices)
            {
                var serviceType = service.GetType();
                var chunks = new List<byte[]>();
                var hasData = false;
                Exception? lastException = null;
                
                // Collect all chunks first to avoid yield in try-catch
                try
                {
                    _logger.LogDebug("Attempting streaming speech with {ServiceType}", serviceType.Name);
                    
                    await foreach (var chunk in service.StreamSpeechAsync(text, voiceId, cancellationToken))
                    {
                        if (chunk.Length > 0)
                        {
                            chunks.Add(chunk);
                            hasData = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogWarning(ex, "Streaming service {ServiceType} failed", serviceType.Name);
                }

                // If we got data, yield it and finish successfully
                if (hasData)
                {
                    _failureCount[serviceType] = 0;
                    _lastFailures.Remove(serviceType);
                    _logger.LogInformation("Successfully collected speech chunks using {ServiceType}", serviceType.Name);
                    
                    foreach (var chunk in chunks)
                    {
                        yield return chunk;
                    }
                    yield break; // Successfully completed
                }

                // Record failure if no data was collected
                if (lastException != null)
                {
                    RecordFailure(serviceType, lastException);
                }
                else
                {
                    _logger.LogWarning("{ServiceType} completed streaming but yielded no data", serviceType.Name);
                }
            }

            _logger.LogError("All fallback services failed to stream speech");
        }

        /// <inheritdoc/>
        public async Task<string> RecognizeSpeechAsync(byte[] audioData, string? language = null, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(IntelligentFallbackVoiceService));

            var availableServices = GetAvailableServices();
            
            foreach (var service in availableServices)
            {
                var serviceType = service.GetType();
                
                try
                {
                    _logger.LogDebug("Attempting speech recognition with {ServiceType}", serviceType.Name);
                    
                    var result = await service.RecognizeSpeechAsync(audioData, language, cancellationToken);
                    
                    if (!string.IsNullOrEmpty(result))
                    {
                        // Success - reset failure count
                        _failureCount[serviceType] = 0;
                        _lastFailures.Remove(serviceType);
                        
                        _logger.LogInformation("Successfully recognized speech using {ServiceType}", serviceType.Name);
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    RecordFailure(serviceType, ex);
                    _logger.LogWarning(ex, "Recognition service {ServiceType} failed, trying next fallback", serviceType.Name);
                }
            }

            _logger.LogError("All fallback services failed to recognize speech");
            return string.Empty;
        }

        /// <summary>
        /// Gets services that are currently available (not in cooldown due to failures).
        /// </summary>
        /// <returns>List of available services in order of preference</returns>
        private List<IVoiceService> GetAvailableServices()
        {
            var now = DateTime.UtcNow;
            var availableServices = new List<IVoiceService>();

            foreach (var service in _fallbackServices)
            {
                var serviceType = service.GetType();
                
                // Check if service is in cooldown
                if (_lastFailures.ContainsKey(serviceType))
                {
                    var lastFailure = _lastFailures[serviceType];
                    var failureCount = _failureCount.GetValueOrDefault(serviceType, 0);
                    
                    if (failureCount >= _maxFailures && now - lastFailure < _cooldownPeriod)
                    {
                        _logger.LogDebug("Service {ServiceType} is in cooldown until {CooldownEnd}", 
                            serviceType.Name, lastFailure.Add(_cooldownPeriod));
                        continue;
                    }
                }

                availableServices.Add(service);
            }

            return availableServices;
        }

        /// <summary>
        /// Records a failure for a service.
        /// </summary>
        /// <param name="serviceType">Type of the failed service</param>
        /// <param name="exception">Exception that occurred</param>
        private void RecordFailure(Type serviceType, Exception exception)
        {
            var now = DateTime.UtcNow;
            _lastFailures[serviceType] = now;
            _failureCount[serviceType] = _failureCount.GetValueOrDefault(serviceType, 0) + 1;

            var failureCount = _failureCount[serviceType];
            
            _logger.LogWarning("Service {ServiceType} failure #{FailureCount}: {ErrorMessage}", 
                serviceType.Name, failureCount, exception.Message);

            if (failureCount >= _maxFailures)
            {
                _logger.LogWarning("Service {ServiceType} has reached maximum failures ({MaxFailures}), " +
                    "entering {CooldownMinutes} minute cooldown", 
                    serviceType.Name, _maxFailures, _cooldownPeriod.TotalMinutes);
            }
        }

        /// <summary>
        /// Gets status information about all fallback services.
        /// </summary>
        /// <returns>Dictionary of service status information</returns>
        public Dictionary<string, object?> GetServiceStatus()
        {
            var status = new Dictionary<string, object?>();
            var now = DateTime.UtcNow;

            foreach (var service in _fallbackServices)
            {
                var serviceType = service.GetType();
                var serviceName = serviceType.Name;
                
                var serviceStatus = new Dictionary<string, object?>
                {
                    ["Available"] = true,
                    ["FailureCount"] = _failureCount.GetValueOrDefault(serviceType, 0),
                    ["InCooldown"] = false,
                    ["CooldownEnds"] = null
                };

                if (_lastFailures.ContainsKey(serviceType))
                {
                    var lastFailure = _lastFailures[serviceType];
                    var failureCount = _failureCount.GetValueOrDefault(serviceType, 0);
                    
                    serviceStatus["LastFailure"] = lastFailure;
                    
                    if (failureCount >= _maxFailures && now - lastFailure < _cooldownPeriod)
                    {
                        serviceStatus["Available"] = false;
                        serviceStatus["InCooldown"] = true;
                        serviceStatus["CooldownEnds"] = lastFailure.Add(_cooldownPeriod);
                    }
                }

                status[serviceName] = serviceStatus;
            }

            return status;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                foreach (var service in _fallbackServices)
                {
                    try
                    {
                        if (service is IDisposable disposableService)
                        {
                            disposableService.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error disposing service {ServiceType}", service.GetType().Name);
                    }
                }
                
                _fallbackServices.Clear();
                _disposed = true;
                _logger.LogDebug("Intelligent Fallback Voice service disposed");
            }
        }
    }
}
