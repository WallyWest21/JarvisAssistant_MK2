# HTTPS Mockup Pattern for OllamaClient Testing

## Problem Diagnosis

The issue you're experiencing is that the HttpClient is throwing "This instance has already started one or more requests. Properties can only be modified before sending the first request." This happens when:

1. A mock HttpMessageHandler is configured
2. The OllamaClient constructor tries to set BaseAddress/Timeout on the HttpClient
3. The mock configuration has somehow "activated" the HttpClient

## Solution: Proper HTTPS Mockup Pattern

Here's the correct pattern for mocking HTTP requests in OllamaClient tests:

```csharp
[Fact]
public async Task GenerateAsync_WithNetworkError_ThrowsInvalidOperationException()
{
    // 1. Create mock handler FIRST
    var mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
    
    // 2. Create HttpClient with pre-configured properties to prevent OllamaClient from modifying them
    var httpClient = new HttpClient(mockHandler.Object)
    {
        BaseAddress = new Uri("http://localhost:11434"),
        Timeout = TimeSpan.FromMinutes(5)
    };
    
    // 3. Create OllamaClient with options that match the pre-configured HttpClient
    var options = Options.Create(new OllamaLLMOptions
    {
        BaseUrl = "http://localhost:11434",  // Must match httpClient.BaseAddress
        Timeout = TimeSpan.FromMinutes(5)
    });
    var ollamaClient = new OllamaClient(httpClient, mockLogger, options);
    
    // 4. Setup mock behavior AFTER client creation
    mockHandler
        .Protected()
        .Setup<Task<HttpResponseMessage>>(
            "SendAsync",
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>())
        .ThrowsAsync(new HttpRequestException("Connection refused"));

    // 5. Test the behavior
    var exception = await Assert.ThrowsAsync<InvalidOperationException>(
        () => ollamaClient.GenerateAsync("Test prompt"));
    
    exception.Message.Should().Contain("Unable to connect to Ollama server");
}
```

## Alternative: Using Moq.Contrib.HttpClient

If you prefer using Moq.Contrib.HttpClient, the pattern is:

```csharp
[Fact]
public async Task GenerateAsync_WithValidRequest_ReturnsResponse()
{
    // Create mock and configure before HttpClient creation
    var mockHandler = new Mock<HttpMessageHandler>();
    
    mockHandler.SetupRequest(HttpMethod.Post, "http://localhost:11434/api/generate")
        .ReturnsResponse(HttpStatusCode.OK, "{"response": "Test response", "done": true}", "application/json");
    
    // Create HttpClient with mock and pre-configure to match OllamaLLMOptions defaults
    var httpClient = mockHandler.CreateClient();
    httpClient.BaseAddress = new Uri("http://localhost:11434");
    
    // Create OllamaClient - it should skip HttpClient configuration since BaseAddress is already set
    var ollamaClient = new OllamaClient(httpClient, mockLogger);
    
    // Test
    var result = await ollamaClient.GenerateAsync("Test prompt");
    result.Should().Be("Test response");
}
```

## Key Points

1. **Pre-configure HttpClient**: Set BaseAddress and Timeout before passing to OllamaClient
2. **Match OllamaLLMOptions**: Ensure the options passed to OllamaClient match the HttpClient configuration
3. **Setup mocks after client creation**: This prevents conflicts during constructor execution
4. **Use MockBehavior.Strict**: This ensures all HTTP calls are explicitly mocked

## Required Using Statements

```csharp
using Moq;
using Moq.Protected;
using Microsoft.Extensions.Options;
using static Moq.Protected.ItExpr;  // For ItExpr.IsAny<T>()
```

## Root Cause

The issue in your current tests is that the OllamaClient constructor is trying to modify HttpClient properties after the mock has been configured, which HttpClient treats as "after the first request has started". The solution is to pre-configure the HttpClient so the OllamaClient constructor skips the property modification.
