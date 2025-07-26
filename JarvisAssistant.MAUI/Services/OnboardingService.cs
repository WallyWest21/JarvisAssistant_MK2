using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models;
using Microsoft.Extensions.Logging;

namespace JarvisAssistant.MAUI.Services
{
    /// <summary>
    /// Service for managing first-run tutorial and onboarding experience.
    /// </summary>
    public class OnboardingService : IOnboardingService
    {
        private readonly ILogger<OnboardingService> _logger;
        private readonly ITelemetryService _telemetryService;
        private readonly IPreferencesService _preferencesService;

        public OnboardingService(
            ILogger<OnboardingService> logger,
            ITelemetryService telemetryService,
            IPreferencesService preferencesService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _telemetryService = telemetryService ?? throw new ArgumentNullException(nameof(telemetryService));
            _preferencesService = preferencesService ?? throw new ArgumentNullException(nameof(preferencesService));
        }

        /// <inheritdoc/>
        public bool IsOnboardingRequired => !_preferencesService.Get("onboarding_completed", false);

        /// <inheritdoc/>
        public async Task<OnboardingStep[]> GetOnboardingStepsAsync()
        {
            await _telemetryService.TrackEventAsync("OnboardingStepsRequested");

            return new OnboardingStep[]
            {
                new OnboardingStep
                {
                    Id = "welcome",
                    Title = "Welcome to JARVIS",
                    Description = "Your AI-powered assistant is ready to help you with tasks, answer questions, and learn from your interactions.",
                    Icon = "🤖",
                    IsRequired = true,
                    Order = 0
                },
                new OnboardingStep
                {
                    Id = "voice_setup",
                    Title = "Voice Interaction",
                    Description = "Enable voice commands to interact with JARVIS naturally. You can always change this in settings.",
                    Icon = "🎤",
                    IsRequired = false,
                    SettingsKey = "voice_enabled",
                    Order = 1
                },
                new OnboardingStep
                {
                    Id = "privacy_settings",
                    Title = "Privacy & Analytics",
                    Description = "Help improve JARVIS by sharing anonymous usage data. Your privacy is our priority - you can opt out anytime.",
                    Icon = "🔒",
                    IsRequired = true,
                    SettingsKey = "telemetry_enabled",
                    Order = 2
                },
                new OnboardingStep
                {
                    Id = "theme_selection",
                    Title = "Choose Your Style",
                    Description = "Select a theme that matches your preference. JARVIS adapts to your system theme by default.",
                    Icon = "🎨",
                    IsRequired = false,
                    SettingsKey = "theme",
                    Order = 3
                },
                new OnboardingStep
                {
                    Id = "tutorial_chat",
                    Title = "Try It Out",
                    Description = "Let's start with a simple conversation. Ask JARVIS anything or try 'What can you do?'",
                    Icon = "💬",
                    IsRequired = true,
                    Order = 4
                },
                new OnboardingStep
                {
                    Id = "completion",
                    Title = "You're All Set!",
                    Description = "JARVIS is ready to assist you. Access settings anytime from the menu to customize your experience.",
                    Icon = "✅",
                    IsRequired = true,
                    Order = 5
                }
            };
        }

        /// <inheritdoc/>
        public async Task<OnboardingProgress> GetProgressAsync()
        {
            var steps = await GetOnboardingStepsAsync();
            var completedSteps = new List<string>();

            foreach (var step in steps)
            {
                var isCompleted = _preferencesService.Get($"onboarding_step_{step.Id}_completed", false);
                if (isCompleted)
                {
                    completedSteps.Add(step.Id);
                }
            }

            return new OnboardingProgress
            {
                TotalSteps = steps.Length,
                CompletedSteps = completedSteps.Count,
                CompletedStepIds = completedSteps,
                IsComplete = completedSteps.Count == steps.Length,
                StartedAt = GetStartTime(),
                CompletedAt = GetCompletionTime()
            };
        }

        /// <inheritdoc/>
        public async Task CompleteStepAsync(string stepId, Dictionary<string, object>? stepData = null)
        {
            _preferencesService.Set($"onboarding_step_{stepId}_completed", true);
            
            var properties = new Dictionary<string, object>
            {
                ["stepId"] = stepId,
                ["completedAt"] = DateTime.UtcNow
            };

            if (stepData != null)
            {
                foreach (var kvp in stepData)
                {
                    properties[kvp.Key] = kvp.Value;
                }
            }

            await _telemetryService.TrackEventAsync("OnboardingStepCompleted", properties);
            _logger.LogDebug("Onboarding step completed: {StepId}", stepId);

            // Check if all steps are completed
            var progress = await GetProgressAsync();
            if (progress.IsComplete)
            {
                await CompleteOnboardingAsync();
            }
        }

