using JarvisAssistant.Core.Interfaces;
using Microsoft.Extensions.Logging;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text;

namespace JarvisAssistant.Services.DocumentProcessors
{
    /// <summary>
    /// Document processor for Microsoft Word documents using OpenXML.
    /// </summary>
    public class WordDocumentProcessor : IDocumentProcessor
    {
        private readonly ILogger<WordDocumentProcessor> _logger;
        private static readonly string[] SupportedExtensions = { ".docx", ".docm" };

        /// <summary>
        /// Initializes a new instance of the <see cref="WordDocumentProcessor"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public WordDocumentProcessor(ILogger<WordDocumentProcessor> logger)
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
                _logger.LogInformation("Starting Word document text extraction for file: {FileName}", fileName);

                if (fileContent == null || fileContent.Length == 0)
                {
                    _logger.LogWarning("Empty file content provided for Word document extraction");
                    return string.Empty;
                }

                using var memoryStream = new MemoryStream(fileContent);
                using var document = WordprocessingDocument.Open(memoryStream, false);

                if (document.MainDocumentPart == null)
                {
                    _logger.LogWarning("Word document {FileName} has no main document part", fileName);
                    return string.Empty;
                }

                var textBuilder = new StringBuilder();
                var body = document.MainDocumentPart.Document.Body;

                if (body != null)
                {
                    await ExtractTextFromBodyAsync(body, textBuilder, cancellationToken);
                }

                // Extract text from headers and footers
                await ExtractHeaderFooterTextAsync(document, textBuilder, cancellationToken);

                // Extract text from footnotes and endnotes
                await ExtractNotesTextAsync(document, textBuilder, cancellationToken);

                var extractedText = textBuilder.ToString().Trim();
                _logger.LogInformation("Successfully extracted {CharacterCount} characters from Word document {FileName}", 
                    extractedText.Length, fileName);

                return extractedText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract text from Word document {FileName}", fileName);
                throw new InvalidOperationException($"Failed to extract text from Word document: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public IEnumerable<string> GetSupportedExtensions()
        {
            return SupportedExtensions;
        }

        /// <summary>
        /// Extracts text from the document body.
        /// </summary>
        /// <param name="body">The document body to extract text from.</param>
        /// <param name="textBuilder">The string builder to append text to.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task ExtractTextFromBodyAsync(Body body, StringBuilder textBuilder, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                foreach (var element in body.Elements())
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    ExtractTextFromElement(element, textBuilder);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Extracts text from headers and footers.
        /// </summary>
        /// <param name="document">The Word document.</param>
        /// <param name="textBuilder">The string builder to append text to.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task ExtractHeaderFooterTextAsync(WordprocessingDocument document, StringBuilder textBuilder, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                // Extract header text
                foreach (var headerPart in document.MainDocumentPart!.HeaderParts)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    if (headerPart.Header != null)
                    {
                        foreach (var element in headerPart.Header.Elements())
                        {
                            ExtractTextFromElement(element, textBuilder);
                        }
                    }
                }

                // Extract footer text
                foreach (var footerPart in document.MainDocumentPart.FooterParts)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    if (footerPart.Footer != null)
                    {
                        foreach (var element in footerPart.Footer.Elements())
                        {
                            ExtractTextFromElement(element, textBuilder);
                        }
                    }
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Extracts text from footnotes and endnotes.
        /// </summary>
        /// <param name="document">The Word document.</param>
        /// <param name="textBuilder">The string builder to append text to.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task ExtractNotesTextAsync(WordprocessingDocument document, StringBuilder textBuilder, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                // Extract footnotes
                var footnotesPart = document.MainDocumentPart!.FootnotesPart;
                if (footnotesPart?.Footnotes != null)
                {
                    foreach (var footnote in footnotesPart.Footnotes.Elements<Footnote>())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        
                        foreach (var element in footnote.Elements())
                        {
                            ExtractTextFromElement(element, textBuilder);
                        }
                    }
                }

                // Extract endnotes
                var endnotesPart = document.MainDocumentPart.EndnotesPart;
                if (endnotesPart?.Endnotes != null)
                {
                    foreach (var endnote in endnotesPart.Endnotes.Elements<Endnote>())
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        
                        foreach (var element in endnote.Elements())
                        {
                            ExtractTextFromElement(element, textBuilder);
                        }
                    }
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Recursively extracts text from OpenXML elements.
        /// </summary>
        /// <param name="element">The element to extract text from.</param>
        /// <param name="textBuilder">The string builder to append text to.</param>
        private void ExtractTextFromElement(object element, StringBuilder textBuilder)
        {
            switch (element)
            {
                case Paragraph paragraph:
                    ExtractTextFromParagraph(paragraph, textBuilder);
                    textBuilder.AppendLine(); // Add line break after paragraph
                    break;

                case Table table:
                    ExtractTextFromTable(table, textBuilder);
                    break;

                case Text text:
                    textBuilder.Append(text.Text);
                    break;

                case Break:
                    textBuilder.AppendLine(); // Handle line breaks
                    break;

                case TabChar:
                    textBuilder.Append('\t'); // Handle tab characters
                    break;

                default:
                    // For other elements, recursively process child elements
                    if (element is OpenXmlElement xmlElement)
                    {
                        foreach (var child in xmlElement.Elements())
                        {
                            ExtractTextFromElement(child, textBuilder);
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Extracts text from a paragraph element.
        /// </summary>
        /// <param name="paragraph">The paragraph to extract text from.</param>
        /// <param name="textBuilder">The string builder to append text to.</param>
        private void ExtractTextFromParagraph(Paragraph paragraph, StringBuilder textBuilder)
        {
            foreach (var run in paragraph.Elements<Run>())
            {
                foreach (var element in run.Elements())
                {
                    ExtractTextFromElement(element, textBuilder);
                }
            }
        }

        /// <summary>
        /// Extracts text from a table element.
        /// </summary>
        /// <param name="table">The table to extract text from.</param>
        /// <param name="textBuilder">The string builder to append text to.</param>
        private void ExtractTextFromTable(Table table, StringBuilder textBuilder)
        {
            foreach (var row in table.Elements<TableRow>())
            {
                foreach (var cell in row.Elements<TableCell>())
                {
                    foreach (var paragraph in cell.Elements<Paragraph>())
                    {
                        ExtractTextFromParagraph(paragraph, textBuilder);
                        textBuilder.Append('\t'); // Separate table cells with tabs
                    }
                }
                textBuilder.AppendLine(); // New line after each table row
            }
        }
    }
}
