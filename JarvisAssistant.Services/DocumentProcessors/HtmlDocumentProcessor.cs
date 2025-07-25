using JarvisAssistant.Core.Interfaces;
using Microsoft.Extensions.Logging;
using HtmlAgilityPack;
using System.Text;

namespace JarvisAssistant.Services.DocumentProcessors
{
    /// <summary>
    /// Document processor for HTML files using HtmlAgilityPack.
    /// </summary>
    public class HtmlDocumentProcessor : IDocumentProcessor
    {
        private readonly ILogger<HtmlDocumentProcessor> _logger;
        private static readonly string[] SupportedExtensions = { ".html", ".htm", ".xhtml" };

        /// <summary>
        /// Initializes a new instance of the <see cref="HtmlDocumentProcessor"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public HtmlDocumentProcessor(ILogger<HtmlDocumentProcessor> logger)
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
                _logger.LogInformation("Starting HTML text extraction for file: {FileName}", fileName);

                if (fileContent == null || fileContent.Length == 0)
                {
                    _logger.LogWarning("Empty file content provided for HTML extraction");
                    return string.Empty;
                }

                var encoding = DetectEncoding(fileContent);
                var htmlContent = encoding.GetString(fileContent);

                var extractedText = await ExtractTextFromHtmlAsync(htmlContent, cancellationToken);

                _logger.LogInformation("Successfully extracted {CharacterCount} characters from HTML file {FileName}", 
                    extractedText.Length, fileName);

