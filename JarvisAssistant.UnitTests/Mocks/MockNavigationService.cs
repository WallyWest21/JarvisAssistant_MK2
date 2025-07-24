using JarvisAssistant.Core.Interfaces;

namespace JarvisAssistant.UnitTests.Mocks
{
    /// <summary>
    /// Mock implementation of INavigationService for testing.
    /// </summary>
    public class MockNavigationService : INavigationService
    {
        public List<string> NavigationHistory { get; } = new();
        public string? ExpectedRoute { get; set; }
        public bool ShouldThrowOnNavigation { get; set; }
        public Exception? NavigationException { get; set; }

        public Task GoToAsync(string route)
        {
            if (ShouldThrowOnNavigation && NavigationException != null)
            {
                throw NavigationException;
            }

            NavigationHistory.Add(route);
            return Task.CompletedTask;
        }

        public Task GoToAsync(string route, IDictionary<string, object> parameters)
        {
            if (ShouldThrowOnNavigation && NavigationException != null)
            {
                throw NavigationException;
            }

            NavigationHistory.Add(route);
            return Task.CompletedTask;
        }

        public Task GoBackAsync()
        {
            if (NavigationHistory.Count > 0)
            {
                NavigationHistory.RemoveAt(NavigationHistory.Count - 1);
            }
            return Task.CompletedTask;
        }

        public Task<bool> CanGoBackAsync()
        {
            return Task.FromResult(NavigationHistory.Count > 0);
        }

        // Additional interface members that may be required
        public Task NavigateToAsync(string route)
        {
            return GoToAsync(route);
        }

        public Task NavigateToAsync(string route, IDictionary<string, object> parameters)
        {
            return GoToAsync(route, parameters);
        }

        public Task PopToRootAsync()
        {
            NavigationHistory.Clear();
            return Task.CompletedTask;
        }

        public void Reset()
        {
            NavigationHistory.Clear();
            ExpectedRoute = null;
            ShouldThrowOnNavigation = false;
            NavigationException = null;
        }
    }
}