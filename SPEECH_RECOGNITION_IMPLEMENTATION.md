# Cross-Platform Speech Recognition Implementation

This document describes the comprehensive speech-to-text implementation for Jarvis Assistant that works on both Windows and Android platforms.

## Architecture Overview

The speech recognition system is built with a layered architecture:

1. **Core Interface Layer** (`JarvisAssistant.Core.Services`)
   - `ISpeechRecognitionService` - Main service interface
   - Core models and enums for speech recognition

2. **Base Implementation Layer** (`JarvisAssistant.Services.Speech`)
   - `SpeechRecognitionServiceBase` - Abstract base class with common functionality
   - State management and event handling

3. **Platform-Specific Layer** (`JarvisAssistant.MAUI.Platforms.*`)
   - `WindowsSpeechRecognitionService` - Windows implementation using Windows.Media.SpeechRecognition
   - `AndroidSpeechRecognitionService` - Android implementation using Android.Speech APIs

## Key Features

### Cross-Platform Compatibility
- **Windows**: Uses Windows.Media.SpeechRecognition API for native Windows speech recognition
- **Android**: Uses Android.Speech.SpeechRecognizer for native Android speech recognition
- Automatic platform detection and service registration

### Recognition Modes
- **Continuous Recognition**: Listens continuously and processes multiple utterances
- **Single Recognition**: Processes one utterance and stops
- **Partial Results**: Real-time streaming of partial recognition results

### Configuration Options
- Language selection (multiple languages supported)
- Confidence threshold settings
- Silence timeout configuration
- Maximum alternatives returned
- Profanity filtering

### Permission Management
- Automatic microphone permission requests
- Platform-specific permission handling
- Permission status reporting

## File Structure

```
JarvisAssistant.Core/
├── Services/
│   └── ISpeechRecognitionService.cs          # Core interface and models

JarvisAssistant.Services/
├── Speech/
│   └── SpeechRecognitionServiceBase.cs       # Base implementation

JarvisAssistant.MAUI/
├── Platforms/
│   ├── Windows/
│   │   └── Services/
│   │       └── WindowsSpeechRecognitionService.cs
│   ├── Android/
│   │   └── Services/
│   │       └── AndroidSpeechRecognitionService.cs
│   └── Android/
│       ├── AndroidManifest.xml              # Updated with permissions
│       └── MainActivity.cs                  # Updated with permission requests
├── ViewModels/
│   └── VoiceViewModel.cs                    # Example usage ViewModel
├── Views/
│   ├── VoicePage.xaml                       # Example UI
│   └── VoicePage.xaml.cs
├── Converters/
│   └── BooleanConverters.cs                 # UI converters
└── MauiProgram.cs                           # Service registration
```

## Core Interface

### ISpeechRecognitionService

```csharp
public interface ISpeechRecognitionService
{
    // Properties
    bool IsListening { get; }
    bool IsAvailable { get; }

    // Events
    event EventHandler<SpeechRecognitionResult>? SpeechRecognized;
    event EventHandler<string>? PartialResultsReceived;
    event EventHandler<SpeechRecognitionState>? StateChanged;

    // Methods
    Task<bool> StartListeningAsync(SpeechRecognitionOptions? options = null);
    Task StopListeningAsync();
    Task<SpeechRecognitionResult> RecognizeSpeechAsync(SpeechRecognitionOptions? options = null, CancellationToken cancellationToken = default);
    Task<PermissionStatus> RequestPermissionsAsync();
    Task<IEnumerable<string>> GetAvailableLanguagesAsync();
}
```

### Key Models

- **SpeechRecognitionResult**: Contains recognized text, confidence, alternatives, and metadata
- **SpeechRecognitionOptions**: Configuration for recognition behavior
- **SpeechRecognitionState**: Enum representing current recognition state
- **PermissionStatus**: Enum representing microphone permission status

## Platform-Specific Implementations

### Windows Implementation

Uses Windows.Media.SpeechRecognition APIs:
- `SpeechRecognizer` for recognition
- `SpeechContinuousRecognitionSession` for continuous listening
- Native Windows permission system

Key features:
- Real-time hypothesis generation
- Multiple language support
- Confidence scoring
- Alternative results

### Android Implementation

Uses Android.Speech APIs:
- `SpeechRecognizer` for recognition
- `IRecognitionListener` for event handling
- Android permissions system

Key features:
- Partial results streaming
- Intent-based configuration
- Error handling
- Background processing

## Usage Examples

### Basic Single Recognition

