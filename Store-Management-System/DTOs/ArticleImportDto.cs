namespace Store_Management_System.DTOs
{
    public class ArticleImportDto
    {
        public string ArticleName { get; set; } = string.Empty;
        public string ArticleCode { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal StandardPrice { get; set; }
        public decimal StandardCost { get; set; }
        public string BaseUnit { get; set; } = "Piece";
        public string? Description { get; set; }
        public string? ArticleGroup { get; set; }
        public decimal ReorderLevel { get; set; }
        public decimal SafetyStock { get; set; }
        public decimal TaxRate { get; set; }
        public string? PrimaryBarcode { get; set; }
    }

    public class ImportResult
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}