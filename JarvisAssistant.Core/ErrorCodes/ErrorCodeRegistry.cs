using System.Collections.Immutable;

namespace JarvisAssistant.Core.ErrorCodes
{
    /// <summary>
    /// Centralized registry of all error codes used throughout the Jarvis Assistant system.
    /// Error codes follow the format: SERVICE-CATEGORY-NUMBER
    /// </summary>
    public static class ErrorCodeRegistry
    {
        #region Service Prefixes
        public const string LLM_SERVICE = "LLM";     // Language Model Service
        public const string VCE_SERVICE = "VCE";     // Voice Engine Service  
        public const string CAD_SERVICE = "CAD";     // Computer-Aided Design Service
        public const string VIS_SERVICE = "VIS";     // Visualization Service
        public const string NET_SERVICE = "NET";     // Network Service
        public const string DB_SERVICE = "DB";       // Database Service
        #endregion

        #region Category Suffixes
        public const string CONN_CATEGORY = "CONN";  // Connection errors
        public const string AUTH_CATEGORY = "AUTH";  // Authentication errors
        public const string PROC_CATEGORY = "PROC";  // Processing errors
        public const string MEM_CATEGORY = "MEM";    // Memory errors
        public const string CONF_CATEGORY = "CONF";  // Configuration errors
        #endregion

        #region LLM Service Errors (LLM-*)
        
        // Connection Errors (LLM-CONN-*)
        public const string LLM_CONN_001 = "LLM-CONN-001"; // Connection refused
        public const string LLM_CONN_002 = "LLM-CONN-002"; // Host not found
        public const string LLM_CONN_003 = "LLM-CONN-003"; // Network unreachable
        public const string LLM_CONN_004 = "LLM-CONN-004"; // Connection timeout
        public const string LLM_CONN_005 = "LLM-CONN-005"; // SSL/TLS failure
        
        // Authentication Errors (LLM-AUTH-*)
        public const string LLM_AUTH_001 = "LLM-AUTH-001"; // Invalid API key
        public const string LLM_AUTH_002 = "LLM-AUTH-002"; // Token expired
        public const string LLM_AUTH_003 = "LLM-AUTH-003"; // Insufficient permissions
        
        // Processing Errors (LLM-PROC-*)
        public const string LLM_PROC_001 = "LLM-PROC-001"; // Model not found
        public const string LLM_PROC_002 = "LLM-PROC-002"; // Request timeout
        public const string LLM_PROC_003 = "LLM-PROC-003"; // Invalid request format
        public const string LLM_PROC_004 = "LLM-PROC-004"; // Response parsing error
        public const string LLM_PROC_005 = "LLM-PROC-005"; // Rate limit exceeded
        
        // Memory Errors (LLM-MEM-*)
        public const string LLM_MEM_001 = "LLM-MEM-001";   // Insufficient memory
        public const string LLM_MEM_002 = "LLM-MEM-002";   // Context too large
        
        // Configuration Errors (LLM-CONF-*)
        public const string LLM_CONF_001 = "LLM-CONF-001"; // Missing configuration
        public const string LLM_CONF_002 = "LLM-CONF-002"; // Invalid endpoint URL
        public const string LLM_CONF_003 = "LLM-CONF-003"; // Invalid timeout setting
        
        #endregion

        #region Voice Engine Errors (VCE-*)
        
        // Connection Errors (VCE-CONN-*)
        public const string VCE_CONN_001 = "VCE-CONN-001"; // Voice service unreachable
        public const string VCE_CONN_002 = "VCE-CONN-002"; // Voice API connection failed
        
        // Authentication Errors (VCE-AUTH-*)
        public const string VCE_AUTH_001 = "VCE-AUTH-001"; // Voice API key invalid
        public const string VCE_AUTH_002 = "VCE-AUTH-002"; // Voice quota exceeded
        
        // Processing Errors (VCE-PROC-*)
        public const string VCE_PROC_001 = "VCE-PROC-001"; // Voice synthesis failed
        public const string VCE_PROC_002 = "VCE-PROC-002"; // Audio format not supported
        public const string VCE_PROC_003 = "VCE-PROC-003"; // Voice not available
        public const string VCE_PROC_004 = "VCE-PROC-004"; // Speech recognition failed
        
        // Voice-specific errors
        public const string VCE_SYNTH_001 = "VCE-SYNTH-001"; // Synthesis temporarily unavailable
        
        #endregion

        #region CAD Service Errors (CAD-*)
        
        // Connection Errors (CAD-CONN-*)
        public const string CAD_CONN_001 = "CAD-CONN-001"; // SolidWorks connection failed
        public const string CAD_CONN_002 = "CAD-CONN-002"; // CAD application not running
        
        // Authentication Errors (CAD-AUTH-*)
        public const string CAD_AUTH_001 = "CAD-AUTH-001"; // CAD license not found
        public const string CAD_AUTH_002 = "CAD-AUTH-002"; // CAD permissions denied
        
