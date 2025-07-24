using JarvisAssistant.Core.Interfaces;

namespace JarvisAssistant.UnitTests.Mocks
{
    /// <summary>
    /// Mock implementation of IDialogService for testing.
    /// </summary>
    public class MockDialogService : IDialogService
    {
        public List<AlertCall> AlertCalls { get; } = new();
        public List<ConfirmCall> ConfirmCalls { get; } = new();
        public List<ActionSheetCall> ActionSheetCalls { get; } = new();
        public List<PromptCall> PromptCalls { get; } = new();

        public bool DefaultConfirmResult { get; set; } = true;
        public string DefaultActionSheetResult { get; set; } = string.Empty;
        public string DefaultPromptResult { get; set; } = string.Empty;

        public Task DisplayAlertAsync(string title, string message, string cancel)
        {
            AlertCalls.Add(new AlertCall(title, message, cancel));
            return Task.CompletedTask;
        }

        public Task<bool> DisplayConfirmAsync(string title, string message, string accept, string cancel)
        {
            ConfirmCalls.Add(new ConfirmCall(title, message, accept, cancel));
            return Task.FromResult(DefaultConfirmResult);
        }

        public Task<string?> DisplayActionSheetAsync(string title, string cancel, string? destruction = null, params string[] buttons)
        {
            ActionSheetCalls.Add(new ActionSheetCall(title, cancel, destruction ?? string.Empty, buttons));
            return Task.FromResult<string?>(DefaultActionSheetResult);
        }

        public Task<string?> DisplayPromptAsync(string title, string message, string accept = "OK", string cancel = "Cancel", string? placeholder = null, int maxLength = -1, string initialValue = "")
        {
            PromptCalls.Add(new PromptCall(title, message, accept, cancel, placeholder, maxLength, initialValue));
            return Task.FromResult<string?>(DefaultPromptResult);
        }

        public void Reset()
        {
            AlertCalls.Clear();
            ConfirmCalls.Clear();
            ActionSheetCalls.Clear();
            PromptCalls.Clear();
            DefaultConfirmResult = true;
            DefaultActionSheetResult = string.Empty;
            DefaultPromptResult = string.Empty;
        }

        public record AlertCall(string Title, string Message, string Cancel);
        public record ConfirmCall(string Title, string Message, string Accept, string Cancel);
        public record ActionSheetCall(string Title, string Cancel, string Destruction, string[] Buttons);
        public record PromptCall(string Title, string Message, string Accept, string Cancel, string? Placeholder, int MaxLength, string InitialValue);
    }
}