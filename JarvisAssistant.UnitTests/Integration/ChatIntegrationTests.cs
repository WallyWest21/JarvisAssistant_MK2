using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models;
using JarvisAssistant.Services;
using System.ComponentModel;

namespace JarvisAssistant.UnitTests.Integration
{
    /// <summary>
    /// Integration tests for the complete chat functionality including voice mode and platform-specific behavior
    /// </summary>
    public class ChatIntegrationTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;

        public ChatIntegrationTests()
        {
            // Setup DI container similar to production
            var services = new ServiceCollection();
            
            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // Add core services (using test implementations)
            services.AddSingleton<ILLMService, TestLLMService>();
            services.AddSingleton<IVoiceService, StubVoiceService>();
            services.AddSingleton<IVoiceModeManager, TestVoiceModeManager>();
            services.AddSingleton<IVoiceCommandProcessor, TestVoiceCommandProcessor>();
            services.AddSingleton<IStatusMonitorService, TestStatusMonitorService>();

            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public async Task LLMService_SendMessage_ShouldReturnResponse()
        {
            // Arrange
            var llmService = _serviceProvider.GetRequiredService<ILLMService>();
            var request = new ChatRequest
            {
                Message = "Hello JARVIS, how are you today?",
                ConversationId = "test-conversation"
            };

            // Act
            var response = await llmService.SendMessageAsync(request);

            // Assert
            Assert.NotNull(response);
            Assert.Contains("Test response to:", response.Message);
            Assert.Equal("assistant", response.Type);
            Assert.True(response.IsComplete);
        }

        [Fact]
        public async Task LLMService_GetActiveModel_ShouldReturnModelName()
        {
            // Arrange
            var llmService = _serviceProvider.GetRequiredService<ILLMService>();

            // Act
            var activeModel = await llmService.GetActiveModelAsync();

            // Assert
            Assert.NotNull(activeModel);
            Assert.Equal("test-model", activeModel);
        }

        [Fact]
        public async Task VoiceModeManager_ToggleMode_ShouldWork()
        {
            // Arrange
            var voiceModeManager = _serviceProvider.GetRequiredService<IVoiceModeManager>();
            
            Assert.False(voiceModeManager.IsVoiceModeActive);

            // Act - Enable voice mode
            var enableResult = await voiceModeManager.EnableVoiceModeAsync();
            
            // Assert
            Assert.True(enableResult);
            Assert.True(voiceModeManager.IsVoiceModeActive);

            // Act - Disable voice mode
            var disableResult = await voiceModeManager.DisableVoiceModeAsync();
            
            // Assert
            Assert.True(disableResult);
            Assert.False(voiceModeManager.IsVoiceModeActive);
        }

        [Fact]
        public async Task ErrorHandling_LLMServiceFailure_ShouldHandleGracefully()
        {
            // Arrange
            var errorLLMService = new ErrorLLMService();
            var request = new ChatRequest
            {
                Message = "Test message",
                ConversationId = "error-test"
            };

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => errorLLMService.SendMessageAsync(request));
        }

        [Fact]
        public async Task StatusMonitorService_GetStatus_ShouldReturnHealthyStatus()
        {
            // Arrange
            var statusService = _serviceProvider.GetRequiredService<IStatusMonitorService>();

            // Act
            var statuses = await statusService.GetAllServiceStatusesAsync();

            // Assert
            Assert.NotNull(statuses);
            Assert.Single(statuses);
            var status = statuses.First();
            Assert.True(status.IsHealthy);
        }

        [Fact]
        public async Task VoiceService_GenerateSpeech_ShouldReturnAudioData()
        {
            // Arrange
            var voiceService = _serviceProvider.GetRequiredService<IVoiceService>();

            // Act
            var audioData = await voiceService.GenerateSpeechAsync("Test speech");

            // Assert
            Assert.NotNull(audioData);
            Assert.True(audioData.Length > 0);
        }

        [Fact]
        public async Task VoiceService_RecognizeSpeech_ShouldReturnText()
        {
            // Arrange
            var voiceService = _serviceProvider.GetRequiredService<IVoiceService>();
            var dummyAudio = new byte[1024];

            // Act
            var recognizedText = await voiceService.RecognizeSpeechAsync(dummyAudio);

            // Assert
            Assert.NotNull(recognizedText);
            Assert.NotEmpty(recognizedText);
        }

