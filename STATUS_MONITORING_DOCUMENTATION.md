# Real-Time Service Status Monitoring System

## Overview

This document describes the comprehensive real-time service status monitoring system implemented for the Jarvis Assistant MK2. The system provides continuous health monitoring, real-time status updates, and responsive UI components that adapt to different platforms.

## Architecture

### Core Components

1. **ServiceHealthChecker** - Performs individual service health checks with retry logic and backoff
2. **StatusMonitorService** - Orchestrates monitoring and provides observable status updates
3. **StatusMonitoringHub** - SignalR client for real-time communication
4. **StatusPanelView** - Platform-adaptive UI component
5. **StatusPanelViewModel** - MVVM view model with reactive data binding

### Class Diagram

```
┌─────────────────────┐    ┌──────────────────────┐
│  ServiceHealthChecker│    │ StatusMonitorService │
│                     │    │                      │
│ + CheckServiceHealth│◄───┤ + StartMonitoring    │
│ + RegisterService   │    │ + StopMonitoring     │
│ + CalculateBackoff  │    │ + GetAllStatuses     │
└─────────────────────┘    └──────────────────────┘
                                      │
                                      ▼
                           ┌──────────────────────┐
                           │ StatusMonitoringHub  │
                           │                      │
                           │ + BroadcastStatus    │
                           │ + SubscribeToService │
                           └──────────────────────┘
```

## Features

### 1. Health Checking System

The `ServiceHealthChecker` class provides:

- **Endpoint Registration**: Register services with health check URLs
- **Response Time Measurement**: Accurate timing using `Stopwatch`
- **Status Classification**:
  - 🟢 Online: Response time < 100ms
  - 🟡 Degraded: Response time 100-500ms
  - 🔴 Offline/Error: No response or HTTP errors
- **Exponential Backoff**: Prevents overwhelming failed services
- **Error Code Generation**: Structured error codes for debugging

#### Example Usage

```csharp
var healthChecker = new ServiceHealthChecker(httpClient, logger);

// Register services
healthChecker.RegisterService("llm-engine", "http://localhost:11434/api/health", "LLM Engine");
healthChecker.RegisterService("vision-api", "http://localhost:5000/health", "Vision API");

// Check health
var status = await healthChecker.CheckServiceHealthAsync("llm-engine");
Console.WriteLine($"Status: {status.State}, Response Time: {status.Metrics["response_time_ms"]}ms");
```

### 2. Real-Time Monitoring

The `StatusMonitorService` provides:

- **Observable Pattern**: Subscribe to real-time status updates using `IObservable<ServiceStatus>`
- **Periodic Monitoring**: Configurable 5-second health check intervals
- **Thread-Safe Operations**: Concurrent access protection using `SemaphoreSlim`
- **Automatic Lifecycle Management**: Start/stop monitoring for individual or all services

#### Example Usage

```csharp
var statusMonitor = serviceProvider.GetService<IStatusMonitorService>();

// Subscribe to updates
statusMonitor.ServiceStatusUpdates.Subscribe(status => 
{
    Console.WriteLine($"Service {status.ServiceName} is now {status.State}");
});

// Start monitoring
await statusMonitor.StartMonitoringAllAsync();
```

### 3. SignalR Integration

The `StatusMonitoringHub` provides:

- **Real-Time Broadcasting**: Push status updates to all connected clients
- **Automatic Reconnection**: Handles connection failures gracefully
- **Group Management**: Subscribe to specific types of updates
- **Scalable Architecture**: Support for multiple clients and services

#### Hub Methods

```csharp
// Server-side hub (separate ASP.NET Core project)
public class StatusHub : Hub
{
    public async Task BroadcastServiceStatus(string serviceName, ServiceStatus status)
    {
        await Clients.All.SendAsync("ServiceStatusUpdated", serviceName, status);
    }
    
    public async Task SubscribeToService(string serviceName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"service_{serviceName}");
    }
}
```

### 4. Platform-Adaptive UI

The `StatusPanelView` provides different layouts for each platform:

#### Desktop (Windows/Mac)
- **Always Visible Sidebar**: 280px wide panel on the left side
- **Detailed Information**: Service names, status icons, response times
- **Manual Refresh**: Button to force status updates
- **Shadow Effects**: Visual depth with drop shadows

#### Mobile (Android/iOS)
- **Collapsible Panel**: Swipe down gesture to expand
- **Compact Indicator**: Minimal status bar when collapsed
- **Backdrop Overlay**: Semi-transparent background when expanded
- **Touch-Friendly**: Large tap targets and gestures

