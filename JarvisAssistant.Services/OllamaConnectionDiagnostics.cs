using Microsoft.Extensions.Logging;
using JarvisAssistant.Core.Models;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace JarvisAssistant.Services
{
    /// <summary>
    /// Utility service for diagnosing Ollama connection issues.
    /// </summary>
    public class OllamaConnectionDiagnostics
    {
        private readonly ILogger<OllamaConnectionDiagnostics> _logger;
        private readonly HttpClient _httpClient;

        public OllamaConnectionDiagnostics(ILogger<OllamaConnectionDiagnostics> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        /// <summary>
        /// Performs comprehensive diagnostics for Ollama connectivity.
        /// </summary>
        /// <param name="endpoints">List of endpoints to test.</param>
        /// <returns>Diagnostic results.</returns>
        public async Task<DiagnosticResult> DiagnoseConnectionAsync(params string[] endpoints)
        {
            var result = new DiagnosticResult();
            
            if (endpoints == null || endpoints.Length == 0)
            {
                endpoints = new[]
                {
                    "http://localhost:11434",
                    "http://127.0.0.1:11434",
                    "http://100.108.155.28:11434",
                    "http://host.docker.internal:11434"
                };
            }

            foreach (var endpoint in endpoints)
            {
                var endpointResult = await TestEndpoint(endpoint);
                result.EndpointResults[endpoint] = endpointResult;
                
                if (endpointResult.IsReachable)
                {
                    result.WorkingEndpoints.Add(endpoint);
                }
            }

            // Test network connectivity
            result.HasInternetConnection = await TestInternetConnectivity();
            result.CanResolveLocalhost = await TestLocalhostResolution();

            // Generate recommendations
            result.Recommendations = GenerateRecommendations(result);

            return result;
        }

        private async Task<EndpointTestResult> TestEndpoint(string endpoint)
        {
            var result = new EndpointTestResult { Endpoint = endpoint };
            
            try
            {
                var uri = new Uri(endpoint);
                
                // Test ping first
                result.PingResult = await TestPing(uri.Host);
                
                // Test port connectivity
                result.PortOpen = await TestPort(uri.Host, uri.Port);
                
                // Test HTTP connectivity
                if (result.PortOpen)
                {
                    result.HttpResult = await TestHttpEndpoint(endpoint);
                }

                result.IsReachable = result.PortOpen && result.HttpResult.IsSuccessful;
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
                _logger.LogError(ex, "Error testing endpoint: {Endpoint}", endpoint);
            }

            return result;
        }

        private async Task<PingTestResult> TestPing(string host)
        {
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(host, 5000);
                
                return new PingTestResult
                {
                    Success = reply.Status == IPStatus.Success,
                    ResponseTime = reply.RoundtripTime,
                    Status = reply.Status.ToString()
                };
            }
            catch (Exception ex)
            {
                return new PingTestResult
                {
                    Success = false,
                    Status = $"Error: {ex.Message}"
                };
            }
        }

        private async Task<bool> TestPort(string host, int port)
        {
            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(host, port);
                var timeoutTask = Task.Delay(5000);
                
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                
                if (completedTask == connectTask && client.Connected)
                {
                    return true;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        private async Task<HttpTestResult> TestHttpEndpoint(string endpoint)
        {
            try
            {
                var testEndpoints = new[]
                {
                    $"{endpoint}/api/tags",
                    $"{endpoint}/api/health",
                    $"{endpoint}/"
                };

                foreach (var testEndpoint in testEndpoints)
                {
                    try
                    {
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                        var response = await _httpClient.GetAsync(testEndpoint, cts.Token);
                        
                        return new HttpTestResult
                        {
                            IsSuccessful = response.IsSuccessStatusCode,
                            StatusCode = (int)response.StatusCode,
                            TestedEndpoint = testEndpoint,
                            ResponseContent = response.IsSuccessStatusCode ? 
                                await response.Content.ReadAsStringAsync() : 
                                $"Error: {response.ReasonPhrase}"
                        };
                    }
                    catch (TaskCanceledException)
                    {
                        // Try next endpoint
                        continue;
                    }
                }

                return new HttpTestResult
                {
                    IsSuccessful = false,
                    StatusCode = 0,
                    ResponseContent = "All HTTP endpoints failed"
                };
            }
            catch (Exception ex)
            {
                return new HttpTestResult
                {
                    IsSuccessful = false,
                    StatusCode = 0,
                    ResponseContent = $"Error: {ex.Message}"
                };
            }
        }

        private async Task<bool> TestInternetConnectivity()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var response = await _httpClient.GetAsync("https://www.google.com", cts.Token);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> TestLocalhostResolution()
        {
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync("localhost", 1000);
                return reply.Status == IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }

        private List<string> GenerateRecommendations(DiagnosticResult result)
        {
            var recommendations = new List<string>();

            if (result.WorkingEndpoints.Count == 0)
            {
                recommendations.Add("No Ollama endpoints are reachable. Please ensure Ollama is installed and running.");
                
                if (!result.CanResolveLocalhost)
                {
                    recommendations.Add("Localhost resolution failed. Check your network configuration.");
                }
                
                recommendations.Add("Try starting Ollama with: 'ollama serve'");
                recommendations.Add("Check if Ollama is running on the default port 11434");
                
                if (result.EndpointResults.Values.Any(e => e.PingResult?.Success == false))
                {
                    recommendations.Add("Network connectivity issues detected. Check firewall settings.");
                }
            }
            else
            {
                recommendations.Add($"Found {result.WorkingEndpoints.Count} working endpoint(s): {string.Join(", ", result.WorkingEndpoints)}");
                recommendations.Add("The application should automatically use the first working endpoint.");
            }

            return recommendations;
        }
    }

    public class DiagnosticResult
    {
        public Dictionary<string, EndpointTestResult> EndpointResults { get; set; } = new();
        public List<string> WorkingEndpoints { get; set; } = new();
        public bool HasInternetConnection { get; set; }
        public bool CanResolveLocalhost { get; set; }
        public List<string> Recommendations { get; set; } = new();
    }

    public class EndpointTestResult
    {
        public string Endpoint { get; set; } = string.Empty;
        public PingTestResult? PingResult { get; set; }
        public bool PortOpen { get; set; }
        public HttpTestResult HttpResult { get; set; } = new();
        public bool IsReachable { get; set; }
        public string? Error { get; set; }
    }

    public class PingTestResult
    {
        public bool Success { get; set; }
        public long ResponseTime { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class HttpTestResult
    {
        public bool IsSuccessful { get; set; }
        public int StatusCode { get; set; }
        public string TestedEndpoint { get; set; } = string.Empty;
        public string ResponseContent { get; set; } = string.Empty;
    }
}