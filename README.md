# Jarvis Assistant .NET MAUI Solution

This solution contains a comprehensive .NET MAUI application for a voice-enabled AI assistant with the following architecture:

## Project Structure

### 1. JarvisAssistant.Core
**Type**: Class Library (.NET 8.0)  
**Purpose**: Contains shared interfaces, models, and core business logic

#### Interfaces
- `ILLMService` - Large Language Model operations (SendMessageAsync, StreamResponseAsync, GetActiveModelAsync)
- `IVoiceService` - Voice operations (GenerateSpeechAsync, StreamSpeechAsync, RecognizeSpeechAsync)
- `IStatusMonitorService` - Service monitoring with observable status updates
- `IThemeManager` - Theme management (Light/Dark/System themes)
- `IErrorHandlingService` - Centralized error handling and logging

#### Models
- `ChatRequest` - Request model for LLM interactions
- `ChatResponse` - Response model from LLM services
- `ServiceStatus` - Service health and status information
- `ErrorInfo` - Comprehensive error information with severity levels

### 2. JarvisAssistant.MAUI
**Type**: .NET MAUI App (net8.0-windows10.0.19041.0, net8.0-android)  
**Purpose**: Main application with UI and platform-specific implementations

#### Structure
- **Views/** - XAML pages and user interface components
- **ViewModels/** - MVVM view models with CommunityToolkit.Mvvm
- **Models/** - UI-specific models and DTOs
- **Services/** - Platform-specific service implementations
- **Behaviors/** - XAML behaviors for enhanced UI interactions

#### Features
- MVVM pattern with CommunityToolkit.Mvvm
- Dependency injection configured in `MauiProgram.cs`
- Base view model with common functionality (error handling, busy states, etc.)
- Target platforms: Windows 10+ and Android

### 3. JarvisAssistant.Services
**Type**: Class Library (.NET 8.0)  
**Purpose**: Service implementations for the core interfaces

*Service implementations will be added here*

### 4. JarvisAssistant.Infrastructure
**Type**: Class Library (.NET 8.0)  
**Purpose**: Data access, external API integrations, and infrastructure concerns

*Infrastructure implementations will be added here*

### 5. JarvisAssistant.UnitTests
**Type**: XUnit Test Project (.NET 8.0)  
**Purpose**: Unit tests for all components

*Unit tests will be added here*

## Getting Started

### Prerequisites
- .NET 8.0 SDK
- Visual Studio 2022 17.8+ with MAUI workload
- For Android development: Android SDK
- For Windows development: Windows 10 SDK (19041 or later)

### Building the Solution
```bash
dotnet restore
dotnet build
```

### Running the Application
```bash
# For Windows
dotnet run --project JarvisAssistant.MAUI --framework net8.0-windows10.0.19041.0

# For Android (requires emulator or connected device)
dotnet run --project JarvisAssistant.MAUI --framework net8.0-android
```

## Architecture Highlights

### Dependency Injection
The solution uses Microsoft.Extensions.DependencyInjection throughout:
- Services are registered in `MauiProgram.cs`
- ViewModels receive dependencies through constructor injection
- Platform-specific services can be registered conditionally

### Error Handling
Centralized error handling through `IErrorHandlingService`:
- Automatic error logging and user notification
- Configurable error severity levels
- Support for technical details and user-friendly messages

### MVVM with CommunityToolkit
- `BaseViewModel` provides common functionality
- Automatic property change notification with `[ObservableProperty]`
- Command handling with `[RelayCommand]`
- Safe async operation execution with error handling

### Observable Services
- `IStatusMonitorService` provides real-time service status updates
- Reactive programming patterns for UI updates
- Service health monitoring and metrics collection

## Next Steps

1. **Implement Service Classes**: Create concrete implementations of the core interfaces
2. **Add UI Pages**: Create XAML pages for chat, settings, and status monitoring
3. **Integrate LLM Services**: Implement connections to OpenAI, Azure OpenAI, or other LLM providers
4. **Add Voice Capabilities**: Implement speech-to-text and text-to-speech functionality
5. **Theme System**: Implement dynamic theming with system theme support
6. **Testing**: Add comprehensive unit and integration tests

## Dependencies

### MAUI Project
- `CommunityToolkit.Mvvm` - MVVM toolkit for .NET
- Project references to Core, Services, and Infrastructure

### Core Project
- No external dependencies (pure .NET 8.0)

### Other Projects
- All projects reference the Core project for shared interfaces and models
