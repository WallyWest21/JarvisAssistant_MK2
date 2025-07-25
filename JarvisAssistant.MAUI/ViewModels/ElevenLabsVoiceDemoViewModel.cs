using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models;
using JarvisAssistant.Services;
using JarvisAssistant.Services.Extensions;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace JarvisAssistant.MAUI.ViewModels
{
    /// <summary>
    /// ViewModel for the ElevenLabs voice demonstration page.
    /// </summary>
    public partial class ElevenLabsVoiceDemoViewModel : INotifyPropertyChanged
    {
        private readonly IVoiceService _voiceService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ElevenLabsVoiceDemoViewModel> _logger;

        private string _statusMessage = "Ready";
        private string _testText = "Hello Sir, I am Jarvis, your AI assistant. How may I help you today?";
        private bool _isGenerating = false;
        private bool _isStreaming = false;
        private string _selectedEmotion = "default";
        private string _selectedVoiceId = "";
        private int _audioQuality = 7;
        private float _speakingRate = 0.9f;
        private float _stability = 0.75f;
        private float _similarity = 0.85f;
        private string _cacheStats = "";
        private string _rateLimitStats = "";
        private string _quotaInfo = "";

        public ElevenLabsVoiceDemoViewModel(
            IVoiceService voiceService,
            IServiceProvider serviceProvider,
            ILogger<ElevenLabsVoiceDemoViewModel> logger)
        {
            _voiceService = voiceService ?? throw new ArgumentNullException(nameof(voiceService));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            Emotions = new ObservableCollection<string> { "default", "excited", "concerned", "calm" };
            TestPhrases = new ObservableCollection<string>
            {
                "Hello Sir, I am Jarvis, your AI assistant. How may I help you today?",
                "System status: All systems are operational and running at optimal parameters.",
                "Alert: Anomalous activity detected in sector 7. Initiating diagnostic protocols.",
                "Excellent work, Sir. The task has been completed successfully.",
                "Please remain calm while I analyze the situation and provide recommendations.",
                "Good morning, Sir. Today's weather is pleasant with clear skies ahead.",
                "Warning: System resources are running low. Immediate attention required.",
                "Task analysis complete. Shall I proceed with the implementation, Sir?"
            };

            AvailableVoices = new ObservableCollection<VoiceOption>();
            
            _ = Task.Run(InitializeAsync);
        }

        public ObservableCollection<string> Emotions { get; }
        public ObservableCollection<string> TestPhrases { get; }
        public ObservableCollection<VoiceOption> AvailableVoices { get; }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string TestText
        {
            get => _testText;
            set => SetProperty(ref _testText, value);
        }

        public bool IsGenerating
        {
            get => _isGenerating;
            set => SetProperty(ref _isGenerating, value);
        }

        public bool IsStreaming
        {
            get => _isStreaming;
            set => SetProperty(ref _isStreaming, value);
        }

        public string SelectedEmotion
        {
            get => _selectedEmotion;
            set => SetProperty(ref _selectedEmotion, value);
        }

        public string SelectedVoiceId
        {
            get => _selectedVoiceId;
            set => SetProperty(ref _selectedVoiceId, value);
        }

        public int AudioQuality
        {
            get => _audioQuality;
            set => SetProperty(ref _audioQuality, value);
        }

        public float SpeakingRate
        {
            get => _speakingRate;
            set => SetProperty(ref _speakingRate, value);
        }

        public float Stability
        {
            get => _stability;
            set => SetProperty(ref _stability, value);
        }

        public float Similarity
        {
            get => _similarity;
            set => SetProperty(ref _similarity, value);
        }

        public string CacheStats
        {
            get => _cacheStats;
            set => SetProperty(ref _cacheStats, value);
        }

        public string RateLimitStats
        {
            get => _rateLimitStats;
            set => SetProperty(ref _rateLimitStats, value);
        }

        public string QuotaInfo
        {
            get => _quotaInfo;
            set => SetProperty(ref _quotaInfo, value);
        }

        public bool IsElevenLabsService => _voiceService is ElevenLabsVoiceService;

        public Command GenerateSpeechCommand => new Command(async () => await GenerateSpeechAsync(), () => !IsGenerating);
        public Command StreamSpeechCommand => new Command(async () => await StreamSpeechAsync(), () => !IsStreaming);
        public Command RefreshStatsCommand => new Command(async () => await RefreshStatisticsAsync());
        public Command ClearCacheCommand => new Command(async () => await ClearCacheAsync());
        public Command TestEmotionCommand => new Command<string>(async (emotion) => await TestEmotionAsync(emotion));
        public Command SelectPhraseCommand => new Command<string>((phrase) => TestText = phrase);

        private async Task InitializeAsync()
        {
            try
            {
                StatusMessage = "Initializing ElevenLabs voice demo...";

                // Load available voices if using ElevenLabs
                if (_voiceService is ElevenLabsVoiceService elevenLabsService)
                {
                    var voices = await elevenLabsService.GetAvailableVoicesAsync();
                    
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        AvailableVoices.Clear();
                        foreach (var voice in voices.Take(10)) // Limit to first 10 voices
                        {
                            AvailableVoices.Add(new VoiceOption
                            {
                                Id = voice.VoiceId,
                                Name = voice.Name,
                                Description = voice.Description ?? voice.Category
                            });
                        }
                        
                        if (AvailableVoices.Any())
                        {
                            SelectedVoiceId = AvailableVoices.First().Id;
                        }
                    });
                }

                await RefreshStatisticsAsync();
                StatusMessage = IsElevenLabsService ? "ElevenLabs service ready" : "Using fallback voice service";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing voice demo");
                StatusMessage = $"Initialization error: {ex.Message}";
            }
        }

        private async Task GenerateSpeechAsync()
        {
            if (IsGenerating || string.IsNullOrWhiteSpace(TestText))
                return;

            try
            {
                IsGenerating = true;
                StatusMessage = "Generating speech...";

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                
                var audioData = await _voiceService.GenerateSpeechAsync(TestText, SelectedVoiceId);
                
                stopwatch.Stop();

                StatusMessage = $"Generated {audioData.Length} bytes in {stopwatch.ElapsedMilliseconds}ms";
                
                await RefreshStatisticsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating speech");
                StatusMessage = $"Generation error: {ex.Message}";
            }
            finally
            {
                IsGenerating = false;
            }
        }

        private async Task StreamSpeechAsync()
        {
            if (IsStreaming || string.IsNullOrWhiteSpace(TestText))
                return;

            try
            {
                IsStreaming = true;
                StatusMessage = "Streaming speech...";

                var totalBytes = 0;
                var chunkCount = 0;
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                await foreach (var chunk in _voiceService.StreamSpeechAsync(TestText, SelectedVoiceId))
                {
                    totalBytes += chunk.Length;
                    chunkCount++;
                    
                    StatusMessage = $"Received {chunkCount} chunks, {totalBytes} bytes...";
                    
                    // Small delay to show streaming progress
                    await Task.Delay(50);
                }

                stopwatch.Stop();
                StatusMessage = $"Streamed {totalBytes} bytes in {chunkCount} chunks ({stopwatch.ElapsedMilliseconds}ms)";
                
                await RefreshStatisticsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error streaming speech");
                StatusMessage = $"Streaming error: {ex.Message}";
            }
            finally
            {
                IsStreaming = false;
            }
        }

        private async Task RefreshStatisticsAsync()
        {
            try
            {
                var stats = await _serviceProvider.GetElevenLabsStatisticsAsync();
                
                var cacheStatsText = "";
                var rateLimitStatsText = "";
                var quotaInfoText = "";

                foreach (var kvp in stats)
                {
                    if (kvp.Key.StartsWith("cache_"))
                    {
                        cacheStatsText += $"{kvp.Key.Replace("cache_", "")}: {kvp.Value}\n";
                    }
                    else if (kvp.Key.StartsWith("rate_limit_"))
                    {
                        rateLimitStatsText += $"{kvp.Key.Replace("rate_limit_", "")}: {kvp.Value}\n";
                    }
                    else if (kvp.Key.Contains("quota") || kvp.Key.Contains("character"))
                    {
                        quotaInfoText += $"{kvp.Key}: {kvp.Value}\n";
                    }
                }

                CacheStats = string.IsNullOrWhiteSpace(cacheStatsText) ? "Cache: Not available" : cacheStatsText.Trim();
                RateLimitStats = string.IsNullOrWhiteSpace(rateLimitStatsText) ? "Rate Limits: Not available" : rateLimitStatsText.Trim();
                QuotaInfo = string.IsNullOrWhiteSpace(quotaInfoText) ? "Quota: Not available" : quotaInfoText.Trim();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing statistics");
                CacheStats = $"Error: {ex.Message}";
                RateLimitStats = $"Error: {ex.Message}";
                QuotaInfo = $"Error: {ex.Message}";
            }
        }

        private async Task ClearCacheAsync()
        {
            try
            {
                var cacheService = _serviceProvider.GetService<IAudioCacheService>();
                if (cacheService != null)
                {
                    var clearedCount = await cacheService.ClearAllAsync();
                    StatusMessage = $"Cleared {clearedCount} cached entries";
                    await RefreshStatisticsAsync();
                }
                else
                {
                    StatusMessage = "Cache service not available";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cache");
                StatusMessage = $"Cache clear error: {ex.Message}";
            }
        }

        private async Task TestEmotionAsync(string emotion)
        {
            var emotionTexts = new Dictionary<string, string>
            {
                ["excited"] = "Excellent! The system is performing exceptionally well, Sir!",
                ["concerned"] = "Alert: I'm detecting some anomalies that require your attention, Sir.",
                ["calm"] = "Everything is running smoothly and within normal parameters, Sir.",
                ["default"] = "How may I assist you today, Sir?"
            };

            if (emotionTexts.TryGetValue(emotion, out var text))
            {
                SelectedEmotion = emotion;
                TestText = text;
                await GenerateSpeechAsync();
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    /// <summary>
    /// Represents a voice option for the voice selection.
    /// </summary>
    public class VoiceOption
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DisplayText => $"{Name} - {Description}";
    }
}
