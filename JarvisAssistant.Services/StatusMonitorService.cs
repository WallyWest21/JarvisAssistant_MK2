using System.ComponentModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace JarvisAssistant.Services
{
    /// <summary>
    /// Real-time service status monitoring service with SignalR integration.
    /// </summary>
    public class StatusMonitorService : IStatusMonitorService, IAsyncDisposable
    {
        private readonly IServiceHealthChecker _healthChecker;
        private readonly ILogger<StatusMonitorService> _logger;
        private readonly Subject<ServiceStatus> _statusSubject;
        private readonly Dictionary<string, ServiceStatus> _serviceStatuses;
        private readonly Dictionary<string, Timer> _monitoringTimers;
        private readonly SemaphoreSlim _semaphore;
        private HubConnection? _hubConnection;
        private bool _disposed;

        private const int MonitoringIntervalSeconds = 5;

        public event PropertyChangedEventHandler? PropertyChanged;

        public StatusMonitorService(IServiceHealthChecker healthChecker, ILogger<StatusMonitorService> logger)
        {
            _healthChecker = healthChecker;
            _logger = logger;
            _statusSubject = new Subject<ServiceStatus>();
            _serviceStatuses = new Dictionary<string, ServiceStatus>();
            _monitoringTimers = new Dictionary<string, Timer>();
            _semaphore = new SemaphoreSlim(1, 1);

            ServiceStatusUpdates = _statusSubject.AsObservable();
            
            // Register default services for monitoring
            RegisterDefaultServices();
        }

        /// <summary>
        /// Gets an observable collection of service status updates.
        /// </summary>
        public IObservable<ServiceStatus> ServiceStatusUpdates { get; }

        /// <summary>
        /// Initializes SignalR connection for real-time status broadcasting.
        /// </summary>
        public async Task InitializeSignalRAsync(string hubUrl)
        {
            try
            {
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(hubUrl)
                    .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
                    .Build();

                // Handle reconnection events
                _hubConnection.Reconnecting += OnReconnecting;
                _hubConnection.Reconnected += OnReconnected;
                _hubConnection.Closed += OnConnectionClosed;

                // Subscribe to status updates from hub
                _hubConnection.On<string, ServiceStatus>("ServiceStatusUpdated", OnServiceStatusUpdated);

                await _hubConnection.StartAsync();
                _logger.LogInformation("StatusMonitorService SignalR connection established");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize SignalR connection");
                // Continue without SignalR - local monitoring will still work
            }
        }

        /// <summary>
        /// Gets the current status of all monitored services.
        /// </summary>
        public async Task<IEnumerable<ServiceStatus>> GetAllServiceStatusesAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                return _serviceStatuses.Values.ToList();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Gets the status of a specific service by name.
        /// </summary>
        public async Task<ServiceStatus?> GetServiceStatusAsync(string serviceName)
        {
            await _semaphore.WaitAsync();
            try
            {
                return _serviceStatuses.TryGetValue(serviceName, out var status) ? status : null;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Starts monitoring the specified service.
        /// </summary>
        public async Task StartMonitoringAsync(string serviceName)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (_monitoringTimers.ContainsKey(serviceName))
                {
                    _logger.LogWarning("Service {ServiceName} is already being monitored", serviceName);
                    return;
                }

                // Perform initial health check
                var initialStatus = await _healthChecker.CheckServiceHealthAsync(serviceName);
                _serviceStatuses[serviceName] = initialStatus;
                _statusSubject.OnNext(initialStatus);

                // Broadcast initial status via SignalR
                await BroadcastStatusUpdateAsync(serviceName, initialStatus);

                // Start periodic monitoring
                var timer = new Timer(
                    async _ => await MonitorServiceAsync(serviceName),
                    null,
                    TimeSpan.FromSeconds(MonitoringIntervalSeconds),
                    TimeSpan.FromSeconds(MonitoringIntervalSeconds));

                _monitoringTimers[serviceName] = timer;
                
                _logger.LogInformation("Started monitoring service: {ServiceName}", serviceName);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Stops monitoring the specified service.
        /// </summary>
        public async Task StopMonitoringAsync(string serviceName)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (_monitoringTimers.TryGetValue(serviceName, out var timer))
                {
                    await timer.DisposeAsync();
                    _monitoringTimers.Remove(serviceName);
                    _serviceStatuses.Remove(serviceName);
                    
                    _logger.LogInformation("Stopped monitoring service: {ServiceName}", serviceName);
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Resets the failure count and backoff state for a specific service.
        /// </summary>
        public async Task ResetServiceFailuresAsync(string serviceName)
        {
            await _semaphore.WaitAsync();
            try
            {
                _healthChecker.ResetServiceFailures(serviceName);
                
                // Immediately perform a fresh health check
                var status = await _healthChecker.CheckServiceHealthAsync(serviceName);
                _serviceStatuses[serviceName] = status;
                _statusSubject.OnNext(status);
                
                await BroadcastStatusUpdateAsync(serviceName, status);
                
                _logger.LogInformation("Reset failures and checked health for service: {ServiceName}", serviceName);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Starts monitoring all registered services.
        /// </summary>
        public async Task StartMonitoringAllAsync()
        {
            var services = _healthChecker.GetRegisteredServices();
            var tasks = services.Select(StartMonitoringAsync);
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Stops monitoring all services.
        /// </summary>
        public async Task StopMonitoringAllAsync()
        {
            var services = _monitoringTimers.Keys.ToList();
            var tasks = services.Select(StopMonitoringAsync);
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Monitors a single service and updates its status.
        /// </summary>
        private async Task MonitorServiceAsync(string serviceName)
        {
            try
            {
                var status = await _healthChecker.CheckServiceHealthAsync(serviceName);
                
                await _semaphore.WaitAsync();
                try
                {
                    var previousStatus = _serviceStatuses.TryGetValue(serviceName, out var prev) ? prev : null;
                    _serviceStatuses[serviceName] = status;

                    // Only emit update if status has changed
                    if (previousStatus == null || !StatusEquals(previousStatus, status))
                    {
                        _statusSubject.OnNext(status);
                        await BroadcastStatusUpdateAsync(serviceName, status);
                        
                        _logger.LogDebug("Service status updated: {ServiceName} - {State} ({ResponseTime}ms)",
                            serviceName, status.State, status.Metrics?.GetValueOrDefault("response_time_ms", 0));
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring service: {ServiceName}", serviceName);
            }
        }

        /// <summary>
        /// Broadcasts status update via SignalR.
        /// </summary>
        private async Task BroadcastStatusUpdateAsync(string serviceName, ServiceStatus status)
        {
            if (_hubConnection?.State == HubConnectionState.Connected)
            {
                try
                {
                    await _hubConnection.InvokeAsync("BroadcastServiceStatus", serviceName, status);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to broadcast status update for {ServiceName}", serviceName);
                }
            }
        }

        /// <summary>
        /// Handles incoming status updates from SignalR hub.
        /// </summary>
        private async void OnServiceStatusUpdated(string serviceName, ServiceStatus status)
        {
            await _semaphore.WaitAsync();
            try
            {
                _serviceStatuses[serviceName] = status;
                _statusSubject.OnNext(status);
                
                _logger.LogDebug("Received status update from hub: {ServiceName} - {State}", 
                    serviceName, status.State);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Handles SignalR reconnection events.
        /// </summary>
        private Task OnReconnecting(Exception? exception)
        {
            _logger.LogWarning(exception, "StatusMonitorService SignalR connection reconnecting");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles SignalR reconnection completed events.
        /// </summary>
        private Task OnReconnected(string? connectionId)
        {
            _logger.LogInformation("StatusMonitorService SignalR connection reconnected with ID: {ConnectionId}", connectionId);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles SignalR connection closed events.
        /// </summary>
        private Task OnConnectionClosed(Exception? exception)
        {
            _logger.LogWarning(exception, "StatusMonitorService SignalR connection closed");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Registers default services for monitoring.
        /// </summary>
        private void RegisterDefaultServices()
        {
            // Register common services - these would be configured based on your environment
            // Fix: Use /api/tags instead of /api/health for Ollama, and use the working IP from tests
            _healthChecker.RegisterService("llm-engine", "http://100.108.155.28:11434/api/tags", "LLM Engine");
            _healthChecker.RegisterService("vision-api", "http://localhost:5000/health", "Vision API");
            // voice-service registration is handled by MauiProgram based on configuration
            // Note: chat-api removed as chat functionality is built into MAUI client, not a separate service
            _healthChecker.RegisterService("signalr-hub", "http://localhost:5003/health", "SignalR Hub");
        }

        /// <summary>
        /// Compares two ServiceStatus objects for equality.
        /// </summary>
        private static bool StatusEquals(ServiceStatus a, ServiceStatus b)
        {
            return a.ServiceName == b.ServiceName &&
                   a.State == b.State &&
                   a.ErrorMessage == b.ErrorMessage &&
                   a.Metrics?.GetValueOrDefault("response_time_ms")?.Equals(
                       b.Metrics?.GetValueOrDefault("response_time_ms")) == true;
        }

        /// <summary>
        /// Disposes of the service and releases all resources.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;

            await StopMonitoringAllAsync();

            if (_hubConnection != null)
            {
                await _hubConnection.DisposeAsync();
            }

            _statusSubject.Dispose();
            _semaphore.Dispose();

            _disposed = true;
        }
    }
}
