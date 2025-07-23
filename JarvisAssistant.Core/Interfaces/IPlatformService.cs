namespace JarvisAssistant.Core.Interfaces
{
    /// <summary>
    /// Represents the different platforms the application can run on.
    /// </summary>
    public enum PlatformType
    {
        /// <summary>
        /// Windows desktop platform.
        /// </summary>
        Windows,

        /// <summary>
        /// Android mobile platform.
        /// </summary>
        Android,

        /// <summary>
        /// Android TV platform.
        /// </summary>
        AndroidTV,

        /// <summary>
        /// iOS mobile platform.
        /// </summary>
        iOS,

        /// <summary>
        /// macOS desktop platform.
        /// </summary>
        MacOS,

        /// <summary>
        /// Unknown or unsupported platform.
        /// </summary>
        Unknown
    }

    /// <summary>
    /// Provides platform-specific detection and optimization services.
    /// </summary>
    public interface IPlatformService
    {
        /// <summary>
        /// Gets the current platform type.
        /// </summary>
        /// <value>The current platform type.</value>
        PlatformType CurrentPlatform { get; }

        /// <summary>
        /// Determines if the current platform is Google TV or Android TV.
        /// </summary>
        /// <returns>True if running on Google TV/Android TV, false otherwise.</returns>
        bool IsGoogleTV();

        /// <summary>
        /// Gets the optimal theme for the current platform.
        /// </summary>
        /// <returns>The recommended theme for the current platform.</returns>
        AppTheme GetOptimalTheme();

        /// <summary>
        /// Determines if the current platform supports multi-panel layouts.
        /// </summary>
        /// <returns>True if multi-panel layouts are supported, false otherwise.</returns>
        bool SupportsMultiPanel();

        /// <summary>
        /// Gets the recommended screen density for the current platform.
        /// </summary>
        /// <returns>The screen density multiplier.</returns>
        double GetScreenDensity();

        /// <summary>
        /// Determines if the current platform primarily uses touch input.
        /// </summary>
        /// <returns>True if touch is the primary input method, false otherwise.</returns>
        bool IsTouchPrimary();

        /// <summary>
        /// Determines if the current platform supports voice input.
        /// </summary>
        /// <returns>True if voice input is available, false otherwise.</returns>
        bool SupportsVoiceInput();
    }
}
