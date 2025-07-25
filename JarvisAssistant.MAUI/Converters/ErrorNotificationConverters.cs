using JarvisAssistant.Core.Models;
using System.Globalization;

namespace JarvisAssistant.MAUI.Converters
{
    /// <summary>
    /// Converts ErrorSeverity enum values to appropriate icon strings for display.
    /// Uses Material Design Icons or Unicode symbols for cross-platform compatibility.
    /// </summary>
    public class SeverityToIconConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is ErrorSeverity severity)
            {
                return severity switch
                {
                    ErrorSeverity.Critical => "⚠", // Critical warning symbol
                    ErrorSeverity.Error => "✕", // X mark
                    ErrorSeverity.Warning => "⚠", // Warning triangle
                    ErrorSeverity.Info => "ℹ", // Information symbol
                    ErrorSeverity.Fatal => "�", // Fatal symbol
                    _ => "•" // Bullet point for unknown
                };
            }

            return "•";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException("SeverityToIconConverter is one-way only.");
        }
    }

    /// <summary>
    /// Converts ErrorSeverity enum values to appropriate colors for Jarvis theme.
    /// Maintains consistency with Jarvis's sophisticated color palette.
    /// </summary>
    public class SeverityToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is ErrorSeverity severity)
            {
                return severity switch
                {
                    ErrorSeverity.Critical => Color.FromArgb("#FF4444"), // Bright red for critical
                    ErrorSeverity.Error => Color.FromArgb("#D4593A"), // Jarvis error red
                    ErrorSeverity.Warning => Color.FromArgb("#E6B800"), // Jarvis warning gold
                    ErrorSeverity.Info => Color.FromArgb("#3A93D4"), // Jarvis info blue
                    ErrorSeverity.Fatal => Color.FromArgb("#800000"), // Dark red for fatal
                    _ => Color.FromArgb("#CCCCCC") // Default light gray
                };
            }

            return Color.FromArgb("#CCCCCC");
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException("SeverityToColorConverter is one-way only.");
        }
    }

    /// <summary>
    /// Converts integer values to boolean for visibility binding.
    /// Returns true if integer is greater than 0.
    /// </summary>
    public class IntToBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int intValue)
            {
                return intValue > 0;
            }

            if (value != null && int.TryParse(value.ToString(), out var parsedValue))
            {
                return parsedValue > 0;
            }

            return false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException("IntToBoolConverter is one-way only.");
        }
    }

    /// <summary>
    /// Converts ErrorSeverity to background color for notification containers.
    /// Provides subtle background tinting based on error severity.
    /// </summary>
    public class SeverityToBackgroundConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is ErrorSeverity severity)
            {
                return severity switch
                {
                    ErrorSeverity.Critical => Color.FromArgb("#3D1010"), // Dark red background
                    ErrorSeverity.Error => Color.FromArgb("#2D1810"), // Jarvis error background
                    ErrorSeverity.Warning => Color.FromArgb("#2D2410"), // Jarvis warning background
                    ErrorSeverity.Info => Color.FromArgb("#102D2D"), // Jarvis info background
                    ErrorSeverity.Fatal => Color.FromArgb("#200000"), // Very dark red for fatal
                    _ => Color.FromArgb("#1A0D2E") // Default Jarvis purple
                };
            }

            return Color.FromArgb("#1A0D2E");
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException("SeverityToBackgroundConverter is one-way only.");
        }
    }

    /// <summary>
    /// Converts boolean values to inverse boolean for complementary visibility.
    /// Useful for showing/hiding elements based on opposite conditions.
    /// </summary>
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }

            return true;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }

            return false;
        }
    }

    /// <summary>
    /// Converts DateTime values to relative time strings (e.g., "2 minutes ago").
    /// Provides user-friendly time representation for notifications.
    /// </summary>
    public class DateTimeToRelativeConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                var timeSpan = DateTime.UtcNow - dateTime;

                if (timeSpan.TotalMinutes < 1)
                {
                    return "Just now";
                }
                else if (timeSpan.TotalMinutes < 60)
                {
                    var minutes = (int)timeSpan.TotalMinutes;
                    return $"{minutes} minute{(minutes == 1 ? "" : "s")} ago";
                }
                else if (timeSpan.TotalHours < 24)
                {
                    var hours = (int)timeSpan.TotalHours;
                    return $"{hours} hour{(hours == 1 ? "" : "s")} ago";
                }
                else if (timeSpan.TotalDays < 7)
                {
                    var days = (int)timeSpan.TotalDays;
                    return $"{days} day{(days == 1 ? "" : "s")} ago";
                }
                else
                {
                    return dateTime.ToString("MMM dd, yyyy", culture);
                }
            }

            return string.Empty;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException("DateTimeToRelativeConverter is one-way only.");
        }
    }

    /// <summary>
    /// Converts notification priority to opacity for visual hierarchy.
    /// Higher priority notifications appear more prominent.
    /// </summary>
    public class PriorityToOpacityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int priority)
            {
                // Map priority (0-10) to opacity (0.6-1.0)
                var normalizedPriority = Math.Max(0, Math.Min(10, priority)) / 10.0;
                return 0.6 + (normalizedPriority * 0.4);
            }

            return 1.0; // Default full opacity
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException("PriorityToOpacityConverter is one-way only.");
        }
    }

    /// <summary>
    /// Converts collection count to visibility boolean.
    /// Shows element only if collection has items.
    /// </summary>
    public class CollectionCountToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                return count > 0;
            }

            if (value is System.Collections.ICollection collection)
            {
                return collection.Count > 0;
            }

            return false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException("CollectionCountToVisibilityConverter is one-way only.");
        }
    }

    /// <summary>
    /// Converts error code to formatted display string.
    /// Formats error codes in a user-friendly way.
    /// </summary>
    public class ErrorCodeToDisplayConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string errorCode && !string.IsNullOrEmpty(errorCode))
            {
                // Format error codes like "LLM-CONN-001" to "LLM Connection Error (001)"
                var parts = errorCode.Split('-');
                if (parts.Length >= 3)
                {
                    var service = parts[0];
                    var category = parts[1];
                    var number = parts[2];

                    var categoryDisplay = category switch
                    {
                        "CONN" => "Connection",
                        "AUTH" => "Authentication",
                        "PROC" => "Processing",
                        "MEM" => "Memory",
                        "CONF" => "Configuration",
                        _ => category
                    };

                    return $"{service} {categoryDisplay} ({number})";
                }

                return errorCode;
            }

            return string.Empty;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException("ErrorCodeToDisplayConverter is one-way only.");
        }
    }
}
