using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Store_Management_System.Models;

namespace Store_Management_System.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ActivityLogsController : Controller
    {
        private readonly InventoryDbContext _context;

        public ActivityLogsController(InventoryDbContext context)
        {
            _context = context;
        }

        // GET: ActivityLogs
        public async Task<IActionResult> Index()
        {
            // Just fetch all logs, ordered by newest first
            var logs = await _context.ActivityLogs
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();

            return View(logs);
        }

        // GET: ActivityLogs/GetLogDetails/5
        [HttpGet]
        public async Task<IActionResult> GetLogDetails(int id)
        {
            var log = await _context.ActivityLogs.FindAsync(id);
            if (log == null)
            {
                return NotFound();
            }

            var jsonData = log.NewValue ?? log.OldValue ?? "{}";
            return Content(jsonData, "application/json");
        }
    }
}