using AmplePack.Models;

namespace AmplePack.Services
{
    public interface IBoxCalculatorService
    {
        Task<BoxCalculatorResult> CalculateBoxRateAsync(BoxCalculatorRequest request);
        Task<List<CostItem>> GetEditableCostItemsAsync();
        Task UpdateCostItemAsync(string itemId, decimal newValue);
    }

    public class BoxCalculatorService : IBoxCalculatorService
    {
        private readonly ILogger<BoxCalculatorService> _logger;

        public BoxCalculatorService(ILogger<BoxCalculatorService> logger)
        {
            _logger = logger;
        }

        public async Task<BoxCalculatorResult> CalculateBoxRateAsync(BoxCalculatorRequest request)
        {
            try
            {
                _logger.LogInformation("Starting box rate calculation for {Length}x{Width}x{Height}, BoardType: {BoardType}, Qty: {Quantity}", 
                    request.Length, request.Width, request.Height, request.BoardType, request.Quantity);

                // Validate board configuration
                if (!BoardTypeConstants.BoardConfigurations.ContainsKey(request.BoardType))
                {
                    throw new ArgumentException($"Invalid board type: {request.BoardType}");
                }

                var boardConfig = BoardTypeConstants.BoardConfigurations[request.BoardType];
                
                // ✅ CORRECTED - Validate required papers for ALL board types
                ValidateRequiredPapers(request, boardConfig);

                var result = new BoxCalculatorResult
                {
                    Length = request.Length,
                    Width = request.Width,
                    Height = request.Height,
                    BoardType = request.BoardType,
                    Quantity = request.Quantity
                };

                // Calculate single sheet analysis with industry formulas
                result.SheetAnalysis = await CalculateSheetAnalysisAsync(request, boardConfig);

                // Calculate comprehensive cost breakdown
                result.CostBreakdown = await CalculateCostBreakdownAsync(request, result.SheetAnalysis);

                // Calculate final pricing
                result.FinalPricePerBox = result.CostBreakdown.SellingPricePerBox;
                result.FinalPricePerBoxWithGST = request.IncludeGST 
                    ? result.FinalPricePerBox + result.CostBreakdown.GSTAmountPerBox
                    : result.FinalPricePerBox;

                result.TotalOrderValue = result.FinalPricePerBox * request.Quantity;
                result.TotalOrderValueWithGST = result.FinalPricePerBoxWithGST * request.Quantity;

                // Calculate efficiency metrics
                result.MaterialEfficiency = result.SheetAnalysis.UtilizationPercentage;
                result.WastagePercentage = 100 - result.MaterialEfficiency;
                result.ProfitPerBox = result.CostBreakdown.ProfitPerBox;

                _logger.LogInformation("Box rate calculation completed: ₹{FinalPrice} per box, {Apps} apps from sheet {SheetSize}", 
                    result.FinalPricePerBoxWithGST, result.SheetAnalysis.TotalApps, 
                    $"{result.SheetAnalysis.SheetLength}\"×{result.SheetAnalysis.SheetWidth}\"");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating box rate for {Length}x{Width}x{Height}", 
                    request.Length, request.Width, request.Height);
                throw;
            }
        }

        // ✅ CORRECTED - Proper validation for ALL ply types (GSM total validation removed)
        private void ValidateRequiredPapers(BoxCalculatorRequest request, BoardConfiguration boardConfig)
        {
            // Top liner is always required
            if (request.Paper1GSM == 0)
                throw new ArgumentException($"Top liner GSM is required for {request.BoardType} boards");

            // Bottom liner is ALWAYS required for ALL board types (Industry Standard)
            if (request.Paper2GSM == 0)
                throw new ArgumentException($"Bottom liner GSM is required for {request.BoardType} boards");

            // Medium is always required
            if (request.MediumGSM == 0)
                throw new ArgumentException($"Medium GSM is required for {request.BoardType} boards");

            // Note: Total GSM validation removed - users can now set any GSM combination
            // Individual GSM values are validated through model annotations (100-400 for liners, 80-200 for medium)
        }

