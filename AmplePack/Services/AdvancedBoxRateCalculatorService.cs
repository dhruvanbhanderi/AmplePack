using AmplePack.Models;
using System.Globalization;

namespace AmplePack.Services
{
    /// <summary>
    /// Advanced Box Rate Calculator Service implementing exact Excel formulas
    /// Supports both single-liner and dual-liner configurations with precision calculations
    /// </summary>
    public class AdvancedBoxRateCalculatorService
    {
        private const int DECIMAL_PRECISION = 12; // High precision for calculations

        /// <summary>
        /// Calculate box rates using exact Excel formulas
        /// </summary>
        /// <param name="input">Input parameters for calculation</param>
        /// <returns>Complete calculation results with all intermediate and final costs</returns>
        public BoxRateCalculatorResult CalculateBoxRates(BoxRateCalculatorInput input)
        {
            var result = new BoxRateCalculatorResult
            {
                Mode = input.Mode
            };

            try
            {
                // Validate inputs first
                var validationErrors = ValidateInputs(input);
                if (validationErrors.Any())
                {
                    result.Errors = validationErrors;
                    return result;
                }

                // Apply default values based on mode
                input.ApplyModeDefaults();

                // Calculate for each quantity
                result.Quantities["1000"] = CalculateForQuantity(input, input.Qty1, input.PrintingRate1);
                result.Quantities["2000"] = CalculateForQuantity(input, input.Qty2, input.PrintingRate2);
                result.Quantities["3000"] = CalculateForQuantity(input, input.Qty3, input.PrintingRate3);

                // Add calculation details for debugging/transparency
                AddCalculationDetails(input, result);
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Calculation error: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Validate input parameters
        /// </summary>
        private List<string> ValidateInputs(BoxRateCalculatorInput input)
        {
            var errors = new List<string>();

            // Check mode
            if (!input.Mode.Equals("single-liner", StringComparison.OrdinalIgnoreCase) &&
                !input.Mode.Equals("dual-liner", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("mode must be 'single-liner' or 'dual-liner'");
            }

            // Check for zero divisors
            if (input.Divide1 <= 0) errors.Add("divide1 cannot be zero");
            if (input.Divide2 <= 0) errors.Add("divide2 cannot be zero");
            if (input.Qty1 <= 0) errors.Add("qty1 cannot be zero");
            if (input.Qty2 <= 0) errors.Add("qty2 cannot be zero");
            if (input.Qty3 <= 0) errors.Add("qty3 cannot be zero");
            if (input.AppsDivisor <= 0) errors.Add("apps_divisor cannot be zero");

            // Check negative values
            if (input.Size1 < 0) errors.Add("size1 must be non-negative");
            if (input.Size2 < 0) errors.Add("size2 must be non-negative");
            if (input.DuplexGsm < 0) errors.Add("duplex_gsm must be non-negative");
            if (input.DuplexRate < 0) errors.Add("duplex_rate must be non-negative");
            if (input.LinerGsm < 0) errors.Add("liner_gsm must be non-negative");
            if (input.LinerRate < 0) errors.Add("liner_rate must be non-negative");

            // Dual-liner specific validation
            if (input.Mode.Equals("dual-liner", StringComparison.OrdinalIgnoreCase))
            {
                if (!input.Liner2Gsm.HasValue || !input.Liner2Rate.HasValue)
                {
                    errors.Add("liner2_gsm and liner2_rate required for dual-liner mode");
                }
                else
                {
                    if (input.Liner2Gsm < 0) errors.Add("liner2_gsm must be non-negative");
                    if (input.Liner2Rate < 0) errors.Add("liner2_rate must be non-negative");
                }
            }

            return errors;
        }

        /// <summary>
        /// Calculate costs for a specific quantity
        /// </summary>
        private QuantityResult CalculateForQuantity(BoxRateCalculatorInput input, decimal quantity, decimal printingRate)
        {
            var result = new QuantityResult();

            try
            {
                // Step 1: Calculate Duplex (Row 8 in Excel)
                result.Duplex = CalculateWithPrecision(
                    (input.Size1 * input.Size2 * input.DuplexGsm * input.DuplexRate) / input.Divide1 / input.Divide2
                );

                // Step 2: Calculate Liner(s)
                if (input.Mode.Equals("single-liner", StringComparison.OrdinalIgnoreCase))
                {
                    // Single liner calculation
                    result.Liner = CalculateWithPrecision(
                        (input.Size1 * input.Size2 * input.LinerGsm * input.LinerRate) / input.Divide1 / input.Divide2
                    );
                }
                else
                {
                    // Dual liner calculation
                    var threePly = CalculateWithPrecision(
                        (input.Size1 * input.Size2 * input.LinerGsm * input.LinerRate) / input.Divide1 / input.Divide2
                    );
                    var fivePly = CalculateWithPrecision(
                        (input.Size1 * input.Size2 * input.Liner2Gsm!.Value * input.Liner2Rate!.Value) / input.Divide1 / input.Divide2
                    );
                    
                    result.Liner = new LinerBreakdown 
                    { 
                        ThreePly = threePly, 
                        FivePly = fivePly 
                    };
                }

                // Step 3: Calculate Printing
                result.Printing = CalculateWithPrecision(printingRate / quantity);

                // Step 4: Calculate Lamination (LEMI.)
                if (input.Mode.Equals("single-liner", StringComparison.OrdinalIgnoreCase))
                {
                    result.Lamination = CalculateWithPrecision(input.Size1 * input.Size2 * input.LaminationRate);
                }
                else
                {
                    result.Lamination = CalculateWithPrecision((input.Size1 * input.Size2 * input.LaminationRate) / 100);
                }

                // Step 5: Calculate Pasting (PESTING)
                if (input.Mode.Equals("single-liner", StringComparison.OrdinalIgnoreCase))
                {
                    result.Pasting = CalculateWithPrecision(input.Size1 * input.Size2 * input.PastingRate);
                }
                else
                {
                    result.Pasting = CalculateWithPrecision((input.Size1 * input.Size2 * input.PastingRate) / 100);
                }

                // Step 6: Calculate Punching (PANCH.)
                result.Punching = CalculateWithPrecision(input.PunchingRate * input.PunchingQty);

                // Step 7: Calculate Freight (FRIGHT)
                result.Freight = CalculateWithPrecision((input.FreightRate * input.FreightQty) / quantity);

                // Step 8: Calculate Base Total
                decimal linerTotal = 0;
                if (result.Liner is decimal singleLiner)
                {
                    linerTotal = singleLiner;
                }
                else if (result.Liner is LinerBreakdown dualLiner)
                {
                    linerTotal = dualLiner.ThreePly + dualLiner.FivePly;
                }

                result.BaseTotal = CalculateWithPrecision(
                    result.Duplex + linerTotal + result.Printing + 
                    result.Lamination + result.Pasting + result.Punching + result.Freight
                );

                // Step 9: Calculate APPS (Mobile)
                var appsCost = CalculateWithPrecision(result.BaseTotal / input.AppsDivisor);
                result.Apps = new CostComponent
                {
                    Cost = appsCost,
                    WithProfit = CalculateWithPrecision(appsCost * (1 + input.ProfitPercent / 100)),
                    ProfitInr = CalculateWithPrecision(appsCost * (input.ProfitPercent / 100))
                };

                // Step 10: Calculate PIN (No Extras)
                var pinNoExtrasCost = CalculateWithPrecision(
                    (input.PinRateNoExtras * input.PinQtyNoExtras) + appsCost
                );
                result.PinNoExtras = new CostComponent
                {
                    Cost = pinNoExtrasCost,
                    WithProfit = CalculateWithPrecision(pinNoExtrasCost * (1 + input.ProfitPercent / 100)),
                    ProfitInr = CalculateWithPrecision(pinNoExtrasCost * (input.ProfitPercent / 100))
                };

                // Step 11: Calculate Box Pes. (No Extras)
                var boxPesNoExtrasCost = CalculateWithPrecision(
                    (input.BoxPesRateNoExtras * input.BoxPesQtyNoExtras) + appsCost
                );
                result.BoxPesNoExtras = new CostComponent
                {
                    Cost = boxPesNoExtrasCost,
                    WithProfit = CalculateWithPrecision(boxPesNoExtrasCost * (1 + input.ProfitPercent / 100)),
                    ProfitInr = CalculateWithPrecision(boxPesNoExtrasCost * (input.ProfitPercent / 100))
                };

                // Step 12: Calculate Plate
                if (input.Mode.Equals("single-liner", StringComparison.OrdinalIgnoreCase))
                {
                    if (quantity == input.Qty1)
                    {
                        result.Plate = 0; // As per spreadsheet
                    }
                    else
                    {
                        result.Plate = CalculateWithPrecision((input.PlateCharge * input.PlateQty) / quantity);
                    }
                }
                else
                {
                    result.Plate = CalculateWithPrecision((input.PlateCharge * input.PlateQty) / quantity);
                }

                // Step 13: Calculate Die
                result.Die = CalculateWithPrecision((input.DieCharge * input.DieQty) / quantity);

                // Step 14: Calculate Updated Base Total
                result.UpdatedBaseTotal = CalculateWithPrecision(result.BaseTotal + result.Plate + result.Die);

                // Step 15: Calculate TOTAL (with Extras)
                var totalWithExtrasCost = CalculateWithPrecision(result.UpdatedBaseTotal / input.AppsDivisor);
                result.TotalWithExtras = new CostComponent
                {
                    Cost = totalWithExtrasCost,
                    WithProfit = CalculateWithPrecision(totalWithExtrasCost * (1 + input.ProfitPercent / 100)),
                    ProfitInr = CalculateWithPrecision(totalWithExtrasCost * (input.ProfitPercent / 100))
                };

                // Step 16: Calculate Pin (with Extras)
                var pinWithExtrasCost = CalculateWithPrecision(
                    (input.PinRateWithExtras * input.PinQtyWithExtras) + totalWithExtrasCost
                );
                result.PinWithExtras = new CostComponent
                {
                    Cost = pinWithExtrasCost,
                    WithProfit = CalculateWithPrecision(pinWithExtrasCost * (1 + input.ProfitPercent / 100)),
                    ProfitInr = CalculateWithPrecision(pinWithExtrasCost * (input.ProfitPercent / 100))
                };

                // Step 17: Calculate Box Pes. (with Extras)
                var boxPesWithExtrasCost = CalculateWithPrecision(
                    (input.BoxPesRateWithExtras * input.BoxPesQtyWithExtras) + totalWithExtrasCost
                );
                result.BoxPesWithExtras = new CostComponent
                {
                    Cost = boxPesWithExtrasCost,
                    WithProfit = CalculateWithPrecision(boxPesWithExtrasCost * (1 + input.ProfitPercent / 100)),
                    ProfitInr = CalculateWithPrecision(boxPesWithExtrasCost * (input.ProfitPercent / 100))
                };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error calculating for quantity {quantity}: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Calculate with high precision using decimal arithmetic
        /// </summary>
        private decimal CalculateWithPrecision(decimal value)
        {
            return Math.Round(value, DECIMAL_PRECISION, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Add calculation details for transparency and debugging
        /// </summary>
        private void AddCalculationDetails(BoxRateCalculatorInput input, BoxRateCalculatorResult result)
        {
            result.CalculationDetails = new Dictionary<string, object>
            {
                { "mode", input.Mode },
                { "calculation_timestamp", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC") },
                { "precision_decimal_places", DECIMAL_PRECISION },
                { "input_parameters", new Dictionary<string, object>
                    {
                        { "size1", input.Size1 },
                        { "size2", input.Size2 },
                        { "duplex_gsm", input.DuplexGsm },
                        { "duplex_rate", input.DuplexRate },
                        { "liner_gsm", input.LinerGsm },
                        { "liner_rate", input.LinerRate },
                        { "apps_divisor", input.AppsDivisor },
                        { "profit_percent", input.ProfitPercent }
                    }
                },
                { "formula_references", new Dictionary<string, string>
                    {
                        { "duplex", "(size1 × size2 × duplex_gsm × duplex_rate) ÷ divide1 ÷ divide2" },
                        { "liner", "(size1 × size2 × liner_gsm × liner_rate) ÷ divide1 ÷ divide2" },
                        { "printing", "printing_rate ÷ quantity" },
                        { "apps", "base_total ÷ apps_divisor" },
                        { "profit", "cost × (profit_percent ÷ 100)" }
                    }
                }
            };

            if (input.Mode.Equals("dual-liner", StringComparison.OrdinalIgnoreCase))
            {
                result.CalculationDetails["dual_liner_parameters"] = new Dictionary<string, object>
                {
                    { "liner2_gsm", input.Liner2Gsm },
                    { "liner2_rate", input.Liner2Rate },
                    { "lamination_calculation", "(size1 × size2 × lamination_rate) ÷ 100" },
                    { "pasting_calculation", "(size1 × size2 × pasting_rate) ÷ 100" }
                };
            }
        }

        /// <summary>
        /// Get supported calculation modes
        /// </summary>
        public List<string> GetSupportedModes()
        {
            return new List<string> { "single-liner", "dual-liner" };
        }

        /// <summary>
        /// Get default input values for a specific mode
        /// </summary>
        public BoxRateCalculatorInput GetDefaultInputForMode(string mode)
        {
            var input = new BoxRateCalculatorInput { Mode = mode };
            input.ApplyModeDefaults();
            return input;
        }

        /// <summary>
        /// Calculate quick estimate for given parameters
        /// </summary>
        public decimal CalculateQuickEstimate(string mode, decimal size1, decimal size2, decimal quantity = 1000)
        {
            var input = GetDefaultInputForMode(mode);
            input.Size1 = size1;
            input.Size2 = size2;
            input.Qty1 = quantity;

            var result = CalculateBoxRates(input);
            
            if (result.Errors.Any())
            {
                throw new InvalidOperationException($"Calculation failed: {string.Join(", ", result.Errors)}");
            }

            // Return Box Pes. with Extras and 10% Profit as the standard estimate
            return result.Quantities["1000"].BoxPesWithExtras.WithProfit;
        }

        /// <summary>
        /// Export calculation results to Excel-like format
        /// </summary>
        public string ExportToExcelFormat(BoxRateCalculatorResult result)
        {
            var sb = new System.Text.StringBuilder();
            
            sb.AppendLine($"Box Rate Calculator Results - {result.Mode.ToUpper()} Mode");
            sb.AppendLine(new string('=', 60));
            sb.AppendLine();

            foreach (var qty in result.Quantities)
            {
                sb.AppendLine($"Quantity: {qty.Key}");
                sb.AppendLine(new string('-', 30));
                
                var qtyResult = qty.Value;
                sb.AppendLine($"Duplex: {qtyResult.Duplex:F8}");
                
                if (qtyResult.Liner is decimal singleLiner)
                {
                    sb.AppendLine($"Liner: {singleLiner:F8}");
                }
                else if (qtyResult.Liner is LinerBreakdown dualLiner)
                {
                    sb.AppendLine($"3-Ply Liner: {dualLiner.ThreePly:F8}");
                    sb.AppendLine($"5-Ply Liner: {dualLiner.FivePly:F8}");
                }
                
                sb.AppendLine($"Printing: {qtyResult.Printing:F8}");
                sb.AppendLine($"Lamination: {qtyResult.Lamination:F8}");
                sb.AppendLine($"Pasting: {qtyResult.Pasting:F8}");
                sb.AppendLine($"Punching: {qtyResult.Punching:F8}");
                sb.AppendLine($"Freight: {qtyResult.Freight:F8}");
                sb.AppendLine($"Base Total: {qtyResult.BaseTotal:F8}");
                sb.AppendLine();
                
                sb.AppendLine($"APPS/Mobile: {qtyResult.Apps.Cost:F8} (With Profit: {qtyResult.Apps.WithProfit:F8})");
                sb.AppendLine($"PIN (No Extras): {qtyResult.PinNoExtras.Cost:F8} (With Profit: {qtyResult.PinNoExtras.WithProfit:F8})");
                sb.AppendLine($"Box Pes. (No Extras): {qtyResult.BoxPesNoExtras.Cost:F8} (With Profit: {qtyResult.BoxPesNoExtras.WithProfit:F8})");
                sb.AppendLine();
                
                sb.AppendLine($"Plate: {qtyResult.Plate:F8}");
                sb.AppendLine($"Die: {qtyResult.Die:F8}");
                sb.AppendLine($"Updated Base Total: {qtyResult.UpdatedBaseTotal:F8}");
                sb.AppendLine();
                
                sb.AppendLine($"TOTAL (With Extras): {qtyResult.TotalWithExtras.Cost:F8} (With Profit: {qtyResult.TotalWithExtras.WithProfit:F8})");
                sb.AppendLine($"PIN (With Extras): {qtyResult.PinWithExtras.Cost:F8} (With Profit: {qtyResult.PinWithExtras.WithProfit:F8})");
                sb.AppendLine($"Box Pes. (With Extras): {qtyResult.BoxPesWithExtras.Cost:F8} (With Profit: {qtyResult.BoxPesWithExtras.WithProfit:F8})");
                sb.AppendLine();
                sb.AppendLine();
            }

            if (result.Errors.Any())
            {
                sb.AppendLine("Errors:");
                foreach (var error in result.Errors)
                {
                    sb.AppendLine($"- {error}");
                }
            }

            return sb.ToString();
        }
    }
}