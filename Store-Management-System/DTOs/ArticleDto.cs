namespace Store_Management_System.DTOs
{
    public class ArticleDto : BaseDto
    {
        public string ArticleCode { get; set; } = string.Empty;
        public string ArticleName { get; set; } = string.Empty;
        public string? ArticleType { get; set; }
        public int? ArticleCategory { get; set; }
        public string? CategoryName { get; set; }
        public string? ArticleGroup { get; set; }
        public string? Description { get; set; }
        public decimal StandardCost { get; set; }
        public decimal StandardPrice { get; set; }
        public decimal ReorderLevel { get; set; }
        public bool IsActive { get; set; }
        public bool IsStockable { get; set; }
        public bool IsPurchasable { get; set; }
        public bool IsSellable { get; set; }
        public decimal TaxRate { get; set; }
    }
}