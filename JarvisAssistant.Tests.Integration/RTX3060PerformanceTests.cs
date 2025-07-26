using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Console;
using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models;
using JarvisAssistant.Services;
using System.Diagnostics;

namespace JarvisAssistant.Tests.Integration
{
    /// <summary>
    /// Comprehensive performance optimization tests for RTX 3060 12GB VRAM.
    /// </summary>
    public class RTX3060PerformanceTests
    {
        private ServiceProvider? _serviceProvider;
        private IGpuMonitoringService? _gpuMonitoring;
        private IModelOptimizationService? _modelOptimization;
        private IPerformanceMonitoringService? _performanceMonitoring;
        private IRequestOptimizationService? _requestOptimization;

        private const int CODE_COMPLETION_TARGET_MS = 500;
        private const int CHAT_RESPONSE_TARGET_MS = 2000;
        private const int VRAM_THRESHOLD_PERCENT = 85;
        private const int MAX_VRAM_GB = 12;

        [SetUp]
        public async Task SetUp()
        {
            var services = new ServiceCollection();
            
            // Add logging
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            
            // Add our services
            services.AddSingleton<IGpuMonitoringService, GpuMonitoringService>();
            services.AddSingleton<IModelOptimizationService, ModelOptimizationService>();
            services.AddSingleton<IPerformanceMonitoringService, PerformanceMonitoringService>();
            services.AddSingleton<IRequestOptimizationService, RequestOptimizationService>();

            _serviceProvider = services.BuildServiceProvider();
            
            _gpuMonitoring = _serviceProvider.GetRequiredService<IGpuMonitoringService>();
            _modelOptimization = _serviceProvider.GetRequiredService<IModelOptimizationService>();
            _performanceMonitoring = _serviceProvider.GetRequiredService<IPerformanceMonitoringService>();
            _requestOptimization = _serviceProvider.GetRequiredService<IRequestOptimizationService>();

            await Task.CompletedTask;
        }

        [TearDown]
        public async Task TearDown()
        {
            _serviceProvider?.Dispose();
            await Task.CompletedTask;
        }

        #region Memory Leak Detection Tests

        [Test]
        public async Task Test_MemoryLeakDetection_LongRunningOperations()
        {
            // Arrange
            var initialMemory = GC.GetTotalMemory(true);
            var iterations = 100;
            
            // Act - Simulate long-running operations
            for (int i = 0; i < iterations; i++)
            {
                var request = CreateTestChatRequest($"Test request {i}");
                var context = CreateTestOptimizationContext();
                
                var optimizedRequest = await _requestOptimization!.OptimizeRequestAsync(request, context);
                var response = CreateTestChatResponse($"Response {i}");
                
                await _requestOptimization.CacheResponseAsync(request, response, 
                    new ResponseCacheSettings { TimeToLive = TimeSpan.FromMinutes(1) });
                
                // Force garbage collection every 10 iterations
                if (i % 10 == 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }
            }
            
            // Force final garbage collection
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var finalMemory = GC.GetTotalMemory(false);
            var memoryIncrease = finalMemory - initialMemory;
            
            // Assert - Memory increase should be reasonable (less than 50MB for 100 operations)
            Assert.That(memoryIncrease < 50 * 1024 * 1024, 
                $"Memory leak detected: {memoryIncrease / (1024 * 1024)}MB increase after {iterations} operations");
        }

        [Test]
        public async Task Test_MemoryLeakDetection_CacheCleanup()
        {
            // Arrange
            var cacheSettings = new ResponseCacheSettings 
            { 
                TimeToLive = TimeSpan.FromMilliseconds(100), // Very short TTL
                MaxCacheSizeBytes = 1024 * 1024 // 1MB limit
            };
            
            var initialMemory = GC.GetTotalMemory(true);
            
            // Act - Fill cache beyond limit
            for (int i = 0; i < 1000; i++)
            {
                var request = CreateTestChatRequest($"Cache test {i}");
                var response = CreateTestChatResponse(new string('X', 1024)); // 1KB response
                
                await _requestOptimization!.CacheResponseAsync(request, response, cacheSettings);
            }
            
            // Wait for cache cleanup
            await Task.Delay(500);
            
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            var finalMemory = GC.GetTotalMemory(false);
            var memoryIncrease = finalMemory - initialMemory;
            
            // Assert - Memory should not increase significantly due to cache cleanup
            Assert.That(memoryIncrease < 10 * 1024 * 1024, 
                $"Cache cleanup failed: {memoryIncrease / (1024 * 1024)}MB memory not cleaned up");
        }

