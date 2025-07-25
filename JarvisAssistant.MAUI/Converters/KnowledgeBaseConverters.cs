using System.Globalization;

namespace JarvisAssistant.MAUI.Converters
{
    /// <summary>
    /// Converter that converts an object to a boolean (true if not null).
    /// </summary>
    public class ObjectToBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool invert = parameter?.ToString()?.Equals("Inverted", StringComparison.OrdinalIgnoreCase) == true;
            bool result = value != null;
            return invert ? !result : result;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter that converts a percentage (0-100) to a progress value (0-1).
    /// </summary>
    public class PercentageToProgressConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double doubleValue)
                return doubleValue / 100.0;
            
            if (value is float floatValue)
                return floatValue / 100.0f;
            
            if (value is int intValue)
                return intValue / 100.0;
            
            return 0.0;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double doubleValue)
                return doubleValue * 100.0;
            
            if (value is float floatValue)
                return floatValue * 100.0f;
            
            return 0;
        }
    }
}
