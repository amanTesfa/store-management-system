using Microsoft.ML.Data;

namespace Store_Management_System.ML
{
    // Training data model - what we learn from
    public class SalesHistoryData
    {
        [LoadColumn(0)]
        public float ProductId { get; set; }

        [LoadColumn(1)]
        public float Year { get; set; }

        [LoadColumn(2)]
        public float Month { get; set; }

        [LoadColumn(3)]
        public float DayOfWeek { get; set; }

        [LoadColumn(4)]
        public float IsWeekend { get; set; }

        [LoadColumn(5)]
        public float Price { get; set; }

        [LoadColumn(6)]
        public float Quantity { get; set; }  // This is what we predict (Label)
    }

    // Prediction output
    public class SalesPrediction
    {
        [ColumnName("Score")]
        public float PredictedQuantity { get; set; }
    }

    // For API responses
    public class DemandForecastResult
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Month { get; set; }
        public float PredictedSales { get; set; }
        public float Confidence { get; set; }
        public string Recommendation { get; set; } = string.Empty;
    }
}