        #endregion

        #region Concurrent Request Handling Tests

        [Test]
        public async Task Test_ConcurrentRequestHandling_MaxConcurrency()
        {
            // Arrange
            var maxConcurrency = 8;
            var requestCount = 50;
            var requests = new List<OptimizedRequest>();
            
            for (int i = 0; i < requestCount; i++)
            {
                var chatRequest = CreateTestChatRequest($"Concurrent request {i}");
                var context = CreateTestOptimizationContext();
                var optimizedRequest = await _requestOptimization!.OptimizeRequestAsync(chatRequest, context);
                requests.Add(optimizedRequest);
            }
            
            var stopwatch = Stopwatch.StartNew();
            
            // Act
            var result = await _requestOptimization!.ProcessInParallelAsync(requests, maxConcurrency);
            
            stopwatch.Stop();
            
            // Assert
            Assert.That(result.TotalRequests, Is.EqualTo(requestCount));
            Assert.That(result.SuccessfulRequests, Is.GreaterThanOrEqualTo(requestCount * 0.95), 
                $"Too many failed requests: {result.FailedRequests}/{requestCount}");
            Assert.That(result.MaxConcurrentOperations, Is.EqualTo(maxConcurrency));
            Assert.That(result.ParallelizationEfficiency, Is.GreaterThan(50), 
                $"Poor parallelization efficiency: {result.ParallelizationEfficiency}%");
            
            // Performance should be better than sequential processing
            var estimatedSequentialTime = requestCount * 1000; // 1 second per request
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(estimatedSequentialTime * 0.5), 
                $"Parallel processing not effective: {stopwatch.ElapsedMilliseconds}ms vs estimated {estimatedSequentialTime}ms");
        }

