using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models.SolidWorks;
using JarvisAssistant.Core.Models;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace JarvisAssistant.Services
{
    /// <summary>
    /// Provides SolidWorks integration capabilities including COM interop, file analysis,
    /// and VBA macro generation with Jarvis personality.
    /// </summary>
    public class SolidWorksService : ISolidWorksService, IDisposable
    {
        private readonly ILogger<SolidWorksService> _logger;
        private readonly ISolidWorksCodeGenerator _codeGenerator;
        private readonly ILLMService _llmService;
        
        private dynamic? _swApp;
        private bool _isConnected;
        private string? _solidWorksVersion;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the SolidWorksService class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="codeGenerator">The code generator service.</param>
        /// <param name="llmService">The LLM service for AI assistance.</param>
        public SolidWorksService(
            ILogger<SolidWorksService> logger, 
            ISolidWorksCodeGenerator codeGenerator,
            ILLMService llmService)
        {
            _logger = logger;
            _codeGenerator = codeGenerator;
            _llmService = llmService;
            _isConnected = false;
        }

        /// <inheritdoc/>
        public bool IsConnected => _isConnected;

        /// <inheritdoc/>
        public string? SolidWorksVersion => _solidWorksVersion;

        /// <inheritdoc/>
        public event EventHandler<SolidWorksConnectionEventArgs>? ConnectionStatusChanged;

        /// <inheritdoc/>
        public async Task<bool> ConnectToSolidWorksAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Sir, attempting to establish connection with SolidWorks...");

                // Try to connect to existing SolidWorks instance
                Type? swType = Type.GetTypeFromProgID("SldWorks.Application");
                if (swType == null)
                {
                    _logger.LogWarning("SolidWorks COM interface not found. Please ensure SolidWorks is installed.");
                    return false;
                }

                try
                {
                    // Use alternative approach for .NET 8 compatibility
                    _swApp = GetActiveObject("SldWorks.Application");
                    _logger.LogInformation("Connected to existing SolidWorks instance, sir.");
                }
                catch (COMException)
                {
                    _logger.LogInformation("No active SolidWorks instance found. Starting SolidWorks...");
                    return await StartSolidWorksAsync(false, cancellationToken);
                }

                if (_swApp != null)
                {
                    _solidWorksVersion = _swApp.RevisionNumber();
                    _isConnected = true;
                    
                    _logger.LogInformation("Successfully connected to SolidWorks {Version}, sir. Ready for your engineering commands.", _solidWorksVersion);
                    
                    OnConnectionStatusChanged(new SolidWorksConnectionEventArgs 
                    { 
                        IsConnected = true, 
                        Version = _solidWorksVersion,
                        Message = "Connected to existing SolidWorks instance"
                    });
                    
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to SolidWorks, sir. The connection attempt was unsuccessful.");
                return false;
            }
        }

        /// <summary>
        /// Gets an active COM object from the Running Object Table.
        /// This is a replacement for Marshal.GetActiveObject in .NET 8.
        /// </summary>
        /// <param name="progId">The ProgID of the COM object.</param>
        /// <returns>The active COM object instance.</returns>
        private static object GetActiveObject(string progId)
        {
            // Convert ProgID to CLSID
            Guid clsid = Type.GetTypeFromProgID(progId)?.GUID ?? throw new ArgumentException($"Invalid ProgID: {progId}");
            
            // Get the Running Object Table
            int hr = GetRunningObjectTable(0, out IRunningObjectTable rot);
            if (hr < 0)
                Marshal.ThrowExceptionForHR(hr);

            try
            {
                // Create moniker from ProgID
                hr = CreateItemMoniker("!", progId, out IMoniker moniker);
                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);

                try
                {
                    // Try to get object from ROT
                    hr = rot.GetObject(moniker, out object obj);
                    if (hr < 0)
                    {
                        // If not found in ROT, try alternative approach
                        return Activator.CreateInstance(Type.GetTypeFromCLSID(clsid))!;
                    }
                    return obj;
                }
                finally
                {
                    Marshal.ReleaseComObject(moniker);
                }
            }
            finally
            {
                Marshal.ReleaseComObject(rot);
            }
        }

        [DllImport("ole32.dll")]
        private static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable prot);

        [DllImport("ole32.dll")]
        private static extern int CreateItemMoniker([MarshalAs(UnmanagedType.LPWStr)] string lpszDelim,
            [MarshalAs(UnmanagedType.LPWStr)] string lpszItem, out IMoniker ppmk);

        [ComImport]
        [Guid("00000102-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IRunningObjectTable
        {
            int Register(int grfFlags, [MarshalAs(UnmanagedType.IUnknown)] object punkObject, IMoniker pmkObjectName);
            int Revoke(int dwRegister);
            int IsRunning(IMoniker pmkObjectName);
            int GetObject(IMoniker pmkObjectName, [MarshalAs(UnmanagedType.IUnknown)] out object ppunkObject);
            int NoteChangeTime(int dwRegister, ref long pfiletime);
            int GetTimeOfLastChange(IMoniker pmkObjectName, out long pfiletime);
            int EnumRunning(out IEnumMoniker ppenumMoniker);
        }

        [ComImport]
        [Guid("0000000f-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMoniker
        {
            // Simplified interface - only what we need
        }

        [ComImport]
        [Guid("00000102-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IEnumMoniker
        {
            // Simplified interface - not used but needed for completeness
        }

        /// <inheritdoc/>
        public async Task<bool> StartSolidWorksAsync(bool silent = false, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Sir, initiating SolidWorks application startup sequence...");

                Type? swType = Type.GetTypeFromProgID("SldWorks.Application");
                if (swType == null)
                {
                    _logger.LogError("SolidWorks is not installed on this system, sir.");
                    return false;
                }

                _swApp = Activator.CreateInstance(swType);
                if (_swApp == null)
                {
                    _logger.LogError("Failed to create SolidWorks instance, sir.");
                    return false;
                }

                // Set visibility based on silent parameter
                _swApp.Visible = !silent;
                
                // Wait for SolidWorks to fully initialize
                await Task.Delay(3000, cancellationToken);

                _solidWorksVersion = _swApp.RevisionNumber();
                _isConnected = true;

                var startupMessage = silent 
                    ? "SolidWorks started in background mode, sir. Ready for automated operations."
                    : "SolidWorks is now ready for your creative engineering endeavors, sir.";

                _logger.LogInformation("SolidWorks {Version} started successfully. {Message}", _solidWorksVersion, startupMessage);

                OnConnectionStatusChanged(new SolidWorksConnectionEventArgs 
                { 
                    IsConnected = true, 
                    Version = _solidWorksVersion,
                    Message = "SolidWorks started successfully"
                });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start SolidWorks, sir. The startup sequence encountered an error.");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task DisconnectAsync()
        {
            try
            {
                if (_swApp != null && _isConnected)
                {
                    _logger.LogInformation("Sir, gracefully disconnecting from SolidWorks...");
                    
                    // Close SolidWorks if we started it
                    try
                    {
                        _swApp.ExitApp();
                    }
                    catch (COMException ex)
                    {
                        _logger.LogWarning(ex, "SolidWorks may have already been closed by the user.");
                    }

                    Marshal.ReleaseComObject(_swApp);
                    _swApp = null;
                }

                _isConnected = false;
                _solidWorksVersion = null;

                _logger.LogInformation("Successfully disconnected from SolidWorks, sir. Session terminated gracefully.");

                OnConnectionStatusChanged(new SolidWorksConnectionEventArgs 
                { 
                    IsConnected = false, 
                    Version = null,
                    Message = "Disconnected from SolidWorks"
                });

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during SolidWorks disconnection, sir.");
            }
        }

        /// <inheritdoc/>
        public async Task<MacroGenerationResponse> GeneratePartMacroAsync(MacroGenerationRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Sir, I shall craft a parametric part macro with my finest attention to detail...");

                // Convert the request to our code generator format
                var vbaRequest = new VBAGenerationRequest
                {
                    Purpose = $"Create {request.MacroName}: {request.Description}",
                    Operations = request.Type switch
                    {
                        MacroType.PartCreation => new List<SolidWorksOperation> { SolidWorksOperation.CreatePart },
                        MacroType.FeatureModification => new List<SolidWorksOperation> { SolidWorksOperation.ModifyFeature },
                        _ => new List<SolidWorksOperation> { SolidWorksOperation.CreatePart }
                    },
                    IncludeJarvisPersonality = request.IncludeJarvisComments,
                    ErrorHandling = request.IncludeErrorHandling ? ErrorHandlingLevel.Comprehensive : ErrorHandlingLevel.Basic
                };

                // Add parameters from the request
                foreach (var param in request.Parameters)
                {
                    vbaRequest.InputParameters.Add(new MacroParameter
                    {
                        Name = param.Key,
                        Type = param.Value?.GetType().Name ?? "Object",
                        DefaultValue = param.Value,
                        IsRequired = true
                    });
                }

                var result = await _codeGenerator.GenerateVBAMacroAsync(vbaRequest, cancellationToken);

                return new MacroGenerationResponse
                {
                    Success = result.Success,
                    GeneratedCode = result.GeneratedCode,
                    Warnings = result.Warnings,
                    Errors = result.Errors,
                    Suggestions = result.OptimizationSuggestions
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "I regret to inform you, sir, that the macro generation encountered an unexpected complication.");
                
                return new MacroGenerationResponse
                {
                    Success = false,
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        /// <inheritdoc/>
        public async Task<MacroGenerationResponse> GenerateAssemblyMacroAsync(AssemblyMacroRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Sir, I shall orchestrate an assembly automation macro of considerable sophistication...");

                var assemblyRequest = new AssemblyAutomationRequest
                {
                    AssemblyName = request.MacroName,
                    Components = request.ComponentFiles.Select(file => new ComponentInsertion
                    {
                        FilePath = file,
                        Position = new Point3D { X = 0, Y = 0, Z = 0 },
                        IsFixed = false
                    }).ToList(),
                    Mates = request.Mates.Select(mate => new MateCreation
                    {
                        Definition = mate,
                        Priority = 0,
                        IsCritical = true
                    }).ToList()
                };

                var result = await _codeGenerator.GenerateAssemblyAutomationAsync(assemblyRequest, cancellationToken);

                return new MacroGenerationResponse
                {
                    Success = result.Success,
                    GeneratedCode = result.GeneratedCode,
                    Warnings = result.Warnings,
                    Errors = result.Errors,
                    Suggestions = result.OptimizationSuggestions
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "I must apologize, sir. The assembly macro generation has encountered an unforeseen difficulty.");
                
                return new MacroGenerationResponse
                {
                    Success = false,
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        /// <inheritdoc/>
        public async Task<MacroGenerationResponse> GenerateDrawingMacroAsync(DrawingMacroRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Sir, I shall prepare a drawing automation macro with exquisite attention to documentation standards...");

                var drawingRequest = new DrawingAutomationRequest
                {
                    DrawingName = request.MacroName,
                    SourceModel = request.SourceModelFile ?? "",
                    SheetFormat = request.DrawingTemplate,
                    AutoViews = request.Views.Select(view => new AutoViewCreation
                    {
                        ViewType = view.Type,
                        Scale = view.Scale,
                        AutoArrange = true,
                        IncludeDimensions = false
                    }).ToList(),
                    DimensionStrategy = DimensioningStrategy.SmartDimensions,
                    AutomateTitleBlock = true
                };

                var result = await _codeGenerator.GenerateDrawingAutomationAsync(drawingRequest, cancellationToken);

                return new MacroGenerationResponse
                {
                    Success = result.Success,
                    GeneratedCode = result.GeneratedCode,
                    Warnings = result.Warnings,
                    Errors = result.Errors,
                    Suggestions = result.OptimizationSuggestions
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "I regret to report, sir, that the drawing macro generation has encountered a complication.");
                
                return new MacroGenerationResponse
                {
                    Success = false,
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        /// <inheritdoc/>
        public async Task<PartAnalysisResult> AnalyzePartFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            if (!_isConnected || _swApp == null)
            {
                _logger.LogWarning("Sir, I must establish a connection to SolidWorks before I can analyze the part file.");
                return new PartAnalysisResult 
                { 
                    Success = false, 
                    Errors = new List<string> { "Not connected to SolidWorks" }
                };
            }

            try
            {
                _logger.LogInformation("Sir, I shall conduct a comprehensive analysis of the part file: {FilePath}", filePath);

                if (!File.Exists(filePath))
                {
                    return new PartAnalysisResult 
                    { 
                        Success = false, 
                        Errors = new List<string> { "File not found" }
                    };
                }

                // Open the part file
                int errors = 0, warnings = 0;
                dynamic doc = _swApp.OpenDoc6(filePath, 1, 0, "", ref errors, ref warnings); // 1 = swDocumentTypes_e.swDocPART

                if (doc == null || errors != 0)
                {
                    return new PartAnalysisResult 
                    { 
                        Success = false, 
                        Errors = new List<string> { $"Failed to open part file. Errors: {errors}, Warnings: {warnings}" }
                    };
                }

                var result = new PartAnalysisResult
                {
                    Success = true,
                    FilePath = filePath,
                    PartName = Path.GetFileNameWithoutExtension(filePath)
                };

                // Analyze mass properties
                await AnalyzeMassProperties(doc, result);

                // Analyze features
                await AnalyzeFeatures(doc, result);

                // Generate optimization suggestions using AI
                await GenerateOptimizationSuggestions(result, cancellationToken);

                // Generate quality assessment
                await AssessQuality(result);

                // Generate manufacturing considerations
                await AnalyzeManufacturing(result);

                _logger.LogInformation("Sir, the analysis is complete. Quality score: {Score}/100", result.QualityScore);

                // Close the document
                _swApp.CloseDoc(filePath);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "I encountered an unexpected difficulty while analyzing the part file, sir.");
                return new PartAnalysisResult 
                { 
                    Success = false, 
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ExecuteMacroAsync(string macroPath, string moduleName, string procedureName, CancellationToken cancellationToken = default)
        {
            if (!_isConnected || _swApp == null)
            {
                _logger.LogWarning("Sir, I require a connection to SolidWorks to execute the macro.");
                return false;
            }

            try
            {
                _logger.LogInformation("Sir, executing macro: {Procedure} from {Module} in {Path}", procedureName, moduleName, macroPath);

                int errors = 0;
                int result = _swApp.RunMacro2(macroPath, moduleName, procedureName, 
                    0, // options
                    ref errors);

                if (result == 1 && errors == 0) // Success
                {
                    _logger.LogInformation("Macro executed successfully, sir. The operation completed without incident.");
                    return true;
                }
                else
                {
                    _logger.LogWarning("Macro execution completed with issues, sir. Result: {Result}, Errors: {Errors}", result, errors);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "I regret to inform you, sir, that the macro execution encountered an error.");
                return false;
            }
        }

        private async Task AnalyzeMassProperties(dynamic doc, PartAnalysisResult result)
        {
            try
            {
                // Get mass properties
                dynamic massProps = doc.Extension.CreateMassProperty();
                if (massProps != null)
                {
                    massProps.UseSystemUnits = true;
                    
                    var centerOfMass = (double[])massProps.CenterOfMass;
                    var principalMoments = (double[])massProps.PrincipalMomentsOfInertia;
                    
                    result.MassProperties = new MassProperties
                    {
                        Mass = massProps.Mass,
                        Volume = massProps.Volume,
                        SurfaceArea = massProps.SurfaceArea,
                        CenterOfMass = new Point3D 
                        { 
                            X = centerOfMass[0], 
                            Y = centerOfMass[1], 
                            Z = centerOfMass[2] 
                        },
                        PrincipalMoments = new MomentOfInertia
                        {
                            Ixx = principalMoments[0],
                            Iyy = principalMoments[1],
                            Izz = principalMoments[2]
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not analyze mass properties");
            }

            await Task.CompletedTask;
        }

        private async Task AnalyzeFeatures(dynamic doc, PartAnalysisResult result)
        {
            try
            {
                dynamic featureManager = doc.FeatureManager;
                dynamic feature = doc.FirstFeature();
                
                while (feature != null)
                {
                    var featureAnalysis = new FeatureAnalysis
                    {
                        Name = feature.Name,
                        Type = feature.GetTypeName2(),
                        IsSuppressed = feature.IsSuppressed(),
                        ComplexityScore = CalculateFeatureComplexity(feature),
                        RebuildTime = 0 // Would need performance profiling
                    };

                    // Analyze feature-specific issues
                    AnalyzeFeatureIssues(feature, featureAnalysis);

                    result.Features.Add(featureAnalysis);
                    feature = feature.GetNextFeature();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not analyze features completely");
            }

            await Task.CompletedTask;
        }

        private int CalculateFeatureComplexity(dynamic feature)
        {
            // Simple complexity scoring based on feature type
            string typeName = feature.GetTypeName2();
            return typeName switch
            {
                "ICE" => 1, // Simple extrude
                "Boss" => 2,
                "Cut" => 2,
                "Fillet" => 3,
                "Chamfer" => 2,
                "Pattern" => 4,
                "Sweep" => 5,
                "Loft" => 6,
                "Boundary" => 7,
                _ => 2
            };
        }

        private void AnalyzeFeatureIssues(dynamic feature, FeatureAnalysis analysis)
        {
            try
            {
                // Check for common issues
                if (feature.IsSuppressed())
                {
                    analysis.Issues.Add("Feature is currently suppressed");
                }

                // Check for errors in feature
                if (feature.GetErrorCode2() != 0)
                {
                    analysis.Issues.Add($"Feature has error code: {feature.GetErrorCode2()}");
                }

                // Add suggestions based on feature type
                string typeName = feature.GetTypeName2();
                switch (typeName)
                {
                    case "Fillet":
                        analysis.Suggestions.Add("Consider using variable radius fillets for better manufacturability");
                        break;
                    case "Pattern":
                        analysis.Suggestions.Add("Verify pattern spacing for manufacturing constraints");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not complete feature issue analysis for {FeatureName}", analysis.Name);
            }
        }

        private async Task GenerateOptimizationSuggestions(PartAnalysisResult result, CancellationToken cancellationToken)
        {
            try
            {
                // Use AI to generate intelligent optimization suggestions
                var request = new ChatRequest
                {
                    Message = $"As Jarvis, analyze this SolidWorks part and provide optimization suggestions:\n" +
                             $"Part: {result.PartName}\n" +
                             $"Features: {result.Features.Count}\n" +
                             $"Mass: {result.MassProperties.Mass:F2} kg\n" +
                             $"Volume: {result.MassProperties.Volume:F2} mm³\n" +
                             $"Complex features: {result.Features.Count(f => f.ComplexityScore > 4)}\n" +
                             $"Suppressed features: {result.Features.Count(f => f.IsSuppressed)}\n\n" +
                             "Provide 3-5 specific optimization suggestions with Jarvis personality.",
                    Type = "user"
                };

                var response = await _llmService.SendMessageAsync(request, cancellationToken);
                
                if (!string.IsNullOrEmpty(response.Message))
                {
                    // Parse the AI response into structured suggestions
                    ParseOptimizationSuggestions(response.Message, result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not generate AI-powered optimization suggestions");
                
                // Fallback to rule-based suggestions
                GenerateRuleBasedSuggestions(result);
            }
        }

        private void ParseOptimizationSuggestions(string aiResponse, PartAnalysisResult result)
        {
            // Simple parsing of AI response - in a real implementation, this would be more sophisticated
            var suggestions = aiResponse.Split('\n')
                .Where(line => line.Trim().Length > 10)
                .Take(5)
                .Select(suggestion => new OptimizationSuggestion
                {
                    Title = "AI Optimization Suggestion",
                    Description = suggestion.Trim(),
                    Category = OptimizationCategory.Performance,
                    Impact = ImpactLevel.Medium,
                    Difficulty = DifficultyLevel.Medium
                }).ToList();

            result.OptimizationSuggestions.AddRange(suggestions);
        }

        private void GenerateRuleBasedSuggestions(PartAnalysisResult result)
        {
            // Generate basic rule-based suggestions as fallback
            if (result.Features.Count(f => f.IsSuppressed) > 0)
            {
                result.OptimizationSuggestions.Add(new OptimizationSuggestion
                {
                    Title = "Remove Suppressed Features",
                    Description = "Consider removing permanently suppressed features to improve model clarity",
                    Category = OptimizationCategory.FeatureTree,
                    Impact = ImpactLevel.Low,
                    Difficulty = DifficultyLevel.Easy
                });
            }

            if (result.Features.Count > 50)
            {
                result.OptimizationSuggestions.Add(new OptimizationSuggestion
                {
                    Title = "Simplify Feature Tree",
                    Description = "Complex feature tree detected. Consider consolidating similar features",
                    Category = OptimizationCategory.Performance,
                    Impact = ImpactLevel.Medium,
                    Difficulty = DifficultyLevel.Medium
                });
            }
        }

        private async Task AssessQuality(PartAnalysisResult result)
        {
            int score = 100;

            // Deduct points for issues
            score -= result.Features.Count(f => f.IsSuppressed) * 2;
            score -= result.Features.Count(f => f.Issues.Any()) * 5;
            score -= Math.Max(0, result.Features.Count - 30) * 1; // Penalize overly complex models

            // Add points for good practices
            if (result.Features.Any(f => f.Type == "Fillet"))
                score += 5; // Good for manufacturability

            result.QualityScore = Math.Max(0, Math.Min(100, score));

            await Task.CompletedTask;
        }

        private async Task AnalyzeManufacturing(PartAnalysisResult result)
        {
            // Add basic manufacturing considerations
            result.ManufacturingConsiderations.Add(new ManufacturingConsideration
            {
                Title = "Machining Considerations",
                Description = "Review sharp internal corners and deep pockets for machining accessibility",
                Process = ManufacturingProcess.Machining,
                Importance = ImportanceLevel.Important,
                CostImpact = CostImpact.Medium
            });

            if (result.Features.Any(f => f.Type.Contains("Fillet")))
            {
                result.ManufacturingConsiderations.Add(new ManufacturingConsideration
                {
                    Title = "Fillet Radii",
                    Description = "Verify fillet radii match available tooling for cost-effective manufacturing",
                    Process = ManufacturingProcess.Machining,
                    Importance = ImportanceLevel.Recommended,
                    CostImpact = CostImpact.Low
                });
            }

            await Task.CompletedTask;
        }

        private void OnConnectionStatusChanged(SolidWorksConnectionEventArgs e)
        {
            ConnectionStatusChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Releases all resources used by the SolidWorksService.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the SolidWorksService and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    _ = DisconnectAsync();
                }

                _disposed = true;
            }
        }
    }
}
