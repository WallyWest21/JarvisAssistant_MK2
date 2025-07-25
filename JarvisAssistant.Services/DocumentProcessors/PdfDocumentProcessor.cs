using JarvisAssistant.Core.Interfaces;
using Microsoft.Extensions.Logging;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Pdf.Content;
using PdfSharp.Pdf.Content.Objects;
using System.Text;

namespace JarvisAssistant.Services.DocumentProcessors
{
    /// <summary>
    /// Document processor for PDF files using PdfSharp.
    /// </summary>
    public class PdfDocumentProcessor : IDocumentProcessor
    {
        private readonly ILogger<PdfDocumentProcessor> _logger;
        private static readonly string[] SupportedExtensions = { ".pdf" };

        /// <summary>
        /// Initializes a new instance of the <see cref="PdfDocumentProcessor"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public PdfDocumentProcessor(ILogger<PdfDocumentProcessor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public bool CanProcess(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            return SupportedExtensions.Contains(extension);
        }

        /// <inheritdoc/>
        public async Task<string> ExtractTextAsync(byte[] fileContent, string fileName, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting PDF text extraction for file: {FileName}", fileName);

                if (fileContent == null || fileContent.Length == 0)
                {
                    _logger.LogWarning("Empty file content provided for PDF extraction");
                    return string.Empty;
                }

                using var memoryStream = new MemoryStream(fileContent);
                var document = PdfReader.Open(memoryStream, PdfDocumentOpenMode.ReadOnly);
                
                var textBuilder = new StringBuilder();
                var pageCount = 0;

                foreach (var page in document.Pages)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    try
                    {
                        var pageText = await ExtractTextFromPageAsync(page, cancellationToken);
                        if (!string.IsNullOrWhiteSpace(pageText))
                        {
                            textBuilder.AppendLine(pageText);
                            textBuilder.AppendLine(); // Add spacing between pages
                        }
                        pageCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to extract text from page {PageNumber} in PDF {FileName}", 
                            pageCount + 1, fileName);
                        // Continue with other pages
                    }
                }

                var extractedText = textBuilder.ToString().Trim();
                _logger.LogInformation("Successfully extracted {CharacterCount} characters from {PageCount} pages in PDF {FileName}", 
                    extractedText.Length, pageCount, fileName);

                return extractedText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract text from PDF {FileName}", fileName);
                throw new InvalidOperationException($"Failed to extract text from PDF: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public IEnumerable<string> GetSupportedExtensions()
        {
            return SupportedExtensions;
        }

        /// <summary>
        /// Extracts text from a single PDF page.
        /// </summary>
        /// <param name="page">The PDF page to extract text from.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the extracted text.</returns>
        private async Task<string> ExtractTextFromPageAsync(PdfPage page, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Get the page content
                    var content = ContentReader.ReadContent(page);
                    var textBuilder = new StringBuilder();

                    // Convert CSequence to CObject[] for processing
                    var contentArray = new CObject[content.Count];
                    for (int i = 0; i < content.Count; i++)
                    {
                        contentArray[i] = content[i];
                    }

                    // Extract text from content objects
                    ExtractTextFromContentObjects(contentArray, textBuilder);

                    return textBuilder.ToString();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to extract text from PDF page");
                    return string.Empty;
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Recursively extracts text from PDF content objects.
        /// </summary>
        /// <param name="objects">The content objects to process.</param>
        /// <param name="textBuilder">The string builder to append text to.</param>
        private void ExtractTextFromContentObjects(CObject[] objects, StringBuilder textBuilder)
        {
            foreach (var obj in objects)
            {
                if (obj is COperator op)
                {
                    // Handle text showing operations
                    if (op.OpCode.Name == "Tj" || op.OpCode.Name == "TJ")
                    {
                        ExtractTextFromOperator(op, textBuilder);
                    }
                }
                else if (obj is CSequence sequence)
                {
                    // Recursively process sequences - convert sequence to array
                    var sequenceArray = new CObject[sequence.Count];
                    for (int i = 0; i < sequence.Count; i++)
                    {
                        sequenceArray[i] = sequence[i];
                    }
                    ExtractTextFromContentObjects(sequenceArray, textBuilder);
                }
            }
        }

        /// <summary>
        /// Extracts text from a text-showing operator.
        /// </summary>
        /// <param name="op">The operator to extract text from.</param>
        /// <param name="textBuilder">The string builder to append text to.</param>
        private void ExtractTextFromOperator(COperator op, StringBuilder textBuilder)
        {
            try
            {
                if (op.Operands.Count > 0)
                {
                    foreach (var operand in op.Operands)
                    {
                        if (operand is CString str)
                        {
                            // Convert PDF string to text
                            var text = str.Value;
                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                textBuilder.Append(text);
                            }
                        }
                        else if (operand is CArray array)
                        {
                            // Handle array of strings (used in TJ operator)
                            foreach (var item in array)
                            {
                                if (item is CString arrayStr)
                                {
                                    var text = arrayStr.Value;
                                    if (!string.IsNullOrWhiteSpace(text))
                                    {
                                        textBuilder.Append(text);
                                    }
                                }
                            }
                        }
                    }

                    // Add space after text operations to separate words
                    textBuilder.Append(' ');
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract text from PDF operator");
            }
        }
    }
}
