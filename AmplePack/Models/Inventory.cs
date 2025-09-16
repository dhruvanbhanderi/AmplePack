using System.ComponentModel.DataAnnotations;

namespace AmplePack.Models
{
    public class Inventory
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Item name is required")]
        [StringLength(100, ErrorMessage = "Item name cannot be longer than 100 characters")]
        public string ItemName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Available quantity is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Available quantity cannot be negative")]
        public decimal AvailableQuantity { get; set; }
        
        [Required(ErrorMessage = "Unit price is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be greater than 0")]
        [DataType(DataType.Currency)]
        public decimal UnitPrice { get; set; }
        
        [Required(ErrorMessage = "Unit is required")]
        [StringLength(20, ErrorMessage = "Unit cannot be longer than 20 characters")]
        public string Unit { get; set; } = string.Empty;
        
        [StringLength(50, ErrorMessage = "Category cannot be longer than 50 characters")]
        public string Category { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Reorder level is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Reorder level cannot be negative")]
        public decimal ReorderLevel { get; set; } = 10;
        
        // Computed properties
        public bool IsLowStock => AvailableQuantity <= ReorderLevel;
        public decimal TotalValue => AvailableQuantity * UnitPrice;
        
        // For backward compatibility - simplified
        public decimal Quantity 
        { 
            get => AvailableQuantity; 
            set => AvailableQuantity = value; 
        }
    }
}