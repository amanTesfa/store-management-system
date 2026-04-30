using Microsoft.ML;
using Store_Management_System.ML;

namespace Store_Management_System.Services
{
    public class ModelTrainerService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ModelTrainerService> _logger;

        public ModelTrainerService(IWebHostEnvironment environment, ILogger<ModelTrainerService> logger)
        {
            _environment = environment;
            _logger = logger;
        }

        public async Task<bool> TrainDemandForecastModel(List<SalesHistoryData> trainingData)
        {
            try
            {
                if (trainingData == null || trainingData.Count < 10)
                {
                    _logger.LogWarning($"Insufficient training data. Need at least 10 records, got {trainingData?.Count ?? 0}");
                    return false;
                }

                _logger.LogInformation($"Training model with {trainingData.Count} records");

                var mlContext = new MLContext();

                // Load data into ML.NET
                var dataView = mlContext.Data.LoadFromEnumerable(trainingData);

                // Build pipeline - label column must match property name in SalesHistoryData
                var pipeline = mlContext.Transforms.Concatenate("Features",
                        "Year", "Month", "DayOfWeek", "IsWeekend", "Price")
                    .Append(mlContext.Regression.Trainers.Sdca(
                        labelColumnName: "Quantity",  // ← Must match property name
                        maximumNumberOfIterations: 100));

                // Train model
                var model = pipeline.Fit(dataView);

                // Save model
                var modelPath = Path.Combine(_environment.ContentRootPath, "ML", "Models", "demand_forecast.zip");
                var directory = Path.GetDirectoryName(modelPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory!);
                }

                mlContext.Model.Save(model, dataView.Schema, modelPath);
                _logger.LogInformation($"Model saved to {modelPath}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error training demand forecast model");
                return false;
            }
        }
    }
}