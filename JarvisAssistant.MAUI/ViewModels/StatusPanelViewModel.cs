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
            IStatusMonitorService statusMonitorService, 
            IDialogService dialogService,
            ILogger<StatusPanelViewModel> logger)
        {
            _statusMonitorService = statusMonitorService;
            _dialogService = dialogService;
            _logger = logger;

            // Subscribe to real-time status updates
            _statusSubscription = _statusMonitorService.ServiceStatusUpdates
                .Subscribe(OnServiceStatusUpdated);

            // Initialize with current status
            _ = Task.Run(LoadInitialStatusAsync);
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
            IsExpanded = !IsExpanded;
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
                await _statusMonitorService.ResetServiceFailuresAsync(serviceStatus.ServiceName);
                
                await _dialogService.DisplayAlertAsync(
                    "Service Reset",
                    $"Failures reset for {serviceStatus.ServiceName}. The service will be rechecked immediately.",
                    "OK");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset failures for {ServiceName}", serviceStatus.ServiceName);
                await _dialogService.DisplayAlertAsync(
                    "Error",
                    $"Failed to reset failures for {serviceStatus.ServiceName}: {ex.Message}",
                    "OK");
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
                // Navigate to settings page - implement based on your navigation pattern
                _logger.LogInformation("Opening service monitoring settings");
                await _dialogService.DisplayAlertAsync(
                    "Settings",
                    "Service monitoring settings will be available in a future update.",
                    "OK");
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load initial service status");
            }
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
            if (!ServiceStatuses.Any())
            {
                OverallStatus = ServiceState.Offline;
                StatusSummary = "No services";
                return;
            }

            var states = ServiceStatuses.Select(s => s.State).ToList();
            var onlineCount = states.Count(s => s == ServiceState.Online);
            var degradedCount = states.Count(s => s == ServiceState.Degraded);
            var offlineCount = states.Count(s => s == ServiceState.Offline || s == ServiceState.Error);

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
