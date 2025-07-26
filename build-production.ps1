# PowerShell Build Script for Production Deployment
param(
    [string]$Configuration = "Release",
    [string]$Platform = "All",
    [switch]$SkipTests = $false,
    [switch]$CreatePackages = $true
)

Write-Host "🚀 JARVIS Assistant Production Build" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Green
Write-Host "Platform: $Platform" -ForegroundColor Green

$ErrorActionPreference = "Stop"
$startTime = Get-Date

try {
    # Clean previous builds
    Write-Host "🧹 Cleaning previous builds..." -ForegroundColor Yellow
    dotnet clean --configuration $Configuration --verbosity minimal
    
    # Restore packages
    Write-Host "📦 Restoring NuGet packages..." -ForegroundColor Yellow
    dotnet restore --verbosity minimal
    
    # Run tests if not skipped
    if (-not $SkipTests) {
        Write-Host "🧪 Running tests..." -ForegroundColor Yellow
        dotnet test --configuration $Configuration --no-restore --verbosity minimal --logger "console;verbosity=minimal"
        if ($LASTEXITCODE -ne 0) {
            throw "Tests failed"
        }
        Write-Host "✅ All tests passed!" -ForegroundColor Green
    }
    
    # Build and publish for Windows
    if ($Platform -eq "All" -or $Platform -eq "Windows") {
        Write-Host "🏗️ Building Windows package..." -ForegroundColor Yellow
        
        $windowsOutput = "bin\Release\net8.0-windows10.0.19041.0\win-x64\publish"
        
        dotnet publish "JarvisAssistant.MAUI\JarvisAssistant.MAUI.csproj" `
            --configuration $Configuration `
            --framework net8.0-windows10.0.19041.0 `
            --runtime win-x64 `
            --self-contained true `
            --output $windowsOutput `
            --verbosity minimal
            
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Windows build completed: $windowsOutput" -ForegroundColor Green
            
            if ($CreatePackages) {
                # Create portable ZIP
                $zipPath = "Deployment\Windows\JarvisAssistant-Windows-Portable.zip"
                Compress-Archive -Path "$windowsOutput\*" -DestinationPath $zipPath -Force
                Write-Host "📦 Created portable package: $zipPath" -ForegroundColor Green
            }
        }
    }
    
    # Build and publish for Android
    if ($Platform -eq "All" -or $Platform -eq "Android") {
        Write-Host "🏗️ Building Android package..." -ForegroundColor Yellow
        
        dotnet publish "JarvisAssistant.MAUI\JarvisAssistant.MAUI.csproj" `
            --configuration $Configuration `
            --framework net8.0-android `
            --verbosity minimal
            
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Android build completed" -ForegroundColor Green
            
            # Find the APK
            $apkPath = Get-ChildItem -Path "JarvisAssistant.MAUI\bin\$Configuration\net8.0-android" -Filter "*.apk" -Recurse | Select-Object -First 1
            if ($apkPath) {
                $destApk = "Deployment\Android\JarvisAssistant.apk"
                Copy-Item $apkPath.FullName $destApk -Force
                Write-Host "📦 Created Android package: $destApk" -ForegroundColor Green
            }
        }
    }
    
    $duration = (Get-Date) - $startTime
    Write-Host "🎉 Build completed successfully in $($duration.TotalMinutes.ToString('F1')) minutes!" -ForegroundColor Green
    
    # Display package information
    Write-Host "`n📦 Created Packages:" -ForegroundColor Cyan
    Get-ChildItem -Path "Deployment" -Recurse -Include "*.zip", "*.apk", "*.msi" | ForEach-Object {
        $size = [math]::Round($_.Length / 1MB, 1)
        Write-Host "  • $($_.Name) ($size MB)" -ForegroundColor White
    }
    
} catch {
    Write-Host "❌ Build failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "`n🚀 Ready for production deployment!" -ForegroundColor Cyan
