using JarvisAssistant.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime;

namespace JarvisAssistant.Services
{
    /// <summary>
    /// Service for optimizing performance on RTX 3060 and similar GPUs.
    /// </summary>
    public class Rtx3060OptimizationService : IPerformanceOptimizationService
    {
        private readonly ILogger<Rtx3060OptimizationService> _logger;
        private readonly IGpuMonitoringService? _gpuMonitoringService;
        private readonly ITelemetryService _telemetryService;
        private readonly PerformanceConfiguration _config;
        private PerformanceCounter? _gpuUsageCounter;
        private PerformanceCounter? _memoryCounter;

        public Rtx3060OptimizationService(
            ILogger<Rtx3060OptimizationService> logger,
            IGpuMonitoringService? gpuMonitoringService,
            ITelemetryService telemetryService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _gpuMonitoringService = gpuMonitoringService;
            _telemetryService = telemetryService ?? throw new ArgumentNullException(nameof(telemetryService));
            _config = new PerformanceConfiguration();

            InitializePerformanceCounters();
        }

        /// <inheritdoc/>
        public async Task<PerformanceProfile> GetOptimalProfileAsync()
        {
            try
            {
                var gpuInfo = await GetGpuInformationAsync();
                var memoryInfo = await GetMemoryInformationAsync();
                var systemLoad = await GetSystemLoadAsync();

                var profile = CreateOptimalProfile(gpuInfo, memoryInfo, systemLoad);
                
                await _telemetryService.TrackEventAsync("PerformanceProfileGenerated", new Dictionary<string, object>
                {
                    ["profileType"] = profile.ProfileType,
                    ["vramUsage"] = gpuInfo.VramUsagePercentage,
                    ["gpuUsage"] = gpuInfo.GpuUsagePercentage,
                    ["systemLoad"] = systemLoad
                });

                return profile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating optimal performance profile");
                await _telemetryService.TrackExceptionAsync(ex);
                
                // Return conservative fallback profile
                return new PerformanceProfile
                {
                    ProfileType = PerformanceProfileType.Balanced,
                    MaxConcurrentOperations = 2,
                    MemoryLimit = 2048, // 2GB conservative limit
                    UseGpuAcceleration = false, // Disable if monitoring unavailable
                    Settings = GetFallbackSettings()
                };
            }
        }

        /// <inheritdoc/>
        public async Task ApplyOptimizationsAsync(PerformanceProfile profile)
        {
            _logger.LogInformation("Applying performance optimizations: {ProfileType}", profile.ProfileType);

            try
            {
                // Apply memory optimizations
                await ApplyMemoryOptimizationsAsync(profile);

                // Apply GPU optimizations if available
                if (profile.UseGpuAcceleration && _gpuMonitoringService != null)
                {
                    await ApplyGpuOptimizationsAsync(profile);
                }

                // Apply thread pool optimizations
                await ApplyThreadPoolOptimizationsAsync(profile);

                // Apply garbage collection optimizations
                await ApplyGcOptimizationsAsync(profile);

                await _telemetryService.TrackEventAsync("OptimizationsApplied", new Dictionary<string, object>
                {
                    ["profileType"] = profile.ProfileType.ToString(),
                    ["useGpuAcceleration"] = profile.UseGpuAcceleration,
                    ["memoryLimit"] = profile.MemoryLimit
                });

                _logger.LogInformation("Performance optimizations applied successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying performance optimizations");
                await _telemetryService.TrackExceptionAsync(ex);
            }
        }

        /// <inheritdoc/>
        public async Task<PerformanceMetrics> GetCurrentMetricsAsync()
        {
            try
            {
                var metrics = new PerformanceMetrics
                {
                    Timestamp = DateTime.UtcNow
                };

                // Get GPU metrics if available
                if (_gpuMonitoringService != null)
                {
                    var gpuStatus = await _gpuMonitoringService.GetCurrentGpuStatusAsync();
                    if (gpuStatus != null)
                    {
                        metrics.GpuUsagePercentage = gpuStatus.UtilizationPercent;
                        metrics.VramUsagePercentage = gpuStatus.VramUsage.UsagePercent;
                        metrics.VramUsageMB = gpuStatus.VramUsage.UsedBytes / (1024 * 1024);
                    }
                }
                else
                {
                    // Fallback to performance counters
                    metrics.GpuUsagePercentage = GetGpuUsageFromCounter();
                }

                // Get system metrics
                metrics.CpuUsagePercentage = GetCpuUsage();
                metrics.MemoryUsageMB = GetMemoryUsage();
                metrics.AvailableMemoryMB = GetAvailableMemory();

                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current performance metrics");
                await _telemetryService.TrackExceptionAsync(ex);
                
                return new PerformanceMetrics
                {
                    Timestamp = DateTime.UtcNow,
                    CpuUsagePercentage = 0,
                    MemoryUsageMB = 0,
                    AvailableMemoryMB = 8192 // Default assumption
                };
            }
        }

