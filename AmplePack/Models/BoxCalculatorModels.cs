using System.ComponentModel.DataAnnotations;

namespace AmplePack.Models
{
    public class BoxCalculatorRequest
    {
        // ✅ REMOVED - Box dimensions (Length, Width, Height)
        // ✅ ADDED - Manual Apps per Sheet Input
        [Display(Name = "Apps per Sheet")]
        [Required(ErrorMessage = "Apps per sheet is required")]
        [Range(1, 1000, ErrorMessage = "Apps must be between 1 and 1000")]
        public int AppsPerSheet { get; set; } = 1; // ✅ CHANGED - Default 1 (minimum)

        [Display(Name = "Board Type")]
        [Required(ErrorMessage = "Board type is required")]
        [RegularExpression("^(3 Ply|5 Ply|7 Ply)$", ErrorMessage = "Invalid board type. Must be 3 Ply, 5 Ply, or 7 Ply")]
        public string BoardType { get; set; } = "3 Ply";

        [Display(Name = "Quantity")]
        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, 1000000, ErrorMessage = "Quantity must be between 1 and 1,000,000")]
        public int Quantity { get; set; } = 1000;

        // Single Sheet Size (Industry Standard)
        [Display(Name = "Sheet Length (in)")]
        [Required(ErrorMessage = "Sheet length is required")]
        [Range(10, 200, ErrorMessage = "Sheet length must be between 10 and 200 inches")]
        public decimal SheetLength { get; set; } = 42;

        [Display(Name = "Sheet Width (in)")]
        [Required(ErrorMessage = "Sheet width is required")]
        [Range(10, 200, ErrorMessage = "Sheet width must be between 10 and 200 inches")]
        public decimal SheetWidth { get; set; } = 30;

        // Paper 1 (Top/Outer Liner) - Always Required
        [Display(Name = "Paper 1 - GSM")]
        [Required(ErrorMessage = "Top liner GSM is required")]
        [Range(100, 400, ErrorMessage = "GSM must be between 100 and 400")]
        public int Paper1GSM { get; set; } = 150;

        [Display(Name = "Paper 1 - Rate (Rs./kg)")]
        [Required(ErrorMessage = "Top liner rate is required")]
        [Range(0, 1000, ErrorMessage = "Top liner rate must be between 0 and 1000")]
        public decimal Paper1RatePerKg { get; set; } = 50.00m;

        // Paper 2 (Bottom/Inner Liner) - Required for ALL ply types (Industry Standard)
        [Display(Name = "Paper 2 - GSM")]
        [Required(ErrorMessage = "Bottom liner GSM is required for all board types")]
        [Range(100, 400, ErrorMessage = "Bottom liner GSM must be between 100 and 400")]
        public int Paper2GSM { get; set; } = 125;

        [Display(Name = "Paper 2 - Rate (Rs./kg)")]
        [Required(ErrorMessage = "Bottom liner rate is required")]
        [Range(0, 1000, ErrorMessage = "Bottom liner rate must be between 0 and 1000")]
        public decimal Paper2RatePerKg { get; set; } = 45.00m;

        // Medium Paper (Corrugated middle layer) - Always Required
        [Display(Name = "Medium - GSM")]
        [Required(ErrorMessage = "Medium GSM is required")]
        [Range(80, 200, ErrorMessage = "Medium GSM must be between 80 and 200")]
        public int MediumGSM { get; set; } = 120;

        [Display(Name = "Medium - Rate (Rs./kg)")]
        [Required(ErrorMessage = "Medium rate is required")]
        [Range(0, 1000, ErrorMessage = "Medium rate must be between 0 and 1000")]
        public decimal MediumRatePerKg { get; set; } = 42.00m;

        // Flute Type - Industry Standard
        [Display(Name = "Flute Type")]
        [Required(ErrorMessage = "Flute type is required")]
        [RegularExpression("^(B|C|E|BC|EB)$", ErrorMessage = "Invalid flute type. Must be B, C, E, BC, or EB")]
        public string FluteType { get; set; } = "B";

        // Processing costs - ZERO FIRST APPROACH (User adds as needed)
        [Display(Name = "Printing Cost (Rs./sheet)")]
        [Range(0, 100, ErrorMessage = "Printing cost must be between 0 and 100")]
        public decimal PrintingCostPerSheet { get; set; } = 0m;

        [Display(Name = "Die Cutting Cost (Rs./sheet)")]
        [Range(0, 100, ErrorMessage = "Die cutting cost must be between 0 and 100")]
        public decimal DieCuttingCostPerSheet { get; set; } = 0m;

        [Display(Name = "Labor Cost (Rs./box)")]
        [Range(0, 10, ErrorMessage = "Labor cost must be between 0 and 10")]
        public decimal LaborCostPerBox { get; set; } = 0m;

        [Display(Name = "Pin Cost (Rs./box)")]
        [Range(0, 5, ErrorMessage = "Pin cost must be between 0 and 5")]
        public decimal PinCostPerBox { get; set; } = 0m;

        [Display(Name = "Lamination Cost (Rs./box)")]
        [Range(0, 20, ErrorMessage = "Lamination cost must be between 0 and 20")]
        public decimal LaminationCostPerBox { get; set; } = 0m;

        [Display(Name = "Transport Cost (Rs./box)")]
        [Range(0, 20, ErrorMessage = "Transport cost must be between 0 and 20")]
        public decimal TransportCostPerBox { get; set; } = 0m;

        // Profit Margin
        [Display(Name = "Profit Margin (%)")]
        [Range(0, 100, ErrorMessage = "Profit margin must be between 0 and 100")]
        public decimal ProfitMarginPercentage { get; set; } = 0m;

        // GST Settings - START WITH NO GST
        [Display(Name = "Include GST")]
        public bool IncludeGST { get; set; } = false;

        [Display(Name = "GST Rate (%)")]
        [Range(0, 50, ErrorMessage = "GST rate must be between 0 and 50")]
        public decimal GSTRate { get; set; } = 18.0m;
    }

    public class BoxCalculatorResult
    {
        // ✅ REMOVED - Box specifications (Length, Width, Height)
        // ✅ KEPT - Essential identifiers
        public string BoardType { get; set; } = "";
        public int Quantity { get; set; }
        public int AppsPerSheet { get; set; }
        
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
        
        // ✅ SIMPLIFIED - Apps is now manual input, not calculated
        public int TotalApps { get; set; }
        
        // ✅ REMOVED - Box layout dimensions (no longer needed without auto-calculation)
        
        // Utilization - ✅ REMOVED (no longer applicable without box dimensions)
        
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
        
        // Processing costs
        public decimal PrintingCostPerBox { get; set; }
        public decimal DieCuttingCostPerBox { get; set; }
        public decimal LaborCostPerBox { get; set; }
        public decimal PinCostPerBox { get; set; }
        public decimal LaminationCostPerBox { get; set; }
        public decimal TransportCostPerBox { get; set; }
        
        // Business costs
        public decimal SubtotalPerBox { get; set; }
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

    // ✅ ADDED - Calculation Constants for Industry Standards
    public static class CalculationConstants
    {
        // Sheet area conversion
        public const decimal SquareInchesPerSquareMeter = 1550m; // 1 m² = 1550.0031 sq inches (industry standard)
        
        // Material layer multipliers for multi-ply boards
        public const decimal FivePlyInnerLinerRatio = 0.85m; // Inner liner as % of top liner
        public const decimal FivePlySecondMediumRatio = 0.9m; // Second medium layer as % of first
        
        public const decimal SevenPlyInnerLiner1Ratio = 0.9m; // First inner liner as % of top
        public const decimal SevenPlyInnerLiner2Ratio = 0.85m; // Second inner liner as % of bottom
        public const decimal SevenPlySecondMediumRatio = 0.95m; // Second medium layer ratio
        public const decimal SevenPlyThirdMediumRatio = 0.9m; // Third medium layer ratio
        
        // Precision settings
        public const int PricePrecisionDigits = 4; // Decimal places for price calculations
        public const int WeightPrecisionDigits = 6; // Decimal places for weight calculations
    }

    // ✅ ENHANCED Industry-Standard Board Configurations WITHOUT Duplex
    public static class BoardTypeConstants
    {
        public static readonly Dictionary<string, BoardConfiguration> BoardConfigurations = new()
        {
            { 
                "3 Ply", 
                new BoardConfiguration 
                { 
                    Name = "3 Ply (Single Wall)", 
                    RequiredPapers = 3, // Top + Medium + Bottom
                    Thickness = 4.0m,
                    Description = "Single wall: Top Liner + Single Corrugated Medium + Bottom Liner",
                    MediumLayers = 1,
                    GSMMultiplier = 1.0m, // Standard single wall
                    WasteFactor = 8.0m,
                    EdgeCrushStrength = 5.5m,
                    IsDuplex = false
                } 
            },
            { 
                "5 Ply", 
                new BoardConfiguration 
                { 
                    Name = "5 Ply (Double Wall)", 
                    RequiredPapers = 4, // Top + Inner + Medium + Bottom
                    Thickness = 6.5m,
                    Description = "Double wall: Top + Inner Liner + Double Medium + Bottom Liner",
                    MediumLayers = 2, // Two corrugated medium layers
                    GSMMultiplier = 1.7m, // Increased material for double wall
                    WasteFactor = 10.0m,
                    EdgeCrushStrength = 8.5m,
                    IsDuplex = false
                } 
            },
            { 
                "7 Ply", 
                new BoardConfiguration 
                { 
                    Name = "7 Ply (Triple Wall)", 
                    RequiredPapers = 7, // Top + 2×Inner + 3×Medium + Bottom
                    Thickness = 15.0m,
                    Description = "Triple wall: Multiple Liners + Triple Corrugated Medium (Heavy Duty)",
                    MediumLayers = 3, // Three corrugated medium layers
                    GSMMultiplier = 2.3m, // Significant increase for triple wall
                    WasteFactor = 12.0m,
                    EdgeCrushStrength = 12.0m,
                    IsDuplex = false // ✅ CHANGED - 7-ply is now regular triple wall
                } 
            }
            // ✅ REMOVED - Duplex Box as requested
        };

        public static readonly List<string> AvailableBoardTypes = new()
        {
            "3 Ply",
            "5 Ply", 
            "7 Ply"
            // ✅ REMOVED - Duplex Box
        };

        public static readonly List<int> StandardGSMValues = new()
        {
            80, 90, 100, 110, 120, 125, 130, 135, 140, 145, 150, 155, 160, 165, 170, 175, 180, 185, 190, 195, 200, 220, 250, 300, 350, 400
        };

        // ✅ ENHANCED Industry-Standard Flute Factors
        public static readonly Dictionary<string, decimal> FluteFactors = new()
        {
            { "B", 1.4m },   // B-Flute (3mm) - Most common for 3-ply
            { "C", 1.45m },  // C-Flute (4mm) - Standard for 5-ply
            { "E", 1.3m },   // E-Flute (1.5mm) - Fine printing applications
            { "BC", 1.8m },  // BC-Flute (6.5mm) - Double wall, ideal for 5-ply
            { "EB", 1.6m }   // EB-Flute combination - Heavy duty 7-ply
        };

        // ENHANCED: Board Type Recommendations
        public static readonly Dictionary<string, string[]> RecommendedFlutes = new()
        {
            { "3 Ply", new[] { "B", "C", "E" } },
            { "5 Ply", new[] { "C", "BC" } },
            { "7 Ply", new[] { "BC", "EB" } }
        };

        // ENHANCED: Structure Definitions
        public static readonly Dictionary<string, DuplexConfiguration> DuplexConfigurations = new()
        {
            { 
                "3 Ply", 
                new DuplexConfiguration 
                { 
                    IsDuplex = false,
                    Structure = "Single Face",
                    Description = "Single corrugated medium between two liners"
                }
            },
            { 
                "5 Ply", 
                new DuplexConfiguration 
                { 
                    IsDuplex = false,
                    Structure = "Double Wall",
                    Description = "Two corrugated mediums with inner liner"
                }
            },
            { 
                "7 Ply", 
                new DuplexConfiguration 
                { 
                    IsDuplex = false,
                    Structure = "Triple Wall",
                    Description = "Three corrugated mediums with multiple liners"
                }
            }
        };
    }

    public class BoardConfiguration
    {
        public string Name { get; set; } = "";
        public int RequiredPapers { get; set; }
        public decimal Thickness { get; set; }
        public string Description { get; set; } = "";
        public int MediumLayers { get; set; } = 1;
        public decimal GSMMultiplier { get; set; } = 1.0m;
        public decimal WasteFactor { get; set; } = 8.0m;
        public decimal EdgeCrushStrength { get; set; } = 5.0m;
        public bool IsDuplex { get; set; } = false; // ✅ ADDED - Duplex flag
    }

    public class DuplexConfiguration
    {
        public bool IsDuplex { get; set; }
        public string Structure { get; set; } = "";
        public string Description { get; set; } = "";
    }
}