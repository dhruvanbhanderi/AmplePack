using System.ComponentModel.DataAnnotations;

namespace AmplePack.Models
{
    /// <summary>
    /// Model for box price calculator input parameters
    /// </summary>
    public class BoxCalculatorInput
    {
        // Box Dimensions (in inches)
        [Required(ErrorMessage = "Length is required")]
        [Range(0.1, 400, ErrorMessage = "Length must be between 0.1 and 400 inches")]
        [Display(Name = "Length (inches)")]
        public decimal Length { get; set; }

        [Required(ErrorMessage = "Width is required")]
        [Range(0.1, 400, ErrorMessage = "Width must be between 0.1 and 400 inches")]
        [Display(Name = "Width (inches)")]
        public decimal Width { get; set; }

        [Required(ErrorMessage = "Height is required")]
        [Range(0.1, 400, ErrorMessage = "Height must be between 0.1 and 400 inches")]
        [Display(Name = "Height (inches)")]
        public decimal Height { get; set; }

        // Board Specifications
        [Required(ErrorMessage = "Board GSM is required")]
        [Range(120, 1000, ErrorMessage = "Board GSM must be between 120 and 1000")]
        [Display(Name = "Board GSM")]
        public int BoardGSM { get; set; } = 200;

        [Required(ErrorMessage = "Board type is required")]
        [Display(Name = "Board Type")]
        public string BoardType { get; set; } = "3 Ply";

        [Range(0.1, 10.0, ErrorMessage = "Compression ratio must be between 0.1 and 10.0")]
        [Display(Name = "Compression Ratio")]
        public decimal CompressionRatio { get; set; } = 1.32m;

        // Quantity and Production
        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, 1000000, ErrorMessage = "Quantity must be between 1 and 1,000,000")]
        [Display(Name = "Quantity")]
        public int Quantity { get; set; } = 1000;

        // Material Rates (per m²)
        [Required(ErrorMessage = "Board rate is required")]
        [Range(0.01, 1000, ErrorMessage = "Board rate must be between 0.01 and 1000")]
        [Display(Name = "Board Rate per m² (Rs.)")]
        public decimal BoardRatePerSqM { get; set; } = 65.0m;

        // Sheet Specifications (in inches)
        [Required(ErrorMessage = "Sheet length is required")]
        [Range(4, 200, ErrorMessage = "Sheet length must be between 4 and 200 inches")]
        [Display(Name = "Sheet Length (inches)")]
        public decimal SheetLength { get; set; } = 47.2m; // ~1200mm

        [Required(ErrorMessage = "Sheet width is required")]
        [Range(4, 200, ErrorMessage = "Sheet width must be between 4 and 200 inches")]
        [Display(Name = "Sheet Width (inches)")]
        public decimal SheetWidth { get; set; } = 39.4m; // ~1000mm

        // Production Costs
        [Range(0, 1000, ErrorMessage = "Printing cost must be between 0 and 1000")]
        [Display(Name = "Printing Cost per m² (Rs.)")]
        public decimal PrintingCostPerSqM { get; set; } = 12.0m;

        [Range(0, 100, ErrorMessage = "Labor cost must be between 0 and 100")]
        [Display(Name = "Labor Cost per Box (Rs.)")]
        public decimal LaborCostPerBox { get; set; } = 2.0m;

        [Range(0, 500, ErrorMessage = "Die cutting cost must be between 0 and 500")]
        [Display(Name = "Die Cutting Cost per m² (Rs.)")]
        public decimal DieCuttingCostPerSqM { get; set; } = 8.0m;

        // Business Costs (as percentages)
        [Range(0, 100, ErrorMessage = "Overhead percentage must be between 0 and 100")]
        [Display(Name = "Overhead (%)")]
        public decimal OverheadPercentage { get; set; } = 15.0m;

        [Range(0, 100, ErrorMessage = "Profit margin must be between 0 and 100")]
        [Display(Name = "Profit Margin (%)")]
        public decimal ProfitMarginPercentage { get; set; } = 20.0m;

        // Additional Options
        [Range(0, 100, ErrorMessage = "Discount percentage must be between 0 and 100")]
        [Display(Name = "Discount (%)")]
        public decimal DiscountPercentage { get; set; } = 0.0m;

        [Range(0, 1000, ErrorMessage = "Shipping cost must be between 0 and 1000")]
        [Display(Name = "Shipping Cost per Order (Rs.)")]
        public decimal ShippingCostPerOrder { get; set; } = 50.0m;

        // Waste and Efficiency
        [Range(0, 50, ErrorMessage = "Waste percentage must be between 0 and 50")]
        [Display(Name = "Waste Allowance (%)")]
        public decimal WastePercentage { get; set; } = 5.0m;

        // Display settings
        [Display(Name = "Show Detailed Breakdown")]
        public bool ShowDetailedBreakdown { get; set; } = true;

        [Display(Name = "Include GST (18%)")]
        public bool IncludeGST { get; set; } = true;
    }
}