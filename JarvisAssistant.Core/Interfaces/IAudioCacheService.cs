using System.Threading.Tasks;

namespace JarvisAssistant.Core.Interfaces
{
    /// <summary>
    /// Service for caching audio responses to reduce API calls and improve performance.
    /// </summary>
    public interface IAudioCacheService
    {
        /// <summary>
        /// Gets cached audio data for the specified text and voice settings.
        /// </summary>
        /// <param name="text">The text that was synthesized.</param>
        /// <param name="voiceId">The voice ID used.</param>
        /// <param name="voiceSettings">The voice settings used.</param>
        /// <returns>Cached audio data or null if not found.</returns>
        Task<byte[]?> GetCachedAudioAsync(string text, string voiceId, object voiceSettings);

        /// <summary>
        /// Stores audio data in the cache.
        /// </summary>
        /// <param name="text">The text that was synthesized.</param>
        /// <param name="voiceId">The voice ID used.</param>
        /// <param name="voiceSettings">The voice settings used.</param>
        /// <param name="audioData">The audio data to cache.</param>
        /// <returns>True if successfully cached, false otherwise.</returns>
        Task<bool> CacheAudioAsync(string text, string voiceId, object voiceSettings, byte[] audioData);

        /// <summary>
        /// Clears expired entries from the cache.
        /// </summary>
        /// <returns>Number of entries removed.</returns>
        Task<int> ClearExpiredEntriesAsync();

        /// <summary>
        /// Gets current cache statistics.
        /// </summary>
        /// <returns>Dictionary containing cache statistics.</returns>
        Dictionary<string, object> GetStatistics();

        /// <summary>
        /// Clears all cached entries.
        /// </summary>
        /// <returns>Number of entries removed.</returns>
        Task<int> ClearAllAsync();
    }
}
