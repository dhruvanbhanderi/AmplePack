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
        [Display(Name = "Box Type", Description = "Type of corrugated box")]
        public string BoxType { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Size is required")]
        [StringLength(30, ErrorMessage = "Size cannot be longer than 30 characters")]
        [Display(Name = "Size (inches)", Description = "Box dimensions in LxWxH format (inches)")]
        public string Size { get; set; } = string.Empty; // Format: L"xW"xH" (e.g., 12"x10"x8")
        
        // Individual Dimensions (for easier calculation and display)
        [Range(2, 72, ErrorMessage = "Length must be between 2 and 72 inches")]
        [Display(Name = "Length (inches)")]
        public decimal LengthInches { get; set; }
        
        [Range(2, 72, ErrorMessage = "Width must be between 2 and 72 inches")]
        [Display(Name = "Width (inches)")]
        public decimal WidthInches { get; set; }
        
        [Range(1, 48, ErrorMessage = "Height must be between 1 and 48 inches")]
        [Display(Name = "Height (inches)")]
        public decimal HeightInches { get; set; }
        
        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        [Display(Name = "Quantity (boxes)")]
        public int Quantity { get; set; }
        
        [Required(ErrorMessage = "Price per box is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price per box must be greater than 0")]
        [DataType(DataType.Currency)]
        [Display(Name = "Price per Box (Rs.)")]
        public decimal PricePerBox { get; set; }
        
        // Construction details
        [StringLength(20)]
        [Display(Name = "Construction Type")]
        public string ConstructionType { get; set; } = "single-wall"; // single-wall, double-wall
        
        [Range(80, 1000)]
        [Display(Name = "GSM")]
        public int GSM { get; set; }
        
        [StringLength(50)]
        [Display(Name = "Paper Type")]
        public string PaperType { get; set; } = string.Empty;
        
        [StringLength(50)]
        [Display(Name = "Printing Type")]
        public string PrintingType { get; set; } = string.Empty;
        
        // Additional order-specific details
        [Display(Name = "Delivery Date")]
        public DateTime? DeliveryDate { get; set; }
        
        [StringLength(500)]
        [Display(Name = "Notes")]
        public string Notes { get; set; } = string.Empty;
        
        // Computed Properties
        [Display(Name = "Product")]
        public string ProductDisplay => CustomerProduct?.ProductName ?? $"{BoxType} ({SizeDisplayInches})";
        
        [Display(Name = "Size")]
        public string SizeDisplayInches => $"{LengthInches}\" × {WidthInches}\" × {HeightInches}\"";
        
        [Display(Name = "Box Area")]
        public decimal BoxAreaSquareInches => LengthInches * WidthInches;
        
        [Display(Name = "Box Volume")]
        public decimal BoxVolumeCubicInches => LengthInches * WidthInches * HeightInches;
        
        [Display(Name = "Total Amount")]
        public decimal TotalAmount => Quantity * PricePerBox;
        
        [Display(Name = "Perimeter")]
        public decimal PerimeterInches => 2 * (LengthInches + WidthInches);
        
        [Display(Name = "Total Area")]
        public decimal TotalAreaSquareInches => Quantity * BoxAreaSquareInches;
        
        [Display(Name = "Total Volume")]
        public decimal TotalVolumeCubicInches => Quantity * BoxVolumeCubicInches;
        
        [Display(Name = "Full Specifications")]
        public string FullSpecifications => $"{SizeDisplayInches} - {ConstructionType} - {GSM} GSM - {PaperType} - {PrintingType}";
        
        // Update Size string when dimensions change
        public void UpdateSizeFromDimensions()
        {
            Size = $"{LengthInches}\"×{WidthInches}\"×{HeightInches}\"";
        }
        
        // Parse dimensions from Size string
        public void ParseDimensionsFromSize()
        {
            if (string.IsNullOrEmpty(Size)) return;
            
            try
            {
                // Handle formats like "12"x10"x8" or "12×10×8" or "12x10x8"
                var cleanSize = Size.Replace("\"", "").Replace("×", "x").Replace("X", "x");
                var parts = cleanSize.Split('x');
                
                if (parts.Length >= 2)
                {
                    if (decimal.TryParse(parts[0].Trim(), out decimal length))
                        LengthInches = length;
                    if (decimal.TryParse(parts[1].Trim(), out decimal width))
                        WidthInches = width;
                    if (parts.Length >= 3 && decimal.TryParse(parts[2].Trim(), out decimal height))
                        HeightInches = height;
                }
            }
            catch
            {
                // If parsing fails, keep existing values
            }
        }
    }
}