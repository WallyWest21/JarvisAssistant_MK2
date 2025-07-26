# Test Windows TTS Directly
Write-Host "Testing Windows Speech API directly..."

# Use .NET Speech Synthesis
Add-Type -AssemblyName System.Speech
$synth = New-Object -TypeName System.Speech.Synthesis.SpeechSynthesizer

# Test basic speech
Write-Host "Speaking test message..."
$synth.Speak("Hello, this is a test of the Windows text to speech system.")

Write-Host "TTS test completed!"
