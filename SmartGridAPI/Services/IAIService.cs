using SmartGridAPI.DTOs;

namespace SmartGridAPI.Services
{
    public interface IAIService
    {
        Task<string> AnalyzeEnergyDataAsync(decimal consumption, decimal production, decimal voltage);
        Task<string> PredictFaultAsync(string nodeId, decimal voltage, decimal current, decimal powerFactor);
        Task<string> OptimizeLoadDistributionAsync(Dictionary<string, decimal> loads);
        Task<string> AnalyzeOutageAsync(string nodeId, string cause, int affectedCustomers);
        Task<string> GenerateReportAsync(DateTime startDate, DateTime endDate, Dictionary<string, object> metrics);
        Task<string> GetGridHealthInsightsAsync(int totalNodes, int activeNodes, int openFaults, int ongoingOutages);
        Task<string> GetChatResponseAsync(string message, List<ChatMessageDto> history);
    }
}
