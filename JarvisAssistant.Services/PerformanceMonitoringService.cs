using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace JarvisAssistant.Services
{
    /// <summary>
    /// Performance monitoring service for comprehensive application performance tracking.
    /// </summary>
    public class PerformanceMonitoringService : IPerformanceMonitoringService, IDisposable
    {
        private readonly ILogger<PerformanceMonitoringService> _logger;
        private readonly IGpuMonitoringService _gpuMonitoring;
        private readonly ConcurrentDictionary<RequestType, List<ResponseTimeEntry>> _responseTimeData;
        private readonly ConcurrentDictionary<string, ModelSwitchEntry> _modelSwitchData;
        private readonly List<MemoryUsageSnapshot> _memorySnapshots;
        private readonly Timer? _memoryMonitorTimer;
        private readonly PerformanceCounter? _cpuCounter;
        private readonly Process _currentProcess;
        private bool _disposed;

        // Performance thresholds
        private readonly Dictionary<RequestType, TimeSpan> _performanceThresholds = new()
        {
            [RequestType.CodeCompletion] = TimeSpan.FromMilliseconds(500),
            [RequestType.ChatMessage] = TimeSpan.FromSeconds(2),
            [RequestType.DocumentAnalysis] = TimeSpan.FromSeconds(10),
            [RequestType.EmbeddingGeneration] = TimeSpan.FromSeconds(5),
            [RequestType.ModelLoading] = TimeSpan.FromSeconds(30),
            [RequestType.VectorSearch] = TimeSpan.FromMilliseconds(100)
        };

        public event EventHandler<PerformanceAlertEventArgs>? PerformanceAlert;

        public PerformanceMonitoringService(
            ILogger<PerformanceMonitoringService> logger,
            IGpuMonitoringService gpuMonitoring)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _gpuMonitoring = gpuMonitoring ?? throw new ArgumentNullException(nameof(gpuMonitoring));
            _responseTimeData = new ConcurrentDictionary<RequestType, List<ResponseTimeEntry>>();
            _modelSwitchData = new ConcurrentDictionary<string, ModelSwitchEntry>();
            _memorySnapshots = new List<MemoryUsageSnapshot>();
            _currentProcess = Process.GetCurrentProcess();

            // Initialize performance counters
            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _cpuCounter.NextValue(); // First call often returns 0
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize CPU performance counter");
            }

            // Start memory monitoring
            _memoryMonitorTimer = new Timer(CollectMemorySnapshot, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));

            _logger.LogInformation("Performance monitoring service initialized");
        }

        /// <inheritdoc/>
        public async Task TrackResponseTimeAsync(RequestType requestType, TimeSpan duration, Dictionary<string, object>? metadata = null)
        {
            try
            {
                var entry = new ResponseTimeEntry
                {
                    RequestType = requestType,
                    Duration = duration,
                    Timestamp = DateTime.UtcNow,
                    Metadata = metadata ?? new Dictionary<string, object>()
                };

                // Add to tracking data
                if (!_responseTimeData.TryGetValue(requestType, out var entries))
                {
                    entries = new List<ResponseTimeEntry>();
                    _responseTimeData[requestType] = entries;
                }

                lock (entries)
                {
                    entries.Add(entry);
                    
                    // Keep only recent entries (last 1000 or last hour)
                    var cutoffTime = DateTime.UtcNow.AddHours(-1);
                    entries.RemoveAll(e => e.Timestamp < cutoffTime && entries.Count > 1000);
                }

                // Check for performance threshold violations
                if (_performanceThresholds.TryGetValue(requestType, out var threshold) && duration > threshold)
                {
                    await TriggerPerformanceAlertAsync(requestType, duration, threshold, metadata);
                }

                _logger.LogDebug("Tracked response time for {RequestType}: {Duration}ms", requestType, duration.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking response time for {RequestType}", requestType);
            }
        }

        /// <inheritdoc/>
        public async Task<PerformanceStatistics> GetPerformanceStatisticsAsync()
        {
            try
            {
                var statistics = new PerformanceStatistics
                {
                    Timestamp = DateTime.UtcNow
                };

                // Calculate response time statistics
                foreach (var kvp in _responseTimeData)
                {
                    var requestType = kvp.Key;
                    var entries = kvp.Value.ToList(); // Create a copy to avoid locking

                    if (entries.Any())
                    {
                        var durations = entries.Select(e => e.Duration).OrderBy(d => d).ToList();
                        
                        statistics.ResponseTimes[requestType] = new ResponseTimeStats
                        {
                            Average = TimeSpan.FromMilliseconds(durations.Average(d => d.TotalMilliseconds)),
                            Median = durations[durations.Count / 2],
                            P95 = durations[(int)(durations.Count * 0.95)],
                            P99 = durations[(int)(durations.Count * 0.99)],
                            Min = durations.First(),
                            Max = durations.Last(),
                            SampleCount = durations.Count
                        };
                    }
                }

                // Get memory usage statistics
                statistics.MemoryUsage = await GetCurrentMemoryUsageStatsAsync();

                // Get GPU performance statistics
                statistics.GpuPerformance = await GetCurrentGpuPerformanceStatsAsync();

                // Calculate cache hit ratios (placeholder - would be implemented with actual cache services)
                statistics.CacheHitRatios = await GetCacheHitRatiosAsync();

                // Count total requests processed
                statistics.TotalRequestsProcessed = _responseTimeData.Values.Sum(entries => entries.Count);

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting performance statistics");
                return new PerformanceStatistics();
            }
        }

        /// <inheritdoc/>
        public async Task<ThroughputMetrics> GetThroughputMetricsAsync(TimeSpan period)
        {
            try
            {
                var cutoffTime = DateTime.UtcNow - period;
                var recentEntries = _responseTimeData.Values
                    .SelectMany(entries => entries)
                    .Where(entry => entry.Timestamp >= cutoffTime)
                    .ToList();

                var totalRequests = recentEntries.Count;
                var totalTokens = recentEntries
                    .Where(e => e.Metadata.ContainsKey("TokenCount"))
                    .Sum(e => Convert.ToInt64(e.Metadata["TokenCount"]));

                var totalBytes = recentEntries
                    .Where(e => e.Metadata.ContainsKey("ByteCount"))
                    .Sum(e => Convert.ToInt64(e.Metadata["ByteCount"]));

                return new ThroughputMetrics
                {
                    RequestsPerSecond = (float)(totalRequests / period.TotalSeconds),
                    TokensPerSecond = (float)(totalTokens / period.TotalSeconds),
                    BytesPerSecond = (long)(totalBytes / period.TotalSeconds),
                    Period = period,
                    ConcurrentRequests = await GetCurrentConcurrentRequestsAsync(),
                    QueueDepth = await GetCurrentQueueDepthAsync()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating throughput metrics for period {Period}", period);
                return new ThroughputMetrics { Period = period };
            }
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<MemoryUsageSnapshot> MonitorMemoryUsageAsync(CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested && !_disposed)
            {
                MemoryUsageSnapshot? snapshot = null;
                
                lock (_memorySnapshots)
                {
                    if (_memorySnapshots.Count > 0)
                    {
                        snapshot = _memorySnapshots.Last();
                    }
                }

                if (snapshot != null)
                {
                    yield return snapshot;
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        /// <inheritdoc/>
        public async Task TrackModelSwitchAsync(string fromModel, string toModel, TimeSpan switchDuration)
        {
            try
            {
                var switchKey = $"{fromModel}->{toModel}";
                var entry = new ModelSwitchEntry
                {
                    FromModel = fromModel,
                    ToModel = toModel,
                    SwitchDuration = switchDuration,
                    Timestamp = DateTime.UtcNow
                };

                _modelSwitchData.AddOrUpdate(switchKey, entry, (key, existing) =>
                {
                    existing.SwitchCount++;
                    existing.TotalSwitchTime = existing.TotalSwitchTime.Add(switchDuration);
                    existing.AverageSwitchTime = TimeSpan.FromMilliseconds(
                        existing.TotalSwitchTime.TotalMilliseconds / existing.SwitchCount);
                    return existing;
                });

                _logger.LogInformation("Tracked model switch from {FromModel} to {ToModel} in {Duration}ms", 
                    fromModel, toModel, switchDuration.TotalMilliseconds);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking model switch from {FromModel} to {ToModel}", fromModel, toModel);
            }
        }

        /// <inheritdoc/>
        public async Task<PerformanceReport> GeneratePerformanceReportAsync(DateTime startTime, DateTime endTime)
        {
            try
            {
                _logger.LogInformation("Generating performance report for period {StartTime} to {EndTime}", startTime, endTime);

                var report = new PerformanceReport
                {
                    StartTime = startTime,
                    EndTime = endTime,
                    TimeRange = endTime - startTime
                };

                // Filter data for the specified time range
                var filteredEntries = _responseTimeData.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Where(e => e.Timestamp >= startTime && e.Timestamp <= endTime).ToList()
                );

                // Generate summary
                report.Summary = await GeneratePerformanceSummaryAsync(filteredEntries);

                // Generate detailed metrics
                foreach (var kvp in filteredEntries)
                {
                    if (kvp.Value.Any())
                    {
                        report.DetailedMetrics[kvp.Key] = await GenerateDetailedMetricsAsync(kvp.Value);
                    }
                }

                // Generate model analysis
                report.ModelAnalysis = await GenerateModelPerformanceAnalysisAsync(startTime, endTime);

                // Identify issues and recommendations
                report.Issues = await IdentifyPerformanceIssuesAsync(filteredEntries);
                report.Recommendations = await GeneratePerformanceRecommendationsAsync(report);

                _logger.LogInformation("Performance report generated with {IssueCount} issues and {RecommendationCount} recommendations", 
                    report.Issues.Count, report.Recommendations.Count);

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating performance report");
                return new PerformanceReport
                {
                    StartTime = startTime,
                    EndTime = endTime,
                    TimeRange = endTime - startTime
                };
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<PerformanceBottleneck>> DetectBottlenecksAsync()
        {
            var bottlenecks = new List<PerformanceBottleneck>();

            try
            {
                // Analyze response times for bottlenecks
                foreach (var kvp in _responseTimeData)
                {
                    var requestType = kvp.Key;
                    var entries = kvp.Value.ToList();

                    if (entries.Count < 10) continue; // Need enough data

                    var recentEntries = entries.TakeLast(100).ToList();
                    var averageTime = recentEntries.Average(e => e.Duration.TotalMilliseconds);
                    var threshold = _performanceThresholds[requestType].TotalMilliseconds;

                    if (averageTime > threshold * 1.5) // 50% above threshold
                    {
                        bottlenecks.Add(new PerformanceBottleneck
                        {
                            Type = BottleneckType.ModelLoading,
                            Severity = averageTime > threshold * 2 ? SeverityLevel.High : SeverityLevel.Medium,
                            Description = $"{requestType} requests are {averageTime:F0}ms on average, which is {((averageTime / threshold - 1) * 100):F0}% above threshold",
                            AffectedComponents = { requestType.ToString() },
                            PotentialSolutions = GetBottleneckSolutions(requestType),
                            EstimatedImpact = new PerformanceImpact
                            {
                                LatencyImpactPercent = (float)((averageTime / threshold - 1) * 100),
                                ThroughputImpactPercent = (float)((threshold / averageTime - 1) * 100),
                                ResourceImpactPercent = 20 // Estimate
                            }
                        });
                    }
                }

                // Check memory usage bottlenecks
                await DetectMemoryBottlenecksAsync(bottlenecks);

                // Check GPU bottlenecks
                await DetectGpuBottlenecksAsync(bottlenecks);

                _logger.LogDebug("Detected {BottleneckCount} performance bottlenecks", bottlenecks.Count);
                return bottlenecks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting performance bottlenecks");
                return bottlenecks;
            }
        }

        private async Task TriggerPerformanceAlertAsync(RequestType requestType, TimeSpan actualDuration, TimeSpan threshold, Dictionary<string, object>? metadata)
        {
            try
            {
                var alertArgs = new PerformanceAlertEventArgs
                {
                    AlertType = AlertType.LatencyThresholdExceeded,
                    Message = $"{requestType} request took {actualDuration.TotalMilliseconds:F0}ms, exceeding threshold of {threshold.TotalMilliseconds:F0}ms",
                    CurrentMetrics = new Dictionary<string, object>
                    {
                        ["RequestType"] = requestType.ToString(),
                        ["ActualDuration"] = actualDuration.TotalMilliseconds,
                        ["Threshold"] = threshold.TotalMilliseconds,
                        ["ExcessPercent"] = ((actualDuration.TotalMilliseconds / threshold.TotalMilliseconds - 1) * 100)
                    },
                    Thresholds = new Dictionary<string, object>
                    {
                        [requestType.ToString()] = threshold.TotalMilliseconds
                    }
                };

                // Add metadata if available
                if (metadata != null)
                {
                    foreach (var kvp in metadata)
                    {
                        alertArgs.CurrentMetrics[kvp.Key] = kvp.Value;
                    }
                }

                PerformanceAlert?.Invoke(this, alertArgs);

                _logger.LogWarning("Performance alert triggered: {Message}", alertArgs.Message);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering performance alert");
            }
        }

        private async void CollectMemorySnapshot(object? state)
        {
            if (_disposed) return;

            try
            {
                var snapshot = new MemoryUsageSnapshot
                {
                    Timestamp = DateTime.UtcNow,
                    ApplicationMemoryBytes = _currentProcess.WorkingSet64,
                    TotalSystemMemoryBytes = GC.GetTotalMemory(false)
                };

                // Get GPU memory if available
                try
                {
                    var vramUsage = await _gpuMonitoring.GetVramUsageAsync();
                    snapshot.GpuMemoryBytes = vramUsage.UsedBytes;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not get GPU memory for snapshot");
                }

                // Get GC stats
                snapshot.GcStats = new GarbageCollectionStats
                {
                    Gen0Collections = GC.CollectionCount(0),
                    Gen1Collections = GC.CollectionCount(1),
                    Gen2Collections = GC.CollectionCount(2),
                    TotalMemoryAllocated = GC.GetTotalMemory(false)
                };

                lock (_memorySnapshots)
                {
                    _memorySnapshots.Add(snapshot);
                    
                    // Keep only last hour of snapshots
                    var cutoffTime = DateTime.UtcNow.AddHours(-1);
                    _memorySnapshots.RemoveAll(s => s.Timestamp < cutoffTime);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting memory snapshot");
            }
        }

        private async Task<MemoryUsageStats> GetCurrentMemoryUsageStatsAsync()
        {
            try
            {
                var snapshots = _memorySnapshots.ToList();
                
                if (!snapshots.Any())
                    return new MemoryUsageStats();

                return new MemoryUsageStats
                {
                    CurrentUsageBytes = snapshots.Last().ApplicationMemoryBytes,
                    PeakUsageBytes = snapshots.Max(s => s.ApplicationMemoryBytes),
                    AverageUsageBytes = (long)snapshots.Average(s => (double)s.ApplicationMemoryBytes),
                    CacheMemoryUsageBytes = await GetCacheMemoryUsageAsync()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current memory usage stats");
                return new MemoryUsageStats();
            }
        }

        private async Task<GpuPerformanceStats> GetCurrentGpuPerformanceStatsAsync()
        {
            try
            {
                var gpuStatus = await _gpuMonitoring.GetGpuStatusAsync();
                var vramUsage = await _gpuMonitoring.GetVramUsageAsync();

                return new GpuPerformanceStats
                {
                    AverageUtilization = gpuStatus.UtilizationPercent,
                    PeakUtilization = gpuStatus.UtilizationPercent, // Would track over time in real implementation
                    VramUtilization = vramUsage.UsagePercent,
                    AverageInferenceTime = TimeSpan.FromMilliseconds(200), // Placeholder
                    Temperature = new TemperatureStats
                    {
                        Current = gpuStatus.TemperatureCelsius,
                        Average = gpuStatus.TemperatureCelsius,
                        Maximum = gpuStatus.TemperatureCelsius
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current GPU performance stats");
                return new GpuPerformanceStats();
            }
        }

        private async Task<Dictionary<string, float>> GetCacheHitRatiosAsync()
        {
            // Placeholder for cache hit ratio calculation
            return await Task.FromResult(new Dictionary<string, float>
            {
                ["ResponseCache"] = 0.75f,
                ["ModelCache"] = 0.90f,
                ["EmbeddingCache"] = 0.85f
            });
        }

        private async Task<long> GetCacheMemoryUsageAsync()
        {
            // Placeholder for cache memory usage calculation
            return await Task.FromResult(100L * 1024 * 1024); // 100MB estimate
        }

        private async Task<int> GetCurrentConcurrentRequestsAsync()
        {
            // Placeholder for concurrent request count
            return await Task.FromResult(5);
        }

        private async Task<int> GetCurrentQueueDepthAsync()
        {
            // Placeholder for queue depth
            return await Task.FromResult(2);
        }

        private async Task<PerformanceSummary> GeneratePerformanceSummaryAsync(Dictionary<RequestType, List<ResponseTimeEntry>> filteredEntries)
        {
            var totalRequests = filteredEntries.Values.Sum(entries => entries.Count);
            var allDurations = filteredEntries.Values.SelectMany(entries => entries.Select(e => e.Duration));

            return await Task.FromResult(new PerformanceSummary
            {
                HealthScore = CalculateHealthScore(filteredEntries),
                TotalRequests = totalRequests,
                AverageResponseTime = allDurations.Any() ? 
                    TimeSpan.FromMilliseconds(allDurations.Average(d => d.TotalMilliseconds)) : 
                    TimeSpan.Zero,
                ErrorRate = 0.05f, // Placeholder
                ResourceUtilization = new ResourceUtilizationSummary
                {
                    AverageCpuUtilization = 45.0f,
                    AverageMemoryUtilization = 60.0f,
                    AverageGpuUtilization = 70.0f,
                    AverageVramUtilization = 55.0f
                }
            });
        }

        private async Task<DetailedMetrics> GenerateDetailedMetricsAsync(List<ResponseTimeEntry> entries)
        {
            var durations = entries.Select(e => e.Duration).OrderBy(d => d).ToList();
            
            return await Task.FromResult(new DetailedMetrics
            {
                ResponseTimes = new ResponseTimeStats
                {
                    Average = TimeSpan.FromMilliseconds(durations.Average(d => d.TotalMilliseconds)),
                    Median = durations[durations.Count / 2],
                    P95 = durations[(int)(durations.Count * 0.95)],
                    P99 = durations[(int)(durations.Count * 0.99)],
                    Min = durations.First(),
                    Max = durations.Last(),
                    SampleCount = durations.Count
                },
                Throughput = new ThroughputMetrics
                {
                    RequestsPerSecond = entries.Count / 3600.0f, // Assuming 1 hour window
                    Period = TimeSpan.FromHours(1)
                },
                ResourceConsumption = new ResourceConsumption
                {
                    AverageCpuUsage = 45.0f,
                    AverageMemoryAllocation = 100L * 1024 * 1024,
                    AverageGpuUsage = 70.0f
                }
            });
        }

        private async Task<ModelPerformanceAnalysis> GenerateModelPerformanceAnalysisAsync(DateTime startTime, DateTime endTime)
        {
            var analysis = new ModelPerformanceAnalysis();

            // Generate model switching stats
            var relevantSwitches = _modelSwitchData.Values
                .Where(s => s.Timestamp >= startTime && s.Timestamp <= endTime)
                .ToList();

            analysis.SwitchingStats = new ModelSwitchingStats
            {
                TotalSwitches = relevantSwitches.Count,
                AverageSwitchTime = relevantSwitches.Any() ? 
                    TimeSpan.FromMilliseconds(relevantSwitches.Average(s => s.SwitchDuration.TotalMilliseconds)) : 
                    TimeSpan.Zero,
                SwitchesPerHour = (float)(relevantSwitches.Count / Math.Max((endTime - startTime).TotalHours, 1.0)),
                SwitchPatterns = relevantSwitches
                    .GroupBy(s => $"{s.FromModel}->{s.ToModel}")
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return await Task.FromResult(analysis);
        }

        private async Task<List<PerformanceIssue>> IdentifyPerformanceIssuesAsync(Dictionary<RequestType, List<ResponseTimeEntry>> filteredEntries)
        {
            var issues = new List<PerformanceIssue>();

            foreach (var kvp in filteredEntries)
            {
                var requestType = kvp.Key;
                var entries = kvp.Value;

                if (!entries.Any()) continue;

                var averageTime = entries.Average(e => e.Duration.TotalMilliseconds);
                var threshold = _performanceThresholds[requestType].TotalMilliseconds;

                if (averageTime > threshold * 1.2) // 20% above threshold
                {
                    issues.Add(new PerformanceIssue
                    {
                        Type = IssueType.HighLatency,
                        Severity = averageTime > threshold * 2 ? SeverityLevel.High : SeverityLevel.Medium,
                        Description = $"{requestType} requests averaging {averageTime:F0}ms, {((averageTime / threshold - 1) * 100):F0}% above threshold",
                        DetectedAt = DateTime.UtcNow,
                        RelatedMetrics = new Dictionary<string, object>
                        {
                            ["RequestType"] = requestType.ToString(),
                            ["AverageTime"] = averageTime,
                            ["Threshold"] = threshold,
                            ["SampleCount"] = entries.Count
                        }
                    });
                }
            }

            return await Task.FromResult(issues);
        }

        private async Task<List<PerformanceRecommendation>> GeneratePerformanceRecommendationsAsync(PerformanceReport report)
        {
            var recommendations = new List<PerformanceRecommendation>();

            // Analyze issues and generate recommendations
            foreach (var issue in report.Issues)
            {
                switch (issue.Type)
                {
                    case IssueType.HighLatency:
                        recommendations.Add(new PerformanceRecommendation
                        {
                            Title = "Reduce Response Latency",
                            Description = $"High latency detected for {issue.RelatedMetrics.GetValueOrDefault("RequestType", "unknown")} requests",
                            ImplementationSteps = {
                                "Enable model quantization to reduce processing time",
                                "Implement request batching for similar operations",
                                "Pre-warm frequently used models",
                                "Optimize model switching strategy"
                            },
                            ExpectedImpact = ImpactLevel.High,
                            ImplementationEffort = EffortLevel.Medium,
                            Priority = PriorityLevel.High
                        });
                        break;
                }
            }

            return await Task.FromResult(recommendations);
        }

        private int CalculateHealthScore(Dictionary<RequestType, List<ResponseTimeEntry>> filteredEntries)
        {
            if (!filteredEntries.Any()) return 100;

            var score = 100;

            foreach (var kvp in filteredEntries)
            {
                var requestType = kvp.Key;
                var entries = kvp.Value;

                if (!entries.Any()) continue;

                var averageTime = entries.Average(e => e.Duration.TotalMilliseconds);
                var threshold = _performanceThresholds[requestType].TotalMilliseconds;

                if (averageTime > threshold)
                {
                    var penalty = Math.Min(30, (int)((averageTime / threshold - 1) * 20));
                    score -= penalty;
                }
            }

            return Math.Max(0, score);
        }

        private List<string> GetBottleneckSolutions(RequestType requestType)
        {
            return requestType switch
            {
                RequestType.CodeCompletion => new List<string>
                {
                    "Enable aggressive caching for code patterns",
                    "Use smaller, specialized code models",
                    "Implement prefix matching optimization"
                },
                RequestType.ChatMessage => new List<string>
                {
                    "Enable response streaming",
                    "Optimize context window size",
                    "Use balanced model quantization"
                },
                RequestType.ModelLoading => new List<string>
                {
                    "Enable model pre-warming",
                    "Use 4-bit quantization",
                    "Implement lazy loading strategy"
                },
                _ => new List<string> { "Optimize processing pipeline", "Enable caching", "Use hardware acceleration" }
            };
        }

        private async Task DetectMemoryBottlenecksAsync(List<PerformanceBottleneck> bottlenecks)
        {
            var snapshots = _memorySnapshots.ToList();
            if (snapshots.Count < 10) return;

            var recentSnapshots = snapshots.TakeLast(20).ToList();
            var averageMemory = recentSnapshots.Average(s => s.ApplicationMemoryBytes);
            var peakMemory = recentSnapshots.Max(s => s.ApplicationMemoryBytes);

            // Check for memory pressure (>80% of available system memory)
            var totalSystemMemory = 16L * 1024 * 1024 * 1024; // Assume 16GB system
            if (averageMemory > totalSystemMemory * 0.8)
            {
                bottlenecks.Add(new PerformanceBottleneck
                {
                    Type = BottleneckType.Memory,
                    Severity = SeverityLevel.High,
                    Description = $"High memory usage detected: {averageMemory / (1024 * 1024 * 1024):F1}GB average",
                    AffectedComponents = { "Memory Management" },
                    PotentialSolutions = {
                        "Enable aggressive model unloading",
                        "Reduce model cache size",
                        "Enable memory compression"
                    }
                });
            }

            await Task.CompletedTask;
        }

        private async Task DetectGpuBottlenecksAsync(List<PerformanceBottleneck> bottlenecks)
        {
            try
            {
                var gpuStatus = await _gpuMonitoring.GetGpuStatusAsync();
                var vramUsage = await _gpuMonitoring.GetVramUsageAsync();

                // Check for VRAM pressure
                if (vramUsage.UsagePercent > 85)
                {
                    bottlenecks.Add(new PerformanceBottleneck
                    {
                        Type = BottleneckType.Gpu,
                        Severity = SeverityLevel.High,
                        Description = $"High VRAM usage: {vramUsage.UsagePercent:F1}%",
                        AffectedComponents = { "GPU Memory" },
                        PotentialSolutions = {
                            "Enable 4-bit model quantization",
                            "Unload unused models",
                            "Reduce batch size"
                        }
                    });
                }

                // Check for low GPU utilization
                if (gpuStatus.UtilizationPercent < 30 && vramUsage.UsagePercent > 50)
                {
                    bottlenecks.Add(new PerformanceBottleneck
                    {
                        Type = BottleneckType.Gpu,
                        Severity = SeverityLevel.Medium,
                        Description = $"Low GPU utilization ({gpuStatus.UtilizationPercent:F1}%) with high VRAM usage",
                        AffectedComponents = { "GPU Compute" },
                        PotentialSolutions = {
                            "Increase batch size",
                            "Enable more GPU layers",
                            "Optimize model parallelization"
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting GPU bottlenecks");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            _memoryMonitorTimer?.Dispose();
            _cpuCounter?.Dispose();
            _currentProcess?.Dispose();

            _logger.LogInformation("Performance monitoring service disposed");
        }

        private class ResponseTimeEntry
        {
            public RequestType RequestType { get; set; }
            public TimeSpan Duration { get; set; }
            public DateTime Timestamp { get; set; }
            public Dictionary<string, object> Metadata { get; set; } = new();
        }

        private class ModelSwitchEntry
        {
            public string FromModel { get; set; } = string.Empty;
            public string ToModel { get; set; } = string.Empty;
            public TimeSpan SwitchDuration { get; set; }
            public DateTime Timestamp { get; set; }
            public int SwitchCount { get; set; } = 1;
            public TimeSpan TotalSwitchTime { get; set; }
            public TimeSpan AverageSwitchTime { get; set; }

            public ModelSwitchEntry()
            {
                TotalSwitchTime = SwitchDuration;
                AverageSwitchTime = SwitchDuration;
            }
        }
    }
}
