using System.Threading.Tasks;

namespace JarvisAssistant.Core.Interfaces
{
    /// <summary>
    /// Service for managing API rate limiting to prevent quota exhaustion.
    /// </summary>
    public interface IRateLimitService
    {
        /// <summary>
        /// Checks if a request can be made without exceeding rate limits.
        /// </summary>
        /// <param name="apiKey">API key identifier.</param>
        /// <returns>True if request can be made, false if rate limited.</returns>
        Task<bool> CanMakeRequestAsync(string apiKey);

        /// <summary>
        /// Records a successful API request.
        /// </summary>
        /// <param name="apiKey">API key identifier.</param>
        /// <param name="characterCount">Number of characters processed.</param>
        /// <returns>Task representing the operation.</returns>
        Task RecordRequestAsync(string apiKey, int characterCount);

        /// <summary>
        /// Gets the estimated wait time before next request can be made.
        /// </summary>
        /// <param name="apiKey">API key identifier.</param>
        /// <returns>Wait time or null if no wait required.</returns>
        Task<TimeSpan?> GetWaitTimeAsync(string apiKey);

        /// <summary>
        /// Gets current rate limiting statistics.
        /// </summary>
        /// <param name="apiKey">API key identifier.</param>
        /// <returns>Dictionary containing rate limit statistics.</returns>
        Dictionary<string, object> GetStatistics(string apiKey);

        /// <summary>
        /// Resets rate limiting data for a specific API key.
        /// </summary>
        /// <param name="apiKey">API key identifier.</param>
        /// <returns>Task representing the operation.</returns>
        Task ResetAsync(string apiKey);
    }
}
