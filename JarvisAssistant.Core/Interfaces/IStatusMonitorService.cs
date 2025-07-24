using JarvisAssistant.Core.Models;
using System.ComponentModel;

namespace JarvisAssistant.Core.Interfaces
{
    /// <summary>
    /// Provides methods for monitoring and reporting the status of various services.
    /// </summary>
    public interface IStatusMonitorService : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets an observable collection of service status updates that can be monitored for changes.
        /// </summary>
        /// <value>An observable that emits service status updates whenever a service status changes.</value>
        IObservable<ServiceStatus> ServiceStatusUpdates { get; }

        /// <summary>
        /// Gets the current status of all monitored services.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a collection of service statuses.</returns>
        Task<IEnumerable<ServiceStatus>> GetAllServiceStatusesAsync();

        /// <summary>
        /// Gets the status of a specific service by name.
        /// </summary>
        /// <param name="serviceName">The name of the service to get status for.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the service status, or null if not found.</returns>
        Task<ServiceStatus?> GetServiceStatusAsync(string serviceName);

        /// <summary>
        /// Starts monitoring the specified service.
        /// </summary>
        /// <param name="serviceName">The name of the service to start monitoring.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task StartMonitoringAsync(string serviceName);

        /// <summary>
        /// Stops monitoring the specified service.
        /// </summary>
        /// <param name="serviceName">The name of the service to stop monitoring.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task StopMonitoringAsync(string serviceName);

        /// <summary>
        /// Starts monitoring all registered services.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task StartMonitoringAllAsync();

        /// <summary>
        /// Stops monitoring all services.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task StopMonitoringAllAsync();

        /// <summary>
        /// Resets the failure count and backoff state for a specific service, forcing an immediate health check.
        /// </summary>
        /// <param name="serviceName">The name of the service to reset failures for.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task ResetServiceFailuresAsync(string serviceName);
    }
}
