# ElevenLabs API Verification Script
# This script tests the new API key and voice ID

Write-Host "🔊 Testing ElevenLabs API with new credentials..." -ForegroundColor Cyan
Write-Host ""

# Set the credentials
$env:ELEVENLABS_API_KEY = "sk_572262d27043d888785a02694bc21fbdc70b548cc017b119"
$env:ELEVENLABS_VOICE_ID = "91AxxCADnelg9FDuKsIS"

Write-Host "API Key: $($env:ELEVENLABS_API_KEY.Substring(0, 15))..." -ForegroundColor Green
Write-Host "Voice ID: $env:ELEVENLABS_VOICE_ID" -ForegroundColor Green
Write-Host ""

# Test API connection
Write-Host "Testing API connection..." -ForegroundColor Yellow
try {
    $headers = @{
        'xi-api-key' = $env:ELEVENLABS_API_KEY
        'Content-Type' = 'application/json'
    }
    
    $response = Invoke-RestMethod -Uri "https://api.elevenlabs.io/v1/user" -Headers $headers -Method Get
    Write-Host "✅ API connection successful!" -ForegroundColor Green
    Write-Host "User subscription: $($response.subscription.tier)" -ForegroundColor Cyan
    Write-Host "Characters remaining: $($response.subscription.character_count)/$($response.subscription.character_limit)" -ForegroundColor Cyan
} catch {
    Write-Host "❌ API connection failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test voice availability
Write-Host "Testing voice availability..." -ForegroundColor Yellow
try {
    $voicesResponse = Invoke-RestMethod -Uri "https://api.elevenlabs.io/v1/voices" -Headers $headers -Method Get
    $targetVoice = $voicesResponse.voices | Where-Object { $_.voice_id -eq $env:ELEVENLABS_VOICE_ID }
    
    if ($targetVoice) {
        Write-Host "✅ Voice found: $($targetVoice.name)" -ForegroundColor Green
        Write-Host "Voice description: $($targetVoice.description)" -ForegroundColor Cyan
        Write-Host "Voice category: $($targetVoice.category)" -ForegroundColor Cyan
    } else {
        Write-Host "❌ Voice ID not found in available voices" -ForegroundColor Red
        Write-Host "Available voices:" -ForegroundColor Yellow
        $voicesResponse.voices | ForEach-Object { 
            Write-Host "  • $($_.name) ($($_.voice_id))" -ForegroundColor White
        }
    }
} catch {
    Write-Host "❌ Voice check failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "🎤 ElevenLabs configuration verification complete!" -ForegroundColor Cyan
Write-Host "JARVIS will now use the new voice for speech synthesis." -ForegroundColor Green
