using JarvisAssistant.Core.Interfaces;

namespace JarvisAssistant.UnitTests.Mocks
{
    /// <summary>
    /// Mock implementation of IDialogService for testing.
    /// </summary>
    public class MockDialogService : IDialogService
    {
        public DisplayAlertCall? LastDisplayedAlert { get; private set; }
        public DisplayConfirmCall? LastDisplayedConfirm { get; private set; }
        public DisplayPromptCall? LastDisplayedPrompt { get; private set; }
        public DisplayActionSheetCall? LastDisplayedActionSheet { get; private set; }

        public List<DisplayAlertCall> AlertHistory { get; } = new();
        public List<DisplayConfirmCall> ConfirmHistory { get; } = new();
        public List<DisplayPromptCall> PromptHistory { get; } = new();
        public List<DisplayActionSheetCall> ActionSheetHistory { get; } = new();

        // Default responses for interactive dialogs
        public bool DefaultConfirmResponse { get; set; } = true;
        public string? DefaultPromptResponse { get; set; } = "Test Input";
        public string? DefaultActionSheetResponse { get; set; } = "OK";

        public Task DisplayAlertAsync(string title, string message, string cancel)
        {
            var call = new DisplayAlertCall(title, message, cancel);
            LastDisplayedAlert = call;
            AlertHistory.Add(call);
            return Task.CompletedTask;
        }

        public Task<bool> DisplayConfirmAsync(string title, string message, string accept, string cancel)
        {
            var call = new DisplayConfirmCall(title, message, accept, cancel);
            LastDisplayedConfirm = call;
            ConfirmHistory.Add(call);
            return Task.FromResult(DefaultConfirmResponse);
        }

        public Task<string?> DisplayPromptAsync(string title, string message, string accept = "OK", string cancel = "Cancel", 
            string? placeholder = null, int maxLength = -1, string initialValue = "")
        {
            var call = new DisplayPromptCall(title, message, accept, cancel, placeholder, maxLength, initialValue);
            LastDisplayedPrompt = call;
            PromptHistory.Add(call);
            return Task.FromResult(DefaultPromptResponse);
        }

        public Task<string?> DisplayActionSheetAsync(string title, string cancel, string? destruction = null, params string[] buttons)
        {
            var call = new DisplayActionSheetCall(title, cancel, destruction, buttons);
            LastDisplayedActionSheet = call;
            ActionSheetHistory.Add(call);
            return Task.FromResult(DefaultActionSheetResponse);
        }

        public void Reset()
        {
            LastDisplayedAlert = null;
            LastDisplayedConfirm = null;
            LastDisplayedPrompt = null;
            LastDisplayedActionSheet = null;
            AlertHistory.Clear();
            ConfirmHistory.Clear();
            PromptHistory.Clear();
            ActionSheetHistory.Clear();
        }
    }

    // Record types for tracking dialog calls
    public record DisplayAlertCall(string Title, string Message, string Cancel);
    public record DisplayConfirmCall(string Title, string Message, string Accept, string Cancel);
    public record DisplayPromptCall(string Title, string Message, string Accept, string Cancel, string? Placeholder, int MaxLength, string InitialValue);
    public record DisplayActionSheetCall(string Title, string Cancel, string? Destruction, string[] Buttons);
}