        private async Task<GpuInformation> GetGpuInformationAsync()
        {
            if (_gpuMonitoringService != null)
            {
                var gpuStatus = await _gpuMonitoringService.GetCurrentGpuStatusAsync();
                if (gpuStatus != null)
                {
                    return new GpuInformation
                    {
                        GpuUsagePercentage = gpuStatus.UtilizationPercent,
                        VramUsagePercentage = gpuStatus.VramUsage.UsagePercent,
                        VramUsageMB = gpuStatus.VramUsage.UsedBytes / (1024 * 1024),
                        IsRtx3060 = gpuStatus.Name?.Contains("RTX 3060") ?? false
                    };
                }
            }

            // Fallback method using performance counters or system info
            return new GpuInformation
            {
                GpuUsagePercentage = GetGpuUsageFromCounter(),
                VramUsagePercentage = 0, // Not available without proper monitoring
                VramUsageMB = 0,
                IsRtx3060 = false // Cannot determine without WMI/monitoring service
            };
        }

        private async Task<MemoryInformation> GetMemoryInformationAsync()
        {
            var memoryUsage = GetMemoryUsage();
            var availableMemory = GetAvailableMemory();
            var totalMemory = memoryUsage + availableMemory;

            return new MemoryInformation
            {
                TotalMemoryMB = totalMemory,
                UsedMemoryMB = memoryUsage,
                AvailableMemoryMB = availableMemory,
                UsagePercentage = totalMemory > 0 ? (double)memoryUsage / totalMemory * 100 : 0
            };
        }

        private async Task<double> GetSystemLoadAsync()
        {
            // Calculate system load based on CPU usage
            var cpuUsage = GetCpuUsage();
            return cpuUsage;
        }

        private PerformanceProfile CreateOptimalProfile(GpuInformation gpu, MemoryInformation memory, double systemLoad)
        {
            var profileType = DetermineProfileType(gpu, memory, systemLoad);
            
            return profileType switch
            {
                PerformanceProfileType.HighPerformance => CreateHighPerformanceProfile(gpu, memory),
                PerformanceProfileType.Balanced => CreateBalancedProfile(gpu, memory),
                PerformanceProfileType.PowerSaver => CreatePowerSaverProfile(gpu, memory),
                _ => CreateBalancedProfile(gpu, memory)
            };
        }

        private PerformanceProfileType DetermineProfileType(GpuInformation gpu, MemoryInformation memory, double systemLoad)
        {
            // High performance if we have good GPU and plenty of memory
            if (gpu.IsRtx3060 && memory.AvailableMemoryMB > 6144 && systemLoad < 50)
            {
                return PerformanceProfileType.HighPerformance;
            }

            // Power saver if system is under heavy load or low memory
            if (systemLoad > 80 || memory.AvailableMemoryMB < 2048)
            {
                return PerformanceProfileType.PowerSaver;
            }

            // Balanced for everything else
            return PerformanceProfileType.Balanced;
        }

        private PerformanceProfile CreateHighPerformanceProfile(GpuInformation gpu, MemoryInformation memory)
        {
            return new PerformanceProfile
            {
                ProfileType = PerformanceProfileType.HighPerformance,
                MaxConcurrentOperations = 6,
                MemoryLimit = Math.Min(6144, memory.AvailableMemoryMB / 2), // Use up to 6GB or half available
                UseGpuAcceleration = gpu.IsRtx3060,
                Settings = new Dictionary<string, object>
                {
                    ["threadPoolMinThreads"] = Environment.ProcessorCount * 2,
                    ["threadPoolMaxThreads"] = Environment.ProcessorCount * 4,
                    ["gcMode"] = "server",
                    ["gcConcurrent"] = true,
                    ["bufferPoolSize"] = 32,
                    ["cacheSize"] = 1024
                }
            };
        }

