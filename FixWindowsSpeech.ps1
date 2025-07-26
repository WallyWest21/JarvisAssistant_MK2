# Fix Windows Speech Service for Jarvis TTS
Write-Host "=== Windows Speech Service Fix ===" -ForegroundColor Green
Write-Host ""

# Check and start Windows Audio service (required for TTS)
try {
    Write-Host "Checking Windows Audio service..." -ForegroundColor Yellow
    $audioService = Get-Service -Name "AudioSrv" -ErrorAction SilentlyContinue
    if ($audioService) {
        if ($audioService.Status -ne "Running") {
            Write-Host "Starting Windows Audio service..." -ForegroundColor Yellow
            Start-Service -Name "AudioSrv"
            Write-Host "✓ Windows Audio service started" -ForegroundColor Green
        } else {
            Write-Host "✓ Windows Audio service is running" -ForegroundColor Green
        }
    } else {
        Write-Host "⚠ Windows Audio service not found" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Error with Windows Audio service: $($_.Exception.Message)" -ForegroundColor Red
}

# Check Windows Audio Endpoint Builder
try {
    Write-Host "Checking Audio Endpoint Builder..." -ForegroundColor Yellow
    $endpointService = Get-Service -Name "AudioEndpointBuilder" -ErrorAction SilentlyContinue
    if ($endpointService) {
        if ($endpointService.Status -ne "Running") {
            Write-Host "Starting Audio Endpoint Builder..." -ForegroundColor Yellow
            Start-Service -Name "AudioEndpointBuilder"
            Write-Host "✓ Audio Endpoint Builder started" -ForegroundColor Green
        } else {
            Write-Host "✓ Audio Endpoint Builder is running" -ForegroundColor Green
        }
    }
} catch {
    Write-Host "❌ Error with Audio Endpoint Builder: $($_.Exception.Message)" -ForegroundColor Red
}

# Test TTS after service fixes
Write-Host ""
Write-Host "Testing TTS after service fixes..." -ForegroundColor Yellow
try {
    Add-Type -AssemblyName System.Speech
    $synth = New-Object System.Speech.Synthesis.SpeechSynthesizer
    $synth.SetOutputToDefaultAudioDevice()
    
    Write-Host "Speaking test message..." -ForegroundColor Yellow
    $synth.Speak("Jarvis TTS is now working correctly")
    Write-Host "✓ TTS test completed successfully!" -ForegroundColor Green
    
    $synth.Dispose()
} catch {
    Write-Host "❌ TTS still not working: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Additional troubleshooting needed..." -ForegroundColor Yellow
    
    # Check for audio devices
    try {
        Write-Host "Checking audio devices..." -ForegroundColor Yellow
        $audioDevices = Get-WmiObject -Class Win32_SoundDevice | Where-Object { $_.Status -eq "OK" }
        if ($audioDevices) {
            Write-Host "✓ Found $($audioDevices.Count) working audio device(s)" -ForegroundColor Green
            foreach ($device in $audioDevices) {
                Write-Host "  - $($device.Name)" -ForegroundColor White
            }
        } else {
            Write-Host "❌ No working audio devices found" -ForegroundColor Red
        }
    } catch {
        Write-Host "❌ Error checking audio devices: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    # Suggest manual steps
    Write-Host ""
    Write-Host "Manual troubleshooting steps:" -ForegroundColor Cyan
    Write-Host "1. Check Windows Sound settings (volume, default device)" -ForegroundColor White
    Write-Host "2. Restart Windows Audio service manually" -ForegroundColor White
    Write-Host "3. Update audio drivers" -ForegroundColor White
    Write-Host "4. Test with Windows built-in Narrator (Win+Ctrl+Enter)" -ForegroundColor White
}

Write-Host ""
Write-Host "Windows Speech service fix completed." -ForegroundColor Green
