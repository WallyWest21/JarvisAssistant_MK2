# Jarvis Assistant LLM Service

This document describes the implementation of the LLM (Large Language Model) service with Ollama integration and Jarvis personality for the Jarvis Assistant application.

## Overview

The LLM service provides sophisticated AI capabilities with a distinctive Jarvis personality, featuring:

- **Ollama Integration**: Direct integration with Ollama for local LLM inference
- **Jarvis Personality**: British sophistication with wit and intelligence
- **Streaming Responses**: Real-time response streaming via SignalR
- **Smart Model Selection**: Automatic model selection based on query type
- **Error Handling**: Graceful error handling with personality-consistent responses

## Architecture

### Core Components

1. **OllamaClient**: HTTP client for Ollama API communication
2. **PersonalityService**: Applies Jarvis personality to responses
3. **OllamaLLMService**: Main service implementing ILLMService
4. **ChatStreamingHub**: SignalR hub for real-time streaming
5. **StreamingResponseService**: Service for managing streaming responses

### Service Dependencies

```
OllamaLLMService
├── OllamaClient (HTTP communication)
├── PersonalityService (Response formatting)
└── ILogger<OllamaLLMService> (Logging)
```

## Configuration

### Ollama Server Setup

The service is configured to connect to Ollama at `http://100.108.155.28:11434`. Ensure:

1. Ollama is running on the target server
2. Required models are installed:
   - `llama3.2` (general queries)
   - `deepseek-coder` (code-related queries)

### Service Registration

Register services in your DI container:

```csharp
services.AddOllamaLLMService("http://your-ollama-server:11434");

// Or with custom configuration
services.AddOllamaLLMService()
    .ConfigureOllamaLLMService(options =>
    {
        options.BaseUrl = "http://custom-server:11434";
        options.Temperature = 0.8;
        options.MaxTokens = 4096;
    });
```

## Query Type Detection

The service automatically detects query types based on content:

| Query Type | Triggers | Model Used |
|------------|----------|------------|
| **Code** | "code", "function", "programming", "debug" | deepseek-coder |
| **Error** | "error", "exception", "bug", "fix" | deepseek-coder |
| **Technical** | "technical", "system", "architecture" | llama3.2 |
| **Mathematical** | "calculate", "math", "equation" | llama3.2 |
| **Creative** | "creative", "story", "write", "poem" | llama3.2 |
| **General** | Default for all other queries | llama3.2 |

### Explicit Query Type

You can explicitly specify the query type:

```csharp
var request = new ChatRequest("Help me with this", "conv123")
{
    Context = new Dictionary<string, object> 
    { 
        ["queryType"] = "Code" 
    }
};
```

## Jarvis Personality

### System Prompts

Different system prompts are used based on query type:

- **Base**: Establishes Jarvis as a sophisticated British AI assistant
- **Code**: Emphasizes technical precision with elegant solutions
- **Technical**: Focuses on detailed analysis with sophisticated language
- **Error**: Maintains composure while providing clear guidance

### Response Formatting

Responses are enhanced with:

1. **British Vocabulary**: Sophisticated word replacements
   - "problem" → "challenge"
   - "error" → "complication"
   - "good" → "excellent"

2. **Politeness Patterns**: 
   - Openings: "Certainly, Sir", "Indeed, Sir"
   - Closings: "I trust this meets your requirements"

3. **Contextual Greetings**:
   - Code: "I believe you'll find this solution rather elegant"
   - Error: "I'm afraid there appears to be a complication"

### Personality Configuration

Personality is configured via `PersonalityPrompts.json`:

```json
{
  "systemPrompts": {
    "base": "You are JARVIS, an advanced AI assistant...",
    "code": "You are JARVIS, specializing in code analysis..."
  },
  "responseTemplates": {
    "general": ["At your service, as always, Sir."],
    "code": ["I believe you'll find this solution rather elegant."]
  },
  "vocabularyEnhancements": {
    "replacements": {
      "problem": "challenge",
      "error": "complication"
    }
  }
}
```

## Usage Examples

### Basic Message

```csharp
var llmService = serviceProvider.GetRequiredService<ILLMService>();

var request = new ChatRequest("Hello, how are you?", "conversation-123");
var response = await llmService.SendMessageAsync(request);

Console.WriteLine(response.Message);
// Output: "Certainly, Sir. I am functioning quite normally. I trust this meets your requirements, Sir."
```

### Streaming Response

```csharp
var request = new ChatRequest("Explain artificial intelligence", "conversation-123");

await foreach (var chunk in llmService.StreamResponseAsync(request))
{
    if (!chunk.IsComplete)
    {
        Console.Write(chunk.Message);
    }
    else
    {
        Console.WriteLine("\n[Response complete]");
    }
}
```

