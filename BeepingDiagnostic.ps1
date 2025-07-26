# Quick beeping diagnostic - test each component separately
Write-Host "=== Beeping Issue Diagnostic ==="

# Test 1: Direct speech (no file involved)
Write-Host "Test 1: Direct speech to speakers..."
Add-Type -AssemblyName System.Speech
$synth = New-Object System.Speech.Synthesis.SpeechSynthesizer
$synth.Volume = 75
$synth.Rate = 0
$synth.SetOutputToDefaultAudioDevice()
$synth.Speak("Direct speech test - no files involved")

Write-Host "Did Test 1 work? (y/n)"
$test1Result = Read-Host

if ($test1Result -eq "y") {
    Write-Host "✅ Direct speech works - issue is with file generation/playback"
    
    # Test 2: WAV file generation
    Write-Host "Test 2: Creating WAV file..."
    $wavFile = "C:\temp\test_tts.wav"
    New-Item -ItemType Directory -Path "C:\temp" -Force | Out-Null
    
    $synth.SetOutputToWaveFile($wavFile)
    $synth.Speak("WAV file generation test")
    $synth.SetOutputToDefaultAudioDevice()
    
    if (Test-Path $wavFile) {
        $fileSize = (Get-Item $wavFile).Length
        Write-Host "WAV file created: $fileSize bytes"
        
        if ($fileSize -gt 1000) {
            # Test 3: SoundPlayer playback
            Write-Host "Test 3: Playing WAV with SoundPlayer..."
            $player = New-Object System.Media.SoundPlayer($wavFile)
            $player.Load()
            $player.PlaySync()
            
            Write-Host "Did Test 3 produce beeping? (y/n)"
            $test3Result = Read-Host
            
            if ($test3Result -eq "y") {
                Write-Host "❌ Issue is with SoundPlayer class"
                
                # Test 4: Alternative playback
                Write-Host "Test 4: Alternative playback method..."
                Start-Process -FilePath $wavFile -Wait
                Write-Host "Did Test 4 work? (y/n)"
                $test4Result = Read-Host
                
                if ($test4Result -eq "y") {
                    Write-Host "✅ Solution: Use Process.Start instead of SoundPlayer"
                } else {
                    Write-Host "❌ WAV file itself is corrupted"
                }
            } else {
                Write-Host "✅ SoundPlayer works - issue was elsewhere"
            }
        } else {
            Write-Host "❌ WAV file too small - TTS generation problem"
        }
        
        Remove-Item $wavFile -ErrorAction SilentlyContinue
    } else {
        Write-Host "❌ WAV file not created"
    }
} else {
    Write-Host "❌ Direct speech doesn't work - Windows TTS problem"
    Write-Host "Checking Windows Speech service..."
    
    $speechService = Get-Service -Name "SpeechSrv" -ErrorAction SilentlyContinue
    if ($speechService) {
        Write-Host "Speech Service status: $($speechService.Status)"
    } else {
        Write-Host "Speech Service not found - this may be the issue"
    }
}

$synth.Dispose()
Write-Host ""
Write-Host "Diagnostic completed."
