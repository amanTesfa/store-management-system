using Microsoft.EntityFrameworkCore;
using Store_Management_System.ML;
using Store_Management_System.Models;

namespace Store_Management_System.Services
{
    public class MLDataService
    {
        private readonly InventoryDbContext _context;

        public MLDataService(InventoryDbContext context)
        {
            _context = context;
        }

        // Get training data - simplified query that EF Core can translate
        public async Task<List<SalesHistoryData>> GetTrainingData(bool useSampleData = true, int? productId = null)
        {
            // First, get the raw data without grouping
            IQueryable<StockMovement> query = _context.StockMovements
                .Include(s => s.Article)
                .Where(s => s.MovementType == "Out");

            // Filter by sample or real data
            if (useSampleData)
            {
                query = query.Where(s => s.ReferenceNumber == "SAMPLE_DATA");
            }
            else
            {
                query = query.Where(s => s.ReferenceNumber != "SAMPLE_DATA" && s.MovementDate >= DateTime.UtcNow.AddMonths(-12));
            }

            if (productId.HasValue)
            {
                query = query.Where(s => s.ArticleId == productId.Value);
            }

            // Execute the query and get raw data
            var rawData = await query
                .Select(s => new
                {
                    s.ArticleId,
                    s.MovementDate,
                    s.Quantity,
                    ArticlePrice = s.Article != null ? s.Article.StandardPrice : 0
                })
                .ToListAsync();

            var salesData = rawData
     .GroupBy(s => new {
         s.ArticleId,
         s.MovementDate.Year,
         s.MovementDate.Month,
         s.MovementDate.DayOfWeek
     })
     .Select(g => new SalesHistoryData
     {
         ProductId = g.Key.ArticleId,
         Year = g.Key.Year,
         Month = g.Key.Month,
         DayOfWeek = (float)g.Key.DayOfWeek,
         IsWeekend = (g.Key.DayOfWeek == DayOfWeek.Saturday || g.Key.DayOfWeek == DayOfWeek.Sunday) ? 1 : 0,
         Price = (float)(g.First().ArticlePrice),
         Quantity = (float)g.Sum(s => (double)s.Quantity)  // ← Changed from QuantitySold to Quantity
     })
     .ToList();

            return salesData;
        }

        public async Task<Dictionary<int, decimal>> GetCurrentStockLevels()
        {
            return await _context.CurrentStocks
                .GroupBy(cs => cs.ArticleId)
                .Select(g => new { ArticleId = g.Key, TotalStock = g.Sum(cs => cs.Quantity) })
                .ToDictionaryAsync(k => k.ArticleId, v => v.TotalStock);
        }
    }
}