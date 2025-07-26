namespace JarvisAssistant.Core.Interfaces
{
    /// <summary>
    /// Interface for telemetry and analytics services.
    /// </summary>
    public interface ITelemetryService
    {
        /// <summary>
        /// Tracks a user action or event.
        /// </summary>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="properties">Additional properties to track.</param>
        Task TrackEventAsync(string eventName, Dictionary<string, object>? properties = null);

        /// <summary>
        /// Tracks application performance metrics.
        /// </summary>
        /// <param name="metricName">The name of the metric.</param>
        /// <param name="value">The metric value.</param>
        /// <param name="properties">Additional properties.</param>
        Task TrackMetricAsync(string metricName, double value, Dictionary<string, string>? properties = null);

        /// <summary>
        /// Tracks an exception or error.
        /// </summary>
        /// <param name="exception">The exception to track.</param>
        /// <param name="properties">Additional properties.</param>
        /// <param name="measurements">Additional measurements.</param>
        Task TrackExceptionAsync(Exception exception, Dictionary<string, string>? properties = null, Dictionary<string, double>? measurements = null);

        /// <summary>
        /// Tracks feature usage.
        /// </summary>
        /// <param name="featureName">The name of the feature.</param>
        /// <param name="properties">Additional properties.</param>
        Task TrackFeatureUsageAsync(string featureName, Dictionary<string, object>? properties = null);

        /// <summary>
        /// Sets user properties for analytics.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="properties">User properties.</param>
        Task SetUserPropertiesAsync(string userId, Dictionary<string, object> properties);

        /// <summary>
        /// Gets telemetry settings.
        /// </summary>
        /// <returns>Current telemetry settings.</returns>
        Task<TelemetrySettings> GetSettingsAsync();

        /// <summary>
        /// Updates telemetry settings.
        /// </summary>
        /// <param name="settings">The new settings.</param>
        Task UpdateSettingsAsync(TelemetrySettings settings);

        /// <summary>
        /// Flushes pending telemetry data.
        /// </summary>
        Task FlushAsync();
    }

    /// <summary>
    /// Telemetry settings for user privacy control.
    /// </summary>
    public class TelemetrySettings
    {
        /// <summary>
        /// Gets or sets whether telemetry is enabled.
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets whether usage analytics are enabled.
        /// </summary>
        public bool EnableUsageAnalytics { get; set; } = true;

        /// <summary>
        /// Gets or sets whether error reporting is enabled.
        /// </summary>
        public bool EnableErrorReporting { get; set; } = true;

        /// <summary>
        /// Gets or sets whether performance metrics are enabled.
        /// </summary>
        public bool EnablePerformanceMetrics { get; set; } = true;

        /// <summary>
        /// Gets or sets whether feature usage tracking is enabled.
        /// </summary>
        public bool EnableFeatureTracking { get; set; } = true;

        /// <summary>
        /// Gets or sets the user ID for analytics (can be anonymous).
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// Gets or sets whether to use anonymous user ID.
        /// </summary>
        public bool UseAnonymousId { get; set; } = true;
    }
}
