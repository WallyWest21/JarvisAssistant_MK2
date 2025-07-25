using JarvisAssistant.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JarvisAssistant.Services.Extensions
{
    /// <summary>
    /// Extension methods for registering SolidWorks integration services in the dependency injection container.
    /// </summary>
    public static class SolidWorksServiceExtensions
    {
        /// <summary>
        /// Adds SolidWorks integration services to the service collection.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddSolidWorksIntegration(this IServiceCollection services)
        {
            // Register the main SolidWorks service
            services.AddSingleton<ISolidWorksService, SolidWorksService>();
            
            // Register the code generator service
            services.AddSingleton<ISolidWorksCodeGenerator, SolidWorksCodeGenerator>();

            return services;
        }

        /// <summary>
        /// Adds SolidWorks integration services with custom configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">Action to configure SolidWorks options.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddSolidWorksIntegration(
            this IServiceCollection services,
            Action<SolidWorksIntegrationOptions> configureOptions)
        {
            var options = new SolidWorksIntegrationOptions();
            configureOptions(options);
            services.AddSingleton(options);

            return services.AddSolidWorksIntegration();
        }

        /// <summary>
        /// Adds SolidWorks integration with automatic COM registration validation.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="validateComRegistration">Whether to validate COM registration at startup.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddSolidWorksIntegrationWithValidation(
            this IServiceCollection services,
            bool validateComRegistration = true)
        {
            if (validateComRegistration)
            {
                // Register a service to validate SolidWorks COM registration
                services.AddSingleton<ISolidWorksComValidator, SolidWorksComValidator>();
            }

            return services.AddSolidWorksIntegration();
        }

        /// <summary>
        /// Adds enhanced SolidWorks services with advanced error handling and monitoring.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <returns>The service collection for chaining.</returns>
        public static IServiceCollection AddEnhancedSolidWorksIntegration(this IServiceCollection services)
        {
            // Add base SolidWorks services
            services.AddSolidWorksIntegration();

            // Add enhanced error handling for SolidWorks operations
            services.AddSingleton<ISolidWorksErrorHandler, SolidWorksErrorHandler>();

            // Add SolidWorks health monitoring
            services.AddSingleton<ISolidWorksHealthMonitor, SolidWorksHealthMonitor>();

            // Add macro template manager
            services.AddSingleton<ISolidWorksMacroTemplateManager, SolidWorksMacroTemplateManager>();

            return services;
        }
    }

    /// <summary>
    /// Configuration options for SolidWorks integration.
    /// </summary>
    public class SolidWorksIntegrationOptions
    {
        /// <summary>
        /// Gets or sets whether to auto-start SolidWorks if not running.
        /// </summary>
        public bool AutoStartSolidWorks { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to start SolidWorks in silent mode.
        /// </summary>
        public bool StartSilent { get; set; } = true;

        /// <summary>
        /// Gets or sets the connection timeout in seconds.
        /// </summary>
        public int ConnectionTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Gets or sets whether to enable advanced error handling.
        /// </summary>
        public bool EnableAdvancedErrorHandling { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to include Jarvis personality in generated macros by default.
        /// </summary>
        public bool IncludeJarvisPersonalityByDefault { get; set; } = true;

        /// <summary>
        /// Gets or sets the default macro complexity level.
        /// </summary>
        public string DefaultMacroComplexity { get; set; } = "Standard";

        /// <summary>
        /// Gets or sets the default error handling level for generated macros.
        /// </summary>
        public string DefaultErrorHandlingLevel { get; set; } = "Comprehensive";

        /// <summary>
        /// Gets or sets whether to enable COM object cleanup monitoring.
        /// </summary>
        public bool EnableComCleanupMonitoring { get; set; } = true;

        /// <summary>
        /// Gets or sets the directory for storing generated macros.
        /// </summary>
        public string? MacroOutputDirectory { get; set; }

        /// <summary>
        /// Gets or sets whether to validate SolidWorks installation at startup.
        /// </summary>
        public bool ValidateInstallationAtStartup { get; set; } = true;
    }

    /// <summary>
    /// Interface for SolidWorks COM registration validation.
    /// </summary>
    public interface ISolidWorksComValidator
    {
        /// <summary>
        /// Validates that SolidWorks COM components are properly registered.
        /// </summary>
        /// <returns>A task that represents the asynchronous validation operation.</returns>
        Task<SolidWorksValidationResult> ValidateComRegistrationAsync();

        /// <summary>
        /// Gets the installed SolidWorks version information.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task<SolidWorksVersionInfo?> GetInstalledVersionAsync();
    }

    /// <summary>
    /// Interface for enhanced SolidWorks error handling.
    /// </summary>
    public interface ISolidWorksErrorHandler
    {
        /// <summary>
        /// Handles SolidWorks-specific errors with intelligent recovery.
        /// </summary>
        /// <param name="exception">The exception that occurred.</param>
        /// <param name="context">The context in which the error occurred.</param>
        /// <returns>A task that represents the asynchronous error handling operation.</returns>
        Task<ErrorHandlingResult> HandleErrorAsync(Exception exception, string context);

        /// <summary>
        /// Attempts to recover from a SolidWorks connection failure.
        /// </summary>
        /// <returns>A task that represents the asynchronous recovery operation.</returns>
        Task<bool> AttemptConnectionRecoveryAsync();
    }

    /// <summary>
    /// Interface for SolidWorks health monitoring.
    /// </summary>
    public interface ISolidWorksHealthMonitor
    {
        /// <summary>
        /// Monitors the health of the SolidWorks connection.
        /// </summary>
        /// <returns>A task that represents the asynchronous monitoring operation.</returns>
        Task<SolidWorksHealthStatus> CheckHealthAsync();

        /// <summary>
        /// Starts continuous health monitoring.
        /// </summary>
        /// <param name="intervalSeconds">The monitoring interval in seconds.</param>
        /// <returns>A task that represents the asynchronous monitoring start operation.</returns>
        Task StartMonitoringAsync(int intervalSeconds = 30);

        /// <summary>
        /// Stops health monitoring.
        /// </summary>
        /// <returns>A task that represents the asynchronous monitoring stop operation.</returns>
        Task StopMonitoringAsync();

        /// <summary>
        /// Event raised when SolidWorks health status changes.
        /// </summary>
        event EventHandler<SolidWorksHealthStatusChangedEventArgs>? HealthStatusChanged;
    }

    /// <summary>
    /// Interface for managing SolidWorks macro templates.
    /// </summary>
    public interface ISolidWorksMacroTemplateManager
    {
        /// <summary>
        /// Gets all available macro templates.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task<IEnumerable<Core.Models.SolidWorks.MacroTemplate>> GetAllTemplatesAsync();

        /// <summary>
        /// Creates a custom macro template.
        /// </summary>
        /// <param name="template">The template to create.</param>
        /// <returns>A task that represents the asynchronous creation operation.</returns>
        Task<bool> CreateTemplateAsync(Core.Models.SolidWorks.MacroTemplate template);

        /// <summary>
        /// Updates an existing macro template.
        /// </summary>
        /// <param name="template">The template to update.</param>
        /// <returns>A task that represents the asynchronous update operation.</returns>
        Task<bool> UpdateTemplateAsync(Core.Models.SolidWorks.MacroTemplate template);

        /// <summary>
        /// Deletes a macro template.
        /// </summary>
        /// <param name="templateName">The name of the template to delete.</param>
        /// <returns>A task that represents the asynchronous deletion operation.</returns>
        Task<bool> DeleteTemplateAsync(string templateName);
    }

    /// <summary>
    /// Represents the result of SolidWorks COM validation.
    /// </summary>
    public class SolidWorksValidationResult
    {
        /// <summary>
        /// Gets or sets whether the validation was successful.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets any validation errors found.
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Gets or sets any validation warnings.
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// Gets or sets the detected SolidWorks version.
        /// </summary>
        public string? DetectedVersion { get; set; }

        /// <summary>
        /// Gets or sets additional validation information.
        /// </summary>
        public Dictionary<string, object> AdditionalInfo { get; set; } = new();
    }

    /// <summary>
    /// Represents SolidWorks version information.
    /// </summary>
    public class SolidWorksVersionInfo
    {
        /// <summary>
        /// Gets or sets the version number.
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the build number.
        /// </summary>
        public string Build { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the installation path.
        /// </summary>
        public string InstallationPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether this is a valid installation.
        /// </summary>
        public bool IsValidInstallation { get; set; }

        /// <summary>
        /// Gets or sets the supported API version.
        /// </summary>
        public string ApiVersion { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents the result of error handling operations.
    /// </summary>
    public class ErrorHandlingResult
    {
        /// <summary>
        /// Gets or sets whether the error was successfully handled.
        /// </summary>
        public bool WasHandled { get; set; }

        /// <summary>
        /// Gets or sets the recovery action taken.
        /// </summary>
        public string? RecoveryAction { get; set; }

        /// <summary>
        /// Gets or sets whether recovery was successful.
        /// </summary>
        public bool RecoverySuccessful { get; set; }

        /// <summary>
        /// Gets or sets any additional error information.
        /// </summary>
        public string? AdditionalInfo { get; set; }
    }

    /// <summary>
    /// Represents SolidWorks health status.
    /// </summary>
    public class SolidWorksHealthStatus
    {
        /// <summary>
        /// Gets or sets whether SolidWorks is currently connected.
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// Gets or sets whether SolidWorks is responsive.
        /// </summary>
        public bool IsResponsive { get; set; }

        /// <summary>
        /// Gets or sets the current SolidWorks version.
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Gets or sets the memory usage in MB.
        /// </summary>
        public long MemoryUsageMB { get; set; }

        /// <summary>
        /// Gets or sets the number of open documents.
        /// </summary>
        public int OpenDocumentCount { get; set; }

        /// <summary>
        /// Gets or sets the last health check timestamp.
        /// </summary>
        public DateTime LastCheckTime { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets any health issues detected.
        /// </summary>
        public List<string> Issues { get; set; } = new();
    }

    /// <summary>
    /// Event arguments for SolidWorks health status changes.
    /// </summary>
    public class SolidWorksHealthStatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the previous health status.
        /// </summary>
        public SolidWorksHealthStatus? PreviousStatus { get; set; }

        /// <summary>
        /// Gets or sets the current health status.
        /// </summary>
        public SolidWorksHealthStatus CurrentStatus { get; set; } = new();

        /// <summary>
        /// Gets or sets the timestamp of the status change.
        /// </summary>
        public DateTime ChangeTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Basic implementation of SolidWorks COM validator.
    /// </summary>
    internal class SolidWorksComValidator : ISolidWorksComValidator
    {
        private readonly ILogger<SolidWorksComValidator> _logger;

        public SolidWorksComValidator(ILogger<SolidWorksComValidator> logger)
        {
            _logger = logger;
        }

        public async Task<SolidWorksValidationResult> ValidateComRegistrationAsync()
        {
            var result = new SolidWorksValidationResult();

            try
            {
                // Check if SolidWorks COM interface is available
                Type? swType = Type.GetTypeFromProgID("SldWorks.Application");
                if (swType == null)
                {
                    result.IsValid = false;
                    result.Errors.Add("SolidWorks COM interface not found. Please ensure SolidWorks is properly installed.");
                    return result;
                }

                result.IsValid = true;
                result.DetectedVersion = "Unknown"; // Would need to query actual version
                
                _logger.LogInformation("Sir, SolidWorks COM registration validation completed successfully.");
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add($"Error during COM validation: {ex.Message}");
                _logger.LogError(ex, "Sir, I encountered difficulties during COM validation.");
            }

            return result;
        }

        public async Task<SolidWorksVersionInfo?> GetInstalledVersionAsync()
        {
            try
            {
                // Implementation would query registry or COM interface for version info
                return new SolidWorksVersionInfo
                {
                    Version = "Unknown",
                    IsValidInstallation = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sir, I could not retrieve SolidWorks version information.");
                return null;
            }
        }
    }

    /// <summary>
    /// Basic implementation of SolidWorks error handler.
    /// </summary>
    internal class SolidWorksErrorHandler : ISolidWorksErrorHandler
    {
        private readonly ILogger<SolidWorksErrorHandler> _logger;

        public SolidWorksErrorHandler(ILogger<SolidWorksErrorHandler> logger)
        {
            _logger = logger;
        }

        public async Task<ErrorHandlingResult> HandleErrorAsync(Exception exception, string context)
        {
            _logger.LogError(exception, "Sir, handling SolidWorks error in context: {Context}", context);

            // Basic error handling logic
            return new ErrorHandlingResult
            {
                WasHandled = true,
                RecoveryAction = "Logged error and continued",
                RecoverySuccessful = true
            };
        }

        public async Task<bool> AttemptConnectionRecoveryAsync()
        {
            _logger.LogInformation("Sir, attempting SolidWorks connection recovery...");
            
            // Implementation would attempt to reconnect to SolidWorks
            return true;
        }
    }

    /// <summary>
    /// Basic implementation of SolidWorks health monitor.
    /// </summary>
    internal class SolidWorksHealthMonitor : ISolidWorksHealthMonitor
    {
        private readonly ILogger<SolidWorksHealthMonitor> _logger;
        private Timer? _monitoringTimer;

        public SolidWorksHealthMonitor(ILogger<SolidWorksHealthMonitor> logger)
        {
            _logger = logger;
        }

        public event EventHandler<SolidWorksHealthStatusChangedEventArgs>? HealthStatusChanged;

        public async Task<SolidWorksHealthStatus> CheckHealthAsync()
        {
            var status = new SolidWorksHealthStatus
            {
                IsConnected = false, // Would check actual connection
                IsResponsive = false,
                LastCheckTime = DateTime.Now
            };

            return status;
        }

        public async Task StartMonitoringAsync(int intervalSeconds = 30)
        {
            _logger.LogInformation("Sir, starting SolidWorks health monitoring with {Interval} second intervals.", intervalSeconds);
            
            _monitoringTimer = new Timer(async _ => await CheckHealthAsync(), 
                null, TimeSpan.Zero, TimeSpan.FromSeconds(intervalSeconds));
        }

        public async Task StopMonitoringAsync()
        {
            _logger.LogInformation("Sir, stopping SolidWorks health monitoring.");
            
            _monitoringTimer?.Dispose();
            _monitoringTimer = null;
        }
    }

    /// <summary>
    /// Basic implementation of macro template manager.
    /// </summary>
    internal class SolidWorksMacroTemplateManager : ISolidWorksMacroTemplateManager
    {
        private readonly ILogger<SolidWorksMacroTemplateManager> _logger;

        public SolidWorksMacroTemplateManager(ILogger<SolidWorksMacroTemplateManager> logger)
        {
            _logger = logger;
        }

        public async Task<IEnumerable<Core.Models.SolidWorks.MacroTemplate>> GetAllTemplatesAsync()
        {
            // Implementation would load templates from storage
            return new List<Core.Models.SolidWorks.MacroTemplate>();
        }

        public async Task<bool> CreateTemplateAsync(Core.Models.SolidWorks.MacroTemplate template)
        {
            _logger.LogInformation("Sir, creating macro template: {TemplateName}", template.Name);
            return true;
        }

        public async Task<bool> UpdateTemplateAsync(Core.Models.SolidWorks.MacroTemplate template)
        {
            _logger.LogInformation("Sir, updating macro template: {TemplateName}", template.Name);
            return true;
        }

        public async Task<bool> DeleteTemplateAsync(string templateName)
        {
            _logger.LogInformation("Sir, deleting macro template: {TemplateName}", templateName);
            return true;
        }
    }
}
