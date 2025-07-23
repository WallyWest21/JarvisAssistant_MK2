using JarvisAssistant.Core.Interfaces;
using JarvisAssistant.Core.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace JarvisAssistant.Services
{
    /// <summary>
    /// Processes voice commands by classifying them and routing to appropriate handlers.
    /// </summary>
    public class VoiceCommandProcessor : IVoiceCommandProcessor
    {
        private readonly ILogger<VoiceCommandProcessor> _logger;
        private readonly ConcurrentDictionary<VoiceCommandType, Func<VoiceCommand, CancellationToken, Task<VoiceCommandResult>>> _commandHandlers;
        private readonly ConcurrentDictionary<VoiceCommandType, List<string>> _commandPatterns;
        private readonly Dictionary<string, object> _statistics;
        private bool _isProcessing;

        /// <summary>
        /// Initializes a new instance of the <see cref="VoiceCommandProcessor"/> class.
        /// </summary>
        /// <param name="logger">The logger instance.</param>
        public VoiceCommandProcessor(ILogger<VoiceCommandProcessor> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _commandHandlers = new ConcurrentDictionary<VoiceCommandType, Func<VoiceCommand, CancellationToken, Task<VoiceCommandResult>>>();
            _commandPatterns = new ConcurrentDictionary<VoiceCommandType, List<string>>();
            _statistics = new Dictionary<string, object>();

            InitializeDefaultPatterns();
            InitializeDefaultHandlers();
            InitializeStatistics();
        }

        /// <inheritdoc/>
        public event EventHandler<VoiceCommandReceivedEventArgs>? CommandReceived;

        /// <inheritdoc/>
        public event EventHandler<VoiceCommandProcessedEventArgs>? CommandProcessed;

        /// <inheritdoc/>
        public bool IsProcessing => _isProcessing;

        /// <inheritdoc/>
        public IReadOnlyList<VoiceCommandType> SupportedCommands => 
            _commandHandlers.Keys.ToList().AsReadOnly();

        /// <inheritdoc/>
        public async Task<VoiceCommand> ClassifyCommandAsync(string commandText, Dictionary<string, object>? context = null, CancellationToken cancellationToken = default)
        {
            var command = new VoiceCommand
            {
                Text = commandText,
                Source = VoiceCommandSource.Manual,
                Timestamp = DateTime.UtcNow,
                RecognitionConfidence = 0.9f, // Assume high confidence for text input
                DetectedLanguage = "en-US"
            };

            if (string.IsNullOrWhiteSpace(commandText))
            {
                _logger.LogWarning("Empty command text provided for classification");
                return command;
            }

            try
            {
                _logger.LogDebug("Classifying command: {Command}", commandText);

                var normalizedText = commandText.ToLowerInvariant().Trim();
                var bestMatch = VoiceCommandType.Unknown;
                var bestConfidence = 0.0f;
                var extractedParameters = new Dictionary<string, object>();

                // Define processing order to prioritize specific patterns over broad ones
                var processingOrder = new[]
                {
                    VoiceCommandType.Settings,    // Process settings first (more specific)
                    VoiceCommandType.Status,      // Process status second (more specific)
                    VoiceCommandType.GenerateCode,
                    VoiceCommandType.Analyze,
                    VoiceCommandType.Help,
                    VoiceCommandType.Stop,
                    VoiceCommandType.Exit,
                    VoiceCommandType.Repeat,
                    VoiceCommandType.Search,      // Process search before chat (more specific)
                    VoiceCommandType.Chat,        // Process chat before navigate (more specific questions)
                    VoiceCommandType.Navigate     // Process navigate last (most broad)
                };

                // Try to match against each command type in priority order
                foreach (var commandType in processingOrder)
                {
                    if (!_commandPatterns.TryGetValue(commandType, out var patterns))
                        continue;

                    foreach (var pattern in patterns)
                    {
                        var confidence = CalculatePatternMatch(normalizedText, pattern, out var parameters);
                        if (confidence > bestConfidence)
                        {
                            bestMatch = commandType;
                            bestConfidence = confidence;
                            extractedParameters = parameters;
                        }
                    }

                    // If we found a high-confidence match for a specific pattern, use it
                    // This prevents broad patterns from overriding specific ones
                    if (bestConfidence >= 0.8f && IsSpecificPattern(commandType))
                    {
                        break;
                    }
                }

                // Update statistics
                IncrementStatistic("total_classifications");
                if (bestMatch != VoiceCommandType.Unknown)
                {
                    IncrementStatistic($"classified_as_{bestMatch.ToString().ToLower()}");
                }

                var classifiedCommand = command.WithClassification(bestMatch, bestConfidence, extractedParameters);
                
                _logger.LogDebug("Command classified as {Type} with confidence {Confidence:P1}", 
                    bestMatch, bestConfidence);

                return classifiedCommand;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error classifying command: {Command}", commandText);
                return command;
            }
        }

        /// <inheritdoc/>
        public async Task<VoiceCommandResult> ProcessCommandAsync(VoiceCommand command, CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                _isProcessing = true;
                OnCommandReceived(command);

                _logger.LogInformation("Processing voice command: {Command}", command.ToLogString());

                if (!command.IsValid)
                {
                    var errorResult = VoiceCommandResult.CreateError("I didn't understand that command. Could you please repeat?");
                    await CompleteProcessing(command, errorResult, stopwatch.Elapsed);
                    return errorResult;
                }

                // Check if we have a handler for this command type
                if (!_commandHandlers.TryGetValue(command.CommandType, out var handler))
                {
                    var errorMessage = $"I don't know how to handle '{command.CommandType}' commands yet.";
                    var errorResult = VoiceCommandResult.CreateError(errorMessage);
                    await CompleteProcessing(command, errorResult, stopwatch.Elapsed);
                    return errorResult;
                }

                // Execute the command handler
                var result = await handler(command, cancellationToken);
                result.ProcessingTimeMs = (int)stopwatch.ElapsedMilliseconds;

                IncrementStatistic("total_processed");
                if (result.Success)
                {
                    IncrementStatistic("successful_processed");
                }
                else
                {
                    IncrementStatistic("failed_processed");
                }

                await CompleteProcessing(command, result, stopwatch.Elapsed);
                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Command processing was cancelled");
                var cancelResult = VoiceCommandResult.CreateError("Command processing was cancelled", false);
                await CompleteProcessing(command, cancelResult, stopwatch.Elapsed);
                return cancelResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing command: {Command}", command.Text);
                var errorResult = VoiceCommandResult.CreateError("An error occurred while processing your command");
                await CompleteProcessing(command, errorResult, stopwatch.Elapsed);
                return errorResult;
            }
            finally
            {
                _isProcessing = false;
                stopwatch.Stop();
            }
        }

        /// <inheritdoc/>
        public async Task<VoiceCommandResult> ProcessTextCommandAsync(string commandText, VoiceCommandSource source, Dictionary<string, object>? context = null, CancellationToken cancellationToken = default)
        {
            var command = await ClassifyCommandAsync(commandText, context, cancellationToken);
            command.Source = source;
            
            return await ProcessCommandAsync(command, cancellationToken);
        }

        /// <inheritdoc/>
        public void RegisterCommandHandler(VoiceCommandType commandType, Func<VoiceCommand, CancellationToken, Task<VoiceCommandResult>> handler)
        {
            _commandHandlers.AddOrUpdate(commandType, handler, (key, oldValue) => handler);
            _logger.LogDebug("Registered handler for command type: {CommandType}", commandType);
        }

        /// <inheritdoc/>
        public void UnregisterCommandHandler(VoiceCommandType commandType)
        {
            _commandHandlers.TryRemove(commandType, out _);
            _logger.LogDebug("Unregistered handler for command type: {CommandType}", commandType);
        }

        /// <inheritdoc/>
        public IReadOnlyList<string> GetCommandPatterns(VoiceCommandType commandType)
        {
            return _commandPatterns.TryGetValue(commandType, out var patterns) 
                ? patterns.AsReadOnly() 
                : new List<string>().AsReadOnly();
        }

        /// <inheritdoc/>
        public void UpdateCommandPatterns(VoiceCommandType commandType, IEnumerable<string> patterns)
        {
            _commandPatterns.AddOrUpdate(commandType, patterns.ToList(), (key, oldValue) => patterns.ToList());
            _logger.LogDebug("Updated patterns for command type {CommandType}: {Count} patterns", 
                commandType, patterns.Count());
        }

        /// <inheritdoc/>
        public Dictionary<string, object> GetProcessingStatistics()
        {
            lock (_statistics)
            {
                return new Dictionary<string, object>(_statistics);
            }
        }

        /// <inheritdoc/>
        public void ClearStatistics()
        {
            lock (_statistics)
            {
                _statistics.Clear();
                InitializeStatistics();
            }
            _logger.LogDebug("Processing statistics cleared");
        }

        private void InitializeDefaultPatterns()
        {
            _commandPatterns[VoiceCommandType.Status] = new List<string>
            {
                @"(?:what'?s|show|tell me|check) (?:my|the|your) status",
                @"status (?:check|report|update)",
                @"how (?:are you|is everything)",
                @"system status",
                @"health check",
                @"show status"
            };

            _commandPatterns[VoiceCommandType.GenerateCode] = new List<string>
            {
                @"(?:generate|create|write|make) (?:some )?code",
                @"(?:generate|create|write|make) (?:a |an )?(?:function|method|class|component)",
                @"code (?:generation|creator)",
                @"write (?:me )?(?:a |some )?(?:code|function|method)",
                @"create (?:a |an )?(?:new )?(?:function|method|class|component)"
            };

            _commandPatterns[VoiceCommandType.Analyze] = new List<string>
            {
                @"(?:analyze|examine|check|review|inspect) (?:this|that|the|my)",
                @"(?:what|how) (?:does|is) (?:this|that)",
                @"(?:explain|describe|tell me about) (?:this|that|the)",
                @"analysis (?:of|for)",
                @"code (?:analysis|review)"
            };

            _commandPatterns[VoiceCommandType.Search] = new List<string>
            {
                @"(?:search|find|look for) (.+)",
                @"(?:show me|find me) (.+)"
            };

            _commandPatterns[VoiceCommandType.Settings] = new List<string>
            {
                @"(?:open|show|change|configure) (?:the )?settings",
                @"(?:preferences|configuration|options)",
                @"(?:change|modify|update) (?:my |the )?(?:settings|preferences|config)"
            };

            _commandPatterns[VoiceCommandType.Navigate] = new List<string>
            {
                @"(?:go to|navigate to) (?:the )?(.+)",
                @"(?:take me to|bring up|display) (?:the )?(.+)",
                @"(?:switch to|change to) (?:the )?(.+)"
            };

            _commandPatterns[VoiceCommandType.Help] = new List<string>
            {
                @"(?:help|assist|support)",
                @"(?:what can you do|what are your capabilities)",
                @"(?:how do I|how can I|show me how to) (.+)",
                @"(?:instructions|guide|tutorial)"
            };

            _commandPatterns[VoiceCommandType.Stop] = new List<string>
            {
                @"(?:stop|halt|cancel|abort)",
                @"(?:stop that|stop it|cancel that)",
                @"(?:never mind|forget it)"
            };

            _commandPatterns[VoiceCommandType.Exit] = new List<string>
            {
                @"(?:exit|quit|close|goodbye|bye)",
                @"(?:shut down|turn off)",
                @"(?:close the app|close application)"
            };

            _commandPatterns[VoiceCommandType.Repeat] = new List<string>
            {
                @"(?:repeat|say that again|what was that)",
                @"(?:repeat that|say it again)",
                @"(?:could you repeat|can you repeat)"
            };

            _commandPatterns[VoiceCommandType.Chat] = new List<string>
            {
                @"(?:what|where) (?:is|are) (?:the )?(?:weather|temperature|time|date)",
                @"(?:tell me|what|how|why|when|where) (?:is |are |about |the )?(.+)",
                @"(?:can you|could you|would you) (.+)",
                @"(?:I want to know|I'm curious about) (.+)"
            };
        }

        private void InitializeDefaultHandlers()
        {
            RegisterCommandHandler(VoiceCommandType.Status, HandleStatusCommand);
            RegisterCommandHandler(VoiceCommandType.GenerateCode, HandleGenerateCodeCommand);
            RegisterCommandHandler(VoiceCommandType.Analyze, HandleAnalyzeCommand);
            RegisterCommandHandler(VoiceCommandType.Navigate, HandleNavigateCommand);
            RegisterCommandHandler(VoiceCommandType.Search, HandleSearchCommand);
            RegisterCommandHandler(VoiceCommandType.Settings, HandleSettingsCommand);
            RegisterCommandHandler(VoiceCommandType.Help, HandleHelpCommand);
            RegisterCommandHandler(VoiceCommandType.Stop, HandleStopCommand);
            RegisterCommandHandler(VoiceCommandType.Exit, HandleExitCommand);
            RegisterCommandHandler(VoiceCommandType.Repeat, HandleRepeatCommand);
            RegisterCommandHandler(VoiceCommandType.Chat, HandleChatCommand);
        }

        private void InitializeStatistics()
        {
            _statistics["total_classifications"] = 0;
            _statistics["total_processed"] = 0;
            _statistics["successful_processed"] = 0;
            _statistics["failed_processed"] = 0;
            _statistics["average_processing_time_ms"] = 0.0;
            _statistics["last_reset"] = DateTime.UtcNow;
        }

        private float CalculatePatternMatch(string text, string pattern, out Dictionary<string, object> parameters)
        {
            parameters = new Dictionary<string, object>();

            try
            {
                var regex = new Regex(pattern, RegexOptions.IgnoreCase);
                var match = regex.Match(text);

                if (match.Success)
                {
                    // Calculate confidence based on match coverage and specificity
                    var matchedLength = match.Value.Length;
                    var totalLength = text.Length;
                    var coverage = (float)matchedLength / totalLength;

                    // Extract named groups as parameters
                    for (int i = 1; i < match.Groups.Count; i++)
                    {
                        var group = match.Groups[i];
                        if (group.Success)
                        {
                            parameters[$"param{i}"] = group.Value.Trim();
                        }
                    }

                    // Boost confidence for exact matches and specific terms
                    var confidence = coverage;
                    
                    // Give bonus for high coverage (complete or near-complete matches)
                    if (coverage >= 0.9f)
                    {
                        confidence = Math.Min(0.95f, confidence + 0.1f);
                    }
                    else if (coverage >= 0.7f)
                    {
                        confidence = Math.Min(0.9f, confidence + 0.05f);
                    }

                    // Boost confidence for specific keywords
                    if (ContainsSpecificKeywords(text, pattern))
                    {
                        confidence = Math.Min(0.95f, confidence + 0.1f);
                    }

                    return Math.Max(0.1f, Math.Min(0.95f, confidence));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error matching pattern {Pattern} against text {Text}", pattern, text);
            }

            return 0.0f;
        }

        private bool ContainsSpecificKeywords(string text, string pattern)
        {
            // Check if the text contains specific keywords that should boost confidence
            var specificKeywords = new[]
            {
                "settings", "preferences", "configuration", "options",
                "status", "health", "system",
                "generate", "create", "write", "code",
                "help", "assist", "support",
                "stop", "halt", "cancel",
                "exit", "quit", "goodbye", "bye"
            };

            return specificKeywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        private async Task<VoiceCommandResult> HandleStatusCommand(VoiceCommand command, CancellationToken cancellationToken)
        {
            await Task.Delay(100, cancellationToken); // Simulate processing

            var response = "All systems are running normally. Voice mode is active and listening for commands.";
            return VoiceCommandResult.CreateSuccess(response);
        }

        private async Task<VoiceCommandResult> HandleGenerateCodeCommand(VoiceCommand command, CancellationToken cancellationToken)
        {
            await Task.Delay(200, cancellationToken); // Simulate processing

            var response = "I'd be happy to help you generate code. What type of code would you like me to create?";
            return VoiceCommandResult.CreateSuccess(response);
        }

        private async Task<VoiceCommandResult> HandleAnalyzeCommand(VoiceCommand command, CancellationToken cancellationToken)
        {
            await Task.Delay(150, cancellationToken); // Simulate processing

            var response = "I can analyze code, data, or other content for you. What would you like me to analyze?";
            return VoiceCommandResult.CreateSuccess(response);
        }

        private async Task<VoiceCommandResult> HandleNavigateCommand(VoiceCommand command, CancellationToken cancellationToken)
        {
            await Task.Delay(100, cancellationToken); // Simulate processing

            if (command.Parameters.TryGetValue("param1", out var target))
            {
                var response = $"Navigating to {target}.";
                return VoiceCommandResult.CreateSuccess(response);
            }

            var defaultResponse = "Where would you like me to navigate?";
            return VoiceCommandResult.CreateSuccess(defaultResponse);
        }

        private async Task<VoiceCommandResult> HandleSearchCommand(VoiceCommand command, CancellationToken cancellationToken)
        {
            await Task.Delay(200, cancellationToken); // Simulate processing

            if (command.Parameters.TryGetValue("param1", out var searchTerm))
            {
                var response = $"Searching for {searchTerm}...";
                return VoiceCommandResult.CreateSuccess(response);
            }

            var defaultResponse = "What would you like me to search for?";
            return VoiceCommandResult.CreateSuccess(defaultResponse);
        }

        private async Task<VoiceCommandResult> HandleSettingsCommand(VoiceCommand command, CancellationToken cancellationToken)
        {
            await Task.Delay(100, cancellationToken); // Simulate processing

            var response = "Opening settings. You can configure voice mode, themes, and other preferences here.";
            return VoiceCommandResult.CreateSuccess(response);
        }

        private async Task<VoiceCommandResult> HandleHelpCommand(VoiceCommand command, CancellationToken cancellationToken)
        {
            await Task.Delay(100, cancellationToken); // Simulate processing

            var response = "I can help you with status checks, code generation, analysis, navigation, search, and more. " +
                          "Just say 'Hey Jarvis' followed by your request.";
            return VoiceCommandResult.CreateSuccess(response);
        }

        private async Task<VoiceCommandResult> HandleStopCommand(VoiceCommand command, CancellationToken cancellationToken)
        {
            await Task.Delay(50, cancellationToken); // Simulate processing

            var response = "Stopping current operation.";
            return VoiceCommandResult.CreateSuccess(response);
        }

        private async Task<VoiceCommandResult> HandleExitCommand(VoiceCommand command, CancellationToken cancellationToken)
        {
            await Task.Delay(50, cancellationToken); // Simulate processing

            var response = "Goodbye! See you later.";
            return VoiceCommandResult.CreateSuccess(response);
        }

        private async Task<VoiceCommandResult> HandleRepeatCommand(VoiceCommand command, CancellationToken cancellationToken)
        {
            await Task.Delay(50, cancellationToken); // Simulate processing

            // In a real implementation, this would repeat the last response
            var response = "I said: All systems are running normally.";
            return VoiceCommandResult.CreateSuccess(response);
        }

        private async Task<VoiceCommandResult> HandleChatCommand(VoiceCommand command, CancellationToken cancellationToken)
        {
            await Task.Delay(300, cancellationToken); // Simulate processing

            var response = "That's an interesting question. I'm still learning how to have conversations. " +
                          "Is there something specific I can help you with?";
            return VoiceCommandResult.CreateSuccess(response);
        }

        private async Task CompleteProcessing(VoiceCommand command, VoiceCommandResult result, TimeSpan processingTime)
        {
            // Update processing time statistics
            UpdateAverageProcessingTime(processingTime.TotalMilliseconds);

            OnCommandProcessed(command, result, processingTime);
        }

        private void OnCommandReceived(VoiceCommand command)
        {
            CommandReceived?.Invoke(this, new VoiceCommandReceivedEventArgs
            {
                Command = command,
                ReceivedAt = DateTime.UtcNow
            });
        }

        private void OnCommandProcessed(VoiceCommand command, VoiceCommandResult result, TimeSpan processingTime)
        {
            CommandProcessed?.Invoke(this, new VoiceCommandProcessedEventArgs
            {
                Command = command,
                Result = result,
                ProcessedAt = DateTime.UtcNow,
                ProcessingTime = processingTime
            });
        }

        private void IncrementStatistic(string key)
        {
            lock (_statistics)
            {
                if (_statistics.TryGetValue(key, out var value) && value is int intValue)
                {
                    _statistics[key] = intValue + 1;
                }
                else
                {
                    _statistics[key] = 1;
                }
            }
        }

        private void UpdateAverageProcessingTime(double processingTimeMs)
        {
            lock (_statistics)
            {
                if (_statistics.TryGetValue("average_processing_time_ms", out var avgValue) && avgValue is double currentAvg &&
                    _statistics.TryGetValue("total_processed", out var countValue) && countValue is int count)
                {
                    var newAvg = ((currentAvg * (count - 1)) + processingTimeMs) / count;
                    _statistics["average_processing_time_ms"] = newAvg;
                }
                else
                {
                    _statistics["average_processing_time_ms"] = processingTimeMs;
                }
            }
        }

        private bool IsSpecificPattern(VoiceCommandType commandType)
        {
            // These command types have specific patterns that should take precedence
            return commandType switch
            {
                VoiceCommandType.Settings => true,
                VoiceCommandType.Status => true,
                VoiceCommandType.GenerateCode => true,
                VoiceCommandType.Analyze => true,
                VoiceCommandType.Help => true,
                VoiceCommandType.Stop => true,
                VoiceCommandType.Exit => true,
                VoiceCommandType.Repeat => true,
                _ => false
            };
        }
    }
}