### Code Assistance

```csharp
var request = new ChatRequest("Write a function to sort an array in C#", "conversation-123");
var response = await llmService.SendMessageAsync(request);

// Response will use deepseek-coder model and include elegant language
```

## SignalR Streaming

### Hub Setup

Configure SignalR in your application:

```csharp
app.MapHub<ChatStreamingHub>("/chatHub");
```

### Client Connection

Connect to the hub for real-time streaming:

```typescript
const connection = new HubConnectionBuilder()
    .withUrl("/chatHub")
    .build();

connection.on("ReceiveResponseChunk", (response) => {
    // Handle streaming chunk
    displayChunk(response.message);
});

connection.on("ReceiveCompletion", (finalResponse) => {
    // Handle completion
    markComplete();
});

await connection.start();
await connection.invoke("JoinConversation", conversationId);
```

## Error Handling

The service provides graceful error handling:

### Connection Errors
```
"Unable to connect to Ollama server. Please ensure it's running and accessible."
```

### Timeout Errors
```
"The request to Ollama timed out. The model may be taking longer than expected to respond."
```

### Personality-Consistent Errors
```
"I'm afraid there appears to be a complication, Sir. Please allow me a moment to resolve this matter."
```

## Testing

### Unit Tests

Run unit tests with:

```bash
dotnet test JarvisAssistant.UnitTests
```

Test coverage includes:
- OllamaClient HTTP interactions
- PersonalityService formatting
- OllamaLLMService integration
- Error scenarios and edge cases

### Integration Tests

Integration tests require a running Ollama instance:

```bash
# Skip integration tests in CI
dotnet test --filter "Category!=Integration"

# Run all tests including integration (requires Ollama)
dotnet test
```

### Mock Testing

Unit tests use Moq for HTTP mocking:

```csharp
_mockHttpHandler.SetupRequest(HttpMethod.Post, "/api/generate")
    .ReturnsResponse(HttpStatusCode.OK, responseJson);
```

## Performance Considerations

### Model Selection
- **Code queries**: Use smaller, specialized models (deepseek-coder)
- **General queries**: Use balanced models (llama3.2)

### Streaming
- Chunks are processed in real-time
- Large responses don't block the UI
- Cancellation is supported

### Caching
- Consider implementing response caching for repeated queries
- Model metadata is cached locally

### Resource Usage
- HTTP connections are reused
- Timeouts prevent hanging requests
- Memory usage is managed for streaming

## Troubleshooting

### Common Issues

1. **Ollama Connection Failed**
   - Verify Ollama is running: `ollama list`
   - Check network connectivity
   - Verify base URL configuration

2. **Models Not Found**
   - Install required models: `ollama pull llama3.2`
   - Check available models: `ollama list`

3. **Slow Responses**
   - Monitor Ollama resource usage
   - Consider using smaller models
   - Increase timeout settings

4. **Personality Not Applied**
   - Verify PersonalityPrompts.json is accessible
   - Check file format and structure
   - Review logging for load errors

### Logging

Enable detailed logging:

```csharp
services.AddLogging(builder => 
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});
```

Log entries include:
- Request/response timing
- Model selection decisions
- Error details and stack traces
- Personality application status

## Future Enhancements

### Planned Features

1. **Advanced Model Selection**
   - Dynamic model switching based on response quality
   - User preferences for model selection

2. **Conversation Context**
   - Multi-turn conversation memory
   - Context-aware personality adjustments

3. **Response Quality Metrics**
   - Response time tracking
   - User feedback integration
   - Model performance analytics

4. **Enhanced Personality**
   - Mood-based responses
   - Time-of-day personality variations
   - User-specific personality tuning

### Configuration Extensions

```csharp
services.ConfigureOllamaLLMService(options =>
{
    options.EnableAdvancedPersonality = true;
    options.ConversationMemorySize = 10;
    options.ResponseQualityTracking = true;
});
```

## Dependencies

- **.NET 8.0**: Target framework
- **Microsoft.Extensions.Http**: HTTP client factory
- **Microsoft.AspNetCore.SignalR**: Real-time communication
- **System.Text.Json**: JSON serialization
- **Microsoft.Extensions.Logging**: Logging framework

## Security Considerations

- Sanitize user inputs before sending to Ollama
- Implement rate limiting for API calls
- Secure SignalR connections with authentication
- Monitor for prompt injection attempts
- Log security-relevant events

## License

This implementation is part of the Jarvis Assistant project and follows the project's licensing terms.
