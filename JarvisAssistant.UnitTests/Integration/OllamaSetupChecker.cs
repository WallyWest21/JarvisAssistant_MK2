using System.Net.Http;
using System.Text.Json;

namespace JarvisAssistant.UnitTests.Integration
{
    /// <summary>
    /// Simple utility to check Ollama setup for integration tests.
    /// </summary>
    public class OllamaSetupChecker
    {
        private readonly string _ollamaUrl;
        private readonly string[] _requiredModels;

        public OllamaSetupChecker(string ollamaUrl = "http://100.108.155.28:11434", params string[] requiredModels)
        {
            _ollamaUrl = ollamaUrl;
            _requiredModels = requiredModels.Length > 0 ? requiredModels : new[] { "llama3.2", "deepseek-coder" };
        }

        /// <summary>
        /// Performs a comprehensive check of Ollama setup.
        /// </summary>
        /// <returns>A detailed setup status.</returns>
        public async Task<OllamaSetupStatus> CheckSetupAsync()
        {
            var status = new OllamaSetupStatus
            {
                OllamaUrl = _ollamaUrl,
                RequiredModels = _requiredModels.ToList()
            };

            try
            {
                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                
                // Test connectivity
                var response = await httpClient.GetAsync($"{_ollamaUrl}/api/tags");
                status.IsOllamaRunning = response.IsSuccessStatusCode;

                if (!status.IsOllamaRunning)
                {
                    status.StatusMessage = $"Ollama is not responding at {_ollamaUrl}. Status: {response.StatusCode}";
                    return status;
                }

                // Get available models
                var jsonContent = await response.Content.ReadAsStringAsync();
                var modelsResponse = JsonSerializer.Deserialize<OllamaModelsResponse>(jsonContent);
                
                if (modelsResponse?.Models != null)
                {
                    status.AvailableModels = modelsResponse.Models.Select(m => m.Name).ToList();
                    status.MissingModels = _requiredModels.Except(status.AvailableModels).ToList();
                    status.IsSetupComplete = !status.MissingModels.Any();
                }

                if (status.IsSetupComplete)
                {
                    status.StatusMessage = "? Ollama is properly configured for integration tests";
                }
                else
                {
                    status.StatusMessage = $"? Missing required models: {string.Join(", ", status.MissingModels)}";
                }
            }
            catch (Exception ex)
            {
                status.StatusMessage = $"? Error checking Ollama setup: {ex.Message}";
            }

            return status;
        }

        /// <summary>
        /// Prints setup instructions for missing components.
        /// </summary>
        /// <param name="status">The setup status to analyze.</param>
        public static void PrintSetupInstructions(OllamaSetupStatus status)
        {
            Console.WriteLine($"Ollama Setup Status for {status.OllamaUrl}");
            Console.WriteLine("=" + new string('=', 50));
            Console.WriteLine();
            Console.WriteLine(status.StatusMessage);
            Console.WriteLine();

            if (!status.IsOllamaRunning)
            {
                Console.WriteLine("?? Setup Instructions:");
                Console.WriteLine("1. Install Ollama from https://ollama.ai");
                Console.WriteLine("2. Start Ollama service: ollama serve");
                Console.WriteLine("3. Verify it's running by visiting the URL in a browser");
                Console.WriteLine();
            }
            else if (status.MissingModels.Any())
            {
                Console.WriteLine("?? Missing Models - Run these commands:");
                foreach (var model in status.MissingModels)
                {
                    Console.WriteLine($"   ollama pull {model}");
                }
                Console.WriteLine();
            }

            if (status.AvailableModels.Any())
            {
                Console.WriteLine("? Available Models:");
                foreach (var model in status.AvailableModels)
                {
                    var isRequired = status.RequiredModels.Contains(model);
                    Console.WriteLine($"   {(isRequired ? "?" : "•")} {model}");
                }
                Console.WriteLine();
            }

            if (status.IsSetupComplete)
            {
                Console.WriteLine("?? Ready to run integration tests!");
                Console.WriteLine("   dotnet test --filter \"FullyQualifiedName~Integration\"");
            }
        }
    }

    /// <summary>
    /// Represents the status of Ollama setup for integration tests.
    /// </summary>
    public class OllamaSetupStatus
    {
        public string OllamaUrl { get; set; } = string.Empty;
        public bool IsOllamaRunning { get; set; }
        public bool IsSetupComplete { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
        public List<string> RequiredModels { get; set; } = new();
        public List<string> AvailableModels { get; set; } = new();
        public List<string> MissingModels { get; set; } = new();
    }
}