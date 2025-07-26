using JarvisAssistant.Core.Interfaces;
using Microsoft.Maui.Storage;

namespace JarvisAssistant.MAUI.Services
{
    /// <summary>
    /// MAUI implementation of preferences service using Microsoft.Maui.Storage.Preferences.
    /// </summary>
    public class MauiPreferencesService : IPreferencesService
    {
        /// <inheritdoc/>
        public T Get<T>(string key, T defaultValue)
        {
            try
            {
                if (typeof(T) == typeof(string))
                {
                    return (T)(object)Preferences.Get(key, defaultValue?.ToString() ?? string.Empty);
                }
                else if (typeof(T) == typeof(bool))
                {
                    return (T)(object)Preferences.Get(key, Convert.ToBoolean(defaultValue));
                }
                else if (typeof(T) == typeof(int))
                {
                    return (T)(object)Preferences.Get(key, Convert.ToInt32(defaultValue));
                }
                else if (typeof(T) == typeof(long))
                {
                    return (T)(object)Preferences.Get(key, Convert.ToInt64(defaultValue));
                }
                else if (typeof(T) == typeof(double))
                {
                    return (T)(object)Preferences.Get(key, Convert.ToDouble(defaultValue));
                }
                else if (typeof(T) == typeof(float))
                {
                    return (T)(object)Preferences.Get(key, Convert.ToSingle(defaultValue));
                }
                else if (typeof(T) == typeof(DateTime))
                {
                    var stringValue = Preferences.Get(key, string.Empty);
                    if (DateTime.TryParse(stringValue, out var dateTime))
                    {
                        return (T)(object)dateTime;
                    }
                    return defaultValue;
                }
                else
                {
                    // For complex types, use JSON serialization
                    var stringValue = Preferences.Get(key, string.Empty);
                    if (!string.IsNullOrEmpty(stringValue))
                    {
                        try
                        {
                            return System.Text.Json.JsonSerializer.Deserialize<T>(stringValue) ?? defaultValue;
                        }
                        catch
                        {
                            return defaultValue;
                        }
                    }
                    return defaultValue;
                }
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <inheritdoc/>
        public void Set<T>(string key, T value)
        {
            try
            {
                if (value is string stringValue)
                {
                    Preferences.Set(key, stringValue);
                }
                else if (value is bool boolValue)
                {
                    Preferences.Set(key, boolValue);
                }
                else if (value is int intValue)
                {
                    Preferences.Set(key, intValue);
                }
                else if (value is long longValue)
                {
                    Preferences.Set(key, longValue);
                }
                else if (value is double doubleValue)
                {
                    Preferences.Set(key, doubleValue);
                }
                else if (value is float floatValue)
                {
                    Preferences.Set(key, floatValue);
                }
                else if (value is DateTime dateTimeValue)
                {
                    Preferences.Set(key, dateTimeValue.ToString("O"));
                }
                else
                {
                    // For complex types, use JSON serialization
                    var jsonValue = System.Text.Json.JsonSerializer.Serialize(value);
                    Preferences.Set(key, jsonValue);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting preference {key}: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public void Remove(string key)
        {
            try
            {
                Preferences.Remove(key);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error removing preference {key}: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public bool Contains(string key)
        {
            try
            {
                return Preferences.ContainsKey(key);
            }
            catch
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public void Clear()
        {
            try
            {
                Preferences.Clear();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing preferences: {ex.Message}");
            }
        }
    }
}