                return extractedText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract text from HTML file {FileName}", fileName);
                throw new InvalidOperationException($"Failed to extract text from HTML file: {ex.Message}", ex);
            }
        }

        /// <inheritdoc/>
        public IEnumerable<string> GetSupportedExtensions()
        {
            return SupportedExtensions;
        }

        /// <summary>
        /// Extracts text from HTML content while preserving structure.
        /// </summary>
        /// <param name="htmlContent">The HTML content as string.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the extracted text.</returns>
        private async Task<string> ExtractTextFromHtmlAsync(string htmlContent, CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                var document = new HtmlDocument();
                document.LoadHtml(htmlContent);

                var textBuilder = new StringBuilder();

                // Remove script and style elements
                RemoveUnwantedElements(document);

                // Extract text from the document
                if (document.DocumentNode != null)
                {
                    ExtractTextFromNode(document.DocumentNode, textBuilder, cancellationToken);
                }

                // Clean up the extracted text
                var extractedText = textBuilder.ToString();
                return CleanExtractedText(extractedText);
            }, cancellationToken);
        }

        /// <summary>
        /// Removes script, style, and other non-content elements from the HTML document.
        /// </summary>
        /// <param name="document">The HTML document to clean.</param>
        private void RemoveUnwantedElements(HtmlDocument document)
        {
            var unwantedTags = new[] { "script", "style", "nav", "header", "footer", "aside", "advertisement", "ads" };

            foreach (var tag in unwantedTags)
            {
                var elements = document.DocumentNode.SelectNodes($"//{tag}");
                if (elements != null)
                {
                    foreach (var element in elements.ToList())
                    {
                        element.Remove();
                    }
                }
            }

            // Remove comments
            var comments = document.DocumentNode.SelectNodes("//comment()");
            if (comments != null)
            {
                foreach (var comment in comments.ToList())
                {
                    comment.Remove();
                }
            }
        }

        /// <summary>
        /// Recursively extracts text from HTML nodes while preserving structure.
        /// </summary>
        /// <param name="node">The HTML node to extract text from.</param>
        /// <param name="textBuilder">The string builder to append text to.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        private void ExtractTextFromNode(HtmlNode node, StringBuilder textBuilder, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (node.NodeType == HtmlNodeType.Text)
            {
                var text = HtmlEntity.DeEntitize(node.InnerText);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    textBuilder.Append(text);
                }
            }
            else if (node.NodeType == HtmlNodeType.Element)
            {
                // Add line breaks for block elements
                var blockElements = new HashSet<string>
                {
                    "p", "div", "h1", "h2", "h3", "h4", "h5", "h6", "br", "hr",
                    "article", "section", "main", "li", "tr", "td", "th", "pre",
                    "blockquote", "address", "figure", "figcaption"
                };

                bool isBlockElement = blockElements.Contains(node.Name.ToLowerInvariant());

                if (isBlockElement && textBuilder.Length > 0 && 
                    !textBuilder.ToString().EndsWith('\n'))
                {
                    textBuilder.AppendLine();
                }

                // Handle special elements
                switch (node.Name.ToLowerInvariant())
                {
                    case "title":
                        // Extract title and add as heading
                        var titleText = HtmlEntity.DeEntitize(node.InnerText);
                        if (!string.IsNullOrWhiteSpace(titleText))
                        {
                            textBuilder.AppendLine($"Title: {titleText}");
                            textBuilder.AppendLine();
                        }
                        return;

                    case "h1":
                    case "h2":
                    case "h3":
                    case "h4":
                    case "h5":
                    case "h6":
                        // Add heading markers
                        var headingLevel = int.Parse(node.Name.Substring(1));
                        var headingText = HtmlEntity.DeEntitize(node.InnerText);
                        if (!string.IsNullOrWhiteSpace(headingText))
                        {
                            textBuilder.AppendLine(new string('#', headingLevel) + " " + headingText);
                            textBuilder.AppendLine();
                        }
                        return;

                    case "br":
                        textBuilder.AppendLine();
                        return;

                    case "hr":
                        textBuilder.AppendLine("---");
                        return;

                    case "img":
                        // Extract alt text from images
                        var altText = node.GetAttributeValue("alt", "");
                        if (!string.IsNullOrWhiteSpace(altText))
                        {
                            textBuilder.Append($"[Image: {altText}]");
                        }
                        return;

                    case "a":
                        // Extract link text and URL
                        var linkText = HtmlEntity.DeEntitize(node.InnerText);
                        var href = node.GetAttributeValue("href", "");
                        if (!string.IsNullOrWhiteSpace(linkText))
                        {
                            if (!string.IsNullOrWhiteSpace(href) && href != linkText)
                            {
                                textBuilder.Append($"{linkText} ({href})");
                            }
                            else
                            {
                                textBuilder.Append(linkText);
                            }
                        }
                        return;

                    case "table":
                        ExtractTableText(node, textBuilder, cancellationToken);
                        return;

                    case "ul":
                    case "ol":
                        ExtractListText(node, textBuilder, cancellationToken);
                        return;
                }

                // Process child nodes
                foreach (var child in node.ChildNodes)
                {
                    ExtractTextFromNode(child, textBuilder, cancellationToken);
                }

                if (isBlockElement)
                {
                    textBuilder.AppendLine();
                }
            }
        }

        /// <summary>
        /// Extracts text from HTML tables with proper formatting.
        /// </summary>
        /// <param name="tableNode">The table node to extract text from.</param>
        /// <param name="textBuilder">The string builder to append text to.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        private void ExtractTableText(HtmlNode tableNode, StringBuilder textBuilder, CancellationToken cancellationToken)
        {
            var rows = tableNode.SelectNodes(".//tr");
            if (rows == null) return;

            textBuilder.AppendLine();
            textBuilder.AppendLine("Table:");

            foreach (var row in rows)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var cells = row.SelectNodes(".//td | .//th");
                if (cells != null)
                {
                    var cellTexts = cells.Select(cell => HtmlEntity.DeEntitize(cell.InnerText).Trim());
                    textBuilder.AppendLine(string.Join(" | ", cellTexts));
                }
            }
            textBuilder.AppendLine();
        }

        /// <summary>
        /// Extracts text from HTML lists with proper formatting.
        /// </summary>
        /// <param name="listNode">The list node to extract text from.</param>
        /// <param name="textBuilder">The string builder to append text to.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        private void ExtractListText(HtmlNode listNode, StringBuilder textBuilder, CancellationToken cancellationToken)
        {
            var items = listNode.SelectNodes(".//li");
            if (items == null) return;

            bool isOrdered = listNode.Name.ToLowerInvariant() == "ol";
            int itemNumber = 1;

            foreach (var item in items)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var itemText = HtmlEntity.DeEntitize(item.InnerText).Trim();
                if (!string.IsNullOrWhiteSpace(itemText))
                {
                    if (isOrdered)
                    {
                        textBuilder.AppendLine($"{itemNumber}. {itemText}");
                        itemNumber++;
                    }
                    else
                    {
                        textBuilder.AppendLine($"• {itemText}");
                    }
                }
            }
        }

        /// <summary>
        /// Cleans and normalizes extracted text.
        /// </summary>
        /// <param name="text">The text to clean.</param>
        /// <returns>The cleaned text.</returns>
        private string CleanExtractedText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // Normalize whitespace
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
            
            // Remove excessive line breaks
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\n\s*\n\s*\n", "\n\n");
            
            // Trim whitespace
            text = text.Trim();

            return text;
        }

        /// <summary>
        /// Detects the encoding of an HTML file.
        /// </summary>
        /// <param name="fileContent">The file content as bytes.</param>
        /// <returns>The detected encoding.</returns>
        private Encoding DetectEncoding(byte[] fileContent)
        {
            if (fileContent.Length == 0)
                return Encoding.UTF8;

            // Check for BOM
            if (fileContent.Length >= 3 && 
                fileContent[0] == 0xEF && fileContent[1] == 0xBB && fileContent[2] == 0xBF)
            {
                return Encoding.UTF8;
            }

            // Try to detect encoding from HTML meta tags
            var sampleText = Encoding.UTF8.GetString(fileContent, 0, Math.Min(1024, fileContent.Length));
            var metaMatch = System.Text.RegularExpressions.Regex.Match(
                sampleText, 
                @"<meta[^>]+charset\s*=\s*[""']?([^""'\s>]+)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (metaMatch.Success)
            {
                try
                {
                    return Encoding.GetEncoding(metaMatch.Groups[1].Value);
                }
                catch (ArgumentException)
                {
                    // Invalid encoding name, fall back to UTF-8
                }
            }

            return Encoding.UTF8;
        }
    }
}
