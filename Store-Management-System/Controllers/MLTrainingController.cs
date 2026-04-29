using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Store_Management_System.Models;
using Store_Management_System.Services;

namespace Store_Management_System.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MLTrainingController : Controller
    {
        private readonly InventoryDbContext _context;
        private readonly MLDataService _dataService;
        private readonly ModelTrainerService _trainerService;
        private readonly ForecastService _forecastService;
        private readonly SampleDataGenerator _sampleDataGenerator;
        private readonly ILogger<MLTrainingController> _logger;

        public MLTrainingController(
            InventoryDbContext context,
            MLDataService dataService,
            ModelTrainerService trainerService,
            ForecastService forecastService,
            SampleDataGenerator sampleDataGenerator,
            ILogger<MLTrainingController> logger)
        {
            _context = context;
            _dataService = dataService;
            _trainerService = trainerService;
            _forecastService = forecastService;
            _sampleDataGenerator = sampleDataGenerator;
            _logger = logger;
        }

        // GET: MLTraining/Index
        public async Task<IActionResult> Index()
        {
            var hasSampleData = await _sampleDataGenerator.HasSampleData();
            var sampleDataCount = await _sampleDataGenerator.GetSampleDataCount();
            var realDataCount = await _context.StockMovements
                .CountAsync(s => s.MovementType == "Out" && s.ReferenceNumber != "SAMPLE_DATA");

            ViewBag.HasSampleData = hasSampleData;
            ViewBag.SampleDataCount = sampleDataCount;
            ViewBag.RealDataCount = realDataCount;

            return View();
        }

        // POST: MLTraining/GenerateSampleData
        [HttpPost]
        public async Task<IActionResult> GenerateSampleData(int months = 12)
        {
            var result = await _sampleDataGenerator.GenerateSampleSalesData(months);
            return Json(result);
        }

        // POST: MLTraining/ClearSampleData
        [HttpPost]
        public async Task<IActionResult> ClearSampleData()
        {
            await _sampleDataGenerator.CleanupSampleData();
            return Json(new { success = true, message = "Sample data cleared successfully" });
        }

        // POST: MLTraining/Train
        [HttpPost]
        public async Task<IActionResult> Train(bool useSampleData = true)
        {
            try
            {
                _logger.LogInformation($"Starting model training using {(useSampleData ? "SAMPLE" : "REAL")} data...");

                var trainingData = await _dataService.GetTrainingData(useSampleData);

                if (!trainingData.Any())
                {
                    return Json(new
                    {
                        success = false,
                        message = useSampleData ?
                        "No sample data available. Generate sample data first." :
                        "No real sales data available. Continue using sample data for now."
                    });
                }

                var success = await _trainerService.TrainDemandForecastModel(trainingData);

                if (success)
                {
                    _forecastService.ReloadModel();
                    return Json(new { success = true, message = $"Model trained successfully using {(useSampleData ? "SAMPLE" : "REAL")} data!" });
                }
                else
                {
                    return Json(new { success = false, message = "Model training failed. Check logs for details." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during model training");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        // GET: MLTraining/CheckData
        [HttpGet]
        public async Task<IActionResult> CheckData()
        {
            var sampleData = await _dataService.GetTrainingData(true);
            var realData = await _dataService.GetTrainingData(false);

            return Json(new
            {
                sampleData = new
                {
                    hasData = sampleData.Any(),
                    recordCount = sampleData.Count,
                    productCount = sampleData.Select(d => d.ProductId).Distinct().Count()
                },
                realData = new
                {
                    hasData = realData.Any(),
                    recordCount = realData.Count,
                    productCount = realData.Select(d => d.ProductId).Distinct().Count()
                }
            });
        }

        // POST: MLTraining/TestPrediction
        [HttpPost]
        public async Task<IActionResult> TestPrediction(int productId)
        {
            var product = await _context.Articles.FindAsync(productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Product not found" });
            }

            var prediction = await _forecastService.PredictDemand(productId, product.ArticleName, product.StandardPrice);

            return Json(new { success = true, prediction });
        }

        // GET: MLTraining/GetProducts
        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _context.Articles
                .Where(a => a.IsActive)
                .Select(a => new { id = a.Id, name = $"{a.ArticleCode} - {a.ArticleName}" })
                .Take(50)
                .ToListAsync();

            return Json(products);
        }
    }
}