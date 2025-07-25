namespace JarvisAssistant.Core.Models.SolidWorks
{
    /// <summary>
    /// Represents a request for generating VBA macros for SolidWorks parts.
    /// </summary>
    public class MacroGenerationRequest
    {
        /// <summary>
        /// Gets or sets the type of macro to generate.
        /// </summary>
        public MacroType Type { get; set; }

        /// <summary>
        /// Gets or sets the name of the macro to generate.
        /// </summary>
        public string MacroName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of what the macro should accomplish.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the parameters for the macro.
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();

        /// <summary>
        /// Gets or sets whether to include Jarvis-style comments.
        /// </summary>
        public bool IncludeJarvisComments { get; set; } = true;

        /// <summary>
        /// Gets or sets the complexity level of the generated macro.
        /// </summary>
        public MacroComplexity Complexity { get; set; } = MacroComplexity.Standard;

        /// <summary>
        /// Gets or sets whether to include error handling.
        /// </summary>
        public bool IncludeErrorHandling { get; set; } = true;

        /// <summary>
        /// Gets or sets the target SolidWorks version for compatibility.
        /// </summary>
        public string? TargetVersion { get; set; }
    }

    /// <summary>
    /// Represents a request for generating assembly automation macros.
    /// </summary>
    public class AssemblyMacroRequest : MacroGenerationRequest
    {
        /// <summary>
        /// Gets or sets the list of component files to include in the assembly.
        /// </summary>
        public List<string> ComponentFiles { get; set; } = new();

        /// <summary>
        /// Gets or sets the mate relationships to create.
        /// </summary>
        public List<MateDefinition> Mates { get; set; } = new();

        /// <summary>
        /// Gets or sets whether to generate configurations.
        /// </summary>
        public bool GenerateConfigurations { get; set; } = false;

        /// <summary>
        /// Gets or sets the assembly template to use.
        /// </summary>
        public string? AssemblyTemplate { get; set; }
    }

    /// <summary>
    /// Represents a request for generating drawing automation macros.
    /// </summary>
    public class DrawingMacroRequest : MacroGenerationRequest
    {
        /// <summary>
        /// Gets or sets the source model file for the drawing.
        /// </summary>
        public string? SourceModelFile { get; set; }

        /// <summary>
        /// Gets or sets the drawing views to create.
        /// </summary>
        public List<DrawingViewDefinition> Views { get; set; } = new();

        /// <summary>
        /// Gets or sets the dimensions to include.
        /// </summary>
        public List<DimensionDefinition> Dimensions { get; set; } = new();

        /// <summary>
        /// Gets or sets the annotations to add.
        /// </summary>
        public List<AnnotationDefinition> Annotations { get; set; } = new();

        /// <summary>
        /// Gets or sets the drawing template to use.
        /// </summary>
        public string? DrawingTemplate { get; set; }
    }

    /// <summary>
    /// Represents the response from macro generation operations.
    /// </summary>
    public class MacroGenerationResponse
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
        /// Gets or sets the file path where the macro was saved.
        /// </summary>
        public string? MacroFilePath { get; set; }

        /// <summary>
        /// Gets or sets any warnings generated during the process.
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// Gets or sets any errors that occurred during generation.
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Gets or sets suggestions for improving the generated macro.
        /// </summary>
        public List<string> Suggestions { get; set; } = new();

        /// <summary>
        /// Gets or sets the estimated execution time for the macro.
        /// </summary>
        public TimeSpan? EstimatedExecutionTime { get; set; }
    }

    /// <summary>
    /// Enumeration of macro types that can be generated.
    /// </summary>
    public enum MacroType
    {
        PartCreation,
        FeatureModification,
        AssemblyAutomation,
        DrawingGeneration,
        MaterialAssignment,
        ConfigurationManagement,
        CustomProperty,
        MassPropertyCalculation,
        ExportAutomation,
        QualityControl
    }

    /// <summary>
    /// Enumeration of macro complexity levels.
    /// </summary>
    public enum MacroComplexity
    {
        Simple,
        Standard,
        Advanced,
        Expert
    }

    /// <summary>
    /// Represents a mate definition for assembly automation.
    /// </summary>
    public class MateDefinition
    {
        /// <summary>
        /// Gets or sets the type of mate.
        /// </summary>
        public MateType Type { get; set; }

        /// <summary>
        /// Gets or sets the first component reference.
        /// </summary>
        public string Component1 { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the second component reference.
        /// </summary>
        public string Component2 { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the face or edge reference for component 1.
        /// </summary>
        public string Reference1 { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the face or edge reference for component 2.
        /// </summary>
        public string Reference2 { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets additional mate parameters.
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Enumeration of SolidWorks mate types.
    /// </summary>
    public enum MateType
    {
        Coincident,
        Parallel,
        Perpendicular,
        Tangent,
        Concentric,
        Distance,
        Angle,
        Gear,
        Rack,
        Screw,
        Universal
    }

    /// <summary>
    /// Represents a drawing view definition.
    /// </summary>
    public class DrawingViewDefinition
    {
        /// <summary>
        /// Gets or sets the view type.
        /// </summary>
        public DrawingViewType Type { get; set; }

        /// <summary>
        /// Gets or sets the view name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the view position.
        /// </summary>
        public Point2D Position { get; set; } = new();

        /// <summary>
        /// Gets or sets the view scale.
        /// </summary>
        public double Scale { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets the view configuration.
        /// </summary>
        public string? Configuration { get; set; }
    }

    /// <summary>
    /// Enumeration of drawing view types.
    /// </summary>
    public enum DrawingViewType
    {
        Front,
        Top,
        Right,
        Isometric,
        Section,
        Detail,
        Auxiliary,
        Custom
    }

    /// <summary>
    /// Represents a 2D point for drawing positioning.
    /// </summary>
    public class Point2D
    {
        /// <summary>
        /// Gets or sets the X coordinate.
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// Gets or sets the Y coordinate.
        /// </summary>
        public double Y { get; set; }
    }

    /// <summary>
    /// Represents a dimension definition for drawings.
    /// </summary>
    public class DimensionDefinition
    {
        /// <summary>
        /// Gets or sets the dimension type.
        /// </summary>
        public DimensionType Type { get; set; }

        /// <summary>
        /// Gets or sets the dimension name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the dimension value.
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Gets or sets the dimension tolerance.
        /// </summary>
        public string? Tolerance { get; set; }

        /// <summary>
        /// Gets or sets the dimension position.
        /// </summary>
        public Point2D Position { get; set; } = new();
    }

    /// <summary>
    /// Enumeration of dimension types.
    /// </summary>
    public enum DimensionType
    {
        Linear,
        Angular,
        Radial,
        Diametral,
        Baseline,
        Chain,
        Ordinate
    }

    /// <summary>
    /// Represents an annotation definition for drawings.
    /// </summary>
    public class AnnotationDefinition
    {
        /// <summary>
        /// Gets or sets the annotation type.
        /// </summary>
        public AnnotationType Type { get; set; }

        /// <summary>
        /// Gets or sets the annotation text.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the annotation position.
        /// </summary>
        public Point2D Position { get; set; } = new();

        /// <summary>
        /// Gets or sets the annotation style.
        /// </summary>
        public string? Style { get; set; }
    }

    /// <summary>
    /// Enumeration of annotation types.
    /// </summary>
    public enum AnnotationType
    {
        Note,
        Symbol,
        SurfaceFinish,
        GeometricTolerance,
        Weld,
        Balloon,
        RevisionCloud,
        Label
    }
}
