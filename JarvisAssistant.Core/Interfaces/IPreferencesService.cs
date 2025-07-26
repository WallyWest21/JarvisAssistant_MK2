namespace JarvisAssistant.Core.Interfaces
{
    /// <summary>
    /// Interface for application preferences management.
    /// </summary>
    public interface IPreferencesService
    {
        /// <summary>
        /// Gets a preference value.
        /// </summary>
        /// <typeparam name="T">The type of the preference value.</typeparam>
        /// <param name="key">The preference key.</param>
        /// <param name="defaultValue">The default value if the preference doesn't exist.</param>
        /// <returns>The preference value or default value.</returns>
        T Get<T>(string key, T defaultValue);

        /// <summary>
        /// Sets a preference value.
        /// </summary>
        /// <typeparam name="T">The type of the preference value.</typeparam>
        /// <param name="key">The preference key.</param>
        /// <param name="value">The preference value.</param>
        void Set<T>(string key, T value);

        /// <summary>
        /// Removes a preference.
        /// </summary>
        /// <param name="key">The preference key.</param>
        void Remove(string key);

        /// <summary>
        /// Checks if a preference exists.
        /// </summary>
        /// <param name="key">The preference key.</param>
        /// <returns>True if the preference exists, false otherwise.</returns>
        bool Contains(string key);

        /// <summary>
        /// Clears all preferences.
        /// </summary>
        void Clear();
    }
}
