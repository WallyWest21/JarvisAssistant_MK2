using JarvisAssistant.Core.Interfaces;

namespace JarvisAssistant.MAUI.Services
{
    /// <summary>
    /// MAUI implementation of the dialog service.
    /// </summary>
    public class MauiDialogService : IDialogService
    {
        /// <summary>
        /// Displays an alert dialog.
        /// </summary>
        public async Task DisplayAlertAsync(string title, string message, string cancel)
        {
            if (Application.Current?.MainPage != null)
            {
                await Application.Current.MainPage.DisplayAlert(title, message, cancel);
            }
        }

        /// <summary>
        /// Displays a confirmation dialog.
        /// </summary>
        public async Task<bool> DisplayConfirmAsync(string title, string message, string accept, string cancel)
        {
            if (Application.Current?.MainPage != null)
            {
                return await Application.Current.MainPage.DisplayAlert(title, message, accept, cancel);
            }
            return false;
        }

        /// <summary>
        /// Displays a prompt dialog for text input.
        /// </summary>
        public async Task<string?> DisplayPromptAsync(string title, string message, string accept = "OK", string cancel = "Cancel", 
            string? placeholder = null, int maxLength = -1, string initialValue = "")
        {
            if (Application.Current?.MainPage != null)
            {
                return await Application.Current.MainPage.DisplayPromptAsync(title, message, accept, cancel, placeholder, maxLength, Keyboard.Default, initialValue);
            }
            return null;
        }

        /// <summary>
        /// Displays an action sheet with multiple options.
        /// </summary>
        public async Task<string?> DisplayActionSheetAsync(string title, string cancel, string? destruction = null, params string[] buttons)
        {
            if (Application.Current?.MainPage != null)
            {
                return await Application.Current.MainPage.DisplayActionSheet(title, cancel, destruction, buttons);
            }
            return null;
        }
    }
}