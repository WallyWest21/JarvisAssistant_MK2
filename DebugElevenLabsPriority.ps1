# Test ElevenLabs Service Registration and Priority
Write-Host "=== ElevenLabs Service Priority Test ===" -ForegroundColor Red
Write-Host ""

# Set environment variable for current session
$env:ELEVENLABS_API_KEY = "sk_572262d27043d888785a02694bc21fbdc70b548cc017b119"
Write-Host "✅ ElevenLabs API Key: $($env:ELEVENLABS_API_KEY.Substring(0, 8))..." -ForegroundColor Green

# Test if the app is actually detecting the environment variable
Write-Host ""
Write-Host "🔍 DEBUGGING SERVICE REGISTRATION:" -ForegroundColor Yellow
Write-Host "Environment variable set: $([Environment]::GetEnvironmentVariable('ELEVENLABS_API_KEY') -ne $null)" -ForegroundColor White

if ([Environment]::GetEnvironmentVariable('ELEVENLABS_API_KEY')) {
    Write-Host "✅ Environment.GetEnvironmentVariable finds the key" -ForegroundColor Green
} else {
    Write-Host "❌ Environment.GetEnvironmentVariable does NOT find the key" -ForegroundColor Red
    Write-Host "This is why ElevenLabs isn't being registered!" -ForegroundColor Red
}

Write-Host ""
Write-Host "🚨 LIKELY ISSUE:" -ForegroundColor Red
Write-Host "The MAUI app is not picking up the environment variable we just set." -ForegroundColor White
Write-Host "Visual Studio might need to be restarted to see the new environment variable." -ForegroundColor White

Write-Host ""
Write-Host "🔧 IMMEDIATE FIX NEEDED:" -ForegroundColor Yellow
Write-Host "1. Force ElevenLabs registration regardless of environment variable" -ForegroundColor White
Write-Host "2. Hardcode the API key temporarily in MauiProgram.cs" -ForegroundColor White
Write-Host "3. Or restart Visual Studio to pick up the environment variable" -ForegroundColor White