#### TV (Tizen)
- **Corner Overlay**: Minimal 200px overlay in top-right corner
- **Compact Display**: Essential information only
- **Auto-Hide**: Fades out after inactivity
- **Remote Control Navigation**: Focus management for TV remotes

### 5. Status Format Examples

The system displays status in the following formats:

```
● LLM Engine    Online (45ms)
● Vision API    Degraded (320ms)
● Voice Service Offline [VIS-CONN-001]
● Chat API      Starting...
● SignalR Hub   Error [HTTP-500-001]
```

#### Status Icons
- 🟢 **Online**: Green circle (response < 100ms)
- 🟡 **Degraded**: Yellow circle (response 100-500ms)
- 🔴 **Offline**: Red circle (no response)
- 🔵 **Starting**: Blue circle with animation
- 🟣 **Stopping**: Purple circle

#### Error Codes
- `SRV-CONN-001`: Connection refused
- `SRV-TIMEOUT-001`: Request timeout
- `SRV-NOT-REG-001`: Service not registered
- `HTTP-xxx-001`: HTTP status code errors
- `VIS-CONN-001`: Vision API connection error

## Configuration

### Service Registration

Configure services in `MauiProgram.cs`:

```csharp
services.AddStatusMonitoring(options =>
{
    options.MonitoringIntervalSeconds = 5;
    options.HealthCheckTimeoutSeconds = 10;
    options.AutoStartMonitoring = true;
    options.SignalRHubUrl = "http://localhost:5003/statusHub";
    
    // Configure service endpoints
    options.ServiceEndpoints["llm-engine"] = new ServiceEndpointConfig
    {
        Name = "llm-engine",
        DisplayName = "LLM Engine",
        HealthEndpoint = "http://localhost:11434/api/health",
        Enabled = true
    };
});
```

### XAML Integration

Add the status panel to any page:

```xml
<Grid>
    <!-- Your main content -->
    <ContentView>
        <!-- Page content here -->
    </ContentView>
    
    <!-- Status panel overlay -->
    <views:StatusPanelView x:Name="StatusPanel" />
</Grid>
```

## Testing

### Unit Tests

The system includes comprehensive unit tests covering:

- **ServiceHealthChecker**: Response time classification, error handling, backoff logic
- **StatusMonitorService**: Concurrent access, observable patterns, lifecycle management
- **StatusPanelViewModel**: UI state management, command handling, data binding

#### Test Categories

1. **Health Check Tests** (`ServiceHealthCheckerTests.cs`)
   - Healthy service responses
   - Slow service detection
   - HTTP error handling
   - Timeout scenarios
   - Connection failures
   - Backoff algorithm

2. **Monitoring Service Tests** (`StatusMonitorServiceTests.cs`)
   - Service lifecycle management
   - Observable pattern verification
   - Concurrent access safety
   - Real-time update propagation

3. **ViewModel Tests** (`StatusPanelViewModelTests.cs`)
   - UI state management
   - Command execution
   - Data binding updates
   - Platform-specific behavior

### Mock Integration

Tests use comprehensive mocking:

```csharp
[Fact]
public async Task CheckServiceHealthAsync_WithHealthyService_ReturnsOnlineStatus()
{
    // Arrange
    var httpHandler = new Mock<HttpMessageHandler>();
    httpHandler.Protected()
        .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
        .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));
    
    var httpClient = new HttpClient(httpHandler.Object);
    var healthChecker = new ServiceHealthChecker(httpClient, Mock.Of<ILogger<ServiceHealthChecker>>());
    
    // Act
    var result = await healthChecker.CheckServiceHealthAsync("test-service");
    
    // Assert
    Assert.Equal(ServiceState.Online, result.State);
}
```

## Performance Considerations

### Memory Management
- **Disposable Pattern**: All monitoring components implement proper disposal
- **Event Unsubscription**: Automatic cleanup of event handlers and observables
- **Timer Management**: Proper disposal of monitoring timers

### Network Efficiency
- **Connection Pooling**: HttpClient reuse for health checks
- **Timeout Configuration**: Reasonable timeouts to prevent hanging requests
- **Backoff Strategy**: Reduces load on failing services

### UI Performance
- **Background Threading**: Health checks run on background threads
- **Main Thread Marshaling**: UI updates use `MainThread.BeginInvokeOnMainThread`
- **Efficient Data Binding**: Observable collections for smooth UI updates

## Error Handling