        // Processing Errors (CAD-PROC-*)
        public const string CAD_PROC_001 = "CAD-PROC-001"; // Model processing failed
        public const string CAD_PROC_002 = "CAD-PROC-002"; // File format not supported
        public const string CAD_PROC_003 = "CAD-PROC-003"; // Assembly error
        
        // CAD-specific errors
        public const string CAD_API_001 = "CAD-API-001";   // SolidWorks API unavailable
        
        #endregion

        #region Visualization Errors (VIS-*)
        
        // Processing Errors (VIS-PROC-*)
        public const string VIS_PROC_001 = "VIS-PROC-001"; // Rendering failed
        public const string VIS_PROC_002 = "VIS-PROC-002"; // Graphics driver error
        public const string VIS_PROC_003 = "VIS-PROC-003"; // Shader compilation failed
        
        // Memory Errors (VIS-MEM-*)
        public const string VIS_MEM_001 = "VIS-MEM-001";   // GPU memory insufficient
        public const string VIS_MEM_002 = "VIS-MEM-002";   // Texture memory exhausted
        
        #endregion

        #region Network Errors (NET-*)
        
        // Connection Errors (NET-CONN-*)
        public const string NET_CONN_001 = "NET-CONN-001"; // Network connectivity issue
        public const string NET_CONN_002 = "NET-CONN-002"; // DNS resolution failed
        public const string NET_CONN_003 = "NET-CONN-003"; // Proxy connection failed
        public const string NET_CONN_004 = "NET-CONN-004"; // Firewall blocking connection
        
        // Authentication Errors (NET-AUTH-*)
        public const string NET_AUTH_001 = "NET-AUTH-001"; // Network authentication failed
        public const string NET_AUTH_002 = "NET-AUTH-002"; // Proxy authentication required
        
        // Processing Errors (NET-PROC-*)
        public const string NET_PROC_001 = "NET-PROC-001"; // HTTP request failed
        public const string NET_PROC_002 = "NET-PROC-002"; // Response parsing error
        public const string NET_PROC_003 = "NET-PROC-003"; // Certificate validation failed
        
        #endregion

        #region Database Errors (DB-*)
        
        // Connection Errors (DB-CONN-*)
        public const string DB_CONN_001 = "DB-CONN-001";   // Database connection failed
        public const string DB_CONN_002 = "DB-CONN-002";   // Connection pool exhausted
        public const string DB_CONN_003 = "DB-CONN-003";   // Database timeout
        
        // Authentication Errors (DB-AUTH-*)
        public const string DB_AUTH_001 = "DB-AUTH-001";   // Database login failed
        public const string DB_AUTH_002 = "DB-AUTH-002";   // Database permissions denied
        
        // Processing Errors (DB-PROC-*)
        public const string DB_PROC_001 = "DB-PROC-001";   // Query execution failed
        public const string DB_PROC_002 = "DB-PROC-002";   // Transaction rollback
        public const string DB_PROC_003 = "DB-PROC-003";   // Data validation error
        
        #endregion

        #region Error Code Metadata
        
        /// <summary>
        /// Gets all error codes organized by service and category.
        /// </summary>
        public static readonly ImmutableDictionary<string, ImmutableList<string>> ErrorCodesByService = 
            new Dictionary<string, ImmutableList<string>>
            {
                [LLM_SERVICE] = ImmutableList.Create(
                    LLM_CONN_001, LLM_CONN_002, LLM_CONN_003, LLM_CONN_004, LLM_CONN_005,
                    LLM_AUTH_001, LLM_AUTH_002, LLM_AUTH_003,
                    LLM_PROC_001, LLM_PROC_002, LLM_PROC_003, LLM_PROC_004, LLM_PROC_005,
                    LLM_MEM_001, LLM_MEM_002,
                    LLM_CONF_001, LLM_CONF_002, LLM_CONF_003
                ),
                [VCE_SERVICE] = ImmutableList.Create(
                    VCE_CONN_001, VCE_CONN_002,
                    VCE_AUTH_001, VCE_AUTH_002,
                    VCE_PROC_001, VCE_PROC_002, VCE_PROC_003, VCE_PROC_004,
                    VCE_SYNTH_001
                ),
                [CAD_SERVICE] = ImmutableList.Create(
                    CAD_CONN_001, CAD_CONN_002,
                    CAD_AUTH_001, CAD_AUTH_002,
                    CAD_PROC_001, CAD_PROC_002, CAD_PROC_003,
                    CAD_API_001
                ),
                [VIS_SERVICE] = ImmutableList.Create(
                    VIS_PROC_001, VIS_PROC_002, VIS_PROC_003,
                    VIS_MEM_001, VIS_MEM_002
                ),
                [NET_SERVICE] = ImmutableList.Create(
                    NET_CONN_001, NET_CONN_002, NET_CONN_003, NET_CONN_004,
                    NET_AUTH_001, NET_AUTH_002,
                    NET_PROC_001, NET_PROC_002, NET_PROC_003
                ),
                [DB_SERVICE] = ImmutableList.Create(
                    DB_CONN_001, DB_CONN_002, DB_CONN_003,
                    DB_AUTH_001, DB_AUTH_002,
                    DB_PROC_001, DB_PROC_002, DB_PROC_003
                )
            }.ToImmutableDictionary();