        private async Task<SheetAnalysis> CalculateSheetAnalysisAsync(
            BoxCalculatorRequest request, 
            BoardConfiguration boardConfig)
        {
            var analysis = new SheetAnalysis
            {
                SheetLength = request.SheetLength,
                SheetWidth = request.SheetWidth,
                SheetArea = request.SheetLength * request.SheetWidth
            };

            // EXACT INDUSTRY FORMULA IMPLEMENTATION:
            // Box Layout Dimensions = Box Dimension + (2 × Height) for flaps
            analysis.BoxLayoutLength = request.Length + (2 * request.Height);
            analysis.BoxLayoutWidth = request.Width + (2 * request.Height);

            _logger.LogDebug("Box layout: {Length} + (2×{Height}) = {LayoutLength}, {Width} + (2×{Height}) = {LayoutWidth}",
                request.Length, request.Height, analysis.BoxLayoutLength, request.Width, analysis.BoxLayoutWidth);

            // Apps Calculation - Industry Standard Formula
            var appsLength1 = (int)Math.Floor(request.SheetLength / analysis.BoxLayoutLength);
            var appsWidth1 = (int)Math.Floor(request.SheetWidth / analysis.BoxLayoutWidth);
            var totalApps1 = appsLength1 * appsWidth1;

            // Try rotated orientation (90 degrees) - Industry Practice
            var appsLength2 = (int)Math.Floor(request.SheetLength / analysis.BoxLayoutWidth);
            var appsWidth2 = (int)Math.Floor(request.SheetWidth / analysis.BoxLayoutLength);
            var totalApps2 = appsLength2 * appsWidth2;

            // Select orientation with maximum apps
            if (totalApps2 > totalApps1)
            {
                analysis.AppsLength = appsLength2;
                analysis.AppsWidth = appsWidth2;
                analysis.TotalApps = totalApps2;
                // Swap layout dimensions for rotated display
                (analysis.BoxLayoutLength, analysis.BoxLayoutWidth) = (analysis.BoxLayoutWidth, analysis.BoxLayoutLength);
                _logger.LogDebug("Using rotated orientation: {AppsLength} × {AppsWidth} = {TotalApps} apps", 
                    appsLength2, appsWidth2, totalApps2);
            }
            else
            {
                analysis.AppsLength = appsLength1;
                analysis.AppsWidth = appsWidth1;
                analysis.TotalApps = totalApps1;
                _logger.LogDebug("Using standard orientation: {AppsLength} × {AppsWidth} = {TotalApps} apps", 
                    appsLength1, appsWidth1, totalApps1);
            }

            // Calculate sheet utilization
            analysis.UsedArea = analysis.TotalApps * (analysis.BoxLayoutLength * analysis.BoxLayoutWidth);
            analysis.WasteArea = analysis.SheetArea - analysis.UsedArea;
            analysis.UtilizationPercentage = analysis.SheetArea > 0 
                ? (analysis.UsedArea / analysis.SheetArea) * 100 
                : 0;

            // Calculate material costs per sheet - Industry Formula
            await CalculateMaterialCostsPerSheetAsync(request, analysis, boardConfig);
            
            // Processing costs per sheet
            analysis.ProcessingCostPerSheet = request.PrintingCostPerSheet + request.DieCuttingCostPerSheet;
            
            analysis.TotalCostPerSheet = analysis.TotalMaterialCostPerSheet + analysis.ProcessingCostPerSheet;

            // Calculate per box costs
            if (analysis.TotalApps > 0)
            {
                analysis.MaterialCostPerBox = analysis.TotalMaterialCostPerSheet / analysis.TotalApps;
                analysis.ProcessingCostPerBox = analysis.ProcessingCostPerSheet / analysis.TotalApps;
                analysis.TotalCostPerBox = analysis.TotalCostPerSheet / analysis.TotalApps;
            }
            else
            {
                throw new InvalidOperationException($"No boxes fit on sheet {request.SheetLength}×{request.SheetWidth} with box layout {analysis.BoxLayoutLength}×{analysis.BoxLayoutWidth}");
            }

            _logger.LogDebug("Sheet Analysis: {TotalApps} apps, {Utilization:F1}% utilization, ₹{CostPerBox:F2} per box",
                analysis.TotalApps, analysis.UtilizationPercentage, analysis.TotalCostPerBox);

            return analysis;
        }

