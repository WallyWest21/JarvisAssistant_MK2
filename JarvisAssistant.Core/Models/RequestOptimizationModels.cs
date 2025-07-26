namespace JarvisAssistant.Core.Models
{
    /// <summary>
    /// Optimized request with performance settings applied.
    /// </summary>
    public class OptimizedRequest
    {
        /// <summary>
        /// Gets or sets the original chat request.
        /// </summary>
        public ChatRequest OriginalRequest { get; set; } = new("", "");

        /// <summary>
        /// Gets or sets optimization settings applied.
        /// </summary>
        public RequestOptimizationSettings OptimizationSettings { get; set; } = new();

        /// <summary>
        /// Gets or sets priority level for processing.
        /// </summary>
        public RequestPriority Priority { get; set; } = RequestPriority.Normal;

        /// <summary>
        /// Gets or sets estimated processing time.
        /// </summary>
        public TimeSpan EstimatedProcessingTime { get; set; }

        /// <summary>
        /// Gets or sets cache key if applicable.
        /// </summary>
        public string? CacheKey { get; set; }

        /// <summary>
        /// Gets or sets whether request can be batched.
        /// </summary>
        public bool CanBatch { get; set; }

        /// <summary>
        /// Gets or sets batch group identifier.
        /// </summary>
        public string? BatchGroup { get; set; }

        /// <summary>
        /// Gets or sets optimization metadata.
        /// </summary>
        public Dictionary<string, object> OptimizationMetadata { get; set; } = new();
    }

    /// <summary>
    /// Optimization context for request processing.
    /// </summary>
    public class OptimizationContext
    {
        /// <summary>
        /// Gets or sets current system performance metrics.
        /// </summary>
        public SystemPerformanceMetrics SystemMetrics { get; set; } = new();

        /// <summary>
        /// Gets or sets user performance preferences.
        /// </summary>
        public UserPerformancePreferences UserPreferences { get; set; } = new();

        /// <summary>
        /// Gets or sets current resource availability.
        /// </summary>
        public ResourceAvailability ResourceAvailability { get; set; } = new();

        /// <summary>
        /// Gets or sets historical performance data.
        /// </summary>
        public HistoricalPerformanceData HistoricalData { get; set; } = new();

        /// <summary>
        /// Gets or sets current queue status.
        /// </summary>
        public QueueStatus QueueStatus { get; set; } = new();
    }

    /// <summary>
    /// Request optimization settings.
    /// </summary>
    public class RequestOptimizationSettings
    {
        /// <summary>
        /// Gets or sets maximum tokens for response.
        /// </summary>
        public int MaxTokens { get; set; } = 1024;

        /// <summary>
        /// Gets or sets streaming chunk size.
        /// </summary>
        public int StreamingChunkSize { get; set; } = 50;

        /// <summary>
        /// Gets or sets timeout for request.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets whether to use compression.
        /// </summary>
        public bool UseCompression { get; set; }

        /// <summary>
        /// Gets or sets whether to enable caching.
        /// </summary>
        public bool EnableCaching { get; set; } = true;

        /// <summary>
        /// Gets or sets cache TTL.
        /// </summary>
        public TimeSpan CacheTtl { get; set; } = TimeSpan.FromMinutes(15);

        /// <summary>
        /// Gets or sets model-specific settings.
        /// </summary>
        public ModelSpecificSettings ModelSettings { get; set; } = new();
    }

    /// <summary>
    /// Model-specific optimization settings.
    /// </summary>
    public class ModelSpecificSettings
    {
        /// <summary>
        /// Gets or sets temperature setting.
        /// </summary>
        public float Temperature { get; set; } = 0.7f;

        /// <summary>
        /// Gets or sets top-p setting.
        /// </summary>
        public float TopP { get; set; } = 0.9f;

        /// <summary>
        /// Gets or sets whether to use 4-bit quantization.
        /// </summary>
        public bool Use4BitQuantization { get; set; }

        /// <summary>
        /// Gets or sets context window size.
        /// </summary>
        public int ContextWindowSize { get; set; } = 4096;

        /// <summary>
        /// Gets or sets batch size for processing.
        /// </summary>
        public int BatchSize { get; set; } = 1;
    }

    /// <summary>
    /// System performance metrics for optimization context.
    /// </summary>
    public class SystemPerformanceMetrics
    {
        /// <summary>
        /// Gets or sets current CPU utilization percentage.
        /// </summary>
        public float CpuUtilization { get; set; }

        /// <summary>
        /// Gets or sets current memory utilization percentage.
        /// </summary>
        public float MemoryUtilization { get; set; }

        /// <summary>
        /// Gets or sets current GPU utilization percentage.
        /// </summary>
        public float GpuUtilization { get; set; }

        /// <summary>
        /// Gets or sets current VRAM utilization percentage.
        /// </summary>
        public float VramUtilization { get; set; }

        /// <summary>
        /// Gets or sets network latency.
        /// </summary>
        public TimeSpan NetworkLatency { get; set; }

        /// <summary>
        /// Gets or sets storage I/O performance.
        /// </summary>
        public StoragePerformanceMetrics StoragePerformance { get; set; } = new();
    }

    /// <summary>
    /// Storage performance metrics.
    /// </summary>
    public class StoragePerformanceMetrics
    {
        /// <summary>
        /// Gets or sets read speed in MB/s.
        /// </summary>
        public float ReadSpeedMbps { get; set; }

        /// <summary>
        /// Gets or sets write speed in MB/s.
        /// </summary>
        public float WriteSpeedMbps { get; set; }

        /// <summary>
        /// Gets or sets I/O operations per second.
        /// </summary>
        public int Iops { get; set; }

        /// <summary>
        /// Gets or sets average latency for I/O operations.
        /// </summary>
        public TimeSpan AverageLatency { get; set; }
    }

    /// <summary>
    /// User performance preferences.
    /// </summary>
    public class UserPerformancePreferences
    {
        /// <summary>
        /// Gets or sets preference for quality vs speed.
        /// </summary>
        public QualitySpeedPreference QualitySpeedBalance { get; set; } = QualitySpeedPreference.Balanced;

        /// <summary>
        /// Gets or sets maximum acceptable response time.
        /// </summary>
        public TimeSpan MaxAcceptableResponseTime { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets or sets whether to prioritize energy efficiency.
        /// </summary>
        public bool PrioritizeEnergyEfficiency { get; set; }

        /// <summary>
        /// Gets or sets whether to enable aggressive optimization.
        /// </summary>
        public bool EnableAggressiveOptimization { get; set; }

        /// <summary>
        /// Gets or sets custom optimization parameters.
        /// </summary>
        public Dictionary<string, object> CustomParameters { get; set; } = new();
    }

    /// <summary>
    /// Resource availability information.
    /// </summary>
    public class ResourceAvailability
    {
        /// <summary>
        /// Gets or sets available system memory in bytes.
        /// </summary>
        public long AvailableMemoryBytes { get; set; }

        /// <summary>
        /// Gets or sets available VRAM in bytes.
        /// </summary>
        public long AvailableVramBytes { get; set; }

        /// <summary>
        /// Gets or sets number of available CPU cores.
        /// </summary>
        public int AvailableCpuCores { get; set; }

        /// <summary>
        /// Gets or sets whether GPU is available.
        /// </summary>
        public bool GpuAvailable { get; set; }

        /// <summary>
        /// Gets or sets network bandwidth availability.
        /// </summary>
        public NetworkBandwidth NetworkBandwidth { get; set; } = new();

        /// <summary>
        /// Gets or sets storage capacity information.
        /// </summary>
        public StorageCapacity StorageCapacity { get; set; } = new();
    }

    /// <summary>
    /// Network bandwidth information.
    /// </summary>
    public class NetworkBandwidth
    {
        /// <summary>
        /// Gets or sets available download bandwidth in Mbps.
        /// </summary>
        public float DownloadMbps { get; set; }

        /// <summary>
        /// Gets or sets available upload bandwidth in Mbps.
        /// </summary>
        public float UploadMbps { get; set; }

        /// <summary>
        /// Gets or sets current latency to services.
        /// </summary>
        public TimeSpan Latency { get; set; }

        /// <summary>
        /// Gets or sets connection quality score.
        /// </summary>
        public float QualityScore { get; set; }
    }

    /// <summary>
    /// Storage capacity information.
    /// </summary>
    public class StorageCapacity
    {
        /// <summary>
        /// Gets or sets total storage space in bytes.
        /// </summary>
        public long TotalBytes { get; set; }

        /// <summary>
        /// Gets or sets available storage space in bytes.
        /// </summary>
        public long AvailableBytes { get; set; }

        /// <summary>
        /// Gets or sets whether storage is SSD.
        /// </summary>
        public bool IsSsd { get; set; }

        /// <summary>
        /// Gets or sets storage performance tier.
        /// </summary>
        public StoragePerformanceTier PerformanceTier { get; set; }
    }

    /// <summary>
    /// Historical performance data for optimization.
    /// </summary>
    public class HistoricalPerformanceData
    {
        /// <summary>
        /// Gets or sets average response times by request type.
        /// </summary>
        public Dictionary<RequestType, TimeSpan> AverageResponseTimes { get; set; } = new();

        /// <summary>
        /// Gets or sets peak usage patterns.
        /// </summary>
        public List<UsagePattern> PeakUsagePatterns { get; set; } = new();

        /// <summary>
        /// Gets or sets successful optimization strategies.
        /// </summary>
        public List<OptimizationStrategy> SuccessfulStrategies { get; set; } = new();

        /// <summary>
        /// Gets or sets performance trends.
        /// </summary>
        public PerformanceTrends Trends { get; set; } = new();
    }

    /// <summary>
    /// Usage pattern information.
    /// </summary>
    public class UsagePattern
    {
        /// <summary>
        /// Gets or sets time of day for peak usage.
        /// </summary>
        public TimeSpan TimeOfDay { get; set; }

        /// <summary>
        /// Gets or sets day of week pattern.
        /// </summary>
        public DayOfWeek DayOfWeek { get; set; }

        /// <summary>
        /// Gets or sets typical request types during peak.
        /// </summary>
        public List<RequestType> TypicalRequestTypes { get; set; } = new();

        /// <summary>
        /// Gets or sets resource usage during peak.
        /// </summary>
        public ResourceUsagePattern ResourceUsage { get; set; } = new();
    }

    /// <summary>
    /// Resource usage pattern.
    /// </summary>
    public class ResourceUsagePattern
    {
        /// <summary>
        /// Gets or sets CPU usage percentage.
        /// </summary>
        public float CpuUsagePercent { get; set; }

        /// <summary>
        /// Gets or sets memory usage percentage.
        /// </summary>
        public float MemoryUsagePercent { get; set; }

        /// <summary>
        /// Gets or sets GPU usage percentage.
        /// </summary>
        public float GpuUsagePercent { get; set; }

        /// <summary>
        /// Gets or sets network usage in Mbps.
        /// </summary>
        public float NetworkUsageMbps { get; set; }
    }

    /// <summary>
    /// Optimization strategy information.
    /// </summary>
    public class OptimizationStrategy
    {
        /// <summary>
        /// Gets or sets strategy name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets strategy parameters.
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();

        /// <summary>
        /// Gets or sets success rate percentage.
        /// </summary>
        public float SuccessRate { get; set; }

        /// <summary>
        /// Gets or sets average performance improvement.
        /// </summary>
        public PerformanceImprovement AverageImprovement { get; set; } = new();

        /// <summary>
        /// Gets or sets applicable conditions.
        /// </summary>
        public List<string> ApplicableConditions { get; set; } = new();
    }

    /// <summary>
    /// Performance trends information.
    /// </summary>
    public class PerformanceTrends
    {
        /// <summary>
        /// Gets or sets latency trend direction.
        /// </summary>
        public TrendDirection LatencyTrend { get; set; }

        /// <summary>
        /// Gets or sets throughput trend direction.
        /// </summary>
        public TrendDirection ThroughputTrend { get; set; }

        /// <summary>
        /// Gets or sets resource usage trend direction.
        /// </summary>
        public TrendDirection ResourceUsageTrend { get; set; }

        /// <summary>
        /// Gets or sets error rate trend direction.
        /// </summary>
        public TrendDirection ErrorRateTrend { get; set; }

        /// <summary>
        /// Gets or sets trend analysis period.
        /// </summary>
        public TimeSpan AnalysisPeriod { get; set; }
    }

    /// <summary>
    /// Queue status information.
    /// </summary>
    public class QueueStatus
    {
        /// <summary>
        /// Gets or sets current queue depth.
        /// </summary>
        public int QueueDepth { get; set; }

        /// <summary>
        /// Gets or sets average wait time.
        /// </summary>
        public TimeSpan AverageWaitTime { get; set; }

        /// <summary>
        /// Gets or sets processing capacity utilization.
        /// </summary>
        public float CapacityUtilization { get; set; }

        /// <summary>
        /// Gets or sets queue by priority.
        /// </summary>
        public Dictionary<RequestPriority, int> QueueByPriority { get; set; } = new();

        /// <summary>
        /// Gets or sets estimated time to process current queue.
        /// </summary>
        public TimeSpan EstimatedProcessingTime { get; set; }
    }

    /// <summary>
    /// Batch processing result.
    /// </summary>
    public class BatchProcessingResult
    {
        /// <summary>
        /// Gets or sets total requests processed.
        /// </summary>
        public int TotalRequests { get; set; }

        /// <summary>
        /// Gets or sets successful requests.
        /// </summary>
        public int SuccessfulRequests { get; set; }

        /// <summary>
        /// Gets or sets failed requests.
        /// </summary>
        public int FailedRequests { get; set; }

        /// <summary>
        /// Gets or sets total processing time.
        /// </summary>
        public TimeSpan TotalProcessingTime { get; set; }

        /// <summary>
        /// Gets or sets average processing time per request.
        /// </summary>
        public TimeSpan AverageProcessingTime { get; set; }

        /// <summary>
        /// Gets or sets efficiency gain from batching.
        /// </summary>
        public float EfficiencyGain { get; set; }

        /// <summary>
        /// Gets or sets individual request results.
        /// </summary>
        public List<IndividualRequestResult> RequestResults { get; set; } = new();

        /// <summary>
        /// Gets or sets resource usage during batch processing.
        /// </summary>
        public BatchResourceUsage ResourceUsage { get; set; } = new();
    }

    /// <summary>
    /// Individual request result within a batch.
    /// </summary>
    public class IndividualRequestResult
    {
        /// <summary>
        /// Gets or sets request identifier.
        /// </summary>
        public string RequestId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether processing was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets processing time for this request.
        /// </summary>
        public TimeSpan ProcessingTime { get; set; }

        /// <summary>
        /// Gets or sets response size in bytes.
        /// </summary>
        public long ResponseSizeBytes { get; set; }

        /// <summary>
        /// Gets or sets error message if failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the response if successful.
        /// </summary>
        public ChatResponse? Response { get; set; }
    }

    /// <summary>
    /// Resource usage during batch processing.
    /// </summary>
    public class BatchResourceUsage
    {
        /// <summary>
        /// Gets or sets peak memory usage in bytes.
        /// </summary>
        public long PeakMemoryUsageBytes { get; set; }

        /// <summary>
        /// Gets or sets average CPU utilization percentage.
        /// </summary>
        public float AverageCpuUtilization { get; set; }

        /// <summary>
        /// Gets or sets average GPU utilization percentage.
        /// </summary>
        public float AverageGpuUtilization { get; set; }

        /// <summary>
        /// Gets or sets network bandwidth consumed in bytes.
        /// </summary>
        public long NetworkBandwidthBytes { get; set; }

        /// <summary>
        /// Gets or sets energy consumption estimate in watt-hours.
        /// </summary>
        public float EnergyConsumptionWh { get; set; }
    }

    /// <summary>
    /// Response cache settings.
    /// </summary>
    public class ResponseCacheSettings
    {
        /// <summary>
        /// Gets or sets cache TTL.
        /// </summary>
        public TimeSpan TimeToLive { get; set; } = TimeSpan.FromMinutes(15);

        /// <summary>
        /// Gets or sets cache key generation strategy.
        /// </summary>
        public CacheKeyStrategy KeyStrategy { get; set; } = CacheKeyStrategy.ContentHash;

        /// <summary>
        /// Gets or sets maximum cache size in bytes.
        /// </summary>
        public long MaxCacheSizeBytes { get; set; } = 100 * 1024 * 1024; // 100MB

        /// <summary>
        /// Gets or sets compression level for cached responses.
        /// </summary>
        public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Balanced;

        /// <summary>
        /// Gets or sets whether to cache partial responses.
        /// </summary>
        public bool CachePartialResponses { get; set; }

        /// <summary>
        /// Gets or sets cache eviction policy.
        /// </summary>
        public CacheEvictionPolicy EvictionPolicy { get; set; } = CacheEvictionPolicy.LeastRecentlyUsed;
    }

    /// <summary>
    /// Compressed data with metadata.
    /// </summary>
    public class CompressedData
    {
        /// <summary>
        /// Gets or sets compressed data bytes.
        /// </summary>
        public byte[] Data { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Gets or sets original size in bytes.
        /// </summary>
        public long OriginalSizeBytes { get; set; }

        /// <summary>
        /// Gets or sets compressed size in bytes.
        /// </summary>
        public long CompressedSizeBytes { get; set; }

        /// <summary>
        /// Gets or sets compression ratio.
        /// </summary>
        public float CompressionRatio => OriginalSizeBytes > 0 ? (float)CompressedSizeBytes / OriginalSizeBytes : 1.0f;

        /// <summary>
        /// Gets or sets compression algorithm used.
        /// </summary>
        public CompressionAlgorithm Algorithm { get; set; }

        /// <summary>
        /// Gets or sets compression metadata.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Embedding batch result.
    /// </summary>
    public class EmbeddingBatchResult
    {
        /// <summary>
        /// Gets or sets generated embeddings.
        /// </summary>
        public List<float[]> Embeddings { get; set; } = new();

        /// <summary>
        /// Gets or sets total processing time.
        /// </summary>
        public TimeSpan TotalProcessingTime { get; set; }

        /// <summary>
        /// Gets or sets embeddings per second.
        /// </summary>
        public float EmbeddingsPerSecond { get; set; }

        /// <summary>
        /// Gets or sets batch efficiency compared to individual processing.
        /// </summary>
        public float BatchEfficiency { get; set; }

        /// <summary>
        /// Gets or sets memory usage during processing.
        /// </summary>
        public long MemoryUsageBytes { get; set; }

        /// <summary>
        /// Gets or sets any errors that occurred.
        /// </summary>
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// Parallel processing result.
    /// </summary>
    public class ParallelProcessingResult
    {
        /// <summary>
        /// Gets or sets total requests processed.
        /// </summary>
        public int TotalRequests { get; set; }

        /// <summary>
        /// Gets or sets successful requests.
        /// </summary>
        public int SuccessfulRequests { get; set; }

        /// <summary>
        /// Gets or sets failed requests.
        /// </summary>
        public int FailedRequests { get; set; }

        /// <summary>
        /// Gets or sets total wall clock time.
        /// </summary>
        public TimeSpan TotalWallClockTime { get; set; }

        /// <summary>
        /// Gets or sets parallelization efficiency.
        /// </summary>
        public float ParallelizationEfficiency { get; set; }

        /// <summary>
        /// Gets or sets maximum concurrent operations achieved.
        /// </summary>
        public int MaxConcurrentOperations { get; set; }

        /// <summary>
        /// Gets or sets individual results.
        /// </summary>
        public List<IndividualRequestResult> Results { get; set; } = new();

        /// <summary>
        /// Gets or sets resource contention metrics.
        /// </summary>
        public ResourceContentionMetrics ResourceContention { get; set; } = new();
    }

    /// <summary>
    /// Resource contention metrics.
    /// </summary>
    public class ResourceContentionMetrics
    {
        /// <summary>
        /// Gets or sets CPU contention score.
        /// </summary>
        public float CpuContention { get; set; }

        /// <summary>
        /// Gets or sets memory contention score.
        /// </summary>
        public float MemoryContention { get; set; }

        /// <summary>
        /// Gets or sets GPU contention score.
        /// </summary>
        public float GpuContention { get; set; }

        /// <summary>
        /// Gets or sets network contention score.
        /// </summary>
        public float NetworkContention { get; set; }

        /// <summary>
        /// Gets or sets I/O contention score.
        /// </summary>
        public float IoContention { get; set; }
    }

    /// <summary>
    /// Request priority levels.
    /// </summary>
    public enum RequestPriority
    {
        Low,
        Normal,
        High,
        Critical
    }

    /// <summary>
    /// Storage performance tiers.
    /// </summary>
    public enum StoragePerformanceTier
    {
        Slow,
        Standard,
        Fast,
        Ultra
    }

    /// <summary>
    /// Trend directions.
    /// </summary>
    public enum TrendDirection
    {
        Improving,
        Stable,
        Degrading,
        Volatile
    }

    /// <summary>
    /// Cache key generation strategies.
    /// </summary>
    public enum CacheKeyStrategy
    {
        ContentHash,
        SemanticHash,
        ParameterBased,
        Custom
    }

    /// <summary>
    /// Compression levels.
    /// </summary>
    public enum CompressionLevel
    {
        None,
        Fast,
        Balanced,
        Maximum
    }

    /// <summary>
    /// Cache eviction policies.
    /// </summary>
    public enum CacheEvictionPolicy
    {
        LeastRecentlyUsed,
        LeastFrequentlyUsed,
        FirstInFirstOut,
        TimeToLive,
        Adaptive
    }

    /// <summary>
    /// Compression algorithms.
    /// </summary>
    public enum CompressionAlgorithm
    {
        None,
        Gzip,
        Deflate,
        Brotli,
        Lz4
    }
}
