using System.Diagnostics;
using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models;
using Microsoft.Extensions.Logging;

namespace JarvisAssistant.Services
{
    /// <summary>
    /// Service health checker that monitors individual services for health and performance.
    /// </summary>
    public class ServiceHealthChecker : IServiceHealthChecker
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ServiceHealthChecker> _logger;
        private readonly Dictionary<string, ServiceEndpoint> _serviceEndpoints;
        private readonly Random _random = new();

        public ServiceHealthChecker(HttpClient httpClient, ILogger<ServiceHealthChecker> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _serviceEndpoints = new Dictionary<string, ServiceEndpoint>();
            
            // Configure HTTP client for health checks
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        /// <summary>
        /// Registers a service endpoint for health checking.
        /// </summary>
        public void RegisterService(string serviceName, string healthEndpoint, string? displayName = null)
        {
            _serviceEndpoints[serviceName] = new ServiceEndpoint
            {
                Name = serviceName,
                DisplayName = displayName ?? serviceName,
                HealthEndpoint = healthEndpoint,
                LastCheck = DateTimeOffset.MinValue,
                ConsecutiveFailures = 0
            };
        }

        /// <summary>
        /// Performs a health check on the specified service.
        /// </summary>
        public async Task<ServiceStatus> CheckServiceHealthAsync(string serviceName)
        {
            if (!_serviceEndpoints.TryGetValue(serviceName, out var endpoint))
            {
                return new ServiceStatus(serviceName, ServiceState.Error)
                {
                    ErrorMessage = "Service not registered for health checking",
                    Metrics = new Dictionary<string, object>
                    {
                        ["error_code"] = "SRV-NOT-REG-001",
                        ["response_time_ms"] = 0
                    }
                };
            }

            var stopwatch = Stopwatch.StartNew();
            var status = new ServiceStatus(serviceName);

            try
            {
                // Calculate backoff delay if there have been consecutive failures
                var backoffDelay = CalculateBackoffDelay(endpoint.ConsecutiveFailures);
                if (backoffDelay > TimeSpan.Zero && 
                    DateTimeOffset.UtcNow - endpoint.LastCheck < backoffDelay)
                {
                    return new ServiceStatus(serviceName, ServiceState.Offline)
                    {
                        ErrorMessage = $"Backing off due to consecutive failures (attempt {endpoint.ConsecutiveFailures})",
                        Metrics = new Dictionary<string, object>
                        {
                            ["error_code"] = "SRV-BACKOFF-001",
                            ["response_time_ms"] = 0,
                            ["consecutive_failures"] = endpoint.ConsecutiveFailures,
                            ["next_check_in_seconds"] = (int)(backoffDelay.TotalSeconds - (DateTimeOffset.UtcNow - endpoint.LastCheck).TotalSeconds)
                        }
                    };
                }

                // Add platform-specific debugging
                _logger.LogDebug("Checking health for {ServiceName} at {Endpoint} (Platform: {Platform})", 
                    serviceName, endpoint.HealthEndpoint, GetCurrentPlatform());

                using var response = await _httpClient.GetAsync(endpoint.HealthEndpoint);
                stopwatch.Stop();

                var responseTimeMs = (int)stopwatch.ElapsedMilliseconds;
                endpoint.LastCheck = DateTimeOffset.UtcNow;
                endpoint.LastResponseTime = responseTimeMs;

                if (response.IsSuccessStatusCode)
                {
                    endpoint.ConsecutiveFailures = 0;
                    
                    // Determine service state based on response time
                    var state = responseTimeMs switch
                    {
                        < 100 => ServiceState.Online,
                        <= 1000 => ServiceState.Degraded,
                        _ => ServiceState.Degraded
                    };

                    status = new ServiceStatus(serviceName, state)
                    {
                        LastHeartbeat = DateTimeOffset.UtcNow,
                        Metrics = new Dictionary<string, object>
                        {
                            ["response_time_ms"] = responseTimeMs,
                            ["status_code"] = (int)response.StatusCode,
                            ["consecutive_failures"] = 0,
                            ["platform"] = GetCurrentPlatform(),
                            ["endpoint"] = endpoint.HealthEndpoint
                        }
                    };

                    _logger.LogDebug("Health check successful for {ServiceName}: {ResponseTime}ms", 
                        serviceName, responseTimeMs);
                }
                else
                {
                    endpoint.ConsecutiveFailures++;
                    status = new ServiceStatus(serviceName, ServiceState.Error)
                    {
                        ErrorMessage = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}",
                        Metrics = new Dictionary<string, object>
                        {
                            ["error_code"] = $"HTTP-{(int)response.StatusCode}-001",
                            ["response_time_ms"] = responseTimeMs,
                            ["status_code"] = (int)response.StatusCode,
                            ["consecutive_failures"] = endpoint.ConsecutiveFailures,
                            ["platform"] = GetCurrentPlatform(),
                            ["endpoint"] = endpoint.HealthEndpoint
                        }
                    };

                    _logger.LogWarning("Health check failed for {ServiceName}: HTTP {StatusCode} at {Endpoint}", 
                        serviceName, response.StatusCode, endpoint.HealthEndpoint);
                }
            }
            catch (TaskCanceledException)
            {
                stopwatch.Stop();
                endpoint.ConsecutiveFailures++;
                endpoint.LastCheck = DateTimeOffset.UtcNow;

                status = new ServiceStatus(serviceName, ServiceState.Offline)
                {
                    ErrorMessage = "Request timeout",
                    Metrics = new Dictionary<string, object>
                    {
                        ["error_code"] = "SRV-TIMEOUT-001",
                        ["response_time_ms"] = (int)stopwatch.ElapsedMilliseconds,
                        ["consecutive_failures"] = endpoint.ConsecutiveFailures,
                        ["platform"] = GetCurrentPlatform(),
                        ["endpoint"] = endpoint.HealthEndpoint
                    }
                };

                _logger.LogWarning("Health check timeout for {ServiceName} after {ElapsedMs}ms at {Endpoint}", 
                    serviceName, stopwatch.ElapsedMilliseconds, endpoint.HealthEndpoint);
            }
            catch (HttpRequestException ex)
            {
                stopwatch.Stop();
                endpoint.ConsecutiveFailures++;
                endpoint.LastCheck = DateTimeOffset.UtcNow;

                status = new ServiceStatus(serviceName, ServiceState.Offline)
                {
                    ErrorMessage = $"Connection error: {ex.Message}",
                    Metrics = new Dictionary<string, object>
                    {
                        ["error_code"] = "SRV-CONN-001",
                        ["response_time_ms"] = (int)stopwatch.ElapsedMilliseconds,
                        ["consecutive_failures"] = endpoint.ConsecutiveFailures,
                        ["platform"] = GetCurrentPlatform(),
                        ["endpoint"] = endpoint.HealthEndpoint
                    }
                };

                _logger.LogError(ex, "Health check connection error for {ServiceName} at {Endpoint}", serviceName, endpoint.HealthEndpoint);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                endpoint.ConsecutiveFailures++;
                endpoint.LastCheck = DateTimeOffset.UtcNow;

                status = new ServiceStatus(serviceName, ServiceState.Error)
                {
                    ErrorMessage = $"Unexpected error: {ex.Message}",
                    Metrics = new Dictionary<string, object>
                    {
                        ["error_code"] = "SRV-UNKNOWN-001",
                        ["response_time_ms"] = (int)stopwatch.ElapsedMilliseconds,
                        ["consecutive_failures"] = endpoint.ConsecutiveFailures,
                        ["platform"] = GetCurrentPlatform(),
                        ["endpoint"] = endpoint.HealthEndpoint
                    }
                };

                _logger.LogError(ex, "Unexpected error during health check for {ServiceName} at {Endpoint}", serviceName, endpoint.HealthEndpoint);
            }

            return status;
        }

