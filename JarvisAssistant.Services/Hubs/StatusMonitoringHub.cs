using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using JarvisAssistant.Core.Models;

namespace JarvisAssistant.Services.Hubs
{
    /// <summary>
    /// SignalR hub client for real-time service status monitoring.
    /// </summary>
    public class StatusMonitoringHub
    {
        private readonly ILogger<StatusMonitoringHub> _logger;
        private HubConnection? _hubConnection;
        private readonly Dictionary<string, Action<ServiceStatus>> _statusHandlers;
        private bool _disposed;

        public StatusMonitoringHub(ILogger<StatusMonitoringHub> logger)
        {
            _logger = logger;
            _statusHandlers = new Dictionary<string, Action<ServiceStatus>>();
        }

        /// <summary>
        /// Event fired when connected to the hub.
        /// </summary>
        public event EventHandler? Connected;

        /// <summary>
        /// Event fired when disconnected from the hub.
        /// </summary>
        public event EventHandler? Disconnected;

        /// <summary>
        /// Event fired when reconnecting to the hub.
        /// </summary>
        public event EventHandler? Reconnecting;

        /// <summary>
        /// Event fired when reconnected to the hub.
        /// </summary>
        public event EventHandler<string>? Reconnected;

        /// <summary>
        /// Gets the current connection state.
        /// </summary>
        public HubConnectionState ConnectionState => _hubConnection?.State ?? HubConnectionState.Disconnected;

        /// <summary>
        /// Initializes the SignalR connection.
        /// </summary>
        public async Task InitializeAsync(string hubUrl)
        {
            try
            {
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(hubUrl)
                    .WithAutomaticReconnect(new[] { 
                        TimeSpan.Zero, 
                        TimeSpan.FromSeconds(2), 
                        TimeSpan.FromSeconds(5), 
                        TimeSpan.FromSeconds(10),
                        TimeSpan.FromSeconds(30)
                    })
                    .Build();

                // Register event handlers
                _hubConnection.Closed += OnConnectionClosed;
                _hubConnection.Reconnecting += OnReconnecting;
                _hubConnection.Reconnected += OnReconnected;

                // Register method handlers
                _hubConnection.On<string, ServiceStatus>("ServiceStatusUpdated", OnServiceStatusUpdated);
                _hubConnection.On<string>("ServiceAdded", OnServiceAdded);
                _hubConnection.On<string>("ServiceRemoved", OnServiceRemoved);

                await _hubConnection.StartAsync();
                Connected?.Invoke(this, EventArgs.Empty);
                _logger.LogInformation("StatusMonitoringHub connection established");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize StatusMonitoringHub connection");
                throw;
            }
        }

