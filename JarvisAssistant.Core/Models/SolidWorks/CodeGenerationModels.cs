namespace JarvisAssistant.Core.Models.SolidWorks
{
    /// <summary>
    /// Represents a request for VBA code generation with specific requirements.
    /// </summary>
    public class VBAGenerationRequest
    {
        /// <summary>
        /// Gets or sets the purpose or goal of the VBA code.
        /// </summary>
        public string Purpose { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the specific SolidWorks operations to perform.
        /// </summary>
        public List<SolidWorksOperation> Operations { get; set; } = new();

        /// <summary>
        /// Gets or sets the input parameters for the macro.
        /// </summary>
        public List<MacroParameter> InputParameters { get; set; } = new();

        /// <summary>
        /// Gets or sets the output or return values expected.
        /// </summary>
        public List<MacroParameter> OutputParameters { get; set; } = new();

        /// <summary>
        /// Gets or sets whether to include Jarvis-style personality.
        /// </summary>
        public bool IncludeJarvisPersonality { get; set; } = true;

        /// <summary>
        /// Gets or sets the error handling level.
        /// </summary>
        public ErrorHandlingLevel ErrorHandling { get; set; } = ErrorHandlingLevel.Comprehensive;

        /// <summary>
        /// Gets or sets the documentation level.
        /// </summary>
        public DocumentationLevel Documentation { get; set; } = DocumentationLevel.Detailed;
    }

    /// <summary>
    /// Represents a request for creating parametric parts.
    /// </summary>
    public class ParametricPartRequest
    {
        /// <summary>
        /// Gets or sets the part name.
        /// </summary>
        public string PartName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the base feature type.
        /// </summary>
        public FeatureType BaseFeature { get; set; }

        /// <summary>
        /// Gets or sets the parametric dimensions.
        /// </summary>
        public List<ParametricDimension> Dimensions { get; set; } = new();

        /// <summary>
        /// Gets or sets the material to assign.
        /// </summary>
        public string? Material { get; set; }

        /// <summary>
        /// Gets or sets additional features to create.
        /// </summary>
        public List<AdditionalFeature> Features { get; set; } = new();

        /// <summary>
        /// Gets or sets the units system to use.
        /// </summary>
        public UnitSystem Units { get; set; } = UnitSystem.Millimeter;
    }

    /// <summary>
    /// Represents a request for modifying existing features.
    /// </summary>
    public class FeatureModificationRequest
    {
        /// <summary>
        /// Gets or sets the target feature name or ID.
        /// </summary>
        public string TargetFeature { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the modification type.
        /// </summary>
        public ModificationType Type { get; set; }

        /// <summary>
        /// Gets or sets the new parameters for the feature.
        /// </summary>
        public Dictionary<string, object> NewParameters { get; set; } = new();

        /// <summary>
        /// Gets or sets whether to create a backup before modification.
        /// </summary>
        public bool CreateBackup { get; set; } = true;

        /// <summary>
        /// Gets or sets the backup naming convention.
        /// </summary>
        public string? BackupNamingPattern { get; set; }
    }

    /// <summary>
    /// Represents a request for assembly automation.
    /// </summary>
    public class AssemblyAutomationRequest
    {
        /// <summary>
        /// Gets or sets the assembly name.
        /// </summary>
        public string AssemblyName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the components to insert.
        /// </summary>
        public List<ComponentInsertion> Components { get; set; } = new();

        /// <summary>
        /// Gets or sets the mates to create.
        /// </summary>
        public List<MateCreation> Mates { get; set; } = new();

        /// <summary>
        /// Gets or sets the pattern operations to perform.
        /// </summary>
        public List<PatternOperation> Patterns { get; set; } = new();

        /// <summary>
        /// Gets or sets whether to optimize mate order.
        /// </summary>
        public bool OptimizeMateOrder { get; set; } = true;
    }

    /// <summary>
    /// Represents a request for drawing automation.
    /// </summary>
    public class DrawingAutomationRequest
    {
        /// <summary>
        /// Gets or sets the drawing name.
        /// </summary>
        public string DrawingName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the source model reference.
        /// </summary>
        public string SourceModel { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the sheet format to use.
        /// </summary>
        public string? SheetFormat { get; set; }

        /// <summary>
        /// Gets or sets the views to create automatically.
        /// </summary>
        public List<AutoViewCreation> AutoViews { get; set; } = new();

        /// <summary>
        /// Gets or sets the dimensioning strategy.
        /// </summary>
        public DimensioningStrategy DimensionStrategy { get; set; } = DimensioningStrategy.SmartDimensions;

        /// <summary>
        /// Gets or sets whether to include title block automation.
        /// </summary>
        public bool AutomateTitleBlock { get; set; } = true;
    }

    /// <summary>
    /// Represents the result of code generation operations.
    /// </summary>
    public class CodeGenerationResult
    {
        /// <summary>
        /// Gets or sets whether the generation was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the generated VBA code.
        /// </summary>
        public string GeneratedCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the module name for the code.
        /// </summary>
        public string ModuleName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the procedure name.
        /// </summary>
        public string ProcedureName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets any compilation warnings.
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// Gets or sets any generation errors.
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Gets or sets optimization suggestions.
        /// </summary>
        public List<string> OptimizationSuggestions { get; set; } = new();

        /// <summary>
        /// Gets or sets the estimated complexity score.
        /// </summary>
        public int ComplexityScore { get; set; }

        /// <summary>
        /// Gets or sets additional metadata about the generated code.
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Represents a macro template with predefined structure.
    /// </summary>
    public class MacroTemplate
    {
        /// <summary>
        /// Gets or sets the template name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the template description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the template code with placeholders.
        /// </summary>
        public string TemplateCode { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the required parameters for the template.
        /// </summary>
        public List<TemplateParameter> RequiredParameters { get; set; } = new();

        /// <summary>
        /// Gets or sets the optional parameters.
        /// </summary>
        public List<TemplateParameter> OptionalParameters { get; set; } = new();

        /// <summary>
        /// Gets or sets the template category.
        /// </summary>
        public MacroTemplateType Category { get; set; }
    }

    /// <summary>
    /// Represents the result of code validation.
    /// </summary>
    public class CodeValidationResult
    {
        /// <summary>
        /// Gets or sets whether the code is valid.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets syntax errors found.
        /// </summary>
        public List<ValidationError> SyntaxErrors { get; set; } = new();

        /// <summary>
        /// Gets or sets API compliance warnings.
        /// </summary>
        public List<ValidationWarning> ApiWarnings { get; set; } = new();

        /// <summary>
        /// Gets or sets performance recommendations.
        /// </summary>
        public List<string> PerformanceRecommendations { get; set; } = new();

        /// <summary>
        /// Gets or sets the overall quality score.
        /// </summary>
        public int QualityScore { get; set; }
    }

    /// <summary>
    /// Enumeration of SolidWorks operations.
    /// </summary>
    public enum SolidWorksOperation
    {
        CreatePart,
        ModifyFeature,
        InsertComponent,
        CreateMate,
        GenerateDrawing,
        ExportFile,
        SetMaterial,
        CreateConfiguration,
        AddCustomProperty,
        CalculateProperties,
        CreatePattern,
        SuppressFeature,
        UnsuppressFeature
    }

    /// <summary>
    /// Represents a macro parameter definition.
    /// </summary>
    public class MacroParameter
    {
        /// <summary>
        /// Gets or sets the parameter name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the parameter type.
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the parameter description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the default value.
        /// </summary>
        public object? DefaultValue { get; set; }

        /// <summary>
        /// Gets or sets whether the parameter is required.
        /// </summary>
        public bool IsRequired { get; set; }
    }

    /// <summary>
    /// Enumeration of error handling levels.
    /// </summary>
    public enum ErrorHandlingLevel
    {
        None,
        Basic,
        Standard,
        Comprehensive,
        Paranoid
    }

    /// <summary>
    /// Enumeration of documentation levels.
    /// </summary>
    public enum DocumentationLevel
    {
        Minimal,
        Standard,
        Detailed,
        Comprehensive
    }

    /// <summary>
    /// Enumeration of macro template types.
    /// </summary>
    public enum MacroTemplateType
    {
        BasicPart,
        ParametricBox,
        SimpleAssembly,
        DrawingTemplate,
        FeaturePattern,
        MaterialAssignment,
        PropertyManager,
        ExportUtility,
        BatchProcessor,
        QualityChecker
    }

    /// <summary>
    /// Enumeration of code enhancement levels.
    /// </summary>
    public enum CodeEnhancementLevel
    {
        Minimal,
        Standard,
        Enhanced,
        Maximum
    }

    /// <summary>
    /// Represents a parametric dimension.
    /// </summary>
    public class ParametricDimension
    {
        /// <summary>
        /// Gets or sets the dimension name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the dimension value.
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Gets or sets the dimension equation (for driven dimensions).
        /// </summary>
        public string? Equation { get; set; }

        /// <summary>
        /// Gets or sets whether this dimension drives others.
        /// </summary>
        public bool IsDriving { get; set; } = true;
    }

    /// <summary>
    /// Enumeration of feature types.
    /// </summary>
    public enum FeatureType
    {
        Extrude,
        Revolve,
        Sweep,
        Loft,
        Cut,
        Fillet,
        Chamfer,
        Shell,
        Pattern,
        Mirror,
        Hole,
        Thread
    }

    /// <summary>
    /// Represents an additional feature to create.
    /// </summary>
    public class AdditionalFeature
    {
        /// <summary>
        /// Gets or sets the feature type.
        /// </summary>
        public FeatureType Type { get; set; }

        /// <summary>
        /// Gets or sets the feature parameters.
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();

        /// <summary>
        /// Gets or sets the feature order/sequence.
        /// </summary>
        public int Order { get; set; }
    }

    /// <summary>
    /// Enumeration of unit systems.
    /// </summary>
    public enum UnitSystem
    {
        Millimeter,
        Inch,
        Meter,
        Foot
    }

    /// <summary>
    /// Enumeration of modification types.
    /// </summary>
    public enum ModificationType
    {
        EditDefinition,
        ChangeDimension,
        Suppress,
        Unsuppress,
        Delete,
        Rename,
        Reorder,
        ReplaceSketch
    }

    /// <summary>
    /// Represents a component insertion operation.
    /// </summary>
    public class ComponentInsertion
    {
        /// <summary>
        /// Gets or sets the component file path.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the insertion point.
        /// </summary>
        public Point3D Position { get; set; } = new();

        /// <summary>
        /// Gets or sets the component configuration.
        /// </summary>
        public string? Configuration { get; set; }

        /// <summary>
        /// Gets or sets whether to fix the component.
        /// </summary>
        public bool IsFixed { get; set; } = false;
    }

    /// <summary>
    /// Represents a 3D point for positioning.
    /// </summary>
    public class Point3D
    {
        /// <summary>
        /// Gets or sets the X coordinate.
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// Gets or sets the Y coordinate.
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// Gets or sets the Z coordinate.
        /// </summary>
        public double Z { get; set; }
    }

    /// <summary>
    /// Represents a mate creation operation.
    /// </summary>
    public class MateCreation
    {
        /// <summary>
        /// Gets or sets the mate definition.
        /// </summary>
        public MateDefinition Definition { get; set; } = new();

        /// <summary>
        /// Gets or sets the mate priority.
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// Gets or sets whether the mate is critical for assembly function.
        /// </summary>
        public bool IsCritical { get; set; } = false;
    }

    /// <summary>
    /// Represents a pattern operation.
    /// </summary>
    public class PatternOperation
    {
        /// <summary>
        /// Gets or sets the pattern type.
        /// </summary>
        public PatternType Type { get; set; }

        /// <summary>
        /// Gets or sets the seed components.
        /// </summary>
        public List<string> SeedComponents { get; set; } = new();

        /// <summary>
        /// Gets or sets the pattern parameters.
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Enumeration of pattern types.
    /// </summary>
    public enum PatternType
    {
        Linear,
        Circular,
        Sketch,
        Table,
        Curve,
        Mirror
    }

    /// <summary>
    /// Represents automatic view creation settings.
    /// </summary>
    public class AutoViewCreation
    {
        /// <summary>
        /// Gets or sets the view type to create.
        /// </summary>
        public DrawingViewType ViewType { get; set; }

        /// <summary>
        /// Gets or sets whether to auto-arrange views.
        /// </summary>
        public bool AutoArrange { get; set; } = true;

        /// <summary>
        /// Gets or sets the view scale.
        /// </summary>
        public double Scale { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets whether to include dimensions.
        /// </summary>
        public bool IncludeDimensions { get; set; } = false;
    }

    /// <summary>
    /// Enumeration of dimensioning strategies.
    /// </summary>
    public enum DimensioningStrategy
    {
        None,
        BasicDimensions,
        SmartDimensions,
        CompleteDimensions,
        CustomDimensions
    }

    /// <summary>
    /// Represents a template parameter.
    /// </summary>
    public class TemplateParameter
    {
        /// <summary>
        /// Gets or sets the parameter name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the parameter type.
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the parameter description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the placeholder in the template.
        /// </summary>
        public string Placeholder { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents a validation error.
    /// </summary>
    public class ValidationError
    {
        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the line number where the error occurs.
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Gets or sets the column number where the error occurs.
        /// </summary>
        public int ColumnNumber { get; set; }

        /// <summary>
        /// Gets or sets the error severity.
        /// </summary>
        public ValidationSeverity Severity { get; set; }
    }

    /// <summary>
    /// Represents a validation warning.
    /// </summary>
    public class ValidationWarning
    {
        /// <summary>
        /// Gets or sets the warning message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the warning category.
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the line number associated with the warning.
        /// </summary>
        public int LineNumber { get; set; }
    }

    /// <summary>
    /// Enumeration of validation severities.
    /// </summary>
    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }
}
