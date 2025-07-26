namespace JarvisAssistant.Core.Models
{
    /// <summary>
    /// Represents a single step in the onboarding process.
    /// </summary>
    public class OnboardingStep
    {
        /// <summary>
        /// Gets or sets the unique identifier for this step.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display title for this step.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description or instructions for this step.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the icon or emoji to display for this step.
        /// </summary>
        public string Icon { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether this step is required.
        /// </summary>
        public bool IsRequired { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether this step has been completed.
        /// </summary>
        public bool IsCompleted { get; set; } = false;

        /// <summary>
        /// Gets or sets the settings key associated with this step (if any).
        /// </summary>
        public string? SettingsKey { get; set; }

        /// <summary>
        /// Gets or sets the order in which this step should be presented.
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// Gets or sets additional metadata for this step.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();

        /// <summary>
        /// Gets or sets the action to perform when this step is activated.
        /// </summary>
        public OnboardingStepAction? Action { get; set; }

        /// <summary>
        /// Gets or sets default settings for this step.
        /// </summary>
        public Dictionary<string, object>? DefaultSettings { get; set; }
    }

    /// <summary>
    /// Represents the overall progress of the onboarding process.
    /// </summary>
    public class OnboardingProgress
    {
        /// <summary>
        /// Gets or sets the total number of onboarding steps.
        /// </summary>
        public int TotalSteps { get; set; }

        /// <summary>
        /// Gets or sets the number of completed steps.
        /// </summary>
        public int CompletedSteps { get; set; }

        /// <summary>
        /// Gets or sets the current step index (0-based).
        /// </summary>
        public int CurrentStepIndex { get; set; } = 0;

        /// <summary>
        /// Gets or sets a value indicating whether onboarding is complete.
        /// </summary>
        public bool IsComplete { get; set; } = false;

        /// <summary>
        /// Gets or sets the completion percentage (0-100).
        /// </summary>
        public double CompletionPercentage => TotalSteps > 0 ? (double)CompletedSteps / TotalSteps * 100 : 0;

        /// <summary>
        /// Gets or sets the time when onboarding was started.
        /// </summary>
        public DateTime? StartedAt { get; set; }

        /// <summary>
        /// Gets or sets the time when onboarding was completed.
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// Gets or sets any steps that were skipped.
        /// </summary>
        public List<string> SkippedSteps { get; set; } = new();

        /// <summary>
        /// Gets or sets the list of completed step IDs.
        /// </summary>
        public List<string> CompletedStepIds { get; set; } = new();

        /// <summary>
        /// Gets or sets a value indicating whether onboarding is completed.
        /// </summary>
        public bool IsCompleted 
        { 
            get => IsComplete; 
            set => IsComplete = value; 
        }

        /// <summary>
        /// Gets the progress percentage as an alternative property name.
        /// </summary>
        public double ProgressPercentage => CompletionPercentage;
    }

    /// <summary>
    /// Represents an action that can be performed during an onboarding step.
    /// </summary>
    public class OnboardingStepAction
    {
        /// <summary>
        /// Gets or sets the type of action to perform.
        /// </summary>
        public OnboardingActionType Type { get; set; }

        /// <summary>
        /// Gets or sets the target of the action (e.g., navigation route, setting key).
        /// </summary>
        public string Target { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional parameters for the action.
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Represents a contextual tip for users.
    /// </summary>
    public class OnboardingTip
    {
        /// <summary>
        /// Gets or sets the unique identifier for this tip.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the title of the tip.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the tip.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether this tip is contextual.
        /// </summary>
        public bool IsContextual { get; set; }

        /// <summary>
        /// Gets or sets the text for any action associated with this tip.
        /// </summary>
        public string? ActionText { get; set; }

        /// <summary>
        /// Gets or sets the command to execute when the tip action is triggered.
        /// </summary>
        public string? ActionCommand { get; set; }

        /// <summary>
        /// Gets or sets the icon or emoji for this tip.
        /// </summary>
        public string? Icon { get; set; }

        /// <summary>
        /// Gets or sets the priority of this tip.
        /// </summary>
        public int Priority { get; set; } = 0;
    }

    /// <summary>
    /// Types of actions that can be performed during onboarding.
    /// </summary>
    public enum OnboardingActionType
    {
        /// <summary>
        /// No specific action required.
        /// </summary>
        None,

        /// <summary>
        /// Navigate to a specific page or route.
        /// </summary>
        Navigate,

        /// <summary>
        /// Update a setting or preference.
        /// </summary>
        UpdateSetting,

        /// <summary>
        /// Show a dialog or popup.
        /// </summary>
        ShowDialog,

        /// <summary>
        /// Execute a custom action.
        /// </summary>
        Custom,

        /// <summary>
        /// Request a permission from the user.
        /// </summary>
        RequestPermission,

        /// <summary>
        /// Initialize a service or feature.
        /// </summary>
        InitializeFeature
    }

    /// <summary>
    /// Event arguments for onboarding-related events.
    /// </summary>
    public class OnboardingEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the onboarding step associated with this event.
        /// </summary>
        public OnboardingStep Step { get; set; } = new();

        /// <summary>
        /// Gets or sets the current progress when this event occurred.
        /// </summary>
        public OnboardingProgress Progress { get; set; } = new();

        /// <summary>
        /// Gets or sets any additional event data.
        /// </summary>
        public Dictionary<string, object> EventData { get; set; } = new();
    }
}