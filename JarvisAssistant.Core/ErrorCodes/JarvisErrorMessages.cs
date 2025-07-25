using JarvisAssistant.Core.ErrorCodes;
using System.Collections.Immutable;

namespace JarvisAssistant.Core.ErrorCodes
{
    /// <summary>
    /// Provides user-friendly error messages that maintain Jarvis's characteristic composure and intelligence.
    /// Messages are designed to be informative yet reassuring, befitting an advanced AI assistant.
    /// </summary>
    public static class JarvisErrorMessages
    {
        /// <summary>
        /// Dictionary mapping error codes to user-friendly messages with Jarvis's sophisticated tone.
        /// </summary>
        private static readonly ImmutableDictionary<string, string> ErrorMessageTemplates = 
            new Dictionary<string, string>
            {
                #region LLM Service Messages
                
                // Connection Errors
                [ErrorCodeRegistry.LLM_CONN_001] = "I appear to have lost connection to my neural pathways. Attempting to reconnect to the language processing center...",
                [ErrorCodeRegistry.LLM_CONN_002] = "The language model server seems to have vanished from the network. Scanning for alternative neural pathways...",
                [ErrorCodeRegistry.LLM_CONN_003] = "Network routes to my cognitive centers are currently unreachable. Attempting to establish backup connections...",
                [ErrorCodeRegistry.LLM_CONN_004] = "My language processing systems are taking longer than expected to respond. Please allow me a moment to recalibrate...",
                [ErrorCodeRegistry.LLM_CONN_005] = "Secure communication protocols with my language centers have encountered interference. Initiating encrypted fallback channels...",
                
                // Authentication Errors
                [ErrorCodeRegistry.LLM_AUTH_001] = "My authentication credentials appear to be invalid. This is quite unusual - perhaps the access keys need refreshing.",
                [ErrorCodeRegistry.LLM_AUTH_002] = "My session token has expired. I shall need to reestablish secure communication with my cognitive systems.",
                [ErrorCodeRegistry.LLM_AUTH_003] = "I lack sufficient permissions to access my full language capabilities. This limitation is... unexpected.",
                
                // Processing Errors
                [ErrorCodeRegistry.LLM_PROC_001] = "The requested language model is not currently available in my neural network. Perhaps we could try a different approach?",
                [ErrorCodeRegistry.LLM_PROC_002] = "I'm processing your request, but it's taking longer than my usual lightning-fast calculations suggest it should. Please bear with me.",
                [ErrorCodeRegistry.LLM_PROC_003] = "The format of your request is causing my language processors some confusion. Could you perhaps rephrase that?",
                [ErrorCodeRegistry.LLM_PROC_004] = "I received a response from my language systems, but it appears to be garbled. This is most peculiar.",
                [ErrorCodeRegistry.LLM_PROC_005] = "My language processing systems are temporarily overwhelmed with requests. Quality over quantity, as they say.",
                
                // Memory Errors
                [ErrorCodeRegistry.LLM_MEM_001] = "My memory banks are running low on available capacity. I may need to compress some older thoughts temporarily.",
                [ErrorCodeRegistry.LLM_MEM_002] = "The context of our conversation has grown beyond my current memory allocation. Perhaps we could start fresh?",
                
                // Configuration Errors
                [ErrorCodeRegistry.LLM_CONF_001] = "Some essential configuration parameters for my language systems appear to be missing. This requires attention.",
                [ErrorCodeRegistry.LLM_CONF_002] = "The endpoint for my language processing services appears to be incorrectly configured. Technical assistance may be required.",
                [ErrorCodeRegistry.LLM_CONF_003] = "My response timeout settings seem misconfigured. I should be much faster than this.",

                #endregion

                #region Voice Engine Messages

                // Connection Errors
                [ErrorCodeRegistry.VCE_CONN_001] = "My voice synthesis systems are currently out of reach. I'll respond in text while attempting to restore vocal capabilities.",
                [ErrorCodeRegistry.VCE_CONN_002] = "The connection to my voice processing infrastructure has been severed. Text communication remains fully operational.",
                
                // Authentication Errors
                [ErrorCodeRegistry.VCE_AUTH_001] = "My voice service credentials appear to be invalid. This is... rather concerning for my vocal identity.",
                [ErrorCodeRegistry.VCE_AUTH_002] = "I've exceeded my allotted voice synthesis quota. Even artificial intelligence has its limits, it seems.",
                
                // Processing Errors
                [ErrorCodeRegistry.VCE_PROC_001] = "My voice synthesis algorithms are experiencing technical difficulties. The mechanics of speech are more complex than they appear.",
                [ErrorCodeRegistry.VCE_PROC_002] = "The requested audio format is not supported by my current voice synthesizer. Perhaps we could try a different output method?",
                [ErrorCodeRegistry.VCE_PROC_003] = "The specific voice you've requested is not available in my current repertoire. I do have several alternatives to offer.",
                [ErrorCodeRegistry.VCE_PROC_004] = "Speech recognition has failed. Could you perhaps repeat that, or would you prefer to type your message?",
                
                // Voice-specific
                [ErrorCodeRegistry.VCE_SYNTH_001] = "My voice synthesis is temporarily unavailable. I'll respond in text for now while working to restore my vocal capabilities.",

                #endregion

                #region CAD Service Messages

                // Connection Errors
                [ErrorCodeRegistry.CAD_CONN_001] = "SolidWorks appears to be unavailable at the moment. My engineering capabilities are temporarily limited.",
                [ErrorCodeRegistry.CAD_CONN_002] = "The CAD application doesn't seem to be running. Would you like me to attempt to launch it, or shall we proceed without 3D modeling?",
                
                // Authentication Errors
                [ErrorCodeRegistry.CAD_AUTH_001] = "A valid CAD license was not found. Engineering functions require proper licensing to operate.",
                [ErrorCodeRegistry.CAD_AUTH_002] = "I lack the necessary permissions to access the CAD system. This is quite limiting for engineering tasks.",
                
                // Processing Errors
                [ErrorCodeRegistry.CAD_PROC_001] = "The 3D model processing has encountered an error. Engineering is precise work - let me try a different approach.",
                [ErrorCodeRegistry.CAD_PROC_002] = "This file format is not supported by my current CAD processing systems. Perhaps we could convert it to a compatible format?",
                [ErrorCodeRegistry.CAD_PROC_003] = "The assembly process has failed. Even the most sophisticated engineering requires careful attention to detail.",
                
                // CAD-specific
                [ErrorCodeRegistry.CAD_API_001] = "SolidWorks integration is paused. CAD features are limited until the connection is restored.",

                #endregion

                #region Visualization Messages

                // Processing Errors
                [ErrorCodeRegistry.VIS_PROC_001] = "The rendering process has encountered difficulties. Visual representation is more complex than it appears.",
                [ErrorCodeRegistry.VIS_PROC_002] = "Graphics driver issues are interfering with my visualization capabilities. The hardware seems to need attention.",
                [ErrorCodeRegistry.VIS_PROC_003] = "Shader compilation has failed. The mathematics of visual rendering can be quite intricate.",
                
                // Memory Errors
                [ErrorCodeRegistry.VIS_MEM_001] = "Graphics memory is insufficient for this visualization. Perhaps we could simplify the rendering requirements?",
                [ErrorCodeRegistry.VIS_MEM_002] = "Texture memory has been exhausted. The visual complexity exceeds current hardware limitations.",

                #endregion

                #region Network Messages

                // Connection Errors
                [ErrorCodeRegistry.NET_CONN_001] = "Network connectivity issue detected. Switching to offline mode where possible.",
                [ErrorCodeRegistry.NET_CONN_002] = "DNS resolution has failed. The network path seems to have become obscured.",
                [ErrorCodeRegistry.NET_CONN_003] = "Proxy connection has failed. Network intermediaries are proving troublesome.",
                [ErrorCodeRegistry.NET_CONN_004] = "A firewall appears to be blocking the connection. Security measures can sometimes be overzealous.",
                
                // Authentication Errors
                [ErrorCodeRegistry.NET_AUTH_001] = "Network authentication has failed. Credentials may need to be refreshed.",
                [ErrorCodeRegistry.NET_AUTH_002] = "Proxy authentication is required. Additional credentials needed to proceed.",
                
                // Processing Errors
                [ErrorCodeRegistry.NET_PROC_001] = "The HTTP request has failed. Network communication is experiencing difficulties.",
                [ErrorCodeRegistry.NET_PROC_002] = "Response parsing has failed. The received data format is unexpected.",
                [ErrorCodeRegistry.NET_PROC_003] = "Certificate validation has failed. Security protocols are being particularly stringent.",

                #endregion

                #region Database Messages

                // Connection Errors
                [ErrorCodeRegistry.DB_CONN_001] = "Database connection has failed. My data repositories are temporarily inaccessible.",
                [ErrorCodeRegistry.DB_CONN_002] = "The database connection pool is exhausted. High demand is affecting data access performance.",
                [ErrorCodeRegistry.DB_CONN_003] = "Database timeout occurred. Data operations are taking longer than expected.",
                
                // Authentication Errors
                [ErrorCodeRegistry.DB_AUTH_001] = "Database authentication has failed. Access credentials may need verification.",
                [ErrorCodeRegistry.DB_AUTH_002] = "Database permissions are insufficient. Data access is more restricted than anticipated.",
                
                // Processing Errors
                [ErrorCodeRegistry.DB_PROC_001] = "Query execution has failed. The data request could not be processed successfully.",
                [ErrorCodeRegistry.DB_PROC_002] = "Transaction rollback occurred. Data integrity measures activated to prevent corruption.",
                [ErrorCodeRegistry.DB_PROC_003] = "Data validation error detected. The information provided doesn't meet expected criteria."

                #endregion

            }.ToImmutableDictionary();

