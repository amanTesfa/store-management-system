using Microsoft.AspNetCore.Http;
using Store_Management_System.Models;
using System.Security.Claims;

namespace Store_Management_System.Services
{
    public class ActivityLogService
    {
        private readonly InventoryDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ActivityLogService(InventoryDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(string action, string entityType, int? entityId = null,
            string? oldValue = null, string? newValue = null, string? details = null)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var userName = user?.Identity?.Name ?? "System";
            var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

            var log = new ActivityLog
            {
                UserId = userId,
                UserName = userName ?? "System",
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                OldValue = oldValue?.Length > 500 ? oldValue.Substring(0, 500) : oldValue,
                NewValue = newValue?.Length > 500 ? newValue.Substring(0, 500) : newValue,
                Details = details?.Length > 500 ? details.Substring(0, 500) : details,
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow
            };

            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}