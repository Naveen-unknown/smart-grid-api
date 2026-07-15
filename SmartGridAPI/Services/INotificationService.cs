using System.Threading.Tasks;

namespace SmartGridAPI.Services
{
    public interface INotificationService
    {
        Task SendNotificationAsync(string title, string message, string type = "Info", string targetRole = "Electricity Officer");
    }
}
