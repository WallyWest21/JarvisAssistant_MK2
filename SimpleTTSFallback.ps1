# Simple TTS test that often works when others fail
Write-Host "=== Simple TTS Fallback Test ==="

# Method 1: PowerShell's built-in Add-Type with minimal config
Write-Host "Method 1: Minimal PowerShell TTS..."
try {
    Add-Type -AssemblyName System.Speech
    $speak = New-Object System.Speech.Synthesis.SpeechSynthesizer
    
    # Absolutely minimal configuration
    $speak.Volume = 50  # Lower volume in case it's a volume issue
    $speak.Rate = -2    # Slower speech
    
    Write-Host "Speaking with minimal config..."
    $speak.Speak("Minimal configuration test")
    $speak.Dispose()
    Write-Host "✅ Minimal config completed"
} catch {
    Write-Host "❌ Minimal config failed: $($_.Exception.Message)"
}

Write-Host ""

# Method 2: cmd.exe SAPI (completely different path)
Write-Host "Method 2: CMD SAPI via PowerShell..."
try {
    $cmd = 'powershell -Command "Add-Type -AssemblyName System.Speech; (New-Object System.Speech.Synthesis.SpeechSynthesizer).Speak(''CMD SAPI test'')"'
    Start-Process -FilePath "cmd.exe" -ArgumentList "/c", $cmd -Wait -WindowStyle Hidden
    Write-Host "✅ CMD SAPI completed"
} catch {
    Write-Host "❌ CMD SAPI failed: $($_.Exception.Message)"
}

Write-Host ""

# Method 3: VBScript SAPI (old but reliable)
Write-Host "Method 3: VBScript SAPI..."
try {
    $vbsCode = @'
Set objVoice = CreateObject("SAPI.SpVoice")
objVoice.Volume = 50
objVoice.Rate = 0
objVoice.Speak "VBScript SAPI test"
'@
    $vbsFile = [System.IO.Path]::GetTempFileName() + ".vbs"
    Set-Content -Path $vbsFile -Value $vbsCode
    Start-Process -FilePath "cscript.exe" -ArgumentList "//nologo", $vbsFile -Wait -WindowStyle Hidden
    Remove-Item $vbsFile -ErrorAction SilentlyContinue
    Write-Host "✅ VBScript SAPI completed"
} catch {
    Write-Host "❌ VBScript SAPI failed: $($_.Exception.Message)"
}

Write-Host ""

# Method 4: Check if it's a codec issue with specific WAV format
Write-Host "Method 4: Testing specific WAV formats..."
try {
    Add-Type -AssemblyName System.Speech
    $synth = New-Object System.Speech.Synthesis.SpeechSynthesizer
    
    # Test multiple common sample rates
    $sampleRates = @(8000, 11025, 16000, 22050, 44100)
    
    foreach ($rate in $sampleRates) {
        try {
            Write-Host "   Testing $rate Hz..."
            $tempWav = [System.IO.Path]::GetTempFileName() + ".wav"
            
            # Create specific audio format
            $audioFormat = New-Object System.Speech.AudioFormat.SpeechAudioFormatInfo(
                $rate, 
                [System.Speech.AudioFormat.AudioBitsPerSample]::Sixteen, 
                [System.Speech.AudioFormat.AudioChannel]::Mono
            )
            
            $synth.SetOutputToAudioStream([System.IO.File]::Create($tempWav), $audioFormat)
            $synth.Speak("Testing $rate hertz sample rate")
            $synth.SetOutputToDefaultAudioDevice()
            
            # Check file size
            $fileSize = (Get-Item $tempWav).Length
            Write-Host "   Generated: $fileSize bytes"
            
            if ($fileSize -gt 1000) {
                # Try to play it
                $player = New-Object System.Media.SoundPlayer($tempWav)
                $player.Load()
                $player.PlaySync()
                Write-Host "   ✅ $rate Hz worked!"
                Remove-Item $tempWav -ErrorAction SilentlyContinue
                break  # Stop at first working rate
            } else {
                Write-Host "   ❌ $rate Hz - file too small"
            }
            
            Remove-Item $tempWav -ErrorAction SilentlyContinue
        } catch {
            Write-Host "   ❌ $rate Hz failed: $($_.Exception.Message)"
        }
    }
    
    $synth.Dispose()
} catch {
    Write-Host "❌ WAV format test failed: $($_.Exception.Message)"
}

Write-Host ""
Write-Host "Results: Which method(s) produced actual speech vs beeping?"
Read-Host "Press Enter to finish"
