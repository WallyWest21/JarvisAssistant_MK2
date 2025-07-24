namespace JarvisAssistant.Services.LLM
{
    /// <summary>
    /// Static class containing all LLM error codes used throughout the system.
    /// </summary>
    public static class LLMErrorCodes
    {
        // HTTP Status Code Errors
        public const string HTTP_400_BAD_REQUEST = "LLM-HTTP-400-001";
        public const string HTTP_401_UNAUTHORIZED = "LLM-HTTP-401-001";
        public const string HTTP_403_FORBIDDEN = "LLM-HTTP-403-001";
        public const string HTTP_404_NOT_FOUND = "LLM-HTTP-404-001";
        public const string HTTP_408_REQUEST_TIMEOUT = "LLM-HTTP-408-001";
        public const string HTTP_429_RATE_LIMITED = "LLM-HTTP-429-001";
        public const string HTTP_500_INTERNAL_ERROR = "LLM-HTTP-500-001";
        public const string HTTP_502_BAD_GATEWAY = "LLM-HTTP-502-001";
        public const string HTTP_503_SERVICE_UNAVAILABLE = "LLM-HTTP-503-001";
        public const string HTTP_504_GATEWAY_TIMEOUT = "LLM-HTTP-504-001";
        public const string HTTP_GENERIC = "LLM-HTTP-GENERIC-001";

        // Connection Errors
        public const string CONN_REFUSED = "LLM-CONN-001";
        public const string CONN_HOST_NOT_FOUND = "LLM-CONN-002";
        public const string CONN_NETWORK_UNREACHABLE = "LLM-CONN-003";
        public const string CONN_TIMEOUT = "LLM-CONN-004";
        public const string CONN_SSL_FAILURE = "LLM-CONN-005";

        // Request/Response Errors
        public const string REQ_TIMEOUT = "LLM-REQ-001";
        public const string REQ_CANCELLED = "LLM-REQ-002";
        public const string RESP_INVALID_JSON = "LLM-RESP-001";
        public const string RESP_EMPTY = "LLM-RESP-002";
        public const string RESP_TOO_LARGE = "LLM-RESP-003";

        // Streaming Errors
        public const string STREAM_CONNECTION_DROPPED = "LLM-STREAM-001";
        public const string STREAM_INVALID_FORMAT = "LLM-STREAM-002";
        public const string STREAM_TIMEOUT = "LLM-STREAM-003";

        // Model Errors
        public const string MODEL_NOT_FOUND = "LLM-MODEL-001";
        public const string MODEL_UNAVAILABLE = "LLM-MODEL-002";
        public const string MODEL_LOADING = "LLM-MODEL-003";

        // Resource Errors
        public const string RESOURCE_OUT_OF_MEMORY = "LLM-RESOURCE-001";
        public const string RESOURCE_DISK_FULL = "LLM-RESOURCE-002";
        public const string RESOURCE_CPU_OVERLOAD = "LLM-RESOURCE-003";

        // Configuration Errors
        public const string CONFIG_INVALID_URL = "LLM-CONFIG-001";
        public const string CONFIG_INVALID_TIMEOUT = "LLM-CONFIG-002";
        public const string CONFIG_MISSING_PARAMS = "LLM-CONFIG-003";

        // Retry and Recovery Errors
        public const string RETRY_MAX_ATTEMPTS = "LLM-RETRY-001";
        public const string RETRY_BACKOFF_ACTIVE = "LLM-RETRY-002";

        // Generic/Unknown Errors
        public const string UNKNOWN_ERROR = "LLM-UNKNOWN-001";
        public const string SOCKET_GENERIC = "LLM-SOCKET-GENERIC-001";
        public const string OPERATION_INVALID = "LLM-OPERATION-001";
    }
}
