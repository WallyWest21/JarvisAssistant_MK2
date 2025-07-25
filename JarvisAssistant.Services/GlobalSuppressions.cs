using System.Diagnostics.CodeAnalysis;

// Global suppressions for platform-specific APIs
[assembly: SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", 
    Justification = "Platform-specific services are properly guarded with OperatingSystem.IsWindows() checks")]
