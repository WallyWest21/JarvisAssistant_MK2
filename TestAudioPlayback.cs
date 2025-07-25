using JarvisAssistant.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

/// <summary>
/// Quick test to verify that voice responses are now being played properly.
/// </summary>
class TestAudioPlayback
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Testing Jarvis Audio Playback Fix ===");
        Console.WriteLine();

        // Setup basic services
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Add voice services
        services.AddSingleton<JarvisAssistant.Core.Interfaces.IVoiceService, WindowsSapiVoiceService>();
        services.AddSingleton<JarvisAssistant.Core.Interfaces.IPlatformService, TestPlatformService>();
        services.AddSingleton<JarvisAssistant.Core.Interfaces.IVoiceCommandProcessor, VoiceCommandProcessor>();
        services.AddSingleton<JarvisAssistant.Core.Interfaces.IVoiceModeManager, VoiceModeManager>();

        using var serviceProvider = services.BuildServiceProvider();
        var voiceModeManager = serviceProvider.GetRequiredService<JarvisAssistant.Core.Interfaces.IVoiceModeManager>();
        var commandProcessor = serviceProvider.GetRequiredService<JarvisAssistant.Core.Interfaces.IVoiceCommandProcessor>();

        Console.WriteLine("✅ Services configured successfully");
        Console.WriteLine();

        // Enable voice mode
        Console.WriteLine("🎤 Enabling voice mode...");
        await voiceModeManager.EnableVoiceModeAsync();
        Console.WriteLine("✅ Voice mode enabled");
        Console.WriteLine();

        // Test a voice command that should generate audio response
        Console.WriteLine("🗣️ Processing test voice command...");
        var testCommand = "what's my status";
        
        try
        {
            var result = await commandProcessor.ProcessTextCommandAsync(
                testCommand, 
                JarvisAssistant.Core.Models.VoiceCommandSource.Manual);

            Console.WriteLine($"Command: \"{testCommand}\"");
            Console.WriteLine($"Response: \"{result.Response}\"");
            Console.WriteLine($"Success: {result.Success}");
            Console.WriteLine($"Should Speak: {result.ShouldSpeak}");
            
            if (result.Success && result.ShouldSpeak)
            {
                Console.WriteLine();
                Console.WriteLine("🔊 Audio response should be playing now!");
                Console.WriteLine("(If you don't hear anything, there may be an audio system issue)");
                
                // Wait a bit for audio to complete
                await Task.Delay(5000);
            }
            else
            {
                Console.WriteLine("❌ Command did not generate a speakable response");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error processing command: {ex.Message}");
        }

        Console.WriteLine();
        Console.WriteLine("Test completed. Press any key to exit...");
        Console.ReadKey();
    }
}

/// <summary>
/// Simple test implementation of IPlatformService for testing.
/// </summary>
public class TestPlatformService : JarvisAssistant.Core.Interfaces.IPlatformService
{
    public bool IsMobile => false;
    public bool IsDesktop => true;
    public bool IsAndroid => false;
    public bool IsIOS => false;
    public bool IsWindows => OperatingSystem.IsWindows();
    public bool IsMacOS => false;
    public bool IsGoogleTV => false;
    public string PlatformName => "Windows Test";
    public string DeviceModel => "Test Device";
}
