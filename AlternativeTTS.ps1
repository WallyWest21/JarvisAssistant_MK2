# Alternative TTS test using different methods
Write-Host "=== Alternative TTS Testing ==="

# Method 1: SAPI COM object (older but more reliable)
Write-Host "Method 1: Using SAPI COM object..."
try {
    $sapi = New-Object -ComObject SAPI.SpVoice
    $sapi.Rate = 0
    $sapi.Volume = 100
    Write-Host "Speaking with SAPI COM..."
    $sapi.Speak("Hello from SAPI COM object", 0)
    Write-Host "SAPI COM test completed"
} catch {
    Write-Host "SAPI COM failed: $($_.Exception.Message)"
}

Write-Host ""

# Method 2: PowerShell Add-Type with different configuration
Write-Host "Method 2: System.Speech with minimal configuration..."
try {
    Add-Type -AssemblyName System.Speech
    $speech = New-Object System.Speech.Synthesis.SpeechSynthesizer
    
    # Use minimal configuration
    $speech.Volume = 100
    $speech.Rate = 0
    
    Write-Host "Speaking with minimal System.Speech config..."
    $speech.Speak("Hello from System Speech minimal config")
    $speech.Dispose()
    Write-Host "Minimal System.Speech test completed"
} catch {
    Write-Host "System.Speech minimal failed: $($_.Exception.Message)"
}

Write-Host ""

# Method 3: Check Windows Speech service
Write-Host "Method 3: Checking Windows Speech service..."
$service = Get-Service -Name "AudioSrv" -ErrorAction SilentlyContinue
if ($service) {
    Write-Host "Windows Audio Service status: $($service.Status)"
} else {
    Write-Host "Windows Audio Service not found"
}

Write-Host ""
Write-Host "Alternative tests completed. Press Enter to continue..."
Read-Host
