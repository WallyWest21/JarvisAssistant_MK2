using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JarvisAssistant.Services
{
    /// <summary>
    /// ElevenLabs implementation of the voice service with streaming, caching, and fallback support.
    /// </summary>
    public class ElevenLabsVoiceService : IVoiceService, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ElevenLabsVoiceService> _logger;
        private readonly IAudioCacheService _cacheService;
        private readonly IRateLimitService _rateLimitService;
        private readonly IVoiceService _fallbackService;
        private readonly ElevenLabsConfig _config;
        private readonly JsonSerializerOptions _jsonOptions;
        private bool _disposed = false;
        private ElevenLabsQuotaResponse? _lastQuotaInfo;
        private DateTime _lastQuotaCheck = DateTime.MinValue;

        /// <summary>
        /// Initializes a new instance of the ElevenLabsVoiceService.
        /// </summary>
        /// <param name="httpClient">HTTP client for API requests.</param>
        /// <param name="config">ElevenLabs configuration.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="cacheService">Audio caching service.</param>
        /// <param name="rateLimitService">Rate limiting service.</param>
        /// <param name="fallbackService">Fallback voice service.</param>
        public ElevenLabsVoiceService(
            HttpClient httpClient,
            ElevenLabsConfig config,
            ILogger<ElevenLabsVoiceService> logger,
            IAudioCacheService cacheService,
            IRateLimitService rateLimitService,
            IVoiceService fallbackService)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _rateLimitService = rateLimitService ?? throw new ArgumentNullException(nameof(rateLimitService));
            _fallbackService = fallbackService ?? throw new ArgumentNullException(nameof(fallbackService));

            if (!_config.IsValid())
            {
                throw new ArgumentException("Invalid ElevenLabs configuration", nameof(config));
            }

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                WriteIndented = false
            };

            ConfigureHttpClient();

            _logger.LogInformation("ElevenLabs voice service initialized with voice ID: {VoiceId}", _config.VoiceId);
        }

        /// <inheritdoc/>
        public async Task<byte[]> GenerateSpeechAsync(string text, string? voiceId = null, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ElevenLabsVoiceService));

            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<byte>();

            try
            {
                var effectiveVoiceId = voiceId ?? _config.VoiceId ?? throw new InvalidOperationException("No voice ID specified");
                var voiceSettings = DetermineVoiceSettings(text);

                // Check cache first
                if (_config.EnableCaching)
                {
                    var cachedAudio = await _cacheService.GetCachedAudioAsync(text, effectiveVoiceId, voiceSettings);
                    if (cachedAudio != null)
                    {
                        _logger.LogDebug("Using cached audio for text: {Text}", text[..Math.Min(50, text.Length)]);
                        return cachedAudio;
                    }
                }

                // Check rate limiting
                if (_config.EnableRateLimiting && !await _rateLimitService.CanMakeRequestAsync(_config.ApiKey!))
                {
                    _logger.LogWarning("Rate limited, using fallback service");
                    return await UseFallbackServiceAsync(text, voiceId, cancellationToken);
                }

                // Check quota
                await CheckQuotaAsync(text, cancellationToken);

                // Prepare and enhance text for Jarvis
                var enhancedText = EnhanceTextForJarvis(text);

                var request = new ElevenLabsRequest
                {
                    Text = enhancedText,
                    VoiceSettings = voiceSettings,
                    ModelId = _config.ModelId,
                    OutputFormat = _config.AudioFormat
                };

                var audioData = await MakeApiRequestAsync(request, effectiveVoiceId, cancellationToken);

                // Record request for rate limiting
                if (_config.EnableRateLimiting)
                {
                    await _rateLimitService.RecordRequestAsync(_config.ApiKey!, enhancedText.Length);
                }

                // Cache the result
                if (_config.EnableCaching && audioData.Length > 0)
                {
                    await _cacheService.CacheAudioAsync(text, effectiveVoiceId, voiceSettings, audioData);
                }

                _logger.LogInformation("Generated speech for text length: {Length}, audio size: {Size} bytes", 
                    text.Length, audioData.Length);

                return audioData;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error generating speech with ElevenLabs, using fallback");
                return await UseFallbackServiceAsync(text, voiceId, cancellationToken);
            }
        }

        /// <inheritdoc/>
        public async IAsyncEnumerable<byte[]> StreamSpeechAsync(string text, string? voiceId = null, 
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ElevenLabsVoiceService));

            if (string.IsNullOrWhiteSpace(text))
                yield break;

            // For streaming, we don't use cache - always generate fresh
            if (!_config.EnableStreaming)
            {
                // Fall back to non-streaming if streaming is disabled
                var audioData = await GenerateSpeechAsync(text, voiceId, cancellationToken);
                if (audioData.Length > 0)
                {
                    // Chunk the complete audio for streaming simulation
                    for (int i = 0; i < audioData.Length; i += _config.StreamingChunkSize)
                    {
                        var chunkSize = Math.Min(_config.StreamingChunkSize, audioData.Length - i);
                        var chunk = new byte[chunkSize];
                        Array.Copy(audioData, i, chunk, 0, chunkSize);
                        yield return chunk;
                    }
                }
                yield break;
            }

            var effectiveVoiceId = voiceId ?? _config.VoiceId ?? throw new InvalidOperationException("No voice ID specified");

            // Check rate limiting
            if (_config.EnableRateLimiting && !await _rateLimitService.CanMakeRequestAsync(_config.ApiKey!))
            {
                _logger.LogWarning("Rate limited, using fallback streaming");
                var fallbackChunks = _fallbackService.StreamSpeechAsync(text, voiceId, cancellationToken);
                await foreach (var chunk in fallbackChunks)
                {
                    yield return chunk;
                }
                yield break;
            }

            // Use a separate method to handle the try-catch logic
            var streamChunks = StreamWithFallbackAsync(text, effectiveVoiceId, cancellationToken);
            await foreach (var chunk in streamChunks)
            {
                yield return chunk;
            }
        }

        private async IAsyncEnumerable<byte[]> StreamWithFallbackAsync(string text, string effectiveVoiceId,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // Try to stream from ElevenLabs first
            var success = false;
            
            // Prepare request outside try block
            ElevenLabsRequest? request = null;
            string? enhancedText = null;
            
            try
            {
                // Check quota
                await CheckQuotaAsync(text, cancellationToken);

                enhancedText = EnhanceTextForJarvis(text);
                var voiceSettings = DetermineVoiceSettings(text);

                request = new ElevenLabsRequest
                {
                    Text = enhancedText,
                    VoiceSettings = voiceSettings,
                    ModelId = _config.ModelId,
                    Stream = true,
                    OutputFormat = _config.AudioFormat
                };
                
                success = true;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error preparing streaming request, using fallback");
                success = false;
            }

            if (success && request != null)
            {
                // Try to stream the actual request
                var streamingSuccessful = false;
                IAsyncEnumerable<byte[]>? chunks = null;
                
                // Get the stream outside of try-catch to avoid yield return issues
                try
                {
                    chunks = StreamApiDirectAsync(request, effectiveVoiceId, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Failed to initialize streaming, using fallback");
                    chunks = null;
                }

                if (chunks != null)
                {
                    // Now iterate through the stream
                    await foreach (var chunk in chunks)
                    {
                        streamingSuccessful = true;
                        yield return chunk;
                    }

                    if (streamingSuccessful)
                    {
                        // Record request for rate limiting
                        if (_config.EnableRateLimiting && enhancedText != null)
                        {
                            await _rateLimitService.RecordRequestAsync(_config.ApiKey!, enhancedText.Length);
                        }

                        _logger.LogInformation("Streamed speech for text length: {Length}", text.Length);
                        yield break;
                    }
                }
            }

            // If we reach here, use fallback
            var fallbackChunks = _fallbackService.StreamSpeechAsync(text, null, cancellationToken);
            await foreach (var chunk in fallbackChunks)
            {
                yield return chunk;
            }
        }

        private async IAsyncEnumerable<byte[]> StreamApiDirectAsync(ElevenLabsRequest request, string voiceId,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var url = _config.GetStreamingUrl();
            var jsonContent = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            HttpResponseMessage? response = null;
            Stream? stream = null;
            
            // Setup phase - can throw exceptions
            try
            {
                response = await _httpClient.PostAsync(url, content, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("ElevenLabs streaming API error {StatusCode}: {Content}", response.StatusCode, errorContent);
                    response.Dispose();
                    yield break; // Return empty stream to indicate failure
                }

                stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Exception during streaming setup");
                response?.Dispose();
                yield break; // Return empty stream
            }

            // Streaming phase - avoid exceptions
            if (stream != null && response != null)
            {
                using (response)
                using (stream)
                {
                    var buffer = new byte[_config.StreamingChunkSize];

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        int bytesRead;
                        try
                        {
                            bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            _logger.LogError(ex, "Exception during stream read, terminating");
                            break;
                        }
                        
                        if (bytesRead == 0)
                            break;

                        var chunk = new byte[bytesRead];
                        Array.Copy(buffer, chunk, bytesRead);
                        yield return chunk;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public async Task<string> RecognizeSpeechAsync(byte[] audioData, string? language = null, CancellationToken cancellationToken = default)
        {
            // ElevenLabs doesn't provide speech recognition, delegate to fallback
            _logger.LogDebug("Speech recognition not supported by ElevenLabs, using fallback service");
            return await _fallbackService.RecognizeSpeechAsync(audioData, language, cancellationToken);
        }

        /// <summary>
        /// Gets current quota information from ElevenLabs API.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Quota information or null if unavailable.</returns>
        public async Task<ElevenLabsQuotaResponse?> GetQuotaInfoAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
                return null;

            try
            {
                var url = $"{_config.BaseUrl}/v1/user/subscription";
                var response = await _httpClient.GetAsync(url, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    var quotaInfo = JsonSerializer.Deserialize<ElevenLabsQuotaResponse>(content, _jsonOptions);
                    
                    _lastQuotaInfo = quotaInfo;
                    _lastQuotaCheck = DateTime.UtcNow;
                    
                    return quotaInfo;
                }
                else
                {
                    _logger.LogWarning("Failed to get quota info: {StatusCode}", response.StatusCode);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving quota information");
                return null;
            }
        }

        /// <summary>
        /// Gets available voices from ElevenLabs API.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of available voices.</returns>
        public async Task<List<ElevenLabsVoice>> GetAvailableVoicesAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
                return new List<ElevenLabsVoice>();

            try
            {
                var url = _config.GetVoicesUrl();
                var response = await _httpClient.GetAsync(url, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    var voicesResponse = JsonSerializer.Deserialize<ElevenLabsVoicesResponse>(content, _jsonOptions);
                    
                    return voicesResponse?.Voices ?? new List<ElevenLabsVoice>();
                }
                else
                {
                    _logger.LogWarning("Failed to get voices: {StatusCode}", response.StatusCode);
                    return new List<ElevenLabsVoice>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available voices");
                return new List<ElevenLabsVoice>();
            }
        }

        private void ConfigureHttpClient()
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("xi-api-key", _config.ApiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("JarvisAssistant", "1.0"));
            _httpClient.Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds);
        }

        private async Task<byte[]> MakeApiRequestAsync(ElevenLabsRequest request, string voiceId, CancellationToken cancellationToken)
        {
            var url = _config.GetTextToSpeechUrl();
            var jsonContent = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            for (int attempt = 0; attempt <= _config.MaxRetryAttempts; attempt++)
            {
                try
                {
                    var response = await _httpClient.PostAsync(url, content, cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        // Rate limited by server
                        var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(60);
                        _logger.LogWarning("Server rate limit exceeded, waiting {Delay}s", retryAfter.TotalSeconds);
                        
                        if (attempt < _config.MaxRetryAttempts)
                        {
                            await Task.Delay(retryAfter, cancellationToken);
                            continue;
                        }
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                        _logger.LogError("ElevenLabs API error {StatusCode}: {Content}", response.StatusCode, errorContent);
                    }

                    if (attempt == _config.MaxRetryAttempts)
                    {
                        throw new HttpRequestException($"ElevenLabs API request failed after {_config.MaxRetryAttempts} attempts: {response.StatusCode}");
                    }
                }
                catch (HttpRequestException)
                {
                    if (attempt == _config.MaxRetryAttempts)
                        throw;

                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt + 1)); // Exponential backoff
                    _logger.LogWarning("API request attempt {Attempt} failed, retrying in {Delay}s", attempt + 1, delay.TotalSeconds);
                    await Task.Delay(delay, cancellationToken);
                }
            }

            return Array.Empty<byte>();
        }

        private async Task CheckQuotaAsync(string text, CancellationToken cancellationToken)
        {
            // Check quota every 5 minutes or if we don't have recent info
            if (_lastQuotaInfo == null || DateTime.UtcNow - _lastQuotaCheck > TimeSpan.FromMinutes(5))
            {
                var quotaInfo = await GetQuotaInfoAsync(cancellationToken);
                if (quotaInfo != null)
                {
                    if (quotaInfo.CharactersRemaining < text.Length)
                    {
                        throw new InvalidOperationException($"Insufficient quota: {quotaInfo.CharactersRemaining} remaining, {text.Length} required");
                    }

                    if (quotaInfo.QuotaUsedPercentage > 90)
                    {
                        _logger.LogWarning("Quota usage high: {Percentage:F1}% used", quotaInfo.QuotaUsedPercentage);
                    }
                }
            }
        }

        private VoiceSettings DetermineVoiceSettings(string text)
        {
            // Analyze text for emotional context and adjust voice accordingly
            var lowerText = text.ToLowerInvariant();

            if (Regex.IsMatch(lowerText, @"\b(error|problem|failed|issue|warning)\b"))
            {
                return VoiceSettings.CreateEmotionalSettings("concerned");
            }
            else if (Regex.IsMatch(lowerText, @"\b(excellent|perfect|success|completed|great)\b"))
            {
                return VoiceSettings.CreateEmotionalSettings("excited");
            }
            else if (Regex.IsMatch(lowerText, @"\b(calm|relax|peace|stable|normal)\b"))
            {
                return VoiceSettings.CreateEmotionalSettings("calm");
            }

            return _config.DefaultVoiceSettings;
        }

        private string EnhanceTextForJarvis(string text)
        {
            // Add SSML-like enhancements for Jarvis personality
            var enhanced = text;

            // Add pauses after "Sir" for dramatic effect
            enhanced = Regex.Replace(enhanced, @"\bSir\b", "Sir<break time=\"500ms\"/>", RegexOptions.IgnoreCase);

            // Add emphasis to technical terms
            enhanced = Regex.Replace(enhanced, @"\b(system|status|analysis|diagnostic|protocol|initialized|activated)\b", 
                "<emphasis level=\"moderate\">$1</emphasis>", RegexOptions.IgnoreCase);

            // Add slight pause before important announcements
            enhanced = Regex.Replace(enhanced, @"^(Alert|Warning|Error|System)", "<break time=\"300ms\"/>$1", RegexOptions.IgnoreCase);

            // Ensure proper pronunciation of technical terms
            enhanced = enhanced.Replace("API", "<phoneme alphabet=\"ipa\" ph=\"ˈeɪ.piː.aɪ\">API</phoneme>");
            enhanced = enhanced.Replace("CPU", "<phoneme alphabet=\"ipa\" ph=\"ˈsiː.piː.juː\">CPU</phoneme>");
            enhanced = enhanced.Replace("GPU", "<phoneme alphabet=\"ipa\" ph=\"ˈdʒiː.piː.juː\">GPU</phoneme>");

            return enhanced;
        }

        private async Task<byte[]> UseFallbackServiceAsync(string text, string? voiceId, CancellationToken cancellationToken)
        {
            if (!_config.EnableFallback)
            {
                _logger.LogWarning("Fallback disabled, returning empty audio");
                return Array.Empty<byte>();
            }

            try
            {
                _logger.LogInformation("Using fallback voice service");
                return await _fallbackService.GenerateSpeechAsync(text, voiceId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallback voice service also failed");
                return Array.Empty<byte>();
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            // HttpClient is injected and managed externally, don't dispose it
        }

        /// <summary>
        /// Performs a health check by validating the API connection and configuration.
        /// </summary>
        /// <returns>True if the service is healthy, false otherwise.</returns>
        public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
                return false;

            try
            {
                // Check if configuration is valid
                if (!_config.IsValid())
                {
                    _logger.LogWarning("ElevenLabs service configuration is invalid");
                    return false;
                }

                // Try to get user information from ElevenLabs API
                var request = new HttpRequestMessage(HttpMethod.Get, "v1/user");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.ApiKey);

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogDebug("ElevenLabs service health check successful");
                    return true;
                }
                else
                {
                    _logger.LogWarning("ElevenLabs service health check failed with status: {StatusCode}", response.StatusCode);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ElevenLabs service health check failed");
                return false;
            }
        }
    }
}
