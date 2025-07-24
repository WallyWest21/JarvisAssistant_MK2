namespace JarvisAssistant.Core.Interfaces
{
    /// <summary>
    /// Interface for platform-specific dialog services to enable testing.
    /// </summary>
    public interface IDialogService
    {
        /// <summary>
        /// Displays an alert dialog.
        /// </summary>
        /// <param name="title">The dialog title.</param>
        /// <param name="message">The dialog message.</param>
        /// <param name="cancel">The cancel button text.</param>
        /// <returns>A task representing the operation.</returns>
        Task DisplayAlertAsync(string title, string message, string cancel);

        /// <summary>
        /// Displays a confirmation dialog.
        /// </summary>
        /// <param name="title">The dialog title.</param>
        /// <param name="message">The dialog message.</param>
        /// <param name="accept">The accept button text.</param>
        /// <param name="cancel">The cancel button text.</param>
        /// <returns>A task representing the operation with a boolean result.</returns>
        Task<bool> DisplayConfirmAsync(string title, string message, string accept, string cancel);

        /// <summary>
        /// Displays a prompt dialog for text input.
        /// </summary>
        /// <param name="title">The dialog title.</param>
        /// <param name="message">The dialog message.</param>
        /// <param name="accept">The accept button text.</param>
        /// <param name="cancel">The cancel button text.</param>
        /// <param name="placeholder">The placeholder text.</param>
        /// <param name="maxLength">The maximum input length.</param>
        /// <param name="initialValue">The initial input value.</param>
        /// <returns>A task representing the operation with a string result.</returns>
        Task<string?> DisplayPromptAsync(string title, string message, string accept = "OK", string cancel = "Cancel", 
            string? placeholder = null, int maxLength = -1, string initialValue = "");

        /// <summary>
        /// Displays an action sheet with multiple options.
        /// </summary>
        /// <param name="title">The action sheet title.</param>
        /// <param name="cancel">The cancel button text.</param>
        /// <param name="destruction">The destruction button text (optional).</param>
        /// <param name="buttons">The action button texts.</param>
        /// <returns>A task representing the operation with the selected button text.</returns>
        Task<string?> DisplayActionSheetAsync(string title, string cancel, string? destruction = null, params string[] buttons);
    }
}