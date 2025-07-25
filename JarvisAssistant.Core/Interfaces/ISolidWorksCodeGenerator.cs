using JarvisAssistant.Core.Models.SolidWorks;

namespace JarvisAssistant.Core.Interfaces
{
    /// <summary>
    /// Provides methods for generating VBA macro templates and code with Jarvis personality.
    /// Specializes in creating intelligent, well-commented SolidWorks automation code.
    /// </summary>
    public interface ISolidWorksCodeGenerator
    {
        /// <summary>
        /// Generates a complete VBA macro with Jarvis-style comments and error handling.
        /// </summary>
        /// <param name="request">The code generation request containing specifications.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the generated code.</returns>
        Task<CodeGenerationResult> GenerateVBAMacroAsync(VBAGenerationRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a parametric part creation macro with intelligent feature management.
        /// </summary>
        /// <param name="request">The parametric part generation request.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the parametric macro code.</returns>
        Task<CodeGenerationResult> GenerateParametricPartMacroAsync(ParametricPartRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a feature modification script with undo/redo support.
        /// </summary>
        /// <param name="request">The feature modification request.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the modification script.</returns>
        Task<CodeGenerationResult> GenerateFeatureModificationScriptAsync(FeatureModificationRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates assembly automation code with intelligent mate handling.
        /// </summary>
        /// <param name="request">The assembly automation request.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the assembly automation code.</returns>
        Task<CodeGenerationResult> GenerateAssemblyAutomationAsync(AssemblyAutomationRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates drawing creation and annotation scripts.
        /// </summary>
        /// <param name="request">The drawing generation request.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the drawing automation code.</returns>
        Task<CodeGenerationResult> GenerateDrawingAutomationAsync(DrawingAutomationRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets predefined macro templates with Jarvis personality.
        /// </summary>
        /// <param name="templateType">The type of template to retrieve.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the template code.</returns>
        Task<MacroTemplate> GetMacroTemplateAsync(MacroTemplateType templateType);

        /// <summary>
        /// Validates generated VBA code for syntax and SolidWorks API compliance.
        /// </summary>
        /// <param name="vbaCode">The VBA code to validate.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains validation results.</returns>
        Task<CodeValidationResult> ValidateVBACodeAsync(string vbaCode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds Jarvis-style comments and personality to existing VBA code.
        /// </summary>
        /// <param name="existingCode">The existing VBA code to enhance.</param>
        /// <param name="enhancementLevel">The level of enhancement to apply.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the enhanced code.</returns>
        Task<string> EnhanceCodeWithJarvisPersonalityAsync(string existingCode, CodeEnhancementLevel enhancementLevel = CodeEnhancementLevel.Standard, CancellationToken cancellationToken = default);
    }
}
