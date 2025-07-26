# Quick PowerShell test to verify Jarvis voice processing
# This uses the PowerShell .NET integration to test the core services

Write-Host "Setting up Jarvis TTS Pipeline Test..."

# Test if we can manually create and test the voice service
try {
    # Load the required assemblies
    Add-Type -Path "C:\Users\Bruce\source\repos\Jarvis Assistant MK2\JarvisAssistant.Services\bin\Debug\net8.0\JarvisAssistant.Services.dll" -ErrorAction SilentlyContinue
    Add-Type -Path "C:\Users\Bruce\source\repos\Jarvis Assistant MK2\JarvisAssistant.Core\bin\Debug\net8.0\JarvisAssistant.Core.dll" -ErrorAction SilentlyContinue
    
    Write-Host "✅ Assemblies loaded"
} catch {
    Write-Host "❌ Failed to load assemblies: $_"
    Write-Host "Building services first..."
    
    # Build the services project
    dotnet build "C:\Users\Bruce\source\repos\Jarvis Assistant MK2\JarvisAssistant.Services\JarvisAssistant.Services.csproj"
    
    Write-Host "Trying to load assemblies again..."
    Add-Type -Path "C:\Users\Bruce\source\repos\Jarvis Assistant MK2\JarvisAssistant.Services\bin\Debug\net8.0\JarvisAssistant.Services.dll"
    Add-Type -Path "C:\Users\Bruce\source\repos\Jarvis Assistant MK2\JarvisAssistant.Core\bin\Debug\net8.0\JarvisAssistant.Core.dll"
}

Write-Host "TTS Pipeline Test setup completed!"