        // ✅ ENHANCED - Industry-accurate material cost calculation with proper duplex logic
        private async Task CalculateMaterialCostsPerSheetAsync(
            BoxCalculatorRequest request, 
            SheetAnalysis analysis, 
            BoardConfiguration boardConfig)
        {
            // Convert sheet area from square inches to square meters
            var sheetAreaSquareMeters = analysis.SheetArea / 1550m; // 1 m² = 1550 sq inches

            // Apply flute factor to medium GSM
            var fluteFactor = BoardTypeConstants.FluteFactors.GetValueOrDefault(request.FluteType, 1.4m);

            _logger.LogDebug("Calculating material costs for {BoardType} board with {FluteType} flute", 
                request.BoardType, request.FluteType);

            // Calculate base material weights and costs
            decimal totalPaper1Cost = 0m;
            decimal totalPaper2Cost = 0m;
            decimal totalMediumCost = 0m;

            // CORRECTED: Board-specific calculations (Duplex Box removed)
            switch (request.BoardType)
            {
                case "3 Ply": // Single Wall
                    {
                        // Standard single wall: Top Liner + Medium + Bottom Liner
                        var paper1Weight = (request.Paper1GSM * sheetAreaSquareMeters) / 1000m;
                        var paper2Weight = (request.Paper2GSM * sheetAreaSquareMeters) / 1000m;
                        var mediumWeight = (request.MediumGSM * fluteFactor * sheetAreaSquareMeters) / 1000m;

                        totalPaper1Cost = paper1Weight * request.Paper1RatePerKg;
                        totalPaper2Cost = paper2Weight * request.Paper2RatePerKg;
                        totalMediumCost = mediumWeight * request.MediumRatePerKg;

                        _logger.LogDebug("3-Ply calculation: Paper1={P1}kg×Rs.{R1}, Paper2={P2}kg×Rs.{R2}, Medium={M}kg×Rs.{RM}", 
                            paper1Weight, request.Paper1RatePerKg, paper2Weight, request.Paper2RatePerKg, 
                            mediumWeight, request.MediumRatePerKg);
                        break;
                    }

                case "5 Ply": // Double Wall
                    {
                        // Double wall structure: Top + Inner + Bottom + 2×Medium layers
                        var topLinerWeight = (request.Paper1GSM * sheetAreaSquareMeters) / 1000m;
                        var innerLinerWeight = (request.Paper1GSM * 0.85m * sheetAreaSquareMeters) / 1000m; // 85% of top liner
                        var bottomLinerWeight = (request.Paper2GSM * sheetAreaSquareMeters) / 1000m;
                        
                        // Two medium layers with flute factor
                        var mediumWeight1 = (request.MediumGSM * fluteFactor * sheetAreaSquareMeters) / 1000m;
                        var mediumWeight2 = (request.MediumGSM * fluteFactor * 0.9m * sheetAreaSquareMeters) / 1000m; // Second layer 90%

                        totalPaper1Cost = (topLinerWeight + innerLinerWeight) * request.Paper1RatePerKg;
                        totalPaper2Cost = bottomLinerWeight * request.Paper2RatePerKg;
                        totalMediumCost = (mediumWeight1 + mediumWeight2) * request.MediumRatePerKg;

                        _logger.LogDebug("5-Ply calculation: Top+Inner={P1}kg×Rs.{R1}, Bottom={P2}kg×Rs.{R2}, 2×Medium={M}kg×Rs.{RM}", 
                            topLinerWeight + innerLinerWeight, request.Paper1RatePerKg, bottomLinerWeight, request.Paper2RatePerKg,
                            mediumWeight1 + mediumWeight2, request.MediumRatePerKg);
                        break;
                    }

                case "7 Ply": // Triple Wall (Regular - No Duplex)
                    {
                        // Triple wall structure: Multiple liners + 3×Medium layers
                        var topLinerWeight = (request.Paper1GSM * sheetAreaSquareMeters) / 1000m;
                        var innerLiner1Weight = (request.Paper1GSM * 0.9m * sheetAreaSquareMeters) / 1000m; // 90% of top
                        var innerLiner2Weight = (request.Paper2GSM * 0.85m * sheetAreaSquareMeters) / 1000m; // 85% of bottom
                        var bottomLinerWeight = (request.Paper2GSM * sheetAreaSquareMeters) / 1000m;

                        // Three medium layers with progressive flute factors
                        var mediumWeight1 = (request.MediumGSM * fluteFactor * sheetAreaSquareMeters) / 1000m; // First layer
                        var mediumWeight2 = (request.MediumGSM * fluteFactor * 0.95m * sheetAreaSquareMeters) / 1000m; // Second layer
                        var mediumWeight3 = (request.MediumGSM * fluteFactor * 0.9m * sheetAreaSquareMeters) / 1000m; // Third layer

                        var totalMediumWeight = mediumWeight1 + mediumWeight2 + mediumWeight3;

                        totalPaper1Cost = (topLinerWeight + innerLiner1Weight) * request.Paper1RatePerKg;
                        totalPaper2Cost = (bottomLinerWeight + innerLiner2Weight) * request.Paper2RatePerKg;
                        totalMediumCost = totalMediumWeight * request.MediumRatePerKg;

                        _logger.LogDebug("7-Ply calculation: Top+Inner1={P1}kg×Rs.{R1}, Bottom+Inner2={P2}kg×Rs.{R2}, 3×Medium={M}kg×Rs.{RM}", 
                            topLinerWeight + innerLiner1Weight, request.Paper1RatePerKg, 
                            bottomLinerWeight + innerLiner2Weight, request.Paper2RatePerKg,
                            totalMediumWeight, request.MediumRatePerKg);
                        break;
                    }

                default:
                    throw new ArgumentException($"Unsupported board type: {request.BoardType}");
            }

            // Assign calculated costs to analysis
            analysis.Paper1CostPerSheet = totalPaper1Cost;
            analysis.Paper2CostPerSheet = totalPaper2Cost;
            analysis.MediumCostPerSheet = totalMediumCost;

            // Total material cost per sheet
            analysis.TotalMaterialCostPerSheet = analysis.Paper1CostPerSheet + 
                                                analysis.Paper2CostPerSheet + 
                                                analysis.MediumCostPerSheet;

            _logger.LogInformation("Material costs for {BoardType}: Paper1=₹{P1:F2}, Paper2=₹{P2:F2}, Medium=₹{M:F2}, Total=₹{T:F2}",
                request.BoardType, analysis.Paper1CostPerSheet, analysis.Paper2CostPerSheet, 
                analysis.MediumCostPerSheet, analysis.TotalMaterialCostPerSheet);
        }

