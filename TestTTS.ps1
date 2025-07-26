# Test Windows Text-to-Speech with improved compatibility
Write-Host "Testing Windows TTS with enhanced compatibility..."

# Test using PowerShell's built-in speech synthesis
Add-Type -AssemblyName System.Speech
$synthesizer = New-Object System.Speech.Synthesis.SpeechSynthesizer

Write-Host "Available voices:"
$synthesizer.GetInstalledVoices() | ForEach-Object { 
    Write-Host "  - $($_.VoiceInfo.Name)" 
}

# Configure synthesizer for better compatibility
Write-Host "`nConfiguring synthesizer..."
$synthesizer.Rate = 0      # Normal speed
$synthesizer.Volume = 100  # Full volume

# Try to select a specific voice (Microsoft David is usually reliable)
try {
    $synthesizer.SelectVoice("Microsoft David Desktop")
    Write-Host "Selected Microsoft David Desktop voice"
} catch {
    Write-Host "Using default voice"
}

Write-Host "`nTesting speech output (synchronous)..."
# Use Speak instead of SpeakAsync for better reliability
$synthesizer.Speak("Hello Sir, this is Jarvis. Text to speech is working correctly.")

Write-Host "`nTesting different audio output methods..."

# Test 1: Simple short phrase
Write-Host "Test 1: Simple phrase"
$synthesizer.Speak("Test one")

# Test 2: Set output to default audio device explicitly
Write-Host "Test 2: Explicit default audio device"
$synthesizer.SetOutputToDefaultAudioDevice()
$synthesizer.Speak("Test two with explicit audio device")

# Test 3: Try different voice if available
Write-Host "Test 3: Try different voice"
try {
    $synthesizer.SelectVoice("Microsoft Zira Desktop")
    $synthesizer.Speak("Test three with Zira voice")
} catch {
    Write-Host "Zira voice not available, using default"
    $synthesizer.Speak("Test three with default voice")
}

# Test 4: Create WAV file and play it (same method as Jarvis uses)
Write-Host "Test 4: WAV file generation and playback"
try {
    $tempWav = [System.IO.Path]::GetTempFileName() + ".wav"
    $synthesizer.SetOutputToWaveFile($tempWav)
    $synthesizer.Speak("Test four using WAV file method")
    $synthesizer.SetOutputToDefaultAudioDevice()
    
    Write-Host "Generated WAV file: $tempWav"
    
    # Play the WAV file using SoundPlayer (same as Jarvis)
    $player = New-Object System.Media.SoundPlayer($tempWav)
    $player.Load()
    Write-Host "Playing WAV file..."
    $player.PlaySync()
    
    # Cleanup
    Remove-Item $tempWav -ErrorAction SilentlyContinue
    Write-Host "WAV file test completed"
} catch {
    Write-Host "WAV file test failed: $($_.Exception.Message)"
}

Write-Host "If you can hear the voice, TTS is working. Press any key to exit..."
Read-Host
$synthesizer.Dispose()
