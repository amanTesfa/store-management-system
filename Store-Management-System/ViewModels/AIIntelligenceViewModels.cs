namespace Store_Management_System.ViewModels
{
    public class AIIntelligenceViewModel
    {
        public bool IsAIEnabled { get; set; }
        public ModelStatus ModelStatus { get; set; } = new();
        public List<DemandForecastItem> DemandForecasts { get; set; } = new();
        public List<SmartAlertItem> SmartAlerts { get; set; } = new();
        public List<PriceOptimizationItem> PriceOptimizations { get; set; } = new();
        public List<SupplierScorecardItem> SupplierScores { get; set; } = new();
        public List<AnomalyItem> Anomalies { get; set; } = new();
    }

    public class ModelStatus
    {
        public bool IsTrained { get; set; }
        public int RecordsUsed { get; set; }
        public double Confidence { get; set; }
        public DateTime? LastTrained { get; set; }
    }

    public class DemandForecastItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int PredictedDemand { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class SmartAlertItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public decimal DailySales { get; set; }
        public int DaysRemaining { get; set; }
        public string Severity { get; set; } = string.Empty;
        public int SuggestedOrder { get; set; }
    }

    public class PriceOptimizationItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; }
        public decimal SuggestedPrice { get; set; }
        public string ExpectedImpact { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
    }

    public class SupplierScorecardItem
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public int Rating { get; set; } // 1-5 stars
        public int DeliveryDays { get; set; }
        public string PriceCompetitiveness { get; set; } = string.Empty;
        public int QualityScore { get; set; }
        public string Recommendation { get; set; } = string.Empty;
    }

    public class AnomalyItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string AnomalyType { get; set; } = string.Empty;
        public int ExpectedValue { get; set; }
        public int ActualValue { get; set; }
        public string Severity { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
    }
}