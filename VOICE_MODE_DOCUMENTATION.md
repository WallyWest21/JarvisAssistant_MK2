# Jarvis Voice Mode System

A comprehensive voice interaction system with platform-specific behavior, designed for seamless operation across mobile, desktop, and TV platforms.

## Overview

The voice mode system provides:
- **Platform-adaptive behavior**: Always-on for TV, toggle for mobile/desktop
- **Wake word detection**: "Hey Jarvis" activation
- **Voice command processing**: Natural language command classification and routing
- **Visual feedback**: Animated voice indicator with SkiaSharp
- **Google TV integration**: Remote control voice button handling

## Architecture

### Core Components

#### 1. IVoiceModeManager
- **Location**: `JarvisAssistant.Core/Interfaces/IVoiceModeManager.cs`
- **Implementation**: `JarvisAssistant.Services/VoiceModeManager.cs`
- **Purpose**: Manages voice mode lifecycle and platform-specific behavior

**Key Features:**
- Platform detection and automatic configuration
- Wake word detection ("Hey Jarvis")
- Voice activity monitoring
- State management (Inactive, Listening, Processing, Error)
- Always-on mode for TV platforms

```csharp
// Usage example
var voiceModeManager = serviceProvider.GetService<IVoiceModeManager>();
await voiceModeManager.EnableVoiceModeAsync();
```

#### 2. IVoiceCommandProcessor
- **Location**: `JarvisAssistant.Core/Interfaces/IVoiceCommandProcessor.cs`
- **Implementation**: `JarvisAssistant.Services/VoiceCommandProcessor.cs`
- **Purpose**: Classifies and processes voice commands

**Supported Commands:**
- Status: "what's my status", "show status"
- Code Generation: "generate code", "create function"
- Analysis: "analyze this", "examine code"
- Navigation: "open settings", "go to dashboard"
- Search: "find something", "search for"
- Help: "help", "what can you do"
- Control: "stop", "exit", "repeat"

```csharp
// Usage example
var processor = serviceProvider.GetService<IVoiceCommandProcessor>();
var result = await processor.ProcessTextCommandAsync("what's my status", VoiceCommandSource.WakeWord);
```

#### 3. VoiceIndicator Control
- **Location**: `JarvisAssistant.MAUI/Controls/VoiceIndicator.cs`
- **Purpose**: Animated microphone visualization with SkiaSharp

**Features:**
- Smooth animations using SkiaSharp
- Platform-adaptive sizing (larger for TV)
- Voice activity visualization
- State-based color changes
- Touch/tap interaction support

### Platform-Specific Components

#### Google TV Voice Handler
- **Location**: `JarvisAssistant.MAUI/Platforms/Android/VoiceHandlers/GoogleTVVoiceHandler.cs`
- **Purpose**: Handles Google TV remote control integration

**Key Features:**
- Captures voice button presses (Search, Voice Assist, Microphone)
- Integrates with MainActivity.DispatchKeyEvent
- Continuous listening management
- Remote control event handling

```csharp
// Integration in MainActivity
public override bool DispatchKeyEvent(KeyEvent? keyEvent)
{
    if (keyEvent != null && _voiceHandler != null && _platformService?.IsGoogleTV() == true)
    {
        if (_voiceHandler.HandleKeyEvent(keyEvent.KeyCode, keyEvent))
        {
            return true; // Event handled by voice system
        }
    }
    return base.DispatchKeyEvent(keyEvent);
}
```

## Platform Behavior

### Google TV / Android TV
- **Activation Mode**: Always-on (cannot be disabled)
- **Wake Word**: Always enabled with "Hey Jarvis"
- **Remote Integration**: Voice button capture
- **Visual Feedback**: Large voice indicator (TV-optimized)

### Mobile (Android/iOS)
- **Activation Mode**: Toggle (user can enable/disable)
- **Wake Word**: Optional, configurable
- **Interaction**: Tap voice indicator to toggle
- **Visual Feedback**: Standard-sized indicator

### Desktop (Windows/macOS)
- **Activation Mode**: Toggle or Push-to-talk
- **Wake Word**: Optional, configurable
- **Interaction**: Click voice indicator or keyboard shortcut
- **Visual Feedback**: Standard-sized indicator

## Voice Command Models

### VoiceCommand
```csharp
public class VoiceCommand
{
    public string Text { get; set; }
    public VoiceCommandSource Source { get; set; }
    public VoiceCommandType CommandType { get; set; }
    public float RecognitionConfidence { get; set; }
    public float ClassificationConfidence { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
    // ... additional properties
}
```

### VoiceCommandResult
```csharp
public class VoiceCommandResult
{
    public bool Success { get; set; }
    public string? Response { get; set; }
    public object? Data { get; set; }
    public bool ShouldSpeak { get; set; }
    public List<string> FollowUpActions { get; set; }
    // ... additional properties
}
```

## Event System

