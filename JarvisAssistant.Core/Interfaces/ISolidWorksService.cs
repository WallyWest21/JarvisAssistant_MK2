using JarvisAssistant.Core.Models.SolidWorks;

namespace JarvisAssistant.Core.Interfaces
{
    /// <summary>
    /// Provides methods for integrating with SolidWorks CAD software.
    /// Implements COM interop for SolidWorks API access and code generation capabilities.
    /// </summary>
    public interface ISolidWorksService
    {
        /// <summary>
        /// Connects to the currently running SolidWorks application instance.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. Returns true if connection is successful.</returns>
        Task<bool> ConnectToSolidWorksAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Starts SolidWorks application if not already running.
        /// </summary>
        /// <param name="silent">If true, starts SolidWorks in background mode.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. Returns true if startup is successful.</returns>
        Task<bool> StartSolidWorksAsync(bool silent = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Disconnects from the SolidWorks application and releases COM objects.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task DisconnectAsync();

        /// <summary>
        /// Generates a VBA macro for creating parametric parts with Jarvis-style comments.
        /// </summary>
        /// <param name="request">The macro generation request containing specifications.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the generated macro code.</returns>
        Task<MacroGenerationResponse> GeneratePartMacroAsync(MacroGenerationRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a VBA macro for assembly automation with intelligent features.
        /// </summary>
        /// <param name="request">The assembly macro generation request.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the generated assembly macro.</returns>
        Task<MacroGenerationResponse> GenerateAssemblyMacroAsync(AssemblyMacroRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a VBA macro for drawing automation and documentation.
        /// </summary>
        /// <param name="request">The drawing macro generation request.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the generated drawing macro.</returns>
        Task<MacroGenerationResponse> GenerateDrawingMacroAsync(DrawingMacroRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes a SolidWorks part file and provides optimization suggestions.
        /// </summary>
        /// <param name="filePath">Path to the SolidWorks part file (.sldprt).</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the analysis results.</returns>
        Task<PartAnalysisResult> AnalyzePartFileAsync(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a VBA macro in the connected SolidWorks instance.
        /// </summary>
        /// <param name="macroPath">Path to the macro file (.swp or .dll).</param>
        /// <param name="moduleName">Name of the module containing the macro.</param>
        /// <param name="procedureName">Name of the procedure to execute.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. Returns true if execution is successful.</returns>
        Task<bool> ExecuteMacroAsync(string macroPath, string moduleName, string procedureName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current connection status to SolidWorks.
        /// </summary>
        /// <returns>True if connected to SolidWorks, false otherwise.</returns>
        bool IsConnected { get; }

        /// <summary>
        /// Gets the version of the connected SolidWorks application.
        /// </summary>
        /// <returns>SolidWorks version string, or null if not connected.</returns>
        string? SolidWorksVersion { get; }

        /// <summary>
        /// Event raised when the connection status to SolidWorks changes.
        /// </summary>
        event EventHandler<SolidWorksConnectionEventArgs>? ConnectionStatusChanged;
    }
}
