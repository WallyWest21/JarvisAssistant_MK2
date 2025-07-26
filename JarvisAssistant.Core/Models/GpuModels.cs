using System.ComponentModel;

namespace JarvisAssistant.Core.Models
{
    /// <summary>
    /// Represents GPU status and specifications.
    /// </summary>
    public class GpuStatus
    {
        /// <summary>
        /// Gets or sets whether GPU is available.
        /// </summary>
        public bool IsAvailable { get; set; }

        /// <summary>
        /// Gets or sets the GPU name/model.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the driver version.
        /// </summary>
        public string? DriverVersion { get; set; }

        /// <summary>
        /// Gets or sets whether CUDA is available.
        /// </summary>
        public bool CudaAvailable { get; set; }

        /// <summary>
        /// Gets or sets total VRAM in bytes.
        /// </summary>
        public long TotalVramBytes { get; set; }

        /// <summary>
        /// Gets or sets current GPU utilization percentage.
        /// </summary>
        public float UtilizationPercent { get; set; }

        /// <summary>
        /// Gets or sets current GPU temperature in Celsius.
        /// </summary>
        public int TemperatureCelsius { get; set; }

        /// <summary>
        /// Gets or sets current power consumption in watts.
        /// </summary>
        public float PowerConsumptionWatts { get; set; }

        /// <summary>
        /// Gets or sets VRAM usage information.
        /// </summary>
        public VramUsage VramUsage { get; set; } = new();

        /// <summary>
        /// Gets or sets GPU compute capability.
        /// </summary>
        public string ComputeCapability { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets GPU clock speeds.
        /// </summary>
        public GpuClockSpeeds ClockSpeeds { get; set; } = new();

        /// <summary>
        /// Convenience properties for compatibility with test code
        /// </summary>
        public int Temperature => TemperatureCelsius;
        public float PowerUsageWatts => PowerConsumptionWatts;
    }

    /// <summary>
    /// Represents VRAM usage statistics.
    /// </summary>
    public class VramUsage
    {
        /// <summary>
        /// Gets or sets total VRAM in bytes.
        /// </summary>
        public long TotalBytes { get; set; }

        /// <summary>
        /// Gets or sets used VRAM in bytes.
        /// </summary>
        public long UsedBytes { get; set; }

        /// <summary>
        /// Gets or sets free VRAM in bytes.
        /// </summary>
        public long FreeBytes { get; set; }

        /// <summary>
        /// Gets or sets usage percentage.
        /// </summary>
        public float UsagePercent => TotalBytes > 0 ? (float)UsedBytes / TotalBytes * 100 : 0;

        /// <summary>
        /// Gets or sets VRAM usage by models.
        /// </summary>
        public Dictionary<string, long> ModelUsage { get; set; } = new();

        /// <summary>
        /// Gets or sets VRAM usage by processes.
        /// </summary>
        public Dictionary<string, long> ProcessUsage { get; set; } = new();
    }

    /// <summary>
    /// GPU clock speeds information.
    /// </summary>
    public class GpuClockSpeeds
    {
        /// <summary>
        /// Gets or sets core clock speed in MHz.
        /// </summary>
        public int CoreClockMHz { get; set; }

        /// <summary>
        /// Gets or sets memory clock speed in MHz.
        /// </summary>
        public int MemoryClockMHz { get; set; }

        /// <summary>
        /// Gets or sets boost clock speed in MHz.
        /// </summary>
        public int BoostClockMHz { get; set; }
    }

    /// <summary>
    /// Represents GPU performance metrics over time.
    /// </summary>
    public class GpuPerformanceMetrics
    {
        /// <summary>
        /// Gets or sets timestamp of the metrics.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets GPU utilization percentage.
        /// </summary>
        public float GpuUtilization { get; set; }

        /// <summary>
        /// Gets or sets memory utilization percentage.
        /// </summary>
        public float MemoryUtilization { get; set; }

        /// <summary>
        /// Gets or sets GPU temperature in Celsius.
        /// </summary>
        public int Temperature { get; set; }

        /// <summary>
        /// Gets or sets power consumption in watts.
        /// </summary>
        public float PowerConsumption { get; set; }

        /// <summary>
        /// Gets or sets average inference time for recent requests.
        /// </summary>
        public TimeSpan AverageInferenceTime { get; set; }

        /// <summary>
        /// Gets or sets recent inference times.
        /// </summary>
        public List<TimeSpan> RecentInferenceTimes { get; set; } = new();

        /// <summary>
        /// Gets or sets clock speeds.
        /// </summary>
        public GpuClockSpeeds ClockSpeeds { get; set; } = new();

        /// <summary>
        /// Gets or sets fan speed percentage.
        /// </summary>
        public float FanSpeedPercent { get; set; }
    }

    /// <summary>
    /// Represents historical performance data.
    /// </summary>
    public class PerformanceHistory
    {
        /// <summary>
        /// Gets or sets time range for the history.
        /// </summary>
        public TimeSpan TimeRange { get; set; }

        /// <summary>
        /// Gets or sets start time of the history.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets end time of the history.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Gets or sets individual metrics data points.
        /// </summary>
        public List<GpuPerformanceMetrics> Metrics { get; set; } = new();

        /// <summary>
        /// Gets or sets average values over the time period.
        /// </summary>
        public GpuPerformanceMetrics AverageMetrics { get; set; } = new();

        /// <summary>
        /// Gets or sets peak values over the time period.
        /// </summary>
        public GpuPerformanceMetrics PeakMetrics { get; set; } = new();

        /// <summary>
        /// Gets or sets minimum values over the time period.
        /// </summary>
        public GpuPerformanceMetrics MinimumMetrics { get; set; } = new();
    }

    /// <summary>
    /// Event arguments for GPU status changes.
    /// </summary>
    public class GpuStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the new GPU status.
        /// </summary>
        public GpuStatus NewStatus { get; set; } = new();

        /// <summary>
        /// Gets or sets the previous GPU status.
        /// </summary>
        public GpuStatus? PreviousStatus { get; set; }

        /// <summary>
        /// Gets or sets the type of change that occurred.
        /// </summary>
        public GpuStatusChangeType ChangeType { get; set; }
    }

    /// <summary>
    /// Types of GPU status changes.
    /// </summary>
    public enum GpuStatusChangeType
    {
        Availability,
        Temperature,
        Utilization,
        MemoryUsage,
        PowerConsumption
    }

    /// <summary>
    /// Event arguments for VRAM threshold exceeded events.
    /// </summary>
    public class VramThresholdExceededEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets current VRAM usage.
        /// </summary>
        public VramUsage CurrentUsage { get; set; } = new();

        /// <summary>
        /// Gets or sets threshold percentage that was exceeded.
        /// </summary>
        public float ThresholdPercent { get; set; }

        /// <summary>
        /// Gets or sets recommended action to take.
        /// </summary>
        public string RecommendedAction { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets severity of the threshold breach.
        /// </summary>
        public SeverityLevel Severity { get; set; } = SeverityLevel.Medium;
    }
}
