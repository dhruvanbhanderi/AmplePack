using System.ComponentModel.DataAnnotations;

namespace AmplePack.Models
{
    /// <summary>
    /// Model representing the complete result of box price calculation
    /// Contains detailed cost breakdown, efficiency metrics, and layout information
    /// </summary>
    public class BoxCalculatorResult
    {
        // Input reference
        public BoxCalculatorInput Input { get; set; } = new();

        // Box specifications
        [Display(Name = "Box Area (sq m)")]
        public decimal BlankAreaSqM { get; set; }

        [Display(Name = "Box Length (mm)")]
        public decimal BlankLengthMm { get; set; }

        [Display(Name = "Box Width (mm)")]
        public decimal BlankWidthMm { get; set; }

        [Display(Name = "Box Weight (grams)")]
        public decimal BoxWeightGrams { get; set; }

        // Layout and efficiency
        [Display(Name = "Boxes Per Sheet")]
        public int BlanksPerSheet { get; set; }

        [Display(Name = "Sheets Required")]
        public int SheetsRequired { get; set; }

        [Display(Name = "Efficiency (%)")]
        public decimal EfficiencyPercentage { get; set; }

        [Display(Name = "Waste (%)")]
        public decimal WastePercentage { get; set; }

        // Layout information
        public LayoutInfo LayoutInfo { get; set; } = new();

        // Cost breakdown
        [Display(Name = "Material Cost per Box")]
        public decimal MaterialCostPerBox { get; set; }

        [Display(Name = "Printing Cost per Box")]
        public decimal PrintingCostPerBox { get; set; }

        [Display(Name = "Die Cutting Cost per Box")]
        public decimal DieCuttingCostPerBox { get; set; }

        [Display(Name = "Labor Cost per Box")]
        public decimal LaborCostPerBox { get; set; }

        [Display(Name = "Base Cost per Box")]
        public decimal BaseCostPerBox { get; set; }

        [Display(Name = "Overhead Cost per Box")]
        public decimal OverheadCostPerBox { get; set; }

        [Display(Name = "Profit per Box")]
        public decimal ProfitPerBox { get; set; }

        [Display(Name = "Cost Before Discount per Box")]
        public decimal CostBeforeDiscountPerBox { get; set; }

        [Display(Name = "Discount per Box")]
        public decimal DiscountPerBox { get; set; }

        [Display(Name = "Cost After Discount per Box")]
        public decimal CostAfterDiscountPerBox { get; set; }

        [Display(Name = "Final Price per Box (Ex-GST)")]
        public decimal FinalPricePerBoxExGST { get; set; }

        [Display(Name = "GST per Box")]
        public decimal GSTPerBox { get; set; }

        [Display(Name = "Final Price per Box (Inc-GST)")]
        public decimal FinalPricePerBoxIncGST { get; set; }

        // Order totals
        [Display(Name = "Total Material Cost")]
        public decimal TotalMaterialCost { get; set; }

        [Display(Name = "Total Production Cost")]
        public decimal TotalProductionCost { get; set; }

        [Display(Name = "Total Order Cost (Ex-GST)")]
        public decimal TotalOrderCostExGST { get; set; }

        [Display(Name = "Total GST")]
        public decimal TotalGST { get; set; }

        [Display(Name = "Total Order Cost (Inc-GST)")]
        public decimal TotalOrderCostIncGST { get; set; }

        [Display(Name = "Shipping Cost")]
        public decimal ShippingCost { get; set; }

        [Display(Name = "Grand Total")]
        public decimal GrandTotal { get; set; }

        // Additional information
        public List<string> Warnings { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public Dictionary<string, decimal> DetailedBreakdown { get; set; } = new();
    }

    /// <summary>
    /// Layout information for sheet optimization
    /// </summary>
    public class LayoutInfo
    {
        [Display(Name = "Boxes Per Row")]
        public int BlanksPerRow { get; set; }

        [Display(Name = "Boxes Per Column")]
        public int BlanksPerColumn { get; set; }

        [Display(Name = "Used Area (sq m)")]
        public decimal UsedAreaSqM { get; set; }

        [Display(Name = "Wasted Area (sq m)")]
        public decimal WastedAreaSqM { get; set; }

        [Display(Name = "Unused Length (mm)")]
        public decimal UnusedLengthMm { get; set; }

        [Display(Name = "Unused Width (mm)")]
        public decimal UnusedWidthMm { get; set; }

        [Display(Name = "Layout Description")]
        public string LayoutDescription { get; set; } = string.Empty;
    }
}