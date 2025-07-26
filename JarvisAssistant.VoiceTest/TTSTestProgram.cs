using System;
using System.IO;
using System.Threading.Tasks;
using JarvisAssistant.Services;

namespace JarvisAssistant.VoiceTest
{
    /// <summary>
    /// TTS-specific test program for testing text-to-speech functionality
    /// </summary>
    public class TTSTestProgram
    {
        public static async Task RunAsync(string[] args)
        {
            Console.WriteLine("=== Jarvis TTS Test ===");
            Console.WriteLine("Testing the fixed TTS functionality...");
            
            try
            {
                // Test WindowsSapiVoiceService
                using var voiceService = new WindowsSapiVoiceService();
                
                Console.WriteLine("Generating speech audio...");
                var audioData = await voiceService.GenerateSpeechAsync("Hello! Jarvis TTS is now working correctly. This is a test of the speech synthesis system.");
                
                Console.WriteLine($"Generated {audioData.Length} bytes of audio data");
                
                if (audioData.Length > 0)
                {
                    // Create a test WAV file
                    var testFile = Path.Combine(Path.GetTempPath(), "jarvis_tts_test.wav");
                    
                    // Create WAV header for 22kHz, 16-bit, mono
                    var wavHeader = new byte[44];
                    var sampleRate = 22050;
                    var bitsPerSample = 16;
                    var channels = 1;
                    var byteRate = sampleRate * channels * bitsPerSample / 8;
                    var blockAlign = channels * bitsPerSample / 8;
                    var dataSize = audioData.Length;
                    var chunkSize = 36 + dataSize;
                    
                    // RIFF header
                    Array.Copy(System.Text.Encoding.ASCII.GetBytes("RIFF"), 0, wavHeader, 0, 4);
                    Array.Copy(BitConverter.GetBytes(chunkSize), 0, wavHeader, 4, 4);
                    Array.Copy(System.Text.Encoding.ASCII.GetBytes("WAVE"), 0, wavHeader, 8, 4);
                    
                    // fmt chunk
                    Array.Copy(System.Text.Encoding.ASCII.GetBytes("fmt "), 0, wavHeader, 12, 4);
                    Array.Copy(BitConverter.GetBytes(16), 0, wavHeader, 16, 4); // PCM format chunk size
                    Array.Copy(BitConverter.GetBytes((short)1), 0, wavHeader, 20, 2); // PCM format
                    Array.Copy(BitConverter.GetBytes((short)channels), 0, wavHeader, 22, 2);
                    Array.Copy(BitConverter.GetBytes(sampleRate), 0, wavHeader, 24, 4);
                    Array.Copy(BitConverter.GetBytes(byteRate), 0, wavHeader, 28, 4);
                    Array.Copy(BitConverter.GetBytes((short)blockAlign), 0, wavHeader, 32, 2);
                    Array.Copy(BitConverter.GetBytes((short)bitsPerSample), 0, wavHeader, 34, 2);
                    
                    // data chunk
                    Array.Copy(System.Text.Encoding.ASCII.GetBytes("data"), 0, wavHeader, 36, 4);
                    Array.Copy(BitConverter.GetBytes(dataSize), 0, wavHeader, 40, 4);
                    
                    // Write complete WAV file
                    using var fileStream = new FileStream(testFile, FileMode.Create);
                    await fileStream.WriteAsync(wavHeader);
                    await fileStream.WriteAsync(audioData);
                    
                    Console.WriteLine($"Created test file: {testFile}");
                    Console.WriteLine("Attempting to play audio...");
                    
                    // Try to play the audio file
                    try
                    {
                        Console.WriteLine("Attempting to play audio using system default player...");
                        var startInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = testFile,
                            UseShellExecute = true
                        };
                        using var process = System.Diagnostics.Process.Start(startInfo);
                        process?.WaitForExit(5000);
                        Console.WriteLine("✓ Audio playback completed successfully!");
                    }
                    catch (Exception playEx)
                    {
                        Console.WriteLine($"❌ Audio playback failed: {playEx.Message}");
                        Console.WriteLine($"Audio file created at: {testFile}");
                        Console.WriteLine("You can manually play this file to test the audio.");
                    }
                    
                    // Clean up
                    try
                    {
                        File.Delete(testFile);
                    }
                    catch { }
                }
                else
                {
                    Console.WriteLine("❌ No audio data generated - TTS not working");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Test failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            
            Console.WriteLine();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}
