using System.Globalization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using JarvisAssistant.Core.Models;
using JarvisAssistant.Core.Converters;

namespace JarvisAssistant.MAUI.Converters
{
    public class InvertedBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return ConverterLogic.InvertBool(value as bool?);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return ConverterLogic.InvertBool(value as bool?);
        }
    }

    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isConnected)
            {
                return isConnected 
                    ? Color.FromArgb("#4CAF50") // Green for connected
                    : Color.FromArgb("#F44336"); // Red for disconnected
            }
            return Color.FromArgb("#9E9E9E"); // Gray for unknown
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StringToBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return ConverterLogic.StringToBool(value);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MessageTypeToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is MessageType messageType)
            {
                var hexColor = ConverterLogic.MessageTypeToColorHex(messageType);
                return Color.FromArgb(hexColor);
            }
            return Color.FromArgb("#4A148C");
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class MessageTypeToIconConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is MessageType messageType)
            {
                return ConverterLogic.MessageTypeToIcon(messageType);
            }
            return "";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PlatformToVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var currentPlatform = DeviceInfo.Current.Idiom.ToString();
            var targetPlatform = parameter?.ToString();
            return ConverterLogic.PlatformMatches(targetPlatform, currentPlatform);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class VoiceActivityToOpacityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return ConverterLogic.VoiceActivityToOpacity(value as double?);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
