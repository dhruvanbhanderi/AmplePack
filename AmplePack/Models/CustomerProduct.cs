using System.ComponentModel.DataAnnotations;

namespace AmplePack.Models
{
    public class CustomerProduct
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Customer is required")]
        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }
        
        [Required(ErrorMessage = "Product name is required")]
        [StringLength(100, ErrorMessage = "Product name cannot be longer than 100 characters")]
        public string ProductName { get; set; } = string.Empty;
        
        [StringLength(500, ErrorMessage = "Description cannot be longer than 500 characters")]
        public string Description { get; set; } = string.Empty;
        
        // Box Dimensions
        [Required(ErrorMessage = "Length is required")]
        [Range(0.1, 1000, ErrorMessage = "Length must be between 0.1 and 1000")]
        public decimal Length { get; set; }
        
        [Required(ErrorMessage = "Width is required")]
        [Range(0.1, 1000, ErrorMessage = "Width must be between 0.1 and 1000")]
        public decimal Width { get; set; }
        
        [Required(ErrorMessage = "Height is required")]
        [Range(0.1, 1000, ErrorMessage = "Height must be between 0.1 and 1000")]
        public decimal Height { get; set; }
        
        // Box Specifications
        [Required(ErrorMessage = "GSM is required")]
        [Range(80, 1000, ErrorMessage = "GSM must be between 80 and 1000")]
        public int GSM { get; set; }
        
        [Required(ErrorMessage = "Paper type is required")]
        [StringLength(50, ErrorMessage = "Paper type cannot be longer than 50 characters")]
        public string PaperType { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Printing type is required")]
        [StringLength(50, ErrorMessage = "Printing type cannot be longer than 50 characters")]
        public string PrintingType { get; set; } = string.Empty;
        
        // Pricing
        [Required(ErrorMessage = "Price per box is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price per box must be greater than 0")]
        [DataType(DataType.Currency)]
        public decimal PricePerBox { get; set; }
        
        [Range(1, int.MaxValue, ErrorMessage = "Default quantity must be at least 1")]
        public int DefaultQuantity { get; set; } = 1;
        
        // Metadata
        [StringLength(20, ErrorMessage = "Category cannot be longer than 20 characters")]
        public string Category { get; set; } = string.Empty; // e.g., "Standard", "Premium", "Special"
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        public DateTime? LastOrderDate { get; set; }
        
        public int TotalOrdersCount { get; set; } = 0;
        
        // Computed Properties
        public string SizeDisplay => $"{Length}×{Width}×{Height}";
        
        public string SpecificationDisplay => $"{GSM} GSM, {PaperType}, {PrintingType}";
    }
}