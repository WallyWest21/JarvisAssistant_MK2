# Kill Jarvis Assistant Processes Script
# This script helps clean up any hanging JarvisAssistant processes that might be locking files

Write-Host "=== Jarvis Assistant Process Cleanup ===" -ForegroundColor Green
Write-Host ""

# Find all JarvisAssistant processes
$processes = Get-Process -Name "*JarvisAssistant*" -ErrorAction SilentlyContinue

if ($processes.Count -eq 0) {
    Write-Host "? No JarvisAssistant processes found running." -ForegroundColor Green
    Write-Host ""
}
else {
    Write-Host "Found $($processes.Count) JarvisAssistant process(es):" -ForegroundColor Yellow
    Write-Host ""
    
    foreach ($process in $processes) {
        Write-Host "  ?? $($process.ProcessName) (PID: $($process.Id))" -ForegroundColor Yellow
        Write-Host "     Started: $($process.StartTime)"
        Write-Host "     Memory: $([Math]::Round($process.WorkingSet64/1MB, 2)) MB"
        Write-Host ""
    }
    
    $response = Read-Host "Do you want to kill these processes? (y/N)"
    
    if ($response -eq 'y' -or $response -eq 'Y') {
        foreach ($process in $processes) {
            try {
                Write-Host "?? Killing $($process.ProcessName) (PID: $($process.Id))..." -ForegroundColor Red
                Stop-Process -Id $process.Id -Force
                Write-Host "? Successfully killed $($process.ProcessName)" -ForegroundColor Green
            }
            catch {
                Write-Host "? Failed to kill $($process.ProcessName): $($_.Exception.Message)" -ForegroundColor Red
            }
        }
        Write-Host ""
        Write-Host "?? Process cleanup completed!" -ForegroundColor Green
    }
    else {
        Write-Host "??  Skipping process cleanup." -ForegroundColor Yellow
    }
}

Write-Host "You can now try building your solution again." -ForegroundColor Cyan
Write-Host ""
Read-Host "Press Enter to exit"