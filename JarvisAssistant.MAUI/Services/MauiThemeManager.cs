using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Services;
using Microsoft.Extensions.Logging;
using CoreAppTheme = JarvisAssistant.Core.Interfaces.AppTheme;
using MauiAppTheme = Microsoft.Maui.ApplicationModel.AppTheme;

namespace JarvisAssistant.MAUI.Services
{
    /// <summary>
    /// MAUI-specific implementation of the theme manager that handles resource dictionary loading and application theming.
    /// </summary>
    public class MauiThemeManager : ThemeManager
    {
        private readonly Dictionary<CoreAppTheme, ResourceDictionary> _themeResources = new();

        public MauiThemeManager(ILogger<ThemeManager> logger, IPlatformService? platformService = null)
            : base(logger, platformService)
        {
        }

        /// <summary>
        /// Loads the theme resource dictionary for the specified theme.
        /// </summary>
        /// <param name="theme">The theme to load resources for.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override async Task LoadThemeResourceDictionary(CoreAppTheme theme)
        {
            try
            {
                var resourceDictionary = await LoadMauiThemeResourceDictionary(theme);
                
                if (resourceDictionary != null)
                {
                    _themeResources[theme] = resourceDictionary;
                }

                await base.LoadThemeResourceDictionary(theme);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load MAUI theme resources for {theme}", ex);
            }
        }

        /// <summary>
        /// Applies the theme using MAUI-specific mechanisms.
        /// </summary>
        /// <param name="theme">The theme to apply.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override async Task ApplyPlatformThemeAsync(CoreAppTheme theme)
        {
            try
            {
                await Task.Run(() =>
                {
                    // Apply MAUI application theme
                    var mauiTheme = theme switch
                    {
                        CoreAppTheme.Light => MauiAppTheme.Light,
                        CoreAppTheme.Dark => MauiAppTheme.Dark,
                        _ => MauiAppTheme.Unspecified
                    };

                    if (Application.Current != null)
                    {
                        Application.Current.UserAppTheme = mauiTheme;
                    }

                    // Apply resource dictionaries
                    ApplyThemeResources(theme);
                });

                // Force UI refresh
                await RefreshUIAsync();

                await base.ApplyPlatformThemeAsync(theme);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to apply MAUI theme {theme}", ex);
            }
        }

        /// <summary>
        /// Gets the system theme preference using MAUI APIs.
        /// </summary>
        /// <returns>The system's preferred theme.</returns>
        protected override CoreAppTheme GetPlatformSystemTheme()
        {
            try
            {
#if ANDROID || IOS || MACCATALYST || WINDOWS
                if (Application.Current != null)
                {
                    var currentTheme = Application.Current.RequestedTheme;
                    return currentTheme switch
                    {
                        MauiAppTheme.Light => CoreAppTheme.Light,
                        MauiAppTheme.Dark => CoreAppTheme.Dark,
                        _ => CoreAppTheme.Light
                    };
                }
#endif
                return CoreAppTheme.Light;
            }
            catch
            {
                return CoreAppTheme.Light;
            }
        }

        /// <summary>
        /// Gets the stored theme preference from MAUI Preferences.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation with the stored theme string.</returns>
        protected override Task<string> GetStoredThemePreferenceAsync()
        {
            try
            {
#if ANDROID || IOS || MACCATALYST || WINDOWS
                return Task.FromResult(Preferences.Get("AppTheme", CoreAppTheme.System.ToString()));
#else
                return Task.FromResult(CoreAppTheme.System.ToString());
#endif
            }
            catch
            {
                return Task.FromResult(CoreAppTheme.System.ToString());
            }
        }

        /// <summary>
        /// Sets the theme preference in MAUI Preferences.
        /// </summary>
        /// <param name="themeString">The theme string to store.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override Task SetStoredThemePreferenceAsync(string themeString)
        {
            try
            {
#if ANDROID || IOS || MACCATALYST || WINDOWS
                Preferences.Set("AppTheme", themeString);
#endif
                return Task.CompletedTask;
            }
            catch
            {
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// Loads a MAUI resource dictionary for the specified theme.
        /// </summary>
        private async Task<ResourceDictionary?> LoadMauiThemeResourceDictionary(CoreAppTheme theme)
        {
            try
            {
                var resourceName = GetThemeResourceName(theme);
                if (string.IsNullOrEmpty(resourceName))
                {
                    return null;
                }

                await Task.Delay(1); // Yield control for async pattern

                // Create the resource dictionary from embedded resource
                var resourceDictionary = new ResourceDictionary();
                
                // In a real implementation, you would load the XAML resource
                // var resourcePath = $"Resources/Themes/{resourceName}.xaml";
                // resourceDictionary.Source = new Uri(resourcePath, UriKind.Relative);
                
                return resourceDictionary;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the resource name for a theme.
        /// </summary>
        private string? GetThemeResourceName(CoreAppTheme theme)
        {
            return theme switch
            {
                CoreAppTheme.Desktop => "DesktopTheme",
                CoreAppTheme.Mobile => "MobileTheme",
                CoreAppTheme.TV => "TVTheme",
                CoreAppTheme.Light => "BaseTheme",
                CoreAppTheme.Dark => "BaseTheme",
                CoreAppTheme.System => null,
                _ => null
            };
        }

        /// <summary>
        /// Applies theme resource dictionaries to the application.
        /// </summary>
        private void ApplyThemeResources(CoreAppTheme theme)
        {
            try
            {
                if (Application.Current?.Resources?.MergedDictionaries != null && _themeResources.ContainsKey(theme))
                {
                    // Remove existing theme resources
                    var toRemove = Application.Current.Resources.MergedDictionaries
                        .Where(d => _themeResources.Values.Contains(d))
                        .ToList();

                    foreach (var dict in toRemove)
                    {
                        Application.Current.Resources.MergedDictionaries.Remove(dict);
                    }

                    // Add new theme resources
                    var newResource = _themeResources[theme];
                    Application.Current.Resources.MergedDictionaries.Add(newResource);
                }
            }
            catch
            {
                // Non-critical error, continue execution
            }
        }

        /// <summary>
        /// Forces a UI refresh to apply theme changes.
        /// </summary>
        private async Task RefreshUIAsync()
        {
            try
            {
                await Task.Run(() =>
                {
                    if (Application.Current?.MainPage != null)
                    {
                        // Use opacity manipulation to force a visual refresh
                        var currentOpacity = Application.Current.MainPage.Opacity;
                        Application.Current.MainPage.Opacity = 0.99;
                        Application.Current.MainPage.Opacity = currentOpacity;

                        // If we have a Shell, refresh its appearance
                        if (Shell.Current != null)
                        {
                            // Force a layout update by slightly changing a property
                            var currentPadding = Shell.Current.Padding;
                            Shell.Current.Padding = new Thickness(currentPadding.Left, currentPadding.Top, 
                                currentPadding.Right, currentPadding.Bottom + 0.01);
                            Shell.Current.Padding = currentPadding;
                        }
                    }
                });
            }
            catch
            {
                // Non-critical error, continue execution
            }
        }
    }
}
