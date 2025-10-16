using System.ComponentModel.DataAnnotations;

namespace AmplePack.Models
{
    public class BoxCalculatorRequest
    {
        [Display(Name = "Length (in)")]
        [Required(ErrorMessage = "Length is required")]
        [Range(0.1, 1000, ErrorMessage = "Length must be between 0.1 and 1000 inches")]
        public decimal Length { get; set; }

        [Display(Name = "Width (in)")]
        [Required(ErrorMessage = "Width is required")]
        [Range(0.1, 1000, ErrorMessage = "Width must be between 0.1 and 1000 inches")]
        public decimal Width { get; set; }

        [Display(Name = "Height (in)")]
        [Required(ErrorMessage = "Height is required")]
        [Range(0.1, 1000, ErrorMessage = "Height must be between 0.1 and 1000 inches")]
        public decimal Height { get; set; }

        [Display(Name = "Board Type")]
        [Required(ErrorMessage = "Board type is required")]
        public string BoardType { get; set; } = "3 Ply";

        [Display(Name = "Quantity")]
        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, 1000000, ErrorMessage = "Quantity must be between 1 and 1,000,000")]
        public int Quantity { get; set; } = 1000;

        // Single Sheet Size (Industry Standard)
        [Display(Name = "Sheet Length (in)")]
        [Required(ErrorMessage = "Sheet length is required")]
        [Range(10, 200, ErrorMessage = "Sheet length must be between 10 and 200 inches")]
        public decimal SheetLength { get; set; } = 40;

        [Display(Name = "Sheet Width (in)")]
        [Required(ErrorMessage = "Sheet width is required")]
        [Range(10, 200, ErrorMessage = "Sheet width must be between 10 and 200 inches")]
        public decimal SheetWidth { get; set; } = 30;

        // Paper 1 (Outer Liner)
        [Display(Name = "Paper 1 - GSM")]
        [Required(ErrorMessage = "Paper 1 GSM is required")]
        [Range(100, 400, ErrorMessage = "GSM must be between 100 and 400")]
        public int Paper1GSM { get; set; } = 150;

        [Display(Name = "Paper 1 - Rate (Rs./kg)")]
        [Required(ErrorMessage = "Paper 1 rate is required")]
        [Range(0.01, 1000, ErrorMessage = "Paper 1 rate must be between 0.01 and 1000")]
        public decimal Paper1RatePerKg { get; set; } = 45.00m;

        // Paper 2 (Inner Liner) - Optional for 3-ply, Required for 5-ply and above
        [Display(Name = "Paper 2 - GSM")]
        [Range(0, 400, ErrorMessage = "GSM must be between 0 and 400")]
        public int Paper2GSM { get; set; } = 0;

        [Display(Name = "Paper 2 - Rate (Rs./kg)")]
        [Range(0, 1000, ErrorMessage = "Paper 2 rate must be between 0 and 1000")]
        public decimal Paper2RatePerKg { get; set; } = 0;

        // Medium Paper (Corrugated middle layer)
        [Display(Name = "Medium - GSM")]
        [Required(ErrorMessage = "Medium GSM is required")]
        [Range(80, 200, ErrorMessage = "Medium GSM must be between 80 and 200")]
        public int MediumGSM { get; set; } = 120;

        [Display(Name = "Medium - Rate (Rs./kg)")]
        [Required(ErrorMessage = "Medium rate is required")]
        [Range(0.01, 1000, ErrorMessage = "Medium rate must be between 0.01 and 1000")]
        public decimal MediumRatePerKg { get; set; } = 42.00m;

        // Processing costs
        [Display(Name = "Printing Cost (Rs./sheet)")]
        [Range(0, 100, ErrorMessage = "Printing cost must be between 0 and 100")]
        public decimal PrintingCostPerSheet { get; set; } = 2.50m;

        [Display(Name = "Die Cutting Cost (Rs./sheet)")]
        [Range(0, 100, ErrorMessage = "Die cutting cost must be between 0 and 100")]
        public decimal DieCuttingCostPerSheet { get; set; } = 1.50m;

        [Display(Name = "Labor Cost (Rs./box)")]
        [Range(0, 10, ErrorMessage = "Labor cost must be between 0 and 10")]
        public decimal LaborCostPerBox { get; set; } = 0.50m;

        // Business parameters
        [Display(Name = "Overhead Percentage (%)")]
        [Range(0, 50, ErrorMessage = "Overhead percentage must be between 0 and 50")]
        public decimal OverheadPercentage { get; set; } = 15.0m;

        [Display(Name = "Profit Margin (%)")]
        [Range(0, 100, ErrorMessage = "Profit margin must be between 0 and 100")]
        public decimal ProfitMarginPercentage { get; set; } = 20.0m;

        [Display(Name = "Wastage Factor (%)")]
        [Range(5, 25, ErrorMessage = "Wastage factor must be between 5 and 25")]
        public decimal WastageFactorPercentage { get; set; } = 10.0m;

        [Display(Name = "Include GST")]
        public bool IncludeGST { get; set; } = true;

        [Display(Name = "GST Rate (%)")]
        [Range(0, 50, ErrorMessage = "GST rate must be between 0 and 50")]
        public decimal GSTRate { get; set; } = 18.0m;
    }

    public class BoxCalculatorResult
    {
        // Box specifications
        public decimal Length { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public string BoardType { get; set; } = "";
        public int Quantity { get; set; }
        
        // Single sheet analysis
        public SheetAnalysis SheetAnalysis { get; set; } = new();
        
        // Cost breakdown
        public CostBreakdown CostBreakdown { get; set; } = new();
        
        // Final pricing
        public decimal FinalPricePerBox { get; set; }
        public decimal FinalPricePerBoxWithGST { get; set; }
        public decimal TotalOrderValue { get; set; }
        public decimal TotalOrderValueWithGST { get; set; }
        
        // Efficiency metrics
        public decimal MaterialEfficiency { get; set; }
        public decimal WastagePercentage { get; set; }
        public decimal ProfitPerBox { get; set; }
    }

    public class SheetAnalysis
    {
        public decimal SheetLength { get; set; }
        public decimal SheetWidth { get; set; }
        public decimal SheetArea { get; set; }
        
        // Apps calculation (number of boxes from one sheet)
        public int AppsLength { get; set; } // How many boxes fit along length
        public int AppsWidth { get; set; }  // How many boxes fit along width
        public int TotalApps { get; set; }  // Total boxes per sheet
        
        // Box layout dimensions
        public decimal BoxLayoutLength { get; set; } // Length + Height for unfolded box
        public decimal BoxLayoutWidth { get; set; }  // Width + Height for unfolded box
        
        // Utilization
        public decimal UsedArea { get; set; }
        public decimal WasteArea { get; set; }
        public decimal UtilizationPercentage { get; set; }
        
        // Material costs per sheet
        public decimal Paper1CostPerSheet { get; set; }
        public decimal Paper2CostPerSheet { get; set; }
        public decimal MediumCostPerSheet { get; set; }
        public decimal TotalMaterialCostPerSheet { get; set; }
        
        // Processing cost per sheet
        public decimal ProcessingCostPerSheet { get; set; }
        public decimal TotalCostPerSheet { get; set; }
        
        // Per box costs from this sheet
        public decimal MaterialCostPerBox { get; set; }
        public decimal ProcessingCostPerBox { get; set; }
        public decimal TotalCostPerBox { get; set; }
    }

    public class CostBreakdown
    {
        // Material costs breakdown
        public decimal Paper1CostPerBox { get; set; }
        public decimal Paper2CostPerBox { get; set; }
        public decimal MediumCostPerBox { get; set; }
        public decimal TotalMaterialCostPerBox { get; set; }
        public decimal WastageCost { get; set; }
        
        // Processing costs
        public decimal PrintingCostPerBox { get; set; }
        public decimal DieCuttingCostPerBox { get; set; }
        public decimal LaborCostPerBox { get; set; }
        
        // Business costs
        public decimal SubtotalPerBox { get; set; }
        public decimal OverheadCostPerBox { get; set; }
        public decimal TotalCostPerBox { get; set; }
        public decimal ProfitPerBox { get; set; }
        public decimal SellingPricePerBox { get; set; }
        
        // Tax
        public decimal GSTAmountPerBox { get; set; }
        public decimal FinalPricePerBox { get; set; }
        
        // Summary for display
        public List<CostItem> CostItems { get; set; } = new();
    }

    public class CostItem
    {
        public string Description { get; set; } = "";
        public decimal AmountPerBox { get; set; }
        public decimal TotalAmount { get; set; }
        public string Category { get; set; } = "";
        public bool IsUserEditable { get; set; }
    }

    public static class BoardTypeConstants
    {
        // Industry standard board configurations
        public static readonly Dictionary<string, BoardConfiguration> BoardConfigurations = new()
        {
            { 
                "3 Ply", 
                new BoardConfiguration 
                { 
                    Name = "3 Ply (Single Wall)", 
                    RequiredPapers = 2, // Outer + Medium
                    Thickness = 3.5m,
                    Description = "Single wall corrugated board"
                } 
            },
            { 
                "5 Ply", 
                new BoardConfiguration 
                { 
                    Name = "5 Ply (Double Wall)", 
                    RequiredPapers = 3, // Outer + Inner + 2 Mediums  
                    Thickness = 6.5m,
                    Description = "Double wall corrugated board"
                } 
            },
            { 
                "7 Ply", 
                new BoardConfiguration 
                { 
                    Name = "7 Ply (Triple Wall)", 
                    RequiredPapers = 4, // Outer + 2 Inner + 3 Mediums
                    Thickness = 15.0m,
                    Description = "Triple wall corrugated board"
                } 
            }
        };

        public static readonly List<string> AvailableBoardTypes = new()
        {
            "3 Ply",
            "5 Ply", 
            "7 Ply"
        };

        public static readonly List<int> StandardGSMValues = new()
        {
            80, 90, 100, 110, 120, 125, 130, 135, 140, 145, 150, 155, 160, 165, 170, 175, 180, 185, 190, 195, 200, 220, 250, 300
        };
    }

    public class BoardConfiguration
    {
        public string Name { get; set; } = "";
        public int RequiredPapers { get; set; }
        public decimal Thickness { get; set; }
        public string Description { get; set; } = "";
    }
}