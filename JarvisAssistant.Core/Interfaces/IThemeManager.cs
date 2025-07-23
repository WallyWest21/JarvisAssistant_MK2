namespace JarvisAssistant.Core.Interfaces
{
    /// <summary>
    /// Represents the available application themes.
    /// </summary>
    public enum AppTheme
    {
        /// <summary>
        /// Light theme with bright backgrounds and dark text.
        /// </summary>
        Light,

        /// <summary>
        /// Dark theme with dark backgrounds and light text.
        /// </summary>
        Dark,

        /// <summary>
        /// System theme that follows the operating system's theme preference.
        /// </summary>
        System,

        /// <summary>
        /// Desktop theme optimized for multi-panel layouts.
        /// </summary>
        Desktop,

        /// <summary>
        /// Mobile theme optimized for single-column layouts.
        /// </summary>
        Mobile,

        /// <summary>
        /// TV theme optimized for voice-first interface.
        /// </summary>
        TV
    }

    /// <summary>
    /// Provides methods for managing application themes and appearance.
    /// </summary>
    public interface IThemeManager
    {
        /// <summary>
        /// Gets the currently active theme.
        /// </summary>
        /// <value>The current application theme.</value>
        AppTheme CurrentTheme { get; }

        /// <summary>
        /// Gets the list of available themes for the current platform.
        /// </summary>
        /// <value>The available application themes.</value>
        IReadOnlyList<AppTheme> AvailableThemes { get; }

        /// <summary>
        /// Occurs when the current theme changes.
        /// </summary>
        event EventHandler<AppTheme>? ThemeChanged;

        /// <summary>
        /// Switches the application to the specified theme.
        /// </summary>
        /// <param name="theme">The theme to switch to.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task SwitchThemeAsync(AppTheme theme);

        /// <summary>
        /// Automatically selects and applies the optimal theme for the current platform.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task AutoSelectThemeAsync();

        /// <summary>
        /// Loads theme resources dynamically.
        /// </summary>
        /// <param name="theme">The theme for which to load resources.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task LoadThemeResourcesAsync(AppTheme theme);

        /// <summary>
        /// Gets the system's preferred theme (light or dark).
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the system's preferred theme.</returns>
        Task<AppTheme> GetSystemThemeAsync();

        /// <summary>
        /// Loads the theme preference from storage.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the stored theme preference.</returns>
        Task<AppTheme> LoadThemePreferenceAsync();

        /// <summary>
        /// Saves the current theme preference to storage.
        /// </summary>
        /// <param name="theme">The theme to save as preference.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task SaveThemePreferenceAsync(AppTheme theme);
    }
}