        private async Task<CostBreakdown> CalculateCostBreakdownAsync(BoxCalculatorRequest request, SheetAnalysis sheetAnalysis)
        {
            var breakdown = new CostBreakdown();

            // Material costs per box (detailed breakdown)
            breakdown.Paper1CostPerBox = sheetAnalysis.Paper1CostPerSheet / Math.Max(sheetAnalysis.TotalApps, 1);
            breakdown.Paper2CostPerBox = sheetAnalysis.Paper2CostPerSheet / Math.Max(sheetAnalysis.TotalApps, 1);
            breakdown.MediumCostPerBox = sheetAnalysis.MediumCostPerSheet / Math.Max(sheetAnalysis.TotalApps, 1);
            breakdown.TotalMaterialCostPerBox = breakdown.Paper1CostPerBox + breakdown.Paper2CostPerBox + breakdown.MediumCostPerBox;
            
            // Wastage cost - Apply board-specific wastage factor
            var boardConfig = BoardTypeConstants.BoardConfigurations[request.BoardType];
            var effectiveWastage = Math.Max(request.WastageFactorPercentage, boardConfig.WasteFactor);
            breakdown.WastageCost = breakdown.TotalMaterialCostPerBox * (effectiveWastage / 100);
            
            // Processing costs per box
            breakdown.PrintingCostPerBox = sheetAnalysis.TotalApps > 0 
                ? request.PrintingCostPerSheet / sheetAnalysis.TotalApps 
                : request.PrintingCostPerSheet;
            breakdown.DieCuttingCostPerBox = sheetAnalysis.TotalApps > 0 
                ? request.DieCuttingCostPerSheet / sheetAnalysis.TotalApps 
                : request.DieCuttingCostPerSheet;
            breakdown.LaborCostPerBox = request.LaborCostPerBox;
            breakdown.PinCostPerBox = request.PinCostPerBox;
            breakdown.TransportCostPerBox = request.TransportCostPerBox;

            // Calculate subtotal
            breakdown.SubtotalPerBox = breakdown.TotalMaterialCostPerBox + breakdown.WastageCost + 
                                     breakdown.PrintingCostPerBox + breakdown.DieCuttingCostPerBox + 
                                     breakdown.LaborCostPerBox + breakdown.PinCostPerBox + breakdown.TransportCostPerBox;

            // Add overhead
            breakdown.OverheadCostPerBox = breakdown.SubtotalPerBox * (request.OverheadPercentage / 100);
            breakdown.TotalCostPerBox = breakdown.SubtotalPerBox + breakdown.OverheadCostPerBox;

            // Add profit margin
            breakdown.ProfitPerBox = breakdown.TotalCostPerBox * (request.ProfitMarginPercentage / 100);
            breakdown.SellingPricePerBox = breakdown.TotalCostPerBox + breakdown.ProfitPerBox;

            // Add GST if applicable
            if (request.IncludeGST)
            {
                breakdown.GSTAmountPerBox = breakdown.SellingPricePerBox * (request.GSTRate / 100);
                breakdown.FinalPricePerBox = breakdown.SellingPricePerBox + breakdown.GSTAmountPerBox;
            }
            else
            {
                breakdown.GSTAmountPerBox = 0;
                breakdown.FinalPricePerBox = breakdown.SellingPricePerBox;
            }

            // Create detailed cost items for transparency
            breakdown.CostItems = new List<CostItem>();

            // Add material breakdown - ALL components now shown
            breakdown.CostItems.Add(new CostItem
            {
                Description = $"Top Liner ({request.Paper1GSM} GSM)",
                AmountPerBox = breakdown.Paper1CostPerBox,
                TotalAmount = breakdown.Paper1CostPerBox * request.Quantity,
                Category = "Material",
                IsUserEditable = true
            });

            breakdown.CostItems.Add(new CostItem
            {
                Description = $"Bottom Liner ({request.Paper2GSM} GSM)",
                AmountPerBox = breakdown.Paper2CostPerBox,
                TotalAmount = breakdown.Paper2CostPerBox * request.Quantity,
                Category = "Material",
                IsUserEditable = true
            });

            breakdown.CostItems.Add(new CostItem
            {
                Description = $"Medium - {request.FluteType} Flute ({request.MediumGSM} GSM)",
                AmountPerBox = breakdown.MediumCostPerBox,
                TotalAmount = breakdown.MediumCostPerBox * request.Quantity,
                Category = "Material",
                IsUserEditable = true
            });

            breakdown.CostItems.AddRange(new[]
            {
                new CostItem
                {
                    Description = $"Wastage ({effectiveWastage:F1}%)",
                    AmountPerBox = breakdown.WastageCost,
                    TotalAmount = breakdown.WastageCost * request.Quantity,
                    Category = "Material",
                    IsUserEditable = true
                },
                new CostItem
                {
                    Description = "Printing Cost",
                    AmountPerBox = breakdown.PrintingCostPerBox,
                    TotalAmount = breakdown.PrintingCostPerBox * request.Quantity,
                    Category = "Processing",
                    IsUserEditable = true
                },
                new CostItem
                {
                    Description = "Die Cutting Cost",
                    AmountPerBox = breakdown.DieCuttingCostPerBox,
                    TotalAmount = breakdown.DieCuttingCostPerBox * request.Quantity,
                    Category = "Processing",
                    IsUserEditable = true
                },
                new CostItem
                {
                    Description = "Labor Cost",
                    AmountPerBox = breakdown.LaborCostPerBox,
                    TotalAmount = breakdown.LaborCostPerBox * request.Quantity,
                    Category = "Processing",
                    IsUserEditable = true
                },
                new CostItem
                {
                    Description = "Pin Cost",
                    AmountPerBox = breakdown.PinCostPerBox,
                    TotalAmount = breakdown.PinCostPerBox * request.Quantity,
                    Category = "Processing",
                    IsUserEditable = true
                },
                new CostItem
                {
                    Description = "Transport Cost",
                    AmountPerBox = breakdown.TransportCostPerBox,
                    TotalAmount = breakdown.TransportCostPerBox * request.Quantity,
                    Category = "Processing",
                    IsUserEditable = true
                },
                new CostItem
                {
                    Description = $"Overhead ({request.OverheadPercentage:F1}%)",
                    AmountPerBox = breakdown.OverheadCostPerBox,
                    TotalAmount = breakdown.OverheadCostPerBox * request.Quantity,
                    Category = "Business",
                    IsUserEditable = true
                },
                new CostItem
                {
                    Description = $"Profit Margin ({request.ProfitMarginPercentage:F1}%)",
                    AmountPerBox = breakdown.ProfitPerBox,
                    TotalAmount = breakdown.ProfitPerBox * request.Quantity,
                    Category = "Business",
                    IsUserEditable = true
                }
            });

            if (request.IncludeGST)
            {
                breakdown.CostItems.Add(new CostItem
                {
                    Description = $"GST ({request.GSTRate:F1}%)",
                    AmountPerBox = breakdown.GSTAmountPerBox,
                    TotalAmount = breakdown.GSTAmountPerBox * request.Quantity,
                    Category = "Tax",
                    IsUserEditable = false
                });
            }

            return breakdown;
        }

