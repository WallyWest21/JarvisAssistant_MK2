# Simple TTS Test - Verify if our TTS integration works
Write-Host "=== Jarvis TTS Service Test ==="

# First verify that basic Windows TTS works
Write-Host "Step 1: Testing direct Windows TTS..."
Add-Type -AssemblyName System.Speech
$synth = New-Object -TypeName System.Speech.Synthesis.SpeechSynthesizer
$synth.Speak("Direct Windows TTS working correctly")
Write-Host "Step 1 completed"

# Test audio file creation and playback manually
Write-Host "Step 2: Testing audio file playback..."

# Create a simple WAV file using Windows TTS
$tempFile = [System.IO.Path]::GetTempFileName()
$wavFile = [System.IO.Path]::ChangeExtension($tempFile, ".wav")

Write-Host "Creating WAV file at: $wavFile"

# Use SpeechSynthesizer to create a WAV file
$synth.SetOutputToWaveFile($wavFile)
$synth.Speak("This is a test of WAV file creation and playback using the same method as Jarvis")
$synth.SetOutputToDefaultAudioDevice()

Write-Host "WAV file created. Testing playback with SoundPlayer..."

# Test playback using the same method as our PlayAudioAsync
$player = New-Object System.Media.SoundPlayer($wavFile)
$player.Load()
Write-Host "Playing audio file..."
$player.PlaySync()
Write-Host "SoundPlayer playback completed"

# Cleanup
Remove-Item $tempFile -ErrorAction SilentlyContinue
Remove-Item $wavFile -ErrorAction SilentlyContinue
Write-Host "Cleanup completed"

Write-Host ""
Write-Host "=== Test Results ==="
Write-Host "If you heard both audio outputs, the TTS system should be working."
Write-Host "Test completed."
