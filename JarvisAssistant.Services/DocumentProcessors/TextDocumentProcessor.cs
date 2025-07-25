using JarvisAssistant.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;

namespace JarvisAssistant.Services.DocumentProcessors
{
    /// <summary>
    /// Document processor for plain text and markup files.
    /// </summary>
    public class TextDocumentProcessor : IDocumentProcessor
    {
        private readonly ILogger<TextDocumentProcessor> _logger;
        private static readonly string[] SupportedExtensions = { ".txt", ".md", ".markdown", ".rtf", ".csv" };

        /// <summary>
        /// Initializes a new instance of the <see cref="TextDocumentProcessor"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public TextDocumentProcessor(ILogger<TextDocumentProcessor> logger)
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
                _logger.LogInformation("Starting text extraction for file: {FileName}", fileName);

                if (fileContent == null || fileContent.Length == 0)
                {
                    _logger.LogWarning("Empty file content provided for text extraction");
                    return string.Empty;
                }

                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                string extractedText;

                switch (extension)
                {
                    case ".rtf":
                        extractedText = await ExtractRtfTextAsync(fileContent, cancellationToken);
                        break;
                    case ".csv":
                        extractedText = await ExtractCsvTextAsync(fileContent, cancellationToken);
                        break;
                    default:
                        extractedText = await ExtractPlainTextAsync(fileContent, cancellationToken);
                        break;
                }

                _logger.LogInformation("Successfully extracted {CharacterCount} characters from text file {FileName}", 
                    extractedText.Length, fileName);

                return extractedText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract text from file {FileName}", fileName);
                throw new InvalidOperationException($"Failed to extract text from file: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public IEnumerable<string> GetSupportedExtensions()
        {
            return SupportedExtensions;
        }

        /// <summary>
        /// Extracts text from plain text files with encoding detection.
        /// </summary>
        /// <param name="fileContent">The file content as bytes.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the extracted text.</returns>
        private async Task<string> ExtractPlainTextAsync(byte[] fileContent, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                // Try to detect encoding
                var encoding = DetectEncoding(fileContent);
                return encoding.GetString(fileContent);
            }, cancellationToken);
        }

        /// <summary>
        /// Extracts text from RTF files by removing RTF control codes.
        /// </summary>
        /// <param name="fileContent">The file content as bytes.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the extracted text.</returns>
        private async Task<string> ExtractRtfTextAsync(byte[] fileContent, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var rtfContent = Encoding.UTF8.GetString(fileContent);
                    return StripRtfFormatting(rtfContent);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse RTF content, falling back to plain text extraction");
                    return Encoding.UTF8.GetString(fileContent);
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Extracts text from CSV files with proper formatting.
        /// </summary>
        /// <param name="fileContent">The file content as bytes.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the extracted text.</returns>
        private async Task<string> ExtractCsvTextAsync(byte[] fileContent, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                var encoding = DetectEncoding(fileContent);
                var csvContent = encoding.GetString(fileContent);
                
                // Convert CSV to readable text format
                var lines = csvContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var textBuilder = new StringBuilder();

                foreach (var line in lines)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    // Replace commas with tabs for better readability
                    var formattedLine = line.Replace(",", "\t");
                    textBuilder.AppendLine(formattedLine);
                }

                return textBuilder.ToString();
            }, cancellationToken);
        }

        /// <summary>
        /// Detects the encoding of a text file using BOM and content analysis.
        /// </summary>
        /// <param name="fileContent">The file content as bytes.</param>
        /// <returns>The detected encoding.</returns>
        private Encoding DetectEncoding(byte[] fileContent)
        {
            if (fileContent.Length == 0)
                return Encoding.UTF8;

            // Check for BOM (Byte Order Mark)
            if (fileContent.Length >= 3 && 
                fileContent[0] == 0xEF && fileContent[1] == 0xBB && fileContent[2] == 0xBF)
            {
                return Encoding.UTF8;
            }

            if (fileContent.Length >= 2)
            {
                if (fileContent[0] == 0xFF && fileContent[1] == 0xFE)
                    return Encoding.Unicode; // UTF-16 LE
                if (fileContent[0] == 0xFE && fileContent[1] == 0xFF)
                    return Encoding.BigEndianUnicode; // UTF-16 BE
            }

            // Try UTF-8 first
            try
            {
                var utf8Decoder = Encoding.UTF8.GetDecoder();
                utf8Decoder.Fallback = DecoderFallback.ExceptionFallback;
                utf8Decoder.GetCharCount(fileContent, 0, fileContent.Length);
                return Encoding.UTF8;
            }
            catch (DecoderFallbackException)
            {
                // If UTF-8 fails, try common encodings
                var encodingsToTry = new[]
                {
                    Encoding.GetEncoding("windows-1252"), // Western European
                    Encoding.ASCII,
                    Encoding.Default // System default
                };

                foreach (var encoding in encodingsToTry)
                {
                    try
                    {
                        var decoder = encoding.GetDecoder();
                        decoder.Fallback = DecoderFallback.ExceptionFallback;
                        decoder.GetCharCount(fileContent, 0, fileContent.Length);
                        return encoding;
                    }
                    catch (DecoderFallbackException)
                    {
                        continue;
                    }
                }

                // Fallback to UTF-8 with replacement fallback
                return Encoding.UTF8;
            }
        }

        /// <summary>
        /// Strips RTF formatting codes to extract plain text.
        /// </summary>
        /// <param name="rtfContent">The RTF content.</param>
        /// <returns>The plain text content.</returns>
        private string StripRtfFormatting(string rtfContent)
        {
            if (string.IsNullOrWhiteSpace(rtfContent))
                return string.Empty;

            var textBuilder = new StringBuilder();
            var inControlWord = false;
            var inGroup = 0;
            var skipNext = false;

            for (int i = 0; i < rtfContent.Length; i++)
            {
                char c = rtfContent[i];

                if (skipNext)
                {
                    skipNext = false;
                    continue;
                }

                switch (c)
                {
                    case '\\':
                        inControlWord = true;
                        // Check for escaped characters
                        if (i + 1 < rtfContent.Length)
                        {
                            char next = rtfContent[i + 1];
                            if (next == '\\' || next == '{' || next == '}')
                            {
                                textBuilder.Append(next);
                                skipNext = true;
                                inControlWord = false;
                            }
                        }
                        break;

                    case '{':
                        inGroup++;
                        inControlWord = false;
                        break;

                    case '}':
                        inGroup--;
                        inControlWord = false;
                        break;

                    case ' ':
                    case '\n':
                    case '\r':
                        if (inControlWord)
                        {
                            inControlWord = false;
                        }
                        else if (!inControlWord && inGroup > 0)
                        {
                            textBuilder.Append(c);
                        }
                        break;

                    default:
                        if (!inControlWord && inGroup > 0)
                        {
                            textBuilder.Append(c);
                        }
                        break;
                }
            }

            return textBuilder.ToString().Trim();
        }
    }
}
