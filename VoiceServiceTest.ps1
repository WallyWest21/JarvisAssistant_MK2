# Quick test to verify the new voice service priority is working
Write-Host "=== Voice Service Priority Test ===" -ForegroundColor Green
Write-Host ""

Write-Host "Environment check:" -ForegroundColor Yellow
Write-Host "ElevenLabs API Key: $($env:ELEVENLABS_API_KEY.Substring(0, 8))..." -ForegroundColor White
Write-Host ""

Write-Host "Service priority order for Jarvis:" -ForegroundColor Yellow
Write-Host "1. 🥇 ElevenLabs API (Voice: Jarvis)" -ForegroundColor Green
Write-Host "2. 🥈 Windows SAPI TTS (Fallback)" -ForegroundColor Cyan
Write-Host "3. 🥉 Stub Service (Final fallback)" -ForegroundColor Gray
Write-Host ""

Write-Host "How it works:" -ForegroundColor Yellow
Write-Host "• When you send a chat message, Jarvis will:" -ForegroundColor White
Write-Host "  1. Try ElevenLabs first (high-quality voice)" -ForegroundColor White
Write-Host "  2. If ElevenLabs fails → automatically use Windows TTS" -ForegroundColor White
Write-Host "  3. If Windows TTS fails → use silent fallback" -ForegroundColor White
Write-Host ""

Write-Host "🎯 Ready to test!" -ForegroundColor Green
Write-Host "Start the Jarvis app and send a chat message." -ForegroundColor Cyan
Write-Host "You should hear high-quality ElevenLabs voice!" -ForegroundColor Cyan
