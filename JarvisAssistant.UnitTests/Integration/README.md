# LLM Service Integration Tests

This directory contains integration tests for the LLM service components that require a running Ollama instance.

## Overview

The integration tests validate the full end-to-end functionality of the LLM service, including:
- Real communication with Ollama API
- Model selection and switching
- Personality formatting
- Streaming responses
- Error handling with real network conditions

## Prerequisites

### Option 1: Local Ollama Installation
1. Install Ollama from [https://ollama.ai](https://ollama.ai)
2. Start Ollama: `ollama serve`
3. Pull required models:
   ```bash
   ollama pull llama3.2
   ollama pull deepseek-coder
   ```

### Option 2: Docker Environment
1. Ensure Docker and Docker Compose are installed
2. Use the provided Docker Compose setup (see below)

## Running Integration Tests

### Automatic Setup (Recommended)
Use the provided scripts that handle Ollama setup automatically:

**Linux/macOS:**
```bash
chmod +x ./run-integration-tests.sh
./run-integration-tests.sh
```

**Windows:**
```cmd
run-integration-tests.bat
```

### Manual Setup
1. Start Ollama (locally or via Docker):
   ```bash
   docker-compose -f docker-compose.test.yml up -d
   ```

2. Enable integration tests:
   ```bash
   export JARVIS_RUN_INTEGRATION_TESTS=true  # Linux/macOS
   set JARVIS_RUN_INTEGRATION_TESTS=true     # Windows
   ```

3. Run the tests:
   ```bash
   dotnet test JarvisAssistant.UnitTests --filter "FullyQualifiedName~Integration"
   ```

### CI/CD Integration
For continuous integration, set the environment variable `JARVIS_RUN_INTEGRATION_TESTS=true` in your CI pipeline.

## Configuration

### Test Settings
Modify `testsettings.json` to customize test behavior:

```json
{
  "OllamaUrl": "http://localhost:11434",
  "TestTimeout": "00:05:00",
  "EnableIntegrationTests": true,
  "TestModels": {
    "General": "llama3.2",
    "Code": "deepseek-coder"
  },
  "MaxStreamingChunks": 10,
  "CancellationTestDelay": "00:00:02"
}
```

### Environment Variables
- `JARVIS_RUN_INTEGRATION_TESTS`: Set to `true` to enable integration tests
- Override any test setting via environment variables

## Test Categories

### Basic Functionality Tests
- `SendMessageAsync_WithRealOllamaInstance_ReturnsJarvisResponse`
- `GetActiveModelAsync_WithRealOllamaInstance_ReturnsValidModel`

### Model Selection Tests
- `SendMessageAsync_WithCodeQuery_UsesCodeModel`
- `SendMessageAsync_WithDifferentQueryTypes_ReturnsAppropriateResponses`

### Streaming Tests
- `StreamResponseAsync_WithRealOllamaInstance_StreamsJarvisResponse`
- `StreamResponseAsync_CancellationToken_CancelsGracefully`

### Personality Tests
- `SendMessageAsync_PersonalityConsistency_MaintainsJarvisCharacter`

## Troubleshooting

### Common Issues

**Tests are skipped with "Ollama instance is not available"**
- Ensure Ollama is running on the configured URL
- Check firewall settings
- Verify models are pulled and available

**Timeout errors**
- Increase `TestTimeout` in `testsettings.json`
- Check Ollama server performance
- Verify model availability

**Model not found errors**
- Ensure required models are pulled: `ollama pull llama3.2 deepseek-coder`
- Check model names in configuration match available models

### Docker Issues
**Port already in use**
```bash
# Check what's using port 11434
netstat -tulpn | grep 11434
# Stop existing Ollama containers
docker stop $(docker ps -q --filter "ancestor=ollama/ollama")
```

**Permission denied**
```bash
# Make scripts executable
chmod +x run-integration-tests.sh
```

## Development Guidelines

### Adding New Integration Tests
1. Create tests in the `LLMServiceIntegrationTests` class
2. Use `IntegrationTestHelper.SkipIfOllamaNotAvailableAsync()` at the beginning
3. Follow the existing naming convention: `MethodName_Scenario_ExpectedResult`
4. Add appropriate assertions for real-world scenarios

### Best Practices
- Keep test execution time reasonable (use `MaxStreamingChunks` to limit streaming tests)
- Test both success and failure scenarios
- Verify Jarvis personality characteristics in responses
- Use realistic prompts that exercise different query types

## Docker Compose Services

### ollama
- Runs the Ollama server
- Exposes port 11434
- Includes health checks

### ollama-setup
- Pulls required models after Ollama starts
- Runs once and exits

## Files Overview

- `IntegrationTestHelper.cs` - Helper utilities for test setup and Ollama availability checking
- `IntegrationTestSettings.cs` - Configuration model for test settings
- `testsettings.json` - Test configuration file
- `docker-compose.test.yml` - Docker environment setup
- `run-integration-tests.sh/.bat` - Test execution scripts
- `LLMServiceIntegrationTests.cs` - Main integration test class