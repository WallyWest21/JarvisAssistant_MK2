using JarvisAssistant.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace JarvisAssistant.MAUI.Services
{
    /// <summary>
    /// Platform detection and optimization service for MAUI applications.
    /// </summary>
    public class PlatformService : IPlatformService
    {
        private readonly ILogger<PlatformService> _logger;
        private PlatformType? _cachedPlatform;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlatformService"/> class.
        /// </summary>
        /// <param name="logger">The logger for recording platform operations.</param>
        public PlatformService(ILogger<PlatformService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the current platform type.
        /// </summary>
        public PlatformType CurrentPlatform
        {
            get
            {
                if (_cachedPlatform.HasValue)
                    return _cachedPlatform.Value;

                _cachedPlatform = DetectPlatform();
                _logger.LogInformation("Detected platform: {Platform}", _cachedPlatform);
                return _cachedPlatform.Value;
            }
        }

        /// <summary>
        /// Determines if the current platform is Google TV or Android TV.
        /// </summary>
        /// <returns>True if running on Google TV/Android TV, false otherwise.</returns>
        public bool IsGoogleTV()
        {
            try
            {
#if ANDROID
                var context = Platform.CurrentActivity?.ApplicationContext ?? Android.App.Application.Context;
                if (context != null)
                {
                    var packageManager = context.PackageManager;
                    
                    // Check for Android TV features
                    var hasLeanback = packageManager?.HasSystemFeature("android.software.leanback") ?? false;
                    var hasTouchscreen = packageManager?.HasSystemFeature("android.hardware.touchscreen") ?? true;
                    
                    // Android TV typically has leanback support and no touchscreen requirement
                    var isAndroidTV = hasLeanback && !hasTouchscreen;
                    
                    _logger.LogDebug("Android TV detection - Leanback: {HasLeanback}, Touchscreen: {HasTouchscreen}, IsTV: {IsTV}", 
                        hasLeanback, hasTouchscreen, isAndroidTV);
                    
                    return isAndroidTV;
                }
#endif
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to detect Google TV platform");
                return false;
            }
        }

        /// <summary>
        /// Gets the optimal theme for the current platform.
        /// </summary>
        /// <returns>The recommended theme for the current platform.</returns>
        public Core.Interfaces.AppTheme GetOptimalTheme()
        {
            var platform = CurrentPlatform;
            
            return platform switch
            {
                PlatformType.Windows or PlatformType.MacOS => Core.Interfaces.AppTheme.Desktop,
                PlatformType.Android when IsGoogleTV() => Core.Interfaces.AppTheme.TV,
                PlatformType.Android or PlatformType.iOS => Core.Interfaces.AppTheme.Mobile,
                _ => Core.Interfaces.AppTheme.System
            };
        }

        /// <summary>
        /// Determines if the current platform supports multi-panel layouts.
        /// </summary>
        /// <returns>True if multi-panel layouts are supported, false otherwise.</returns>
        public bool SupportsMultiPanel()
        {
            return CurrentPlatform switch
            {
                PlatformType.Windows or PlatformType.MacOS => true,
                PlatformType.Android when IsGoogleTV() => true,
                _ => false
            };
        }

        /// <summary>
        /// Gets the recommended screen density for the current platform.
        /// </summary>
        /// <returns>The screen density multiplier.</returns>
        public double GetScreenDensity()
        {
            try
            {
#if ANDROID
                var density = Android.Content.Res.Resources.System?.DisplayMetrics?.Density ?? 1.0f;
                return density;
#elif WINDOWS
                // Windows uses system DPI scaling
                return 1.0; // Will be handled by system DPI awareness
#elif IOS
                var scale = UIKit.UIScreen.MainScreen?.Scale ?? 1.0;
                return scale;
#else
                return 1.0;
#endif
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get screen density, using default");
                return 1.0;
            }
        }

        /// <summary>
        /// Determines if the current platform primarily uses touch input.
        /// </summary>
        /// <returns>True if touch is the primary input method, false otherwise.</returns>
        public bool IsTouchPrimary()
        {
            return CurrentPlatform switch
            {
                PlatformType.Android when !IsGoogleTV() => true,
                PlatformType.iOS => true,
                _ => false
            };
        }

        /// <summary>
        /// Determines if the current platform supports voice input.
        /// </summary>
        /// <returns>True if voice input is available, false otherwise.</returns>
        public bool SupportsVoiceInput()
        {
            return CurrentPlatform switch
            {
                PlatformType.Android => true,
                PlatformType.iOS => true,
                PlatformType.Windows => true,
                _ => false
            };
        }

        /// <summary>
        /// Detects the current platform type.
        /// </summary>
        /// <returns>The detected platform type.</returns>
        private PlatformType DetectPlatform()
        {
            try
            {
#if ANDROID
                return IsGoogleTV() ? PlatformType.AndroidTV : PlatformType.Android;
#elif WINDOWS
                return PlatformType.Windows;
#elif IOS
                return PlatformType.iOS;
#elif MACCATALYST
                return PlatformType.MacOS;
#else
                return PlatformType.Unknown;
#endif
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to detect platform, defaulting to Unknown");
                return PlatformType.Unknown;
            }
        }
    }
}