### Graceful Degradation
- **Service Failures**: Continue monitoring other services when one fails
- **Network Issues**: Automatic retry with exponential backoff
- **UI Fallbacks**: Show cached status when real-time updates fail

### Logging Strategy
- **Structured Logging**: Uses Microsoft.Extensions.Logging with structured data
- **Log Levels**: Appropriate levels for different scenarios
- **Error Context**: Detailed error information for debugging

```csharp
_logger.LogError(ex, "Health check failed for {ServiceName} with error code {ErrorCode}", 
    serviceName, errorCode);
```

## Security Considerations

### Network Security
- **HTTPS Support**: Configurable SSL/TLS for health check endpoints
- **Authentication**: Support for bearer tokens and API keys
- **Timeout Limits**: Prevent DoS through reasonable timeout settings

### Data Protection
- **Sensitive Information**: Avoid logging sensitive data in health checks
- **Error Messages**: Sanitize error messages for client display
- **Configuration Security**: Secure storage of endpoint URLs and credentials

## Deployment

### Prerequisites
- .NET 8.0 MAUI runtime
- SignalR hub server (separate ASP.NET Core project)
- Health check endpoints on monitored services

### Configuration Files
- `appsettings.json`: Service endpoint configuration
- `MauiProgram.cs`: Dependency injection setup
- Platform-specific settings in `Platforms/` folders

### Monitoring Dashboard
For a complete monitoring solution, deploy the included SignalR hub:

```bash
# Clone the hub server
git clone [hub-server-repo]
cd StatusMonitoringHub

# Build and run
dotnet build
dotnet run --urls=http://localhost:5003
```

## Future Enhancements

### Planned Features
1. **Historical Data**: Store and display service health history
2. **Alert System**: Email/SMS notifications for service failures
3. **Custom Metrics**: Support for application-specific health metrics
4. **Dashboard Export**: Export status data to monitoring platforms
5. **Performance Trending**: Graph response time trends over time

### Integration Opportunities
- **Application Insights**: Azure monitoring integration
- **Prometheus**: Metrics export for Prometheus/Grafana
- **Health Check Libraries**: Integration with ASP.NET Core health checks
- **Service Mesh**: Support for Istio/Envoy service mesh monitoring

## Troubleshooting

### Common Issues

1. **Services Not Appearing**
   - Verify service registration in `MauiProgram.cs`
   - Check health check endpoint URLs
   - Ensure network connectivity

2. **SignalR Connection Failures**
   - Verify hub server is running
   - Check firewall settings
   - Validate hub URL configuration

3. **UI Not Updating**
   - Verify view model binding
   - Check for threading issues
   - Ensure proper disposal of subscriptions

### Debug Commands

```csharp
// Check service registration
var services = healthChecker.GetRegisteredServices();
Console.WriteLine($"Registered services: {string.Join(", ", services)}");

// Verify status monitor state
var statuses = await statusMonitor.GetAllServiceStatusesAsync();
foreach (var status in statuses)
{
    Console.WriteLine($"{status.ServiceName}: {status.State}");
}

// Test SignalR connection
Console.WriteLine($"Hub connection state: {hubConnection.State}");
```

## API Reference

### IStatusMonitorService Interface

```csharp
public interface IStatusMonitorService : INotifyPropertyChanged
{
    IObservable<ServiceStatus> ServiceStatusUpdates { get; }
    Task<IEnumerable<ServiceStatus>> GetAllServiceStatusesAsync();
    Task<ServiceStatus?> GetServiceStatusAsync(string serviceName);
    Task StartMonitoringAsync(string serviceName);
    Task StopMonitoringAsync(string serviceName);
    Task StartMonitoringAllAsync();
    Task StopMonitoringAllAsync();
}
```

### ServiceStatus Model

```csharp
public class ServiceStatus
{
    public string ServiceName { get; set; }
    public ServiceState State { get; set; }
    public DateTimeOffset LastHeartbeat { get; set; }
    public Dictionary<string, object>? Metrics { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Version { get; set; }
    public TimeSpan? Uptime { get; set; }
    
    // Computed properties
    public bool IsHealthy { get; }
    public bool IsError { get; }
    public bool HasError { get; }
    public bool IsChecking { get; }
    public string? ErrorCode { get; }
}
```

### ServiceState Enumeration

```csharp
public enum ServiceState
{
    Offline,
    Starting,
    Online,
    Degraded,
    Error,
    Stopping
}
```

This comprehensive status monitoring system provides real-time visibility into service health with a responsive, platform-adaptive user interface suitable for desktop, mobile, and TV platforms.
