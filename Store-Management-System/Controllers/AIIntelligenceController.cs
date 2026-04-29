using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Store_Management_System.Models;
using Store_Management_System.Services;
using Store_Management_System.ViewModels;

namespace Store_Management_System.Controllers
{
    [Authorize]
    public class AIIntelligenceController : Controller
    {
        private readonly InventoryDbContext _context;
        private readonly ForecastService _forecastService;
        private readonly ILogger<AIIntelligenceController> _logger;
        private readonly IChatbotService? _chatbotService;

        public AIIntelligenceController(
            InventoryDbContext context,
            ForecastService forecastService, IChatbotService chatbotService,
            ILogger<AIIntelligenceController> logger)
        {
            _context = context;
            _forecastService = forecastService;
            _logger = logger;
            _chatbotService = chatbotService;
        }

        // GET: AIIntelligence
        public async Task<IActionResult> Index()
        {
            var model = new AIIntelligenceViewModel();

            // Get AI Status
            model.IsAIEnabled = GetUserAIPreference();
            model.ModelStatus = await GetModelStatus();

            // Get all AI insights
            if (model.IsAIEnabled)
            {
                model.DemandForecasts = await GetDemandForecasts();
                model.SmartAlerts = await GetSmartAlerts();
                model.PriceOptimizations = await GetPriceOptimizations();
                model.SupplierScores = await GetSupplierScorecard();
                model.Anomalies = await GetAnomalies();
            }

            return View(model);
        }

        [HttpPost]
        public IActionResult ToggleAI(bool enable)
        {
            SetUserAIPreference(enable);
            return Json(new { success = true, enabled = enable });
        }
        [HttpPost]
        public async Task<IActionResult> ChatQuery([FromBody] ChatQueryRequest request)
        {
            if (_chatbotService == null)
            {
                return Json(new { success = false, answer = "Chat service is not available." });
            }

            var response = await _chatbotService.ProcessQueryAsync(request.Query);
            return Json(response);
        }

        [HttpGet]
        public IActionResult GetChatHelp()
        {
            var helpText = new
            {
                examples = new[]
                {
            "What's the forecast for wireless mouse?",
            "How many laptops are in stock?",
            "Show me low stock items",
            "Tell me about iPhone cases",
            "Any price optimization suggestions?",
            "Show supplier information",
            "What are this week's sales?"
        }
            };
            return Json(helpText);
        }

        [HttpGet]
        public IActionResult TrainModel()
        {
            return View();
        }

        [HttpPost]
        public IActionResult TrainIntentModel()
        {
            try
            {
                var dataPath = Path.Combine(Directory.GetCurrentDirectory(), "ML", "Data", "intents.csv");

                if (!System.IO.File.Exists(dataPath))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Training data file not found. Please create ML/Data/intents.csv first."
                    });
                }

                // Create trainer with dependencies from this controller
                var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
                var trainerLogger = loggerFactory.CreateLogger<IntentModelTrainer>();

                // We need an IWebHostEnvironment - get it from HttpContext
                var env = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();

                var trainer = new IntentModelTrainer(env, trainerLogger);
                var (model, accuracy) = trainer.TrainModel();

                _logger.LogInformation("Intent model trained successfully with accuracy: {Accuracy:P2}", accuracy);

                return Json(new
                {
                    success = true,
                    message = $"Intent model trained successfully! Accuracy: {accuracy:P2}",
                    accuracy = accuracy
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error training intent model");
                return Json(new { success = false, message = $"Training failed: {ex.Message}" });
            }
        }

