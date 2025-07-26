# Advanced TTS Diagnostic - Find the exact cause of beeping
Write-Host "=== Advanced TTS Diagnostic Test ==="
Write-Host ""

# Check Windows audio system status
Write-Host "1. Checking Windows Audio System..."
$audioService = Get-Service -Name "AudioSrv" -ErrorAction SilentlyContinue
if ($audioService) {
    Write-Host "   Audio Service Status: $($audioService.Status)"
} else {
    Write-Host "   ❌ Audio Service not found"
}

$audioEndpoint = Get-Service -Name "AudioEndpointBuilder" -ErrorAction SilentlyContinue
if ($audioEndpoint) {
    Write-Host "   Audio Endpoint Builder: $($audioEndpoint.Status)"
} else {
    Write-Host "   ❌ Audio Endpoint Builder not found"
}

Write-Host ""

# Test 1: Direct SAPI COM (bypasses .NET Speech)
Write-Host "2. Testing SAPI COM Object (bypasses .NET)..."
try {
    $sapi = New-Object -ComObject "SAPI.SpVoice"
    Write-Host "   SAPI COM created successfully"
    $sapi.Volume = 100
    $sapi.Rate = 0
    Write-Host "   Speaking with SAPI COM..."
    $sapi.Speak("SAPI COM test - this bypasses dot NET speech", 0)
    Write-Host "   ✅ SAPI COM test completed"
    [System.Runtime.Interopservices.Marshal]::ReleaseComObject($sapi) | Out-Null
} catch {
    Write-Host "   ❌ SAPI COM failed: $($_.Exception.Message)"
}

Write-Host ""

# Test 2: Check audio devices
Write-Host "3. Checking Audio Devices..."
try {
    Add-Type -TypeDefinition @"
        using System;
        using System.Runtime.InteropServices;
        public class AudioDevices {
            [DllImport("winmm.dll")]
            public static extern int waveOutGetNumDevs();
        }
"@
    $deviceCount = [AudioDevices]::waveOutGetNumDevs()
    Write-Host "   Available audio output devices: $deviceCount"
} catch {
    Write-Host "   Could not check audio devices: $($_.Exception.Message)"
}

Write-Host ""

# Test 3: System.Speech with different output formats
Write-Host "4. Testing System.Speech with different formats..."
Add-Type -AssemblyName System.Speech

# Test 3a: Default format (no explicit format setting)
try {
    Write-Host "   Test 3a: Default audio format"
    $synth1 = New-Object System.Speech.Synthesis.SpeechSynthesizer
    $synth1.Volume = 100
    $synth1.Rate = 0
    $synth1.SetOutputToDefaultAudioDevice()
    $synth1.Speak("Test with default format")
    $synth1.Dispose()
    Write-Host "   ✅ Default format test completed"
} catch {
    Write-Host "   ❌ Default format failed: $($_.Exception.Message)"
}

# Test 3b: WAV file with default format
try {
    Write-Host "   Test 3b: WAV file with default settings"
    $synth2 = New-Object System.Speech.Synthesis.SpeechSynthesizer
    $tempWav = [System.IO.Path]::GetTempFileName() + ".wav"
    
    # Don't specify format - let Windows choose
    $synth2.SetOutputToWaveFile($tempWav)
    $synth2.Speak("WAV test with Windows default format")
    $synth2.SetOutputToDefaultAudioDevice()
    
    $fileInfo = Get-Item $tempWav
    Write-Host "   Generated WAV file: $($fileInfo.Length) bytes"
    
    # Check if the file has content
    if ($fileInfo.Length -gt 1000) {
        Write-Host "   Playing WAV with SoundPlayer..."
        $player = New-Object System.Media.SoundPlayer($tempWav)
        $player.LoadAsync()
        Start-Sleep -Seconds 1  # Wait for load
        $player.PlaySync()
        Write-Host "   ✅ WAV playback completed"
    } else {
        Write-Host "   ❌ WAV file too small ($($fileInfo.Length) bytes)"
    }
    
    Remove-Item $tempWav -ErrorAction SilentlyContinue
    $synth2.Dispose()
} catch {
    Write-Host "   ❌ WAV file test failed: $($_.Exception.Message)"
}

Write-Host ""

# Test 4: Alternative playback methods
Write-Host "5. Testing alternative audio playback..."

# Test 4a: Media Player
try {
    Write-Host "   Test 4a: Windows Media Player COM"
    $synth3 = New-Object System.Speech.Synthesis.SpeechSynthesizer
    $tempWav2 = [System.IO.Path]::GetTempFileName() + ".wav"
    $synth3.SetOutputToWaveFile($tempWav2)
    $synth3.Speak("Media Player COM test")
    $synth3.SetOutputToDefaultAudioDevice()
    $synth3.Dispose()
    
    # Try Windows Media Player COM
    $wmp = New-Object -ComObject "WMPlayer.OCX"
    $wmp.URL = $tempWav2
    $wmp.controls.play()
    Start-Sleep -Seconds 3  # Wait for playback
    $wmp.controls.stop()
    [System.Runtime.Interopservices.Marshal]::ReleaseComObject($wmp) | Out-Null
    
    Remove-Item $tempWav2 -ErrorAction SilentlyContinue
    Write-Host "   ✅ Media Player test completed"
} catch {
    Write-Host "   ❌ Media Player test failed: $($_.Exception.Message)"
}

Write-Host ""
Write-Host "=== Diagnostic Summary ==="
Write-Host "Which tests produced beeping vs actual speech?"
Write-Host "This will help identify if the issue is:"
Write-Host "- SAPI/Windows TTS engine problem"
Write-Host "- .NET Speech Synthesis problem" 
Write-Host "- Audio format/codec problem"
Write-Host "- Audio playback method problem"
Write-Host ""
Write-Host "Press Enter to continue..."
Read-Host
