# Test TTS using the same method as Jarvis with improved sample rate
Write-Host "=== Testing Jarvis TTS Method with 22kHz ==="

Add-Type -AssemblyName System.Speech
$synthesizer = New-Object System.Speech.Synthesis.SpeechSynthesizer

# Create memory stream for audio generation (same as Jarvis)
$memoryStream = New-Object System.IO.MemoryStream

try {
    Write-Host "Configuring synthesizer for 22kHz output..."
    
    # Configure for 22kHz, 16-bit, mono (same as updated Jarvis)
    $audioFormat = New-Object System.Speech.AudioFormat.SpeechAudioFormatInfo(22050, [System.Speech.AudioFormat.AudioBitsPerSample]::Sixteen, [System.Speech.AudioFormat.AudioChannel]::Mono)
    $synthesizer.SetOutputToAudioStream($memoryStream, $audioFormat)
    
    Write-Host "Generating speech to memory stream..."
    $synthesizer.Speak("Hello Sir, this is a test using the same method as Jarvis with improved sample rate.")
    
    # Get the audio data
    $audioData = $memoryStream.ToArray()
    Write-Host "Generated $($audioData.Length) bytes of audio data"
    
    if ($audioData.Length -gt 0) {
        # Create WAV file with proper headers (same method as Jarvis)
        $tempFile = [System.IO.Path]::GetTempFileName()
        $wavFile = [System.IO.Path]::ChangeExtension($tempFile, ".wav")
        
        Write-Host "Creating WAV file: $wavFile"
        
        # Create WAV headers
        $bytesPerSample = 2  # 16-bit = 2 bytes
        $channels = 1
        $sampleRate = 22050
        $blockAlign = $channels * $bytesPerSample
        $byteRate = $sampleRate * $blockAlign
        
        $wavStream = New-Object System.IO.MemoryStream
        $writer = New-Object System.IO.BinaryWriter($wavStream)
        
        # RIFF header
        $writer.Write([System.Text.Encoding]::ASCII.GetBytes("RIFF"))
        $writer.Write([uint32](36 + $audioData.Length))
        $writer.Write([System.Text.Encoding]::ASCII.GetBytes("WAVE"))
        
        # Format chunk
        $writer.Write([System.Text.Encoding]::ASCII.GetBytes("fmt "))
        $writer.Write([uint32]16)  # PCM format chunk size
        $writer.Write([uint16]1)   # PCM format
        $writer.Write([uint16]$channels)
        $writer.Write([uint32]$sampleRate)
        $writer.Write([uint32]$byteRate)
        $writer.Write([uint16]$blockAlign)
        $writer.Write([uint16]16)  # bits per sample
        
        # Data chunk
        $writer.Write([System.Text.Encoding]::ASCII.GetBytes("data"))
        $writer.Write([uint32]$audioData.Length)
        $writer.Write($audioData)
        
        $wavData = $wavStream.ToArray()
        [System.IO.File]::WriteAllBytes($wavFile, $wavData)
        
        $writer.Close()
        $wavStream.Close()
        
        Write-Host "WAV file created successfully"
        Write-Host "Playing audio using SoundPlayer (same as Jarvis)..."
        
        # Play using SoundPlayer (same as Jarvis)
        $player = New-Object System.Media.SoundPlayer($wavFile)
        $player.Load()
        $player.PlaySync()
        
        Write-Host "Audio playback completed"
        
        # Cleanup
        Start-Sleep -Seconds 1
        Remove-Item $tempFile -ErrorAction SilentlyContinue
        Remove-Item $wavFile -ErrorAction SilentlyContinue
        
    } else {
        Write-Host "❌ No audio data generated"
    }
    
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)"
} finally {
    $synthesizer.SetOutputToDefaultAudioDevice()
    $memoryStream.Close()
    $synthesizer.Dispose()
}

Write-Host ""
Write-Host "Test completed. Did you hear speech instead of beeping?"
Read-Host "Press Enter to continue"