```csharp
var speechService = serviceProvider.GetService<ISpeechRecognitionService>();

// Check availability
if (!speechService.IsAvailable)
{
    // Handle unavailable speech recognition
    return;
}

// Request permissions
var permissionStatus = await speechService.RequestPermissionsAsync();
if (permissionStatus != PermissionStatus.Granted)
{
    // Handle denied permissions
    return;
}

// Recognize single utterance
var result = await speechService.RecognizeSpeechAsync(new SpeechRecognitionOptions
{
    Language = "en-US",
    EnablePartialResults = true,
    MaxAlternatives = 3
});

Console.WriteLine($"Recognized: {result.Text} (Confidence: {result.Confidence:P})");
```

### Continuous Recognition

```csharp
// Subscribe to events
speechService.SpeechRecognized += (sender, result) =>
{
    Console.WriteLine($"Speech: {result.Text}");
};

speechService.PartialResultsReceived += (sender, partial) =>
{
    Console.WriteLine($"Partial: {partial}");
};

// Start continuous recognition
var started = await speechService.StartListeningAsync(new SpeechRecognitionOptions
{
    ContinuousRecognition = true,
    Language = "en-US",
    EnablePartialResults = true
});

if (started)
{
    // Recognition is now active
    // Stop when needed
    await speechService.StopListeningAsync();
}
```

### Integration with LLM

```csharp
private async Task ProcessSpeechWithLLMAsync(string speechText)
{
    try
    {
        // Send to LLM
        var response = await _llmService.SendMessageAsync(new ChatRequest
        {
            Message = speechText,
            ConversationId = conversationId
        });

        // Convert response to speech
        if (response?.Response != null)
        {
            await _voiceService.GenerateSpeechAsync(response.Response);
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to process speech with LLM");
    }
}
```

## Configuration

### Service Registration

The speech recognition service is automatically registered in `MauiProgram.cs`:

```csharp
#if WINDOWS
services.AddSingleton<ISpeechRecognitionService, WindowsSpeechRecognitionService>();
#elif ANDROID
services.AddSingleton<ISpeechRecognitionService, AndroidSpeechRecognitionService>();
#endif
```

### Permissions Configuration

**Android (AndroidManifest.xml):**
```xml
<uses-permission android:name="android.permission.RECORD_AUDIO" />
<queries>
    <intent>
        <action android:name="android.speech.RecognitionService" />
    </intent>
</queries>
```

**Windows (Package.appxmanifest):**
```xml
<Capabilities>
    <DeviceCapability Name="microphone" />
</Capabilities>
```

## Error Handling

The implementation includes comprehensive error handling:

1. **Permission Errors**: Proper handling of denied/restricted permissions
2. **Platform Availability**: Graceful degradation when speech recognition isn't available
3. **Network Errors**: Timeout and retry logic for network-dependent features
4. **Threading**: All platform-specific calls are properly marshaled to main thread

## Performance Considerations

1. **Resource Management**: Proper disposal of speech recognizers and cleanup
2. **Threading**: Non-blocking async operations
3. **Memory**: Efficient event handler management and cleanup
4. **Battery**: Proper stopping of continuous recognition when not needed

## Testing

The implementation includes:

1. **Availability Testing**: Check if speech recognition is available on the platform
2. **Permission Testing**: Verify microphone permissions
3. **Language Testing**: List available languages
4. **Recognition Testing**: Test single and continuous recognition modes

Use the `TestSpeechRecognitionCommand` in `VoiceViewModel` to run comprehensive tests.

## Future Enhancements

Potential improvements for future versions:

1. **Offline Recognition**: Support for offline speech recognition models
2. **Custom Models**: Integration with custom speech recognition models
3. **Real-time Streaming**: WebSocket-based streaming recognition
4. **Voice Activity Detection**: More sophisticated silence detection
5. **Noise Cancellation**: Audio preprocessing for better recognition accuracy
6. **Multi-language**: Automatic language detection and switching

## Troubleshooting

### Common Issues

1. **Permissions Denied**: Ensure microphone permissions are granted
2. **Service Unavailable**: Check if Google Play Services are available (Android)
3. **No Audio Input**: Verify microphone hardware and system settings
4. **Poor Recognition**: Check language settings and ensure clear speech
5. **Threading Issues**: Ensure UI updates are on main thread

### Debugging

Enable detailed logging by setting log level to Debug in your logging configuration. The implementation provides extensive logging for troubleshooting recognition issues.

## Dependencies

### NuGet Packages
- `CommunityToolkit.Mvvm` - For MVVM support
- `Microsoft.Extensions.Logging.Abstractions` - For logging

### Platform Dependencies
- **Windows**: Windows.Media.SpeechRecognition (built-in)
- **Android**: Android.Speech (built-in), AndroidX libraries for permissions

## Conclusion

This implementation provides a robust, cross-platform speech recognition solution that can be easily integrated into any .NET MAUI application. It follows best practices for error handling, resource management, and platform-specific optimizations while maintaining a clean, consistent API across platforms.
