# Test ElevenLabs API connection with your credentials
Write-Host "=== ElevenLabs API Test ===" -ForegroundColor Green
Write-Host ""

$apiKey = "sk_572262d27043d888785a02694bc21fbdc70b548cc017b119"
$voiceId = "91AxxCADnelg9FDuKsIS"
$baseUrl = "https://api.elevenlabs.io"

Write-Host "Testing API connection..." -ForegroundColor Yellow
Write-Host "API Key: $($apiKey.Substring(0, 8))..." -ForegroundColor White
Write-Host "Voice ID: $voiceId" -ForegroundColor White
Write-Host ""

try {
    # Test 1: Get voice information
    Write-Host "Test 1: Getting voice information..." -ForegroundColor Yellow
    $headers = @{
        "xi-api-key" = $apiKey
        "Content-Type" = "application/json"
    }
    
    $voiceResponse = Invoke-RestMethod -Uri "$baseUrl/v1/voices/$voiceId" -Method GET -Headers $headers
    Write-Host "✓ Voice found: $($voiceResponse.name)" -ForegroundColor Green
    Write-Host "  - Category: $($voiceResponse.category)" -ForegroundColor White
    Write-Host "  - Description: $($voiceResponse.description)" -ForegroundColor White
    
    # Test 2: Check account quota
    Write-Host "`nTest 2: Checking account quota..." -ForegroundColor Yellow
    $quotaResponse = Invoke-RestMethod -Uri "$baseUrl/v1/user/subscription" -Method GET -Headers $headers
    Write-Host "✓ Account active" -ForegroundColor Green
    Write-Host "  - Character limit: $($quotaResponse.character_limit)" -ForegroundColor White
    Write-Host "  - Characters used: $($quotaResponse.character_count)" -ForegroundColor White
    Write-Host "  - Characters remaining: $($quotaResponse.character_limit - $quotaResponse.character_count)" -ForegroundColor White
    
    # Test 3: Generate a small test audio
    Write-Host "`nTest 3: Generating test audio..." -ForegroundColor Yellow
    $textToSpeak = "Hello! This is a test of ElevenLabs TTS for Jarvis Assistant."
    
    $body = @{
        text = $textToSpeak
        voice_settings = @{
            stability = 0.4
            similarity_boost = 0.9
            style = 0.2
            use_speaker_boost = $true
        }
    } | ConvertTo-Json
    
    $audioResponse = Invoke-RestMethod -Uri "$baseUrl/v1/text-to-speech/$voiceId" -Method POST -Headers $headers -Body $body -ContentType "application/json"
    
    if ($audioResponse) {
        Write-Host "✓ Audio generation successful!" -ForegroundColor Green
        Write-Host "  - Generated audio data: $($audioResponse.Length) bytes" -ForegroundColor White
        
        # Save to temp file and try to play
        $tempFile = [System.IO.Path]::GetTempFileName() + ".mp3"
        [System.IO.File]::WriteAllBytes($tempFile, $audioResponse)
        Write-Host "  - Saved to: $tempFile" -ForegroundColor White
        
        # Try to play the audio
        try {
            Write-Host "  - Attempting to play audio..." -ForegroundColor White
            Start-Process -FilePath $tempFile -WindowStyle Hidden
            Write-Host "✓ Audio playback initiated" -ForegroundColor Green
        } catch {
            Write-Host "⚠ Could not auto-play audio file, but generation was successful" -ForegroundColor Yellow
        }
        
        # Cleanup after a moment
        Start-Sleep -Seconds 3
        try { Remove-Item $tempFile -ErrorAction SilentlyContinue } catch { }
    }
    
    Write-Host "`n🎉 ElevenLabs API is working correctly!" -ForegroundColor Green
    Write-Host "Jarvis will now use ElevenLabs as primary TTS with Windows SAPI as fallback." -ForegroundColor Cyan
    
} catch {
    Write-Host "`n❌ ElevenLabs API test failed:" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode
        Write-Host "Status Code: $statusCode" -ForegroundColor Red
        
        if ($statusCode -eq 401) {
            Write-Host "This suggests an authentication issue with the API key." -ForegroundColor Yellow
        } elseif ($statusCode -eq 429) {
            Write-Host "This suggests rate limiting - too many requests." -ForegroundColor Yellow
        } elseif ($statusCode -eq 400) {
            Write-Host "This suggests a bad request - possibly invalid voice ID or parameters." -ForegroundColor Yellow
        }
    }
    
    Write-Host "`nJarvis will fall back to Windows SAPI TTS." -ForegroundColor Cyan
}

Write-Host ""
Write-Host "Press any key to continue..."
Read-Host