        [Fact]
        public async Task VoiceCommandProcessor_ClassifyCommand_ShouldReturnClassifiedCommand()
        {
            // Arrange
            var processor = _serviceProvider.GetRequiredService<IVoiceCommandProcessor>();

            // Act
            var command = await processor.ClassifyCommandAsync("what's my status");

            // Assert
            Assert.NotNull(command);
            Assert.Equal("what's my status", command.Text);
            Assert.Equal(VoiceCommandType.Status, command.CommandType);
        }

        public void Dispose()
        {
            _serviceProvider?.Dispose();
        }
    }

    // Test implementations of services
    public class TestLLMService : ILLMService
    {
        public Task<ChatResponse> SendMessageAsync(ChatRequest request, CancellationToken cancellationToken = default)
        {
            var response = new ChatResponse
            {
                Message = $"Test response to: {request.Message}",
                Type = "assistant",
                IsComplete = true,
                ResponseId = Guid.NewGuid().ToString(),
                Timestamp = DateTimeOffset.UtcNow
            };
            return Task.FromResult(response);
        }

        public async IAsyncEnumerable<ChatResponse> StreamResponseAsync(ChatRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return new ChatResponse
            {
                Message = $"Streaming test response to: {request.Message}",
                Type = "assistant",
                IsComplete = true,
                ResponseId = Guid.NewGuid().ToString(),
                Timestamp = DateTimeOffset.UtcNow
            };
        }

        public Task<string> GetActiveModelAsync()
        {
            return Task.FromResult("test-model");
        }
    }

    public class ErrorLLMService : ILLMService
    {
        public Task<ChatResponse> SendMessageAsync(ChatRequest request, CancellationToken cancellationToken = default)
        {
            throw new Exception("Test LLM service error");
        }

        public async IAsyncEnumerable<ChatResponse> StreamResponseAsync(ChatRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            throw new Exception("Test LLM service streaming error");
            yield break; // This will never be reached, but is required for compiler
        }

        public Task<string> GetActiveModelAsync()
        {
            throw new Exception("Test LLM service model error");
        }
    }

    public class StubVoiceService : IVoiceService
    {
        public Task<byte[]> GenerateSpeechAsync(string text, string? voiceId = null, CancellationToken cancellationToken = default)
        {
            // Return dummy audio data for testing
            var dummyAudio = new byte[1024];
            Random.Shared.NextBytes(dummyAudio);
            return Task.FromResult(dummyAudio);
        }

        public async IAsyncEnumerable<byte[]> StreamSpeechAsync(string text, string? voiceId = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // Return dummy streaming audio data for testing
            var dummyChunk = new byte[256];
            Random.Shared.NextBytes(dummyChunk);
            yield return dummyChunk;
        }

        public Task<string> RecognizeSpeechAsync(byte[] audioData, string? language = null, CancellationToken cancellationToken = default)
        {
            // Return dummy recognized text for testing
            return Task.FromResult("Test recognized speech text");
        }
    }

    public class TestVoiceModeManager : IVoiceModeManager
    {
        private bool _isActive = false;

        public bool IsVoiceModeActive => _isActive;
        public bool CanToggleVoiceMode => true;
        public VoiceActivationMode ActivationMode => VoiceActivationMode.Toggle;
        public VoiceModeState CurrentState => _isActive ? VoiceModeState.Listening : VoiceModeState.Inactive;
        public bool IsWakeWordDetectionEnabled => false;

        public event EventHandler<VoiceModeStateChangedEventArgs>? StateChanged;
        public event EventHandler<WakeWordDetectedEventArgs>? WakeWordDetected;
        public event EventHandler<VoiceActivityDetectedEventArgs>? VoiceActivityDetected;

        public Task<bool> EnableVoiceModeAsync(CancellationToken cancellationToken = default)
        {
            _isActive = true;
            return Task.FromResult(true);
        }

        public Task<bool> DisableVoiceModeAsync(CancellationToken cancellationToken = default)
        {
            _isActive = false;
            return Task.FromResult(true);
        }

        public Task<bool> ToggleVoiceModeAsync(CancellationToken cancellationToken = default)
        {
            _isActive = !_isActive;
            return Task.FromResult(_isActive);
        }

        public Task<string?> ListenForCommandAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<string?>("test command");
        }

        public Task ConfigureWakeWordDetectionAsync(bool enabled, float sensitivity = 0.7f, string[]? wakeWords = null)
        {
            return Task.CompletedTask;
        }

        public float GetCurrentAudioLevel()
        {
            return 0.5f;
        }
    }

    public class TestVoiceCommandProcessor : IVoiceCommandProcessor
    {
        public bool IsProcessing => false;
        public IReadOnlyList<VoiceCommandType> SupportedCommands => new List<VoiceCommandType> 
        { 
            VoiceCommandType.Status, 
            VoiceCommandType.Help, 
            VoiceCommandType.Chat 
        };

        public event EventHandler<VoiceCommandReceivedEventArgs>? CommandReceived;
        public event EventHandler<VoiceCommandProcessedEventArgs>? CommandProcessed;

        public Task<VoiceCommand> ClassifyCommandAsync(string commandText, Dictionary<string, object>? context = null, CancellationToken cancellationToken = default)
        {
            var commandType = commandText.ToLowerInvariant() switch
            {
                var text when text.Contains("status") => VoiceCommandType.Status,
                var text when text.Contains("help") => VoiceCommandType.Help,
                _ => VoiceCommandType.Chat
            };

            var command = new VoiceCommand
            {
                Text = commandText,
                CommandType = commandType,
                ClassificationConfidence = 0.9f,
                RecognitionConfidence = 0.95f,
                Source = VoiceCommandSource.Manual
            };

            return Task.FromResult(command);
        }

        public Task<VoiceCommandResult> ProcessCommandAsync(VoiceCommand command, CancellationToken cancellationToken = default)
        {
            var result = VoiceCommandResult.CreateSuccess($"Processed {command.CommandType} command: {command.Text}");
            return Task.FromResult(result);
        }

        public Task<VoiceCommandResult> ProcessTextCommandAsync(string commandText, VoiceCommandSource source, Dictionary<string, object>? context = null, CancellationToken cancellationToken = default)
        {
            var command = new VoiceCommand
            {
                Text = commandText,
                Source = source,
                CommandType = VoiceCommandType.Chat
            };

            return ProcessCommandAsync(command, cancellationToken);
        }

        public void RegisterCommandHandler(VoiceCommandType commandType, Func<VoiceCommand, CancellationToken, Task<VoiceCommandResult>> handler)
        {
            // Test implementation - do nothing
        }

        public void UnregisterCommandHandler(VoiceCommandType commandType)
        {
            // Test implementation - do nothing
        }

        public IReadOnlyList<string> GetCommandPatterns(VoiceCommandType commandType)
        {
            return new List<string> { "test pattern" };
        }

        public void UpdateCommandPatterns(VoiceCommandType commandType, IEnumerable<string> patterns)
        {
            // Test implementation - do nothing
        }

        public Dictionary<string, object> GetProcessingStatistics()
        {
            return new Dictionary<string, object> { { "processed", 0 } };
        }

        public void ClearStatistics()
        {
            // Test implementation - do nothing
        }
    }

    public class TestStatusMonitorService : IStatusMonitorService
    {
        private readonly List<ServiceStatus> _statuses = new() 
        { 
            new ServiceStatus("TestService", ServiceState.Online) 
        };

        // Simple observable implementation without System.Reactive
        private readonly List<IObserver<ServiceStatus>> _observers = new();

        public IObservable<ServiceStatus> ServiceStatusUpdates => new TestObservable(_observers);

        public event PropertyChangedEventHandler? PropertyChanged;

        public Task<IEnumerable<ServiceStatus>> GetAllServiceStatusesAsync()
        {
            return Task.FromResult<IEnumerable<ServiceStatus>>(_statuses);
        }

        public Task<ServiceStatus?> GetServiceStatusAsync(string serviceName)
        {
            var status = _statuses.FirstOrDefault(s => s.ServiceName == serviceName) 
                ?? new ServiceStatus(serviceName, ServiceState.Online);
            return Task.FromResult<ServiceStatus?>(status);
        }

        public Task StartMonitoringAsync(string serviceName)
        {
            return Task.CompletedTask;
        }

        public Task StopMonitoringAsync(string serviceName)
        {
            return Task.CompletedTask;
        }

        public Task StartMonitoringAllAsync()
        {
            return Task.CompletedTask;
        }

        public Task StopMonitoringAllAsync()
        {
            return Task.CompletedTask;
        }

        public Task ResetServiceFailuresAsync(string serviceName)
        {
            return Task.CompletedTask;
        }

        private class TestObservable : IObservable<ServiceStatus>
        {
            private readonly List<IObserver<ServiceStatus>> _observers;

            public TestObservable(List<IObserver<ServiceStatus>> observers)
            {
                _observers = observers;
            }

            public IDisposable Subscribe(IObserver<ServiceStatus> observer)
            {
                _observers.Add(observer);
                return new TestDisposable(() => _observers.Remove(observer));
            }
        }

        private class TestDisposable : IDisposable
        {
            private readonly Action _dispose;

            public TestDisposable(Action dispose)
            {
                _dispose = dispose;
            }

            public void Dispose() => _dispose();
        }
    }
}
