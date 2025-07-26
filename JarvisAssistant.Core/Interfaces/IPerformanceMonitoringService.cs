using JarvisAssistant.Core.Models;

namespace JarvisAssistant.Core.Interfaces
{
    /// <summary>
    /// Provides methods for advanced performance monitoring and optimization.
    /// </summary>
    public interface IPerformanceMonitoringService
    {
        /// <summary>
        /// Tracks response times for different types of requests.
        /// </summary>
        /// <param name="requestType">Type of request being tracked.</param>
        /// <param name="duration">Duration of the request.</param>
        /// <param name="metadata">Additional metadata about the request.</param>
        Task TrackResponseTimeAsync(RequestType requestType, TimeSpan duration, Dictionary<string, object>? metadata = null);

        /// <summary>
        /// Gets current performance statistics.
        /// </summary>
        /// <returns>Current performance metrics.</returns>
        Task<PerformanceStatistics> GetPerformanceStatisticsAsync();

        /// <summary>
        /// Gets throughput metrics for a specific time period.
        /// </summary>
        /// <param name="period">Time period to analyze.</param>
        /// <returns>Throughput metrics.</returns>
        Task<ThroughputMetrics> GetThroughputMetricsAsync(TimeSpan period);

        /// <summary>
        /// Monitors memory usage patterns.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Stream of memory usage data.</returns>
        IAsyncEnumerable<MemoryUsageSnapshot> MonitorMemoryUsageAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Tracks model switching frequency and performance impact.
        /// </summary>
        /// <param name="fromModel">Model being switched from.</param>
        /// <param name="toModel">Model being switched to.</param>
        /// <param name="switchDuration">Time taken to switch.</param>
        Task TrackModelSwitchAsync(string fromModel, string toModel, TimeSpan switchDuration);

        /// <summary>
        /// Generates performance report for a specific time range.
        /// </summary>
        /// <param name="startTime">Start time for the report.</param>
        /// <param name="endTime">End time for the report.</param>
        /// <returns>Comprehensive performance report.</returns>
        Task<PerformanceReport> GeneratePerformanceReportAsync(DateTime startTime, DateTime endTime);

        /// <summary>
        /// Detects performance bottlenecks and provides recommendations.
        /// </summary>
        /// <returns>List of detected bottlenecks and optimization suggestions.</returns>
        Task<IEnumerable<PerformanceBottleneck>> DetectBottlenecksAsync();

        /// <summary>
        /// Event triggered when performance metrics exceed thresholds.
        /// </summary>
        event EventHandler<PerformanceAlertEventArgs>? PerformanceAlert;
    }
}
