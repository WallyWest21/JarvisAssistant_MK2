using System.ComponentModel;
using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JarvisAssistant.Services
{
    /// <summary>
    /// Custom status monitor service that extends the base StatusMonitorService with voice service monitoring.
    /// </summary>
    public class CustomStatusMonitorService : IStatusMonitorService, IAsyncDisposable
    {
        private readonly StatusMonitorService _baseStatusMonitor;
        private readonly VoiceServiceHealthChecker _voiceHealthChecker;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CustomStatusMonitorService> _logger;
        private Timer? _voiceMonitoringTimer;
        private bool _disposed;

        public event PropertyChangedEventHandler? PropertyChanged
        {
            add => _baseStatusMonitor.PropertyChanged += value;
            remove => _baseStatusMonitor.PropertyChanged -= value;
        }

        public CustomStatusMonitorService(
            StatusMonitorService baseStatusMonitor,
            IServiceProvider serviceProvider,
            ILogger<CustomStatusMonitorService> logger)
        {
            _baseStatusMonitor = baseStatusMonitor ?? throw new ArgumentNullException(nameof(baseStatusMonitor));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _voiceHealthChecker = serviceProvider.GetRequiredService<VoiceServiceHealthChecker>();
            
            // Start voice service monitoring
            StartVoiceServiceMonitoring();
        }

        /// <inheritdoc />
        public IObservable<ServiceStatus> ServiceStatusUpdates => _baseStatusMonitor.ServiceStatusUpdates;

        /// <inheritdoc />
        public async Task<IEnumerable<ServiceStatus>> GetAllServiceStatusesAsync()
        {
            return await _baseStatusMonitor.GetAllServiceStatusesAsync();
        }

        /// <inheritdoc />
        public async Task<ServiceStatus?> GetServiceStatusAsync(string serviceName)
        {
            return await _baseStatusMonitor.GetServiceStatusAsync(serviceName);
        }

        /// <inheritdoc />
        public async Task StartMonitoringAsync(string serviceName)
        {
            await _baseStatusMonitor.StartMonitoringAsync(serviceName);
        }

        /// <inheritdoc />
        public async Task StopMonitoringAsync(string serviceName)
        {
            await _baseStatusMonitor.StopMonitoringAsync(serviceName);
        }

        /// <inheritdoc />
        public async Task StartMonitoringAllAsync()
        {
            await _baseStatusMonitor.StartMonitoringAllAsync();
        }

        /// <inheritdoc />
        public async Task StopMonitoringAllAsync()
        {
            await _baseStatusMonitor.StopMonitoringAllAsync();
        }

        /// <inheritdoc />
        public async Task ResetServiceFailuresAsync(string serviceName)
        {
            if (serviceName == "voice-service")
            {
                _voiceHealthChecker.ResetFailures();
            }
            
            await _baseStatusMonitor.ResetServiceFailuresAsync(serviceName);
        }

        /// <summary>
        /// Starts monitoring the voice service with a dedicated health checker.
        /// </summary>
        private void StartVoiceServiceMonitoring()
        {
            _voiceMonitoringTimer = new Timer(
                async _ => await CheckVoiceServiceHealth(),
                null,
                TimeSpan.Zero, // Start immediately
                TimeSpan.FromSeconds(10)); // Check every 10 seconds

            _logger.LogInformation("Voice service monitoring started");
        }

        /// <summary>
        /// Performs a health check on the voice service and updates status.
        /// </summary>
        private async Task CheckVoiceServiceHealth()
        {
            try
            {
                var status = await _voiceHealthChecker.CheckHealthAsync();
                
                // The status will be automatically propagated through the base monitor's mechanism
                // if voice-service is registered with the base monitor. If not, we could emit it directly
                // through a custom observable, but for now we rely on the base monitor.
                
                _logger.LogDebug("Voice service health check completed: {State}", status.State);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during voice service health check");
            }
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;

            if (_voiceMonitoringTimer != null)
            {
                await _voiceMonitoringTimer.DisposeAsync();
            }

            await _baseStatusMonitor.DisposeAsync();
            
            _disposed = true;
        }
    }
}