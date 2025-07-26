# Set ElevenLabs API Key for Jarvis MAUI App
# Run this in PowerShell to set the environment variable for the current session

Write-Host "Setting ElevenLabs API Key for Jarvis..." -ForegroundColor Green

# Set the API key directly
$apiKey = "sk_572262d27043d888785a02694bc21fbdc70b548cc017b119"

if ($apiKey) {
    # Set environment variable for current session
    $env:ELEVENLABS_API_KEY = $apiKey
    Write-Host "✅ ElevenLabs API key set for current session!" -ForegroundColor Green
    
    # Optionally set it permanently for the user
    $setPermanent = Read-Host "Set permanently for your user account? (y/n)"
    if ($setPermanent -eq 'y' -or $setPermanent -eq 'Y') {
        [Environment]::SetEnvironmentVariable("ELEVENLABS_API_KEY", $apiKey, "User")
        Write-Host "✅ ElevenLabs API key set permanently!" -ForegroundColor Green
        Write-Host "⚠️  You may need to restart Visual Studio for it to pick up the new environment variable." -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "Now when you run the Jarvis MAUI app, it will use ElevenLabs for real speech!" -ForegroundColor Cyan
    Write-Host "The app will automatically detect the API key and switch from static to real voice." -ForegroundColor Cyan
} else {
    Write-Host "❌ No API key provided." -ForegroundColor Red
}

Write-Host ""
Write-Host "Current ELEVENLABS_API_KEY status:" -ForegroundColor Yellow
if ($env:ELEVENLABS_API_KEY) {
    Write-Host "✅ Set: $($env:ELEVENLABS_API_KEY.Substring(0, 8))..." -ForegroundColor Green
} else {
    Write-Host "❌ Not set" -ForegroundColor Red
}
