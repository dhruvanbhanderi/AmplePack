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
                _logger.LogInformation("Starting box rate calculation for BoardType: {BoardType}, Apps: {Apps}, Qty: {Quantity}", 
                    request.BoardType, request.AppsPerSheet, request.Quantity);

                if (!BoardTypeConstants.BoardConfigurations.ContainsKey(request.BoardType))
                {
                    throw new ArgumentException($"Invalid board type: {request.BoardType}");
                }

                var boardConfig = BoardTypeConstants.BoardConfigurations[request.BoardType];
                
                ValidateRequiredPapers(request, boardConfig);

                var result = new BoxCalculatorResult
                {
                    BoardType = request.BoardType,
                    Quantity = request.Quantity,
                    AppsPerSheet = request.AppsPerSheet
                };

                // ✅ SIMPLIFIED - Calculate single sheet analysis with manual apps
                result.SheetAnalysis = await CalculateSheetAnalysisAsync(request, boardConfig);

                result.CostBreakdown = await CalculateCostBreakdownAsync(request, result.SheetAnalysis);

                result.FinalPricePerBox = result.CostBreakdown.SellingPricePerBox;
                result.FinalPricePerBoxWithGST = request.IncludeGST 
                    ? result.FinalPricePerBox + result.CostBreakdown.GSTAmountPerBox
                    : result.FinalPricePerBox;

                // ✅ FIXED - Overflow protection for large order calculations
                try
                {
                    checked
                    {
                        result.TotalOrderValue = result.FinalPricePerBox * request.Quantity;
                        result.TotalOrderValueWithGST = result.FinalPricePerBoxWithGST * request.Quantity;
                    }
                }
                catch (OverflowException)
                {
                    _logger.LogWarning("Overflow detected in order total calculation for Quantity: {Quantity}, Price: {Price}", 
                        request.Quantity, result.FinalPricePerBoxWithGST);
                    throw new InvalidOperationException("Order total exceeds maximum calculable value. Please reduce quantity or check pricing.");
                }

                // ✅ SIMPLIFIED - Efficiency metrics removed (no box layout calculation)
                result.MaterialEfficiency = 100m; // Not applicable with manual apps
                result.WastagePercentage = 0m; // ✅ CHANGED - No wastage calculated
                result.ProfitPerBox = result.CostBreakdown.ProfitPerBox;

                _logger.LogInformation("Box rate calculation completed: ₹{FinalPrice} per box, {Apps} apps per sheet", 
                    result.FinalPricePerBoxWithGST, result.SheetAnalysis.TotalApps);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating box rate for {BoardType}", request.BoardType);
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

            // ✅ SIMPLIFIED - Use manual apps directly (no calculation needed)
            analysis.TotalApps = request.AppsPerSheet;

            _logger.LogDebug("Using manual apps: {TotalApps} per sheet", analysis.TotalApps);

            // ✅ REMOVED - Box layout and utilization calculations (not needed with manual apps)

            // Calculate material costs per sheet
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
                throw new InvalidOperationException("Apps per sheet must be greater than 0");
            }

            _logger.LogDebug("Sheet Analysis: {TotalApps} apps, ₹{CostPerBox:F2} per box",
                analysis.TotalApps, analysis.TotalCostPerBox);

            return analysis;
        }

        // ✅ ENHANCED - Industry-accurate material cost calculation with proper duplex logic
        private async Task CalculateMaterialCostsPerSheetAsync(
            BoxCalculatorRequest request, 
            SheetAnalysis analysis, 
            BoardConfiguration boardConfig)
        {
            // Convert sheet area from square inches to square meters using constant
            var sheetAreaSquareMeters = analysis.SheetArea / CalculationConstants.SquareInchesPerSquareMeter;

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
                        var innerLinerWeight = (request.Paper1GSM * CalculationConstants.FivePlyInnerLinerRatio * sheetAreaSquareMeters) / 1000m;
                        var bottomLinerWeight = (request.Paper2GSM * sheetAreaSquareMeters) / 1000m;
                        
                        // Two medium layers with flute factor
                        var mediumWeight1 = (request.MediumGSM * fluteFactor * sheetAreaSquareMeters) / 1000m;
                        var mediumWeight2 = (request.MediumGSM * fluteFactor * CalculationConstants.FivePlySecondMediumRatio * sheetAreaSquareMeters) / 1000m;

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
                        var innerLiner1Weight = (request.Paper1GSM * CalculationConstants.SevenPlyInnerLiner1Ratio * sheetAreaSquareMeters) / 1000m;
                        var innerLiner2Weight = (request.Paper2GSM * CalculationConstants.SevenPlyInnerLiner2Ratio * sheetAreaSquareMeters) / 1000m;
                        var bottomLinerWeight = (request.Paper2GSM * sheetAreaSquareMeters) / 1000m;

                        // Three medium layers with progressive flute factors
                        var mediumWeight1 = (request.MediumGSM * fluteFactor * sheetAreaSquareMeters) / 1000m;
                        var mediumWeight2 = (request.MediumGSM * fluteFactor * CalculationConstants.SevenPlySecondMediumRatio * sheetAreaSquareMeters) / 1000m;
                        var mediumWeight3 = (request.MediumGSM * fluteFactor * CalculationConstants.SevenPlyThirdMediumRatio * sheetAreaSquareMeters) / 1000m;

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

            // Material costs per box (detailed breakdown) - ✅ ADDED proper rounding
            breakdown.Paper1CostPerBox = Math.Round(
                sheetAnalysis.Paper1CostPerSheet / Math.Max(sheetAnalysis.TotalApps, 1), 
                CalculationConstants.PricePrecisionDigits, 
                MidpointRounding.AwayFromZero);
            breakdown.Paper2CostPerBox = Math.Round(
                sheetAnalysis.Paper2CostPerSheet / Math.Max(sheetAnalysis.TotalApps, 1), 
                CalculationConstants.PricePrecisionDigits, 
                MidpointRounding.AwayFromZero);
            breakdown.MediumCostPerBox = Math.Round(
                sheetAnalysis.MediumCostPerSheet / Math.Max(sheetAnalysis.TotalApps, 1), 
                CalculationConstants.PricePrecisionDigits, 
                MidpointRounding.AwayFromZero);
            breakdown.TotalMaterialCostPerBox = breakdown.Paper1CostPerBox + breakdown.Paper2CostPerBox + breakdown.MediumCostPerBox;
            
            // Processing costs per box - ✅ ADDED proper rounding
            breakdown.PrintingCostPerBox = Math.Round(
                sheetAnalysis.TotalApps > 0 
                    ? request.PrintingCostPerSheet / sheetAnalysis.TotalApps 
                    : request.PrintingCostPerSheet,
                CalculationConstants.PricePrecisionDigits,
                MidpointRounding.AwayFromZero);
            breakdown.DieCuttingCostPerBox = Math.Round(
                sheetAnalysis.TotalApps > 0 
                    ? request.DieCuttingCostPerSheet / sheetAnalysis.TotalApps 
                    : request.DieCuttingCostPerSheet,
                CalculationConstants.PricePrecisionDigits,
                MidpointRounding.AwayFromZero);
            breakdown.LaborCostPerBox = request.LaborCostPerBox;
            breakdown.PinCostPerBox = request.PinCostPerBox;
            breakdown.LaminationCostPerBox = request.LaminationCostPerBox;
            breakdown.TransportCostPerBox = request.TransportCostPerBox;

            // Calculate subtotal - WITHOUT wastage - ✅ ADDED proper rounding
            breakdown.SubtotalPerBox = Math.Round(
                breakdown.TotalMaterialCostPerBox + 
                breakdown.PrintingCostPerBox + breakdown.DieCuttingCostPerBox + 
                breakdown.LaborCostPerBox + breakdown.PinCostPerBox + 
                breakdown.LaminationCostPerBox + breakdown.TransportCostPerBox,
                CalculationConstants.PricePrecisionDigits,
                MidpointRounding.AwayFromZero);

            // ✅ REMOVED - Overhead calculation
            breakdown.TotalCostPerBox = breakdown.SubtotalPerBox; // No overhead added

            // Add profit margin (on total cost) - ✅ ADDED proper rounding
            breakdown.ProfitPerBox = Math.Round(
                breakdown.TotalCostPerBox * (request.ProfitMarginPercentage / 100),
                CalculationConstants.PricePrecisionDigits,
                MidpointRounding.AwayFromZero);
            breakdown.SellingPricePerBox = Math.Round(
                breakdown.TotalCostPerBox + breakdown.ProfitPerBox,
                CalculationConstants.PricePrecisionDigits,
                MidpointRounding.AwayFromZero);

            // Add GST if applicable - ✅ ADDED proper rounding
            if (request.IncludeGST)
            {
                breakdown.GSTAmountPerBox = Math.Round(
                    breakdown.SellingPricePerBox * (request.GSTRate / 100),
                    CalculationConstants.PricePrecisionDigits,
                    MidpointRounding.AwayFromZero);
                breakdown.FinalPricePerBox = Math.Round(
                    breakdown.SellingPricePerBox + breakdown.GSTAmountPerBox,
                    CalculationConstants.PricePrecisionDigits,
                    MidpointRounding.AwayFromZero);
            }
            else
            {
                breakdown.GSTAmountPerBox = 0;
                breakdown.FinalPricePerBox = breakdown.SellingPricePerBox;
            }

            // Create detailed cost items for transparency
            breakdown.CostItems = new List<CostItem>();

            // Add material breakdown
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
                    Description = "Lamination Cost",
                    AmountPerBox = breakdown.LaminationCostPerBox,
                    TotalAmount = breakdown.LaminationCostPerBox * request.Quantity,
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
                new CostItem { Description = "Lamination Cost (Rs./box)", AmountPerBox = 0m, IsUserEditable = true, Category = "Processing" },
                new CostItem { Description = "Transport Cost (Rs./box)", AmountPerBox = 0m, IsUserEditable = true, Category = "Processing" }, // ✅ FIXED - Zero first
                // ✅ REMOVED - Overhead and Wastage items
                new CostItem { Description = "Profit Margin (%)", AmountPerBox = 0m, IsUserEditable = true, Category = "Business" }
            };
        }

        public async Task UpdateCostItemAsync(string itemId, decimal newValue)
        {
            _logger.LogInformation("Cost item {ItemId} updated to {NewValue}", itemId, newValue);
            // In production, this would update database/cache
            await Task.CompletedTask;
        }
    }
}