# Speech Recognition Test Project

This is a dedicated test project for the Jarvis Assistant speech recognition functionality. It provides isolated testing capabilities for both Windows and Android platforms.

## Features

### Platform Support
- **Windows**: Uses `System.Speech` for native Windows speech recognition
- **Android**: Uses `Android.Speech` APIs for native Android speech recognition
- **Cross-platform**: Shared interface and diagnostic capabilities

### Testing Capabilities

#### Recognition Modes
- **Single Recognition**: One-time speech recognition with configurable timeout
- **Continuous Recognition**: Ongoing speech recognition until manually stopped
- **Partial Results**: Real-time display of partial recognition results

#### Diagnostic Tools
- **System Information**: Platform details, OS version, device info
- **Permission Status**: Microphone permission checking and requesting
- **Available Languages**: List of supported speech recognition languages
- **Engine Testing**: Validation of speech recognition engine availability

#### Configuration Options
- **Language Selection**: Choose from available speech recognition languages
- **Timeout Settings**: Configurable silence and maximum listening timeouts
- **Confidence Levels**: Display recognition confidence scores
- **Alternative Results**: Show multiple recognition alternatives

### User Interface

#### Status Section
- Current service status and state
- Availability and permission indicators
- Real-time state updates

#### Controls Section
- Language picker for recognition settings
- Single and continuous recognition buttons
- Stop recognition and permission request buttons
- Diagnostic tools and log management

#### Results Section
- **Partial Text**: Live display of partial recognition results
- **Final Results**: Completed recognition text with confidence scores
- **Alternatives**: Multiple recognition possibilities with confidence

#### Diagnostics Section
- System information display
- Error and warning messages
- Configuration validation results

#### Activity Log
- Real-time logging of all speech recognition activities
- Timestamped messages for debugging
- Automatic log size management (last 100 messages)

## Usage

### Quick Start
1. **Run Diagnostics**: Click "Run Diagnostics" to check system compatibility
2. **Request Permissions**: Click "Request Permissions" if needed (especially on Android)
3. **Test Single Recognition**: Click "Single Recognition" and speak
4. **Test Continuous**: Click "Start Continuous" for ongoing recognition

### Troubleshooting

#### Windows Issues
- Ensure Windows Speech Recognition is installed and configured
- Check microphone permissions in Windows Settings
- Verify System.Speech engine availability
- Run as administrator if needed

#### Android Issues
- Grant microphone permission when prompted
- Ensure device has Google Speech Services or equivalent
- Check for network connectivity (some engines require internet)
- Verify Android API level compatibility (21+)

### Common Problems

#### "Service Not Available"
- **Windows**: System.Speech not installed or configured
- **Android**: Speech recognition service not available on device
- **Solution**: Check diagnostics for specific platform requirements

#### "Permission Denied"
- **Windows**: Microphone access blocked in Windows settings
- **Android**: Microphone permission not granted
- **Solution**: Use "Request Permissions" button or grant manually in system settings

#### "No Speech Detected"
- Check microphone volume and positioning
- Verify background noise levels
- Adjust timeout settings if needed
- Test with different languages

#### Recognition Accuracy Issues
- Speak clearly and at moderate pace
- Reduce background noise
- Use appropriate language setting
- Check confidence scores for recognition quality

### Development Notes

#### Architecture
- **ISpeechRecognitionService**: Core interface for all platforms
- **WindowsSpeechRecognitionService**: Windows-specific implementation
- **AndroidSpeechRecognitionService**: Android-specific implementation
- **SpeechTestViewModel**: UI logic and command handling

#### Key Classes
- **SpeechRecognitionOptions**: Configuration for recognition sessions
- **SpeechRecognitionResult**: Results with text, confidence, and alternatives
- **DiagnosticResult**: System information and validation results

#### Extension Points
- Add new platform implementations by implementing `ISpeechRecognitionService`
- Extend diagnostic capabilities in platform-specific services
- Customize UI elements in MainPage.xaml
- Add new recognition options in SpeechRecognitionOptions

## Building and Running

### Prerequisites
- .NET 8 SDK
- Visual Studio 2022 with MAUI workload
- Windows 10/11 (for Windows testing)
- Android device or emulator (for Android testing)

### Build Commands
```bash
# Restore dependencies
dotnet restore

# Build for Windows
dotnet build -f net8.0-windows10.0.19041.0

# Build for Android
dotnet build -f net8.0-android

# Run on Windows
dotnet run -f net8.0-windows10.0.19041.0

# Deploy to Android device
dotnet build -f net8.0-android -c Release
```

### Platform-Specific Notes

#### Windows
- Requires Windows 10 version 1903 or later
- System.Speech package provides speech recognition
- No additional configuration required for basic functionality

#### Android
- Requires Android API 21 (Android 5.0) or later
- Requires RECORD_AUDIO permission
- Speech recognition quality depends on device and services
- May require Google Speech Services for optimal performance

## Logging and Debugging

The application provides comprehensive logging at multiple levels:

- **Debug**: Detailed internal operations
- **Info**: General status and successful operations  
- **Warning**: Non-critical issues and fallbacks
- **Error**: Critical failures and exceptions

All logs are displayed in the Activity Log section of the UI with timestamps for easy debugging and troubleshooting.

## Future Enhancements

Potential improvements for this test application:

- Voice activity detection (VAD) testing
- Speech recognition performance benchmarking
- Audio level monitoring and visualization
- Custom grammar testing capabilities
- Network-based speech service testing
- Noise reduction and audio preprocessing options
- Multi-language recognition testing
- Speech synthesis (TTS) integration testing
