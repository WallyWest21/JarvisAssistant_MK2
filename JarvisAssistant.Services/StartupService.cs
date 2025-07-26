using JarvisAssistant.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace JarvisAssistant.Services
{
    /// <summary>
    /// Service for managing application startup and first-run experience.
    /// </summary>
    public class StartupService : IStartupService
    {
        private readonly ILogger<StartupService> _logger;
        private readonly ITelemetryService _telemetryService;
        private readonly IPreferencesService _preferencesService;
        private readonly IServiceProvider _serviceProvider;
        private readonly List<IStartupTask> _startupTasks;

        public StartupService(
            ILogger<StartupService> logger,
            ITelemetryService telemetryService,
            IPreferencesService preferencesService,
            IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _telemetryService = telemetryService ?? throw new ArgumentNullException(nameof(telemetryService));
            _preferencesService = preferencesService ?? throw new ArgumentNullException(nameof(preferencesService));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _startupTasks = new List<IStartupTask>();
        }

        /// <inheritdoc/>
        public bool IsFirstRun => !_preferencesService.Get("app_initialized", false);

        /// <inheritdoc/>
        public async Task<StartupResult> InitializeAsync(IProgress<StartupProgress>? progress = null)
        {
            var startTime = DateTime.UtcNow;
            _logger.LogInformation("Starting application initialization");

            try
            {
                progress?.Report(new StartupProgress("Initializing core services...", 0, 100));
                await _telemetryService.TrackEventAsync("AppStartup", new Dictionary<string, object>
                {
                    ["isFirstRun"] = IsFirstRun,
                    ["startTime"] = startTime
                });

                // Initialize core services
                var coreProgress = 0;
                var totalTasks = _startupTasks.Count + 3; // Core tasks + startup tasks

                // Task 1: Initialize preferences
                progress?.Report(new StartupProgress("Loading preferences...", ++coreProgress * 100 / totalTasks, 100));
                await InitializePreferencesAsync();

                // Task 2: Initialize telemetry
                progress?.Report(new StartupProgress("Configuring telemetry...", ++coreProgress * 100 / totalTasks, 100));
                await InitializeTelemetryAsync();

                // Task 3: Run startup tasks
                foreach (var task in _startupTasks)
                {
                    progress?.Report(new StartupProgress($"Running {task.GetType().Name}...", ++coreProgress * 100 / totalTasks, 100));
                    await task.ExecuteAsync();
                }

                // Final setup
                progress?.Report(new StartupProgress("Finalizing startup...", 90, 100));
                await FinalizeStartupAsync();

                progress?.Report(new StartupProgress("Ready!", 100, 100));

                var duration = DateTime.UtcNow - startTime;
                _logger.LogInformation("Application initialization completed in {Duration}ms", duration.TotalMilliseconds);

                await _telemetryService.TrackMetricAsync("AppStartupDuration", duration.TotalMilliseconds);
                await _telemetryService.TrackEventAsync("AppStartupCompleted", new Dictionary<string, object>
                {
                    ["duration"] = duration.TotalMilliseconds,
                    ["isFirstRun"] = IsFirstRun
                });

                return new StartupResult
                {
                    IsSuccess = true,
                    Duration = duration,
                    IsFirstRun = IsFirstRun
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during application initialization");
                await _telemetryService.TrackExceptionAsync(ex, new Dictionary<string, string>
                {
                    ["context"] = "AppStartup"
                });

                return new StartupResult
                {
                    IsSuccess = false,
                    Duration = DateTime.UtcNow - startTime,
                    Error = ex,
                    IsFirstRun = IsFirstRun
                };
            }
        }

        /// <inheritdoc/>
        public void RegisterStartupTask(IStartupTask task)
        {
            _startupTasks.Add(task);
            _logger.LogDebug("Registered startup task: {TaskType}", task.GetType().Name);
        }

        /// <inheritdoc/>
        public async Task CompleteFirstRunAsync()
        {
            _preferencesService.Set("app_initialized", true);
            _preferencesService.Set("first_run_completed", DateTime.UtcNow.ToString("O"));
            
            await _telemetryService.TrackEventAsync("FirstRunCompleted", new Dictionary<string, object>
            {
                ["completedAt"] = DateTime.UtcNow
            });

            _logger.LogInformation("First run completed");
        }

        private async Task InitializePreferencesAsync()
        {
            try
            {
                // Set default preferences if first run
                if (IsFirstRun)
                {
                    _preferencesService.Set("theme", "auto");
                    _preferencesService.Set("voice_enabled", true);
                    _preferencesService.Set("notifications_enabled", true);
                    _preferencesService.Set("performance_mode", "balanced");
                }

                _logger.LogDebug("Preferences initialized");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing preferences");
                throw;
            }
        }

        private async Task InitializeTelemetryAsync()
        {
            try
            {
                var settings = await _telemetryService.GetSettingsAsync();
                
                // Set default telemetry settings for first run
                if (IsFirstRun)
                {
                    settings.IsEnabled = true; // User can opt-out later
                    settings.EnableUsageAnalytics = true;
                    settings.EnableErrorReporting = true;
                    settings.EnablePerformanceMetrics = true;
                    settings.EnableFeatureTracking = true;
                    settings.UseAnonymousId = true;
                    
                    await _telemetryService.UpdateSettingsAsync(settings);
                }

                _logger.LogDebug("Telemetry initialized. Enabled: {IsEnabled}", settings.IsEnabled);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing telemetry");
                throw;
            }
        }

        private async Task FinalizeStartupAsync()
        {
            try
            {
                // Perform any final cleanup or setup
                await Task.Delay(100); // Small delay for UI smoothness
                
                // Trigger garbage collection after startup
                GC.Collect();
                GC.WaitForPendingFinalizers();

                _logger.LogDebug("Startup finalization completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during startup finalization");
                throw;
            }
        }
    }
}