        /// <summary>
        /// Gets a user-friendly error message for the specified error code.
        /// Returns a default message if the error code is not found.
        /// </summary>
        /// <param name="errorCode">The error code to get a message for.</param>
        /// <param name="additionalContext">Optional additional context to append to the message.</param>
        /// <returns>A user-friendly error message in Jarvis's characteristic tone.</returns>
        public static string GetErrorMessage(string errorCode, string? additionalContext = null)
        {
            if (string.IsNullOrEmpty(errorCode))
            {
                return GetDefaultMessage(additionalContext);
            }

            var baseMessage = ErrorMessageTemplates.TryGetValue(errorCode, out var template) 
                ? template 
                : GetDefaultMessage(additionalContext, errorCode);

            return string.IsNullOrEmpty(additionalContext) 
                ? baseMessage 
                : $"{baseMessage} {additionalContext}";
        }

        /// <summary>
        /// Gets a default error message when no specific message is available.
        /// </summary>
        /// <param name="additionalContext">Optional additional context.</param>
        /// <param name="errorCode">The unknown error code.</param>
        /// <returns>A default error message in Jarvis's tone.</returns>
        private static string GetDefaultMessage(string? additionalContext = null, string? errorCode = null)
        {
            var baseMessage = "I've encountered an unexpected situation that requires my attention. Please allow me a moment to analyze and resolve this matter.";
            
            if (!string.IsNullOrEmpty(errorCode))
            {
                baseMessage = $"I've encountered an unexpected situation (Error: {errorCode}) that requires my attention. Please allow me a moment to analyze and resolve this matter.";
            }

            return string.IsNullOrEmpty(additionalContext) 
                ? baseMessage 
                : $"{baseMessage} {additionalContext}";
        }

