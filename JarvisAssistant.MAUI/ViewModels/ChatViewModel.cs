using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models;
using JarvisAssistant.MAUI.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text;
using System.Net.Http;
using System.Diagnostics;
using System.Net.Http.Headers;
using MediaManager;
#if WINDOWS
using System.Speech.Synthesis;
#endif

namespace JarvisAssistant.MAUI.ViewModels
{
    public partial class ChatViewModel : ObservableObject
    {
        private readonly ILLMService? _llmService;
        private readonly IVoiceService? _voiceService;
        private readonly IVoiceModeManager? _voiceModeManager;
        private readonly IVoiceCommandProcessor? _voiceCommandProcessor;
        private readonly ILogger<ChatViewModel>? _logger;

        // Use ObservableProperty attributes for proper UI binding
        [ObservableProperty]
        private string currentMessage = string.Empty;

        [ObservableProperty]
        private bool isVoiceModeActive;

        [ObservableProperty]
        private bool isSending;

        [ObservableProperty]
        private bool isListening;

        [ObservableProperty]
        private bool isConnected = true;

        [ObservableProperty]
        private string statusMessage = "Ready";

        [ObservableProperty]
        private double voiceActivityLevel;

        [ObservableProperty]
        private string voiceCommandFeedback = string.Empty;

        [ObservableProperty]
        private bool showVoiceToggle = true;

        public ObservableCollection<ChatMessage> Messages { get; } = new();

        public ChatViewModel(
            ILLMService? llmService = null,
            IVoiceService? voiceService = null,
            IVoiceModeManager? voiceModeManager = null,
            IVoiceCommandProcessor? voiceCommandProcessor = null,
            ILogger<ChatViewModel>? logger = null)
        {
            _llmService = llmService;
            _voiceService = voiceService;
            _voiceModeManager = voiceModeManager;
            _voiceCommandProcessor = voiceCommandProcessor;
            _logger = logger;

            // Platform-specific voice mode behavior
            InitializePlatformBehavior();

            // Check service availability and update status
            CheckServiceAvailability();

            // Add welcome message
            AddWelcomeMessage();
        }

        private void InitializePlatformBehavior()
        {
            // TV platform: Always active voice mode, no toggle
            if (DeviceInfo.Idiom == DeviceIdiom.TV)
            {
                isVoiceModeActive = true;
                showVoiceToggle = false;
            }
            // Desktop/Mobile: Show toggle, default off
            else
            {
                isVoiceModeActive = false;
                showVoiceToggle = true;
            }
        }

        private void CheckServiceAvailability()
        {
            try
            {
                bool llmAvailable = _llmService != null;
                bool voiceAvailable = _voiceService != null && _voiceModeManager != null;

                if (!llmAvailable)
                {
                    isConnected = false;
                    statusMessage = "LLM Service Offline";
                    _logger?.LogWarning("LLM Service is not available");
                }
                else if (!voiceAvailable)
                {
                    statusMessage = "Voice Services Limited";
                    showVoiceToggle = false;
                    _logger?.LogWarning("Voice services are not available");
                }
                else
                {
                    isConnected = true;
                    statusMessage = "All Systems Online";
                }
            }
            catch (Exception ex)
            {
                isConnected = false;
                statusMessage = "Service Check Failed";
                _logger?.LogError(ex, "Error checking service availability");
            }
        }

        private void AddWelcomeMessage()
        {
            string welcomeText;
            MessageType messageType;

            if (_llmService == null)
            {
                welcomeText = "Greetings. I am JARVIS - Just A Rather Very Intelligent System. " +
                             "However, my language processing services are currently offline. " +
                             "You may send messages, but I cannot respond until the LLM service is restored.";
                messageType = MessageType.Error;
            }
            else if (_voiceService == null || _voiceModeManager == null)
            {
                welcomeText = "Greetings. I am JARVIS - Just A Rather Very Intelligent System. " +
                             "Text chat is available, but voice services are currently limited.";
                messageType = MessageType.System;
            }
            else
            {
                welcomeText = "Greetings. I am JARVIS - Just A Rather Very Intelligent System. " +
                             "All systems are online. How may I assist you today?";
                messageType = MessageType.System;
            }

            var welcomeMessage = new ChatMessage(welcomeText, false, messageType);
            Messages.Add(welcomeMessage);
        }

