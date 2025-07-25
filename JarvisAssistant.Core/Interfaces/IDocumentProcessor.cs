namespace JarvisAssistant.Core.Interfaces
{
    /// <summary>
    /// Provides methods for processing documents and extracting text content.
    /// </summary>
    public interface IDocumentProcessor
    {
        /// <summary>
        /// Determines if the processor can handle the specified file type.
        /// </summary>
        /// <param name="fileName">The name of the file to check.</param>
        /// <returns>True if the processor can handle this file type, false otherwise.</returns>
        bool CanProcess(string fileName);

        /// <summary>
        /// Extracts text content from a document.
        /// </summary>
        /// <param name="fileContent">The binary content of the file.</param>
        /// <param name="fileName">The name of the file being processed.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the extracted text.</returns>
        Task<string> ExtractTextAsync(byte[] fileContent, string fileName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the supported file extensions for this processor.
        /// </summary>
        /// <returns>An enumerable of supported file extensions (including the dot, e.g., ".pdf").</returns>
        IEnumerable<string> GetSupportedExtensions();
    }
}
