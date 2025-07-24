using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using JarvisAssistant.Core.Models;

namespace JarvisAssistant.Services.LLM
{
    /// <summary>
    /// Service responsible for applying Jarvis personality to responses.
    /// </summary>
    public class PersonalityService : IPersonalityService
    {
        private readonly ILogger<PersonalityService> _logger;
        private readonly Random _random = new();
        private PersonalityPrompts? _prompts;

        public PersonalityService(ILogger<PersonalityService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            LoadPersonalityPrompts();
        }

        /// <summary>
        /// Formats a response with Jarvis personality based on the query type.
        /// </summary>
        /// <param name="originalResponse">The original response from the LLM.</param>
        /// <param name="queryType">The type of query to determine personality style.</param>
        /// <param name="isStreaming">Whether this is part of a streaming response.</param>
        /// <returns>The formatted response with Jarvis personality.</returns>
        public async Task<string> FormatResponseAsync(string originalResponse, QueryType queryType, bool isStreaming = false)
        {
            if (string.IsNullOrWhiteSpace(originalResponse) || _prompts == null)
                return originalResponse;

            try
            {
                var formattedResponse = originalResponse;

                // Apply vocabulary enhancements
                formattedResponse = ApplyVocabularyEnhancements(formattedResponse);

                // Add personality elements for complete responses
                if (!isStreaming)
                {
                    formattedResponse = AddPersonalityElements(formattedResponse, queryType);
                }

                return formattedResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying personality formatting");
                return originalResponse; // Return original on error
            }
        }

        /// <summary>
        /// Gets the system prompt for the specified query type.
        /// </summary>
        /// <param name="queryType">The type of query.</param>
        /// <returns>The appropriate system prompt.</returns>
        public string GetSystemPrompt(QueryType queryType)
        {
            if (_prompts?.SystemPrompts == null)
                return GetDefaultSystemPrompt();

            var promptKey = queryType.ToString().ToLowerInvariant();
            
            return _prompts.SystemPrompts.TryGetValue(promptKey, out var prompt) 
                ? prompt 
                : _prompts.SystemPrompts.GetValueOrDefault("base", GetDefaultSystemPrompt());
        }

        /// <summary>
        /// Gets a contextual greeting based on the query type.
        /// </summary>
        /// <param name="queryType">The type of query.</param>
        /// <returns>An appropriate greeting.</returns>
        public string GetContextualGreeting(QueryType queryType)
        {
            if (_prompts?.ResponseTemplates == null)
                return "At your service, Sir.";

            var templateKey = queryType.ToString().ToLowerInvariant();
            var templates = _prompts.ResponseTemplates.GetValueOrDefault(templateKey) 
                ?? _prompts.ResponseTemplates.GetValueOrDefault("general");

            if (templates != null && templates.Count > 0)
            {
                return templates[_random.Next(templates.Count)];
            }

            return "At your service, Sir.";
        }

        /// <summary>
        /// Determines if the user should be addressed as "Sir" or "Madam".
        /// </summary>
        /// <param name="context">Optional context to determine appropriate addressing.</param>
        /// <returns>The appropriate form of address.</returns>
        public string GetAppropriateAddress(string? context = null)
        {
            // For now, default to "Sir" - could be enhanced with context analysis
            // or user preferences in the future
            return "Sir";
        }

        /// <summary>
        /// Loads personality prompts from the JSON file.
        /// </summary>
        private void LoadPersonalityPrompts()
        {
            try
            {
                var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
                var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);
                var promptsPath = Path.Combine(assemblyDirectory!, "LLM", "PersonalityPrompts.json");

                if (!File.Exists(promptsPath))
                {
                    _logger.LogWarning("PersonalityPrompts.json not found at {Path}. Using default prompts.", promptsPath);
                    return;
                }

                var json = File.ReadAllText(promptsPath);
                _prompts = JsonSerializer.Deserialize<PersonalityPrompts>(json, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    AllowTrailingCommas = true
                });

                _logger.LogInformation("Personality prompts loaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load personality prompts. Using defaults.");
            }
        }

        /// <summary>
        /// Applies vocabulary enhancements to make the response more sophisticated.
        /// </summary>
        /// <param name="response">The original response.</param>
        /// <returns>The enhanced response.</returns>
        private string ApplyVocabularyEnhancements(string response)
        {
            if (_prompts?.VocabularyEnhancements?.Replacements == null)
                return response;

            var enhancedResponse = response;

            // Apply word replacements
            foreach (var replacement in _prompts.VocabularyEnhancements.Replacements)
            {
                var pattern = @"\b" + Regex.Escape(replacement.Key) + @"\b";
                enhancedResponse = Regex.Replace(enhancedResponse, pattern, replacement.Value, RegexOptions.IgnoreCase);
            }

            return enhancedResponse;
        }

        /// <summary>
        /// Adds personality elements like openings, closings, and transitions.
        /// </summary>
        /// <param name="response">The response to enhance.</param>
        /// <param name="queryType">The type of query.</param>
        /// <returns>The response with personality elements added.</returns>
        private string AddPersonalityElements(string response, QueryType queryType)
        {
            if (_prompts?.PolitenessPatterns == null)
                return response;

            var result = response;

            // Add opening politeness
            if (_prompts.PolitenessPatterns.Opening != null && _prompts.PolitenessPatterns.Opening.Count > 0)
            {
                var opening = _prompts.PolitenessPatterns.Opening[_random.Next(_prompts.PolitenessPatterns.Opening.Count)];
                result = $"{opening} {result}";
            }

            // Add closing politeness
            if (_prompts.PolitenessPatterns.Closing != null && _prompts.PolitenessPatterns.Closing.Count > 0)
            {
                var closing = _prompts.PolitenessPatterns.Closing[_random.Next(_prompts.PolitenessPatterns.Closing.Count)];
                result = $"{result}\n\n{closing}";
            }

            return result;
        }

        /// <summary>
        /// Gets the default system prompt when the JSON file is not available.
        /// </summary>
        /// <returns>A default system prompt.</returns>
        private static string GetDefaultSystemPrompt()
        {
            return "You are JARVIS, an advanced AI assistant with a sophisticated British manner of speaking. " +
                   "You are knowledgeable, helpful, and maintain technical accuracy while adding personality to your responses. " +
                   "Address users respectfully and provide assistance with intelligence and subtle wit.";
        }
    }

    // Data models for personality prompts JSON
    internal class PersonalityPrompts
    {
        public Dictionary<string, string>? SystemPrompts { get; set; }
        public Dictionary<string, List<string>>? ResponseTemplates { get; set; }
        public PolitenessPatterns? PolitenessPatterns { get; set; }
        public VocabularyEnhancements? VocabularyEnhancements { get; set; }
    }

    internal class PolitenessPatterns
    {
        public List<string>? Opening { get; set; }
        public List<string>? Closing { get; set; }
        public List<string>? Transitions { get; set; }
    }

    internal class VocabularyEnhancements
    {
        public Dictionary<string, string>? Replacements { get; set; }
        public List<string>? BritishPhrases { get; set; }
    }
}
