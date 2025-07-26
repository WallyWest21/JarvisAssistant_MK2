using JarvisAssistant.Core.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace JarvisAssistant.MAUI.ViewModels
{
    /// <summary>
    /// ViewModel for performance settings configuration.
    /// </summary>
    public class PerformanceSettingsViewModel : INotifyPropertyChanged
    {
        private QualitySpeedPreference _qualitySpeedBalance = QualitySpeedPreference.Balanced;
        private int _maxTokensPerResponse = 1024;
        private int _batchSize = 5;
        private int _streamingChunkSize = 50;
        private int _cacheSizeMB = 100;
        private bool _enableCaching = true;
        private bool _useCompression = true;
        private bool _enableBackgroundOptimization = true;
        private bool _enableGpuMonitoring = true;
        private int _maxConcurrentRequests = 4;
        private int _requestTimeoutSeconds = 30;
        private int _vramThresholdPercent = 85;
        private bool _enableAutomaticModelOptimization = true;
        private bool _useQuantization = true;
        private bool _enableEmbeddingCaching = true;

        // Performance targets
        private int _codeCompletionTargetMs = 500;
        private int _chatResponseTargetMs = 2000;

        // Advanced settings
        private bool _showAdvancedSettings = false;
        private int _cacheCleanupIntervalMinutes = 5;
        private int _batchTimeoutMs = 100;
        private float _temperature = 0.7f;
        private int _contextWindowSize = 4096;

        public event PropertyChangedEventHandler? PropertyChanged;

        #region Basic Settings

        public QualitySpeedPreference QualitySpeedBalance
        {
            get => _qualitySpeedBalance;
            set
            {
                if (SetProperty(ref _qualitySpeedBalance, value))
                {
                    ApplyQualitySpeedPreset(value);
                }
            }
        }

        public int MaxTokensPerResponse
        {
            get => _maxTokensPerResponse;
            set => SetProperty(ref _maxTokensPerResponse, Math.Max(1, Math.Min(4096, value)));
        }

        public int BatchSize
        {
            get => _batchSize;
            set => SetProperty(ref _batchSize, Math.Max(1, Math.Min(20, value)));
        }

        public int StreamingChunkSize
        {
            get => _streamingChunkSize;
            set => SetProperty(ref _streamingChunkSize, Math.Max(1, Math.Min(200, value)));
        }

        public int CacheSizeMB
        {
            get => _cacheSizeMB;
            set => SetProperty(ref _cacheSizeMB, Math.Max(10, Math.Min(1000, value)));
        }

        public bool EnableCaching
        {
            get => _enableCaching;
            set => SetProperty(ref _enableCaching, value);
        }

        public bool UseCompression
        {
            get => _useCompression;
            set => SetProperty(ref _useCompression, value);
        }

        #endregion

        #region System Settings

        public bool EnableBackgroundOptimization
        {
            get => _enableBackgroundOptimization;
            set => SetProperty(ref _enableBackgroundOptimization, value);
        }

        public bool EnableGpuMonitoring
        {
            get => _enableGpuMonitoring;
            set => SetProperty(ref _enableGpuMonitoring, value);
        }

        public int MaxConcurrentRequests
        {
            get => _maxConcurrentRequests;
            set => SetProperty(ref _maxConcurrentRequests, Math.Max(1, Math.Min(16, value)));
        }

        public int RequestTimeoutSeconds
        {
            get => _requestTimeoutSeconds;
            set => SetProperty(ref _requestTimeoutSeconds, Math.Max(5, Math.Min(120, value)));
        }

        public int VramThresholdPercent
        {
            get => _vramThresholdPercent;
            set => SetProperty(ref _vramThresholdPercent, Math.Max(50, Math.Min(95, value)));
        }

        public bool EnableAutomaticModelOptimization
        {
            get => _enableAutomaticModelOptimization;
            set => SetProperty(ref _enableAutomaticModelOptimization, value);
        }

        public bool UseQuantization
        {
            get => _useQuantization;
            set => SetProperty(ref _useQuantization, value);
        }

        public bool EnableEmbeddingCaching
        {
            get => _enableEmbeddingCaching;
            set => SetProperty(ref _enableEmbeddingCaching, value);
        }

        #endregion

        #region Performance Targets

        public int CodeCompletionTargetMs
        {
            get => _codeCompletionTargetMs;
            set => SetProperty(ref _codeCompletionTargetMs, Math.Max(100, Math.Min(2000, value)));
        }

        public int ChatResponseTargetMs
        {
            get => _chatResponseTargetMs;
            set => SetProperty(ref _chatResponseTargetMs, Math.Max(500, Math.Min(10000, value)));
        }

        #endregion

        #region Advanced Settings

        public bool ShowAdvancedSettings
        {
            get => _showAdvancedSettings;
            set => SetProperty(ref _showAdvancedSettings, value);
        }

        public int CacheCleanupIntervalMinutes
        {
            get => _cacheCleanupIntervalMinutes;
            set => SetProperty(ref _cacheCleanupIntervalMinutes, Math.Max(1, Math.Min(60, value)));
        }

        public int BatchTimeoutMs
        {
            get => _batchTimeoutMs;
            set => SetProperty(ref _batchTimeoutMs, Math.Max(50, Math.Min(1000, value)));
        }

        public float Temperature
        {
            get => _temperature;
            set => SetProperty(ref _temperature, Math.Max(0.1f, Math.Min(2.0f, value)));
        }

        public int ContextWindowSize
        {
            get => _contextWindowSize;
            set => SetProperty(ref _contextWindowSize, Math.Max(512, Math.Min(16384, value)));
        }

        #endregion

        #region Commands

        public Command ApplySettingsCommand { get; }
        public Command ResetToDefaultsCommand { get; }
        public Command OptimizeForSpeedCommand { get; }
        public Command OptimizeForQualityCommand { get; }
        public Command ToggleAdvancedSettingsCommand { get; }

        #endregion

        public PerformanceSettingsViewModel()
        {
            ApplySettingsCommand = new Command(OnApplySettings);
            ResetToDefaultsCommand = new Command(OnResetToDefaults);
            OptimizeForSpeedCommand = new Command(OnOptimizeForSpeed);
            OptimizeForQualityCommand = new Command(OnOptimizeForQuality);
            ToggleAdvancedSettingsCommand = new Command(OnToggleAdvancedSettings);

            LoadCurrentSettings();
        }

        private void ApplyQualitySpeedPreset(QualitySpeedPreference preference)
        {
            switch (preference)
            {
                case QualitySpeedPreference.MaxSpeed:
                    MaxTokensPerResponse = 256;
                    StreamingChunkSize = 100;
                    BatchSize = 10;
                    UseQuantization = true;
                    ContextWindowSize = 2048;
                    Temperature = 0.5f;
                    CodeCompletionTargetMs = 300;
                    ChatResponseTargetMs = 1000;
                    break;

                case QualitySpeedPreference.MaxQuality:
                    MaxTokensPerResponse = 2048;
                    StreamingChunkSize = 25;
                    BatchSize = 2;
                    UseQuantization = false;
                    ContextWindowSize = 8192;
                    Temperature = 0.8f;
                    CodeCompletionTargetMs = 800;
                    ChatResponseTargetMs = 4000;
                    break;

                case QualitySpeedPreference.Balanced:
                default:
                    MaxTokensPerResponse = 1024;
                    StreamingChunkSize = 50;
                    BatchSize = 5;
                    UseQuantization = true;
                    ContextWindowSize = 4096;
                    Temperature = 0.7f;
                    CodeCompletionTargetMs = 500;
                    ChatResponseTargetMs = 2000;
                    break;
            }
        }

        private async void OnApplySettings()
        {
            try
            {
                var settings = CreateSettingsFromViewModel();
                await SaveSettingsAsync(settings);
                
                // Notify user
                await Shell.Current.DisplayAlert("Settings Applied", 
                    "Performance settings have been saved and will take effect immediately.", "OK");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", 
                    $"Failed to apply settings: {ex.Message}", "OK");
            }
        }

        private void OnResetToDefaults()
        {
            QualitySpeedBalance = QualitySpeedPreference.Balanced;
            EnableCaching = true;
            UseCompression = true;
            EnableBackgroundOptimization = true;
            EnableGpuMonitoring = true;
            EnableAutomaticModelOptimization = true;
            EnableEmbeddingCaching = true;
            VramThresholdPercent = 85;
            MaxConcurrentRequests = 4;
            RequestTimeoutSeconds = 30;
            CacheCleanupIntervalMinutes = 5;
            BatchTimeoutMs = 100;
        }

        private void OnOptimizeForSpeed()
        {
            QualitySpeedBalance = QualitySpeedPreference.MaxSpeed;
        }

        private void OnOptimizeForQuality()
        {
            QualitySpeedBalance = QualitySpeedPreference.MaxQuality;
        }

        private void OnToggleAdvancedSettings()
        {
            ShowAdvancedSettings = !ShowAdvancedSettings;
        }

        private PerformanceSettings CreateSettingsFromViewModel()
        {
            return new PerformanceSettings
            {
                QualitySpeedBalance = QualitySpeedBalance,
                MaxTokensPerResponse = MaxTokensPerResponse,
                BatchSize = BatchSize,
                StreamingChunkSize = StreamingChunkSize,
                CacheSizeLimitBytes = CacheSizeMB * 1024 * 1024,
                EnableCaching = EnableCaching,
                UseCompression = UseCompression,
                EnableBackgroundOptimization = EnableBackgroundOptimization,
                EnableGpuMonitoring = EnableGpuMonitoring,
                MaxConcurrentRequests = MaxConcurrentRequests,
                RequestTimeout = TimeSpan.FromSeconds(RequestTimeoutSeconds),
                VramThresholdPercent = VramThresholdPercent,
                EnableAutomaticModelOptimization = EnableAutomaticModelOptimization,
                UseQuantization = UseQuantization,
                EnableEmbeddingCaching = EnableEmbeddingCaching,
                CodeCompletionTargetTime = TimeSpan.FromMilliseconds(CodeCompletionTargetMs),
                ChatResponseTargetTime = TimeSpan.FromMilliseconds(ChatResponseTargetMs),
                CacheCleanupInterval = TimeSpan.FromMinutes(CacheCleanupIntervalMinutes),
                BatchTimeout = TimeSpan.FromMilliseconds(BatchTimeoutMs),
                Temperature = Temperature,
                ContextWindowSize = ContextWindowSize
            };
        }

        private void LoadCurrentSettings()
        {
            try
            {
                // Load from preferences or configuration
                var preferences = Preferences.Default;
                
                QualitySpeedBalance = Enum.Parse<QualitySpeedPreference>(
                    preferences.Get(nameof(QualitySpeedBalance), QualitySpeedPreference.Balanced.ToString()));
                
                MaxTokensPerResponse = preferences.Get(nameof(MaxTokensPerResponse), 1024);
                BatchSize = preferences.Get(nameof(BatchSize), 5);
                StreamingChunkSize = preferences.Get(nameof(StreamingChunkSize), 50);
                CacheSizeMB = preferences.Get(nameof(CacheSizeMB), 100);
                EnableCaching = preferences.Get(nameof(EnableCaching), true);
                UseCompression = preferences.Get(nameof(UseCompression), true);
                EnableBackgroundOptimization = preferences.Get(nameof(EnableBackgroundOptimization), true);
                EnableGpuMonitoring = preferences.Get(nameof(EnableGpuMonitoring), true);
                MaxConcurrentRequests = preferences.Get(nameof(MaxConcurrentRequests), 4);
                RequestTimeoutSeconds = preferences.Get(nameof(RequestTimeoutSeconds), 30);
                VramThresholdPercent = preferences.Get(nameof(VramThresholdPercent), 85);
                EnableAutomaticModelOptimization = preferences.Get(nameof(EnableAutomaticModelOptimization), true);
                UseQuantization = preferences.Get(nameof(UseQuantization), true);
                EnableEmbeddingCaching = preferences.Get(nameof(EnableEmbeddingCaching), true);
                CodeCompletionTargetMs = preferences.Get(nameof(CodeCompletionTargetMs), 500);
                ChatResponseTargetMs = preferences.Get(nameof(ChatResponseTargetMs), 2000);
                CacheCleanupIntervalMinutes = preferences.Get(nameof(CacheCleanupIntervalMinutes), 5);
                BatchTimeoutMs = preferences.Get(nameof(BatchTimeoutMs), 100);
                Temperature = preferences.Get(nameof(Temperature), 0.7f);
                ContextWindowSize = preferences.Get(nameof(ContextWindowSize), 4096);
            }
            catch (Exception ex)
            {
                // If loading fails, use defaults
                System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex.Message}");
            }
        }

        private async Task SaveSettingsAsync(PerformanceSettings settings)
        {
            try
            {
                var preferences = Preferences.Default;
                
                preferences.Set(nameof(QualitySpeedBalance), settings.QualitySpeedBalance.ToString());
                preferences.Set(nameof(MaxTokensPerResponse), settings.MaxTokensPerResponse);
                preferences.Set(nameof(BatchSize), settings.BatchSize);
                preferences.Set(nameof(StreamingChunkSize), settings.StreamingChunkSize);
                preferences.Set(nameof(CacheSizeMB), (int)(settings.CacheSizeLimitBytes / (1024 * 1024)));
                preferences.Set(nameof(EnableCaching), settings.EnableCaching);
                preferences.Set(nameof(UseCompression), settings.UseCompression);
                preferences.Set(nameof(EnableBackgroundOptimization), settings.EnableBackgroundOptimization);
                preferences.Set(nameof(EnableGpuMonitoring), settings.EnableGpuMonitoring);
                preferences.Set(nameof(MaxConcurrentRequests), settings.MaxConcurrentRequests);
                preferences.Set(nameof(RequestTimeoutSeconds), (int)settings.RequestTimeout.TotalSeconds);
                preferences.Set(nameof(VramThresholdPercent), settings.VramThresholdPercent);
                preferences.Set(nameof(EnableAutomaticModelOptimization), settings.EnableAutomaticModelOptimization);
                preferences.Set(nameof(UseQuantization), settings.UseQuantization);
                preferences.Set(nameof(EnableEmbeddingCaching), settings.EnableEmbeddingCaching);
                preferences.Set(nameof(CodeCompletionTargetMs), (int)settings.CodeCompletionTargetTime.TotalMilliseconds);
                preferences.Set(nameof(ChatResponseTargetMs), (int)settings.ChatResponseTargetTime.TotalMilliseconds);
                preferences.Set(nameof(CacheCleanupIntervalMinutes), (int)settings.CacheCleanupInterval.TotalMinutes);
                preferences.Set(nameof(BatchTimeoutMs), (int)settings.BatchTimeout.TotalMilliseconds);
                preferences.Set(nameof(Temperature), settings.Temperature);
                preferences.Set(nameof(ContextWindowSize), settings.ContextWindowSize);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save settings: {ex.Message}", ex);
            }
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
