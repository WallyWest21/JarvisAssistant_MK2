using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace JarvisAssistant.Services
{
    /// <summary>
    /// Custom health checker for voice services that can handle different voice service implementations.
    /// </summary>
    public class VoiceServiceHealthChecker
    {
        private readonly IVoiceService _voiceService;
        private readonly ILogger<VoiceServiceHealthChecker> _logger;
        private DateTime _lastCheck = DateTime.MinValue;
        private int _consecutiveFailures = 0;
        private const int MaxBackoffFailures = 5;

        public VoiceServiceHealthChecker(IVoiceService voiceService, ILogger<VoiceServiceHealthChecker> logger)
        {
            _voiceService = voiceService ?? throw new ArgumentNullException(nameof(voiceService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Performs a health check on the voice service.
        /// </summary>
        /// <returns>Service status indicating health state.</returns>
        public async Task<ServiceStatus> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var serviceName = "voice-service";

            try
            {
                // Calculate backoff delay if there have been consecutive failures
                var backoffDelay = CalculateBackoffDelay(_consecutiveFailures);
                if (backoffDelay > TimeSpan.Zero && 
                    DateTime.UtcNow - _lastCheck < backoffDelay)
                {
                    return new ServiceStatus(serviceName, ServiceState.Offline)
                    {
                        ErrorMessage = $"Backing off due to consecutive failures (attempt {_consecutiveFailures})",
                        Metrics = new Dictionary<string, object>
                        {
                            ["error_code"] = "SRV-BACKOFF-001",
                            ["response_time_ms"] = 0,
                            ["consecutive_failures"] = _consecutiveFailures,
                            ["next_check_in_seconds"] = (int)(backoffDelay.TotalSeconds - (DateTime.UtcNow - _lastCheck).TotalSeconds)
                        }
                    };
                }

                _lastCheck = DateTime.UtcNow;

                // Check based on service type
                bool isHealthy = false;
                string serviceType = "Unknown";

                if (_voiceService is ElevenLabsVoiceService elevenLabsService)
                {
                    serviceType = "ElevenLabs";
                    isHealthy = await elevenLabsService.IsHealthyAsync(cancellationToken);
                }
                else if (_voiceService is StubVoiceService)
                {
                    serviceType = "Stub";
                    // Stub service is always healthy
                    isHealthy = true;
                }
                else if (_voiceService is WindowsSapiVoiceService)
                {
                    serviceType = "Windows SAPI";
                    // Windows SAPI service is always healthy if running on Windows
                    isHealthy = OperatingSystem.IsWindows();
                }
                else
                {
                    serviceType = _voiceService.GetType().Name;
                    // For other services, try a simple test
                    try
                    {
                        var testAudio = await _voiceService.GenerateSpeechAsync("test", null, cancellationToken);
                        isHealthy = testAudio != null;
                    }
                    catch
                    {
                        isHealthy = false;
                    }
                }

                stopwatch.Stop();
                var responseTimeMs = (int)stopwatch.ElapsedMilliseconds;

                if (isHealthy)
                {
                    _consecutiveFailures = 0;
                    var state = responseTimeMs switch
                    {
                        < 100 => ServiceState.Online,
                        < 500 => ServiceState.Online,
                        < 2000 => ServiceState.Degraded,
                        _ => ServiceState.Degraded
                    };

                    _logger.LogDebug("Voice service ({ServiceType}) health check successful: {ResponseTime}ms", 
                        serviceType, responseTimeMs);

                    return new ServiceStatus(serviceName, state)
                    {
                        Metrics = new Dictionary<string, object>
                        {
                            ["response_time_ms"] = responseTimeMs,
                            ["service_type"] = serviceType,
                            ["consecutive_failures"] = 0,
                            ["last_check"] = _lastCheck
                        }
                    };
                }
                else
                {
                    _consecutiveFailures++;
                    _logger.LogWarning("Voice service ({ServiceType}) health check failed (failure #{FailureCount})", 
                        serviceType, _consecutiveFailures);

                    return new ServiceStatus(serviceName, ServiceState.Offline)
                    {
                        ErrorMessage = $"{serviceType} voice service is not responding",
                        Metrics = new Dictionary<string, object>
                        {
                            ["error_code"] = "SRV-UNHEALTHY-001",
                            ["response_time_ms"] = responseTimeMs,
                            ["service_type"] = serviceType,
                            ["consecutive_failures"] = _consecutiveFailures,
                            ["last_check"] = _lastCheck
                        }
                    };
                }
            }
            catch (OperationCanceledException)
            {
                _consecutiveFailures++;
                return new ServiceStatus(serviceName, ServiceState.Offline)
                {
                    ErrorMessage = "Health check was cancelled",
                    Metrics = new Dictionary<string, object>
                    {
                        ["error_code"] = "SRV-TIMEOUT-001",
                        ["response_time_ms"] = (int)stopwatch.ElapsedMilliseconds,
                        ["consecutive_failures"] = _consecutiveFailures
                    }
                };
            }
            catch (Exception ex)
            {
                _consecutiveFailures++;
                _logger.LogError(ex, "Voice service health check failed with exception");

                return new ServiceStatus(serviceName, ServiceState.Error)
                {
                    ErrorMessage = $"Health check error: {ex.Message}",
                    Metrics = new Dictionary<string, object>
                    {
                        ["error_code"] = "SRV-ERROR-001",
                        ["response_time_ms"] = (int)stopwatch.ElapsedMilliseconds,
                        ["consecutive_failures"] = _consecutiveFailures,
                        ["exception_type"] = ex.GetType().Name
                    }
                };
            }
        }

        /// <summary>
        /// Resets the failure count for the voice service.
        /// </summary>
        public void ResetFailures()
        {
            _consecutiveFailures = 0;
            _logger.LogInformation("Voice service failure count reset");
        }

        /// <summary>
        /// Calculates backoff delay based on consecutive failures.
        /// </summary>
        private static TimeSpan CalculateBackoffDelay(int consecutiveFailures)
        {
            if (consecutiveFailures <= 1)
                return TimeSpan.Zero;

            // Exponential backoff: 2^failures seconds, max 5 minutes
            var backoffSeconds = Math.Min(Math.Pow(2, consecutiveFailures - 1), 300);
            return TimeSpan.FromSeconds(backoffSeconds);
        }
    }
}
