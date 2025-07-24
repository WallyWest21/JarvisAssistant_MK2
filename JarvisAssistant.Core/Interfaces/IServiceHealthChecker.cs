using JarvisAssistant.Core.Models;

namespace JarvisAssistant.Core.Interfaces
{
    /// <summary>
    /// Interface for service health checking functionality.
    /// </summary>
    public interface IServiceHealthChecker
    {
        /// <summary>
        /// Registers a service endpoint for health checking.
        /// </summary>
        void RegisterService(string serviceName, string healthEndpoint, string? displayName = null);

        /// <summary>
        /// Performs a health check on the specified service.
        /// </summary>
        Task<ServiceStatus> CheckServiceHealthAsync(string serviceName);

        /// <summary>
        /// Resets the consecutive failure count for a service, clearing any backoff delay.
        /// </summary>
        void ResetServiceFailures(string serviceName);

        /// <summary>
        /// Gets the current failure count for a service.
        /// </summary>
        int GetServiceFailureCount(string serviceName);

        /// <summary>
        /// Gets all registered service names.
        /// </summary>
        IEnumerable<string> GetRegisteredServices();

        /// <summary>
        /// Gets the display name for a service.
        /// </summary>
        string GetServiceDisplayName(string serviceName);
    }
}
