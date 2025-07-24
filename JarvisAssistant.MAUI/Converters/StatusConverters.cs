using System.Globalization;
using JarvisAssistant.Core.Models;

namespace JarvisAssistant.MAUI.ViewModels
{
    /// <summary>
    /// Converts ServiceState to color for status indicators.
    /// </summary>
    public class ServiceStateToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is ServiceState state)
            {
                return state switch
                {
                    ServiceState.Online => Colors.Green,
                    ServiceState.Degraded => Colors.Orange,
                    ServiceState.Offline => Colors.Red,
                    ServiceState.Error => Colors.Red,
                    ServiceState.Starting => Colors.Blue,
                    ServiceState.Stopping => Colors.Purple,
                    _ => Colors.Gray
                };
            }
            return Colors.Gray;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts ServiceState to icon for status indicators.
    /// </summary>
    public class ServiceStateToIconConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is ServiceState state)
            {
                return state switch
                {
                    ServiceState.Online => "✓",
                    ServiceState.Degraded => "⚠",
                    ServiceState.Offline => "✕",
                    ServiceState.Error => "⚠",
                    ServiceState.Starting => "⟳",
                    ServiceState.Stopping => "◼",
                    _ => "?"
                };
            }
            return "?";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts response time metrics to text display.
    /// </summary>
    public class ResponseTimeToTextConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Dictionary<string, object> metrics)
            {
                if (metrics.TryGetValue("response_time_ms", out var responseTime))
                {
                    if (responseTime is int ms)
                    {
                        return $"({ms}ms)";
                    }
                }
            }
            return string.Empty;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts boolean to opacity for fade effects.
    /// </summary>
    public class BoolToOpacityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? 0.8 : 0.0;
            }
            return 0.0;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts boolean to inverted boolean.
    /// </summary>
    public class InvertedBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return true;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }
    }

    /// <summary>
    /// Converts expanded state to translation Y for panel animation.
    /// </summary>
    public class ExpandedToTranslationConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isExpanded)
            {
                return isExpanded ? 0 : -400; // Slide up/down 400 units
            }
            return -400;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
