using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using JarvisAssistant.Services;
using JarvisAssistant.Core.Models;
using System.Net;
using Moq.Protected;

namespace JarvisAssistant.UnitTests.Services
{
    /// <summary>
    /// Unit tests for the ServiceHealthChecker class.
    /// </summary>
    public class ServiceHealthCheckerTests : IDisposable
    {
        private readonly Mock<ILogger<ServiceHealthChecker>> _loggerMock;
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly HttpClient _httpClient;
        private readonly ServiceHealthChecker _healthChecker;

        public ServiceHealthCheckerTests()
        {
            _loggerMock = new Mock<ILogger<ServiceHealthChecker>>();
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _healthChecker = new ServiceHealthChecker(_httpClient, _loggerMock.Object);
        }

        [Fact]
        public async Task CheckServiceHealthAsync_WithHealthyService_ReturnsOnlineStatus()
        {
            // Arrange
            var serviceName = "test-service";
            var healthEndpoint = "http://localhost:8080/health";
            
            _healthChecker.RegisterService(serviceName, healthEndpoint);
            
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("Healthy")
            };

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(responseMessage);

            // Act
            var result = await _healthChecker.CheckServiceHealthAsync(serviceName);

            // Assert
            Assert.Equal(serviceName, result.ServiceName);
            Assert.Equal(ServiceState.Online, result.State);
            Assert.True(result.IsHealthy);
            Assert.NotNull(result.Metrics);
            Assert.True(result.Metrics.ContainsKey("response_time_ms"));
            Assert.Equal(200, result.Metrics["status_code"]);
        }

        [Fact]
        public async Task CheckServiceHealthAsync_WithSlowService_ReturnsDegradedStatus()
        {
            // Arrange
            var serviceName = "slow-service";
            var healthEndpoint = "http://localhost:8080/health";
            
            _healthChecker.RegisterService(serviceName, healthEndpoint);
            
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("Healthy")
            };

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .Returns(async () =>
                {
                    await Task.Delay(200); // Simulate slow response
                    return responseMessage;
                });

            // Act
            var result = await _healthChecker.CheckServiceHealthAsync(serviceName);

