namespace Store_Management_System.DTOs
{
    public class CustomerDto : BaseDto
    {
        public string ConsigneeCode { get; set; } = string.Empty;
        public string ConsigneeName { get; set; } = string.Empty;
        public string ConsigneeType { get; set; } = string.Empty;
        public string? ContactPerson { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Mobile { get; set; }
        public string? ShippingAddress { get; set; }
        public string? BillingAddress { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Country { get; set; }
        public string? TaxNumber { get; set; }
        public string? PaymentTerms { get; set; }
        public decimal? CreditLimit { get; set; }
        public decimal CurrentBalance { get; set; }
        public string? PriceLevel { get; set; }
        public bool IsActive { get; set; }
        public string? Tin { get; set; }
        public string? Industry { get; set; }
        public int? NumberOfEmployees { get; set; }
    }
}