        /// <summary>
        /// Resets the consecutive failure count for a service, clearing any backoff delay.
        /// </summary>
        public void ResetServiceFailures(string serviceName)
        {
            if (_serviceEndpoints.TryGetValue(serviceName, out var endpoint))
            {
                endpoint.ConsecutiveFailures = 0;
                _logger.LogInformation("Reset consecutive failures for service: {ServiceName}", serviceName);
            }
        }

        /// <summary>
        /// Gets the current failure count for a service.
        /// </summary>
        public int GetServiceFailureCount(string serviceName)
        {
            return _serviceEndpoints.TryGetValue(serviceName, out var endpoint) 
                ? endpoint.ConsecutiveFailures 
                : 0;
        }

        /// <summary>
        /// Gets all registered service names.
        /// </summary>
        public IEnumerable<string> GetRegisteredServices()
        {
            return _serviceEndpoints.Keys;
        }

        /// <summary>
        /// Gets the display name for a service.
        /// </summary>
        public string GetServiceDisplayName(string serviceName)
        {
            return _serviceEndpoints.TryGetValue(serviceName, out var endpoint) 
                ? endpoint.DisplayName 
                : serviceName;
        }

        /// <summary>
        /// Calculates exponential backoff delay based on consecutive failures.
        /// </summary>
        private TimeSpan CalculateBackoffDelay(int consecutiveFailures)
        {
            if (consecutiveFailures < 1) return TimeSpan.Zero;

            // Exponential backoff: 2^failures seconds with jitter, max 5 minutes
            var baseDelay = Math.Min(Math.Pow(2, consecutiveFailures), 300);
            var jitter = _random.NextDouble() * 0.3; // ±30% jitter
            var delay = baseDelay * (1 + jitter);
            
            return TimeSpan.FromSeconds(delay);
        }

        /// <summary>
        /// Gets the current platform name for debugging purposes.
        /// </summary>
        private static string GetCurrentPlatform()
        {
#if ANDROID
            return "Android";
#elif IOS
            return "iOS";
#elif WINDOWS
            return "Windows";
#elif MACCATALYST
            return "macOS";
#else
            return "Unknown";
#endif
        }
    }

    /// <summary>
    /// Represents a service endpoint for health checking.
    /// </summary>
    internal class ServiceEndpoint
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string HealthEndpoint { get; set; } = string.Empty;
        public DateTimeOffset LastCheck { get; set; }
        public int LastResponseTime { get; set; }
        public int ConsecutiveFailures { get; set; }
    }
}
