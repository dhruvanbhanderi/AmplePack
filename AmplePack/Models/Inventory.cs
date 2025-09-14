using System.ComponentModel.DataAnnotations;

namespace AmplePack.Models
{
    public class Inventory
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Item name is required")]
        [StringLength(100, ErrorMessage = "Item name cannot be longer than 100 characters")]
        public string ItemName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Quantity is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Quantity cannot be negative")]
        public decimal Quantity { get; set; }
        
        [Required(ErrorMessage = "Unit is required")]
        [StringLength(20, ErrorMessage = "Unit cannot be longer than 20 characters")]
        public string Unit { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Reorder level is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Reorder level cannot be negative")]
        public decimal ReorderLevel { get; set; }
    }
}