        public async Task<List<CostItem>> GetEditableCostItemsAsync()
        {
            return new List<CostItem>
            {
                new CostItem { Description = "Top Liner Rate (Rs./kg)", AmountPerBox = 50.00m, IsUserEditable = true, Category = "Material" },
                new CostItem { Description = "Bottom Liner Rate (Rs./kg)", AmountPerBox = 45.00m, IsUserEditable = true, Category = "Material" },
                new CostItem { Description = "Medium Rate (Rs./kg)", AmountPerBox = 42.00m, IsUserEditable = true, Category = "Material" },
                new CostItem { Description = "Printing Cost (Rs./sheet)", AmountPerBox = 0m, IsUserEditable = true, Category = "Processing" }, // ✅ FIXED - Zero first
                new CostItem { Description = "Die Cutting Cost (Rs./sheet)", AmountPerBox = 0m, IsUserEditable = true, Category = "Processing" }, // ✅ FIXED - Zero first
                new CostItem { Description = "Labor Cost (Rs./box)", AmountPerBox = 0m, IsUserEditable = true, Category = "Processing" }, // ✅ FIXED - Zero first
                new CostItem { Description = "Pin Cost (Rs./box)", AmountPerBox = 0m, IsUserEditable = true, Category = "Processing" }, // ✅ FIXED - Zero first
                new CostItem { Description = "Transport Cost (Rs./box)", AmountPerBox = 0m, IsUserEditable = true, Category = "Processing" }, // ✅ FIXED - Zero first
                new CostItem { Description = "Overhead Percentage (%)", AmountPerBox = 0m, IsUserEditable = true, Category = "Business" }, // ✅ FIXED - Zero first
                new CostItem { Description = "Profit Margin (%)", AmountPerBox = 0m, IsUserEditable = true, Category = "Business" }, // ✅ FIXED - Zero first
                new CostItem { Description = "Wastage Factor (%)", AmountPerBox = 5.0m, IsUserEditable = true, Category = "Material" } // ✅ KEPT - Minimum industry standard
            };
        }

        public async Task UpdateCostItemAsync(string itemId, decimal newValue)
        {
            _logger.LogInformation("Cost item {ItemId} updated to {NewValue}", itemId, newValue);
            // In production, this would update database/cache
        }
    }
}