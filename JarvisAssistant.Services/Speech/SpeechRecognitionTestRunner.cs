using JarvisAssistant.Core.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace JarvisAssistant.Services.Speech.Tests
{
    /// <summary>
    /// Test harness for speech recognition functionality
    /// </summary>
    public class SpeechRecognitionTestRunner
    {
        private readonly ISpeechRecognitionService _speechService;
        private readonly ILogger<SpeechRecognitionTestRunner> _logger;

        public SpeechRecognitionTestRunner(
            ISpeechRecognitionService speechService,
            ILogger<SpeechRecognitionTestRunner> logger)
        {
            _speechService = speechService;
            _logger = logger;
        }

        /// <summary>
        /// Runs a comprehensive test of the speech recognition service
        /// </summary>
        public async Task RunAllTestsAsync()
        {
            _logger.LogInformation("Starting speech recognition tests...");

            try
            {
                await TestAvailabilityAsync();
                await TestPermissionsAsync();
                await TestLanguagesAsync();
                await TestSingleRecognitionAsync();
                await TestContinuousRecognitionAsync();

                _logger.LogInformation("All speech recognition tests completed successfully!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Speech recognition tests failed");
                throw;
            }
        }

        private async Task TestAvailabilityAsync()
        {
            _logger.LogInformation("Testing speech recognition availability...");

            var isAvailable = _speechService.IsAvailable;
            _logger.LogInformation("Speech recognition available: {Available}", isAvailable);

            if (!isAvailable)
            {
                throw new InvalidOperationException("Speech recognition is not available on this platform");
            }
        }

        private async Task TestPermissionsAsync()
        {
            _logger.LogInformation("Testing microphone permissions...");

            var permissionStatus = await _speechService.RequestPermissionsAsync();
            _logger.LogInformation("Permission status: {Status}", permissionStatus);

            if (permissionStatus != PermissionStatus.Granted)
            {
                throw new InvalidOperationException($"Microphone permission not granted: {permissionStatus}");
            }
        }

        private async Task TestLanguagesAsync()
        {
            _logger.LogInformation("Testing available languages...");

            var languages = await _speechService.GetAvailableLanguagesAsync();
            _logger.LogInformation("Available languages: {Languages}", string.Join(", ", languages));

            if (!languages.Any())
            {
                _logger.LogWarning("No languages available for speech recognition");
            }
        }

        private async Task TestSingleRecognitionAsync()
        {
            _logger.LogInformation("Testing single recognition...");
            _logger.LogInformation("Please speak a test phrase when prompted...");

            var options = new SpeechRecognitionOptions
            {
                Language = "en-US",
                EnablePartialResults = true,
                MaxAlternatives = 3,
                SilenceTimeout = TimeSpan.FromSeconds(3)
            };

            try
            {
                var result = await _speechService.RecognizeSpeechAsync(options);
                
                _logger.LogInformation("Single recognition result:");
                _logger.LogInformation("  Text: {Text}", result.Text);
                _logger.LogInformation("  Confidence: {Confidence:P}", result.Confidence);
                _logger.LogInformation("  Duration: {Duration}", result.Duration);
                
                if (result.Alternatives.Any())
                {
                    _logger.LogInformation("  Alternatives:");
                    foreach (var alt in result.Alternatives)
                    {
                        _logger.LogInformation("    - {Text} ({Confidence:P})", alt.Text, alt.Confidence);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Single recognition test failed");
                throw;
            }
        }

        private async Task TestContinuousRecognitionAsync()
        {
            _logger.LogInformation("Testing continuous recognition...");
            _logger.LogInformation("Starting continuous recognition for 10 seconds...");

            var recognitionCount = 0;
            var partialCount = 0;

            // Subscribe to events
            _speechService.SpeechRecognized += (sender, result) =>
            {
                recognitionCount++;
                _logger.LogInformation("Continuous recognition #{Count}: {Text} ({Confidence:P})", 
                    recognitionCount, result.Text, result.Confidence);
            };

            _speechService.PartialResultsReceived += (sender, partial) =>
            {
                partialCount++;
                _logger.LogDebug("Partial result #{Count}: {Text}", partialCount, partial);
            };

            _speechService.StateChanged += (sender, state) =>
            {
                _logger.LogDebug("Speech recognition state changed: {State}", state);
            };

            var options = new SpeechRecognitionOptions
            {
                Language = "en-US",
                ContinuousRecognition = true,
                EnablePartialResults = true,
                MaxAlternatives = 1,
                MaxListeningTime = TimeSpan.FromSeconds(10)
            };

            try
            {
                var started = await _speechService.StartListeningAsync(options);
                
                if (!started)
                {
                    throw new InvalidOperationException("Failed to start continuous recognition");
                }

                _logger.LogInformation("Continuous recognition started. Please speak...");
                
                // Wait for the max listening time
                await Task.Delay(TimeSpan.FromSeconds(10));
                
                await _speechService.StopListeningAsync();
                
                _logger.LogInformation("Continuous recognition test completed:");
                _logger.LogInformation("  Total recognitions: {Count}", recognitionCount);
                _logger.LogInformation("  Total partial results: {Count}", partialCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Continuous recognition test failed");
                await _speechService.StopListeningAsync(); // Ensure cleanup
                throw;
            }
        }

        /// <summary>
        /// Runs a quick diagnostic test
        /// </summary>
        public async Task RunDiagnosticAsync()
        {
            _logger.LogInformation("Running speech recognition diagnostic...");

            try
            {
                // Test basic functionality
                _logger.LogInformation("Platform: {Platform}", GetPlatformName());
                _logger.LogInformation("Available: {Available}", _speechService.IsAvailable);
                _logger.LogInformation("Currently listening: {Listening}", _speechService.IsListening);
                
                var permissionStatus = await _speechService.RequestPermissionsAsync();
                _logger.LogInformation("Permission status: {Status}", permissionStatus);
                
                var languages = await _speechService.GetAvailableLanguagesAsync();
                _logger.LogInformation("Available languages count: {Count}", languages.Count());
                
                _logger.LogInformation("Speech recognition diagnostic completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Speech recognition diagnostic failed");
                throw;
            }
        }

        private static string GetPlatformName()
        {
            // Since DeviceInfo is not available in the Services project, 
            // we'll determine the platform using runtime information
#if WINDOWS
            return "Windows";
#elif ANDROID
            return "Android";
#elif IOS
            return "iOS";
#elif MACCATALYST
            return "MacCatalyst";
#else
            return System.Runtime.InteropServices.RuntimeInformation.OSDescription;
#endif
        }
    }
}
