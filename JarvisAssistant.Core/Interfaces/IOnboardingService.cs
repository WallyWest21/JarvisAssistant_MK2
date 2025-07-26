using JarvisAssistant.Core.Models;

namespace JarvisAssistant.Core.Interfaces
{
    /// <summary>
    /// Interface for managing application onboarding and first-run experience.
    /// </summary>
    public interface IOnboardingService
    {
        /// <summary>
        /// Gets a value indicating whether onboarding is required for the current user.
        /// </summary>
        bool IsOnboardingRequired { get; }

        /// <summary>
        /// Gets the onboarding steps for the current application state.
        /// </summary>
        /// <returns>An array of onboarding steps to present to the user.</returns>
        Task<OnboardingStep[]> GetOnboardingStepsAsync();

        /// <summary>
        /// Marks a specific onboarding step as completed.
        /// </summary>
        /// <param name="stepId">The unique identifier of the completed step.</param>
        /// <param name="stepData">Optional data collected during the step.</param>
        Task CompleteStepAsync(string stepId, Dictionary<string, object>? stepData = null);

        /// <summary>
        /// Marks the entire onboarding process as completed.
        /// </summary>
        /// <param name="completionData">Optional completion data to store.</param>
        Task CompleteOnboardingAsync(Dictionary<string, object>? completionData = null);

        /// <summary>
        /// Resets the onboarding state, requiring it to be completed again.
        /// </summary>
        Task ResetOnboardingAsync();

        /// <summary>
        /// Gets the current onboarding progress.
        /// </summary>
        /// <returns>Current progress information.</returns>
        Task<OnboardingProgress> GetProgressAsync();

        /// <summary>
        /// Skips the onboarding process entirely.
        /// </summary>
        /// <param name="reason">Optional reason for skipping.</param>
        Task SkipOnboardingAsync(string? reason = null);

        /// <summary>
        /// Gets contextual tips for a specific area of the application.
        /// </summary>
        /// <param name="context">The context for which to get tips.</param>
        /// <returns>An array of contextual tips.</returns>
        Task<OnboardingTip[]> GetContextualTipsAsync(string context);
    }
}