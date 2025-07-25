using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models;
using Microsoft.Extensions.Logging;

namespace JarvisAssistant.MAUI.ViewModels
{
    /// <summary>
    /// ViewModel for the status panel view with real-time updates.
    /// </summary>
    public partial class StatusPanelViewModel : ObservableObject, IStatusPanelViewModel, IDisposable
    {
        private readonly IStatusMonitorService _statusMonitorService;
        private readonly IDialogService _dialogService;
        private readonly ILogger<StatusPanelViewModel> _logger;
        private IDisposable? _statusSubscription;
        private bool _disposed;

        [ObservableProperty]
        private ObservableCollection<ServiceStatus> _serviceStatuses = new();

        [ObservableProperty]
        private bool _isExpanded;

        [ObservableProperty]
        private DateTime _lastUpdated = DateTime.Now;

        [ObservableProperty]
        private ServiceState _overallStatus = ServiceState.Offline;

        [ObservableProperty]
        private string _statusSummary = "Loading...";

        [ObservableProperty]
        private bool _isLoading;

        public StatusPanelViewModel(
            IStatusMonitorService? statusMonitorService,
            IDialogService? dialogService,
            ILogger<StatusPanelViewModel>? logger)
        {
            _statusMonitorService = statusMonitorService;
            _dialogService = dialogService;
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<StatusPanelViewModel>.Instance;

            _logger.LogInformation("StatusPanelViewModel constructor called");

            // Add immediate test data to verify UI is working
            System.Diagnostics.Debug.WriteLine("=== StatusPanelViewModel: Adding immediate test data ===");
            AddImmediateTestData();

            // Subscribe to real-time status updates if service is available
            if (_statusMonitorService != null)
            {
                try
                {
                    _statusSubscription = _statusMonitorService.ServiceStatusUpdates
                        .Subscribe(OnServiceStatusUpdated);
                    _logger.LogInformation("Successfully subscribed to status updates");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to subscribe to status updates");
                }
            }
            else
            {
                _logger.LogWarning("IStatusMonitorService is null - status monitoring will not be available");
            }

            // Initialize with current status
            _ = Task.Run(LoadInitialStatusAsync);

            // TEST: Manually test commands after a delay to see if they work
            _ = Task.Run(async () =>
            {
                await Task.Delay(3000); // Wait 3 seconds
                System.Diagnostics.Debug.WriteLine("=== MANUAL COMMAND TEST STARTING ===");

                try
                {
                    // Test if we can call the command directly
                    if (ToggleExpandedCommand.CanExecute(null))
                    {
                        System.Diagnostics.Debug.WriteLine("=== ToggleExpandedCommand.CanExecute returned TRUE ===");
                        ToggleExpandedCommand.Execute(null);
                        System.Diagnostics.Debug.WriteLine("=== ToggleExpandedCommand.Execute called ===");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("=== ToggleExpandedCommand.CanExecute returned FALSE ===");
                    }

                    // Test TestCommandCommand if available
                    if (TestCommandCommand.CanExecute(null))
                    {
                        System.Diagnostics.Debug.WriteLine("=== TestCommandCommand.CanExecute returned TRUE ===");
                        TestCommandCommand.Execute(null);
                        System.Diagnostics.Debug.WriteLine("=== TestCommandCommand.Execute called ===");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("=== TestCommandCommand.CanExecute returned FALSE ===");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"=== ERROR in manual command test: {ex} ===");
                }
            });
        }

        /// <summary>
        /// Refreshes all service statuses.
        /// </summary>
        [RelayCommand]
        public async Task RefreshStatusAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;

                if (_statusMonitorService != null)
                {
                    var statuses = await _statusMonitorService.GetAllServiceStatusesAsync();

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        ServiceStatuses.Clear();
                        foreach (var status in statuses.OrderBy(s => s.ServiceName))
                        {
                            ServiceStatuses.Add(status);
                        }

                        UpdateOverallStatus();
                        LastUpdated = DateTime.Now;
                    });

