# ElevenLabs Audible Test Setup
# This script helps you run the audible ElevenLabs voice tests

Write-Host "ElevenLabs Audible Test Setup" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Green

# Check if ElevenLabs API key is set
$apiKey = $env:ELEVENLABS_API_KEY
if (-not $apiKey) {
    Write-Host ""
    Write-Warning "ElevenLabs API key not found!"
    Write-Host "To run the audible tests, you need to:"
    Write-Host "1. Get an API key from https://elevenlabs.io/"
    Write-Host "2. Set it as an environment variable:"
    Write-Host "   `$env:ELEVENLABS_API_KEY = 'your-api-key-here'" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Or you can set it temporarily for this session:"
    $userApiKey = Read-Host "Enter your ElevenLabs API key (or press Enter to skip)"
    if ($userApiKey) {
        $env:ELEVENLABS_API_KEY = $userApiKey
        Write-Host "API key set for this session!" -ForegroundColor Green
    } else {
        Write-Host "Skipping API key setup. Tests will be skipped." -ForegroundColor Yellow
    }
} else {
    Write-Host "ElevenLabs API key found: $($apiKey.Substring(0, 8))..." -ForegroundColor Green
}

Write-Host ""
Write-Host "Running ElevenLabs audible tests..." -ForegroundColor Cyan

# Run the specific audible tests
dotnet test "JarvisAssistant.UnitTests.csproj" --filter "Category=Audible" --logger "console;verbosity=detailed"

Write-Host ""
Write-Host "Test execution completed!" -ForegroundColor Green
Write-Host ""
Write-Host "Note: These tests will:"
Write-Host "- Make real API calls to ElevenLabs"
Write-Host "- Generate actual audio files"
Write-Host "- Attempt to play audio through your speakers"
Write-Host "- Use your ElevenLabs API quota"