        /// <summary>
        /// Subscribes to status updates for a specific service.
        /// </summary>
        public async Task SubscribeToServiceAsync(string serviceName, Action<ServiceStatus> statusHandler)
        {
            if (_hubConnection?.State != HubConnectionState.Connected)
            {
                _logger.LogWarning("Cannot subscribe to service {ServiceName} - hub not connected", serviceName);
                return;
            }

            try
            {
                _statusHandlers[serviceName] = statusHandler;
                await _hubConnection.InvokeAsync("SubscribeToService", serviceName);
                _logger.LogDebug("Subscribed to service status updates: {ServiceName}", serviceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to subscribe to service {ServiceName}", serviceName);
            }
        }

        /// <summary>
        /// Unsubscribes from status updates for a specific service.
        /// </summary>
        public async Task UnsubscribeFromServiceAsync(string serviceName)
        {
            if (_hubConnection?.State != HubConnectionState.Connected)
            {
                return;
            }

            try
            {
                _statusHandlers.Remove(serviceName);
                await _hubConnection.InvokeAsync("UnsubscribeFromService", serviceName);
                _logger.LogDebug("Unsubscribed from service status updates: {ServiceName}", serviceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unsubscribe from service {ServiceName}", serviceName);
            }
        }

        /// <summary>
        /// Broadcasts a service status update to all connected clients.
        /// </summary>
        public async Task BroadcastServiceStatusAsync(string serviceName, ServiceStatus status)
        {
            if (_hubConnection?.State != HubConnectionState.Connected)
            {
                _logger.LogWarning("Cannot broadcast status for {ServiceName} - hub not connected", serviceName);
                return;
            }

            try
            {
                await _hubConnection.InvokeAsync("BroadcastServiceStatus", serviceName, status);
                _logger.LogDebug("Broadcasted status update for service: {ServiceName}", serviceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast status for service {ServiceName}", serviceName);
            }
        }

        /// <summary>
        /// Requests the current status of all services.
        /// </summary>
        public async Task<IEnumerable<ServiceStatus>> GetAllServiceStatusesAsync()
        {
            if (_hubConnection?.State != HubConnectionState.Connected)
            {
                _logger.LogWarning("Cannot get service statuses - hub not connected");
                return Enumerable.Empty<ServiceStatus>();
            }

            try
            {
                var statuses = await _hubConnection.InvokeAsync<IEnumerable<ServiceStatus>>("GetAllServiceStatuses");
                _logger.LogDebug("Retrieved {Count} service statuses from hub", statuses.Count());
                return statuses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get service statuses from hub");
                return Enumerable.Empty<ServiceStatus>();
            }
        }

        /// <summary>
        /// Joins a monitoring group for specific types of updates.
        /// </summary>
        public async Task JoinMonitoringGroupAsync(string groupName)
        {
            if (_hubConnection?.State != HubConnectionState.Connected)
            {
                return;
            }

            try
            {
                await _hubConnection.InvokeAsync("JoinGroup", groupName);
                _logger.LogDebug("Joined monitoring group: {GroupName}", groupName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to join monitoring group {GroupName}", groupName);
            }
        }

        /// <summary>
        /// Leaves a monitoring group.
        /// </summary>
        public async Task LeaveMonitoringGroupAsync(string groupName)
        {
            if (_hubConnection?.State != HubConnectionState.Connected)
            {
                return;
            }

            try
            {
                await _hubConnection.InvokeAsync("LeaveGroup", groupName);
                _logger.LogDebug("Left monitoring group: {GroupName}", groupName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to leave monitoring group {GroupName}", groupName);
            }
        }

        /// <summary>
        /// Handles incoming service status updates.
        /// </summary>
        private void OnServiceStatusUpdated(string serviceName, ServiceStatus status)
        {
            try
            {
                if (_statusHandlers.TryGetValue(serviceName, out var handler))
                {
                    handler(status);
                }
                _logger.LogDebug("Received status update for service: {ServiceName} - {State}", 
                    serviceName, status.State);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling status update for service {ServiceName}", serviceName);
            }
        }

        /// <summary>
        /// Handles service added notifications.
        /// </summary>
        private void OnServiceAdded(string serviceName)
        {
            _logger.LogInformation("New service added to monitoring: {ServiceName}", serviceName);
        }

        /// <summary>
        /// Handles service removed notifications.
        /// </summary>
        private void OnServiceRemoved(string serviceName)
        {
            _statusHandlers.Remove(serviceName);
            _logger.LogInformation("Service removed from monitoring: {ServiceName}", serviceName);
        }

        /// <summary>
        /// Handles connection closed events.
        /// </summary>
        private Task OnConnectionClosed(Exception? exception)
        {
            Disconnected?.Invoke(this, EventArgs.Empty);
            if (exception != null)
            {
                _logger.LogWarning(exception, "StatusMonitoringHub connection closed with error");
            }
            else
            {
                _logger.LogInformation("StatusMonitoringHub connection closed");
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles reconnecting events.
        /// </summary>
        private Task OnReconnecting(Exception? exception)
        {
            Reconnecting?.Invoke(this, EventArgs.Empty);
            _logger.LogWarning(exception, "StatusMonitoringHub reconnecting");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles reconnected events.
        /// </summary>
        private Task OnReconnected(string? connectionId)
        {
            Reconnected?.Invoke(this, connectionId ?? string.Empty);
            _logger.LogInformation("StatusMonitoringHub reconnected with ID: {ConnectionId}", connectionId);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Disposes of the hub connection.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;

            if (_hubConnection != null)
            {
                await _hubConnection.DisposeAsync();
            }

            _statusHandlers.Clear();
            _disposed = true;
        }
    }
}
