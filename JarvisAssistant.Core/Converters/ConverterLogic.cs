using JarvisAssistant.Core.Models;

namespace JarvisAssistant.Core.Converters
{
    public static class ConverterLogic
    {
        public static bool InvertBool(bool? value)
        {
            if (value is bool boolValue)
                return !boolValue;
            return false;
        }

        public static bool StringToBool(object? value)
        {
            return !string.IsNullOrWhiteSpace(value?.ToString());
        }

        public static string MessageTypeToColorHex(MessageType messageType)
        {
            return messageType switch
            {
                MessageType.Error => "#FF5722",
                MessageType.Voice => "#00E5FF",
                MessageType.Code => "#1A1A1A",
                MessageType.System => "#9C27B0",
                _ => "#4A148C"
            };
        }

        public static string MessageTypeToIcon(MessageType messageType)
        {
            return messageType switch
            {
                MessageType.Error => "?",
                MessageType.Voice => "??",
                MessageType.Code => "??",
                MessageType.System => "??",
                _ => ""
            };
        }

        public static double VoiceActivityToOpacity(double? value)
        {
            if (value is double activity)
            {
                return Math.Max(0.2, Math.Min(1.0, activity));
            }
            return 0.2;
        }

        public static bool PlatformMatches(string? targetPlatform, string? currentPlatform)
        {
            if (targetPlatform is null || currentPlatform is null)
                return false;
            
            return currentPlatform.Equals(targetPlatform, StringComparison.OrdinalIgnoreCase);
        }
    }
}