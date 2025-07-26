using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Services;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace JarvisAssistant.MAUI.ViewModels
{
    /// <summary>
    /// ViewModel for the splash screen.
    /// </summary>
    public partial class SplashViewModel : BaseViewModel
    {
        private readonly IStartupService _startupService;
        private readonly IOnboardingService _onboardingService;
        private readonly ITelemetryService _telemetryService;
        private readonly INavigationService _navigationService;
        private readonly ILogger<SplashViewModel> _logger;

        [ObservableProperty]
        private bool isLoading = true;

        [ObservableProperty]
        private string statusText = "Initializing systems...";

        [ObservableProperty]
        private double progressValue = 0.0;

        [ObservableProperty]
        private string appVersion = "1.0.0";

        public SplashViewModel(
            IStartupService startupService,
            IOnboardingService onboardingService,
            ITelemetryService telemetryService,
            INavigationService navigationService,
            ILogger<SplashViewModel> logger)
        {
            _startupService = startupService ?? throw new ArgumentNullException(nameof(startupService));
            _onboardingService = onboardingService ?? throw new ArgumentNullException(nameof(onboardingService));
            _telemetryService = telemetryService ?? throw new ArgumentNullException(nameof(telemetryService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Get app version
            AppVersion = GetAppVersion();
        }

        /// <summary>
        /// Initializes the splash screen and starts the application startup process.
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Starting application initialization from splash screen");

                // Create progress reporter
                var progress = new Progress<StartupProgress>(OnStartupProgress);

                // Start initialization
                var result = await _startupService.InitializeAsync(progress);

                if (result.IsSuccess)
                {
                    await OnStartupCompleted(result);
                }
                else
                {
                    await OnStartupFailed(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error during application startup");
                await OnStartupFailed(new StartupResult
                {
                    IsSuccess = false,
                    Error = ex,
                    Duration = TimeSpan.Zero,
                    IsFirstRun = _startupService.IsFirstRun
                });
            }
        }

        private void OnStartupProgress(StartupProgress progress)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusText = progress.Message;
                ProgressValue = progress.Percentage / 100.0;
                _logger.LogDebug("Startup progress: {Message} ({Percentage}%)", progress.Message, progress.Percentage);
            });
        }

        private async Task OnStartupCompleted(StartupResult result)
        {
            _logger.LogInformation("Application startup completed successfully in {Duration}ms", result.Duration.TotalMilliseconds);

            // Track successful startup
            await _telemetryService.TrackEventAsync("SplashScreenCompleted", new Dictionary<string, object>
            {
                ["success"] = true,
                ["duration"] = result.Duration.TotalMilliseconds,
                ["isFirstRun"] = result.IsFirstRun
            });

            // Update UI to show completion
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusText = "Ready!";
                ProgressValue = 1.0;
            });

            // Small delay for smooth transition
            await Task.Delay(500);

            // Navigate to appropriate screen
            await NavigateToNextScreen(result.IsFirstRun);
        }

        private async Task OnStartupFailed(StartupResult result)
        {
            _logger.LogError("Application startup failed: {Error}", result.Error?.Message);

            // Track failed startup
            await _telemetryService.TrackEventAsync("SplashScreenFailed", new Dictionary<string, object>
            {
                ["success"] = false,
                ["error"] = result.Error?.Message ?? "Unknown error",
                ["duration"] = result.Duration.TotalMilliseconds,
                ["isFirstRun"] = result.IsFirstRun
            });

            // Update UI to show error
            MainThread.BeginInvokeOnMainThread(() =>
            {
                StatusText = "Startup failed. Please restart the application.";
                ProgressValue = 0.0;
                IsLoading = false;
            });

            // In a real app, you might want to show an error dialog or retry option
            await Task.Delay(3000);

            // For now, try to navigate to main screen anyway
            await NavigateToNextScreen(result.IsFirstRun);
        }

        private async Task NavigateToNextScreen(bool isFirstRun)
        {
            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    IsLoading = false;
                });

                if (isFirstRun || _onboardingService.IsOnboardingRequired)
                {
                    // Navigate to onboarding
                    await _navigationService.NavigateToAsync("OnboardingPage");
                    _logger.LogInformation("Navigated to onboarding screen");
                }
                else
                {
                    // Navigate to main application
                    await _navigationService.NavigateToAsync("MainPage");
                    _logger.LogInformation("Navigated to main application screen");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating from splash screen");
                
                // Fallback navigation
                try
                {
                    await _navigationService.NavigateToAsync("MainPage");
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogCritical(fallbackEx, "Critical error: Unable to navigate from splash screen");
                }
            }
        }

        private string GetAppVersion()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                return version?.ToString(3) ?? "1.0.0";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not determine app version");
                return "1.0.0";
            }
        }

        [RelayCommand]
        private async Task RetryStartup()
        {
            if (!IsLoading)
            {
                IsLoading = true;
                StatusText = "Retrying initialization...";
                ProgressValue = 0.0;
                
                await InitializeAsync();
            }
        }
    }
}
