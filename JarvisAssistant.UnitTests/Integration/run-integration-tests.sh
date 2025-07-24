#!/bin/bash

# Script to run integration tests with Ollama
set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" &> /dev/null && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." &> /dev/null && pwd)"

echo "?? Starting Ollama Integration Tests"

# Check if Ollama is already running
if curl -s http://localhost:11434/api/tags > /dev/null 2>&1; then
    echo "? Ollama is already running"
    USE_EXISTING=true
else
    echo "?? Starting Ollama with Docker Compose..."
    cd "$SCRIPT_DIR"
    docker-compose -f docker-compose.test.yml up -d ollama
    
    # Wait for Ollama to be ready
    echo "? Waiting for Ollama to be ready..."
    timeout 120 bash -c 'until curl -s http://localhost:11434/api/tags > /dev/null; do sleep 2; done'
    
    echo "?? Pulling required models..."
    docker-compose -f docker-compose.test.yml up ollama-setup
    USE_EXISTING=false
fi

# Set environment variable to enable integration tests
export JARVIS_RUN_INTEGRATION_TESTS=true

# Run the integration tests
echo "?? Running integration tests..."
cd "$PROJECT_ROOT"
dotnet test JarvisAssistant.UnitTests --filter "FullyQualifiedName~Integration" --verbosity normal

EXIT_CODE=$?

# Cleanup if we started Ollama
if [ "$USE_EXISTING" = false ]; then
    echo "?? Cleaning up test environment..."
    cd "$SCRIPT_DIR"
    docker-compose -f docker-compose.test.yml down -v
fi

if [ $EXIT_CODE -eq 0 ]; then
    echo "? Integration tests passed!"
else
    echo "? Integration tests failed!"
fi

exit $EXIT_CODE