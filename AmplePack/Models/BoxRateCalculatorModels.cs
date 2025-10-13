using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AmplePack.Models
{
    /// <summary>
    /// Input model for the Box Rate Calculator based on Excel formulas
    /// Supports both single-liner and dual-liner configurations
    /// </summary>
    public class BoxRateCalculatorInput
    {
        [Required]
        [JsonPropertyName("mode")]
        public string Mode { get; set; } = "single-liner"; // "single-liner" or "dual-liner"

        // Common Inputs (both sheets)
        [Required]
        [Range(1, 1000, ErrorMessage = "Size-1 must be between 1 and 1000")]
        [JsonPropertyName("size1")]
        public decimal Size1 { get; set; } = 20;

        [Required]
        [Range(1, 1000, ErrorMessage = "Size-2 must be between 1 and 1000")]
        [JsonPropertyName("size2")]
        public decimal Size2 { get; set; } = 21;

        [Required]
        [Range(50, 1000, ErrorMessage = "Duplex GSM must be between 50 and 1000")]
        [JsonPropertyName("duplex_gsm")]
        public decimal DuplexGsm { get; set; } = 230;

        [Required]
        [Range(1, 200, ErrorMessage = "Duplex rate must be between 1 and 200")]
        [JsonPropertyName("duplex_rate")]
        public decimal DuplexRate { get; set; } = 58;

        [Required]
        [Range(50, 1000, ErrorMessage = "Liner GSM must be between 50 and 1000")]
        [JsonPropertyName("liner_gsm")]
        public decimal LinerGsm { get; set; } = 360;

        [Required]
        [Range(1, 200, ErrorMessage = "Liner rate must be between 1 and 200")]
        [JsonPropertyName("liner_rate")]
        public decimal LinerRate { get; set; } = 43;

        [Required]
        [Range(1, 10000, ErrorMessage = "Divide-1 must be between 1 and 10000")]
        [JsonPropertyName("divide1")]
        public decimal Divide1 { get; set; } = 1550;

        [Required]
        [Range(1, 10000, ErrorMessage = "Divide-2 must be between 1 and 10000")]
        [JsonPropertyName("divide2")]
        public decimal Divide2 { get; set; } = 1000;

        [Required]
        [Range(0, 10000, ErrorMessage = "Printing rate 1 must be between 0 and 10000")]
        [JsonPropertyName("printing_rate1")]
        public decimal PrintingRate1 { get; set; } = 3000;

        [Required]
        [Range(0, 10000, ErrorMessage = "Printing rate 2 must be between 0 and 10000")]
        [JsonPropertyName("printing_rate2")]
        public decimal PrintingRate2 { get; set; } = 2000;

        [Required]
        [Range(0, 10000, ErrorMessage = "Printing rate 3 must be between 0 and 10000")]
        [JsonPropertyName("printing_rate3")]
        public decimal PrintingRate3 { get; set; } = 2100;

        [Required]
        [Range(1, 100000, ErrorMessage = "Quantity 1 must be between 1 and 100000")]
        [JsonPropertyName("qty1")]
        public decimal Qty1 { get; set; } = 1000;

        [Required]
        [Range(1, 100000, ErrorMessage = "Quantity 2 must be between 1 and 100000")]
        [JsonPropertyName("qty2")]
        public decimal Qty2 { get; set; } = 2000;

        [Required]
        [Range(1, 100000, ErrorMessage = "Quantity 3 must be between 1 and 100000")]
        [JsonPropertyName("qty3")]
        public decimal Qty3 { get; set; } = 3000;

        [Range(0, 10, ErrorMessage = "Lamination rate must be between 0 and 10")]
        [JsonPropertyName("lamination_rate")]
        public decimal LaminationRate { get; set; } = 0.0039m;

        [Range(0, 10, ErrorMessage = "Pasting rate must be between 0 and 10")]
        [JsonPropertyName("pasting_rate")]
        public decimal PastingRate { get; set; } = 0.0013m;

        [Range(0, 10, ErrorMessage = "Punching rate must be between 0 and 10")]
        [JsonPropertyName("punching_rate")]
        public decimal PunchingRate { get; set; } = 0.35m;

        [Range(0, 10, ErrorMessage = "Punching quantity must be between 0 and 10")]
        [JsonPropertyName("punching_qty")]
        public decimal PunchingQty { get; set; } = 1;

        [Range(0, 10000, ErrorMessage = "Freight rate must be between 0 and 10000")]
        [JsonPropertyName("freight_rate")]
        public decimal FreightRate { get; set; } = 300;

        [Range(0, 10, ErrorMessage = "Freight quantity must be between 0 and 10")]
        [JsonPropertyName("freight_qty")]
        public decimal FreightQty { get; set; } = 1;

        [Range(0, 100, ErrorMessage = "Profit percent must be between 0 and 100")]
        [JsonPropertyName("profit_percent")]
        public decimal ProfitPercent { get; set; } = 10;

        // Single-Liner Mode Specific
        [Range(0, 10000, ErrorMessage = "Plate charge must be between 0 and 10000")]
        [JsonPropertyName("plate_charge")]
        public decimal PlateCharge { get; set; } = 1000;

        [Range(0, 10, ErrorMessage = "Plate quantity must be between 0 and 10")]
        [JsonPropertyName("plate_qty")]
        public decimal PlateQty { get; set; } = 1;

        [Range(0, 10000, ErrorMessage = "Die charge must be between 0 and 10000")]
        [JsonPropertyName("die_charge")]
        public decimal DieCharge { get; set; } = 0;

        [Range(0, 10, ErrorMessage = "Die quantity must be between 0 and 10")]
        [JsonPropertyName("die_qty")]
        public decimal DieQty { get; set; } = 1;

        [Range(0, 10, ErrorMessage = "PIN rate (no extras) must be between 0 and 10")]
        [JsonPropertyName("pin_rate_no_extras")]
        public decimal PinRateNoExtras { get; set; } = 0.4m;

        [Range(0, 10, ErrorMessage = "PIN quantity (no extras) must be between 0 and 10")]
        [JsonPropertyName("pin_qty_no_extras")]
        public decimal PinQtyNoExtras { get; set; } = 1;

        [Range(0, 10, ErrorMessage = "Box pasting rate (no extras) must be between 0 and 10")]
        [JsonPropertyName("box_pes_rate_no_extras")]
        public decimal BoxPesRateNoExtras { get; set; } = 0;

        [Range(0, 10, ErrorMessage = "Box pasting quantity (no extras) must be between 0 and 10")]
        [JsonPropertyName("box_pes_qty_no_extras")]
        public decimal BoxPesQtyNoExtras { get; set; } = 0;

        [Range(0, 10, ErrorMessage = "PIN rate (with extras) must be between 0 and 10")]
        [JsonPropertyName("pin_rate_with_extras")]
        public decimal PinRateWithExtras { get; set; } = 0.08m;

        [Range(0, 10, ErrorMessage = "PIN quantity (with extras) must be between 0 and 10")]
        [JsonPropertyName("pin_qty_with_extras")]
        public decimal PinQtyWithExtras { get; set; } = 3;

        [Range(0, 10, ErrorMessage = "Box pasting rate (with extras) must be between 0 and 10")]
        [JsonPropertyName("box_pes_rate_with_extras")]
        public decimal BoxPesRateWithExtras { get; set; } = 0.07m;

        [Range(0, 10, ErrorMessage = "Box pasting quantity (with extras) must be between 0 and 10")]
        [JsonPropertyName("box_pes_qty_with_extras")]
        public decimal BoxPesQtyWithExtras { get; set; } = 1;

        [Range(0.1, 10, ErrorMessage = "APPS divisor must be between 0.1 and 10")]
        [JsonPropertyName("apps_divisor")]
        public decimal AppsDivisor { get; set; } = 2;

        // Dual-Liner Mode Specific
        [Range(50, 1000, ErrorMessage = "Liner 2 GSM must be between 50 and 1000")]
        [JsonPropertyName("liner2_gsm")]
        public decimal? Liner2Gsm { get; set; }

        [Range(1, 200, ErrorMessage = "Liner 2 rate must be between 1 and 200")]
        [JsonPropertyName("liner2_rate")]
        public decimal? Liner2Rate { get; set; }

        /// <summary>
        /// Apply default values based on mode
        /// </summary>
        public void ApplyModeDefaults()
        {
            if (Mode.Equals("single-liner", StringComparison.OrdinalIgnoreCase))
            {
                // Single-Liner defaults from first Excel sheet
                Size1 = 20;
                Size2 = 21;
                DuplexGsm = 230;
                DuplexRate = 58;
                LinerGsm = 360;
                LinerRate = 43;
                Divide1 = 1550;
                Divide2 = 1000;
                PrintingRate1 = 3000;
                PrintingRate2 = 2000;
                PrintingRate3 = 2100;
                Qty1 = 1000;
                Qty2 = 2000;
                Qty3 = 3000;
                LaminationRate = 0.0039m;
                PastingRate = 0.0013m;
                PunchingRate = 0.35m;
                PunchingQty = 1;
                FreightRate = 300;
                FreightQty = 1;
                PlateCharge = 1000;
                PlateQty = 1;
                DieCharge = 0;
                DieQty = 1;
                PinRateNoExtras = 0.4m;
                PinQtyNoExtras = 1;
                BoxPesRateNoExtras = 0;
                BoxPesQtyNoExtras = 0;
                PinRateWithExtras = 0.08m;
                PinQtyWithExtras = 3;
                BoxPesRateWithExtras = 0.07m;
                BoxPesQtyWithExtras = 1;
                AppsDivisor = 2;
                ProfitPercent = 10;
            }
            else if (Mode.Equals("dual-liner", StringComparison.OrdinalIgnoreCase))
            {
                // Dual-Liner defaults from second Excel sheet
                Size1 = 19;
                Size2 = 20;
                DuplexGsm = 230;
                DuplexRate = 65;
                LinerGsm = 290;
                LinerRate = 43;
                Liner2Gsm = 290;
                Liner2Rate = 43;
                Divide1 = 1550;
                Divide2 = 1000;
                PrintingRate1 = 2400;
                PrintingRate2 = 3400;
                PrintingRate3 = 3400;
                Qty1 = 1000;
                Qty2 = 2000;
                Qty3 = 3000;
                LaminationRate = 0.55m;
                PastingRate = 0.28m;
                PunchingRate = 0.4m;
                PunchingQty = 1;
                FreightRate = 0;
                FreightQty = 1;
                PlateCharge = 1000;
                PlateQty = 1;
                DieCharge = 1800;
                DieQty = 1;
                PinRateNoExtras = 0.1m;
                PinQtyNoExtras = 3;
                BoxPesRateNoExtras = 0.07m;
                BoxPesQtyNoExtras = 1;
                PinRateWithExtras = 0.08m;
                PinQtyWithExtras = 3;
                BoxPesRateWithExtras = 0.07m;
                BoxPesQtyWithExtras = 1;
                AppsDivisor = 1;
                ProfitPercent = 10;
            }
        }
    }

    /// <summary>
    /// Cost component structure for calculations
    /// </summary>
    public class CostComponent
    {
        [JsonPropertyName("cost")]
        public decimal Cost { get; set; }

        [JsonPropertyName("with_profit")]
        public decimal WithProfit { get; set; }

        [JsonPropertyName("profit_inr")]
        public decimal ProfitInr { get; set; }
    }

    /// <summary>
    /// Quantity-specific calculation results
    /// </summary>
    public class QuantityResult
    {
        [JsonPropertyName("duplex")]
        public decimal Duplex { get; set; }

        [JsonPropertyName("liner")]
        public object Liner { get; set; } = new(); // Can be decimal or LinerBreakdown

        [JsonPropertyName("printing")]
        public decimal Printing { get; set; }

        [JsonPropertyName("lamination")]
        public decimal Lamination { get; set; }

        [JsonPropertyName("pasting")]
        public decimal Pasting { get; set; }

        [JsonPropertyName("punching")]
        public decimal Punching { get; set; }

        [JsonPropertyName("freight")]
        public decimal Freight { get; set; }

        [JsonPropertyName("base_total")]
        public decimal BaseTotal { get; set; }

        [JsonPropertyName("apps")]
        public CostComponent Apps { get; set; } = new();

        [JsonPropertyName("pin_no_extras")]
        public CostComponent PinNoExtras { get; set; } = new();

        [JsonPropertyName("box_pes_no_extras")]
        public CostComponent BoxPesNoExtras { get; set; } = new();

        [JsonPropertyName("plate")]
        public decimal Plate { get; set; }

        [JsonPropertyName("die")]
        public decimal Die { get; set; }

        [JsonPropertyName("updated_base_total")]
        public decimal UpdatedBaseTotal { get; set; }

        [JsonPropertyName("total_with_extras")]
        public CostComponent TotalWithExtras { get; set; } = new();

        [JsonPropertyName("pin_with_extras")]
        public CostComponent PinWithExtras { get; set; } = new();

        [JsonPropertyName("box_pes_with_extras")]
        public CostComponent BoxPesWithExtras { get; set; } = new();
    }

    /// <summary>
    /// Liner breakdown for dual-liner mode
    /// </summary>
    public class LinerBreakdown
    {
        [JsonPropertyName("3_ply")]
        public decimal ThreePly { get; set; }

        [JsonPropertyName("5_ply")]
        public decimal FivePly { get; set; }
    }

    /// <summary>
    /// Complete calculation result
    /// </summary>
    public class BoxRateCalculatorResult
    {
        [JsonPropertyName("mode")]
        public string Mode { get; set; } = string.Empty;

        [JsonPropertyName("quantities")]
        public Dictionary<string, QuantityResult> Quantities { get; set; } = new();

        [JsonPropertyName("errors")]
        public List<string> Errors { get; set; } = new();

        [JsonPropertyName("calculation_details")]
        public Dictionary<string, object> CalculationDetails { get; set; } = new();
    }

    public class BoxCalculatorViewModel
    {
        public BoxRateCalculatorInput Input { get; set; } = new();
        public BoxRateCalculatorResult? Result { get; set; }
        public List<string> SupportedModes { get; set; } = new();
        public bool ShowResults { get; set; }

        public decimal GetTotalLinerCost(object quantityResult)
        {
            // Helper method to calculate total liner cost
            // This uses reflection to get the liner properties
            var type = quantityResult.GetType();
            var liner = type.GetProperty("Liner")?.GetValue(quantityResult);
            
            decimal total = 0;
            
            if (liner != null)
            {
                // Check if it's a simple decimal (single-liner mode)
                if (liner is decimal singleLiner)
                {
                    total += singleLiner;
                }
                // Check if it's a LinerBreakdown (dual-liner mode)
                else if (liner is LinerBreakdown dualLiner)
                {
                    total += dualLiner.ThreePly + dualLiner.FivePly;
                }
            }
            
            return total;
        }
    }
}