using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using JarvisAssistant.Core.Interfaces;

namespace JarvisAssistant.Services.Extensions
{
    /// <summary>
    /// Extension methods for adding voice service monitoring to existing status monitoring.
    /// </summary>
    public static class VoiceServiceMonitoringExtensions
    {
        /// <summary>
        /// Adds voice service health monitoring to the existing status monitoring system.
        /// This will replace the standard IStatusMonitorService with the CustomStatusMonitorService
        /// that includes voice service health checking.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddVoiceServiceMonitoring(this IServiceCollection services)
        {
            // Register the voice service health checker
            services.AddSingleton<VoiceServiceHealthChecker>();

            // Replace the IStatusMonitorService with our custom implementation
            // that includes voice service monitoring
            services.Replace(ServiceDescriptor.Singleton<IStatusMonitorService>(serviceProvider =>
            {
                // Get the base status monitor service (it's already registered by AddStatusMonitoring)
                var baseStatusMonitor = ActivatorUtilities.CreateInstance<StatusMonitorService>(serviceProvider);

                // Wrap it with our custom status monitor that adds voice service monitoring
                return new CustomStatusMonitorService(
                    baseStatusMonitor,
                    serviceProvider,
                    serviceProvider.GetRequiredService<ILogger<CustomStatusMonitorService>>());
            }));

            return services;
        }
    }
}
