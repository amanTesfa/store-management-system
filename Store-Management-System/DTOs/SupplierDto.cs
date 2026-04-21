namespace Store_Management_System.DTOs
{
    public class SupplierDto : BaseDto
    {
        public string Name { get; set; } = string.Empty;
        public string? ContactPerson { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? PaymentTerms { get; set; }
        public decimal? CreditLimit { get; set; }
        public bool IsActive { get; set; }
        public int? Rating { get; set; }
    }
}