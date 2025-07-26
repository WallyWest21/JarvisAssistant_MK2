# Test the TTS integration for chat responses
Write-Host "=== Testing Jarvis Chat TTS Integration ==="
Write-Host ""

# Wait for app to start
Write-Host "Waiting for Jarvis app to start..."
Start-Sleep -Seconds 15

Write-Host "✅ App should now be running"
Write-Host ""
Write-Host "=== Test Instructions ==="
Write-Host "1. Make sure the Voice Mode checkbox is CHECKED (enabled)"
Write-Host "2. Type a message in the chat (e.g., 'hello' or 'what can you do')"
Write-Host "3. Press Send or Enter"
Write-Host "4. Jarvis should respond with text AND speech"
Write-Host ""
Write-Host "Expected behavior:"
Write-Host "- Jarvis displays text response ✅"
Write-Host "- Jarvis speaks the response aloud 🔊"
Write-Host "- Check logs for 'Generating speech for chat response' messages"
Write-Host ""

# Test direct TTS to confirm audio system works
Write-Host "Testing direct TTS first to confirm audio system..."
Add-Type -AssemblyName System.Speech
$synth = New-Object -TypeName System.Speech.Synthesis.SpeechSynthesizer
$synth.Speak("Direct TTS test. If you hear this, your audio system is working correctly.")

Write-Host ""
Write-Host "If you heard the direct TTS but Jarvis chat doesn't speak:"
Write-Host "- Check that Voice Mode is enabled in the app"
Write-Host "- Look for TTS error messages in the debug output"
Write-Host "- Verify the voice service is properly registered"
Write-Host ""
Write-Host "Press any key when you've tested the chat TTS..."
Read-Host
