# PowerShell script to test Windows speech recognition directly
Write-Host "=== Windows Speech Recognition Test ===" -ForegroundColor Green
Write-Host

# Test if Speech Recognition is available
try {
    Add-Type -AssemblyName System.Speech
    Write-Host "✓ System.Speech assembly loaded successfully" -ForegroundColor Green
} catch {
    Write-Host "✗ Failed to load System.Speech assembly: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Create speech recognition engine
try {
    $recognizer = New-Object System.Speech.Recognition.SpeechRecognitionEngine
    Write-Host "✓ Speech recognition engine created" -ForegroundColor Green
} catch {
    Write-Host "✗ Failed to create speech recognition engine: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test microphone access
try {
    $recognizer.SetInputToDefaultAudioDevice()
    Write-Host "✓ Default audio device set" -ForegroundColor Green
} catch {
    Write-Host "✗ Failed to set default audio device: $($_.Exception.Message)" -ForegroundColor Red
    $recognizer.Dispose()
    exit 1
}

# Load grammar
try {
    $grammar = New-Object System.Speech.Recognition.DictationGrammar
    $recognizer.LoadGrammar($grammar)
    Write-Host "✓ Dictation grammar loaded" -ForegroundColor Green
} catch {
    Write-Host "✗ Failed to load grammar: $($_.Exception.Message)" -ForegroundColor Red
    $recognizer.Dispose()
    exit 1
}

# Test single recognition
Write-Host "`n=== Testing Single Recognition ===" -ForegroundColor Yellow
Write-Host "Press ENTER to start recognition, then speak clearly..." -ForegroundColor Cyan
Read-Host

try {
    Write-Host "Listening... (10 second timeout)" -ForegroundColor Yellow
    $result = $recognizer.Recognize([System.TimeSpan]::FromSeconds(10))
    
    if ($result -ne $null) {
        Write-Host "✓ Recognition successful!" -ForegroundColor Green
        Write-Host "Text: '$($result.Text)'" -ForegroundColor White
        Write-Host "Confidence: $($result.Confidence)" -ForegroundColor White
    } else {
        Write-Host "✗ No speech detected within timeout period" -ForegroundColor Red
    }
} catch {
    Write-Host "✗ Recognition failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Cleanup
$recognizer.Dispose()
Write-Host "`nTest completed. Speech recognition engine disposed." -ForegroundColor Green
Write-Host "Press ENTER to exit..."
Read-Host
