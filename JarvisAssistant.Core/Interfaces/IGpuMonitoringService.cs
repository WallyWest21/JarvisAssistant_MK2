using JarvisAssistant.Core.Models;

namespace JarvisAssistant.Core.Interfaces
{
    /// <summary>
    /// Provides methods for monitoring GPU performance and resource usage.
    /// </summary>
    public interface IGpuMonitoringService
    {
        /// <summary>
        /// Gets current GPU status including availability and specifications.
        /// </summary>
        /// <returns>Current GPU status information.</returns>
        Task<GpuStatus> GetGpuStatusAsync();

        /// <summary>
        /// Gets current GPU status including availability and specifications.
        /// For compatibility with test code.
        /// </summary>
        /// <returns>Current GPU status information.</returns>
        Task<GpuStatus?> GetCurrentGpuStatusAsync();

        /// <summary>
        /// Gets current VRAM usage statistics.
        /// </summary>
        /// <returns>VRAM usage information in bytes.</returns>
        Task<VramUsage> GetVramUsageAsync();

        /// <summary>
        /// Monitors GPU metrics over time.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Stream of GPU performance metrics.</returns>
        IAsyncEnumerable<GpuPerformanceMetrics> MonitorPerformanceAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Starts real-time performance monitoring.
        /// </summary>
        Task StartMonitoringAsync();

        /// <summary>
        /// Stops performance monitoring.
        /// </summary>
        Task StopMonitoringAsync();

        /// <summary>
        /// Gets historical performance metrics.
        /// </summary>
        /// <param name="duration">Time range for historical data.</param>
        /// <returns>Historical performance data.</returns>
        Task<PerformanceHistory> GetPerformanceHistoryAsync(TimeSpan duration);

        /// <summary>
        /// Event triggered when GPU status changes.
        /// </summary>
        event EventHandler<GpuStatusChangedEventArgs>? GpuStatusChanged;

        /// <summary>
        /// Event triggered when VRAM usage exceeds threshold.
        /// </summary>
        event EventHandler<VramThresholdExceededEventArgs>? VramThresholdExceeded;
    }
}