                    _logger.LogInformation("Refreshed {Count} service statuses", statuses.Count());
                }
                else
                {
                    _logger.LogWarning("Cannot refresh statuses - IStatusMonitorService is null");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh service statuses");
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Toggles the expanded state of the mobile panel.
        /// </summary>
        [RelayCommand]
        public void ToggleExpanded()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== ToggleExpanded called. Current IsExpanded: {IsExpanded} ===");
                System.Diagnostics.Debug.WriteLine($"=== Current Thread: {Thread.CurrentThread.Name ?? "Unknown"} ===");

                IsExpanded = !IsExpanded;

                System.Diagnostics.Debug.WriteLine($"=== ToggleExpanded completed. New IsExpanded: {IsExpanded} ===");
                _logger.LogInformation("Status panel expanded state toggled to: {IsExpanded}", IsExpanded);

                // Force property change notification
                OnPropertyChanged(nameof(IsExpanded));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? ERROR in ToggleExpanded: {ex}");
                _logger.LogError(ex, "Error in ToggleExpanded");
            }
        }

        /// <summary>
        /// Simple test command to verify command binding is working
        /// </summary>
        [RelayCommand]
        public void TestCommand()
        {
            System.Diagnostics.Debug.WriteLine("?????? TEST COMMAND EXECUTED - COMMAND BINDING IS WORKING! ??????");
            _logger.LogInformation("Test command executed successfully");

            // Also test toggling IsExpanded directly
            IsExpanded = !IsExpanded;
            System.Diagnostics.Debug.WriteLine($"Test command toggled IsExpanded to: {IsExpanded}");
        }

        /// <summary>
        /// Shows detailed information for a specific service.
        /// </summary>
        [RelayCommand]
        public async Task ShowServiceDetailsAsync(ServiceStatus serviceStatus)
        {
            if (serviceStatus == null) return;

            try
            {
                var message = FormatServiceDetails(serviceStatus);

                if (_dialogService != null)
                {
                    // If service has failures, show option to reset
                    if (serviceStatus.Metrics?.TryGetValue("consecutive_failures", out var failures) == true &&
                        Convert.ToInt32(failures) > 0)
                    {
                        var shouldReset = await _dialogService.DisplayConfirmAsync(
                            $"Service Details: {serviceStatus.ServiceName}",
                            $"{message}\n\nThis service has consecutive failures. Would you like to reset the failure count and retry immediately?",
                            "Reset & Retry",
                            "Close");

                        if (shouldReset)
                        {
                            await ResetServiceFailuresAsync(serviceStatus);
                        }
                    }
                    else
                    {
                        await _dialogService.DisplayAlertAsync(
                            $"Service Details: {serviceStatus.ServiceName}",
                            message,
                            "OK");
                    }
                }
                else
                {
                    _logger.LogInformation("Service Details for {ServiceName}: {Details}", serviceStatus.ServiceName, message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show service details for {ServiceName}", serviceStatus.ServiceName);
            }
        }

        /// <summary>
        /// Resets failures for a specific service.
        /// </summary>
        [RelayCommand]
        public async Task ResetServiceFailuresAsync(ServiceStatus serviceStatus)
        {
            if (serviceStatus == null) return;

            try
            {
                IsLoading = true;

                if (_statusMonitorService != null)
                {
                    await _statusMonitorService.ResetServiceFailuresAsync(serviceStatus.ServiceName);

                    if (_dialogService != null)
                    {
                        await _dialogService.DisplayAlertAsync(
                            "Service Reset",
                            $"Failures reset for {serviceStatus.ServiceName}. The service will be rechecked immediately.",
                            "OK");
                    }
                }
                else
                {
                    _logger.LogWarning("Cannot reset service failures - IStatusMonitorService is null");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset failures for {ServiceName}", serviceStatus.ServiceName);

                if (_dialogService != null)
                {
                    await _dialogService.DisplayAlertAsync(
                        "Error",
                        $"Failed to reset failures for {serviceStatus.ServiceName}: {ex.Message}",
                        "OK");
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Opens the settings page.
        /// </summary>
        [RelayCommand]
        private async Task OpenSettingsAsync()
        {
            try
            {
                _logger.LogInformation("Opening service monitoring settings");

                if (_dialogService != null)
                {
                    await _dialogService.DisplayAlertAsync(
                        "Settings",
                        "Service monitoring settings will be available in a future update.",
                        "OK");
                }
                else
                {
                    _logger.LogInformation("Settings requested but dialog service not available");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open settings");
            }
        }

        /// <summary>
        /// Loads the initial service status data.
        /// </summary>
        private async Task LoadInitialStatusAsync()
        {
            try
            {
                await RefreshStatusAsync();

                // If no services were loaded, add some test data for debugging
                if (!ServiceStatuses.Any())
                {
                    _logger.LogWarning("No services found from status monitor service. Adding test data for debugging.");
                    await AddTestDataAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load initial service status. Adding test data for debugging.");
                await AddTestDataAsync();
            }
        }

        /// <summary>
        /// Adds immediate test data in the constructor for debugging.
        /// </summary>
        private void AddImmediateTestData()
        {
            try
            {
                // Check if ElevenLabs is configured
                var elevenLabsApiKey = Environment.GetEnvironmentVariable("ELEVENLABS_API_KEY");
                bool hasElevenLabs = !string.IsNullOrWhiteSpace(elevenLabsApiKey);

                // Add test services immediately to show that the UI is working
                var testServices = new[]
                {
                    new ServiceStatus("llm-engine", ServiceState.Offline)
                    {
                        ErrorMessage = "Connection refused",
                        Metrics = new Dictionary<string, object>
                        {
                            ["error_code"] = "SRV-CONN-001",
                            ["response_time_ms"] = 0,
                            ["consecutive_failures"] = 3,
                            ["platform"] = "Android",
                            ["endpoint"] = "http://10.0.2.2:11434/api/tags"
                        }
                    },
                    new ServiceStatus("vision-api", ServiceState.Offline)
                    {
                        ErrorMessage = "Service not found",
                        Metrics = new Dictionary<string, object>
                        {
                            ["error_code"] = "HTTP-404-001",
                            ["response_time_ms"] = 0,
                            ["consecutive_failures"] = 1,
                            ["platform"] = "Android",
                            ["endpoint"] = "http://localhost:5000/health"
                        }
                    },
                    new ServiceStatus("voice-service", hasElevenLabs ? ServiceState.Online : ServiceState.Offline)
                    {
                        ErrorMessage = hasElevenLabs ? null : "No API key configured",
                        Metrics = new Dictionary<string, object>
                        {
                            ["service_type"] = hasElevenLabs ? "ElevenLabs" : "Stub",
                            ["response_time_ms"] = hasElevenLabs ? 120 : 0,
                            ["consecutive_failures"] = hasElevenLabs ? 0 : 1,
                            ["platform"] = "Windows",
                            ["api_configured"] = hasElevenLabs
                        }
                    },
                    new ServiceStatus("system-health", ServiceState.Online)
                    {
                        Metrics = new Dictionary<string, object>
                        {
                            ["response_time_ms"] = 45,
                            ["consecutive_failures"] = 0,
                            ["platform"] = "Android",
                            ["status_code"] = 200
                        }
                    }
                };

                foreach (var service in testServices)
                {
                    ServiceStatuses.Add(service);
                }

                UpdateOverallStatus();
                LastUpdated = DateTime.Now;

                // Debug the IsExpanded state
                System.Diagnostics.Debug.WriteLine($"=== Initial IsExpanded state: {IsExpanded} ===");

                System.Diagnostics.Debug.WriteLine($"? Added {testServices.Length} immediate test services to status panel");
                _logger.LogInformation("Added {Count} immediate test services to status panel", testServices.Length);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error adding immediate test data: {ex}");
                _logger.LogError(ex, "Error adding immediate test data");
            }
        }

        /// <summary>
        /// Adds test service status data for debugging purposes.
        /// </summary>
        private async Task AddTestDataAsync()
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ServiceStatuses.Clear();

                // Add test services to show that the UI is working
                var testServices = new[]
                {
                    new ServiceStatus("llm-engine", ServiceState.Offline)
                    {
                        ErrorMessage = "Connection refused",
                        Metrics = new Dictionary<string, object>
                        {
                            ["error_code"] = "SRV-CONN-001",
                            ["response_time_ms"] = 0,
                            ["consecutive_failures"] = 3,
                            ["platform"] = "Android",
                            ["endpoint"] = "http://10.0.2.2:11434/api/tags"
                        }
                    },
                    new ServiceStatus("vision-api", ServiceState.Offline)
                    {
                        ErrorMessage = "Service not found",
                        Metrics = new Dictionary<string, object>
                        {
                            ["error_code"] = "HTTP-404-001",
                            ["response_time_ms"] = 0,
                            ["consecutive_failures"] = 1,
                            ["platform"] = "Android",
                            ["endpoint"] = "http://localhost:5000/health"
                        }
                    },
                    new ServiceStatus("voice-service", ServiceState.Offline)
                    {
                        ErrorMessage = "Connection timeout",
                        Metrics = new Dictionary<string, object>
                        {
                            ["error_code"] = "SRV-TIMEOUT-001",
                            ["response_time_ms"] = 5000,
                            ["consecutive_failures"] = 2,
                            ["platform"] = "Android",
                            ["endpoint"] = "http://localhost:5001/health"
                        }
                    },
                    new ServiceStatus("system-health", ServiceState.Online)
                    {
                        Metrics = new Dictionary<string, object>
                        {
                            ["response_time_ms"] = 45,
                            ["consecutive_failures"] = 0,
                            ["platform"] = "Android",
                            ["status_code"] = 200
                        }
                    }
                };

                foreach (var service in testServices)
                {
                    ServiceStatuses.Add(service);
                }

                UpdateOverallStatus();
                LastUpdated = DateTime.Now;

                _logger.LogInformation("Added {Count} test services to status panel", testServices.Length);
            });
        }

        /// <summary>
        /// Handles real-time service status updates.
        /// </summary>
        private void OnServiceStatusUpdated(ServiceStatus updatedStatus)
        {
            try
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    // Find and update existing status or add new one
                    var existingStatus = ServiceStatuses.FirstOrDefault(s => s.ServiceName == updatedStatus.ServiceName);
                    if (existingStatus != null)
                    {
                        var index = ServiceStatuses.IndexOf(existingStatus);
                        ServiceStatuses[index] = updatedStatus;
                    }
                    else
                    {
                        // Insert in alphabetical order
                        var insertIndex = ServiceStatuses
                            .Select((status, index) => new { status, index })
                            .FirstOrDefault(x => string.Compare(x.status.ServiceName, updatedStatus.ServiceName, StringComparison.OrdinalIgnoreCase) > 0)?.index ?? ServiceStatuses.Count;

                        ServiceStatuses.Insert(insertIndex, updatedStatus);
                    }

                    UpdateOverallStatus();
                    LastUpdated = DateTime.Now;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle service status update for {ServiceName}", updatedStatus.ServiceName);
            }
        }

        /// <summary>
        /// Updates the overall status based on all service statuses.
        /// </summary>
        private void UpdateOverallStatus()
        {
            System.Diagnostics.Debug.WriteLine($"=== UpdateOverallStatus called. ServiceStatuses count: {ServiceStatuses.Count} ===");

            if (!ServiceStatuses.Any())
            {
                OverallStatus = ServiceState.Offline;
                StatusSummary = "No services";
                System.Diagnostics.Debug.WriteLine("=== No services found, setting status summary to 'No services' ===");
                return;
            }

            var states = ServiceStatuses.Select(s => s.State).ToList();
            var onlineCount = states.Count(s => s == ServiceState.Online);
            var degradedCount = states.Count(s => s == ServiceState.Degraded);
            var offlineCount = states.Count(s => s == ServiceState.Offline || s == ServiceState.Error);

            System.Diagnostics.Debug.WriteLine($"=== Service counts - Online: {onlineCount}, Degraded: {degradedCount}, Offline: {offlineCount} ===");

            // Determine overall status
            if (offlineCount > 0)
            {
                OverallStatus = ServiceState.Error;
                StatusSummary = $"{offlineCount} service{(offlineCount == 1 ? "" : "s")} offline";
            }
            else if (degradedCount > 0)
            {
                OverallStatus = ServiceState.Degraded;
                StatusSummary = $"{degradedCount} degraded, {onlineCount} online";
            }
            else
            {
                OverallStatus = ServiceState.Online;
                StatusSummary = $"All {onlineCount} services online";
            }

            System.Diagnostics.Debug.WriteLine($"=== Final status summary: '{StatusSummary}', Overall status: {OverallStatus} ===");
        }

        /// <summary>
        /// Formats detailed service information for display.
        /// </summary>
        private static string FormatServiceDetails(ServiceStatus status)
        {
            var details = new List<string>
            {
                $"Status: {status.State}",
                $"Last Check: {status.LastHeartbeat:yyyy-MM-dd HH:mm:ss}"
            };

            if (status.Metrics != null)
            {
                if (status.Metrics.TryGetValue("response_time_ms", out var responseTime))
                    details.Add($"Response Time: {responseTime}ms");

                if (status.Metrics.TryGetValue("consecutive_failures", out var failures))
                    details.Add($"Consecutive Failures: {failures}");

                if (status.Metrics.TryGetValue("error_code", out var errorCode))
                    details.Add($"Error Code: {errorCode}");

                if (status.Metrics.TryGetValue("status_code", out var statusCode))
                    details.Add($"HTTP Status: {statusCode}");

                if (status.Metrics.TryGetValue("platform", out var platform))
                    details.Add($"Platform: {platform}");

                if (status.Metrics.TryGetValue("endpoint", out var endpoint))
                    details.Add($"Endpoint: {endpoint}");
            }

            if (!string.IsNullOrEmpty(status.ErrorMessage))
                details.Add($"Error: {status.ErrorMessage}");

            if (status.Uptime.HasValue)
                details.Add($"Uptime: {status.Uptime.Value:dd\\.hh\\:mm\\:ss}");

            if (!string.IsNullOrEmpty(status.Version))
                details.Add($"Version: {status.Version}");

            return string.Join("\n", details);
        }

        /// <summary>
        /// Disposes of the view model and releases resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            _statusSubscription?.Dispose();
            _disposed = true;
        }
    }
}
