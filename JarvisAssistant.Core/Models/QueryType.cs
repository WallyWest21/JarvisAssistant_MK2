namespace JarvisAssistant.Core.Models
{
    /// <summary>
    /// Defines the type of query being made to determine the appropriate model to use.
    /// </summary>
    public enum QueryType
    {
        /// <summary>
        /// General conversation and assistance.
        /// </summary>
        General,

        /// <summary>
        /// Code assistance, programming help, and technical guidance.
        /// </summary>
        Code,

        /// <summary>
        /// Technical documentation, system information, and detailed analysis.
        /// </summary>
        Technical,

        /// <summary>
        /// Creative writing, storytelling, and artistic tasks.
        /// </summary>
        Creative,

        /// <summary>
        /// Mathematical calculations and scientific computations.
        /// </summary>
        Mathematical,

        /// <summary>
        /// Error analysis and troubleshooting assistance.
        /// </summary>
        Error
    }
}