            // Assert
            Assert.Equal(ServiceState.Degraded, result.State);
            Assert.True(result.IsHealthy);
            Assert.True((int)result.Metrics!["response_time_ms"] >= 200);
        }

        [Fact]
        public async Task CheckServiceHealthAsync_WithHttpError_ReturnsErrorStatus()
        {
            // Arrange
            var serviceName = "error-service";
            var healthEndpoint = "http://localhost:8080/health";
            
            _healthChecker.RegisterService(serviceName, healthEndpoint);
            
            var responseMessage = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                ReasonPhrase = "Internal Server Error"
            };

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(responseMessage);

            // Act
            var result = await _healthChecker.CheckServiceHealthAsync(serviceName);

            // Assert
            Assert.Equal(ServiceState.Error, result.State);
            Assert.False(result.IsHealthy);
            Assert.Contains("HTTP 500", result.ErrorMessage);
            Assert.Equal("HTTP-500-001", result.Metrics!["error_code"]);
        }

        [Fact]
        public async Task CheckServiceHealthAsync_WithTimeout_ReturnsOfflineStatus()
        {
            // Arrange
            var serviceName = "timeout-service";
            var healthEndpoint = "http://localhost:8080/health";
            
            _healthChecker.RegisterService(serviceName, healthEndpoint);
            
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new TaskCanceledException("Request timeout"));

            // Act
            var result = await _healthChecker.CheckServiceHealthAsync(serviceName);

            // Assert
            Assert.Equal(ServiceState.Offline, result.State);
            Assert.False(result.IsHealthy);
            Assert.Contains("timeout", result.ErrorMessage!.ToLower());
            Assert.Equal("SRV-TIMEOUT-001", result.Metrics!["error_code"]);
        }

        [Fact]
        public async Task CheckServiceHealthAsync_WithConnectionError_ReturnsOfflineStatus()
        {
            // Arrange
            var serviceName = "connection-error-service";
            var healthEndpoint = "http://localhost:8080/health";
            
            _healthChecker.RegisterService(serviceName, healthEndpoint);
            
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new HttpRequestException("Connection refused"));

            // Act
            var result = await _healthChecker.CheckServiceHealthAsync(serviceName);

            // Assert
            Assert.Equal(ServiceState.Offline, result.State);
            Assert.False(result.IsHealthy);
            Assert.Contains("Connection error", result.ErrorMessage);
            Assert.Equal("SRV-CONN-001", result.Metrics!["error_code"]);
        }

        [Fact]
        public async Task CheckServiceHealthAsync_WithUnregisteredService_ReturnsErrorStatus()
        {
            // Arrange
            var serviceName = "unregistered-service";

            // Act
            var result = await _healthChecker.CheckServiceHealthAsync(serviceName);

            // Assert
            Assert.Equal(ServiceState.Error, result.State);
            Assert.Contains("not registered", result.ErrorMessage);
            Assert.Equal("SRV-NOT-REG-001", result.Metrics!["error_code"]);
        }

        [Fact]
        public async Task CheckServiceHealthAsync_WithConsecutiveFailures_ImplementsBackoff()
        {
            // Arrange
            var serviceName = "backoff-service";
            var healthEndpoint = "http://localhost:8080/health";
            
            _healthChecker.RegisterService(serviceName, healthEndpoint);
            
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new HttpRequestException("Connection refused"));

            // Act - First failure
            var result1 = await _healthChecker.CheckServiceHealthAsync(serviceName);
            // Act - Second failure (should trigger backoff)
            var result2 = await _healthChecker.CheckServiceHealthAsync(serviceName);

            // Assert
            Assert.Equal(1, result1.Metrics!["consecutive_failures"]);
            Assert.Equal("SRV-BACKOFF-001", result2.Metrics!["error_code"]);
            Assert.Contains("Backing off", result2.ErrorMessage);
        }

        [Theory]
        [InlineData(50, ServiceState.Online)]
        [InlineData(150, ServiceState.Degraded)]
        [InlineData(600, ServiceState.Degraded)]
        public async Task CheckServiceHealthAsync_WithDifferentResponseTimes_ReturnsExpectedState(
            int delayMs, ServiceState expectedState)
        {
            // Arrange
            var serviceName = "response-time-service";
            var healthEndpoint = "http://localhost:8080/health";
            
            _healthChecker.RegisterService(serviceName, healthEndpoint);
            
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK);

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .Returns(async () =>
                {
                    await Task.Delay(delayMs);
                    return responseMessage;
                });

            // Act
            var result = await _healthChecker.CheckServiceHealthAsync(serviceName);

            // Assert
            Assert.Equal(expectedState, result.State);
            Assert.True((int)result.Metrics!["response_time_ms"] >= delayMs);
        }

        [Fact]
        public void GetRegisteredServices_ReturnsAllRegisteredServices()
        {
            // Arrange
            _healthChecker.RegisterService("service1", "http://localhost:8080/health");
            _healthChecker.RegisterService("service2", "http://localhost:8081/health");
            _healthChecker.RegisterService("service3", "http://localhost:8082/health");

            // Act
            var services = _healthChecker.GetRegisteredServices().ToList();

            // Assert
            Assert.Equal(3, services.Count);
            Assert.Contains("service1", services);
            Assert.Contains("service2", services);
            Assert.Contains("service3", services);
        }

        [Fact]
        public void GetServiceDisplayName_WithRegisteredService_ReturnsDisplayName()
        {
            // Arrange
            var serviceName = "test-service";
            var displayName = "Test Service Display Name";
            _healthChecker.RegisterService(serviceName, "http://localhost:8080/health", displayName);

            // Act
            var result = _healthChecker.GetServiceDisplayName(serviceName);

            // Assert
            Assert.Equal(displayName, result);
        }

        [Fact]
        public void GetServiceDisplayName_WithUnregisteredService_ReturnsServiceName()
        {
            // Arrange
            var serviceName = "unregistered-service";

            // Act
            var result = _healthChecker.GetServiceDisplayName(serviceName);

            // Assert
            Assert.Equal(serviceName, result);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
