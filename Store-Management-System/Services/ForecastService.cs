using Microsoft.ML;
using Store_Management_System.ML;
using Store_Management_System.Models;

namespace Store_Management_System.Services
{
    public class ForecastService
    {
        private readonly MLContext _mlContext;
        private ITransformer? _model;
        private PredictionEngine<SalesHistoryData, SalesPrediction>? _predictionEngine;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ForecastService> _logger;

        public ForecastService(IWebHostEnvironment environment, ILogger<ForecastService> logger)
        {
            _mlContext = new MLContext();
            _environment = environment;
            _logger = logger;
            LoadModel();
        }

        private void LoadModel()
        {
            try
            {
                var modelPath = Path.Combine(_environment.ContentRootPath, "ML", "Models", "demand_forecast.zip");
                if (File.Exists(modelPath))
                {
                    _model = _mlContext.Model.Load(modelPath, out _);
                    _predictionEngine = _mlContext.Model.CreatePredictionEngine<SalesHistoryData, SalesPrediction>(_model);
                    _logger.LogInformation("Demand forecast model loaded successfully");
                }
                else
                {
                    _logger.LogWarning("Demand forecast model not found. Train the model first.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading demand forecast model");
            }
        }

        public void ReloadModel()
        {
            LoadModel();
        }

        public async Task<DemandForecastResult?> PredictDemand(int productId, string productName, decimal price, int monthsAhead = 1)
        {
            if (_predictionEngine == null)
            {
                _logger.LogWarning("Prediction engine not available");
                return null;
            }

            try
            {
                var currentDate = DateTime.UtcNow;
                var targetMonth = currentDate.AddMonths(monthsAhead);

                var input = new SalesHistoryData
                {
                    ProductId = productId,
                    Year = targetMonth.Year,
                    Month = targetMonth.Month,
                    DayOfWeek = (float)targetMonth.DayOfWeek,
                    IsWeekend = (targetMonth.DayOfWeek == DayOfWeek.Saturday || targetMonth.DayOfWeek == DayOfWeek.Sunday) ? 1 : 0,
                    Price = (float)price,
                    Quantity = 0 // Not used for prediction
                };

                var prediction = _predictionEngine.Predict(input);
                var predictedQuantity = Math.Max(0, prediction.PredictedQuantity);

                // Calculate confidence based on model quality
                var confidence = 0.7f; // Default, improve with actual metrics

                // Generate recommendation
                var recommendation = GenerateRecommendation(predictedQuantity);

                return new DemandForecastResult
                {
                    ProductId = productId,
                    ProductName = productName,
                    Year = targetMonth.Year,
                    Month = targetMonth.Month,
                    PredictedSales = predictedQuantity,
                    Confidence = confidence,
                    Recommendation = recommendation
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error predicting demand for product {productId}");
                return null;
            }
        }

        private string GenerateRecommendation(float predictedSales)
        {
            if (predictedSales <= 0)
                return "No significant demand predicted for this period.";
            else if (predictedSales < 10)
                return "Low demand expected. Consider maintaining minimum stock levels.";
            else if (predictedSales < 50)
                return "Moderate demand expected. Ensure adequate stock availability.";
            else
                return "High demand expected! Consider increasing stock levels and preparing for peak sales.";
        }

        public async Task<List<DemandForecastResult>> PredictMultipleProducts(List<Article> products, int monthsAhead = 1)
        {
            var results = new List<DemandForecastResult>();

            foreach (var product in products)
            {
                var result = await PredictDemand(product.Id, product.ArticleName, product.StandardPrice, monthsAhead);
                if (result != null)
                {
                    results.Add(result);
                }
            }

            return results.OrderByDescending(r => r.PredictedSales).ToList();
        }
    }
}