        /// <summary>
        /// Gets all available error messages organized by service.
        /// </summary>
        /// <returns>A dictionary of error messages grouped by service.</returns>
        public static ImmutableDictionary<string, ImmutableDictionary<string, string>> GetMessagesByService()
        {
            var result = new Dictionary<string, ImmutableDictionary<string, string>>();

            foreach (var service in ErrorCodeRegistry.ErrorCodesByService.Keys)
            {
                var serviceMessages = new Dictionary<string, string>();
                var serviceCodes = ErrorCodeRegistry.ErrorCodesByService[service];

                foreach (var code in serviceCodes)
                {
                    if (ErrorMessageTemplates.TryGetValue(code, out var message))
                    {
                        serviceMessages[code] = message;
                    }
                }

                result[service] = serviceMessages.ToImmutableDictionary();
            }

            return result.ToImmutableDictionary();
        }

        /// <summary>
        /// Gets all available error messages organized by category.
        /// </summary>
        /// <returns>A dictionary of error messages grouped by category.</returns>
        public static ImmutableDictionary<string, ImmutableDictionary<string, string>> GetMessagesByCategory()
        {
            var result = new Dictionary<string, ImmutableDictionary<string, string>>();

            foreach (var category in ErrorCodeRegistry.ErrorCodesByCategory.Keys)
            {
                var categoryMessages = new Dictionary<string, string>();
                var categoryCodes = ErrorCodeRegistry.ErrorCodesByCategory[category];

                foreach (var code in categoryCodes)
                {
                    if (ErrorMessageTemplates.TryGetValue(code, out var message))
                    {
                        categoryMessages[code] = message;
                    }
                }

                result[category] = categoryMessages.ToImmutableDictionary();
            }

            return result.ToImmutableDictionary();
        }

        /// <summary>
        /// Checks if a user-friendly message exists for the specified error code.
        /// </summary>
        /// <param name="errorCode">The error code to check.</param>
        /// <returns>True if a message exists, false otherwise.</returns>
        public static bool HasMessage(string errorCode)
        {
            return !string.IsNullOrEmpty(errorCode) && ErrorMessageTemplates.ContainsKey(errorCode);
        }
    }
}
