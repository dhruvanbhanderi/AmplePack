using AmplePack.Models;

namespace AmplePack.Services
{
    /// <summary>
    /// Service for calculating corrugated box prices following industry standards
    /// Implements step-by-step calculation methodology for accurate pricing
    /// </summary>
    public class BoxPriceCalculatorService
    {
        private const decimal GST_RATE = 0.18m; // 18% GST
        private const decimal INCH_TO_MM_CONVERSION = 25.4m; // 1 inch = 25.4mm
        private const decimal MM_TO_M_CONVERSION = 1000m; // 1000mm = 1m

        /// <summary>
        /// Calculate complete box pricing with detailed breakdown
        /// </summary>
        /// <param name="input">Box calculator input parameters</param>
        /// <returns>Detailed calculation results</returns>
        public BoxCalculatorResult CalculateBoxPrice(BoxCalculatorInput input)
        {
            var result = new BoxCalculatorResult { Input = input };

            try
            {
                // Step 1: Calculate blank dimensions and area
                CalculateBlankDimensions(input, result);

                // Step 2: Calculate box weight
                CalculateBoxWeight(input, result);

                // Step 3: Optimize sheet layout
                OptimizeSheetLayout(input, result);

                // Step 4: Calculate material costs
                CalculateMaterialCosts(input, result);

                // Step 5: Calculate production costs
                CalculateProductionCosts(input, result);

                // Step 6: Calculate business costs (overhead, profit)
                CalculateBusinessCosts(input, result);

                // Step 7: Apply discounts and GST
                ApplyDiscountsAndGST(input, result);

                // Step 8: Calculate total order costs
                CalculateTotalOrderCosts(input, result);

                // Step 9: Generate warnings and recommendations
                GenerateWarningsAndRecommendations(input, result);

                // Step 10: Create detailed breakdown
                CreateDetailedBreakdown(input, result);
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"Calculation error: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Step 1: Calculate blank dimensions using standard box formula
        /// Convert inches to mm first, then calculate blank dimensions
        /// </summary>
        private void CalculateBlankDimensions(BoxCalculatorInput input, BoxCalculatorResult result)
        {
            // Convert inches to mm
            decimal lengthMm = input.Length * INCH_TO_MM_CONVERSION;
            decimal widthMm = input.Width * INCH_TO_MM_CONVERSION;
            decimal heightMm = input.Height * INCH_TO_MM_CONVERSION;

            // Standard margin for box construction (typically 10-15mm per side)
            const decimal standardMargin = 12.0m;

            // Calculate blank dimensions with compression ratio
            result.BlankLengthMm = (lengthMm + widthMm + standardMargin) * input.CompressionRatio;
            result.BlankWidthMm = (widthMm + heightMm + standardMargin) * input.CompressionRatio;

            // Convert to square meters
            result.BlankAreaSqM = (result.BlankLengthMm * result.BlankWidthMm) / (MM_TO_M_CONVERSION * MM_TO_M_CONVERSION);
        }

        /// <summary>
        /// Step 2: Calculate box weight based on board GSM and area
        /// </summary>
        private void CalculateBoxWeight(BoxCalculatorInput input, BoxCalculatorResult result)
        {
            // Weight = Area (m²) × GSM (grams per m²)
            result.BoxWeightGrams = result.BlankAreaSqM * input.BoardGSM;
        }

        /// <summary>
        /// Step 3: Optimize sheet layout to minimize waste
        /// </summary>
        private void OptimizeSheetLayout(BoxCalculatorInput input, BoxCalculatorResult result)
        {
            // Convert sheet dimensions from inches to mm
            decimal sheetLengthMm = input.SheetLength * INCH_TO_MM_CONVERSION;
            decimal sheetWidthMm = input.SheetWidth * INCH_TO_MM_CONVERSION;

            // Check if blank can fit at all
            if (result.BlankLengthMm > sheetLengthMm && result.BlankWidthMm > sheetWidthMm)
            {
                // Blank is larger than sheet in both dimensions
                result.BlanksPerSheet = 0;
                result.SheetsRequired = int.MaxValue;
                result.EfficiencyPercentage = 0;
                result.WastePercentage = 100 + input.WastePercentage;
                result.LayoutInfo.LayoutDescription = "Blank too large for sheet size";
                return;
            }

            // Calculate how many blanks fit in each direction
            int blanksPerRowLength = 0;
            int blanksPerRowWidth = 0;
            int blanksPerColLength = 0;
            int blanksPerColWidth = 0;

            // Standard orientation (length along sheet length)
            if (result.BlankLengthMm <= sheetLengthMm)
                blanksPerRowLength = (int)(sheetLengthMm / result.BlankLengthMm);
            if (result.BlankWidthMm <= sheetWidthMm)
                blanksPerColLength = (int)(sheetWidthMm / result.BlankWidthMm);

            // Rotated orientation (width along sheet length)
            if (result.BlankWidthMm <= sheetLengthMm)
                blanksPerRowWidth = (int)(sheetLengthMm / result.BlankWidthMm);
            if (result.BlankLengthMm <= sheetWidthMm)
                blanksPerColWidth = (int)(sheetWidthMm / result.BlankLengthMm);

            // Try both orientations and pick the best
            int option1 = blanksPerRowLength * blanksPerColLength; // Standard orientation
            int option2 = blanksPerRowWidth * blanksPerColWidth;   // Rotated orientation

            if (option1 >= option2 && option1 > 0)
            {
                result.BlanksPerSheet = option1;
                result.LayoutInfo.BlanksPerRow = blanksPerRowLength;
                result.LayoutInfo.BlanksPerColumn = blanksPerColLength;
                result.LayoutInfo.UnusedLengthMm = sheetLengthMm - (blanksPerRowLength * result.BlankLengthMm);
                result.LayoutInfo.UnusedWidthMm = sheetWidthMm - (blanksPerColLength * result.BlankWidthMm);
            }
            else if (option2 > 0)
            {
                result.BlanksPerSheet = option2;
                result.LayoutInfo.BlanksPerRow = blanksPerRowWidth;
                result.LayoutInfo.BlanksPerColumn = blanksPerColWidth;
                result.LayoutInfo.UnusedLengthMm = sheetLengthMm - (blanksPerRowWidth * result.BlankWidthMm);
                result.LayoutInfo.UnusedWidthMm = sheetWidthMm - (blanksPerColWidth * result.BlankLengthMm);
            }
            else
            {
                // No blanks fit
                result.BlanksPerSheet = 0;
                result.SheetsRequired = int.MaxValue;
                result.EfficiencyPercentage = 0;
                result.WastePercentage = 100 + input.WastePercentage;
                result.LayoutInfo.LayoutDescription = "Blank dimensions too large for sheet";
                return;
            }

            // Calculate sheets required
            if (result.BlanksPerSheet > 0)
            {
                result.SheetsRequired = (int)Math.Ceiling((double)input.Quantity / result.BlanksPerSheet);
            }
            else
            {
                result.SheetsRequired = int.MaxValue;
            }

            // Calculate efficiency and waste
            decimal sheetAreaSqM = (sheetLengthMm * sheetWidthMm) / (MM_TO_M_CONVERSION * MM_TO_M_CONVERSION);
            result.LayoutInfo.UsedAreaSqM = result.BlanksPerSheet * result.BlankAreaSqM;
            result.LayoutInfo.WastedAreaSqM = sheetAreaSqM - result.LayoutInfo.UsedAreaSqM;
            
            if (sheetAreaSqM > 0)
            {
                result.EfficiencyPercentage = (result.LayoutInfo.UsedAreaSqM / sheetAreaSqM) * 100;
            }
            else
            {
                result.EfficiencyPercentage = 0;
            }
            
            result.WastePercentage = 100 - result.EfficiencyPercentage;

            // Add waste allowance from input
            result.WastePercentage += input.WastePercentage;

            // Layout description
            result.LayoutInfo.LayoutDescription = $"{result.LayoutInfo.BlanksPerRow} × {result.LayoutInfo.BlanksPerColumn} layout " +
                                                 $"({result.BlanksPerSheet} blanks per sheet, {result.EfficiencyPercentage:F1}% efficiency)";
        }

        /// <summary>
        /// Step 4: Calculate material costs
        /// </summary>
        private void CalculateMaterialCosts(BoxCalculatorInput input, BoxCalculatorResult result)
        {
            // Base material cost per box
            result.MaterialCostPerBox = result.BlankAreaSqM * input.BoardRatePerSqM;

            // Add waste allowance
            decimal wasteMultiplier = 1 + (result.WastePercentage / 100);
            result.MaterialCostPerBox *= wasteMultiplier;

            // Total material cost for order
            result.TotalMaterialCost = result.MaterialCostPerBox * input.Quantity;
        }

        /// <summary>
        /// Step 5: Calculate production costs (printing, die cutting, labor)
        /// </summary>
        private void CalculateProductionCosts(BoxCalculatorInput input, BoxCalculatorResult result)
        {
            // Printing cost per box
            result.PrintingCostPerBox = result.BlankAreaSqM * input.PrintingCostPerSqM;

            // Die cutting cost per box
            result.DieCuttingCostPerBox = result.BlankAreaSqM * input.DieCuttingCostPerSqM;

            // Labor cost per box (fixed)
            result.LaborCostPerBox = input.LaborCostPerBox;

            // Base cost per box (material + production)
            result.BaseCostPerBox = result.MaterialCostPerBox + result.PrintingCostPerBox + 
                                   result.DieCuttingCostPerBox + result.LaborCostPerBox;

            // Total production cost
            result.TotalProductionCost = (result.PrintingCostPerBox + result.DieCuttingCostPerBox + result.LaborCostPerBox) * input.Quantity;
        }

        /// <summary>
        /// Step 6: Calculate business costs (overhead and profit)
        /// </summary>
        private void CalculateBusinessCosts(BoxCalculatorInput input, BoxCalculatorResult result)
        {
            // Overhead cost per box
            result.OverheadCostPerBox = result.BaseCostPerBox * (input.OverheadPercentage / 100);

            // Cost before profit
            decimal costBeforeProfit = result.BaseCostPerBox + result.OverheadCostPerBox;

            // Profit per box
            result.ProfitPerBox = costBeforeProfit * (input.ProfitMarginPercentage / 100);

            // Cost before discount
            result.CostBeforeDiscountPerBox = costBeforeProfit + result.ProfitPerBox;
        }

        /// <summary>
        /// Step 7: Apply discounts and calculate GST
        /// </summary>
        private void ApplyDiscountsAndGST(BoxCalculatorInput input, BoxCalculatorResult result)
        {
            // Discount per box
            result.DiscountPerBox = result.CostBeforeDiscountPerBox * (input.DiscountPercentage / 100);

            // Cost after discount
            result.CostAfterDiscountPerBox = result.CostBeforeDiscountPerBox - result.DiscountPerBox;

            // Final price ex-GST
            result.FinalPricePerBoxExGST = result.CostAfterDiscountPerBox;

            // GST calculation
            if (input.IncludeGST)
            {
                result.GSTPerBox = result.FinalPricePerBoxExGST * GST_RATE;
                result.FinalPricePerBoxIncGST = result.FinalPricePerBoxExGST + result.GSTPerBox;
            }
            else
            {
                result.GSTPerBox = 0;
                result.FinalPricePerBoxIncGST = result.FinalPricePerBoxExGST;
            }
        }

        /// <summary>
        /// Step 8: Calculate total order costs
        /// </summary>
        private void CalculateTotalOrderCosts(BoxCalculatorInput input, BoxCalculatorResult result)
        {
            // Total order cost ex-GST
            result.TotalOrderCostExGST = result.FinalPricePerBoxExGST * input.Quantity;

            // Total GST
            result.TotalGST = result.GSTPerBox * input.Quantity;

            // Total order cost inc GST
            result.TotalOrderCostIncGST = result.TotalOrderCostExGST + result.TotalGST;

            // Shipping cost
            result.ShippingCost = input.ShippingCostPerOrder;

            // Grand total
            result.GrandTotal = result.TotalOrderCostIncGST + result.ShippingCost;
        }

        /// <summary>
        /// Step 9: Generate warnings and recommendations
        /// </summary>
        private void GenerateWarningsAndRecommendations(BoxCalculatorInput input, BoxCalculatorResult result)
        {
            // Efficiency warnings
            if (result.EfficiencyPercentage < 60)
            {
                result.Warnings.Add($"Low sheet efficiency ({result.EfficiencyPercentage:F1}%). Consider adjusting box dimensions or sheet size.");
            }

            if (result.WastePercentage > 25)
            {
                result.Warnings.Add($"High waste percentage ({result.WastePercentage:F1}%). Review layout optimization.");
            }

            // Convert dimensions to mm for size checking
            decimal sheetLengthMm = input.SheetLength * INCH_TO_MM_CONVERSION;
            decimal sheetWidthMm = input.SheetWidth * INCH_TO_MM_CONVERSION;

            // Size warnings
            if (result.BlankLengthMm > sheetLengthMm || result.BlankWidthMm > sheetWidthMm)
            {
                result.Warnings.Add("Box blank larger than sheet size. Production not possible with current sheet dimensions.");
            }

            // Economic warnings
            if (result.FinalPricePerBoxIncGST < 5.0m)
            {
                result.Warnings.Add("Very low per-box price. Verify calculations and minimum order quantities.");
            }

            if (input.ProfitMarginPercentage < 10)
            {
                result.Warnings.Add("Low profit margin. Consider increasing margins for business sustainability.");
            }

            // Recommendations
            if (result.EfficiencyPercentage < 80 && result.EfficiencyPercentage >= 60)
            {
                result.Recommendations.Add("Consider optimizing box dimensions to improve sheet utilization.");
            }

            if (input.Quantity < 500)
            {
                result.Recommendations.Add("Small quantity order. Consider minimum order quantities for better economics.");
            }

            if (input.BoardGSM < 150)
            {
                result.Recommendations.Add("Low GSM board. Ensure structural integrity for intended use.");
            }
        }

        /// <summary>
        /// Step 10: Create detailed cost breakdown
        /// </summary>
        private void CreateDetailedBreakdown(BoxCalculatorInput input, BoxCalculatorResult result)
        {
            result.DetailedBreakdown = new Dictionary<string, decimal>
            {
                { "Material Cost per Box", result.MaterialCostPerBox },
                { "Printing Cost per Box", result.PrintingCostPerBox },
                { "Die Cutting Cost per Box", result.DieCuttingCostPerBox },
                { "Labor Cost per Box", result.LaborCostPerBox },
                { "Base Cost per Box", result.BaseCostPerBox },
                { "Overhead Cost per Box", result.OverheadCostPerBox },
                { "Profit per Box", result.ProfitPerBox },
                { "Cost before Discount per Box", result.CostBeforeDiscountPerBox },
                { "Discount per Box", result.DiscountPerBox },
                { "Final Price per Box (Ex-GST)", result.FinalPricePerBoxExGST },
                { "GST per Box", result.GSTPerBox },
                { "Final Price per Box (Inc-GST)", result.FinalPricePerBoxIncGST },
                { "Total Material Cost", result.TotalMaterialCost },
                { "Total Production Cost", result.TotalProductionCost },
                { "Total Order Cost (Ex-GST)", result.TotalOrderCostExGST },
                { "Total GST", result.TotalGST },
                { "Total Order Cost (Inc-GST)", result.TotalOrderCostIncGST },
                { "Shipping Cost", result.ShippingCost },
                { "Grand Total", result.GrandTotal }
            };
        }

        /// <summary>
        /// Get optimized sheet layout suggestions
        /// </summary>
        public List<SheetLayoutSuggestion> GetSheetLayoutSuggestions(BoxCalculatorInput input)
        {
            var suggestions = new List<SheetLayoutSuggestion>();
            var standardSheets = SheetSpecs.GetStandardSheetSizes().Where(s => s.IsStandard).ToList();

            foreach (var sheet in standardSheets)
            {
                var tempInput = new BoxCalculatorInput
                {
                    Length = input.Length,
                    Width = input.Width,
                    Height = input.Height,
                    BoardGSM = input.BoardGSM,
                    CompressionRatio = input.CompressionRatio,
                    SheetLength = sheet.LengthInches,
                    SheetWidth = sheet.WidthInches,
                    Quantity = input.Quantity
                };

                var tempResult = new BoxCalculatorResult { Input = tempInput };
                CalculateBlankDimensions(tempInput, tempResult);
                OptimizeSheetLayout(tempInput, tempResult);

                suggestions.Add(new SheetLayoutSuggestion
                {
                    SheetSize = sheet.SheetSize,
                    BlanksPerSheet = tempResult.BlanksPerSheet,
                    EfficiencyPercentage = tempResult.EfficiencyPercentage,
                    SheetsRequired = tempResult.SheetsRequired,
                    LayoutDescription = tempResult.LayoutInfo.LayoutDescription
                });
            }

            return suggestions.OrderByDescending(s => s.EfficiencyPercentage).ToList();
        }
    }

    /// <summary>
    /// Suggestion for optimal sheet layout
    /// </summary>
    public class SheetLayoutSuggestion
    {
        public string SheetSize { get; set; } = string.Empty;
        public int BlanksPerSheet { get; set; }
        public decimal EfficiencyPercentage { get; set; }
        public int SheetsRequired { get; set; }
        public string LayoutDescription { get; set; } = string.Empty;
    }
}