namespace JarvisAssistant.Core.Models.SolidWorks
{
    /// <summary>
    /// Represents the result of analyzing a SolidWorks part file.
    /// </summary>
    public class PartAnalysisResult
    {
        /// <summary>
        /// Gets or sets whether the analysis was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Gets or sets the analyzed file path.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the part name.
        /// </summary>
        public string PartName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the part mass properties.
        /// </summary>
        public MassProperties MassProperties { get; set; } = new();

        /// <summary>
        /// Gets or sets the feature analysis results.
        /// </summary>
        public List<FeatureAnalysis> Features { get; set; } = new();

        /// <summary>
        /// Gets or sets the optimization suggestions.
        /// </summary>
        public List<OptimizationSuggestion> OptimizationSuggestions { get; set; } = new();

        /// <summary>
        /// Gets or sets the quality issues found.
        /// </summary>
        public List<QualityIssue> QualityIssues { get; set; } = new();

        /// <summary>
        /// Gets or sets the manufacturing considerations.
        /// </summary>
        public List<ManufacturingConsideration> ManufacturingConsiderations { get; set; } = new();

        /// <summary>
        /// Gets or sets the material information.
        /// </summary>
        public MaterialInfo? Material { get; set; }

        /// <summary>
        /// Gets or sets the analysis timestamp.
        /// </summary>
        public DateTime AnalysisTimestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets any analysis errors encountered.
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Gets or sets the overall quality score (0-100).
        /// </summary>
        public int QualityScore { get; set; }
    }

    /// <summary>
    /// Represents mass properties of a part.
    /// </summary>
    public class MassProperties
    {
        /// <summary>
        /// Gets or sets the mass in kilograms.
        /// </summary>
        public double Mass { get; set; }

        /// <summary>
        /// Gets or sets the volume in cubic millimeters.
        /// </summary>
        public double Volume { get; set; }

        /// <summary>
        /// Gets or sets the surface area in square millimeters.
        /// </summary>
        public double SurfaceArea { get; set; }

        /// <summary>
        /// Gets or sets the center of mass.
        /// </summary>
        public Point3D CenterOfMass { get; set; } = new();

        /// <summary>
        /// Gets or sets the principal moments of inertia.
        /// </summary>
        public MomentOfInertia PrincipalMoments { get; set; } = new();

        /// <summary>
        /// Gets or sets the bounding box dimensions.
        /// </summary>
        public BoundingBox BoundingBox { get; set; } = new();
    }

    /// <summary>
    /// Represents moment of inertia values.
    /// </summary>
    public class MomentOfInertia
    {
        /// <summary>
        /// Gets or sets the moment about X-axis.
        /// </summary>
        public double Ixx { get; set; }

        /// <summary>
        /// Gets or sets the moment about Y-axis.
        /// </summary>
        public double Iyy { get; set; }

        /// <summary>
        /// Gets or sets the moment about Z-axis.
        /// </summary>
        public double Izz { get; set; }

        /// <summary>
        /// Gets or sets the product of inertia Ixy.
        /// </summary>
        public double Ixy { get; set; }

        /// <summary>
        /// Gets or sets the product of inertia Ixz.
        /// </summary>
        public double Ixz { get; set; }

        /// <summary>
        /// Gets or sets the product of inertia Iyz.
        /// </summary>
        public double Iyz { get; set; }
    }

    /// <summary>
    /// Represents a 3D bounding box.
    /// </summary>
    public class BoundingBox
    {
        /// <summary>
        /// Gets or sets the minimum point of the bounding box.
        /// </summary>
        public Point3D MinPoint { get; set; } = new();

        /// <summary>
        /// Gets or sets the maximum point of the bounding box.
        /// </summary>
        public Point3D MaxPoint { get; set; } = new();

        /// <summary>
        /// Gets the width (X dimension) of the bounding box.
        /// </summary>
        public double Width => MaxPoint.X - MinPoint.X;

        /// <summary>
        /// Gets the height (Y dimension) of the bounding box.
        /// </summary>
        public double Height => MaxPoint.Y - MinPoint.Y;

        /// <summary>
        /// Gets the depth (Z dimension) of the bounding box.
        /// </summary>
        public double Depth => MaxPoint.Z - MinPoint.Z;
    }

    /// <summary>
    /// Represents the analysis of a single feature.
    /// </summary>
    public class FeatureAnalysis
    {
        /// <summary>
        /// Gets or sets the feature name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the feature type.
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the feature is suppressed.
        /// </summary>
        public bool IsSuppressed { get; set; }

        /// <summary>
        /// Gets or sets the feature complexity score.
        /// </summary>
        public int ComplexityScore { get; set; }

        /// <summary>
        /// Gets or sets the rebuild time in milliseconds.
        /// </summary>
        public double RebuildTime { get; set; }

        /// <summary>
        /// Gets or sets any feature-specific issues.
        /// </summary>
        public List<string> Issues { get; set; } = new();

        /// <summary>
        /// Gets or sets suggestions for improving the feature.
        /// </summary>
        public List<string> Suggestions { get; set; } = new();

        /// <summary>
        /// Gets or sets the parent features this feature depends on.
        /// </summary>
        public List<string> Dependencies { get; set; } = new();
    }

    /// <summary>
    /// Represents an optimization suggestion for the part.
    /// </summary>
    public class OptimizationSuggestion
    {
        /// <summary>
        /// Gets or sets the suggestion title.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the detailed description of the suggestion.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the category of optimization.
        /// </summary>
        public OptimizationCategory Category { get; set; }

