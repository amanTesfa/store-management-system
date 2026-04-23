using Microsoft.ML;
using Store_Management_System.Models;

namespace Store_Management_System.Services
{
    public class IntentModelTrainer
    {
        private readonly MLContext _mlContext;
        private readonly string _dataPath;
        private readonly string _modelPath;
        private readonly ILogger<IntentModelTrainer> _logger;

        public IntentModelTrainer(IWebHostEnvironment environment, ILogger<IntentModelTrainer> logger)
        {
            _mlContext = new MLContext(seed: 42); // Fixed seed for reproducibility
            _dataPath = Path.Combine(environment.ContentRootPath, "ML", "Data", "intents.csv");
            _modelPath = Path.Combine(environment.ContentRootPath, "ML", "Models", "intent_classifier.zip");
            _logger = logger;
        }

        public (ITransformer Model, double Accuracy) TrainModel()
        {
            _logger.LogInformation("Starting intent model training...");

            // Load data
            var dataView = _mlContext.Data.LoadFromTextFile<IntentTrainingData>(
                _dataPath,
                separatorChar: ',',
                hasHeader: true,
                allowQuoting: true);

            // Split data into training and testing sets (80/20)
            var split = _mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);
            var trainingData = split.TrainSet;
            var testingData = split.TestSet;

            _logger.LogInformation($"Training data loaded. Rows: {trainingData.GetRowCount()}");

            // Build the pipeline
            var pipeline = _mlContext.Transforms.Conversion.MapValueToKey("Label", nameof(IntentTrainingData.Intent))
                .Append(_mlContext.Transforms.Text.FeaturizeText("Features", nameof(IntentTrainingData.Text)))
                .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(
                    labelColumnName: "Label",
                    featureColumnName: "Features"))
                .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            // Train the model
            var model = pipeline.Fit(trainingData);

            // Evaluate
            var predictions = model.Transform(testingData);
            var metrics = _mlContext.MulticlassClassification.Evaluate(predictions);

            _logger.LogInformation($"Model trained successfully!");
            _logger.LogInformation($"Macro Accuracy: {metrics.MacroAccuracy:P2}");
            _logger.LogInformation($"Micro Accuracy: {metrics.MicroAccuracy:P2}");
            _logger.LogInformation($"Log Loss: {metrics.LogLoss:F4}");

            // Display per-class metrics
            _logger.LogInformation("Per-class performance:");
            if (metrics.PerClassLogLoss != null)
            {
                var labelNames = new[] { "DemandForecast", "CurrentStock", "LowStockAlert", "ProductInfo",
                                        "PriceOptimization", "SupplierInfo", "SalesReport", "GeneralHelp" };
                for (int i = 0; i < metrics.PerClassLogLoss.Count; i++)
                {
                    var className = i < labelNames.Length ? labelNames[i] : $"Class{i}";
                    _logger.LogInformation($"  {className}: LogLoss={metrics.PerClassLogLoss[i]:F4}");
                }
            }

            // Save the model
            var directory = Path.GetDirectoryName(_modelPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory!);
            }
            _mlContext.Model.Save(model, dataView.Schema, _modelPath);
            _logger.LogInformation($"Model saved to: {_modelPath}");

            return (model, metrics.MacroAccuracy);
        }

        public void TrainIfNotExists()
        {
            if (!File.Exists(_modelPath))
            {
                _logger.LogWarning("Intent model not found. Training new model...");
                TrainModel();
            }
            else
            {
                _logger.LogInformation($"Intent model already exists at: {_modelPath}");
                _logger.LogInformation($"Last trained: {File.GetLastWriteTime(_modelPath)}");
            }
        }
    }
}