        private PerformanceProfile CreateBalancedProfile(GpuInformation gpu, MemoryInformation memory)
        {
            return new PerformanceProfile
            {
                ProfileType = PerformanceProfileType.Balanced,
                MaxConcurrentOperations = 4,
                MemoryLimit = Math.Min(4096, memory.AvailableMemoryMB / 3), // Use up to 4GB or third available
                UseGpuAcceleration = _gpuMonitoringService != null,
                Settings = new Dictionary<string, object>
                {
                    ["threadPoolMinThreads"] = Environment.ProcessorCount,
                    ["threadPoolMaxThreads"] = Environment.ProcessorCount * 2,
                    ["gcMode"] = "workstation",
                    ["gcConcurrent"] = true,
                    ["bufferPoolSize"] = 16,
                    ["cacheSize"] = 512
                }
            };
        }

        private PerformanceProfile CreatePowerSaverProfile(GpuInformation gpu, MemoryInformation memory)
        {
            return new PerformanceProfile
            {
                ProfileType = PerformanceProfileType.PowerSaver,
                MaxConcurrentOperations = 2,
                MemoryLimit = Math.Min(2048, memory.AvailableMemoryMB / 4), // Use up to 2GB or quarter available
                UseGpuAcceleration = false, // Disable GPU acceleration to save power
                Settings = new Dictionary<string, object>
                {
                    ["threadPoolMinThreads"] = Math.Max(1, Environment.ProcessorCount / 2),
                    ["threadPoolMaxThreads"] = Environment.ProcessorCount,
                    ["gcMode"] = "workstation",
                    ["gcConcurrent"] = false,
                    ["bufferPoolSize"] = 8,
                    ["cacheSize"] = 256
                }
            };
        }

        private Dictionary<string, object> GetFallbackSettings()
        {
            return new Dictionary<string, object>
            {
                ["threadPoolMinThreads"] = Environment.ProcessorCount,
                ["threadPoolMaxThreads"] = Environment.ProcessorCount * 2,
                ["gcMode"] = "workstation",
                ["gcConcurrent"] = true,
                ["bufferPoolSize"] = 8,
                ["cacheSize"] = 256
            };
        }

        private async Task ApplyMemoryOptimizationsAsync(PerformanceProfile profile)
        {
            // Set memory limits and garbage collection settings
            if (profile.Settings.TryGetValue("gcMode", out var gcMode))
            {
                // Note: GC mode cannot be changed at runtime, this would need to be set at startup
                _logger.LogDebug("GC mode setting: {GcMode}", gcMode);
            }

            // Force garbage collection if in power saver mode
            if (profile.ProfileType == PerformanceProfileType.PowerSaver)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }

        private async Task ApplyGpuOptimizationsAsync(PerformanceProfile profile)
        {
            if (_gpuMonitoringService == null) return;

            try
            {
                // Apply GPU-specific optimizations
                _logger.LogDebug("Applying GPU optimizations for profile: {ProfileType}", profile.ProfileType);
                
                // This would interact with GPU monitoring service to optimize settings
                await Task.CompletedTask; // Placeholder for GPU optimization logic
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to apply GPU optimizations");
            }
        }

