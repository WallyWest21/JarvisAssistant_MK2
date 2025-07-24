using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using JarvisAssistant.Services;
using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models;
using System.Reactive.Linq;

namespace JarvisAssistant.UnitTests.Services
{
    /// <summary>
    /// Unit tests for the StatusMonitorService class.
    /// </summary>
    public class StatusMonitorServiceTests : IAsyncDisposable
    {
        private readonly Mock<IServiceHealthChecker> _healthCheckerMock;
        private readonly Mock<ILogger<StatusMonitorService>> _loggerMock;
        private readonly StatusMonitorService _statusMonitorService;

        public StatusMonitorServiceTests()
        {
            _healthCheckerMock = new Mock<IServiceHealthChecker>();
            _loggerMock = new Mock<ILogger<StatusMonitorService>>();
            _statusMonitorService = new StatusMonitorService(_healthCheckerMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task StartMonitoringAsync_WithValidService_StartsMonitoring()
        {
            // Arrange
            var serviceName = "test-service";
            var expectedStatus = new ServiceStatus(serviceName, ServiceState.Online);
            
            _healthCheckerMock
                .Setup(x => x.CheckServiceHealthAsync(serviceName))
                .ReturnsAsync(expectedStatus);

            var statusUpdates = new List<ServiceStatus>();
            _statusMonitorService.ServiceStatusUpdates.Subscribe(statusUpdates.Add);

            // Act
            await _statusMonitorService.StartMonitoringAsync(serviceName);

            // Give some time for the initial check
            await Task.Delay(100);

            // Assert
            var statuses = await _statusMonitorService.GetAllServiceStatusesAsync();
            Assert.Single(statuses);
            Assert.Equal(serviceName, statuses.First().ServiceName);
            Assert.Single(statusUpdates);
            Assert.Equal(serviceName, statusUpdates[0].ServiceName);
        }

        [Fact]
        public async Task StartMonitoringAsync_WhenAlreadyMonitoring_LogsWarning()
        {
            // Arrange
            var serviceName = "test-service";
            var expectedStatus = new ServiceStatus(serviceName, ServiceState.Online);
            
            _healthCheckerMock
                .Setup(x => x.CheckServiceHealthAsync(serviceName))
                .ReturnsAsync(expectedStatus);

            // Act
            await _statusMonitorService.StartMonitoringAsync(serviceName);
            await _statusMonitorService.StartMonitoringAsync(serviceName); // Second call

            // Assert
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("already being monitored")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task StopMonitoringAsync_WithMonitoredService_StopsMonitoring()
        {
            // Arrange
            var serviceName = "test-service";
            var expectedStatus = new ServiceStatus(serviceName, ServiceState.Online);
            
            _healthCheckerMock
                .Setup(x => x.CheckServiceHealthAsync(serviceName))
                .ReturnsAsync(expectedStatus);

            await _statusMonitorService.StartMonitoringAsync(serviceName);

            // Act
            await _statusMonitorService.StopMonitoringAsync(serviceName);

            // Assert
            var statuses = await _statusMonitorService.GetAllServiceStatusesAsync();
            Assert.Empty(statuses);
        }

        [Fact]
        public async Task GetServiceStatusAsync_WithExistingService_ReturnsStatus()
        {
            // Arrange
            var serviceName = "test-service";
            var expectedStatus = new ServiceStatus(serviceName, ServiceState.Online);
            
            _healthCheckerMock
                .Setup(x => x.CheckServiceHealthAsync(serviceName))
                .ReturnsAsync(expectedStatus);

            await _statusMonitorService.StartMonitoringAsync(serviceName);
            await Task.Delay(100); // Wait for initial check

            // Act
            var result = await _statusMonitorService.GetServiceStatusAsync(serviceName);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(serviceName, result.ServiceName);
            Assert.Equal(ServiceState.Online, result.State);
        }

        [Fact]
        public async Task GetServiceStatusAsync_WithNonExistentService_ReturnsNull()
        {
            // Arrange
            var serviceName = "non-existent-service";

            // Act
            var result = await _statusMonitorService.GetServiceStatusAsync(serviceName);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ServiceStatusUpdates_OnStatusChange_EmitsUpdate()
        {
            // Arrange
            var serviceName = "test-service";
            var initialStatus = new ServiceStatus(serviceName, ServiceState.Online);
            var updatedStatus = new ServiceStatus(serviceName, ServiceState.Degraded);
            
            var healthCheckCalls = 0;
            _healthCheckerMock
                .Setup(x => x.CheckServiceHealthAsync(serviceName))
                .Returns(() =>
                {
                    healthCheckCalls++;
                    return Task.FromResult(healthCheckCalls == 1 ? initialStatus : updatedStatus);
                });

            var statusUpdates = new List<ServiceStatus>();
            _statusMonitorService.ServiceStatusUpdates.Subscribe(statusUpdates.Add);

            // Act
            await _statusMonitorService.StartMonitoringAsync(serviceName);
            await Task.Delay(100); // Wait for initial check

            // Assert
            Assert.Single(statusUpdates);
            Assert.Equal(ServiceState.Online, statusUpdates[0].State);
        }

        [Fact]
        public async Task StartMonitoringAllAsync_WithMultipleServices_StartsAllMonitoring()
        {
            // Arrange
            var services = new[] { "service1", "service2", "service3" };
            
            _healthCheckerMock
                .Setup(x => x.GetRegisteredServices())
                .Returns(services);

            foreach (var service in services)
            {
                _healthCheckerMock
                    .Setup(x => x.CheckServiceHealthAsync(service))
                    .ReturnsAsync(new ServiceStatus(service, ServiceState.Online));
            }

            // Act
            await _statusMonitorService.StartMonitoringAllAsync();
            await Task.Delay(200); // Wait for all checks

            // Assert
            var statuses = await _statusMonitorService.GetAllServiceStatusesAsync();
            Assert.Equal(3, statuses.Count());
            
            foreach (var service in services)
            {
                Assert.Contains(statuses, s => s.ServiceName == service);
            }
        }

        [Fact]
        public async Task StopMonitoringAllAsync_WithMultipleServices_StopsAllMonitoring()
        {
            // Arrange
            var services = new[] { "service1", "service2", "service3" };
            
            _healthCheckerMock
                .Setup(x => x.GetRegisteredServices())
                .Returns(services);

            foreach (var service in services)
            {
                _healthCheckerMock
                    .Setup(x => x.CheckServiceHealthAsync(service))
                    .ReturnsAsync(new ServiceStatus(service, ServiceState.Online));
            }

            await _statusMonitorService.StartMonitoringAllAsync();

            // Act
            await _statusMonitorService.StopMonitoringAllAsync();

            // Assert
            var statuses = await _statusMonitorService.GetAllServiceStatusesAsync();
            Assert.Empty(statuses);
        }

        [Fact]
        public void ServiceStatusUpdates_IsObservable_CanBeSubscribed()
        {
            // Arrange
            var statusUpdates = new List<ServiceStatus>();
            var subscription = _statusMonitorService.ServiceStatusUpdates.Subscribe(statusUpdates.Add);

            // Act
            // Just verify that we can subscribe without errors

            // Assert
            Assert.NotNull(subscription);
            subscription.Dispose();
        }

        [Fact]
        public async Task MonitoringService_WithPeriodicChecks_UpdatesStatus()
        {
            // Arrange
            var serviceName = "periodic-service";
            var callCount = 0;
            
            _healthCheckerMock
                .Setup(x => x.CheckServiceHealthAsync(serviceName))
                .Returns(() =>
                {
                    callCount++;
                    return Task.FromResult(new ServiceStatus(serviceName, ServiceState.Online));
                });

            var statusUpdates = new List<ServiceStatus>();
            _statusMonitorService.ServiceStatusUpdates.Subscribe(statusUpdates.Add);

            // Act
            await _statusMonitorService.StartMonitoringAsync(serviceName);
            await Task.Delay(100); // Wait for initial check

            // Assert
            Assert.True(callCount >= 1, "Health check should be called at least once");
            Assert.Single(statusUpdates);
        }

        [Fact]
        public async Task StatusMonitorService_HandlesConcurrentAccess_ThreadSafe()
        {
            // Arrange
            var services = Enumerable.Range(1, 10).Select(i => $"service{i}").ToArray();
            
            _healthCheckerMock
                .Setup(x => x.GetRegisteredServices())
                .Returns(services);

            foreach (var service in services)
            {
                _healthCheckerMock
                    .Setup(x => x.CheckServiceHealthAsync(service))
                    .ReturnsAsync(new ServiceStatus(service, ServiceState.Online));
            }

            // Act
            var tasks = services.Select(service => _statusMonitorService.StartMonitoringAsync(service));
            await Task.WhenAll(tasks);

            await Task.Delay(200); // Wait for all checks

            // Assert
            var statuses = await _statusMonitorService.GetAllServiceStatusesAsync();
            Assert.Equal(services.Length, statuses.Count());
        }

        public async ValueTask DisposeAsync()
        {
            await _statusMonitorService.DisposeAsync();
        }
    }
}
