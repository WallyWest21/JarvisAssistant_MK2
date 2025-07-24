using System.Text.Json;

namespace JarvisAssistant.UnitTests.Integration
{
    /// <summary>
    /// Configuration settings for integration tests.
    /// </summary>
    public class IntegrationTestSettings
    {
        /// <summary>
        /// Gets or sets the Ollama server URL.
        /// </summary>
        public string OllamaUrl { get; set; } = "http://100.108.155.28:11434";

        /// <summary>
        /// Gets or sets the test timeout duration.
        /// </summary>
        public TimeSpan TestTimeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets whether integration tests should be enabled.
        /// </summary>
        public bool EnableIntegrationTests { get; set; } = true;

        /// <summary>
        /// Gets or sets the model names to use for testing.
        /// </summary>
        public Dictionary<string, string> TestModels { get; set; } = new()
        {
            ["General"] = "llama3.2",
            ["Code"] = "deepseek-coder"
        };

        /// <summary>
        /// Gets or sets the maximum number of streaming chunks to collect during tests.
        /// </summary>
        public int MaxStreamingChunks { get; set; } = 10;

        /// <summary>
        /// Gets or sets the delay before cancelling streaming tests.
        /// </summary>
        public TimeSpan CancellationTestDelay { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Loads integration test settings from a JSON file or returns default settings.
        /// </summary>
        /// <param name="filePath">Path to the settings file. If null, uses default path.</param>
        /// <returns>The loaded or default settings.</returns>
        public static IntegrationTestSettings Load(string? filePath = null)
        {
            try
            {
                var settingsPath = filePath ?? Path.Combine(
                    Path.GetDirectoryName(typeof(IntegrationTestSettings).Assembly.Location)!,
                    "Integration",
                    "testsettings.json"
                );

                if (!File.Exists(settingsPath))
                {
                    return new IntegrationTestSettings();
                }

                var json = File.ReadAllText(settingsPath);
                var settings = JsonSerializer.Deserialize<IntegrationTestSettings>(json, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    AllowTrailingCommas = true
                });

                return settings ?? new IntegrationTestSettings();
            }
            catch
            {
                // Return default settings if loading fails
                return new IntegrationTestSettings();
            }
        }

        /// <summary>
        /// Checks if integration tests are enabled in the current environment.
        /// </summary>
        /// <returns>True if tests should run, false otherwise.</returns>
        public bool ShouldRunIntegrationTests()
        {
            // Check environment variable override
            var envOverride = Environment.GetEnvironmentVariable("JARVIS_RUN_INTEGRATION_TESTS");
            if (bool.TryParse(envOverride, out var envValue))
            {
                return envValue;
            }

            return EnableIntegrationTests;
        }
    }
}