        /// <summary>
        /// Gets error codes organized by category.
        /// </summary>
        public static readonly ImmutableDictionary<string, ImmutableList<string>> ErrorCodesByCategory = 
            new Dictionary<string, ImmutableList<string>>
            {
                [CONN_CATEGORY] = ImmutableList.Create(
                    LLM_CONN_001, LLM_CONN_002, LLM_CONN_003, LLM_CONN_004, LLM_CONN_005,
                    VCE_CONN_001, VCE_CONN_002,
                    CAD_CONN_001, CAD_CONN_002,
                    NET_CONN_001, NET_CONN_002, NET_CONN_003, NET_CONN_004,
                    DB_CONN_001, DB_CONN_002, DB_CONN_003
                ),
                [AUTH_CATEGORY] = ImmutableList.Create(
                    LLM_AUTH_001, LLM_AUTH_002, LLM_AUTH_003,
                    VCE_AUTH_001, VCE_AUTH_002,
                    CAD_AUTH_001, CAD_AUTH_002,
                    NET_AUTH_001, NET_AUTH_002,
                    DB_AUTH_001, DB_AUTH_002
                ),
                [PROC_CATEGORY] = ImmutableList.Create(
                    LLM_PROC_001, LLM_PROC_002, LLM_PROC_003, LLM_PROC_004, LLM_PROC_005,
                    VCE_PROC_001, VCE_PROC_002, VCE_PROC_003, VCE_PROC_004,
                    CAD_PROC_001, CAD_PROC_002, CAD_PROC_003,
                    VIS_PROC_001, VIS_PROC_002, VIS_PROC_003,
                    NET_PROC_001, NET_PROC_002, NET_PROC_003,
                    DB_PROC_001, DB_PROC_002, DB_PROC_003
                ),
                [MEM_CATEGORY] = ImmutableList.Create(
                    LLM_MEM_001, LLM_MEM_002,
                    VIS_MEM_001, VIS_MEM_002
                ),
                [CONF_CATEGORY] = ImmutableList.Create(
                    LLM_CONF_001, LLM_CONF_002, LLM_CONF_003
                )
            }.ToImmutableDictionary();

        #endregion

        #region Utility Methods

        /// <summary>
        /// Extracts the service prefix from an error code.
        /// </summary>
        /// <param name="errorCode">The error code to parse.</param>
        /// <returns>The service prefix or null if invalid format.</returns>
        public static string? GetServiceFromErrorCode(string errorCode)
        {
            if (string.IsNullOrEmpty(errorCode)) return null;
            
            var parts = errorCode.Split('-');
            return parts.Length >= 3 ? parts[0] : null;
        }

        /// <summary>
        /// Extracts the category from an error code.
        /// </summary>
        /// <param name="errorCode">The error code to parse.</param>
        /// <returns>The category or null if invalid format.</returns>
        public static string? GetCategoryFromErrorCode(string errorCode)
        {
            if (string.IsNullOrEmpty(errorCode)) return null;
            
            var parts = errorCode.Split('-');
            return parts.Length >= 3 ? parts[1] : null;
        }

        /// <summary>
        /// Extracts the error number from an error code.
        /// </summary>
        /// <param name="errorCode">The error code to parse.</param>
        /// <returns>The error number or null if invalid format.</returns>
        public static string? GetNumberFromErrorCode(string errorCode)
        {
            if (string.IsNullOrEmpty(errorCode)) return null;
            
            var parts = errorCode.Split('-');
            return parts.Length >= 3 ? parts[2] : null;
        }

        /// <summary>
        /// Validates that an error code follows the correct format.
        /// </summary>
        /// <param name="errorCode">The error code to validate.</param>
        /// <returns>True if the error code is valid, false otherwise.</returns>
        public static bool IsValidErrorCode(string errorCode)
        {
            if (string.IsNullOrEmpty(errorCode)) return false;
            
            var parts = errorCode.Split('-');
            if (parts.Length != 3) return false;
            
            // Validate service prefix
            var validServices = new[] { LLM_SERVICE, VCE_SERVICE, CAD_SERVICE, VIS_SERVICE, NET_SERVICE, DB_SERVICE };
            if (!validServices.Contains(parts[0])) return false;
            
            // Validate category
            var validCategories = new[] { CONN_CATEGORY, AUTH_CATEGORY, PROC_CATEGORY, MEM_CATEGORY, CONF_CATEGORY };
            if (!validCategories.Contains(parts[1])) return false;
            
            // Validate number format (3 digits)
            return parts[2].Length == 3 && parts[2].All(char.IsDigit);
        }

        #endregion
    }
}
