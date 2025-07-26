using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

// Use alias to distinguish between System.IO.Compression.CompressionLevel and our custom CompressionLevel
using SystemCompressionLevel = System.IO.Compression.CompressionLevel;
using CoreCompressionLevel = JarvisAssistant.Core.Models.CompressionLevel;

namespace JarvisAssistant.Services
{
    /// <summary>
    /// Request optimization service for intelligent batching, caching, and performance optimization.
    /// </summary>
    public class RequestOptimizationService : IRequestOptimizationService, IDisposable
    {
        private readonly ILogger<RequestOptimizationService> _logger;
        private readonly IPerformanceMonitoringService _performanceMonitoring;
        private readonly IGpuMonitoringService _gpuMonitoring;
        private readonly ConcurrentDictionary<string, CachedResponse> _responseCache;
        private readonly ConcurrentDictionary<string, BatchGroup> _batchGroups;
        private readonly Timer? _cacheCleanupTimer;
        private readonly Timer? _batchProcessingTimer;
        private readonly SemaphoreSlim _batchingSemaphore;
        private bool _disposed;

        private const int MAX_CACHE_SIZE_MB = 100;
        private const int MAX_BATCH_SIZE = 10;
        private const int BATCH_TIMEOUT_MS = 100;
        private const int CACHE_CLEANUP_INTERVAL_MS = 60000; // 1 minute
        private const int PARALLEL_REQUEST_LIMIT = 8;

        public RequestOptimizationService(
            ILogger<RequestOptimizationService> logger,
            IPerformanceMonitoringService performanceMonitoring,
            IGpuMonitoringService gpuMonitoring)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _performanceMonitoring = performanceMonitoring ?? throw new ArgumentNullException(nameof(performanceMonitoring));
            _gpuMonitoring = gpuMonitoring ?? throw new ArgumentNullException(nameof(gpuMonitoring));
            _responseCache = new ConcurrentDictionary<string, CachedResponse>();
            _batchGroups = new ConcurrentDictionary<string, BatchGroup>();
            _batchingSemaphore = new SemaphoreSlim(1, 1);

            // Start background tasks
            _cacheCleanupTimer = new Timer(CleanupExpiredCache, null, CACHE_CLEANUP_INTERVAL_MS, CACHE_CLEANUP_INTERVAL_MS);
            _batchProcessingTimer = new Timer(ProcessPendingBatches, null, BATCH_TIMEOUT_MS, BATCH_TIMEOUT_MS);

            _logger.LogInformation("Request optimization service initialized with {MaxCacheSizeMB}MB cache and batch size {MaxBatchSize}", 
                MAX_CACHE_SIZE_MB, MAX_BATCH_SIZE);
        }

        /// <inheritdoc/>
        public async Task<BatchProcessingResult> BatchRequestsAsync(IEnumerable<OptimizedRequest> requests, CancellationToken cancellationToken = default)
        {
            try
            {
                var requestList = requests.ToList();
                _logger.LogInformation("Processing batch of {RequestCount} requests", requestList.Count);

                var stopwatch = Stopwatch.StartNew();
                var result = new BatchProcessingResult
                {
                    TotalRequests = requestList.Count
                };

                // Group requests by similarity for batching
                var batchGroups = GroupRequestsForBatching(requestList);
                var allResults = new List<IndividualRequestResult>();

                // Process each batch group
                foreach (var group in batchGroups)
                {
                    var groupResults = await ProcessBatchGroupAsync(group, cancellationToken);
                    allResults.AddRange(groupResults);
                }

                stopwatch.Stop();

                // Calculate results
                result.RequestResults = allResults;
                result.SuccessfulRequests = allResults.Count(r => r.Success);
                result.FailedRequests = allResults.Count(r => !r.Success);
                result.TotalProcessingTime = stopwatch.Elapsed;
                result.AverageProcessingTime = TimeSpan.FromMilliseconds(
                    allResults.Average(r => r.ProcessingTime.TotalMilliseconds));

                // Calculate efficiency gain (estimate based on batching)
                var estimatedIndividualTime = requestList.Count * 1000; // 1 second per request estimate
                result.EfficiencyGain = Math.Max(0, 
                    (float)(estimatedIndividualTime - stopwatch.Elapsed.TotalMilliseconds) / estimatedIndividualTime * 100);

                _logger.LogInformation("Batch processing completed: {SuccessfulRequests}/{TotalRequests} successful in {TotalTime}ms, {EfficiencyGain:F1}% efficiency gain",
                    result.SuccessfulRequests, result.TotalRequests, result.TotalProcessingTime.TotalMilliseconds, result.EfficiencyGain);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing batch requests");
                return new BatchProcessingResult
                {
                    TotalRequests = requests.Count(),
                    FailedRequests = requests.Count()
                };
            }
        }