        private async Task ApplyThreadPoolOptimizationsAsync(PerformanceProfile profile)
        {
            try
            {
                if (profile.Settings.TryGetValue("threadPoolMinThreads", out var minThreadsObj) &&
                    profile.Settings.TryGetValue("threadPoolMaxThreads", out var maxThreadsObj))
                {
                    var minThreads = Convert.ToInt32(minThreadsObj);
                    var maxThreads = Convert.ToInt32(maxThreadsObj);

                    ThreadPool.SetMinThreads(minThreads, minThreads);
                    ThreadPool.SetMaxThreads(maxThreads, maxThreads);

                    _logger.LogDebug("Thread pool optimized: Min={MinThreads}, Max={MaxThreads}", minThreads, maxThreads);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to apply thread pool optimizations");
            }
        }

        private async Task ApplyGcOptimizationsAsync(PerformanceProfile profile)
        {
            try
            {
                // Configure garbage collection settings
                if (profile.ProfileType == PerformanceProfileType.HighPerformance)
                {
                    // Aggressive memory management for high performance
                    GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                }
                else if (profile.ProfileType == PerformanceProfileType.PowerSaver)
                {
                    // Conservative memory management for power saving
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to apply GC optimizations");
            }
        }

        private void InitializePerformanceCounters()
        {
            try
            {
                // Try to initialize GPU performance counter
                _gpuUsageCounter = new PerformanceCounter("GPU Engine", "Utilization Percentage", "_Total");
                _logger.LogDebug("GPU performance counter initialized");
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not initialize GPU performance counter (this is normal on many systems)");
            }

            try
            {
                // Initialize memory performance counter
                _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
                _logger.LogDebug("Memory performance counter initialized");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not initialize memory performance counter");
            }
        }

        private double GetGpuUsageFromCounter()
        {
            try
            {
                return _gpuUsageCounter?.NextValue() ?? 0.0;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not read GPU usage from performance counter");
                return 0.0;
            }
        }

        private double GetCpuUsage()
        {
            try
            {
                using var process = Process.GetCurrentProcess();
                var startTime = DateTime.UtcNow;
                var startCpuUsage = process.TotalProcessorTime;
                
                Thread.Sleep(100); // Small delay for measurement
                
                var endTime = DateTime.UtcNow;
                var endCpuUsage = process.TotalProcessorTime;
                
                var cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
                var totalMsPassed = (endTime - startTime).TotalMilliseconds;
                var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed) * 100;
                
                return Math.Max(0, Math.Min(100, cpuUsageTotal));
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not calculate CPU usage");
                return 0.0;
            }
        }

        private long GetMemoryUsage()
        {
            try
            {
                using var process = Process.GetCurrentProcess();
                return process.WorkingSet64 / (1024 * 1024); // Convert to MB
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not get memory usage");
                return 0;
            }
        }

        private long GetAvailableMemory()
        {
            try
            {
                return (long)(_memoryCounter?.NextValue() ?? 4096); // Default to 4GB if unavailable
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not get available memory");
                return 4096; // Default fallback
            }
        }

        public void Dispose()
        {
            _gpuUsageCounter?.Dispose();
            _memoryCounter?.Dispose();
        }
    }

    // Supporting classes
    public class PerformanceConfiguration
    {
        public int DefaultMemoryLimitMB { get; set; } = 4096;
        public int DefaultMaxConcurrentOperations { get; set; } = 4;
        public bool EnableGpuAcceleration { get; set; } = true;
    }

    public class GpuInformation
    {
        public double GpuUsagePercentage { get; set; }
        public double VramUsagePercentage { get; set; }
        public long VramUsageMB { get; set; }
        public bool IsRtx3060 { get; set; }
    }

    public class MemoryInformation
    {
        public long TotalMemoryMB { get; set; }
        public long UsedMemoryMB { get; set; }
        public long AvailableMemoryMB { get; set; }
        public double UsagePercentage { get; set; }
    }

    public class PerformanceProfile
    {
        public PerformanceProfileType ProfileType { get; set; }
        public int MaxConcurrentOperations { get; set; }
        public long MemoryLimit { get; set; }
        public bool UseGpuAcceleration { get; set; }
        public Dictionary<string, object> Settings { get; set; } = new();
    }

    public enum PerformanceProfileType
    {
        PowerSaver,
        Balanced,
        HighPerformance
    }

    public class PerformanceMetrics
    {
        public DateTime Timestamp { get; set; }
        public double CpuUsagePercentage { get; set; }
        public long MemoryUsageMB { get; set; }
        public long AvailableMemoryMB { get; set; }
        public double GpuUsagePercentage { get; set; }
        public double VramUsagePercentage { get; set; }
        public long VramUsageMB { get; set; }
    }

    public interface IPerformanceOptimizationService
    {
        Task<PerformanceProfile> GetOptimalProfileAsync();
        Task ApplyOptimizationsAsync(PerformanceProfile profile);
        Task<PerformanceMetrics> GetCurrentMetricsAsync();
    }
}