        [HttpGet]
        public IActionResult GetModelInfo()
        {
            var modelPath = Path.Combine(Directory.GetCurrentDirectory(), "ML", "Models", "intent_classifier.zip");
            var dataPath = Path.Combine(Directory.GetCurrentDirectory(), "ML", "Data", "intents.csv");

            var modelExists = System.IO.File.Exists(modelPath);
            var dataExists = System.IO.File.Exists(dataPath);

            var info = new
            {
                ModelExists = modelExists,
                DataExists = dataExists,
                ModelPath = modelPath,
                DataPath = dataPath,
                ModelSize = modelExists ? new FileInfo(modelPath).Length : 0,
                LastTrained = modelExists ? System.IO.File.GetLastWriteTime(modelPath).ToString("yyyy-MM-dd HH:mm:ss") : "Never",
                DataRows = dataExists ? System.IO.File.ReadAllLines(dataPath).Length - 1 : 0
            };

            return Json(info);
        }
        private async Task<List<DemandForecastItem>> GetDemandForecasts()
        {
            var products = await _context.Articles
                .Where(a => a.IsActive && a.IsSellable)
                .Take(20)
                .ToListAsync();

            var forecasts = new List<DemandForecastItem>();

            foreach (var product in products)
            {
                var currentStock = await _context.CurrentStocks
                    .Where(cs => cs.ArticleId == product.Id)
                    .SumAsync(cs => cs.Quantity);

                var prediction = await _forecastService.PredictDemand(
                    product.Id, product.ArticleName, product.StandardPrice);

                if (prediction != null)
                {
                    forecasts.Add(new DemandForecastItem
                    {
                        ProductId = product.Id,
                        ProductName = product.ArticleName,
                        CurrentStock = (int)currentStock,
                        PredictedDemand = (int)prediction.PredictedSales,
                        Status = GetStatus((int)currentStock, (int)prediction.PredictedSales)
                    });
                }
            }

            return forecasts.OrderByDescending(f => f.PredictedDemand).ToList();
        }

        private async Task<List<SmartAlertItem>> GetSmartAlerts()
        {
            var alerts = new List<SmartAlertItem>();
            var products = await _context.Articles
                .Where(a => a.IsActive && a.IsSellable)
                .ToListAsync();

            foreach (var product in products)
            {
                var currentStock = await _context.CurrentStocks
                    .Where(cs => cs.ArticleId == product.Id)
                    .SumAsync(cs => cs.Quantity);

                var dailySales = await GetDailySalesAverage(product.Id);
                var daysRemaining = dailySales > 0 ? currentStock / dailySales : 999;

                if (daysRemaining <= 7 && daysRemaining > 0)
                {
                    alerts.Add(new SmartAlertItem
                    {
                        ProductId = product.Id,
                        ProductName = product.ArticleName,
                        CurrentStock = (int)currentStock,
                        DailySales = dailySales,
                        DaysRemaining = (int)daysRemaining,
                        Severity = daysRemaining <= 3 ? "Critical" : "Warning",
                        SuggestedOrder = (int)(dailySales * 30) // Order 30 days worth
                    });
                }
            }

            return alerts.OrderBy(a => a.DaysRemaining).ToList();
        }

        private async Task<List<PriceOptimizationItem>> GetPriceOptimizations()
        {
            // Simple price optimization logic
            var optimizations = new List<PriceOptimizationItem>();
            var products = await _context.Articles
                .Where(a => a.IsActive && a.IsSellable)
                .Take(10)
                .ToListAsync();

            foreach (var product in products)
            {
                var currentStock = await _context.CurrentStocks
                    .Where(cs => cs.ArticleId == product.Id)
                    .SumAsync(cs => cs.Quantity);

                var monthlySales = await GetMonthlySalesAverage(product.Id);
                var stockMonths = monthlySales > 0 ? currentStock / monthlySales : 999;

                if (stockMonths > 6) // Overstocked
                {
                    optimizations.Add(new PriceOptimizationItem
                    {
                        ProductId = product.Id,
                        ProductName = product.ArticleName,
                        CurrentPrice = product.StandardPrice,
                        SuggestedPrice = product.StandardPrice * 0.9m, // 10% discount
                        ExpectedImpact = "Clear excess stock, increase turnover",
                        Reason = $"Overstocked ({stockMonths:F0} months of inventory)"
                    });
                }
            }

            return optimizations;
        }

