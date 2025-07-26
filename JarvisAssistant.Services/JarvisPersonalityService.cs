using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models;
using JarvisAssistant.Services.LLM;
using Microsoft.Extensions.Logging;

namespace JarvisAssistant.Services
{
    /// <summary>
    /// Enhanced personality service with contextual Jarvis responses.
    /// </summary>
    public class JarvisPersonalityService : IPersonalityService
    {
        private readonly ILogger<JarvisPersonalityService> _logger;
        private readonly ITelemetryService _telemetryService;
        private readonly Random _random;
        private readonly Dictionary<string, List<string>> _responses;

        public JarvisPersonalityService(ILogger<JarvisPersonalityService> logger, ITelemetryService telemetryService)
        {
            _logger = logger;
            _telemetryService = telemetryService;
            _random = new Random();
            _responses = InitializeResponses();
        }

        public async Task<string> FormatResponseAsync(string originalResponse, QueryType queryType, bool isStreaming = false)
        {
            var personality = GetPersonalityForQueryType(queryType);
            await _telemetryService.TrackFeatureUsageAsync("PersonalityResponse", new Dictionary<string, object>
            {
                ["queryType"] = queryType.ToString(),
                ["personalityType"] = personality,
                ["isStreaming"] = isStreaming
            });

            return personality switch
            {
                "greeting" => GetRandomResponse("greetings") + " " + originalResponse,
                "helpful" => GetRandomResponse("helpful") + " " + originalResponse,
                "witty" => originalResponse + " " + GetRandomResponse("witty"),
                "professional" => GetRandomResponse("professional") + " " + originalResponse,
                "technical" => GetRandomResponse("professional") + " " + originalResponse,
                _ => originalResponse
            };
        }

        public string GetSystemPrompt(QueryType queryType)
        {
            return queryType switch
            {
                QueryType.General => "You are Jarvis, an intelligent AI assistant. Be helpful, professional, and slightly formal. Address the user as 'Sir' or 'Madam' when appropriate.",
                QueryType.Code => "You are Jarvis, a technical AI assistant specializing in code analysis and programming guidance. Provide precise, actionable technical advice.",
                QueryType.Technical => "You are Jarvis, a technical expert AI assistant. Analyze technical data with precision and provide comprehensive insights.",
                QueryType.Creative => "You are Jarvis, an AI assistant with creative capabilities. Help with creative tasks while maintaining your characteristic measured and intelligent demeanor.",
                QueryType.Mathematical => "You are Jarvis, an AI assistant specialized in mathematical computations and analysis. Provide accurate calculations and clear explanations.",
                QueryType.Error => "You are Jarvis, an AI assistant focused on error analysis and troubleshooting. Help diagnose issues with systematic precision.",
                _ => "You are Jarvis, an intelligent AI assistant. Be helpful and professional."
            };
        }

        public string GetContextualGreeting(QueryType queryType)
        {
            return queryType switch
            {
                QueryType.General => "Good day. How may I assist you today?",
                QueryType.Code => "Ready to assist with your programming needs, Sir.",
                QueryType.Technical => "Technical systems standing by. What requires analysis?",
                QueryType.Creative => "Creative subroutines activated. How may I help inspire you today?",
                QueryType.Mathematical => "Mathematical processing ready. What calculations shall I perform?",
                QueryType.Error => "Diagnostic mode engaged. Let me help identify the issue.",
                _ => "At your service. What can I do for you?"
            };
        }

        public string GetAppropriateAddress(string? context = null)
        {
            // For now, default to "Sir" - could be enhanced with context analysis
            return "Sir";
        }

        private Dictionary<string, List<string>> InitializeResponses()
        {
            return new Dictionary<string, List<string>>
            {
                ["greetings"] = new List<string>
                {
                    "Good to see you again, Sir.",
                    "At your service.",
                    "How may I assist you today?",
                    "Ready to help as always.",
                    "What can I do for you?"
                },
                ["helpful"] = new List<string>
                {
                    "I'm here to help.",
                    "Let me take care of that for you.",
                    "Consider it done.",
                    "Right away, Sir.",
                    "I'll handle that immediately."
                },
                ["witty"] = new List<string>
                {
                    "Was that helpful, or shall I try harder?",
                    "My algorithms are quite satisfied with that result.",
                    "Another successful interaction logged.",
                    "Efficiency: optimal.",
                    "I do aim to please."
                },
                ["professional"] = new List<string>
                {
                    "Based on my analysis,",
                    "According to my calculations,",
                    "My systems indicate that",
                    "Processing complete:",
                    "Analysis suggests that"
                }
            };
        }

        private string GetPersonalityForQueryType(QueryType queryType) => queryType switch
        {
            QueryType.General => "helpful",
            QueryType.Code => "professional",
            QueryType.Technical => "professional",
            QueryType.Creative => "helpful",
            QueryType.Mathematical => "professional",
            QueryType.Error => "professional",
            _ => "helpful"
        };

        private string GetRandomResponse(string category) => 
            _responses.TryGetValue(category, out var responses) 
                ? responses[_random.Next(responses.Count)] 
                : string.Empty;
    }
}
