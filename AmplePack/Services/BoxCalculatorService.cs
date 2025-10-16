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
                
                // Validate required papers based on board type
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

                _logger.LogInformation("Box rate calculation completed: ?{FinalPrice} per box, {Apps} apps from sheet {SheetSize}", 
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

        private void ValidateRequiredPapers(BoxCalculatorRequest request, BoardConfiguration boardConfig)
        {
            switch (boardConfig.RequiredPapers)
            {
                case 2: // 3 Ply - Paper1 + Medium
                    if (request.Paper1GSM == 0)
                        throw new ArgumentException("Paper 1 GSM is required for 3 Ply boards");
                    if (request.MediumGSM == 0)
                        throw new ArgumentException("Medium GSM is required for 3 Ply boards");
                    break;

                case 3: // 5 Ply - Paper1 + Paper2 + Medium
                    if (request.Paper1GSM == 0)
                        throw new ArgumentException("Paper 1 GSM is required for 5 Ply boards");
                    if (request.Paper2GSM == 0)
                        throw new ArgumentException("Paper 2 GSM is required for 5 Ply boards");
                    if (request.MediumGSM == 0)
                        throw new ArgumentException("Medium GSM is required for 5 Ply boards");
                    break;

                case 4: // 7 Ply - All papers required
                    if (request.Paper1GSM == 0)
                        throw new ArgumentException("Paper 1 GSM is required for 7 Ply boards");
                    if (request.Paper2GSM == 0)
                        throw new ArgumentException("Paper 2 GSM is required for 7 Ply boards");
                    if (request.MediumGSM == 0)
                        throw new ArgumentException("Medium GSM is required for 7 Ply boards");
                    break;
            }
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

            _logger.LogDebug("Sheet Analysis: {TotalApps} apps, {Utilization:F1}% utilization, ?{CostPerBox:F2} per box",
                analysis.TotalApps, analysis.UtilizationPercentage, analysis.TotalCostPerBox);

            return analysis;
        }

        private async Task CalculateMaterialCostsPerSheetAsync(
            BoxCalculatorRequest request, 
            SheetAnalysis analysis, 
            BoardConfiguration boardConfig)
        {
            // Convert sheet area from square inches to square meters
            var sheetAreaSquareMeters = analysis.SheetArea / 1550m; // 1 m² = 1550 sq inches

            // Calculate Paper 1 cost (always required)
            if (request.Paper1GSM > 0)
            {
                var paper1WeightKg = (request.Paper1GSM * sheetAreaSquareMeters) / 1000m;
                analysis.Paper1CostPerSheet = paper1WeightKg * request.Paper1RatePerKg;
            }

            // Calculate Paper 2 cost (for 5-ply and above)
            if (request.Paper2GSM > 0)
            {
                var paper2WeightKg = (request.Paper2GSM * sheetAreaSquareMeters) / 1000m;
                analysis.Paper2CostPerSheet = paper2WeightKg * request.Paper2RatePerKg;
            }

            // Calculate Medium cost (always required) 
            // Medium layer count based on board type: 3-ply=1, 5-ply=2, 7-ply=3
            var mediumLayers = boardConfig.RequiredPapers switch
            {
                2 => 1, // 3 Ply
                3 => 2, // 5 Ply
                4 => 3, // 7 Ply
                _ => 1
            };

            var mediumWeightKg = (request.MediumGSM * sheetAreaSquareMeters * mediumLayers) / 1000m;
            analysis.MediumCostPerSheet = mediumWeightKg * request.MediumRatePerKg;

            // Total material cost per sheet
            analysis.TotalMaterialCostPerSheet = analysis.Paper1CostPerSheet + 
                                                analysis.Paper2CostPerSheet + 
                                                analysis.MediumCostPerSheet;

            _logger.LogDebug("Material costs per sheet: Paper1=?{Paper1:F2}, Paper2=?{Paper2:F2}, Medium=?{Medium:F2} (×{Layers}), Total=?{Total:F2}",
                analysis.Paper1CostPerSheet, analysis.Paper2CostPerSheet, analysis.MediumCostPerSheet, mediumLayers, analysis.TotalMaterialCostPerSheet);
        }

        private async Task<CostBreakdown> CalculateCostBreakdownAsync(BoxCalculatorRequest request, SheetAnalysis sheetAnalysis)
        {
            var breakdown = new CostBreakdown();

            // Material costs per box (detailed breakdown)
            breakdown.Paper1CostPerBox = sheetAnalysis.Paper1CostPerSheet / Math.Max(sheetAnalysis.TotalApps, 1);
            breakdown.Paper2CostPerBox = sheetAnalysis.Paper2CostPerSheet / Math.Max(sheetAnalysis.TotalApps, 1);
            breakdown.MediumCostPerBox = sheetAnalysis.MediumCostPerSheet / Math.Max(sheetAnalysis.TotalApps, 1);
            breakdown.TotalMaterialCostPerBox = breakdown.Paper1CostPerBox + breakdown.Paper2CostPerBox + breakdown.MediumCostPerBox;
            
            // Wastage cost
            breakdown.WastageCost = breakdown.TotalMaterialCostPerBox * (request.WastageFactorPercentage / 100);
            
            // Processing costs per box
            breakdown.PrintingCostPerBox = sheetAnalysis.TotalApps > 0 
                ? request.PrintingCostPerSheet / sheetAnalysis.TotalApps 
                : request.PrintingCostPerSheet;
            breakdown.DieCuttingCostPerBox = sheetAnalysis.TotalApps > 0 
                ? request.DieCuttingCostPerSheet / sheetAnalysis.TotalApps 
                : request.DieCuttingCostPerSheet;
            breakdown.LaborCostPerBox = request.LaborCostPerBox;

            // Calculate subtotal
            breakdown.SubtotalPerBox = breakdown.TotalMaterialCostPerBox + breakdown.WastageCost + 
                                     breakdown.PrintingCostPerBox + breakdown.DieCuttingCostPerBox + 
                                     breakdown.LaborCostPerBox;

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

            // Add material breakdown
            if (breakdown.Paper1CostPerBox > 0)
            {
                breakdown.CostItems.Add(new CostItem
                {
                    Description = $"Paper 1 - Outer Liner ({request.Paper1GSM} GSM)",
                    AmountPerBox = breakdown.Paper1CostPerBox,
                    TotalAmount = breakdown.Paper1CostPerBox * request.Quantity,
                    Category = "Material",
                    IsUserEditable = true
                });
            }

            if (breakdown.Paper2CostPerBox > 0)
            {
                breakdown.CostItems.Add(new CostItem
                {
                    Description = $"Paper 2 - Inner Liner ({request.Paper2GSM} GSM)",
                    AmountPerBox = breakdown.Paper2CostPerBox,
                    TotalAmount = breakdown.Paper2CostPerBox * request.Quantity,
                    Category = "Material",
                    IsUserEditable = true
                });
            }

            if (breakdown.MediumCostPerBox > 0)
            {
                breakdown.CostItems.Add(new CostItem
                {
                    Description = $"Medium - Corrugated Layer ({request.MediumGSM} GSM)",
                    AmountPerBox = breakdown.MediumCostPerBox,
                    TotalAmount = breakdown.MediumCostPerBox * request.Quantity,
                    Category = "Material",
                    IsUserEditable = true
                });
            }

            breakdown.CostItems.AddRange(new[]
            {
                new CostItem
                {
                    Description = $"Wastage ({request.WastageFactorPercentage:F1}%)",
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
                new CostItem { Description = "Paper 1 Rate (Rs./kg)", AmountPerBox = 45.00m, IsUserEditable = true, Category = "Material" },
                new CostItem { Description = "Paper 2 Rate (Rs./kg)", AmountPerBox = 45.00m, IsUserEditable = true, Category = "Material" },
                new CostItem { Description = "Medium Rate (Rs./kg)", AmountPerBox = 42.00m, IsUserEditable = true, Category = "Material" },
                new CostItem { Description = "Printing Cost (Rs./sheet)", AmountPerBox = 2.50m, IsUserEditable = true, Category = "Processing" },
                new CostItem { Description = "Die Cutting Cost (Rs./sheet)", AmountPerBox = 1.50m, IsUserEditable = true, Category = "Processing" },
                new CostItem { Description = "Labor Cost (Rs./box)", AmountPerBox = 0.50m, IsUserEditable = true, Category = "Processing" },
                new CostItem { Description = "Overhead Percentage (%)", AmountPerBox = 15.0m, IsUserEditable = true, Category = "Business" },
                new CostItem { Description = "Profit Margin (%)", AmountPerBox = 20.0m, IsUserEditable = true, Category = "Business" },
                new CostItem { Description = "Wastage Factor (%)", AmountPerBox = 10.0m, IsUserEditable = true, Category = "Material" }
            };
        }

        public async Task UpdateCostItemAsync(string itemId, decimal newValue)
        {
            _logger.LogInformation("Cost item {ItemId} updated to {NewValue}", itemId, newValue);
            // In production, this would update database/cache
        }
    }
}