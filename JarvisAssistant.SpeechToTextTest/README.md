# Speech-to-Text Troubleshooter

A .NET MAUI application designed to help troubleshoot speech-to-text functionality across different platforms.

## Features

- **Volume Level Monitoring**: Real-time display of input volume with visual indicators
- **Buffer Size Control**: Adjustable buffer size from 50ms to 500ms
- **Sensitivity Settings**: Voice detection sensitivity control (1-10 scale)
- **Recording Simulation**: Simulated speech recognition with sample results
- **Advanced Settings**: Toggle panel with additional options including:
  - Noise suppression
  - Continuous recognition
  - Partial results display
  - Language selection

## User Interface

The application features a dark theme with the following color scheme:
- Background: `#1a1a2e`
- Frame backgrounds: `#16213e`
- Control backgrounds: `#0f3460`
- Accent color: `#00adb5`
- Status colors: Green (`#00cf9f`), Yellow (`#ffd93d`), Red (`#ff6b6b`)

## Getting Started

### Prerequisites

- .NET 8.0 or later
- Visual Studio 2022 or VS Code with C# extension
- .NET MAUI workload installed

### Building and Running

1. Clone the repository
2. Open the project in Visual Studio or VS Code
3. Build the solution
4. Run on your target platform (Windows, iOS, Android, macOS)

## Usage

1. **Volume Monitoring**: The app displays real-time volume levels with threshold indicators
2. **Adjust Settings**: Use the sliders to modify buffer size and sensitivity
3. **Start Recording**: Click "Start Recording" to begin speech recognition simulation
4. **View Results**: Recognition results appear in the results section with confidence scores
5. **Advanced Options**: Toggle advanced settings for additional configuration

## Platform Support

- Windows (WinUI 3)
- iOS
- Android
- macOS

## Technology Stack

- .NET MAUI
- C# 12
- XAML for UI definition

## License

This project is for demonstration and troubleshooting purposes.
