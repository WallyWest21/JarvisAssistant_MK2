using JarvisAssistant.Core.Models;

namespace JarvisAssistant.Core.Interfaces
{
    /// <summary>
    /// Provides methods for optimized model loading and management.
    /// </summary>
    public interface IModelOptimizationService
    {
        /// <summary>
        /// Loads a model with optimal settings for the current hardware.
        /// </summary>
        /// <param name="modelName">Name of the model to load.</param>
        /// <param name="optimizationLevel">Level of optimization to apply.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Model loading result with performance metrics.</returns>
        Task<ModelLoadResult> LoadModelAsync(string modelName, OptimizationLevel optimizationLevel = OptimizationLevel.Balanced, CancellationToken cancellationToken = default);

        /// <summary>
        /// Loads a model with specific settings.
        /// </summary>
        /// <param name="modelInfo">Model information and specifications.</param>
        /// <param name="loadSettings">Model loading settings.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Model loading result with performance metrics.</returns>
        Task<ModelLoadResult> LoadModelAsync(ModelInfo modelInfo, ModelLoadSettings loadSettings, CancellationToken cancellationToken = default);

        /// <summary>
        /// Unloads a model to free memory.
        /// </summary>
        /// <param name="modelName">Name of the model to unload.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Success status.</returns>
        Task<bool> UnloadModelAsync(string modelName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Pre-warms frequently used models.
        /// </summary>
        /// <param name="modelNames">List of models to pre-warm.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Pre-warming results.</returns>
        Task<Dictionary<string, ModelWarmupResult>> PreWarmModelsAsync(IEnumerable<string> modelNames, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets currently loaded models and their memory usage.
        /// </summary>
        /// <returns>Dictionary of loaded models and their memory footprint.</returns>
        Task<Dictionary<string, ModelMemoryInfo>> GetLoadedModelsAsync();

        /// <summary>
        /// Optimizes model for specific use case.
        /// </summary>
        /// <param name="modelName">Model to optimize.</param>
        /// <param name="useCase">Specific use case for optimization.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Optimization result.</returns>
        Task<ModelOptimizationResult> OptimizeModelForUseCaseAsync(string modelName, ModelUseCase useCase, CancellationToken cancellationToken = default);

        /// <summary>
        /// Manages automatic model unloading based on usage patterns.
        /// </summary>
        /// <param name="strategy">Unloading strategy to use.</param>
        /// <returns>Task representing the operation.</returns>
        Task ConfigureAutoUnloadingAsync(ModelUnloadStrategy strategy);

        /// <summary>
        /// Gets model optimization recommendations based on current usage.
        /// </summary>
        /// <returns>List of optimization recommendations.</returns>
        Task<IEnumerable<OptimizationRecommendation>> GetOptimizationRecommendationsAsync();
    }
}
