using JarvisAssistant.Core.Interfaces;

namespace JarvisAssistant.MAUI.Services
{
    /// <summary>
    /// MAUI implementation of the navigation service.
    /// </summary>
    public class MauiNavigationService : INavigationService
    {
        /// <summary>
        /// Navigates to a page by route.
        /// </summary>
        public async Task NavigateToAsync(string route)
        {
            await Shell.Current.GoToAsync(route);
        }

        /// <summary>
        /// Navigates to a page by route with parameters.
        /// </summary>
        public async Task NavigateToAsync(string route, IDictionary<string, object> parameters)
        {
            await Shell.Current.GoToAsync(route, parameters);
        }

        /// <summary>
        /// Navigates back in the navigation stack.
        /// </summary>
        public async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }

        /// <summary>
        /// Navigates to the root page.
        /// </summary>
        public async Task PopToRootAsync()
        {
            await Shell.Current.GoToAsync("//");
        }
    }
}