        /// <inheritdoc/>
        public async Task<OptimizedRequest> OptimizeRequestAsync(ChatRequest request, OptimizationContext context)
        {
            try
            {
                _logger.LogDebug("Optimizing request for conversation {ConversationId}", request.ConversationId);

                var optimizedRequest = new OptimizedRequest
                {
                    OriginalRequest = request,
                    OptimizationSettings = await CreateOptimizationSettingsAsync(request, context),
                    Priority = DeterminePriority(request, context),
                    CanBatch = CanRequestBeBatched(request),
                    BatchGroup = DetermineBatchGroup(request)
                };

                // Generate cache key
                optimizedRequest.CacheKey = await GenerateCacheKeyAsync(request, optimizedRequest.OptimizationSettings);

                // Estimate processing time based on request type and context
                optimizedRequest.EstimatedProcessingTime = EstimateProcessingTime(request, context);

                // Add optimization metadata
                optimizedRequest.OptimizationMetadata = new Dictionary<string, object>
                {
                    ["OptimizationLevel"] = DetermineOptimizationLevel(context),
                    ["SystemLoad"] = context.SystemMetrics.CpuUtilization,
                    ["VramUsage"] = context.SystemMetrics.VramUtilization,
                    ["QueueDepth"] = context.QueueStatus.QueueDepth,
                    ["OptimizedAt"] = DateTime.UtcNow
                };

                _logger.LogDebug("Request optimized: Priority={Priority}, CanBatch={CanBatch}, EstimatedTime={EstimatedTime}ms",
                    optimizedRequest.Priority, optimizedRequest.CanBatch, optimizedRequest.EstimatedProcessingTime.TotalMilliseconds);

                return optimizedRequest;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error optimizing request");
                return new OptimizedRequest { OriginalRequest = request };
            }
        }