        /// <inheritdoc/>
        public async Task CompleteOnboardingAsync(Dictionary<string, object>? completionData = null)
        {
            _preferencesService.Set("onboarding_completed", true);
            _preferencesService.Set("onboarding_completed_at", DateTime.UtcNow.ToString("O"));

            var progress = await GetProgressAsync();
            var properties = new Dictionary<string, object>
            {
                ["totalSteps"] = progress.TotalSteps,
                ["completedAt"] = DateTime.UtcNow
            };

            if (completionData != null)
            {
                foreach (var kvp in completionData)
                {
                    properties[kvp.Key] = kvp.Value;
                }
            }

            await _telemetryService.TrackEventAsync("OnboardingCompleted", properties);
            _logger.LogInformation("Onboarding completed");
        }

        /// <inheritdoc/>
        public async Task ResetOnboardingAsync()
        {
            var steps = await GetOnboardingStepsAsync();
            
            foreach (var step in steps)
            {
                _preferencesService.Remove($"onboarding_step_{step.Id}_completed");
            }

            _preferencesService.Remove("onboarding_completed");
            _preferencesService.Remove("onboarding_completed_at");
            _preferencesService.Remove("onboarding_started_at");

            await _telemetryService.TrackEventAsync("OnboardingReset");
            _logger.LogInformation("Onboarding reset");
        }

        /// <inheritdoc/>
        public async Task SkipOnboardingAsync(string? reason = null)
        {
            _preferencesService.Set("onboarding_completed", true);
            _preferencesService.Set("onboarding_skipped", true);
            _preferencesService.Set("onboarding_skip_reason", reason ?? "user_choice");
            _preferencesService.Set("onboarding_completed_at", DateTime.UtcNow.ToString("O"));

            await _telemetryService.TrackEventAsync("OnboardingSkipped", new Dictionary<string, object>
            {
                ["reason"] = reason ?? "user_choice",
                ["skippedAt"] = DateTime.UtcNow
            });

            _logger.LogInformation("Onboarding skipped with reason: {Reason}", reason ?? "user_choice");
        }

        /// <inheritdoc/>
        public async Task<OnboardingTip[]> GetContextualTipsAsync(string context)
        {
            await _telemetryService.TrackEventAsync("ContextualTipsRequested", new Dictionary<string, object>
            {
                ["context"] = context
            });

            return context.ToLowerInvariant() switch
            {
                "chat" => new OnboardingTip[]
                {
                    new OnboardingTip
                    {
                        Id = "voice_commands",
                        Title = "Voice Commands",
                        Description = "Tap the microphone button to speak to JARVIS directly.",
                        IsContextual = true,
                        Icon = "🎤",
                        Priority = 1
                    },
                    new OnboardingTip
                    {
                        Id = "conversation_history",
                        Title = "Conversation History",
                        Description = "Scroll up to see previous messages and continue conversations.",
                        IsContextual = true,
                        Icon = "📜",
                        Priority = 2
                    }
                },
                "settings" => new OnboardingTip[]
                {
                    new OnboardingTip
                    {
                        Id = "privacy_controls",
                        Title = "Privacy Controls",
                        Description = "Manage what data JARVIS can access and how it's used.",
                        IsContextual = true,
                        Icon = "🔒",
                        Priority = 1
                    },
                    new OnboardingTip
                    {
                        Id = "performance_tuning",
                        Title = "Performance Settings",
                        Description = "Adjust performance settings based on your device capabilities.",
                        IsContextual = true,
                        Icon = "⚙️",
                        Priority = 2
                    }
                },
                "voice" => new OnboardingTip[]
                {
                    new OnboardingTip
                    {
                        Id = "voice_activation",
                        Title = "Voice Activation",
                        Description = "Say 'Hey JARVIS' to start a voice conversation.",
                        IsContextual = true,
                        Icon = "🗣️",
                        Priority = 1,
                        ActionText = "Try it now",
                        ActionCommand = "enable_voice_activation"
                    },
                    new OnboardingTip
                    {
                        Id = "voice_commands_list",
                        Title = "Voice Commands",
                        Description = "Try commands like 'What's the weather?' or 'Set a reminder'.",
                        IsContextual = true,
                        Icon = "📝",
                        Priority = 2,
                        ActionText = "View commands",
                        ActionCommand = "show_voice_commands"
                    }
                },
                _ => Array.Empty<OnboardingTip>()
            };
        }

        private DateTime? GetStartTime()
        {
            var startTimeStr = _preferencesService.Get("onboarding_started_at", "");
            if (DateTime.TryParse(startTimeStr, out var startTime))
            {
                return startTime;
            }
            return null;
        }

        private DateTime? GetCompletionTime()
        {
            var completionTimeStr = _preferencesService.Get("onboarding_completed_at", "");
            if (DateTime.TryParse(completionTimeStr, out var completionTime))
            {
                return completionTime;
            }
            return null;
        }
    }
}
