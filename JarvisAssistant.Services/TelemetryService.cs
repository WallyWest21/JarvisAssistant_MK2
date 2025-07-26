using JarvisAssistant.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace JarvisAssistant.Services
{
    /// <summary>
    /// Local telemetry service that respects user privacy and can optionally send data to remote endpoints.
    /// </summary>
    public class TelemetryService : ITelemetryService
    {
        private readonly ILogger<TelemetryService> _logger;
        private readonly HttpClient? _httpClient;
        private readonly string _localStoragePath;
        private TelemetrySettings _settings;
        private readonly string _sessionId;
        private readonly DateTime _sessionStart;
        private readonly Queue<TelemetryEvent> _eventQueue;
        private readonly SemaphoreSlim _settingsLock;

        public TelemetryService(ILogger<TelemetryService> logger, HttpClient? httpClient = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = httpClient;
            _localStoragePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JarvisAssistant", "Telemetry");
            _sessionId = Guid.NewGuid().ToString();
            _sessionStart = DateTime.UtcNow;
            _eventQueue = new Queue<TelemetryEvent>();
            _settingsLock = new SemaphoreSlim(1, 1);

            // Ensure local storage directory exists
            Directory.CreateDirectory(_localStoragePath);

            // Load settings
            _settings = LoadSettingsAsync().GetAwaiter().GetResult();

            _logger.LogInformation("Telemetry service initialized with session ID: {SessionId}", _sessionId);
        }

        /// <inheritdoc/>
        public async Task TrackEventAsync(string eventName, Dictionary<string, object>? properties = null)
        {
            if (!_settings.IsEnabled || !_settings.EnableUsageAnalytics)
            {
                _logger.LogDebug("Event tracking disabled: {EventName}", eventName);
                return;
            }

            var telemetryEvent = new TelemetryEvent
            {
                EventName = eventName,
                Timestamp = DateTime.UtcNow,
                SessionId = _sessionId,
                Properties = properties ?? new Dictionary<string, object>(),
                EventType = TelemetryEventType.Event
            };

            await QueueEventAsync(telemetryEvent);
            _logger.LogDebug("Tracked event: {EventName}", eventName);
        }

        /// <inheritdoc/>
        public async Task TrackMetricAsync(string metricName, double value, Dictionary<string, string>? properties = null)
        {
            if (!_settings.IsEnabled || !_settings.EnablePerformanceMetrics)
            {
                _logger.LogDebug("Metric tracking disabled: {MetricName}", metricName);
                return;
            }

            var telemetryEvent = new TelemetryEvent
            {
                EventName = metricName,
                Timestamp = DateTime.UtcNow,
                SessionId = _sessionId,
                Properties = new Dictionary<string, object> { ["value"] = value },
                EventType = TelemetryEventType.Metric
            };

            if (properties != null)
            {
                foreach (var kvp in properties)
                {
                    telemetryEvent.Properties[kvp.Key] = kvp.Value;
                }
            }

            await QueueEventAsync(telemetryEvent);
            _logger.LogDebug("Tracked metric: {MetricName} = {Value}", metricName, value);
        }

        /// <inheritdoc/>
        public async Task TrackExceptionAsync(Exception exception, Dictionary<string, string>? properties = null, Dictionary<string, double>? measurements = null)
        {
            if (!_settings.IsEnabled || !_settings.EnableErrorReporting)
            {
                _logger.LogDebug("Exception tracking disabled");
                return;
            }

            var telemetryEvent = new TelemetryEvent
            {
                EventName = "Exception",
                Timestamp = DateTime.UtcNow,
                SessionId = _sessionId,
                Properties = new Dictionary<string, object>
                {
                    ["exceptionType"] = exception.GetType().Name,
                    ["message"] = exception.Message,
                    ["stackTrace"] = exception.StackTrace ?? string.Empty
                },
                EventType = TelemetryEventType.Exception
            };

            if (properties != null)
            {
                foreach (var kvp in properties)
                {
                    telemetryEvent.Properties[kvp.Key] = kvp.Value;
                }
            }

            if (measurements != null)
            {
                foreach (var kvp in measurements)
                {
                    telemetryEvent.Properties[kvp.Key] = kvp.Value;
                }
            }

            await QueueEventAsync(telemetryEvent);
            _logger.LogDebug("Tracked exception: {ExceptionType}", exception.GetType().Name);
        }

        /// <inheritdoc/>
        public async Task TrackFeatureUsageAsync(string featureName, Dictionary<string, object>? properties = null)
        {
            if (!_settings.IsEnabled || !_settings.EnableFeatureTracking)
            {
                _logger.LogDebug("Feature tracking disabled: {FeatureName}", featureName);
                return;
            }

            var telemetryEvent = new TelemetryEvent
            {
                EventName = "FeatureUsage",
                Timestamp = DateTime.UtcNow,
                SessionId = _sessionId,
                Properties = new Dictionary<string, object>
                {
                    ["featureName"] = featureName
                },
                EventType = TelemetryEventType.FeatureUsage
            };

            if (properties != null)
            {
                foreach (var kvp in properties)
                {
                    telemetryEvent.Properties[kvp.Key] = kvp.Value;
                }
            }

            await QueueEventAsync(telemetryEvent);
            _logger.LogDebug("Tracked feature usage: {FeatureName}", featureName);
        }

        /// <inheritdoc/>
        public async Task SetUserPropertiesAsync(string userId, Dictionary<string, object> properties)
        {
            if (!_settings.IsEnabled)
            {
                _logger.LogDebug("User properties tracking disabled");
                return;
            }

            await _settingsLock.WaitAsync();
            try
            {
                _settings.UserId = _settings.UseAnonymousId ? GenerateAnonymousId(userId) : userId;
                await SaveSettingsAsync();

                var telemetryEvent = new TelemetryEvent
                {
                    EventName = "UserProperties",
                    Timestamp = DateTime.UtcNow,
                    SessionId = _sessionId,
                    Properties = new Dictionary<string, object>(properties)
                    {
                        ["userId"] = _settings.UserId
                    },
                    EventType = TelemetryEventType.UserProperties
                };

                await QueueEventAsync(telemetryEvent);
                _logger.LogDebug("Set user properties for user: {UserId}", _settings.UserId);
            }
            finally
            {
                _settingsLock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<TelemetrySettings> GetSettingsAsync()
        {
            await _settingsLock.WaitAsync();
            try
            {
                return new TelemetrySettings
                {
                    IsEnabled = _settings.IsEnabled,
                    EnableUsageAnalytics = _settings.EnableUsageAnalytics,
                    EnableErrorReporting = _settings.EnableErrorReporting,
                    EnablePerformanceMetrics = _settings.EnablePerformanceMetrics,
                    EnableFeatureTracking = _settings.EnableFeatureTracking,
                    UserId = _settings.UserId,
                    UseAnonymousId = _settings.UseAnonymousId
                };
            }
            finally
            {
                _settingsLock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task UpdateSettingsAsync(TelemetrySettings settings)
        {
            await _settingsLock.WaitAsync();
            try
            {
                _settings = settings;
                await SaveSettingsAsync();
                _logger.LogInformation("Telemetry settings updated. Enabled: {IsEnabled}", settings.IsEnabled);
            }
            finally
            {
                _settingsLock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task FlushAsync()
        {
            try
            {
                // Save all queued events to local storage
                await SaveQueuedEventsAsync();
                
                // Optionally send to remote endpoint if configured
                await SendQueuedEventsAsync();
                
                _logger.LogDebug("Telemetry data flushed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error flushing telemetry data");
            }
        }

        private async Task QueueEventAsync(TelemetryEvent telemetryEvent)
        {
            try
            {
                lock (_eventQueue)
                {
                    _eventQueue.Enqueue(telemetryEvent);

                    // Keep queue size manageable
                    while (_eventQueue.Count > 1000)
                    {
                        _eventQueue.Dequeue();
                    }
                }

                // Periodically flush events
                if (_eventQueue.Count >= 10)
                {
                    await Task.Run(async () => await SaveQueuedEventsAsync());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error queuing telemetry event: {EventName}", telemetryEvent.EventName);
            }
        }

        private async Task SaveQueuedEventsAsync()
        {
            try
            {
                List<TelemetryEvent> eventsToSave;
                lock (_eventQueue)
                {
                    if (_eventQueue.Count == 0) return;
                    
                    eventsToSave = new List<TelemetryEvent>(_eventQueue);
                    _eventQueue.Clear();
                }

                var fileName = $"telemetry_{DateTime.UtcNow:yyyy-MM-dd}.json";
                var filePath = Path.Combine(_localStoragePath, fileName);

                var existingEvents = new List<TelemetryEvent>();
                if (File.Exists(filePath))
                {
                    var existingJson = await File.ReadAllTextAsync(filePath);
                    existingEvents = JsonSerializer.Deserialize<List<TelemetryEvent>>(existingJson) ?? new List<TelemetryEvent>();
                }

                existingEvents.AddRange(eventsToSave);

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var json = JsonSerializer.Serialize(existingEvents, options);
                await File.WriteAllTextAsync(filePath, json);

                _logger.LogDebug("Saved {Count} telemetry events to local storage", eventsToSave.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving telemetry events to local storage");
            }
        }

        private async Task SendQueuedEventsAsync()
        {
            if (_httpClient == null)
            {
                _logger.LogDebug("HTTP client not configured, skipping remote telemetry upload");
                return;
            }

            try
            {
                // Implementation for sending to remote analytics service
                // This would be configured based on the chosen analytics provider
                // For now, we just log that we would send the data
                _logger.LogDebug("Would send telemetry data to remote endpoint (not implemented)");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending telemetry data to remote endpoint");
            }
        }

        private async Task<TelemetrySettings> LoadSettingsAsync()
        {
            try
            {
                var settingsPath = Path.Combine(_localStoragePath, "settings.json");
                if (!File.Exists(settingsPath))
                {
                    var defaultSettings = new TelemetrySettings
                    {
                        UserId = _settings?.UserId ?? GenerateAnonymousId(Environment.MachineName)
                    };
                    await SaveSettingsAsync(defaultSettings);
                    return defaultSettings;
                }

                var json = await File.ReadAllTextAsync(settingsPath);
                var settings = JsonSerializer.Deserialize<TelemetrySettings>(json) ?? new TelemetrySettings();
                
                // Ensure we have a user ID
                if (string.IsNullOrEmpty(settings.UserId))
                {
                    settings.UserId = GenerateAnonymousId(Environment.MachineName);
                    await SaveSettingsAsync(settings);
                }

                return settings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading telemetry settings, using defaults");
                return new TelemetrySettings
                {
                    UserId = GenerateAnonymousId(Environment.MachineName)
                };
            }
        }

        private async Task SaveSettingsAsync(TelemetrySettings? settings = null)
        {
            try
            {
                var settingsToSave = settings ?? _settings;
                var settingsPath = Path.Combine(_localStoragePath, "settings.json");
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var json = JsonSerializer.Serialize(settingsToSave, options);
                await File.WriteAllTextAsync(settingsPath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving telemetry settings");
            }
        }

        private static string GenerateAnonymousId(string input)
        {
            // Generate a consistent anonymous ID based on input
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input + "JarvisAssistant"));
            return Convert.ToHexString(hash)[..16].ToLowerInvariant();
        }
    }

    /// <summary>
    /// Represents a telemetry event.
    /// </summary>
    public class TelemetryEvent
    {
        public string EventName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public Dictionary<string, object> Properties { get; set; } = new();
        public TelemetryEventType EventType { get; set; }
    }

    /// <summary>
    /// Types of telemetry events.
    /// </summary>
    public enum TelemetryEventType
    {
        Event,
        Metric,
        Exception,
        FeatureUsage,
        UserProperties
    }
}
