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

            // Check if box can fit at all
            if (result.BlankLengthMm > sheetLengthMm && result.BlankWidthMm > sheetWidthMm)
            {
                // Box is larger than sheet in both dimensions
                result.BlanksPerSheet = 0;
                result.SheetsRequired = int.MaxValue;
                result.EfficiencyPercentage = 0;
                result.WastePercentage = 100 + input.WastePercentage;
                result.LayoutInfo.LayoutDescription = "Box too large for sheet size";
                return;
            }

            // Calculate how many boxes fit in each direction
            int boxesPerRowLength = 0;
            int boxesPerRowWidth = 0;
            int boxesPerColLength = 0;
            int boxesPerColWidth = 0;

            // Standard orientation (length along sheet length)
            if (result.BlankLengthMm <= sheetLengthMm)
                boxesPerRowLength = (int)(sheetLengthMm / result.BlankLengthMm);
            if (result.BlankWidthMm <= sheetWidthMm)
                boxesPerColLength = (int)(sheetWidthMm / result.BlankWidthMm);

            // Rotated orientation (width along sheet length)
            if (result.BlankWidthMm <= sheetLengthMm)
                boxesPerRowWidth = (int)(sheetLengthMm / result.BlankWidthMm);
            if (result.BlankLengthMm <= sheetWidthMm)
                boxesPerColWidth = (int)(sheetWidthMm / result.BlankLengthMm);

            // Try both orientations and pick the best
            int option1 = boxesPerRowLength * boxesPerColLength; // Standard orientation
            int option2 = boxesPerRowWidth * boxesPerColWidth;   // Rotated orientation

            if (option1 >= option2 && option1 > 0)
            {
                result.BlanksPerSheet = option1;
                result.LayoutInfo.BlanksPerRow = boxesPerRowLength;
                result.LayoutInfo.BlanksPerColumn = boxesPerColLength;
                result.LayoutInfo.UnusedLengthMm = sheetLengthMm - (boxesPerRowLength * result.BlankLengthMm);
                result.LayoutInfo.UnusedWidthMm = sheetWidthMm - (boxesPerColLength * result.BlankWidthMm);
            }
            else if (option2 > 0)
            {
                result.BlanksPerSheet = option2;
                result.LayoutInfo.BlanksPerRow = boxesPerRowWidth;
                result.LayoutInfo.BlanksPerColumn = boxesPerColWidth;
                result.LayoutInfo.UnusedLengthMm = sheetLengthMm - (boxesPerRowWidth * result.BlankWidthMm);
                result.LayoutInfo.UnusedWidthMm = sheetWidthMm - (boxesPerColWidth * result.BlankLengthMm);
            }
            else
            {
                // No boxes fit
                result.BlanksPerSheet = 0;
                result.SheetsRequired = int.MaxValue;
                result.EfficiencyPercentage = 0;
                result.WastePercentage = 100 + input.WastePercentage;
                result.LayoutInfo.LayoutDescription = "Box dimensions too large for sheet";
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
                                                 $"({result.BlanksPerSheet} boxes per sheet, {result.EfficiencyPercentage:F1}% efficiency)";
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
                result.Warnings.Add("Box larger than sheet size. Production not possible with current sheet dimensions.");
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

        /// <summary>
        /// Generate detailed sheet layout visualization data
        /// </summary>
        /// <param name="request">Layout visualization request</param>
        /// <returns>Detailed visualization data for frontend rendering</returns>
        public SheetLayoutVisualization GenerateLayoutVisualization(LayoutVisualizationRequest request)
        {
            var visualization = new SheetLayoutVisualization();

            try
            {
                Console.WriteLine($"Service: Processing visualization request for {request.Length}x{request.Width}x{request.Height} box on {request.SheetLength}x{request.SheetWidth} sheet");

                // Convert dimensions to mm
                var lengthMm = request.Length * INCH_TO_MM_CONVERSION;
                var widthMm = request.Width * INCH_TO_MM_CONVERSION;
                var heightMm = request.Height * INCH_TO_MM_CONVERSION;
                var sheetLengthMm = request.SheetLength * INCH_TO_MM_CONVERSION;
                var sheetWidthMm = request.SheetWidth * INCH_TO_MM_CONVERSION;

                Console.WriteLine($"Service: Converted to mm - Box: {lengthMm}x{widthMm}x{heightMm}, Sheet: {sheetLengthMm}x{sheetWidthMm}");

                // Calculate blank dimensions
                const decimal standardMargin = 12.0m;
                var blankLengthMm = (lengthMm + widthMm + standardMargin) * request.CompressionRatio;
                var blankWidthMm = (widthMm + heightMm + standardMargin) * request.CompressionRatio;

                Console.WriteLine($"Service: Calculated blank dimensions - {blankLengthMm}x{blankWidthMm} mm");

                // Set basic dimensions
                visualization.SheetLengthMm = sheetLengthMm;
                visualization.SheetWidthMm = sheetWidthMm;
                visualization.SheetLengthInches = request.SheetLength;
                visualization.SheetWidthInches = request.SheetWidth;
                visualization.BlankLengthMm = blankLengthMm;
                visualization.BlankWidthMm = blankWidthMm;
                visualization.BlankLengthInches = blankLengthMm / INCH_TO_MM_CONVERSION;
                visualization.BlankWidthInches = blankWidthMm / INCH_TO_MM_CONVERSION;

                // Calculate layout - try both orientations
                var layout1 = CalculateLayoutOption(sheetLengthMm, sheetWidthMm, blankLengthMm, blankWidthMm, false);
                var layout2 = CalculateLayoutOption(sheetLengthMm, sheetWidthMm, blankLengthMm, blankWidthMm, true);

                Console.WriteLine($"Service: Layout option 1 (normal): {layout1.totalBlanks} blanks ({layout1.blanksPerRow}x{layout1.blanksPerColumn})");
                Console.WriteLine($"Service: Layout option 2 (rotated): {layout2.totalBlanks} blanks ({layout2.blanksPerRow}x{layout2.blanksPerColumn})");

                // Choose the best layout
                var bestLayout = layout1.totalBlanks >= layout2.totalBlanks ? layout1 : layout2;
                
                visualization.BlanksPerRow = bestLayout.blanksPerRow;
                visualization.BlanksPerColumn = bestLayout.blanksPerColumn;
                visualization.TotalBlanksPerSheet = bestLayout.totalBlanks;
                visualization.UnusedLengthMm = bestLayout.unusedLength;
                visualization.UnusedWidthMm = bestLayout.unusedWidth;

                Console.WriteLine($"Service: Best layout - {bestLayout.totalBlanks} blanks, unused: {bestLayout.unusedLength}x{bestLayout.unusedWidth} mm");

                // Calculate areas and efficiency
                var sheetAreaSqM = (sheetLengthMm * sheetWidthMm) / (MM_TO_M_CONVERSION * MM_TO_M_CONVERSION);
                var blankAreaSqM = (blankLengthMm * blankWidthMm) / (MM_TO_M_CONVERSION * MM_TO_M_CONVERSION);
                visualization.UsedAreaSqM = visualization.TotalBlanksPerSheet * blankAreaSqM;
                visualization.WastedAreaSqM = sheetAreaSqM - visualization.UsedAreaSqM;
                visualization.EfficiencyPercentage = sheetAreaSqM > 0 ? (visualization.UsedAreaSqM / sheetAreaSqM) * 100 : 0;
                visualization.WastePercentage = 100 - visualization.EfficiencyPercentage;

                Console.WriteLine($"Service: Efficiency calculation - {visualization.EfficiencyPercentage:F1}% efficiency, {visualization.WastePercentage:F1}% waste");

                // Generate blank positions
                GenerateBlankPositions(visualization, bestLayout.rotated);

                // Generate waste areas
                GenerateWasteAreas(visualization);

                // Generate layout description and suggestions
                GenerateLayoutDescription(visualization);
                GenerateOptimizationSuggestions(visualization, request);

                visualization.IsOptimal = visualization.EfficiencyPercentage >= 75;

                Console.WriteLine($"Service: Generated {visualization.BlankPositions.Count} blank positions and {visualization.WasteAreas.Count} waste areas");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Service error in GenerateLayoutVisualization: {ex.Message}");
                Console.WriteLine($"Service stack trace: {ex.StackTrace}");
                visualization.OptimizationSuggestions.Add($"Error generating layout: {ex.Message}");
            }

            return visualization;
        }

        /// <summary>
        /// Calculate layout option for given orientation
        /// </summary>
        private (int blanksPerRow, int blanksPerColumn, int totalBlanks, decimal unusedLength, decimal unusedWidth, bool rotated) 
            CalculateLayoutOption(decimal sheetLength, decimal sheetWidth, decimal blankLength, decimal blankWidth, bool rotateBlank)
        {
            var actualBlankLength = rotateBlank ? blankWidth : blankLength;
            var actualBlankWidth = rotateBlank ? blankLength : blankWidth;

            if (actualBlankLength > sheetLength || actualBlankWidth > sheetWidth)
            {
                return (0, 0, 0, sheetLength, sheetWidth, rotateBlank);
            }

            var blanksPerRow = (int)(sheetLength / actualBlankLength);
            var blanksPerColumn = (int)(sheetWidth / actualBlankWidth);
            var totalBlanks = blanksPerRow * blanksPerColumn;
            var unusedLength = sheetLength - (blanksPerRow * actualBlankLength);
            var unusedWidth = sheetWidth - (blanksPerColumn * actualBlankWidth);

            return (blanksPerRow, blanksPerColumn, totalBlanks, unusedLength, unusedWidth, rotateBlank);
        }

        /// <summary>
        /// Generate positions for each box on the sheet
        /// </summary>
        private void GenerateBlankPositions(SheetLayoutVisualization visualization, bool rotated)
        {
            visualization.BlankPositions.Clear();

            var actualBlankLength = rotated ? visualization.BlankWidthMm : visualization.BlankLengthMm;
            var actualBlankWidth = rotated ? visualization.BlankLengthMm : visualization.BlankWidthMm;

            for (int row = 0; row < visualization.BlanksPerColumn; row++)
            {
                for (int col = 0; col < visualization.BlanksPerRow; col++)
                {
                    var startX = col * actualBlankLength;
                    var startY = row * actualBlankWidth;
                    
                    // Ensure the box position is within sheet bounds
                    if (startX + actualBlankLength <= visualization.SheetLengthMm && 
                        startY + actualBlankWidth <= visualization.SheetWidthMm)
                    {
                        var position = new BlankPosition
                        {
                            Row = row + 1,
                            Column = col + 1,
                            StartXMm = startX,
                            StartYMm = startY,
                            EndXMm = startX + actualBlankLength,
                            EndYMm = startY + actualBlankWidth,
                            WidthMm = actualBlankLength,
                            HeightMm = actualBlankWidth,
                            
                            // Calculate percentages for CSS positioning with bounds checking
                            StartXPercent = Math.Min(100, Math.Max(0, (startX / visualization.SheetLengthMm) * 100)),
                            StartYPercent = Math.Min(100, Math.Max(0, (startY / visualization.SheetWidthMm) * 100)),
                            WidthPercent = Math.Min(100, Math.Max(0, (actualBlankLength / visualization.SheetLengthMm) * 100)),
                            HeightPercent = Math.Min(100, Math.Max(0, (actualBlankWidth / visualization.SheetWidthMm) * 100))
                        };

                        visualization.BlankPositions.Add(position);
                    }
                }
            }
        }

        /// <summary>
        /// Generate waste area information
        /// </summary>
        private void GenerateWasteAreas(SheetLayoutVisualization visualization)
        {
            visualization.WasteAreas.Clear();

            // Calculate used area dimensions
            var usedLengthMm = visualization.BlanksPerRow * (visualization.BlankLengthMm);
            var usedWidthMm = visualization.BlanksPerColumn * (visualization.BlankWidthMm);
            
            // Ensure used dimensions don't exceed sheet dimensions
            usedLengthMm = Math.Min(usedLengthMm, visualization.SheetLengthMm);
            usedWidthMm = Math.Min(usedWidthMm, visualization.SheetWidthMm);
            
            // Recalculate actual unused dimensions
            var actualUnusedLength = visualization.SheetLengthMm - usedLengthMm;
            var actualUnusedWidth = visualization.SheetWidthMm - usedWidthMm;

            // Right margin waste (if any)
            if (actualUnusedLength > 0)
            {
                var rightWaste = new WasteArea
                {
                    AreaType = "RightMargin",
                    StartXMm = usedLengthMm,
                    StartYMm = 0,
                    EndXMm = visualization.SheetLengthMm,
                    EndYMm = usedWidthMm,
                    WidthMm = actualUnusedLength,
                    HeightMm = usedWidthMm,
                    AreaSqM = (actualUnusedLength * usedWidthMm) / (MM_TO_M_CONVERSION * MM_TO_M_CONVERSION),
                    
                    // Calculate percentages with bounds checking
                    StartXPercent = Math.Min(100, Math.Max(0, (usedLengthMm / visualization.SheetLengthMm) * 100)),
                    StartYPercent = 0,
                    WidthPercent = Math.Min(100, Math.Max(0, (actualUnusedLength / visualization.SheetLengthMm) * 100)),
                    HeightPercent = Math.Min(100, Math.Max(0, (usedWidthMm / visualization.SheetWidthMm) * 100))
                };
                
                // Only add if the waste area makes sense
                if (rightWaste.WidthPercent > 0.1m && rightWaste.HeightPercent > 0.1m)
                {
                    visualization.WasteAreas.Add(rightWaste);
                }
            }

            // Bottom margin waste (if any)
            if (actualUnusedWidth > 0)
            {
                var bottomWaste = new WasteArea
                {
                    AreaType = "BottomMargin",
                    StartXMm = 0,
                    StartYMm = usedWidthMm,
                    EndXMm = usedLengthMm,
                    EndYMm = visualization.SheetWidthMm,
                    WidthMm = usedLengthMm,
                    HeightMm = actualUnusedWidth,
                    AreaSqM = (usedLengthMm * actualUnusedWidth) / (MM_TO_M_CONVERSION * MM_TO_M_CONVERSION),
                    
                    // Calculate percentages with bounds checking
                    StartXPercent = 0,
                    StartYPercent = Math.Min(100, Math.Max(0, (usedWidthMm / visualization.SheetWidthMm) * 100)),
                    WidthPercent = Math.Min(100, Math.Max(0, (usedLengthMm / visualization.SheetLengthMm) * 100)),
                    HeightPercent = Math.Min(100, Math.Max(0, (actualUnusedWidth / visualization.SheetWidthMm) * 100))
                };
                
                // Only add if the waste area makes sense
                if (bottomWaste.WidthPercent > 0.1m && bottomWaste.HeightPercent > 0.1m)
                {
                    visualization.WasteAreas.Add(bottomWaste);
                }
            }

            // Corner waste (if both margins exist)
            if (actualUnusedLength > 0 && actualUnusedWidth > 0)
            {
                var cornerWaste = new WasteArea
                {
                    AreaType = "Corner",
                    StartXMm = usedLengthMm,
                    StartYMm = usedWidthMm,
                    EndXMm = visualization.SheetLengthMm,
                    EndYMm = visualization.SheetWidthMm,
                    WidthMm = actualUnusedLength,
                    HeightMm = actualUnusedWidth,
                    AreaSqM = (actualUnusedLength * actualUnusedWidth) / (MM_TO_M_CONVERSION * MM_TO_M_CONVERSION),
                    
                    // Calculate percentages with bounds checking
                    StartXPercent = Math.Min(100, Math.Max(0, (usedLengthMm / visualization.SheetLengthMm) * 100)),
                    StartYPercent = Math.Min(100, Math.Max(0, (usedWidthMm / visualization.SheetWidthMm) * 100)),
                    WidthPercent = Math.Min(100, Math.Max(0, (actualUnusedLength / visualization.SheetLengthMm) * 100)),
                    HeightPercent = Math.Min(100, Math.Max(0, (actualUnusedWidth / visualization.SheetWidthMm) * 100))
                };
                
                // Only add if the waste area makes sense
                if (cornerWaste.WidthPercent > 0.1m && cornerWaste.HeightPercent > 0.1m)
                {
                    visualization.WasteAreas.Add(cornerWaste);
                }
            }
            
            // Update unused dimensions in visualization
            visualization.UnusedLengthMm = actualUnusedLength;
            visualization.UnusedWidthMm = actualUnusedWidth;
        }

        /// <summary>
        /// Generate layout description
        /// </summary>
        private void GenerateLayoutDescription(SheetLayoutVisualization visualization)
        {
            visualization.LayoutDescription = $"{visualization.BlanksPerRow} × {visualization.BlanksPerColumn} layout " +
                                           $"({visualization.TotalBlanksPerSheet} boxes per sheet, {visualization.EfficiencyPercentage:F1}% efficiency)";
        }

        /// <summary>
        /// Generate optimization suggestions
        /// </summary>
        private void GenerateOptimizationSuggestions(SheetLayoutVisualization visualization, LayoutVisualizationRequest request)
        {
            visualization.OptimizationSuggestions.Clear();

            if (visualization.EfficiencyPercentage < 60)
            {
                visualization.OptimizationSuggestions.Add("Consider adjusting box dimensions to improve sheet utilization.");
                visualization.OptimizationSuggestions.Add("Try different sheet sizes for better efficiency.");
            }
            else if (visualization.EfficiencyPercentage < 75)
            {
                visualization.OptimizationSuggestions.Add("Good layout, but there's room for improvement. Consider fine-tuning dimensions.");
            }
            else if (visualization.EfficiencyPercentage >= 85)
            {
                visualization.OptimizationSuggestions.Add("Excellent sheet utilization! This is an optimal layout.");
            }

            if (visualization.WastePercentage > 25)
            {
                visualization.OptimizationSuggestions.Add($"High waste ({visualization.WastePercentage:F1}%). Consider reducing box size or changing sheet size.");
            }

            if (visualization.TotalBlanksPerSheet == 0)
            {
                visualization.OptimizationSuggestions.Add("Box blank is too large for the selected sheet size. Choose a larger sheet or reduce box dimensions.");
            }
            else if (visualization.TotalBlanksPerSheet == 1)
            {
                visualization.OptimizationSuggestions.Add("Only one blank fits per sheet. Consider optimizing dimensions for better economics.");
            }
        }

        /// <summary>
        /// Get live layout visualization updates
        /// </summary>
        public SheetLayoutVisualization GetLiveLayoutUpdate(LayoutVisualizationRequest request)
        {
            return GenerateLayoutVisualization(request);
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