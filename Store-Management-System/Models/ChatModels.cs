using Microsoft.ML.Data;  // ← Add this using

namespace Store_Management_System.Models
{
    public class ChatQueryRequest
    {
        public string Query { get; set; } = string.Empty;
    }

    public class ChatbotResponse
    {
        public string Intent { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public object? Data { get; set; }
        public bool Success { get; set; } = true;
    }

    public enum ChatIntent
    {
        DemandForecast,
        CurrentStock,
        LowStockAlert,
        ProductInfo,
        PriceOptimization,
        SupplierInfo,
        SalesReport,
        GeneralHelp,
        Unknown
    }

    // Fix: Add LoadColumn attributes
    public class IntentTrainingData
    {
        [LoadColumn(0)]  // ← First column in CSV
        public string Text { get; set; } = string.Empty;

        [LoadColumn(1)]  // ← Second column in CSV
        public string Intent { get; set; } = string.Empty;
    }

    public class IntentPrediction
    {
        [ColumnName("PredictedLabel")]
        public string PredictedIntent { get; set; } = string.Empty;

        public float[]? Score { get; set; }
    }
}