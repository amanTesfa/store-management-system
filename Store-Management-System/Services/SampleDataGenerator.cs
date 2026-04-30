using Microsoft.EntityFrameworkCore;
using Store_Management_System.Models;

namespace Store_Management_System.Services
{
    public class SampleDataGenerator
    {
        private readonly InventoryDbContext _context;
        private readonly Random _random = new Random();

        public SampleDataGenerator(InventoryDbContext context)
        {
            _context = context;
        }

        public async Task<GenerateResult> GenerateSampleSalesData(int monthsOfHistory = 12)
        {
            try
            {
                var products = await _context.Articles
                    .Where(a => a.IsActive && a.IsSellable)
                    .Take(40) // Limit to 30 products for sample
                    .ToListAsync();

                if (!products.Any())
                {
                    return new GenerateResult { Success = false, Message = "No active products found" };
                }

                var sampleMovements = new List<StockMovement>();
                var sampleCount = 0;

                foreach (var product in products)
                {
                    // Base demand varies by product type (5-100 units per month)
                    var baseDemand = _random.Next(5, 100);

                    for (int month = 1; month <= monthsOfHistory; month++)
                    {
                        var movementDate = DateTime.UtcNow.AddMonths(-month);

                        // Add seasonality (higher in Nov-Dec, lower in Jan, Feb)
                        double seasonality = 1.0;
                        if (movementDate.Month == 11 || movementDate.Month == 12)
                            seasonality = 1.5; // Holiday season boost
                        else if (movementDate.Month == 1 || movementDate.Month == 2)
                            seasonality = 0.6; // Post-holiday dip
                        else if (movementDate.Month >= 3 && movementDate.Month <= 5)
                            seasonality = 1.1; // Spring boost

                        // Add random variation
                        var variation = _random.NextDouble() * 0.6 + 0.7; // 0.7 to 1.3

                        // Add trend (slightly increasing over time)
                        var trend = 1 + (month / 24.0); // 5% increase over 12 months

                        var quantity = (int)(baseDemand * seasonality * variation * trend);
                        if (quantity <= 0) quantity = 1;
                        if (quantity > 500) quantity = 500; // Cap at 500

                        sampleMovements.Add(new StockMovement
                        {
                            MovementNumber = $"SAMPLE-{product.Id}-{movementDate:yyyyMMdd}",
                            ArticleId = product.Id,
                            WarehouseId = 1,
                            MovementType = "Out",
                            ActivityType = "Sales",
                            Quantity = quantity,
                            UnitCost = product.StandardCost,
                            TotalCost = quantity * product.StandardCost,
                            MovementDate = movementDate,
                            CreatedBy = 1,
                            ReferenceNumber = "SAMPLE_DATA",
                            Notes = "Generated sample data for ML training"
                        });
                        sampleCount++;
                    }
                }

                // Remove old sample data first
                await CleanupSampleData();

                // Add new sample data in batches
                var batchSize = 500;
                for (int i = 0; i < sampleMovements.Count; i += batchSize)
                {
                    var batch = sampleMovements.Skip(i).Take(batchSize);
                    _context.StockMovements.AddRange(batch);
                    await _context.SaveChangesAsync();
                }

                return new GenerateResult
                {
                    Success = true,
                    Message = $"Generated {sampleCount} sample records for {products.Count} products",
                    RecordCount = sampleCount,
                    ProductCount = products.Count
                };
            }
            catch (Exception ex)
            {
                return new GenerateResult { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task CleanupSampleData()
        {
            var sampleData = await _context.StockMovements
                .Where(s => s.ReferenceNumber == "SAMPLE_DATA")
                .ToListAsync();

            if (sampleData.Any())
            {
                _context.StockMovements.RemoveRange(sampleData);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> HasSampleData()
        {
            return await _context.StockMovements
                .AnyAsync(s => s.ReferenceNumber == "SAMPLE_DATA");
        }

        public async Task<int> GetSampleDataCount()
        {
            return await _context.StockMovements
                .CountAsync(s => s.ReferenceNumber == "SAMPLE_DATA");
        }
    }

    public class GenerateResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int RecordCount { get; set; }
        public int ProductCount { get; set; }
    }
}