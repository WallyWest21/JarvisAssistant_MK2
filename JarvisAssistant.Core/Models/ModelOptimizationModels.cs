namespace JarvisAssistant.Core.Models
{
    /// <summary>
    /// Optimization levels for model loading.
    /// </summary>
    public enum OptimizationLevel
    {
        /// <summary>
        /// Maximum quality, slower performance.
        /// </summary>
        Quality,

        /// <summary>
        /// Balanced quality and performance.
        /// </summary>
        Balanced,

        /// <summary>
        /// Maximum speed, lower quality.
        /// </summary>
        Speed,

        /// <summary>
        /// Custom optimization settings.
        /// </summary>
        Custom
    }

    /// <summary>
    /// Model use cases for targeted optimization.
    /// </summary>
    public enum ModelUseCase
    {
        /// <summary>
        /// Code completion and generation.
        /// </summary>
        CodeCompletion,

        /// <summary>
        /// General chat conversations.
        /// </summary>
        GeneralChat,

        /// <summary>
        /// Document analysis and summarization.
        /// </summary>
        DocumentAnalysis,

        /// <summary>
        /// Embedding generation.
        /// </summary>
        EmbeddingGeneration,

        /// <summary>
        /// Real-time chat streaming.
        /// </summary>
        StreamingChat
    }

    /// <summary>
    /// Model unloading strategies.
    /// </summary>
    public enum ModelUnloadStrategy
    {
        /// <summary>
        /// Least recently used models are unloaded first.
        /// </summary>
        LeastRecentlyUsed,

        /// <summary>
        /// Models with lowest usage frequency are unloaded first.
        /// </summary>
        LowestUsageFrequency,

        /// <summary>
        /// Largest models are unloaded first to free most memory.
        /// </summary>
        LargestFirst,

        /// <summary>
        /// Custom unloading logic.
        /// </summary>
        Custom
    }

    /// <summary>
    /// Result of model loading operation.
    /// </summary>
    public class ModelLoadResult
    {
        /// <summary>
        /// Gets or sets whether the model was loaded successfully.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the loaded model name.
        /// </summary>
        public string ModelName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the time taken to load the model.
        /// </summary>
        public TimeSpan LoadTime { get; set; }

        /// <summary>
        /// Gets or sets the memory consumed by the model.
        /// </summary>
        public long MemoryUsageBytes { get; set; }

        /// <summary>
        /// Gets or sets the VRAM consumed by the model.
        /// </summary>
        public long VramUsageBytes { get; set; }

        /// <summary>
        /// Gets or sets optimization settings applied.
        /// </summary>
        public ModelOptimizationSettings OptimizationSettings { get; set; } = new();

        /// <summary>
        /// Gets or sets any error message if loading failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets model performance characteristics.
        /// </summary>
        public ModelPerformanceInfo PerformanceInfo { get; set; } = new();
    }

    /// <summary>
    /// Model warmup result.
    /// </summary>
    public class ModelWarmupResult
    {
        /// <summary>
        /// Gets or sets whether warmup was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets warmup time.
        /// </summary>
        public TimeSpan WarmupTime { get; set; }

        /// <summary>
        /// Gets or sets first inference time after warmup.
        /// </summary>
        public TimeSpan FirstInferenceTime { get; set; }

        /// <summary>
        /// Gets or sets error message if warmup failed.
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Information about a loaded model's memory usage.
    /// </summary>
    public class ModelMemoryInfo
    {
        /// <summary>
        /// Gets or sets system memory usage in bytes.
        /// </summary>
        public long SystemMemoryBytes { get; set; }

        /// <summary>
        /// Gets or sets VRAM usage in bytes.
        /// </summary>
        public long VramBytes { get; set; }

        /// <summary>
        /// Gets or sets when the model was loaded.
        /// </summary>
        public DateTime LoadedAt { get; set; }

        /// <summary>
        /// Gets or sets when the model was last used.
        /// </summary>
        public DateTime LastUsedAt { get; set; }

        /// <summary>
        /// Gets or sets usage frequency.
        /// </summary>
        public int UsageCount { get; set; }

        /// <summary>
        /// Gets or sets model size information.
        /// </summary>
        public ModelSizeInfo SizeInfo { get; set; } = new();
    }

    /// <summary>
    /// Model optimization result.
    /// </summary>
    public class ModelOptimizationResult
    {
        /// <summary>
        /// Gets or sets whether optimization was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets optimizations applied.
        /// </summary>
        public List<string> OptimizationsApplied { get; set; } = new();

        /// <summary>
        /// Gets or sets performance improvement metrics.
        /// </summary>
        public PerformanceImprovement PerformanceImprovement { get; set; } = new();

        /// <summary>
        /// Gets or sets memory savings achieved.
        /// </summary>
        public long MemorySavingsBytes { get; set; }

        /// <summary>
        /// Gets or sets any warnings from optimization.
        /// </summary>
        public List<string> Warnings { get; set; } = new();
    }

    /// <summary>
    /// Optimization recommendation.
    /// </summary>
    public class OptimizationRecommendation
    {
        /// <summary>
        /// Gets or sets recommendation type.
        /// </summary>
        public RecommendationType Type { get; set; }

        /// <summary>
        /// Gets or sets recommendation description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets expected impact.
        /// </summary>
        public ImpactLevel ExpectedImpact { get; set; }

        /// <summary>
        /// Gets or sets implementation difficulty.
        /// </summary>
        public DifficultyLevel Difficulty { get; set; }

        /// <summary>
        /// Gets or sets estimated performance gain.
        /// </summary>
        public PerformanceGain EstimatedGain { get; set; } = new();
    }

    /// <summary>
    /// Model optimization settings.
    /// </summary>
    public class ModelOptimizationSettings
    {
        /// <summary>
        /// Gets or sets whether 4-bit quantization is enabled.
        /// </summary>
        public bool Use4BitQuantization { get; set; }

        /// <summary>
        /// Gets or sets whether 8-bit quantization is enabled.
        /// </summary>
        public bool Use8BitQuantization { get; set; }

        /// <summary>
        /// Gets or sets whether model compression is enabled.
        /// </summary>
        public bool UseCompression { get; set; }

        /// <summary>
        /// Gets or sets context window size.
        /// </summary>
        public int ContextWindowSize { get; set; } = 4096;

        /// <summary>
        /// Gets or sets maximum tokens per response.
        /// </summary>
        public int MaxTokensPerResponse { get; set; } = 1024;

        /// <summary>
        /// Gets or sets batch size for processing.
        /// </summary>
        public int BatchSize { get; set; } = 1;

        /// <summary>
        /// Gets or sets whether to use GPU acceleration.
        /// </summary>
        public bool UseGpuAcceleration { get; set; } = true;

        /// <summary>
        /// Gets or sets number of GPU layers to use.
        /// </summary>
        public int GpuLayers { get; set; } = -1; // -1 means use all available

        /// <summary>
        /// Gets or sets custom optimization parameters.
        /// </summary>
        public Dictionary<string, object> CustomParameters { get; set; } = new();
    }

    /// <summary>
    /// Model performance information.
    /// </summary>
    public class ModelPerformanceInfo
    {
        /// <summary>
        /// Gets or sets average tokens per second.
        /// </summary>
        public float TokensPerSecond { get; set; }

        /// <summary>
        /// Gets or sets average inference latency.
        /// </summary>
        public TimeSpan AverageLatency { get; set; }

        /// <summary>
        /// Gets or sets time to first token.
        /// </summary>
        public TimeSpan TimeToFirstToken { get; set; }

        /// <summary>
        /// Gets or sets memory bandwidth utilization.
        /// </summary>
        public float MemoryBandwidthUtilization { get; set; }

        /// <summary>
        /// Gets or sets GPU utilization during inference.
        /// </summary>
        public float GpuUtilization { get; set; }
    }

    /// <summary>
    /// Model size information.
    /// </summary>
    public class ModelSizeInfo
    {
        /// <summary>
        /// Gets or sets parameter count.
        /// </summary>
        public long ParameterCount { get; set; }

        /// <summary>
        /// Gets or sets model file size in bytes.
        /// </summary>
        public long FileSizeBytes { get; set; }

        /// <summary>
        /// Gets or sets quantization bit size.
        /// </summary>
        public int QuantizationBits { get; set; }

        /// <summary>
        /// Gets or sets model architecture.
        /// </summary>
        public string Architecture { get; set; } = string.Empty;
    }

    /// <summary>
    /// Performance improvement metrics.
    /// </summary>
    public class PerformanceImprovement
    {
        /// <summary>
        /// Gets or sets speed improvement factor.
        /// </summary>
        public float SpeedImprovement { get; set; }

        /// <summary>
        /// Gets or sets latency reduction percentage.
        /// </summary>
        public float LatencyReduction { get; set; }

        /// <summary>
        /// Gets or sets throughput increase percentage.
        /// </summary>
        public float ThroughputIncrease { get; set; }

        /// <summary>
        /// Gets or sets memory efficiency improvement.
        /// </summary>
        public float MemoryEfficiencyGain { get; set; }
    }

    /// <summary>
    /// Performance gain estimation.
    /// </summary>
    public class PerformanceGain
    {
        /// <summary>
        /// Gets or sets expected speed increase percentage.
        /// </summary>
        public float SpeedIncreasePercent { get; set; }

        /// <summary>
        /// Gets or sets expected memory savings percentage.
        /// </summary>
        public float MemorySavingsPercent { get; set; }

        /// <summary>
        /// Gets or sets expected quality impact percentage.
        /// </summary>
        public float QualityImpactPercent { get; set; }
    }

    /// <summary>
    /// Recommendation types.
    /// </summary>
    public enum RecommendationType
    {
        ModelOptimization,
        MemoryManagement,
        CacheConfiguration,
        BatchSizeAdjustment,
        QuantizationSettings,
        HardwareUpgrade
    }

    /// <summary>
    /// Impact levels.
    /// </summary>
    public enum ImpactLevel
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Difficulty levels.
    /// </summary>
    public enum DifficultyLevel
    {
        Easy,
        Medium,
        Hard,
        Expert
    }
}
