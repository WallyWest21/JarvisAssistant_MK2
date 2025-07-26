using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace JarvisAssistant.Services
{
    /// <summary>
    /// GPU monitoring service implementation for RTX 3060 12GB optimization.
    /// </summary>
    public class GpuMonitoringService : IGpuMonitoringService, IDisposable
    {
        private readonly ILogger<GpuMonitoringService> _logger;
        private readonly Timer? _monitoringTimer;
        private readonly List<GpuPerformanceMetrics> _performanceHistory;
        private readonly object _lockObject = new();
        private bool _isMonitoring;
        private bool _disposed;

        private const int HISTORY_RETENTION_HOURS = 24;
        private const int MONITORING_INTERVAL_MS = 1000;
        private const float VRAM_THRESHOLD_PERCENT = 85.0f;

        public event EventHandler<GpuStatusChangedEventArgs>? GpuStatusChanged;
        public event EventHandler<VramThresholdExceededEventArgs>? VramThresholdExceeded;

        public GpuMonitoringService(ILogger<GpuMonitoringService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _performanceHistory = new List<GpuPerformanceMetrics>();
            _monitoringTimer = new Timer(MonitoringCallback, null, Timeout.Infinite, Timeout.Infinite);
        }

        /// <inheritdoc/>
        public async Task<GpuStatus> GetGpuStatusAsync()
        {
            try
            {
                _logger.LogDebug("Getting GPU status");

                var gpuStatus = new GpuStatus();

                // Check for NVIDIA GPU using WMI
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController WHERE Name LIKE '%NVIDIA%'");
                using var collection = searcher.Get();

                foreach (ManagementObject obj in collection)
                {
                    gpuStatus.IsAvailable = true;
                    gpuStatus.Name = obj["Name"]?.ToString() ?? "Unknown NVIDIA GPU";
                    
                    // Get VRAM info (in bytes)
                    if (obj["AdapterRAM"] != null && uint.TryParse(obj["AdapterRAM"].ToString(), out uint vramBytes))
                    {
                        gpuStatus.TotalVramBytes = vramBytes;
                    }

                    gpuStatus.DriverVersion = obj["DriverVersion"]?.ToString();
                    break; // Use first NVIDIA GPU found
                }

                // Check CUDA availability
                await CheckCudaAvailabilityAsync(gpuStatus);

                // Get real-time metrics if GPU is available
                if (gpuStatus.IsAvailable)
                {
                    await UpdateRealTimeMetricsAsync(gpuStatus);
                }

                _logger.LogInformation("GPU Status: Available={Available}, Name={Name}, VRAM={VramGB}GB, CUDA={CudaAvailable}", 
                    gpuStatus.IsAvailable, gpuStatus.Name, gpuStatus.TotalVramBytes / (1024.0 * 1024.0 * 1024.0), gpuStatus.CudaAvailable);

                return gpuStatus;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get GPU status");
                return new GpuStatus { IsAvailable = false };
            }
        }

        /// <inheritdoc/>
        public async Task<GpuStatus?> GetCurrentGpuStatusAsync()
        {
            return await GetGpuStatusAsync();
        }

        /// <inheritdoc/>
        public async Task<VramUsage> GetVramUsageAsync()
        {
            try
            {
                var vramUsage = new VramUsage();

                // Get VRAM info using nvidia-ml-py equivalent or WMI
                if (await IsNvidiaSmiAvailableAsync())
                {
                    vramUsage = await GetVramUsageFromNvidiaSmiAsync();
                }
                else
                {
                    // Fallback to WMI estimates
                    vramUsage = await GetVramUsageFromWmiAsync();
                }

                // Check threshold and trigger event if needed
                if (vramUsage.UsagePercent > VRAM_THRESHOLD_PERCENT)
                {
                    VramThresholdExceeded?.Invoke(this, new VramThresholdExceededEventArgs
                    {
                        CurrentUsage = vramUsage,
                        ThresholdPercent = VRAM_THRESHOLD_PERCENT,
                        RecommendedAction = "Consider unloading unused models or reducing model size"
                    });
                }

                return vramUsage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get VRAM usage");
                return new VramUsage();
            }
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<GpuPerformanceMetrics> MonitorPerformanceAsync(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested && !_disposed)
            {
                GpuPerformanceMetrics? metrics = null;
                
                lock (_lockObject)
                {
                    if (_performanceHistory.Count > 0)
                    {
                        metrics = _performanceHistory.Last();
                    }
                }

                if (metrics != null)
                {
                    yield return metrics;
                }

                try
                {
                    await Task.Delay(MONITORING_INTERVAL_MS, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        /// <inheritdoc/>
        public async Task StartMonitoringAsync()
        {
            if (_isMonitoring)
            {
                _logger.LogWarning("GPU monitoring is already started");
                return;
            }

            _logger.LogInformation("Starting GPU performance monitoring");
            
            _isMonitoring = true;
            _monitoringTimer?.Change(0, MONITORING_INTERVAL_MS);
            
            await Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task StopMonitoringAsync()
        {
            if (!_isMonitoring)
            {
                _logger.LogWarning("GPU monitoring is already stopped");
                return;
            }

            _logger.LogInformation("Stopping GPU performance monitoring");
            
            _isMonitoring = false;
            _monitoringTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            
            await Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<PerformanceHistory> GetPerformanceHistoryAsync(TimeSpan duration)
        {
            try
            {
                var cutoffTime = DateTime.UtcNow - duration;
                List<GpuPerformanceMetrics> relevantMetrics;

                lock (_lockObject)
                {
                    relevantMetrics = _performanceHistory
                        .Where(m => m.Timestamp >= cutoffTime)
                        .ToList();
                }

                var history = new PerformanceHistory
                {
                    TimeRange = duration,
                    Metrics = relevantMetrics
                };

                if (relevantMetrics.Count > 0)
                {
                    // Calculate averages
                    history.AverageMetrics = new GpuPerformanceMetrics
                    {
                        GpuUtilization = relevantMetrics.Average(m => m.GpuUtilization),
                        MemoryUtilization = relevantMetrics.Average(m => m.MemoryUtilization),
                        Temperature = (int)relevantMetrics.Average(m => m.Temperature),
                        PowerConsumption = relevantMetrics.Average(m => m.PowerConsumption),
                        AverageInferenceTime = TimeSpan.FromMilliseconds(
                            relevantMetrics.Where(m => m.RecentInferenceTimes.Count > 0)
                                          .SelectMany(m => m.RecentInferenceTimes)
                                          .Average(t => t.TotalMilliseconds))
                    };

                    // Calculate peaks
                    history.PeakMetrics = new GpuPerformanceMetrics
                    {
                        GpuUtilization = relevantMetrics.Max(m => m.GpuUtilization),
                        MemoryUtilization = relevantMetrics.Max(m => m.MemoryUtilization),
                        Temperature = relevantMetrics.Max(m => m.Temperature),
                        PowerConsumption = relevantMetrics.Max(m => m.PowerConsumption)
                    };
                }

                return await Task.FromResult(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get performance history for duration {Duration}", duration);
                return new PerformanceHistory { TimeRange = duration };
            }
        }

        private async void MonitoringCallback(object? state)
        {
            if (!_isMonitoring || _disposed) return;

            try
            {
                var metrics = await CollectPerformanceMetricsAsync();
                
                lock (_lockObject)
                {
                    _performanceHistory.Add(metrics);
                    
                    // Clean up old metrics
                    var cutoffTime = DateTime.UtcNow.AddHours(-HISTORY_RETENTION_HOURS);
                    _performanceHistory.RemoveAll(m => m.Timestamp < cutoffTime);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during GPU monitoring");
            }
        }

        private async Task<GpuPerformanceMetrics> CollectPerformanceMetricsAsync()
        {
            var metrics = new GpuPerformanceMetrics();

            try
            {
                // Get GPU utilization and temperature
                if (await IsNvidiaSmiAvailableAsync())
                {
                    var nvidiaSmiMetrics = await GetMetricsFromNvidiaSmiAsync();
                    metrics.GpuUtilization = nvidiaSmiMetrics.GpuUtilization;
                    metrics.Temperature = nvidiaSmiMetrics.Temperature;
                    metrics.PowerConsumption = nvidiaSmiMetrics.PowerConsumption;
                }

                // Get VRAM usage
                var vramUsage = await GetVramUsageAsync();
                metrics.MemoryUtilization = vramUsage.UsagePercent;

                // Add to recent metrics
                lock (_lockObject)
                {
                    if (_performanceHistory.Count > 0)
                    {
                        var recentMetrics = _performanceHistory.TakeLast(10).ToList();
                        var avgInferenceTime = recentMetrics
                            .SelectMany(m => m.RecentInferenceTimes)
                            .DefaultIfEmpty(TimeSpan.Zero)
                            .Average(t => t.TotalMilliseconds);
                        
                        metrics.AverageInferenceTime = TimeSpan.FromMilliseconds(avgInferenceTime);
                    }
                }

                return metrics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to collect performance metrics");
                return metrics;
            }
        }

        private async Task CheckCudaAvailabilityAsync(GpuStatus gpuStatus)
        {
            try
            {
                // Check if CUDA is available by looking for nvidia-ml.dll or running nvidia-smi
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var nvidiaSmiPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), 
                        "NVIDIA Corporation", "NVSMI", "nvidia-smi.exe");
                    
                    if (File.Exists(nvidiaSmiPath))
                    {
                        var processInfo = new ProcessStartInfo
                        {
                            FileName = nvidiaSmiPath,
                            Arguments = "--query-gpu=driver_version --format=csv,noheader,nounits",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };

                        using var process = Process.Start(processInfo);
                        if (process != null)
                        {
                            await process.WaitForExitAsync();
                            if (process.ExitCode == 0)
                            {
                                gpuStatus.CudaAvailable = true;
                                var output = await process.StandardOutput.ReadToEndAsync();
                                gpuStatus.DriverVersion = output.Trim();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check CUDA availability");
            }
        }

        private async Task UpdateRealTimeMetricsAsync(GpuStatus gpuStatus)
        {
            try
            {
                // Get current GPU utilization and temperature using nvidia-smi
                if (await IsNvidiaSmiAvailableAsync())
                {
                    var metrics = await GetMetricsFromNvidiaSmiAsync();
                    gpuStatus.UtilizationPercent = metrics.GpuUtilization;
                    gpuStatus.TemperatureCelsius = metrics.Temperature;
                    gpuStatus.PowerConsumptionWatts = metrics.PowerConsumption;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update real-time GPU metrics");
            }
        }

        private async Task<bool> IsNvidiaSmiAvailableAsync()
        {
            try
            {
                var nvidiaSmiPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), 
                    "NVIDIA Corporation", "NVSMI", "nvidia-smi.exe");
                
                return await Task.FromResult(File.Exists(nvidiaSmiPath));
            }
            catch
            {
                return false;
            }
        }

        private async Task<VramUsage> GetVramUsageFromNvidiaSmiAsync()
        {
            try
            {
                var nvidiaSmiPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), 
                    "NVIDIA Corporation", "NVSMI", "nvidia-smi.exe");

                var processInfo = new ProcessStartInfo
                {
                    FileName = nvidiaSmiPath,
                    Arguments = "--query-gpu=memory.total,memory.used,memory.free --format=csv,noheader,nounits",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    if (process.ExitCode == 0)
                    {
                        var output = await process.StandardOutput.ReadToEndAsync();
                        var values = output.Trim().Split(',').Select(v => v.Trim()).ToArray();
                        
                        if (values.Length >= 3 && 
                            long.TryParse(values[0], out long totalMB) &&
                            long.TryParse(values[1], out long usedMB) &&
                            long.TryParse(values[2], out long freeMB))
                        {
                            return new VramUsage
                            {
                                TotalBytes = totalMB * 1024 * 1024,
                                UsedBytes = usedMB * 1024 * 1024,
                                FreeBytes = freeMB * 1024 * 1024
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get VRAM usage from nvidia-smi");
            }

            return new VramUsage();
        }

        private async Task<VramUsage> GetVramUsageFromWmiAsync()
        {
            try
            {
                // Fallback to WMI estimates (less accurate)
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController WHERE Name LIKE '%NVIDIA%'");
                using var collection = searcher.Get();

                foreach (ManagementObject obj in collection)
                {
                    if (obj["AdapterRAM"] != null && uint.TryParse(obj["AdapterRAM"].ToString(), out uint vramBytes))
                    {
                        // Estimate usage (this is not accurate, just a placeholder)
                        var totalBytes = (long)vramBytes;
                        var estimatedUsedBytes = totalBytes / 4; // Rough estimate
                        
                        return new VramUsage
                        {
                            TotalBytes = totalBytes,
                            UsedBytes = estimatedUsedBytes,
                            FreeBytes = totalBytes - estimatedUsedBytes
                        };
                    }
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get VRAM usage from WMI");
            }

            return await Task.FromResult(new VramUsage());
        }

        private async Task<(float GpuUtilization, int Temperature, float PowerConsumption)> GetMetricsFromNvidiaSmiAsync()
        {
            try
            {
                var nvidiaSmiPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), 
                    "NVIDIA Corporation", "NVSMI", "nvidia-smi.exe");

                var processInfo = new ProcessStartInfo
                {
                    FileName = nvidiaSmiPath,
                    Arguments = "--query-gpu=utilization.gpu,temperature.gpu,power.draw --format=csv,noheader,nounits",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(processInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    if (process.ExitCode == 0)
                    {
                        var output = await process.StandardOutput.ReadToEndAsync();
                        var values = output.Trim().Split(',').Select(v => v.Trim()).ToArray();
                        
                        if (values.Length >= 3 && 
                            float.TryParse(values[0], out float utilization) &&
                            int.TryParse(values[1], out int temperature) &&
                            float.TryParse(values[2], out float power))
                        {
                            return (utilization, temperature, power);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get metrics from nvidia-smi");
            }

            return (0f, 0, 0f);
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            _isMonitoring = false;
            _monitoringTimer?.Dispose();
            
            _logger.LogInformation("GPU monitoring service disposed");
        }
    }
}
