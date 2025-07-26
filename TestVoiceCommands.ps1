# Test voice command processing in the running Jarvis app
# This script will wait for the app to start, then trigger voice commands

Write-Host "Jarvis Voice Command Test"
Write-Host "========================"
Write-Host ""

# Wait for the app to start
Write-Host "Waiting for Jarvis app to start..."
Start-Sleep -Seconds 10

Write-Host "Testing direct Windows TTS first..."

# Test 1: Direct Windows TTS to confirm audio system works
Add-Type -AssemblyName System.Speech
$synth = New-Object -TypeName System.Speech.Synthesis.SpeechSynthesizer
$synth.Speak("Direct Windows TTS test. If you hear this, your audio system is working.")

Write-Host "Direct TTS test completed."
Write-Host ""

# Test 2: Instructions for manual testing
Write-Host "Manual Testing Instructions:"
Write-Host "1. Open the Jarvis Assistant MAUI app"
Write-Host "2. Go to the chat interface"
Write-Host "3. Try typing a voice command such as:"
Write-Host "   - 'what's my status'"
Write-Host "   - 'help'"
Write-Host "   - 'hello jarvis'"
Write-Host "4. Look for audio output and check the logs"
Write-Host ""
Write-Host "Expected behavior:"
Write-Host "- The command should be processed successfully"
Write-Host "- You should hear Jarvis respond with speech"
Write-Host "- Check the debug output/logs for audio playback messages"
Write-Host ""

Write-Host "Press any key when you've tested the voice commands..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

Write-Host ""
Write-Host "Voice command test session completed."
