using Microsoft.Extensions.DependencyInjection;
using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Services.Hubs;

namespace JarvisAssistant.Services.Extensions
{
    /// <summary>
    /// Extension methods for registering status monitoring services.
    /// </summary>
    public static class StatusMonitoringExtensions
    {
        /// <summary>
        /// Adds status monitoring services to the dependency injection container.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddStatusMonitoring(this IServiceCollection services)
        {
            // Register HttpClient for health checks
            services.AddHttpClient<ServiceHealthChecker>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
            });

            // Register health checker
            services.AddSingleton<ServiceHealthChecker>();

            // Register status monitor service
            services.AddSingleton<IStatusMonitorService, StatusMonitorService>();

            // Register SignalR hub client
            services.AddSingleton<StatusMonitoringHub>();

            return services;
        }

        /// <summary>
        /// Configures status monitoring with custom settings.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Action to configure monitoring options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddStatusMonitoring(
            this IServiceCollection services, 
            Action<StatusMonitoringOptions> configureOptions)
        {
            var options = new StatusMonitoringOptions();
            configureOptions(options);

            services.AddSingleton(options);
            services.AddStatusMonitoring();

            return services;
        }
    }

    /// <summary>
    /// Configuration options for status monitoring.
    /// </summary>
    public class StatusMonitoringOptions
    {
        /// <summary>
        /// Gets or sets the monitoring interval in seconds.
        /// </summary>
        public int MonitoringIntervalSeconds { get; set; } = 5;

        /// <summary>
        /// Gets or sets the health check timeout in seconds.
        /// </summary>
        public int HealthCheckTimeoutSeconds { get; set; } = 10;

        /// <summary>
        /// Gets or sets the SignalR hub URL.
        /// </summary>
        public string? SignalRHubUrl { get; set; }

        /// <summary>
        /// Gets or sets whether to automatically start monitoring all registered services.
        /// </summary>
        public bool AutoStartMonitoring { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of consecutive failures before backoff.
        /// </summary>
        public int MaxConsecutiveFailures { get; set; } = 3;

        /// <summary>
        /// Gets or sets custom service endpoints for monitoring.
        /// </summary>
        public Dictionary<string, ServiceEndpointConfig> ServiceEndpoints { get; set; } = new();
    }

    /// <summary>
    /// Configuration for a service endpoint.
    /// </summary>
    public class ServiceEndpointConfig
    {
        /// <summary>
        /// Gets or sets the service name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the health check endpoint URL.
        /// </summary>
        public string HealthEndpoint { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether this service is enabled for monitoring.
        /// </summary>
        public bool Enabled { get; set; } = true;
    }
}
