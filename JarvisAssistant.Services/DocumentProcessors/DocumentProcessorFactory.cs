using JarvisAssistant.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace JarvisAssistant.Services.DocumentProcessors
{
    /// <summary>
    /// Factory for creating appropriate document processors based on file type.
    /// </summary>
    public class DocumentProcessorFactory : IDocumentProcessor
    {
        private readonly List<IDocumentProcessor> _processors;
        private readonly ILogger<DocumentProcessorFactory> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentProcessorFactory"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        /// <param name="loggerFactory">The logger factory for creating processor-specific loggers.</param>
        public DocumentProcessorFactory(ILogger<DocumentProcessorFactory> logger, ILoggerFactory loggerFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));

            // Initialize all available processors
            _processors = new List<IDocumentProcessor>
            {
                new PdfDocumentProcessor(loggerFactory.CreateLogger<PdfDocumentProcessor>()),
                new WordDocumentProcessor(loggerFactory.CreateLogger<WordDocumentProcessor>()),
                new TextDocumentProcessor(loggerFactory.CreateLogger<TextDocumentProcessor>()),
                new HtmlDocumentProcessor(loggerFactory.CreateLogger<HtmlDocumentProcessor>())
            };

            _logger.LogInformation("Initialized document processor factory with {ProcessorCount} processors", _processors.Count);
        }

        /// <inheritdoc/>
        public bool CanProcess(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            return _processors.Any(p => p.CanProcess(fileName));
        }

        /// <inheritdoc/>
        public async Task<string> ExtractTextAsync(byte[] fileContent, string fileName, CancellationToken cancellationToken = default)
        {
            var processor = GetProcessorForFile(fileName);
            
            if (processor == null)
            {
                var extension = Path.GetExtension(fileName);
                throw new NotSupportedException($"No processor available for file type: {extension}");
            }

            _logger.LogDebug("Using {ProcessorType} for file {FileName}", 
                processor.GetType().Name, fileName);

            return await processor.ExtractTextAsync(fileContent, fileName, cancellationToken);
        }

        /// <inheritdoc/>
        public IEnumerable<string> GetSupportedExtensions()
        {
            return _processors.SelectMany(p => p.GetSupportedExtensions()).Distinct();
        }

        /// <summary>
        /// Gets the appropriate processor for a given file.
        /// </summary>
        /// <param name="fileName">The name of the file to process.</param>
        /// <returns>The appropriate processor, or null if none available.</returns>
        public IDocumentProcessor? GetProcessorForFile(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return null;

            return _processors.FirstOrDefault(p => p.CanProcess(fileName));
        }

        /// <summary>
        /// Gets information about all available processors.
        /// </summary>
        /// <returns>A dictionary mapping processor types to their supported extensions.</returns>
        public Dictionary<string, IEnumerable<string>> GetProcessorInfo()
        {
            return _processors.ToDictionary(
                p => p.GetType().Name,
                p => p.GetSupportedExtensions()
            );
        }

        /// <summary>
        /// Validates that a file can be processed.
        /// </summary>
        /// <param name="fileName">The name of the file to validate.</param>
        /// <param name="fileSize">The size of the file in bytes.</param>
        /// <param name="maxFileSizeBytes">The maximum allowed file size in bytes.</param>
        /// <returns>A validation result indicating if the file can be processed.</returns>
        public FileValidationResult ValidateFile(string fileName, long fileSize, long maxFileSizeBytes = 100 * 1024 * 1024) // 100MB default
        {
            var result = new FileValidationResult { IsValid = true };

            if (string.IsNullOrWhiteSpace(fileName))
            {
                result.IsValid = false;
                result.ErrorMessage = "File name cannot be empty";
                return result;
            }

            if (fileSize <= 0)
            {
                result.IsValid = false;
                result.ErrorMessage = "File size must be greater than zero";
                return result;
            }

            if (fileSize > maxFileSizeBytes)
            {
                result.IsValid = false;
                result.ErrorMessage = $"File size ({fileSize:N0} bytes) exceeds maximum allowed size ({maxFileSizeBytes:N0} bytes)";
                return result;
            }

            if (!CanProcess(fileName))
            {
                var extension = Path.GetExtension(fileName);
                result.IsValid = false;
                result.ErrorMessage = $"File type '{extension}' is not supported";
                result.SupportedExtensions = GetSupportedExtensions().ToList();
                return result;
            }

            result.ProcessorType = GetProcessorForFile(fileName)?.GetType().Name;
            return result;
        }
    }

    /// <summary>
    /// Represents the result of file validation.
    /// </summary>
    public class FileValidationResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether the file is valid for processing.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the error message if validation failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the type of processor that would handle this file.
        /// </summary>
        public string? ProcessorType { get; set; }

        /// <summary>
        /// Gets or sets the list of supported file extensions (populated when validation fails due to unsupported type).
        /// </summary>
        public List<string> SupportedExtensions { get; set; } = new();
    }
}
