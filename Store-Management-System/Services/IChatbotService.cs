using Store_Management_System.Models;

namespace Store_Management_System.Services
{
    public interface IChatbotService
    {
        Task<ChatbotResponse> ProcessQueryAsync(string userQuery);
    }
}