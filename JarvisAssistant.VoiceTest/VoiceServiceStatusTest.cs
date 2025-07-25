using System;

namespace JarvisAssistant.VoiceTest
{
    /// <summary>
    /// Simple test to verify environment variable reading for voice service status.
    /// </summary>
    public class VoiceServiceStatusTest
    {
        public static void TestEnvironmentVariableReading()
        {
            Console.WriteLine("🔍 Testing Voice Service Status Logic...");
            Console.WriteLine("========================================");
            
            // Test environment variable reading
            var elevenLabsApiKey = Environment.GetEnvironmentVariable("ELEVENLABS_API_KEY");
            bool hasElevenLabs = !string.IsNullOrWhiteSpace(elevenLabsApiKey);
            
            Console.WriteLine($"Environment Variable: ELEVENLABS_API_KEY");
            Console.WriteLine($"Value: {(string.IsNullOrWhiteSpace(elevenLabsApiKey) ? "(not set)" : $"{elevenLabsApiKey.Substring(0, Math.Min(15, elevenLabsApiKey.Length))}...")}");
            Console.WriteLine($"Has ElevenLabs: {hasElevenLabs}");
            Console.WriteLine($"Expected Voice Service Status: {(hasElevenLabs ? "Online" : "Offline")}");
            Console.WriteLine();
            
            // Test the same logic that StatusPanelViewModel uses
            var expectedServiceType = hasElevenLabs ? "ElevenLabs" : "Fallback";
            var expectedResponseTime = hasElevenLabs ? 120 : 0;
            var expectedFailures = hasElevenLabs ? 0 : 1;
            var expectedErrorMessage = hasElevenLabs ? null : "No API key configured";
            
            Console.WriteLine("📊 Expected Service Details:");
            Console.WriteLine($"  Service Type: {expectedServiceType}");
            Console.WriteLine($"  Response Time: {expectedResponseTime}ms");
            Console.WriteLine($"  Consecutive Failures: {expectedFailures}");
            Console.WriteLine($"  Error Message: {expectedErrorMessage ?? "(none)"}");
            Console.WriteLine($"  API Configured: {hasElevenLabs}");
            
            if (hasElevenLabs)
            {
                Console.WriteLine();
                Console.WriteLine("✅ RESULT: Voice service should show as ONLINE");
                Console.WriteLine("   - ElevenLabs API key is configured");
                Console.WriteLine("   - Service type: ElevenLabs");
                Console.WriteLine("   - No error message expected");
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("❌ RESULT: Voice service should show as OFFLINE");
                Console.WriteLine("   - No ElevenLabs API key configured");
                Console.WriteLine("   - Service type: Fallback");
                Console.WriteLine("   - Error: No API key configured");
            }
        }
    }
}