        [Test]
        public async Task Test_ConcurrentRequestHandling_ResourceContention()
        {
            // Arrange
            var tasks = new List<Task<bool>>();
            var concurrentRequests = 20;
            
            // Act - Create multiple concurrent tasks that access shared resources
            for (int i = 0; i < concurrentRequests; i++)
            {
                var taskId = i;
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var request = CreateTestChatRequest($"Resource contention test {taskId}");
                        var context = CreateTestOptimizationContext();
                        
                        var optimizedRequest = await _requestOptimization!.OptimizeRequestAsync(request, context);
                        var response = CreateTestChatResponse($"Response {taskId}");
                        
                        await _requestOptimization.CacheResponseAsync(request, response, 
                            new ResponseCacheSettings { TimeToLive = TimeSpan.FromMinutes(1) });
                        
                        var cachedResponse = await _requestOptimization.GetCachedResponseAsync(request);
                        
                        return cachedResponse != null;
                    }
                    catch
                    {
                        return false;
                    }
                }));
            }
            
            var results = await Task.WhenAll(tasks);
            
            // Assert - All tasks should complete successfully without resource contention issues
            var successCount = results.Count(r => r);
            Assert.That(successCount, Is.GreaterThanOrEqualTo(concurrentRequests * 0.95), 
                $"Resource contention detected: only {successCount}/{concurrentRequests} requests succeeded");
        }

        #endregion

        #region Cache Effectiveness Tests

        [Test]
        public async Task Test_CacheEffectiveness_HitRate()
        {
            // Arrange
            var cacheSettings = new ResponseCacheSettings 
            { 
                TimeToLive = TimeSpan.FromMinutes(5),
                CompressionLevel = CompressionLevel.Balanced
            };
            
            var uniqueRequests = new List<ChatRequest>();
            for (int i = 0; i < 10; i++)
            {
                uniqueRequests.Add(CreateTestChatRequest($"Unique request {i}"));
            }
            
            // Act - Cache all unique requests
            foreach (var request in uniqueRequests)
            {
                var response = CreateTestChatResponse($"Response for {request.Message}");
                await _requestOptimization!.CacheResponseAsync(request, response, cacheSettings);
            }
            
            // Test cache hits
            int cacheHits = 0;
            int totalRequests = 0;
            
            for (int i = 0; i < 100; i++)
            {
                var randomRequest = uniqueRequests[i % uniqueRequests.Count];
                var cachedResponse = await _requestOptimization!.GetCachedResponseAsync(randomRequest);
                
                totalRequests++;
                if (cachedResponse != null)
                {
                    cacheHits++;
                }
            }
            
            var hitRate = (float)cacheHits / totalRequests * 100;
            
            // Assert - Cache hit rate should be very high for repeated requests
            Assert.That(hitRate, Is.GreaterThanOrEqualTo(95), $"Poor cache hit rate: {hitRate}%");
        }

        [Test]
        public async Task Test_CacheEffectiveness_CompressionRatio()
        {
            // Arrange
            var largeContent = new string('A', 10000); // 10KB of repeated content
            var request = CreateTestChatRequest("Large content test");
            var response = CreateTestChatResponse(largeContent);
            
            var uncompressedSettings = new ResponseCacheSettings 
            { 
                CompressionLevel = CompressionLevel.None,
                TimeToLive = TimeSpan.FromMinutes(1)
            };
            
            var compressedSettings = new ResponseCacheSettings 
            { 
                CompressionLevel = CompressionLevel.Balanced,
                TimeToLive = TimeSpan.FromMinutes(1)
            };
            
            // Act
            var originalData = System.Text.Encoding.UTF8.GetBytes(largeContent);
            var compressedData = await _requestOptimization!.CompressDataAsync(originalData);
            
            var compressionRatio = (float)compressedData.CompressedSizeBytes / compressedData.OriginalSizeBytes;
            
            // Assert - Compression should be effective for repetitive content
            Assert.That(compressionRatio, Is.LessThan(0.1), 
                $"Poor compression ratio: {compressionRatio:F2} (compressed: {compressedData.CompressedSizeBytes}, original: {compressedData.OriginalSizeBytes})");
        }

        #endregion

        #region Model Switching Speed Tests

        [Test]
        public async Task Test_ModelSwitchingSpeed_ColdStart()
        {
            // Arrange
            var model1 = new ModelInfo 
            { 
                Name = "test-model-1", 
                SizeBytes = 1024 * 1024 * 1024, // 1GB
                RequiredVramBytes = 2L * 1024 * 1024 * 1024 // 2GB
            };
            
            var model2 = new ModelInfo 
            { 
                Name = "test-model-2", 
                SizeBytes = 2L * 1024 * 1024 * 1024, // 2GB
                RequiredVramBytes = 3L * 1024 * 1024 * 1024 // 3GB
            };
            
            var loadSettings = new ModelLoadSettings
            {
                EnableCaching = true,
                UseMemoryMapping = true,
                PreloadLayers = true
            };
            
            // Act
            var stopwatch = Stopwatch.StartNew();
            
            var loadResult1 = await _modelOptimization!.LoadModelAsync(model1, loadSettings);
            var switchTime1 = stopwatch.ElapsedMilliseconds;
            
            stopwatch.Restart();
            var loadResult2 = await _modelOptimization!.LoadModelAsync(model2, loadSettings);
            var switchTime2 = stopwatch.ElapsedMilliseconds;
            
            stopwatch.Stop();
            
            // Assert
            Assert.That(loadResult1.Success, Is.True, $"Model 1 load failed: {loadResult1.ErrorMessage}");
            Assert.That(loadResult2.Success, Is.True, $"Model 2 load failed: {loadResult2.ErrorMessage}");
            
            // Cold start should be reasonable (under 30 seconds for test models)
            Assert.That(switchTime1, Is.LessThan(30000), $"Model 1 cold start too slow: {switchTime1}ms");
            Assert.That(switchTime2, Is.LessThan(30000), $"Model 2 cold start too slow: {switchTime2}ms");
        }

        [Test]
        public async Task Test_ModelSwitchingSpeed_WarmStart()
        {
            // Arrange
            var model = new ModelInfo 
            { 
                Name = "test-model-warm", 
                SizeBytes = 1024 * 1024 * 1024, // 1GB
                RequiredVramBytes = 2L * 1024 * 1024 * 1024 // 2GB
            };
            
            var loadSettings = new ModelLoadSettings
            {
                EnableCaching = true,
                UseMemoryMapping = true,
                PreloadLayers = true
            };
            
            // Pre-load model (cold start)
            await _modelOptimization!.LoadModelAsync(model, loadSettings);
            await _modelOptimization.UnloadModelAsync(model.Name);
            
            // Act - Warm start
            var stopwatch = Stopwatch.StartNew();
            var loadResult = await _modelOptimization.LoadModelAsync(model, loadSettings);
            stopwatch.Stop();
            
            // Assert
            Assert.That(loadResult.Success, Is.True, $"Warm start failed: {loadResult.ErrorMessage}");
            
            // Warm start should be much faster (under 5 seconds)
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(5000), 
                $"Model warm start too slow: {stopwatch.ElapsedMilliseconds}ms");
        }

        #endregion

        #region Performance Target Tests

        [Test]
        public async Task Test_PerformanceTargets_CodeCompletion()
        {
            // Arrange
            var codeCompletionRequest = CreateTestChatRequest("def fibonacci(n):");
            var context = CreateTestOptimizationContext();
            context.UserPreferences.QualitySpeedBalance = QualitySpeedPreference.MaxSpeed;
            
            var stopwatch = Stopwatch.StartNew();
            
            // Act
            var optimizedRequest = await _requestOptimization!.OptimizeRequestAsync(codeCompletionRequest, context);
            
            // Simulate code completion processing
            await Task.Delay((int)optimizedRequest.EstimatedProcessingTime.TotalMilliseconds);
            
            stopwatch.Stop();
            
            // Assert
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThanOrEqualTo(CODE_COMPLETION_TARGET_MS), 
                $"Code completion too slow: {stopwatch.ElapsedMilliseconds}ms > {CODE_COMPLETION_TARGET_MS}ms target");
        }

        [Test]
        public async Task Test_PerformanceTargets_ChatResponse()
        {
            // Arrange
            var chatRequest = CreateTestChatRequest("Explain the concept of machine learning in detail with examples.");
            var context = CreateTestOptimizationContext();
            context.UserPreferences.QualitySpeedBalance = QualitySpeedPreference.Balanced;
            
            var stopwatch = Stopwatch.StartNew();
            
            // Act
            var optimizedRequest = await _requestOptimization!.OptimizeRequestAsync(chatRequest, context);
            
            // Simulate chat response processing
            await Task.Delay((int)optimizedRequest.EstimatedProcessingTime.TotalMilliseconds);
            
            stopwatch.Stop();
            
            // Assert
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThanOrEqualTo(CHAT_RESPONSE_TARGET_MS), 
                $"Chat response too slow: {stopwatch.ElapsedMilliseconds}ms > {CHAT_RESPONSE_TARGET_MS}ms target");
        }

        [Test]
        public async Task Test_PerformanceTargets_VramUsage()
        {
            // Arrange
            var gpuStatus = await _gpuMonitoring!.GetCurrentGpuStatusAsync();
            
            if (gpuStatus == null)
            {
                // Skip test if GPU monitoring is not available
                Assert.Ignore("GPU monitoring not available");
                return;
            }
            
            // Act & Assert
            var vramUsagePercent = (float)gpuStatus.VramUsage.UsedBytes / gpuStatus.VramUsage.TotalBytes * 100;
            
            Assert.That(vramUsagePercent, Is.LessThanOrEqualTo(VRAM_THRESHOLD_PERCENT), 
                $"VRAM usage too high: {vramUsagePercent:F1}% > {VRAM_THRESHOLD_PERCENT}% threshold");
            
            Assert.That(gpuStatus.VramUsage.TotalBytes, Is.GreaterThanOrEqualTo((long)MAX_VRAM_GB * 1024 * 1024 * 1024 * 0.9), 
                $"Expected ~{MAX_VRAM_GB}GB VRAM, found {gpuStatus.VramUsage.TotalBytes / (1024.0 * 1024 * 1024):F1}GB");
        }

        #endregion

        #region Embedding Optimization Tests

        [Test]
        public async Task Test_EmbeddingOptimization_BatchProcessing()
        {
            // Arrange
            var texts = new List<string>();
            for (int i = 0; i < 100; i++)
            {
                texts.Add($"This is test text number {i} for embedding generation.");
            }
            
            var batchSize = 10;
            var stopwatch = Stopwatch.StartNew();
            
            // Act
            var result = await _requestOptimization!.OptimizeEmbeddingGenerationAsync(texts, batchSize);
            
            stopwatch.Stop();
            
            // Assert
            Assert.That(result.Embeddings.Count == texts.Count || result.Embeddings.Count >= texts.Count * 0.95, 
                $"Embedding generation failed for some texts: {result.Embeddings.Count}/{texts.Count}");
            
            Assert.That(result.EmbeddingsPerSecond, Is.GreaterThan(1), 
                $"Embedding generation too slow: {result.EmbeddingsPerSecond:F2} embeddings/second");
            
            Assert.That(result.BatchEfficiency, Is.GreaterThan(0), 
                $"No batching efficiency gained: {result.BatchEfficiency:F1}%");
            
            Assert.That(result.Errors.Count, Is.LessThanOrEqualTo(texts.Count * 0.05), 
                $"Too many embedding errors: {result.Errors.Count}");
        }

        #endregion

        #region GPU Monitoring Tests

        [Test]
        public async Task Test_GpuMonitoring_RealTimeMetrics()
        {
            // Arrange & Act
            var gpuStatus = await _gpuMonitoring!.GetCurrentGpuStatusAsync();
            
            if (gpuStatus == null)
            {
                // Skip test if GPU monitoring is not available
                Assert.Ignore("GPU monitoring not available");
                return;
            }
            
            // Assert
            Assert.That(gpuStatus.IsAvailable, Is.True, "GPU should be available");
            Assert.That(gpuStatus.VramUsage.TotalBytes, Is.GreaterThan(0), "VRAM total should be greater than 0");
            Assert.That(gpuStatus.Temperature, Is.InRange(0, 100), 
                $"Invalid GPU temperature: {gpuStatus.Temperature}°C");
            Assert.That(gpuStatus.PowerUsageWatts, Is.InRange(0f, 500f), 
                $"Invalid power usage: {gpuStatus.PowerUsageWatts}W");
        }

        [Test]
        public async Task Test_GpuMonitoring_PerformanceMetrics()
        {
            // Arrange
            var monitoringDuration = TimeSpan.FromSeconds(5);
            var metrics = new List<GpuStatus>();
            
            // Act
            var stopwatch = Stopwatch.StartNew();
            while (stopwatch.Elapsed < monitoringDuration)
            {
                var status = await _gpuMonitoring!.GetCurrentGpuStatusAsync();
                if (status != null)
                {
                    metrics.Add(status);
                }
                await Task.Delay(100); // Sample every 100ms
            }
            
            // Assert
            Assert.That(metrics.Count, Is.GreaterThanOrEqualTo(10), $"Insufficient GPU metrics collected: {metrics.Count}");
            
            var avgUtilization = metrics.Average(m => m.UtilizationPercent);
            var maxTemperature = metrics.Max(m => m.Temperature);
            var avgPowerUsage = metrics.Average(m => m.PowerUsageWatts);
            
            Assert.That(avgUtilization, Is.InRange(0f, 100f), 
                $"Invalid average GPU utilization: {avgUtilization:F1}%");
            Assert.That(maxTemperature, Is.InRange(0, 100), 
                $"Invalid maximum GPU temperature: {maxTemperature}°C");
            Assert.That(avgPowerUsage, Is.GreaterThanOrEqualTo(0), $"Invalid average power usage: {avgPowerUsage:F1}W");
        }

        #endregion

        #region Helper Methods

        private ChatRequest CreateTestChatRequest(string message)
        {
            return new ChatRequest(message)
            {
                ConversationId = Guid.NewGuid().ToString()
            };
        }

        private ChatResponse CreateTestChatResponse(string message)
        {
            return new ChatResponse(message, "test-model")
            {
                Timestamp = DateTimeOffset.UtcNow
            };
        }

        private OptimizationContext CreateTestOptimizationContext()
        {
            return new OptimizationContext
            {
                SystemMetrics = new SystemPerformanceMetrics
                {
                    CpuUtilization = 50,
                    MemoryUtilization = 60,
                    VramUtilization = 70
                },
                UserPreferences = new UserPerformancePreferences
                {
                    QualitySpeedBalance = QualitySpeedPreference.Balanced
                },
                QueueStatus = new QueueStatus
                {
                    QueueDepth = 2,
                    AverageWaitTime = TimeSpan.FromMilliseconds(100)
                }
            };
        }

        #endregion
    }
}
