using System.ComponentModel.DataAnnotations;

namespace AmplePack.Models
{
    public class OrderDetail
    {
        public int Id { get; set; }
        
        [Required]
        public int OrderId { get; set; }
        public Order? Order { get; set; }
        
        // Link to CustomerProduct (saved product pattern)
        public int? CustomerProductId { get; set; }
        public CustomerProduct? CustomerProduct { get; set; }
        
        [Required(ErrorMessage = "Box type is required")]
        [StringLength(50, ErrorMessage = "Box type cannot be longer than 50 characters")]
        public string BoxType { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Size is required")]
        [StringLength(20, ErrorMessage = "Size cannot be longer than 20 characters")]
        public string Size { get; set; } = string.Empty; // Format: LxWxH
        
        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }
        
        [Required(ErrorMessage = "Price per box is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price per box must be greater than 0")]
        [DataType(DataType.Currency)]
        public decimal PricePerBox { get; set; }
        
        // Additional order-specific details
        public DateTime? DeliveryDate { get; set; }
        public string Notes { get; set; } = string.Empty;
        
        // Computed property for display
        public string ProductDisplay => CustomerProduct?.ProductName ?? $"{BoxType} ({Size})";
    }
}