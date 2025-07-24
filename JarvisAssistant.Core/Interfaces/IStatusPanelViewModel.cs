using System.Collections.ObjectModel;
using JarvisAssistant.Core.Models;

namespace JarvisAssistant.Core.Interfaces
{
    /// <summary>
    /// Interface for the status panel view model to enable testing.
    /// </summary>
    public interface IStatusPanelViewModel
    {
        /// <summary>
        /// Gets the collection of service statuses.
        /// </summary>
        ObservableCollection<ServiceStatus> ServiceStatuses { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the panel is expanded.
        /// </summary>
        bool IsExpanded { get; set; }

        /// <summary>
        /// Gets the last updated timestamp.
        /// </summary>
        DateTime LastUpdated { get; }

        /// <summary>
        /// Gets the overall status of all services.
        /// </summary>
        ServiceState OverallStatus { get; }

        /// <summary>
        /// Gets the status summary text.
        /// </summary>
        string StatusSummary { get; }

        /// <summary>
        /// Gets a value indicating whether the view model is currently loading.
        /// </summary>
        bool IsLoading { get; }

        /// <summary>
        /// Refreshes all service statuses.
        /// </summary>
        /// <returns>A task representing the refresh operation.</returns>
        Task RefreshStatusAsync();

        /// <summary>
        /// Toggles the expanded state of the panel.
        /// </summary>
        void ToggleExpanded();

        /// <summary>
        /// Shows detailed information for a specific service.
        /// </summary>
        /// <param name="serviceStatus">The service status to show details for.</param>
        /// <returns>A task representing the operation.</returns>
        Task ShowServiceDetailsAsync(ServiceStatus serviceStatus);
    }
}