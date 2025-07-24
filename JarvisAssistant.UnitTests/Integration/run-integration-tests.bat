@echo off
REM Script to run integration tests with Ollama on Windows
setlocal enabledelayedexpansion

echo ?? Starting Ollama Integration Tests

REM Check if Ollama is already running
curl -s http://localhost:11434/api/tags >nul 2>&1
if !errorlevel! equ 0 (
    echo ? Ollama is already running
    set USE_EXISTING=true
) else (
    echo ?? Starting Ollama with Docker Compose...
    cd /d "%~dp0"
    docker-compose -f docker-compose.test.yml up -d ollama
    
    echo ? Waiting for Ollama to be ready...
    :wait_loop
    timeout /t 2 /nobreak >nul
    curl -s http://localhost:11434/api/tags >nul 2>&1
    if !errorlevel! neq 0 goto wait_loop
    
    echo ?? Pulling required models...
    docker-compose -f docker-compose.test.yml up ollama-setup
    set USE_EXISTING=false
)

REM Set environment variable to enable integration tests
set JARVIS_RUN_INTEGRATION_TESTS=true

REM Run the integration tests
echo ?? Running integration tests...
cd /d "%~dp0..\.."
dotnet test JarvisAssistant.UnitTests --filter "FullyQualifiedName~Integration" --verbosity normal

set EXIT_CODE=!errorlevel!

REM Cleanup if we started Ollama
if "!USE_EXISTING!"=="false" (
    echo ?? Cleaning up test environment...
    cd /d "%~dp0"
    docker-compose -f docker-compose.test.yml down -v
)

if !EXIT_CODE! equ 0 (
    echo ? Integration tests passed!
) else (
    echo ? Integration tests failed!
)

exit /b !EXIT_CODE!