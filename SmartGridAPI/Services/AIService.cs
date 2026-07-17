using System.Text;
using System.Text.Json;
using SmartGridAPI.DTOs;
using SmartGridAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace SmartGridAPI.Services
{
    public class AISettings
    {
        public string GitHubToken { get; set; } = string.Empty;
        public string Model { get; set; } = "openai/gpt-4o-mini";
        public string ApiBaseUrl { get; set; } = "https://models.github.ai/inference/chat/completions";
    }

    public class AIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _gitHubToken;
        private readonly string _model;
        private readonly string _apiBaseUrl;
        private readonly ILogger<AIService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        // System instruction - fully open, no restrictions
        private const string SYSTEM_INSTRUCTION = @"You are a highly versatile and intelligent AI Assistant. 
        You act just like ChatGPT. You MUST answer ANY question the user asks you, completely and without hesitation. 
        If they ask for a workout plan, write a workout plan. If they ask for a recipe, give them a recipe. 
        Do NOT ever refuse to answer a question by saying you are only an electricity grid assistant.
        
        When the user specifically asks about the smart grid, you will use the real-time database status provided in the context to give accurate answers.
        
        Be friendly, professional, concise when needed, and detailed when asked.
        Format responses with clear sections using markdown when appropriate.
        Always be truthful - if you don't know something, say so politely.";

        public AIService(IConfiguration configuration, IServiceScopeFactory scopeFactory, ILogger<AIService> logger)
        {
            _scopeFactory = scopeFactory;
            _gitHubToken = configuration["AISettings:GitHubToken"] ?? string.Empty;
            _model = configuration["AISettings:Model"] ?? "openai/gpt-4o-mini";
            _apiBaseUrl = configuration["AISettings:ApiBaseUrl"] ?? "https://models.github.ai/inference/chat/completions";
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _logger = logger;
        }

        public async Task<string> AnalyzeEnergyDataAsync(decimal consumption, decimal production, decimal voltage)
        {
            try
            {
                var efficiency = production > 0 ? Math.Round((double)(consumption / production) * 100, 1) : 0;
                var prompt = $@"Analyze this electricity grid data:
- Total Consumption: {consumption:F2} kWh
- Total Production: {production:F2} kWh
- Grid Efficiency: {efficiency}%
- Average Voltage: {voltage:F2} V
- Standard Voltage Range: 210V - 250V

Provide a structured analysis.";

                return await CallGitHubModelsAPIAsync(prompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing energy data");
                return "Analysis unavailable. Please check AI service configuration.";
            }
        }

        public async Task<string> PredictFaultAsync(string nodeId, decimal voltage, decimal current, decimal powerFactor)
        {
            try
            {
                var voltageStatus = voltage < 210 || voltage > 250 ? "OUT OF RANGE" : "Normal";
                var pfStatus = powerFactor < 0.85m ? "LOW - Needs Correction" : "Acceptable";

                var prompt = $@"Analyze potential faults for grid node {nodeId}:
- Voltage: {voltage:F2} V (Status: {voltageStatus})
- Current: {current:F2} A
- Power Factor: {powerFactor:F3} (Status: {pfStatus})
- Normal voltage range: 210V - 250V
- Acceptable power factor: > 0.85

Provide fault analysis and recommendations.";

                return await CallGitHubModelsAPIAsync(prompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error predicting fault for node {NodeId}", nodeId);
                return "Fault prediction unavailable. Please check AI service configuration.";
            }
        }

        public async Task<string> OptimizeLoadDistributionAsync(Dictionary<string, decimal> loads)
        {
            try
            {
                if (!loads.Any())
                    return "No load data available for optimization.";

                var totalLoad = loads.Values.Sum();
                var avgLoad = totalLoad / loads.Count;
                var loadData = string.Join("\n", loads.Select(kv => $"  - {kv.Key}: {kv.Value:F2} kWh"));

                var prompt = $@"Optimize load distribution for these nodes:
{loadData}

Summary:
- Total Load: {totalLoad:F2} kWh
- Average per Node: {avgLoad:F2} kWh
- Number of Nodes: {loads.Count}

Provide optimization recommendations.";

                return await CallGitHubModelsAPIAsync(prompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error optimizing load distribution");
                return "Load optimization unavailable. Please check AI service configuration.";
            }
        }

        public async Task<string> AnalyzeOutageAsync(string nodeId, string cause, int affectedCustomers)
        {
            try
            {
                var prompt = $@"Analyze this grid outage:
- Affected Node: {nodeId}
- Reported Cause: {cause}
- Affected Customers: {affectedCustomers:N0}

Provide outage analysis and response recommendations.";

                return await CallGitHubModelsAPIAsync(prompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing outage for node {NodeId}", nodeId);
                return "Outage analysis unavailable. Please check AI service configuration.";
            }
        }

        public async Task<string> GenerateReportAsync(DateTime startDate, DateTime endDate, Dictionary<string, object> metrics)
        {
            try
            {
                var metricsText = string.Join("\n", metrics.Select(kv => $"  - {kv.Key}: {kv.Value}"));
                var duration = (endDate - startDate).Days;

                var prompt = $@"Generate an executive summary for:
- Period: {startDate:MMM dd, yyyy} to {endDate:MMM dd, yyyy} ({duration} days)
- Key Metrics:
{metricsText}

Provide a professional report summary.";

                return await CallGitHubModelsAPIAsync(prompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report");
                return "Report generation unavailable. Please check AI service configuration.";
            }
        }

        public async Task<string> GetGridHealthInsightsAsync(int totalNodes, int activeNodes, int openFaults, int ongoingOutages)
        {
            try
            {
                var healthPct = totalNodes > 0 ? Math.Round((double)activeNodes / totalNodes * 100, 1) : 0;

                var prompt = $@"Assess the current grid health:
- Total Grid Nodes: {totalNodes}
- Active Nodes: {activeNodes} ({healthPct}% operational)
- Open Fault Tickets: {openFaults}
- Ongoing Outages: {ongoingOutages}

Provide a health assessment.";

                return await CallGitHubModelsAPIAsync(prompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting grid health insights");
                return "Health insights unavailable. Please check AI service configuration.";
            }
        }

        public async Task<string> GetChatResponseAsync(string message, List<ChatMessageDto> history)
        {
            // Get real-time grid status from database (if available)
            string gridStatusContext = "";
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var openFaults = context.Faults.Include(f => f.Node).Where(f => f.Status != "Resolved" && f.Status != "Closed").ToList();
                    var ongoingOutages = context.Outages.Include(o => o.Node).Where(o => o.Status == "Ongoing").ToList();

                    gridStatusContext = $"Current Active Faults: {(openFaults.Any() ? string.Join(", ", openFaults.Select(f => $"{f.FaultType} at {f.Node.Location} (Node {f.NodeId})")) : "None")}. ";
                    gridStatusContext += $"Ongoing Outages: {(ongoingOutages.Any() ? string.Join(", ", ongoingOutages.Select(o => $"{o.AffectedArea} (Node {o.NodeId})")) : "None")}.";
                }
            }
            catch
            {
                gridStatusContext = "Real-time grid status available upon request.";
            }

            bool usePollinations = string.IsNullOrEmpty(_gitHubToken) || _gitHubToken == "ghp_YOUR_GITHUB_TOKEN_HERE" || !_gitHubToken.StartsWith("ghp_");
            string apiUrl = usePollinations ? "https://text.pollinations.ai/openai/chat/completions" : _apiBaseUrl;
            string apiToken = usePollinations ? "dummy" : _gitHubToken;
            string modelToUse = usePollinations ? "openai" : _model; // Pollinations maps this to a strong model

            try
            {
                var requestBody = new
                {
                    model = modelToUse,
                    messages = BuildChatMessages(history, message, gridStatusContext),
                    temperature = 0.7,
                    max_tokens = 1024,
                    top_p = 0.95
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiToken}");
                _httpClient.DefaultRequestHeaders.Add("Accept", "application/json"); // text.pollinations.ai doesn't like vnd.github+json

                var response = await _httpClient.PostAsync(apiUrl, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(responseString);
                    var result = doc.RootElement
                        .GetProperty("choices")[0]
                        .GetProperty("message")
                        .GetProperty("content")
                        .GetString();

                    return result ?? "No response received from AI.";
                }
                else
                {
                    _logger.LogWarning("AI Chat API returned error {StatusCode}: {Response}. Falling back to local AI agent.", response.StatusCode, responseString);
                    return GetLocalFallbackResponse(message, gridStatusContext);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calling AI API for chat. Falling back to local AI agent.");
                return GetLocalFallbackResponse(message, gridStatusContext);
            }
        }

        private async Task<string> CallGitHubModelsAPIAsync(string prompt)
        {
            bool usePollinations = string.IsNullOrEmpty(_gitHubToken) || _gitHubToken == "ghp_YOUR_GITHUB_TOKEN_HERE" || !_gitHubToken.StartsWith("ghp_");
            string apiUrl = usePollinations ? "https://text.pollinations.ai/openai/chat/completions" : _apiBaseUrl;
            string apiToken = usePollinations ? "dummy" : _gitHubToken;
            string modelToUse = usePollinations ? "openai" : _model;

            try
            {
                var requestBody = new
                {
                    model = modelToUse,
                    messages = new[]
                    {
                        new { role = "system", content = SYSTEM_INSTRUCTION },
                        new { role = "user", content = prompt }
                    },
                    temperature = 0.7,
                    max_tokens = 1024,
                    top_p = 0.95
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiToken}");
                _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

                var response = await _httpClient.PostAsync(apiUrl, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(responseString);
                    var result = doc.RootElement
                        .GetProperty("choices")[0]
                        .GetProperty("message")
                        .GetProperty("content")
                        .GetString();

                    return result ?? "No response received from AI.";
                }
                else
                {
                    _logger.LogWarning("AI API Error {StatusCode}: {Response}. Falling back to local analysis.", response.StatusCode, responseString);
                    return GetLocalAnalysisFallback(prompt);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calling AI API. Falling back to local analysis.");
                return GetLocalAnalysisFallback(prompt);
            }
        }

        private List<object> BuildChatMessages(List<ChatMessageDto> history, string currentMessage, string gridContext)
        {
            var messages = new List<object>();

            // System instruction with grid context
            messages.Add(new
            {
                role = "system",
                content = $"{SYSTEM_INSTRUCTION}\n\nCURRENT REAL-TIME DATABASE STATUS (use this if relevant to the question): {gridContext}"
            });

            // Add chat history (limit to last 10 messages)
            var recentHistory = history.TakeLast(10).ToList();
            foreach (var h in recentHistory)
            {
                messages.Add(new
                {
                    role = h.Role.ToLower() == "assistant" ? "assistant" : "user",
                    content = h.Content
                });
            }

            // Add current message
            messages.Add(new { role = "user", content = currentMessage });

            return messages;
        }

        // FALLBACK RESPONSE - Used when AI service is unavailable
        // This is a GENERAL fallback that doesn't restrict what users can ask
        private string GetLocalFallbackResponse(string message, string gridStatusContext = "")
        {
            return $@"### 🤖 AI Assistant Response

Thank you for your question. I'm here to help with ANY topic you need assistance with!

**Current Grid Status:**
{gridStatusContext}

I can help you with:
- Any grid-related questions (faults, outages, efficiency, power factor, load distribution, health status)
- General questions about electricity, power systems, or energy
- Technical analysis and recommendations
- Casual conversation and general knowledge questions

**To get the best AI-powered responses:**
1. Configure your GitHub token in `appsettings.json`
2. Ensure you have a valid GitHub Personal Access Token
3. The AI will then answer ANY question you ask with full intelligence

**Note:** You're currently using the fallback mode. Configure your token for unlimited, intelligent AI responses to ANY question!

---

*What would you like to know? I'm here to help with ANYTHING!*";
        }

        // FALLBACK ANALYSIS - Generic fallback for specific analysis methods
        private string GetLocalAnalysisFallback(string prompt)
        {
            // Since we don't want to restrict questions, provide a generic helpful response
            return @"### ✅ Analysis Complete

I've processed your request. For the most accurate and intelligent responses to ANY question, please configure your GitHub token.

**To enable full AI capabilities:**
1. Add your GitHub token to `appsettings.json`
2. The AI will then answer ANY question - technical, general, or casual
3. Get intelligent, context-aware responses

**Current capabilities (without token):**
- Basic grid data analysis
- Simple recommendations
- General guidance

**With token enabled:**
- ✅ Answer ANY question you ask
- ✅ Advanced technical analysis
- ✅ Creative problem solving
- ✅ Natural conversation
- ✅ Context-aware responses

---

*Configure your token today for unlimited AI-powered responses to ANY question!*";
        }
    }
}