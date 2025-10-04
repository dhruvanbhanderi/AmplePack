namespace AmplePack.Models
{
    /// <summary>
    /// Specifications for different board types and materials
    /// </summary>
    public class BoardSpecs
    {
        public string BoardType { get; set; } = string.Empty;
        public decimal DefaultCompressionRatio { get; set; }
        public decimal DefaultGSM { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public static List<BoardSpecs> GetStandardBoardTypes()
        {
            return new List<BoardSpecs>
            {
                new BoardSpecs
                {
                    BoardType = "3 Ply",
                    DefaultCompressionRatio = 1.32m,
                    DefaultGSM = 200,
                    Description = "Standard 3-ply corrugated board"
                },
                new BoardSpecs
                {
                    BoardType = "5 Ply",
                    DefaultCompressionRatio = 1.45m,
                    DefaultGSM = 300,
                    Description = "Heavy duty 5-ply corrugated board"
                },
                new BoardSpecs
                {
                    BoardType = "7 Ply",
                    DefaultCompressionRatio = 1.55m,
                    DefaultGSM = 450,
                    Description = "Extra heavy duty 7-ply corrugated board"
                },
                new BoardSpecs
                {
                    BoardType = "Single Wall",
                    DefaultCompressionRatio = 1.30m,
                    DefaultGSM = 150,
                    Description = "Single wall corrugated board"
                },
                new BoardSpecs
                {
                    BoardType = "Double Wall",
                    DefaultCompressionRatio = 1.40m,
                    DefaultGSM = 350,
                    Description = "Double wall corrugated board"
                }
            };
        }
    }

    /// <summary>
    /// Standard material rates for different regions/suppliers
    /// </summary>
    public class MaterialRates
    {
        public string RateCategory { get; set; } = string.Empty;
        public decimal BoardRatePerSqM { get; set; }
        public decimal PrintingRatePerSqM { get; set; }
        public decimal DieCuttingRatePerSqM { get; set; }
        public decimal LaborRatePerBox { get; set; }
        public string Region { get; set; } = string.Empty;
        public DateTime EffectiveDate { get; set; }

        public static List<MaterialRates> GetStandardRates()
        {
            return new List<MaterialRates>
            {
                new MaterialRates
                {
                    RateCategory = "Standard Rates",
                    BoardRatePerSqM = 65.0m,
                    PrintingRatePerSqM = 12.0m,
                    DieCuttingRatePerSqM = 8.0m,
                    LaborRatePerBox = 2.0m,
                    Region = "Mumbai",
                    EffectiveDate = DateTime.Now
                },
                new MaterialRates
                {
                    RateCategory = "Premium Rates",
                    BoardRatePerSqM = 75.0m,
                    PrintingRatePerSqM = 15.0m,
                    DieCuttingRatePerSqM = 10.0m,
                    LaborRatePerBox = 2.5m,
                    Region = "Delhi",
                    EffectiveDate = DateTime.Now
                },
                new MaterialRates
                {
                    RateCategory = "Economy Rates",
                    BoardRatePerSqM = 55.0m,
                    PrintingRatePerSqM = 10.0m,
                    DieCuttingRatePerSqM = 6.0m,
                    LaborRatePerBox = 1.5m,
                    Region = "Ahmedabad",
                    EffectiveDate = DateTime.Now
                }
            };
        }
    }

    /// <summary>
    /// Standard sheet sizes available (dimensions in inches)
    /// </summary>
    public class SheetSpecs
    {
        public string SheetSize { get; set; } = string.Empty;
        public decimal LengthInches { get; set; }
        public decimal WidthInches { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsStandard { get; set; } = true;

        // Legacy properties for backwards compatibility - will convert from inches
        public decimal LengthMm => LengthInches * 25.4m;
        public decimal WidthMm => WidthInches * 25.4m;

        public static List<SheetSpecs> GetStandardSheetSizes()
        {
            return new List<SheetSpecs>
            {
                new SheetSpecs
                {
                    SheetSize = "Small (47×39 inches)",
                    LengthInches = 47.2m,
                    WidthInches = 39.4m,
                    Description = "Standard small sheet (~1200×1000mm)",
                    IsStandard = true
                },
                new SheetSpecs
                {
                    SheetSize = "Medium (59×47 inches)",
                    LengthInches = 59.1m,
                    WidthInches = 47.2m,
                    Description = "Standard medium sheet (~1500×1200mm)",
                    IsStandard = true
                },
                new SheetSpecs
                {
                    SheetSize = "Large (71×55 inches)",
                    LengthInches = 70.9m,
                    WidthInches = 55.1m,
                    Description = "Standard large sheet (~1800×1400mm)",
                    IsStandard = true
                },
                new SheetSpecs
                {
                    SheetSize = "Extra Large (83×59 inches)",
                    LengthInches = 82.7m,
                    WidthInches = 59.1m,
                    Description = "Extra large sheet (~2100×1500mm)",
                    IsStandard = true
                },
                new SheetSpecs
                {
                    SheetSize = "Custom",
                    LengthInches = 47.2m,
                    WidthInches = 39.4m,
                    Description = "Custom sheet size",
                    IsStandard = false
                }
            };
        }
    }
}