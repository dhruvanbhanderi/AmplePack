namespace AmplePack.Models
{
    /// <summary>
    /// Model for box price calculation results
    /// </summary>
    public class BoxCalculatorResult
    {
        // Input Parameters (for reference)
        public BoxCalculatorInput Input { get; set; } = new();

        // Basic Calculations
        public decimal BlankLengthMm { get; set; }
        public decimal BlankWidthMm { get; set; }
        public decimal BlankAreaSqM { get; set; }
        public decimal BoxWeightGrams { get; set; }

        // Sheet Layout Optimization
        public int BlanksPerSheet { get; set; }
        public int SheetsRequired { get; set; }
        public decimal WastePercentage { get; set; }
        public decimal EfficiencyPercentage { get; set; }

        // Cost Breakdown (per box)
        public decimal MaterialCostPerBox { get; set; }
        public decimal PrintingCostPerBox { get; set; }
        public decimal DieCuttingCostPerBox { get; set; }
        public decimal LaborCostPerBox { get; set; }
        public decimal BaseCostPerBox { get; set; }
        
        // Business Costs (per box)
        public decimal OverheadCostPerBox { get; set; }
        public decimal ProfitPerBox { get; set; }
        public decimal CostBeforeDiscountPerBox { get; set; }
        public decimal DiscountPerBox { get; set; }
        public decimal CostAfterDiscountPerBox { get; set; }

        // Final Pricing (per box)
        public decimal FinalPricePerBoxExGST { get; set; }
        public decimal GSTPerBox { get; set; }
        public decimal FinalPricePerBoxIncGST { get; set; }

        // Total Order Costs
        public decimal TotalMaterialCost { get; set; }
        public decimal TotalProductionCost { get; set; }
        public decimal TotalOrderCostExGST { get; set; }
        public decimal TotalGST { get; set; }
        public decimal TotalOrderCostIncGST { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal GrandTotal { get; set; }

        // Warnings and Recommendations
        public List<string> Warnings { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();

        // Detailed Breakdown
        public Dictionary<string, decimal> DetailedBreakdown { get; set; } = new();

        // Sheet Layout Information
        public SheetLayoutInfo LayoutInfo { get; set; } = new();
    }

    /// <summary>
    /// Information about how boxes fit on sheets
    /// </summary>
    public class SheetLayoutInfo
    {
        public int BlanksPerRow { get; set; }
        public int BlanksPerColumn { get; set; }
        public decimal UnusedWidthMm { get; set; }
        public decimal UnusedLengthMm { get; set; }
        public decimal UsedAreaSqM { get; set; }
        public decimal WastedAreaSqM { get; set; }
        public string LayoutDescription { get; set; } = string.Empty;
    }
}