### Voice Mode Events
- **StateChanged**: Voice mode state transitions
- **WakeWordDetected**: "Hey Jarvis" detection
- **VoiceActivityDetected**: Audio level monitoring

### Command Processing Events
- **CommandReceived**: New voice command received
- **CommandProcessed**: Command processing completed

## Usage Examples

### Basic Setup
```csharp
// In MauiProgram.cs
services.AddSingleton<IVoiceModeManager, VoiceModeManager>();
services.AddSingleton<IVoiceCommandProcessor, VoiceCommandProcessor>();
services.AddSingleton<IVoiceService, StubVoiceService>();
```

### Voice Indicator in XAML
```xml
<ContentPage xmlns:controls="clr-namespace:JarvisAssistant.MAUI.Controls">
    <controls:VoiceIndicator x:Name="voiceIndicator"
                            WidthRequest="80"
                            HeightRequest="80"
                            PrimaryColor="DodgerBlue"
                            AccentColor="LightBlue"
                            Tapped="OnVoiceIndicatorTapped" />
</ContentPage>
```

### Command Processing
```csharp
// Subscribe to events
_commandProcessor.CommandProcessed += OnCommandProcessed;

// Process a command
var result = await _commandProcessor.ProcessTextCommandAsync(
    "what's my status", 
    VoiceCommandSource.Manual);

if (result.Success && result.ShouldSpeak)
{
    // Generate speech response
    var audioData = await _voiceService.GenerateSpeechAsync(result.Response);
    // Play audio...
}
```

### Custom Command Handler
```csharp
// Register custom handler
_commandProcessor.RegisterCommandHandler(VoiceCommandType.Custom, async (command, ct) =>
{
    // Custom processing logic
    return VoiceCommandResult.CreateSuccess("Custom command executed!");
});
```

## Testing

### Unit Tests
- **VoiceModeManagerTests**: Platform behavior, state management
- **VoiceCommandProcessorTests**: Command classification, processing
- **GoogleTVVoiceHandlerTests**: Remote control integration
- **VoiceCommandModelTests**: Model validation and serialization

### Test Coverage
- Platform-specific activation modes
- Wake word detection simulation
- Command classification accuracy
- Error handling and recovery
- Event system functionality

### Running Tests
```bash
dotnet test JarvisAssistant.UnitTests/Voice/
```

## Configuration

### Wake Word Settings
```csharp
await voiceModeManager.ConfigureWakeWordDetectionAsync(
    enabled: true,
    sensitivity: 0.7f,
    wakeWords: new[] { "hey jarvis", "jarvis" });
```

### Command Pattern Customization
```csharp
// Update patterns for better recognition
processor.UpdateCommandPatterns(VoiceCommandType.Status, new[]
{
    @"(?:what'?s|show|tell me) (?:my|the) status",
    @"system (?:status|health)",
    @"how (?:are things|is everything)"
});
```

## Dependencies

### NuGet Packages
- **SkiaSharp.Views.Maui.Controls**: Voice indicator animations
- **Microsoft.Extensions.Logging**: Comprehensive logging
- **Microsoft.Extensions.DependencyInjection**: Service registration

### Project References
- **JarvisAssistant.Core**: Interfaces and models
- **JarvisAssistant.Services**: Business logic implementations
- **JarvisAssistant.Infrastructure**: Platform services

## Future Enhancements

### Planned Features
1. **Real TTS/STT Integration**: Replace stub implementations
2. **Machine Learning**: Improved command classification
3. **Multi-language Support**: Localized wake words and commands
4. **Voice Biometrics**: Speaker identification and authentication
5. **Contextual Understanding**: Command history and context awareness

### Integration Points
- **Azure Cognitive Services**: Cloud-based STT/TTS
- **OpenAI Whisper**: Local speech recognition
- **Custom Wake Word Models**: Trained "Hey Jarvis" detection
- **Hardware Integration**: Dedicated voice hardware support

## Troubleshooting

### Common Issues

**Voice mode not activating on TV:**
- Verify `IPlatformService.IsGoogleTV()` returns true
- Check MainActivity voice handler initialization
- Ensure proper key event handling

**Wake word not detected:**
- Verify wake word detection is enabled
- Check sensitivity settings (0.0-1.0 range)
- Review audio input permissions

**Commands not classified:**
- Check command patterns in VoiceCommandProcessor
- Verify text preprocessing and normalization
- Review classification confidence thresholds

### Debug Logging
```csharp
// Enable detailed voice logging
services.AddLogging(builder =>
{
    builder.AddDebug();
    builder.SetMinimumLevel(LogLevel.Debug);
});
```

## Contributing

When adding new voice features:
1. Update interfaces in `JarvisAssistant.Core/Interfaces/`
2. Implement in `JarvisAssistant.Services/`
3. Add platform-specific code in appropriate `Platforms/` folders
4. Create comprehensive unit tests
5. Update this documentation

## License

This voice mode system is part of the Jarvis Assistant MK2 project and follows the same licensing terms.
