using System.ComponentModel.DataAnnotations;

namespace AmplePack.Models
{
    public class OrderDetailInput
    {
        [Required(ErrorMessage = "Customer is required")]
        public int CustomerId { get; set; }
        
        public int? CustomerProductId { get; set; }
        
        public string BoxType { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; } = 1;
        
        [Required(ErrorMessage = "Price per box is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price per box must be greater than 0")]
        public decimal PricePerBox { get; set; }
        
        public DateTime? DeliveryDate { get; set; }
        public string? Notes { get; set; }
        
        // Manual entry properties
        [Range(0.1, 1000, ErrorMessage = "Length must be between 0.1 and 1000")]
        public decimal? Length { get; set; }
        
        [Range(0.1, 1000, ErrorMessage = "Width must be between 0.1 and 1000")]
        public decimal? Width { get; set; }
        
        [Range(0.1, 1000, ErrorMessage = "Height must be between 0.1 and 1000")]
        public decimal? Height { get; set; }
        
        [Range(80, 1000, ErrorMessage = "GSM must be between 80 and 1000")]
        public int? GSM { get; set; }
        
        public string? PaperType { get; set; }
        public string? PrintingType { get; set; }
    }
}