        /// <inheritdoc/>
        public async Task CacheResponseAsync(ChatRequest request, ChatResponse response, ResponseCacheSettings cacheSettings)
        {
            try
            {
                var cacheKey = await GenerateCacheKeyAsync(request, null);
                
                var cachedResponse = new CachedResponse
                {
                    Response = response,
                    CachedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.Add(cacheSettings.TimeToLive),
                    CompressionLevel = cacheSettings.CompressionLevel,
                    AccessCount = 0,
                    LastAccessedAt = DateTime.UtcNow
                };

                // Compress response if configured
                if (cacheSettings.CompressionLevel != CoreCompressionLevel.None)
                {
                    cachedResponse.CompressedData = await CompressResponseAsync(response, cacheSettings.CompressionLevel);
                    cachedResponse.IsCompressed = true;
                }

                // Check cache size limits
                await EnsureCacheCapacityAsync(cacheSettings.MaxCacheSizeBytes);

                _responseCache[cacheKey] = cachedResponse;

                _logger.LogDebug("Cached response for key {CacheKey}, expires at {ExpiresAt}", 
                    cacheKey.Substring(0, Math.Min(8, cacheKey.Length)), cachedResponse.ExpiresAt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error caching response");
            }
        }

        /// <inheritdoc/>
        public async Task<ChatResponse?> GetCachedResponseAsync(ChatRequest request)
        {
            try
            {
                var cacheKey = await GenerateCacheKeyAsync(request, null);
                
                if (_responseCache.TryGetValue(cacheKey, out var cachedResponse))
                {
                    // Check if cache entry is still valid
                    if (DateTime.UtcNow <= cachedResponse.ExpiresAt)
                    {
                        // Update access statistics
                        cachedResponse.AccessCount++;
                        cachedResponse.LastAccessedAt = DateTime.UtcNow;

                        // Decompress if necessary
                        if (cachedResponse.IsCompressed && cachedResponse.CompressedData != null)
                        {
                            var decompressedResponse = await DecompressResponseAsync(cachedResponse.CompressedData);
                            _logger.LogDebug("Cache hit for key {CacheKey} (compressed), access count: {AccessCount}", 
                                cacheKey.Substring(0, Math.Min(8, cacheKey.Length)), cachedResponse.AccessCount);
                            return decompressedResponse;
                        }

                        _logger.LogDebug("Cache hit for key {CacheKey}, access count: {AccessCount}", 
                            cacheKey.Substring(0, Math.Min(8, cacheKey.Length)), cachedResponse.AccessCount);
                        return cachedResponse.Response;
                    }
                    else
                    {
                        // Remove expired entry
                        _responseCache.TryRemove(cacheKey, out _);
                        _logger.LogDebug("Removed expired cache entry for key {CacheKey}", 
                            cacheKey.Substring(0, Math.Min(8, cacheKey.Length)));
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cached response");
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<CompressedData> CompressDataAsync(byte[] data)
        {
            try
            {
                using var outputStream = new MemoryStream();
                using var compressionStream = new GZipStream(outputStream, SystemCompressionLevel.Optimal);
                
                await compressionStream.WriteAsync(data);
                await compressionStream.FlushAsync();
                
                var compressedBytes = outputStream.ToArray();
                
                return new CompressedData
                {
                    Data = compressedBytes,
                    OriginalSizeBytes = data.Length,
                    CompressedSizeBytes = compressedBytes.Length,
                    Algorithm = CompressionAlgorithm.Gzip,
                    Metadata = new Dictionary<string, object>
                    {
                        ["CompressedAt"] = DateTime.UtcNow,
                        ["CompressionRatio"] = (float)compressedBytes.Length / data.Length
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error compressing data");
                return new CompressedData
                {
                    Data = data,
                    OriginalSizeBytes = data.Length,
                    CompressedSizeBytes = data.Length,
                    Algorithm = CompressionAlgorithm.None
                };
            }
        }

        /// <inheritdoc/>
        public async Task<byte[]> DecompressDataAsync(CompressedData compressedData)
        {
            try
            {
                if (compressedData.Algorithm == CompressionAlgorithm.None)
                {
                    return compressedData.Data;
                }

                using var inputStream = new MemoryStream(compressedData.Data);
                using var decompressionStream = new GZipStream(inputStream, CompressionMode.Decompress);
                using var outputStream = new MemoryStream();
                
                await decompressionStream.CopyToAsync(outputStream);
                return outputStream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decompressing data");
                return compressedData.Data; // Return original data if decompression fails
            }
        }

        /// <inheritdoc/>
        public async Task<EmbeddingBatchResult> OptimizeEmbeddingGenerationAsync(IEnumerable<string> texts, int batchSize, CancellationToken cancellationToken = default)
        {
            try
            {
                var textList = texts.ToList();
                _logger.LogInformation("Optimizing embedding generation for {TextCount} texts with batch size {BatchSize}", 
                    textList.Count, batchSize);

                var stopwatch = Stopwatch.StartNew();
                var result = new EmbeddingBatchResult();
                var allEmbeddings = new List<float[]>();
                var errors = new List<string>();

                // Process in optimized batches
                for (int i = 0; i < textList.Count; i += batchSize)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    var batch = textList.Skip(i).Take(batchSize).ToList();
                    
                    try
                    {
                        // Simulate embedding generation (replace with actual embedding service call)
                        var batchEmbeddings = await GenerateEmbeddingBatchAsync(batch, cancellationToken);
                        allEmbeddings.AddRange(batchEmbeddings);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Batch {i / batchSize + 1}: {ex.Message}");
                        _logger.LogWarning(ex, "Error processing embedding batch {BatchIndex}", i / batchSize + 1);
                    }
                }

                stopwatch.Stop();

                result.Embeddings = allEmbeddings;
                result.TotalProcessingTime = stopwatch.Elapsed;
                result.EmbeddingsPerSecond = (float)(allEmbeddings.Count / stopwatch.Elapsed.TotalSeconds);
                result.BatchEfficiency = CalculateBatchEfficiency(textList.Count, batchSize, stopwatch.Elapsed);
                result.Errors = errors;

                _logger.LogInformation("Embedding generation completed: {EmbeddingCount} embeddings in {TotalTime}ms, {EmbeddingsPerSecond:F1} embeddings/sec",
                    allEmbeddings.Count, result.TotalProcessingTime.TotalMilliseconds, result.EmbeddingsPerSecond);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error optimizing embedding generation");
                return new EmbeddingBatchResult
                {
                    Errors = { ex.Message }
                };
            }
        }

        /// <inheritdoc/>
        public async Task<ParallelProcessingResult> ProcessInParallelAsync(IEnumerable<OptimizedRequest> requests, int maxConcurrency, CancellationToken cancellationToken = default)
        {
            try
            {
                var requestList = requests.ToList();
                _logger.LogInformation("Processing {RequestCount} requests in parallel with max concurrency {MaxConcurrency}", 
                    requestList.Count, maxConcurrency);

                var stopwatch = Stopwatch.StartNew();
                var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
                var results = new ConcurrentBag<IndividualRequestResult>();

                // Process requests in parallel with controlled concurrency
                var tasks = requestList.Select(async request =>
                {
                    await semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        var requestStopwatch = Stopwatch.StartNew();
                        var success = await ProcessIndividualRequestAsync(request, cancellationToken);
                        requestStopwatch.Stop();

                        results.Add(new IndividualRequestResult
                        {
                            RequestId = request.OriginalRequest.ConversationId,
                            Success = success,
                            ProcessingTime = requestStopwatch.Elapsed,
                            ResponseSizeBytes = success ? 1024 : 0 // Placeholder
                        });
                    }
                    catch (Exception ex)
                    {
                        results.Add(new IndividualRequestResult
                        {
                            RequestId = request.OriginalRequest.ConversationId,
                            Success = false,
                            ErrorMessage = ex.Message,
                            ProcessingTime = TimeSpan.Zero
                        });
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                await Task.WhenAll(tasks);
                stopwatch.Stop();

                var resultList = results.ToList();
                var successfulResults = resultList.Where(r => r.Success).ToList();

                var result = new ParallelProcessingResult
                {
                    TotalRequests = requestList.Count,
                    SuccessfulRequests = successfulResults.Count,
                    FailedRequests = resultList.Count - successfulResults.Count,
                    TotalWallClockTime = stopwatch.Elapsed,
                    MaxConcurrentOperations = Math.Min(maxConcurrency, requestList.Count),
                    Results = resultList,
                    ParallelizationEfficiency = CalculateParallelizationEfficiency(requestList.Count, stopwatch.Elapsed, maxConcurrency)
                };

                _logger.LogInformation("Parallel processing completed: {SuccessfulRequests}/{TotalRequests} successful in {TotalTime}ms, {Efficiency:F1}% efficiency",
                    result.SuccessfulRequests, result.TotalRequests, result.TotalWallClockTime.TotalMilliseconds, result.ParallelizationEfficiency);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing requests in parallel");
                return new ParallelProcessingResult
                {
                    TotalRequests = requests.Count(),
                    FailedRequests = requests.Count()
                };
            }
        }

        private async Task<RequestOptimizationSettings> CreateOptimizationSettingsAsync(ChatRequest request, OptimizationContext context)
        {
            var settings = new RequestOptimizationSettings();

            // Adjust based on system performance
            if (context.SystemMetrics.CpuUtilization > 80)
            {
                settings.MaxTokens = Math.Min(settings.MaxTokens, 512);
                settings.Timeout = TimeSpan.FromSeconds(15);
            }

            if (context.SystemMetrics.VramUtilization > 85)
            {
                settings.StreamingChunkSize = 25; // Smaller chunks
                settings.UseCompression = true;
            }

            // Adjust based on user preferences
            switch (context.UserPreferences.QualitySpeedBalance)
            {
                case QualitySpeedPreference.MaxSpeed:
                    settings.MaxTokens = 256;
                    settings.StreamingChunkSize = 100;
                    settings.ModelSettings.Use4BitQuantization = true;
                    settings.ModelSettings.ContextWindowSize = 2048;
                    break;

                case QualitySpeedPreference.MaxQuality:
                    settings.MaxTokens = 2048;
                    settings.StreamingChunkSize = 25;
                    settings.ModelSettings.ContextWindowSize = 8192;
                    break;

                case QualitySpeedPreference.Balanced:
                default:
                    settings.MaxTokens = 1024;
                    settings.StreamingChunkSize = 50;
                    settings.ModelSettings.ContextWindowSize = 4096;
                    break;
            }

            // Adjust based on queue status
            if (context.QueueStatus.QueueDepth > 5)
            {
                settings.EnableCaching = true;
                settings.UseCompression = true;
            }

            return await Task.FromResult(settings);
        }

        private RequestPriority DeterminePriority(ChatRequest request, OptimizationContext context)
        {
            // Code completion gets high priority for responsiveness
            if (request.Message.Contains("complete") || request.Message.Contains("suggest"))
                return RequestPriority.High;

            // Long requests get lower priority
            if (request.Message.Length > 1000)
                return RequestPriority.Low;

            // High system load reduces priority
            if (context.SystemMetrics.CpuUtilization > 80)
                return RequestPriority.Low;

            return RequestPriority.Normal;
        }

        private bool CanRequestBeBatched(ChatRequest request)
        {
            // Simple heuristics for batching
            return request.Message.Length < 500 && // Short requests
                   !request.Message.Contains("urgent") && // Not urgent
                   !request.Message.Contains("stream"); // Not streaming
        }

        private string? DetermineBatchGroup(ChatRequest request)
        {
            if (!CanRequestBeBatched(request))
                return null;

            // Group by request type/pattern
            if (request.Message.Contains("explain") || request.Message.Contains("what is"))
                return "explanation";
            
            if (request.Message.Contains("code") || request.Message.Contains("function"))
                return "coding";
            
            if (request.Message.Contains("translate"))
                return "translation";

            return "general";
        }

        private async Task<string> GenerateCacheKeyAsync(ChatRequest request, RequestOptimizationSettings? settings)
        {
            var keyComponents = new StringBuilder();
            keyComponents.Append(request.Message);
            
            if (settings != null)
            {
                keyComponents.Append($"|tokens:{settings.MaxTokens}");
                keyComponents.Append($"|context:{settings.ModelSettings.ContextWindowSize}");
                keyComponents.Append($"|temp:{settings.ModelSettings.Temperature}");
            }

            // Add conversation context hash if available
            if (!string.IsNullOrEmpty(request.ConversationId))
            {
                keyComponents.Append($"|conv:{request.ConversationId}");
            }

            var keyString = keyComponents.ToString();
            
            // Generate hash
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(keyString));
            return Convert.ToHexString(hashBytes);
        }

        private TimeSpan EstimateProcessingTime(ChatRequest request, OptimizationContext context)
        {
            var baseTime = TimeSpan.FromMilliseconds(1000); // 1 second base

            // Adjust for message length
            var lengthMultiplier = Math.Max(0.5, Math.Min(3.0, request.Message.Length / 500.0));
            baseTime = TimeSpan.FromMilliseconds(baseTime.TotalMilliseconds * lengthMultiplier);

            // Adjust for system load
            var loadMultiplier = 1.0 + (context.SystemMetrics.CpuUtilization / 100.0);
            baseTime = TimeSpan.FromMilliseconds(baseTime.TotalMilliseconds * loadMultiplier);

            // Adjust for queue depth
            var queueMultiplier = 1.0 + (context.QueueStatus.QueueDepth * 0.1);
            baseTime = TimeSpan.FromMilliseconds(baseTime.TotalMilliseconds * queueMultiplier);

            return baseTime;
        }

        private OptimizationLevel DetermineOptimizationLevel(OptimizationContext context)
        {
            if (context.SystemMetrics.VramUtilization > 85 || context.SystemMetrics.CpuUtilization > 80)
                return OptimizationLevel.Speed;

            if (context.UserPreferences.QualitySpeedBalance == QualitySpeedPreference.MaxQuality)
                return OptimizationLevel.Quality;

            return OptimizationLevel.Balanced;
        }

        private List<List<OptimizedRequest>> GroupRequestsForBatching(List<OptimizedRequest> requests)
        {
            var groups = new List<List<OptimizedRequest>>();
            var batchableRequests = requests.Where(r => r.CanBatch).GroupBy(r => r.BatchGroup);

            foreach (var group in batchableRequests)
            {
                var groupRequests = group.ToList();
                
                // Split into batches of MAX_BATCH_SIZE
                for (int i = 0; i < groupRequests.Count; i += MAX_BATCH_SIZE)
                {
                    groups.Add(groupRequests.Skip(i).Take(MAX_BATCH_SIZE).ToList());
                }
            }

            // Add non-batchable requests as individual batches
            foreach (var request in requests.Where(r => !r.CanBatch))
            {
                groups.Add(new List<OptimizedRequest> { request });
            }

            return groups;
        }

        private async Task<List<IndividualRequestResult>> ProcessBatchGroupAsync(List<OptimizedRequest> group, CancellationToken cancellationToken)
        {
            var results = new List<IndividualRequestResult>();

            try
            {
                if (group.Count == 1)
                {
                    // Process single request
                    var request = group[0];
                    var stopwatch = Stopwatch.StartNew();
                    var success = await ProcessIndividualRequestAsync(request, cancellationToken);
                    stopwatch.Stop();

                    results.Add(new IndividualRequestResult
                    {
                        RequestId = request.OriginalRequest.ConversationId,
                        Success = success,
                        ProcessingTime = stopwatch.Elapsed,
                        ResponseSizeBytes = success ? 1024 : 0 // Placeholder
                    });
                }
                else
                {
                    // Process as batch (placeholder for actual batch processing)
                    _logger.LogDebug("Processing batch of {BatchSize} requests", group.Count);
                    
                    var batchStopwatch = Stopwatch.StartNew();
                    
                    // Simulate batch processing
                    await Task.Delay(500 * group.Count, cancellationToken);
                    
                    batchStopwatch.Stop();

                    // Distribute processing time among requests
                    var avgProcessingTime = TimeSpan.FromMilliseconds(batchStopwatch.Elapsed.TotalMilliseconds / group.Count);

                    foreach (var request in group)
                    {
                        results.Add(new IndividualRequestResult
                        {
                            RequestId = request.OriginalRequest.ConversationId,
                            Success = true,
                            ProcessingTime = avgProcessingTime,
                            ResponseSizeBytes = 1024 // Placeholder
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing batch group");
                
                // Mark all requests in group as failed
                foreach (var request in group)
                {
                    results.Add(new IndividualRequestResult
                    {
                        RequestId = request.OriginalRequest.ConversationId,
                        Success = false,
                        ErrorMessage = ex.Message,
                        ProcessingTime = TimeSpan.Zero
                    });
                }
            }

            return results;
        }

        private async Task<bool> ProcessIndividualRequestAsync(OptimizedRequest request, CancellationToken cancellationToken)
        {
            try
            {
                // Placeholder for actual request processing
                await Task.Delay((int)request.EstimatedProcessingTime.TotalMilliseconds, cancellationToken);
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing individual request {RequestId}", request.OriginalRequest.ConversationId);
                return false;
            }
        }

        private async Task<byte[]> CompressResponseAsync(ChatResponse response, CoreCompressionLevel compressionLevel)
        {
            var json = JsonSerializer.Serialize(response);
            var bytes = Encoding.UTF8.GetBytes(json);
            
            var compressed = await CompressDataAsync(bytes);
            return compressed.Data;
        }

        private async Task<ChatResponse> DecompressResponseAsync(byte[] compressedData)
        {
            var decompressed = await DecompressDataAsync(new CompressedData
            {
                Data = compressedData,
                Algorithm = CompressionAlgorithm.Gzip
            });

            var json = Encoding.UTF8.GetString(decompressed);
            return JsonSerializer.Deserialize<ChatResponse>(json) ?? new ChatResponse("", "");
        }

        private async Task EnsureCacheCapacityAsync(long maxCacheSizeBytes)
        {
            var currentSize = _responseCache.Values.Sum(c => EstimateCacheEntrySize(c));
            
            if (currentSize > maxCacheSizeBytes)
            {
                // Remove oldest entries until under limit
                var entriesToRemove = _responseCache.Values
                    .OrderBy(c => c.LastAccessedAt)
                    .TakeWhile(c => currentSize > maxCacheSizeBytes)
                    .ToList();

                foreach (var entry in entriesToRemove)
                {
                    var keyToRemove = _responseCache.FirstOrDefault(kvp => kvp.Value == entry).Key;
                    if (keyToRemove != null)
                    {
                        _responseCache.TryRemove(keyToRemove, out _);
                        currentSize -= EstimateCacheEntrySize(entry);
                    }
                }

                _logger.LogDebug("Removed {EntryCount} cache entries to free space", entriesToRemove.Count);
            }

            await Task.CompletedTask;
        }

        private long EstimateCacheEntrySize(CachedResponse cachedResponse)
        {
            var baseSize = cachedResponse.Response.Message.Length * 2; // UTF-16 estimate
            if (cachedResponse.IsCompressed && cachedResponse.CompressedData != null)
            {
                return baseSize + cachedResponse.CompressedData.Length;
            }
            return baseSize + 200; // Overhead estimate
        }

        private async Task<List<float[]>> GenerateEmbeddingBatchAsync(List<string> texts, CancellationToken cancellationToken)
        {
            // Placeholder for actual embedding generation
            await Task.Delay(100 * texts.Count, cancellationToken);
            
            return texts.Select(text => new float[768]).ToList(); // 768-dimensional embeddings
        }

        private float CalculateBatchEfficiency(int totalTexts, int batchSize, TimeSpan actualTime)
        {
            // Estimate individual processing time vs batch time
            var estimatedIndividualTime = totalTexts * 200; // 200ms per text estimate
            var batchingOverhead = Math.Max(1, totalTexts / batchSize) * 50; // 50ms overhead per batch
            var estimatedBatchTime = estimatedIndividualTime * 0.7 + batchingOverhead; // 30% efficiency gain

            return Math.Max(0, (float)((estimatedIndividualTime - actualTime.TotalMilliseconds) / estimatedIndividualTime * 100));
        }

        private float CalculateParallelizationEfficiency(int requestCount, TimeSpan wallClockTime, int maxConcurrency)
        {
            // Theoretical minimum time if perfect parallelization
            var theoreticalMinTime = (requestCount * 1000.0) / maxConcurrency; // 1 second per request
            
            // Actual efficiency
            return Math.Max(0, (float)((theoreticalMinTime / wallClockTime.TotalMilliseconds) * 100));
        }

        private async void CleanupExpiredCache(object? state)
        {
            if (_disposed) return;

            try
            {
                var now = DateTime.UtcNow;
                var expiredKeys = _responseCache
                    .Where(kvp => kvp.Value.ExpiresAt <= now)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in expiredKeys)
                {
                    _responseCache.TryRemove(key, out _);
                }

                if (expiredKeys.Count > 0)
                {
                    _logger.LogDebug("Cleaned up {ExpiredCount} expired cache entries", expiredKeys.Count);
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cache cleanup");
            }
        }

        private async void ProcessPendingBatches(object? state)
        {
            if (_disposed) return;

            try
            {
                await _batchingSemaphore.WaitAsync();
                
                // Process any pending batch groups that have timed out
                var groupsToProcess = _batchGroups.Values
                    .Where(g => DateTime.UtcNow - g.CreatedAt > TimeSpan.FromMilliseconds(BATCH_TIMEOUT_MS))
                    .ToList();

                foreach (var group in groupsToProcess)
                {
                    _batchGroups.TryRemove(group.Id, out _);
                    
                    // Process the batch
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await ProcessBatchGroupAsync(group.Requests, CancellationToken.None);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing timed-out batch group {GroupId}", group.Id);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing pending batches");
            }
            finally
            {
                _batchingSemaphore.Release();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            _cacheCleanupTimer?.Dispose();
            _batchProcessingTimer?.Dispose();
            _batchingSemaphore?.Dispose();

            _logger.LogInformation("Request optimization service disposed");
        }

        private class CachedResponse
        {
            public ChatResponse Response { get; set; } = new("", "");
            public DateTime CachedAt { get; set; }
            public DateTime ExpiresAt { get; set; }
            public DateTime LastAccessedAt { get; set; }
            public int AccessCount { get; set; }
            public CoreCompressionLevel CompressionLevel { get; set; }
            public bool IsCompressed { get; set; }
            public byte[]? CompressedData { get; set; }
        }

        private class BatchGroup
        {
            public string Id { get; set; } = Guid.NewGuid().ToString();
            public List<OptimizedRequest> Requests { get; set; } = new();
            public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        }
    }
}