        [RelayCommand]
        private async Task SendMessageAsync()
        {
            if (string.IsNullOrWhiteSpace(currentMessage) || isSending)
                return;

            var userMessage = currentMessage.Trim();
            currentMessage = string.Empty;
            isSending = true;

            try
            {
                // Add user message to chat
                var userChatMessage = new ChatMessage(userMessage, true);
                Messages.Add(userChatMessage);

                // Check if LLM service is available
                if (_llmService == null)
                {
                    var offlineMessage = new ChatMessage(
                        "I apologize, but my language processing services are currently offline. Please check your connection and try again later.",
                        false,
                        MessageType.Error);
                    Messages.Add(offlineMessage);
                    return;
                }

                // Create request
                var request = new ChatRequest
                {
                    Message = userMessage,
                    Type = "user",
                    ConversationId = "main-chat"
                };

                // Add thinking indicator
                var thinkingMessage = new ChatMessage("Analyzing...", false, MessageType.System)
                {
                    IsStreaming = true
                };
                Messages.Add(thinkingMessage);

                // Send to LLM service using correct method name
                var response = await _llmService.SendMessageAsync(request);

                // Remove thinking indicator
                Messages.Remove(thinkingMessage);

                // Add response
                var responseMessage = new ChatMessage(response.Message, false)
                {
                    Type = response.Type == "error" ? MessageType.Error : MessageType.Text
                };
                Messages.Add(responseMessage);

                // Generate speech for the response if voice mode is active and voice service is available
                if (isVoiceModeActive && _voiceService != null && !string.IsNullOrWhiteSpace(response.Message))
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            _logger?.LogInformation("🎯 FORCING ElevenLabs for chat response: '{Message}'", response.Message);
                            
                            // DIRECT ELEVENLABS CALL - BYPASS THE BROKEN SERVICE INJECTION
                            var audioData = await CallElevenLabsDirectly(response.Message);
                            
                            if (audioData != null && audioData.Length > 0)
                            {
                                _logger?.LogInformation("✅ Playing ElevenLabs audio ({Size} bytes)", audioData.Length);
                                await PlayChatResponseAudio(audioData);
                            }
                            else
                            {
                                _logger?.LogWarning("❌ ElevenLabs failed, using direct Windows speech");
                                // Fallback to direct speech (no WAV files, no beeping)
                                await SpeakDirectly(response.Message);
                            }
                        }
                        catch (Exception audioEx)
                        {
                            _logger?.LogError(audioEx, "Failed ElevenLabs, falling back to direct speech");
                            try
                            {
                                await SpeakDirectly(response.Message);
                            }
                            catch (Exception fallbackEx)
                            {
                                _logger?.LogError(fallbackEx, "All TTS methods failed");
                            }
                        }
                    });
                }

                // Auto-scroll to bottom
                WeakReferenceMessenger.Default.Send("ScrollToBottom");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error sending message");
                
                var errorMessage = new ChatMessage(
                    "I apologize, but I encountered an error processing your request. Please try again.",
                    false,
                    MessageType.Error);
                Messages.Add(errorMessage);
            }
            finally
            {
                isSending = false;
            }
        }

        [RelayCommand]
        private async Task ProcessVoiceCommandAsync()
        {
            if (isListening || !isVoiceModeActive)
                return;

            // Check if voice services are available
            if (_voiceModeManager == null || _voiceCommandProcessor == null)
            {
                voiceCommandFeedback = "Voice services unavailable.";
                await Task.Delay(2000);
                voiceCommandFeedback = string.Empty;
                return;
            }

            isListening = true;
            voiceCommandFeedback = "Listening...";

            try
            {
                // Start voice recognition using voice mode manager
                var voiceResult = await _voiceModeManager.ListenForCommandAsync(TimeSpan.FromSeconds(5));
                
                if (!string.IsNullOrEmpty(voiceResult))
                {
                    voiceCommandFeedback = $"Processing: \"{voiceResult}\"";
                    
                    // Process as voice command first
                    var voiceCommand = new VoiceCommand
                    {
                        Text = voiceResult,
                        Timestamp = DateTime.UtcNow,
                        Source = VoiceCommandSource.Manual
                    };

                    var commandResult = await _voiceCommandProcessor.ProcessCommandAsync(voiceCommand);
                    
                    if (commandResult.Success)
                    {
                        // Command was handled by voice processor
                        var commandMessage = new ChatMessage(
                            $"Voice Command: {voiceResult}\nResult: {commandResult.Response}",
                            false,
                            MessageType.Voice);
                        Messages.Add(commandMessage);
                    }
                    else
                    {
                        // Treat as regular chat message
                        currentMessage = voiceResult;
                        await SendMessageAsync();
                    }
                }
                else
                {
                    voiceCommandFeedback = "No speech detected.";
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error processing voice command");
                voiceCommandFeedback = "Voice recognition error.";
            }
            finally
            {
                isListening = false;
                
                // Clear feedback after delay
                await Task.Delay(2000);
                voiceCommandFeedback = string.Empty;
            }
        }

        [RelayCommand]
        private async Task ToggleVoiceModeAsync()
        {
            if (DeviceInfo.Idiom == DeviceIdiom.TV)
                return; // TV always has voice mode active

            // Check if voice services are available
            if (_voiceModeManager == null)
            {
                statusMessage = "Voice services unavailable";
                await Task.Delay(2000);
                statusMessage = isConnected ? "Ready" : "LLM Service Offline";
                return;
            }

            isVoiceModeActive = !isVoiceModeActive;

            try
            {
                if (isVoiceModeActive)
                {
                    await _voiceModeManager.EnableVoiceModeAsync();
                    statusMessage = "Voice mode activated";
                }
                else
                {
                    await _voiceModeManager.DisableVoiceModeAsync();
                    statusMessage = "Voice mode deactivated";
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error toggling voice mode");
                isVoiceModeActive = !isVoiceModeActive; // Revert on error
            }
        }

        /// <summary>
        /// Plays audio data for chat responses when voice mode is active using cross-platform MediaManager.
        /// </summary>
        /// <param name="audioData">The audio data to play.</param>
        private async Task PlayChatResponseAudio(byte[] audioData)
        {
            try
            {
                // ElevenLabs returns MP3 data - save as MP3 and use MediaManager for cross-platform playback
                var tempFile = Path.GetTempFileName();
                var audioFile = Path.ChangeExtension(tempFile, ".mp3");

                _logger?.LogInformation("🎵 Saving ElevenLabs MP3 audio to: {AudioFile}", audioFile);
                await File.WriteAllBytesAsync(audioFile, audioData);

                try
                {
                    _logger?.LogInformation("🎵 Playing MP3 using MediaManager (cross-platform)");
                    
                    // Use MediaManager for cross-platform audio playback
                    await CrossMediaManager.Current.Play(audioFile);
                    
                    // Wait for playback to complete
                    while (CrossMediaManager.Current.IsPlaying())
                    {
                        await Task.Delay(100);
                    }
                    
                    _logger?.LogInformation("✅ MediaManager playback completed");
                }
                catch (Exception mediaEx)
                {
                    _logger?.LogError(mediaEx, "MediaManager failed, trying platform-specific fallback");
                    
                    // Platform-specific fallback
#if WINDOWS
                    try
                    {
                        // Windows COM object fallback
                        await Task.Run(() =>
                        {
                            dynamic wmp = Activator.CreateInstance(Type.GetTypeFromProgID("WMPlayer.OCX"));
                            wmp.URL = audioFile;
                            wmp.controls.play();
                            
                            while (wmp.playState != 1 && wmp.playState != 8)
                            {
                                System.Threading.Thread.Sleep(100);
                            }
                            
                            wmp.close();
                            System.Runtime.InteropServices.Marshal.ReleaseComObject(wmp);
                        });
                        
                        _logger?.LogInformation("✅ Windows COM fallback completed");
                    }
                    catch (Exception comEx)
                    {
                        _logger?.LogError(comEx, "Windows COM fallback failed");
                    }
#else
                    // For other platforms (Android, iOS), MediaManager should handle it
                    _logger?.LogWarning("MediaManager failed on non-Windows platform, no additional fallback available");
#endif
                }
                finally
                {
                    // Clean up temp file after a delay to allow playback completion
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(10000); // Wait 10 seconds
                        try
                        {
                            if (File.Exists(audioFile))
                                File.Delete(audioFile);
                        }
                        catch { }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in PlayChatResponseAudio");
            }
        }

        /// <summary>
        /// Checks if the audio data already contains WAV file headers.
        /// </summary>
        private static bool IsWavFile(byte[] audioData)
        {
            if (audioData.Length < 12) return false;
            
            return audioData[0] == 0x52 && audioData[1] == 0x49 && 
                   audioData[2] == 0x46 && audioData[3] == 0x46 && // "RIFF"
                   audioData[8] == 0x57 && audioData[9] == 0x41 && 
                   audioData[10] == 0x56 && audioData[11] == 0x45; // "WAVE"
        }

        /// <summary>
        /// Creates a WAV file with proper headers from raw PCM audio data.
        /// </summary>
        private static byte[] CreateWavFile(byte[] audioData, int sampleRate, int channels, int bitsPerSample)
        {
            var bytesPerSample = bitsPerSample / 8;
            var blockAlign = channels * bytesPerSample;
            var byteRate = sampleRate * blockAlign;

            using var wav = new MemoryStream();
            using var writer = new BinaryWriter(wav);

            // RIFF header
            writer.Write("RIFF".ToCharArray());
            writer.Write(36 + audioData.Length);
            writer.Write("WAVE".ToCharArray());

            // Format chunk
            writer.Write("fmt ".ToCharArray());
            writer.Write(16); // PCM format chunk size
            writer.Write((short)1); // PCM format
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write((short)blockAlign);
            writer.Write((short)bitsPerSample);

            // Data chunk
            writer.Write("data".ToCharArray());
            writer.Write(audioData.Length);
            writer.Write(audioData);

            return wav.ToArray();
        }

        [RelayCommand]
        private async Task RefreshConversationAsync()
        {
            try
            {
                statusMessage = "Refreshing...";
                
                // Check service availability again
                CheckServiceAvailability();
                
                await Task.Delay(1000);
                
                statusMessage = isConnected ? "Conversation refreshed" : "LLM Service Offline";
                await Task.Delay(2000);
                statusMessage = isConnected ? "Ready" : "LLM Service Offline";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error refreshing conversation");
                statusMessage = "Refresh failed";
            }
        }

        [RelayCommand]
        private void ClearConversation()
        {
            Messages.Clear();
            AddWelcomeMessage();
        }

        // Platform-specific input handling
        public void HandleKeyboardShortcut(string shortcut)
        {
            switch (shortcut.ToLower())
            {
                case "ctrl+enter":
                    if (DeviceInfo.Platform == DevicePlatform.WinUI || DeviceInfo.Platform == DevicePlatform.MacCatalyst)
                    {
                        _ = SendMessageAsync();
                    }
                    break;
                case "ctrl+r":
                    _ = RefreshConversationAsync();
                    break;
                case "ctrl+shift+v":
                    _ = ToggleVoiceModeAsync();
                    break;
            }
        }

        // Update voice activity level for visual indicator
        public void UpdateVoiceActivityLevel(double level)
        {
            VoiceActivityLevel = Math.Max(0, Math.Min(1, level));
        }

        /// <summary>
        /// Direct ElevenLabs API call to bypass broken service injection
        /// </summary>
        private async Task<byte[]> CallElevenLabsDirectly(string text)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("xi-api-key", "sk_572262d27043d888785a02694bc21fbdc70b548cc017b119");
                
                var requestBody = new
                {
                    text = text,
                    voice_settings = new
                    {
                        stability = 0.4f,
                        similarity_boost = 0.9f,
                        style = 0.2f,
                        use_speaker_boost = true
                    }
                };
                
                var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                
                var response = await httpClient.PostAsync("https://api.elevenlabs.io/v1/text-to-speech/91AxxCADnelg9FDuKsIS", content);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger?.LogInformation("🎉 ElevenLabs API call successful!");
                    return await response.Content.ReadAsByteArrayAsync();
                }
                else
                {
                    _logger?.LogWarning("ElevenLabs API failed: {StatusCode}", response.StatusCode);
                    return Array.Empty<byte>();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Direct ElevenLabs call failed");
                return Array.Empty<byte>();
            }
        }

        /// <summary>
        /// Direct Windows speech without WAV files (no beeping)
        /// </summary>
        private async Task SpeakDirectly(string text)
        {
#if WINDOWS
            if (OperatingSystem.IsWindows())
            {
                await Task.Run(() =>
                {
                    try
                    {
                        using var synthesizer = new System.Speech.Synthesis.SpeechSynthesizer();
                        synthesizer.SetOutputToDefaultAudioDevice();
                        synthesizer.Rate = 0;
                        synthesizer.Volume = 80;
                        synthesizer.Speak(text);
                        _logger?.LogInformation("✅ Direct Windows speech completed (no beeping)");
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Direct Windows speech failed");
                    }
                });
            }
#endif
        }
    }
}
