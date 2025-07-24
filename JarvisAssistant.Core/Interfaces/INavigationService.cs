namespace JarvisAssistant.Core.Interfaces
{
    /// <summary>
    /// Interface for navigation services to enable testing without MAUI dependencies.
    /// </summary>
    public interface INavigationService
    {
        /// <summary>
        /// Navigates to a page by route.
        /// </summary>
        /// <param name="route">The navigation route.</param>
        /// <returns>A task representing the navigation operation.</returns>
        Task NavigateToAsync(string route);

        /// <summary>
        /// Navigates to a page by route with parameters.
        /// </summary>
        /// <param name="route">The navigation route.</param>
        /// <param name="parameters">The navigation parameters.</param>
        /// <returns>A task representing the navigation operation.</returns>
        Task NavigateToAsync(string route, IDictionary<string, object> parameters);

        /// <summary>
        /// Navigates back in the navigation stack.
        /// </summary>
        /// <returns>A task representing the navigation operation.</returns>
        Task GoBackAsync();

        /// <summary>
        /// Navigates to the root page.
        /// </summary>
        /// <returns>A task representing the navigation operation.</returns>
        Task PopToRootAsync();
    }
}