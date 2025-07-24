namespace JarvisAssistant.Core.Models
{
    /// <summary>
    /// Represents the possible states of a service.
    /// </summary>
    public enum ServiceState
    {
        /// <summary>
        /// The service is offline or not responding.
        /// </summary>
        Offline,

        /// <summary>
        /// The service is starting up.
        /// </summary>
        Starting,

        /// <summary>
        /// The service is online and functioning normally.
        /// </summary>
        Online,

        /// <summary>
        /// The service is experiencing degraded performance.
        /// </summary>
        Degraded,

        /// <summary>
        /// The service has encountered an error.
        /// </summary>
        Error,

        /// <summary>
        /// The service is shutting down.
        /// </summary>
        Stopping
    }

    /// <summary>
    /// Represents the current status of a service including health and performance metrics.
    /// </summary>
    public class ServiceStatus
    {
        /// <summary>
        /// Gets or sets the name of the service.
        /// </summary>
        public string ServiceName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the current state of the service.
        /// </summary>
        public ServiceState State { get; set; } = ServiceState.Offline;

        /// <summary>
        /// Gets or sets the timestamp of the last successful heartbeat or health check.
        /// </summary>
        public DateTimeOffset LastHeartbeat { get; set; } = DateTimeOffset.MinValue;

        /// <summary>
        /// Gets or sets performance and diagnostic metrics for the service.
        /// </summary>
        public Dictionary<string, object>? Metrics { get; set; }

        /// <summary>
        /// Gets or sets the error message if the service is in an error state.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the version of the service.
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Gets or sets the uptime of the service.
        /// </summary>
        public TimeSpan? Uptime { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceStatus"/> class.
        /// </summary>
        public ServiceStatus()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceStatus"/> class with the specified service name and state.
        /// </summary>
        /// <param name="serviceName">The name of the service.</param>
        /// <param name="state">The current state of the service.</param>
        public ServiceStatus(string serviceName, ServiceState state = ServiceState.Offline)
        {
            ServiceName = serviceName;
            State = state;
            LastHeartbeat = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Gets a value indicating whether the service is healthy (online or degraded).
        /// </summary>
        public bool IsHealthy => State == ServiceState.Online || State == ServiceState.Degraded;

        /// <summary>
        /// Gets a value indicating whether the service is in an error state.
        /// </summary>
        public bool IsError => State == ServiceState.Error;

        /// <summary>
        /// Gets a value indicating whether the service has an error.
        /// </summary>
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        /// <summary>
        /// Gets a value indicating whether the service is currently being checked.
        /// </summary>
        public bool IsChecking => State == ServiceState.Starting;

        /// <summary>
        /// Gets the error code from metrics if available.
        /// </summary>
        public string? ErrorCode => Metrics?.TryGetValue("error_code", out var code) == true ? code?.ToString() : null;

        /// <summary>
        /// Updates the heartbeat timestamp to the current time.
        /// </summary>
        public void UpdateHeartbeat()
        {
            LastHeartbeat = DateTimeOffset.UtcNow;
        }
    }
}