        private async Task<List<SupplierScorecardItem>> GetSupplierScorecard()
        {
            var suppliers = await _context.Suppliers
                .Where(s => s.IsActive && !s.IsDeleted)
                .ToListAsync();

            var scores = new List<SupplierScorecardItem>();

            foreach (var supplier in suppliers)
            {
                // Get purchase orders for this supplier
                var purchaseOrders = await _context.Vouchers
                    .Where(v => v.SupplierId == supplier.Id && v.VoucherType == "PO")
                    .ToListAsync();

                // Calculate average delivery time (mock logic)
                var avgDeliveryDays = purchaseOrders.Any() ? 5 : 0;

                // Calculate price competitiveness (mock)
                var priceScore = purchaseOrders.Any() ? 4 : 3;

                scores.Add(new SupplierScorecardItem
                {
                    SupplierId = supplier.Id,
                    SupplierName = supplier.Name,
                    Rating = priceScore,
                    DeliveryDays = avgDeliveryDays,
                    PriceCompetitiveness = priceScore == 4 ? "-5%" : "0%",
                    QualityScore = 95,
                    Recommendation = avgDeliveryDays <= 5 ? "Excellent" : "Review terms"
                });
            }

            return scores.OrderByDescending(s => s.Rating).ToList();
        }

        private async Task<List<AnomalyItem>> GetAnomalies()
        {
            var anomalies = new List<AnomalyItem>();
            var today = DateTime.UtcNow.Date;
            var yesterday = today.AddDays(-1);

            // Check for unusual sales spikes
            var products = await _context.Articles
                .Where(a => a.IsActive)
                .Take(20)
                .ToListAsync();

            foreach (var product in products)
            {
                var todaySales = await _context.StockMovements
                    .Where(s => s.ArticleId == product.Id && s.MovementType == "Out" && s.MovementDate.Date == today)
                    .SumAsync(s => s.Quantity);

                var avgDailySales = await GetDailySalesAverage(product.Id);

                if (todaySales > avgDailySales * 3 && avgDailySales > 0)
                {
                    anomalies.Add(new AnomalyItem
                    {
                        ProductId = product.Id,
                        ProductName = product.ArticleName,
                        AnomalyType = "Sales Spike",
                        ExpectedValue = (int)avgDailySales,
                        ActualValue = (int)todaySales,
                        Severity = "High",
                        Recommendation = "Investigate unusual order pattern"
                    });
                }
            }

            return anomalies;
        }

        // Helper methods
        private async Task<decimal> GetDailySalesAverage(int productId)
        {
            var last30Days = DateTime.UtcNow.AddDays(-30);
            var sales = await _context.StockMovements
                .Where(s => s.ArticleId == productId && s.MovementType == "Out" && s.MovementDate >= last30Days)
                .SumAsync(s => s.Quantity);

            return sales / 30;
        }

        private async Task<decimal> GetMonthlySalesAverage(int productId)
        {
            var last6Months = DateTime.UtcNow.AddMonths(-6);
            var sales = await _context.StockMovements
                .Where(s => s.ArticleId == productId && s.MovementType == "Out" && s.MovementDate >= last6Months)
                .SumAsync(s => s.Quantity);

            return sales / 6;
        }

        private string GetStatus(int currentStock, int predictedDemand)
        {
            if (currentStock < predictedDemand * 0.3) return "Critical";
            if (currentStock < predictedDemand) return "Warning";
            return "Good";
        }

        private async Task<ModelStatus> GetModelStatus()
        {
            var modelPath = Path.Combine(Directory.GetCurrentDirectory(), "ML", "Models", "demand_forecast.zip");
            var modelExists = System.IO.File.Exists(modelPath);

            var salesCount = await _context.StockMovements
                .CountAsync(s => s.MovementType == "Out");

            return new ModelStatus
            {
                IsTrained = modelExists,
                RecordsUsed = salesCount,
                Confidence = modelExists ? 0.72 : 0,
                LastTrained = modelExists ? System.IO.File.GetLastWriteTime(modelPath) : (DateTime?)null
            };
        }

        private bool GetUserAIPreference()
        {
            // You can store this in database per user
            // For now, default to true
            return true;
        }

        private void SetUserAIPreference(bool enable)
        {
            // Store in database or session
            // Implementation depends on your user settings
        }
    }
}