        /// <summary>
        /// Gets or sets the potential impact level.
        /// </summary>
        public ImpactLevel Impact { get; set; }

        /// <summary>
        /// Gets or sets the difficulty of implementing the suggestion.
        /// </summary>
        public DifficultyLevel Difficulty { get; set; }

        /// <summary>
        /// Gets or sets the estimated time savings in seconds.
        /// </summary>
        public double EstimatedTimeSavings { get; set; }

        /// <summary>
        /// Gets or sets the features affected by this optimization.
        /// </summary>
        public List<string> AffectedFeatures { get; set; } = new();
    }

    /// <summary>
    /// Represents a quality issue found in the part.
    /// </summary>
    public class QualityIssue
    {
        /// <summary>
        /// Gets or sets the issue title.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the issue description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the severity of the issue.
        /// </summary>
        public IssueSeverity Severity { get; set; }

        /// <summary>
        /// Gets or sets the issue category.
        /// </summary>
        public IssueCategory Category { get; set; }

        /// <summary>
        /// Gets or sets the feature(s) where the issue was found.
        /// </summary>
        public List<string> AffectedFeatures { get; set; } = new();

        /// <summary>
        /// Gets or sets suggested remediation steps.
        /// </summary>
        public List<string> RemediationSteps { get; set; } = new();

        /// <summary>
        /// Gets or sets the potential consequences if not addressed.
        /// </summary>
        public List<string> Consequences { get; set; } = new();
    }

    /// <summary>
    /// Represents a manufacturing consideration for the part.
    /// </summary>
    public class ManufacturingConsideration
    {
        /// <summary>
        /// Gets or sets the consideration title.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the detailed description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the manufacturing process category.
        /// </summary>
        public ManufacturingProcess Process { get; set; }

        /// <summary>
        /// Gets or sets the importance level.
        /// </summary>
        public ImportanceLevel Importance { get; set; }

        /// <summary>
        /// Gets or sets the estimated cost impact.
        /// </summary>
        public CostImpact CostImpact { get; set; }

        /// <summary>
        /// Gets or sets suggestions for manufacturability improvements.
        /// </summary>
        public List<string> Improvements { get; set; } = new();

        /// <summary>
        /// Gets or sets alternative manufacturing methods.
        /// </summary>
        public List<string> Alternatives { get; set; } = new();
    }

    /// <summary>
    /// Represents material information for the part.
    /// </summary>
    public class MaterialInfo
    {
        /// <summary>
        /// Gets or sets the material name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the material density in kg/m³.
        /// </summary>
        public double Density { get; set; }

        /// <summary>
        /// Gets or sets the material category.
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the material cost per unit.
        /// </summary>
        public double? CostPerUnit { get; set; }

        /// <summary>
        /// Gets or sets the material properties.
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new();

        /// <summary>
        /// Gets or sets alternative material suggestions.
        /// </summary>
        public List<MaterialAlternative> Alternatives { get; set; } = new();
    }

    /// <summary>
    /// Represents a material alternative suggestion.
    /// </summary>
    public class MaterialAlternative
    {
        /// <summary>
        /// Gets or sets the alternative material name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the reason for the suggestion.
        /// </summary>
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the expected benefits.
        /// </summary>
        public List<string> Benefits { get; set; } = new();

        /// <summary>
        /// Gets or sets the potential drawbacks.
        /// </summary>
        public List<string> Drawbacks { get; set; } = new();

        /// <summary>
        /// Gets or sets the cost comparison factor.
        /// </summary>
        public double CostFactor { get; set; } = 1.0;
    }

    /// <summary>
    /// Represents event arguments for SolidWorks connection status changes.
    /// </summary>
    public class SolidWorksConnectionEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets whether SolidWorks is currently connected.
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// Gets or sets the SolidWorks version if connected.
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Gets or sets any connection-related message.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the status change.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Enumeration of optimization categories.
    /// </summary>
    public enum OptimizationCategory
    {
        Performance,
        FeatureTree,
        Geometry,
        Materials,
        Configurations,
        References,
        Modeling,
        Assembly
    }

    /// <summary>
    /// Enumeration of impact levels.
    /// </summary>
    public enum ImpactLevel
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Enumeration of difficulty levels.
    /// </summary>
    public enum DifficultyLevel
    {
        Easy,
        Medium,
        Hard,
        Expert
    }

    /// <summary>
    /// Enumeration of issue severities.
    /// </summary>
    public enum IssueSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// Enumeration of issue categories.
    /// </summary>
    public enum IssueCategory
    {
        Geometry,
        Features,
        References,
        Performance,
        Standards,
        Compatibility,
        Manufacturing,
        Assembly
    }

    /// <summary>
    /// Enumeration of manufacturing processes.
    /// </summary>
    public enum ManufacturingProcess
    {
        Machining,
        Casting,
        Forging,
        SheetMetal,
        Injection,
        Welding,
        Additive,
        Assembly,
        Finishing,
        General
    }

    /// <summary>
    /// Enumeration of importance levels.
    /// </summary>
    public enum ImportanceLevel
    {
        Optional,
        Recommended,
        Important,
        Critical
    }

    /// <summary>
    /// Enumeration of cost impact levels.
    /// </summary>
    public enum CostImpact
    {
        Minimal,
        Low,
        Medium,
        High,
        Significant
    }
}
