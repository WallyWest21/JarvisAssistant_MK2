namespace JarvisAssistant.Core.Models
{
    /// <summary>
    /// Information about a machine learning model.
    /// </summary>
    public class ModelInfo
    {
        /// <summary>
        /// Gets or sets the model name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the model size in bytes.
        /// </summary>
        public long SizeBytes { get; set; }

        /// <summary>
        /// Gets or sets the required VRAM in bytes.
        /// </summary>
        public long RequiredVramBytes { get; set; }

        /// <summary>
        /// Gets or sets the model type.
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the model version.
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets model capabilities.
        /// </summary>
        public List<string> Capabilities { get; set; } = new();

        /// <summary>
        /// Gets or sets model metadata.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Settings for model loading operations.
    /// </summary>
    public class ModelLoadSettings
    {
        /// <summary>
        /// Gets or sets whether to enable caching.
        /// </summary>
        public bool EnableCaching { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to use memory mapping.
        /// </summary>
        public bool UseMemoryMapping { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to preload layers.
        /// </summary>
        public bool PreloadLayers { get; set; } = true;

        /// <summary>
        /// Gets or sets optimization level.
        /// </summary>
        public OptimizationLevel OptimizationLevel { get; set; } = OptimizationLevel.Balanced;

        /// <summary>
        /// Gets or sets quantization settings.
        /// </summary>
        public QuantizationSettings? QuantizationSettings { get; set; }

        /// <summary>
        /// Gets or sets loading timeout.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(5);
    }

    /// <summary>
    /// Quantization settings for model optimization.
    /// </summary>
    public class QuantizationSettings
    {
        /// <summary>
        /// Gets or sets whether quantization is enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets quantization precision.
        /// </summary>
        public int Precision { get; set; } = 8;

        /// <summary>
        /// Gets or sets quantization type.
        /// </summary>
        public string Type { get; set; } = "dynamic";
    }

    /// <summary>
    /// System metrics for optimization context.
    /// </summary>
    public class SystemMetrics
    {
        /// <summary>
        /// Gets or sets CPU utilization percentage.
        /// </summary>
        public float CpuUtilization { get; set; }

        /// <summary>
        /// Gets or sets memory utilization percentage.
        /// </summary>
        public float MemoryUtilization { get; set; }

        /// <summary>
        /// Gets or sets VRAM utilization percentage.
        /// </summary>
        public float VramUtilization { get; set; }

        /// <summary>
        /// Gets or sets GPU utilization percentage.
        /// </summary>
        public float GpuUtilization { get; set; }

        /// <summary>
        /// Gets or sets network utilization percentage.
        /// </summary>
        public float NetworkUtilization { get; set; }

        /// <summary>
        /// Gets or sets disk utilization percentage.
        /// </summary>
        public float DiskUtilization { get; set; }

        /// <summary>
        /// Gets or sets GPU temperature in Celsius.
        /// </summary>
        public int GpuTemperature { get; set; }

        /// <summary>
        /// Gets or sets available system memory in bytes.
        /// </summary>
        public long AvailableMemoryBytes { get; set; }

        /// <summary>
        /// Gets or sets available VRAM in bytes.
        /// </summary>
        public long AvailableVramBytes { get; set; }
    }

    /// <summary>
    /// User preferences for optimization.
    /// </summary>
    public class UserPreferences
    {
        /// <summary>
        /// Gets or sets quality vs speed balance preference.
        /// </summary>
        public QualitySpeedPreference QualitySpeedBalance { get; set; } = QualitySpeedPreference.Balanced;

        /// <summary>
        /// Gets or sets whether to prioritize power efficiency.
        /// </summary>
        public bool PrioritizePowerEfficiency { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to enable aggressive caching.
        /// </summary>
        public bool EnableAggressiveCaching { get; set; } = true;

        /// <summary>
        /// Gets or sets maximum acceptable latency.
        /// </summary>
        public TimeSpan MaxAcceptableLatency { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets or sets preferred model switching behavior.
        /// </summary>
        public ModelSwitchingBehavior ModelSwitchingBehavior { get; set; } = ModelSwitchingBehavior.Automatic;

        /// <summary>
        /// Gets or sets whether to enable background optimization.
        /// </summary>
        public bool EnableBackgroundOptimization { get; set; } = true;
    }

    /// <summary>
    /// Quality vs speed preference for optimization.
    /// </summary>
    public enum QualitySpeedPreference
    {
        MaxQuality,
        HighQuality,
        Balanced,
        HighSpeed,
        MaxSpeed
    }

    /// <summary>
    /// Model switching behavior preferences.
    /// </summary>
    public enum ModelSwitchingBehavior
    {
        Manual,
        Automatic,
        Hybrid
    }

    /// <summary>
    /// Performance settings for the application.
    /// </summary>
    public class PerformanceSettings
    {
        /// <summary>
        /// Gets or sets the quality vs speed preference.
        /// </summary>
        public QualitySpeedPreference QualitySpeedBalance { get; set; } = QualitySpeedPreference.Balanced;

        /// <summary>
        /// Gets or sets maximum tokens per response.
        /// </summary>
        public int MaxTokensPerResponse { get; set; } = 1024;

        /// <summary>
        /// Gets or sets batch size for processing.
        /// </summary>
        public int BatchSize { get; set; } = 5;

        /// <summary>
        /// Gets or sets streaming chunk size.
        /// </summary>
        public int StreamingChunkSize { get; set; } = 50;

        /// <summary>
        /// Gets or sets cache size limit in bytes.
        /// </summary>
        public long CacheSizeLimitBytes { get; set; } = 100L * 1024 * 1024; // 100MB

        /// <summary>
        /// Gets or sets whether caching is enabled.
        /// </summary>
        public bool EnableCaching { get; set; } = true;

        /// <summary>
        /// Gets or sets whether compression is enabled.
        /// </summary>
        public bool UseCompression { get; set; } = true;

        /// <summary>
        /// Gets or sets whether background optimization is enabled.
        /// </summary>
        public bool EnableBackgroundOptimization { get; set; } = true;

        /// <summary>
        /// Gets or sets whether GPU monitoring is enabled.
        /// </summary>
        public bool EnableGpuMonitoring { get; set; } = true;

        /// <summary>
        /// Gets or sets maximum concurrent requests.
        /// </summary>
        public int MaxConcurrentRequests { get; set; } = 4;

        /// <summary>
        /// Gets or sets request timeout.
        /// </summary>
        public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets VRAM threshold percentage.
        /// </summary>
        public int VramThresholdPercent { get; set; } = 85;

        /// <summary>
        /// Gets or sets whether automatic model optimization is enabled.
        /// </summary>
        public bool EnableAutomaticModelOptimization { get; set; } = true;

        /// <summary>
        /// Gets or sets whether quantization is enabled.
        /// </summary>
        public bool UseQuantization { get; set; } = true;

        /// <summary>
        /// Gets or sets whether embedding caching is enabled.
        /// </summary>
        public bool EnableEmbeddingCaching { get; set; } = true;

        /// <summary>
        /// Gets or sets code completion target time.
        /// </summary>
        public TimeSpan CodeCompletionTargetTime { get; set; } = TimeSpan.FromMilliseconds(500);

        /// <summary>
        /// Gets or sets chat response target time.
        /// </summary>
        public TimeSpan ChatResponseTargetTime { get; set; } = TimeSpan.FromMilliseconds(2000);

        /// <summary>
        /// Gets or sets cache cleanup interval.
        /// </summary>
        public TimeSpan CacheCleanupInterval { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets batch timeout.
        /// </summary>
        public TimeSpan BatchTimeout { get; set; } = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Gets or sets temperature for model responses.
        /// </summary>
        public float Temperature { get; set; } = 0.7f;

        /// <summary>
        /// Gets or sets context window size.
        /// </summary>
        public int ContextWindowSize { get; set; } = 4096;
    }

    /// <summary>
    /// Types of requests for performance tracking.
    /// </summary>
    public enum RequestType
    {
        CodeCompletion,
        ChatMessage,
        DocumentAnalysis,
        EmbeddingGeneration,
        ModelLoading,
        VectorSearch
    }

    /// <summary>
    /// Performance statistics for the application.
    /// </summary>
    public class PerformanceStatistics
    {
        /// <summary>
        /// Gets or sets response time statistics by request type.
        /// </summary>
        public Dictionary<RequestType, ResponseTimeStats> ResponseTimes { get; set; } = new();

        /// <summary>
        /// Gets or sets current memory usage statistics.
        /// </summary>
        public MemoryUsageStats MemoryUsage { get; set; } = new();

        /// <summary>
        /// Gets or sets GPU performance statistics.
        /// </summary>
        public GpuPerformanceStats GpuPerformance { get; set; } = new();

        /// <summary>
        /// Gets or sets cache hit ratios.
        /// </summary>
        public Dictionary<string, float> CacheHitRatios { get; set; } = new();

        /// <summary>
        /// Gets or sets active model count.
        /// </summary>
        public int ActiveModelCount { get; set; }

        /// <summary>
        /// Gets or sets total requests processed.
        /// </summary>
        public long TotalRequestsProcessed { get; set; }

        /// <summary>
        /// Gets or sets timestamp of statistics generation.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Response time statistics for a specific request type.
    /// </summary>
    public class ResponseTimeStats
    {
        /// <summary>
        /// Gets or sets average response time.
        /// </summary>
        public TimeSpan Average { get; set; }

        /// <summary>
        /// Gets or sets median response time.
        /// </summary>
        public TimeSpan Median { get; set; }

        /// <summary>
        /// Gets or sets 95th percentile response time.
        /// </summary>
        public TimeSpan P95 { get; set; }

        /// <summary>
        /// Gets or sets 99th percentile response time.
        /// </summary>
        public TimeSpan P99 { get; set; }

        /// <summary>
        /// Gets or sets minimum response time.
        /// </summary>
        public TimeSpan Min { get; set; }

        /// <summary>
        /// Gets or sets maximum response time.
        /// </summary>
        public TimeSpan Max { get; set; }

        /// <summary>
        /// Gets or sets sample count.
        /// </summary>
        public int SampleCount { get; set; }
    }

    /// <summary>
    /// Throughput metrics for a time period.
    /// </summary>
    public class ThroughputMetrics
    {
        /// <summary>
        /// Gets or sets requests per second.
        /// </summary>
        public float RequestsPerSecond { get; set; }

        /// <summary>
        /// Gets or sets tokens per second.
        /// </summary>
        public float TokensPerSecond { get; set; }

        /// <summary>
        /// Gets or sets bytes processed per second.
        /// </summary>
        public long BytesPerSecond { get; set; }

        /// <summary>
        /// Gets or sets concurrent request count.
        /// </summary>
        public int ConcurrentRequests { get; set; }

        /// <summary>
        /// Gets or sets queue depth.
        /// </summary>
        public int QueueDepth { get; set; }

        /// <summary>
        /// Gets or sets time period for metrics.
        /// </summary>
        public TimeSpan Period { get; set; }
    }

    /// <summary>
    /// Memory usage snapshot.
    /// </summary>
    public class MemoryUsageSnapshot
    {
        /// <summary>
        /// Gets or sets total system memory usage in bytes.
        /// </summary>
        public long TotalSystemMemoryBytes { get; set; }

        /// <summary>
        /// Gets or sets application memory usage in bytes.
        /// </summary>
        public long ApplicationMemoryBytes { get; set; }

        /// <summary>
        /// Gets or sets GPU memory usage in bytes.
        /// </summary>
        public long GpuMemoryBytes { get; set; }

        /// <summary>
        /// Gets or sets memory usage by component.
        /// </summary>
        public Dictionary<string, long> ComponentMemoryUsage { get; set; } = new();

        /// <summary>
        /// Gets or sets garbage collection statistics.
        /// </summary>
        public GarbageCollectionStats GcStats { get; set; } = new();

        /// <summary>
        /// Gets or sets timestamp of snapshot.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Memory usage statistics.
    /// </summary>
    public class MemoryUsageStats
    {
        /// <summary>
        /// Gets or sets current memory usage in bytes.
        /// </summary>
        public long CurrentUsageBytes { get; set; }

        /// <summary>
        /// Gets or sets peak memory usage in bytes.
        /// </summary>
        public long PeakUsageBytes { get; set; }

        /// <summary>
        /// Gets or sets average memory usage in bytes.
        /// </summary>
        public long AverageUsageBytes { get; set; }

        /// <summary>
        /// Gets or sets memory usage by models.
        /// </summary>
        public Dictionary<string, long> ModelMemoryUsage { get; set; } = new();

        /// <summary>
        /// Gets or sets cache memory usage in bytes.
        /// </summary>
        public long CacheMemoryUsageBytes { get; set; }
    }

    /// <summary>
    /// GPU performance statistics.
    /// </summary>
    public class GpuPerformanceStats
    {
        /// <summary>
        /// Gets or sets average GPU utilization percentage.
        /// </summary>
        public float AverageUtilization { get; set; }

        /// <summary>
        /// Gets or sets peak GPU utilization percentage.
        /// </summary>
        public float PeakUtilization { get; set; }

        /// <summary>
        /// Gets or sets average inference time.
        /// </summary>
        public TimeSpan AverageInferenceTime { get; set; }

        /// <summary>
        /// Gets or sets VRAM utilization percentage.
        /// </summary>
        public float VramUtilization { get; set; }

        /// <summary>
        /// Gets or sets temperature statistics.
        /// </summary>
        public TemperatureStats Temperature { get; set; } = new();
    }

    /// <summary>
    /// Temperature statistics.
    /// </summary>
    public class TemperatureStats
    {
        /// <summary>
        /// Gets or sets current temperature in Celsius.
        /// </summary>
        public int Current { get; set; }

        /// <summary>
        /// Gets or sets average temperature in Celsius.
        /// </summary>
        public int Average { get; set; }

        /// <summary>
        /// Gets or sets maximum temperature in Celsius.
        /// </summary>
        public int Maximum { get; set; }
    }

    /// <summary>
    /// Garbage collection statistics.
    /// </summary>
    public class GarbageCollectionStats
    {
        /// <summary>
        /// Gets or sets generation 0 collection count.
        /// </summary>
        public int Gen0Collections { get; set; }

        /// <summary>
        /// Gets or sets generation 1 collection count.
        /// </summary>
        public int Gen1Collections { get; set; }

        /// <summary>
        /// Gets or sets generation 2 collection count.
        /// </summary>
        public int Gen2Collections { get; set; }

        /// <summary>
        /// Gets or sets total memory allocated in bytes.
        /// </summary>
        public long TotalMemoryAllocated { get; set; }

        /// <summary>
        /// Gets or sets last GC pause time.
        /// </summary>
        public TimeSpan LastPauseTime { get; set; }
    }

    /// <summary>
    /// Comprehensive performance report.
    /// </summary>
    public class PerformanceReport
    {
        /// <summary>
        /// Gets or sets report time range.
        /// </summary>
        public TimeSpan TimeRange { get; set; }

        /// <summary>
        /// Gets or sets start time of the report.
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Gets or sets end time of the report.
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Gets or sets overall performance summary.
        /// </summary>
        public PerformanceSummary Summary { get; set; } = new();

        /// <summary>
        /// Gets or sets detailed metrics by request type.
        /// </summary>
        public Dictionary<RequestType, DetailedMetrics> DetailedMetrics { get; set; } = new();

        /// <summary>
        /// Gets or sets model performance analysis.
        /// </summary>
        public ModelPerformanceAnalysis ModelAnalysis { get; set; } = new();

        /// <summary>
        /// Gets or sets identified performance issues.
        /// </summary>
        public List<PerformanceIssue> Issues { get; set; } = new();

        /// <summary>
        /// Gets or sets performance recommendations.
        /// </summary>
        public List<PerformanceRecommendation> Recommendations { get; set; } = new();
    }

    /// <summary>
    /// Performance summary.
    /// </summary>
    public class PerformanceSummary
    {
        /// <summary>
        /// Gets or sets overall health score (0-100).
        /// </summary>
        public int HealthScore { get; set; }

        /// <summary>
        /// Gets or sets total requests processed.
        /// </summary>
        public long TotalRequests { get; set; }

        /// <summary>
        /// Gets or sets average response time across all request types.
        /// </summary>
        public TimeSpan AverageResponseTime { get; set; }

        /// <summary>
        /// Gets or sets error rate percentage.
        /// </summary>
        public float ErrorRate { get; set; }

        /// <summary>
        /// Gets or sets resource utilization summary.
        /// </summary>
        public ResourceUtilizationSummary ResourceUtilization { get; set; } = new();
    }

    /// <summary>
    /// Resource utilization summary.
    /// </summary>
    public class ResourceUtilizationSummary
    {
        /// <summary>
        /// Gets or sets average CPU utilization percentage.
        /// </summary>
        public float AverageCpuUtilization { get; set; }

        /// <summary>
        /// Gets or sets average memory utilization percentage.
        /// </summary>
        public float AverageMemoryUtilization { get; set; }

        /// <summary>
        /// Gets or sets average GPU utilization percentage.
        /// </summary>
        public float AverageGpuUtilization { get; set; }

        /// <summary>
        /// Gets or sets average VRAM utilization percentage.
        /// </summary>
        public float AverageVramUtilization { get; set; }
    }

    /// <summary>
    /// Detailed metrics for a specific request type.
    /// </summary>
    public class DetailedMetrics
    {
        /// <summary>
        /// Gets or sets response time distribution.
        /// </summary>
        public ResponseTimeStats ResponseTimes { get; set; } = new();

        /// <summary>
        /// Gets or sets throughput metrics.
        /// </summary>
        public ThroughputMetrics Throughput { get; set; } = new();

        /// <summary>
        /// Gets or sets error count and types.
        /// </summary>
        public Dictionary<string, int> ErrorCounts { get; set; } = new();

        /// <summary>
        /// Gets or sets resource consumption metrics.
        /// </summary>
        public ResourceConsumption ResourceConsumption { get; set; } = new();
    }

    /// <summary>
    /// Resource consumption metrics.
    /// </summary>
    public class ResourceConsumption
    {
        /// <summary>
        /// Gets or sets average CPU usage during requests.
        /// </summary>
        public float AverageCpuUsage { get; set; }

        /// <summary>
        /// Gets or sets average memory allocation per request.
        /// </summary>
        public long AverageMemoryAllocation { get; set; }

        /// <summary>
        /// Gets or sets average GPU usage during requests.
        /// </summary>
        public float AverageGpuUsage { get; set; }

        /// <summary>
        /// Gets or sets network bandwidth consumption.
        /// </summary>
        public long NetworkBandwidthBytes { get; set; }
    }

    /// <summary>
    /// Model performance analysis.
    /// </summary>
    public class ModelPerformanceAnalysis
    {
        /// <summary>
        /// Gets or sets performance by model.
        /// </summary>
        public Dictionary<string, ModelMetrics> ModelMetrics { get; set; } = new();

        /// <summary>
        /// Gets or sets model switching statistics.
        /// </summary>
        public ModelSwitchingStats SwitchingStats { get; set; } = new();

        /// <summary>
        /// Gets or sets model efficiency rankings.
        /// </summary>
        public List<ModelEfficiencyRanking> EfficiencyRankings { get; set; } = new();
    }

    /// <summary>
    /// Metrics for a specific model.
    /// </summary>
    public class ModelMetrics
    {
        /// <summary>
        /// Gets or sets usage count.
        /// </summary>
        public int UsageCount { get; set; }

        /// <summary>
        /// Gets or sets total processing time.
        /// </summary>
        public TimeSpan TotalProcessingTime { get; set; }

        /// <summary>
        /// Gets or sets average inference time.
        /// </summary>
        public TimeSpan AverageInferenceTime { get; set; }

        /// <summary>
        /// Gets or sets tokens per second.
        /// </summary>
        public float TokensPerSecond { get; set; }

        /// <summary>
        /// Gets or sets memory efficiency.
        /// </summary>
        public float MemoryEfficiency { get; set; }

        /// <summary>
        /// Gets or sets quality metrics if available.
        /// </summary>
        public QualityMetrics? QualityMetrics { get; set; }
    }

    /// <summary>
    /// Quality metrics for model output.
    /// </summary>
    public class QualityMetrics
    {
        /// <summary>
        /// Gets or sets accuracy score.
        /// </summary>
        public float AccuracyScore { get; set; }

        /// <summary>
        /// Gets or sets relevance score.
        /// </summary>
        public float RelevanceScore { get; set; }

        /// <summary>
        /// Gets or sets user satisfaction rating.
        /// </summary>
        public float UserSatisfaction { get; set; }
    }

    /// <summary>
    /// Model switching statistics.
    /// </summary>
    public class ModelSwitchingStats
    {
        /// <summary>
        /// Gets or sets total switches performed.
        /// </summary>
        public int TotalSwitches { get; set; }

        /// <summary>
        /// Gets or sets average switch time.
        /// </summary>
        public TimeSpan AverageSwitchTime { get; set; }

        /// <summary>
        /// Gets or sets switch frequency.
        /// </summary>
        public float SwitchesPerHour { get; set; }

        /// <summary>
        /// Gets or sets common switch patterns.
        /// </summary>
        public Dictionary<string, int> SwitchPatterns { get; set; } = new();
    }

    /// <summary>
    /// Model efficiency ranking.
    /// </summary>
    public class ModelEfficiencyRanking
    {
        /// <summary>
        /// Gets or sets model name.
        /// </summary>
        public string ModelName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets efficiency score.
        /// </summary>
        public float EfficiencyScore { get; set; }

        /// <summary>
        /// Gets or sets ranking position.
        /// </summary>
        public int Rank { get; set; }

        /// <summary>
        /// Gets or sets efficiency factors.
        /// </summary>
        public EfficiencyFactors Factors { get; set; } = new();
    }

    /// <summary>
    /// Efficiency factors for model ranking.
    /// </summary>
    public class EfficiencyFactors
    {
        /// <summary>
        /// Gets or sets speed score.
        /// </summary>
        public float SpeedScore { get; set; }

        /// <summary>
        /// Gets or sets memory efficiency score.
        /// </summary>
        public float MemoryScore { get; set; }

        /// <summary>
        /// Gets or sets quality score.
        /// </summary>
        public float QualityScore { get; set; }

        /// <summary>
        /// Gets or sets power efficiency score.
        /// </summary>
        public float PowerScore { get; set; }
    }

    /// <summary>
    /// Performance bottleneck identification.
    /// </summary>
    public class PerformanceBottleneck
    {
        /// <summary>
        /// Gets or sets bottleneck type.
        /// </summary>
        public BottleneckType Type { get; set; }

        /// <summary>
        /// Gets or sets severity level.
        /// </summary>
        public SeverityLevel Severity { get; set; }

        /// <summary>
        /// Gets or sets description of the bottleneck.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets affected components.
        /// </summary>
        public List<string> AffectedComponents { get; set; } = new();

        /// <summary>
        /// Gets or sets potential solutions.
        /// </summary>
        public List<string> PotentialSolutions { get; set; } = new();

        /// <summary>
        /// Gets or sets estimated impact on performance.
        /// </summary>
        public PerformanceImpact EstimatedImpact { get; set; } = new();
    }

    /// <summary>
    /// Performance issue identification.
    /// </summary>
    public class PerformanceIssue
    {
        /// <summary>
        /// Gets or sets issue type.
        /// </summary>
        public IssueType Type { get; set; }

        /// <summary>
        /// Gets or sets severity level.
        /// </summary>
        public SeverityLevel Severity { get; set; }

        /// <summary>
        /// Gets or sets issue description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets when the issue was detected.
        /// </summary>
        public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets related metrics.
        /// </summary>
        public Dictionary<string, object> RelatedMetrics { get; set; } = new();
    }

    /// <summary>
    /// Performance recommendation.
    /// </summary>
    public class PerformanceRecommendation
    {
        /// <summary>
        /// Gets or sets recommendation title.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets detailed description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets implementation steps.
        /// </summary>
        public List<string> ImplementationSteps { get; set; } = new();

        /// <summary>
        /// Gets or sets expected impact.
        /// </summary>
        public ImpactLevel ExpectedImpact { get; set; }

        /// <summary>
        /// Gets or sets implementation effort.
        /// </summary>
        public EffortLevel ImplementationEffort { get; set; }

        /// <summary>
        /// Gets or sets priority level.
        /// </summary>
        public PriorityLevel Priority { get; set; }
    }

    /// <summary>
    /// Performance impact measurement.
    /// </summary>
    public class PerformanceImpact
    {
        /// <summary>
        /// Gets or sets latency impact percentage.
        /// </summary>
        public float LatencyImpactPercent { get; set; }

        /// <summary>
        /// Gets or sets throughput impact percentage.
        /// </summary>
        public float ThroughputImpactPercent { get; set; }

        /// <summary>
        /// Gets or sets resource usage impact percentage.
        /// </summary>
        public float ResourceImpactPercent { get; set; }
    }

    /// <summary>
    /// Event arguments for performance alerts.
    /// </summary>
    public class PerformanceAlertEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets alert type.
        /// </summary>
        public AlertType AlertType { get; set; }

        /// <summary>
        /// Gets or sets alert message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets current metrics that triggered the alert.
        /// </summary>
        public Dictionary<string, object> CurrentMetrics { get; set; } = new();

        /// <summary>
        /// Gets or sets threshold values that were exceeded.
        /// </summary>
        public Dictionary<string, object> Thresholds { get; set; } = new();
    }

    /// <summary>
    /// Bottleneck types.
    /// </summary>
    public enum BottleneckType
    {
        Memory,
        Gpu,
        Cpu,
        Network,
        Storage,
        ModelLoading,
        CacheEfficiency
    }

    /// <summary>
    /// Issue types.
    /// </summary>
    public enum IssueType
    {
        HighLatency,
        LowThroughput,
        MemoryLeak,
        GpuUnderutilization,
        ExcessiveModelSwitching,
        CacheMisses,
        NetworkBottleneck
    }

    /// <summary>
    /// Severity levels.
    /// </summary>
    public enum SeverityLevel
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Effort levels for implementation.
    /// </summary>
    public enum EffortLevel
    {
        Low,
        Medium,
        High,
        VeryHigh
    }

    /// <summary>
    /// Priority levels.
    /// </summary>
    public enum PriorityLevel
    {
        Low,
        Medium,
        High,
        Urgent
    }

    /// <summary>
    /// Alert types.
    /// </summary>
    public enum AlertType
    {
        LatencyThresholdExceeded,
        MemoryThresholdExceeded,
        GpuTemperatureHigh,
        VramThresholdExceeded,
        ThroughputBelowThreshold,
        ErrorRateExceeded
    }

    /// <summary>
    /// Hardware capabilities information.
    /// </summary>
    public class HardwareCapabilities
    {
        /// <summary>
        /// Gets or sets whether GPU is available.
        /// </summary>
        public bool HasGpu { get; set; }

        /// <summary>
        /// Gets or sets total VRAM in bytes.
        /// </summary>
        public long TotalVramBytes { get; set; }

        /// <summary>
        /// Gets or sets available VRAM in bytes.
        /// </summary>
        public long AvailableVramBytes { get; set; }

        /// <summary>
        /// Gets or sets CPU core count.
        /// </summary>
        public int CpuCoreCount { get; set; }

        /// <summary>
        /// Gets or sets total system memory in bytes.
        /// </summary>
        public long TotalMemoryBytes { get; set; }

        /// <summary>
        /// Gets or sets available memory in bytes.
        /// </summary>
        public long AvailableMemoryBytes { get; set; }

        /// <summary>
        /// Gets or sets whether CUDA is available.
        /// </summary>
        public bool SupportsCuda { get; set; }

        /// <summary>
        /// Gets or sets compute capability.
        /// </summary>
        public string ComputeCapability { get; set; } = string.Empty;
    }

    /// <summary>
    /// Optimization hints for request processing.
    /// </summary>
    public class OptimizationHints
    {
        /// <summary>
        /// Gets or sets whether to prioritize speed over quality.
        /// </summary>
        public bool PrioritizeSpeed { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to use caching aggressively.
        /// </summary>
        public bool UseAggressiveCaching { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to enable batching.
        /// </summary>
        public bool EnableBatching { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to use model switching.
        /// </summary>
        public bool AllowModelSwitching { get; set; } = true;

        /// <summary>
        /// Gets or sets maximum acceptable latency.
        /// </summary>
        public TimeSpan MaxAcceptableLatency { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets or sets optimization metadata.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}
