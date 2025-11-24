using AmplePack.Models;
using AmplePack.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AmplePack.Controllers
{
    [Authorize]
    public class BoxCalculatorController : Controller
    {
        private readonly IBoxCalculatorService _calculatorService;
        private readonly ILogger<BoxCalculatorController> _logger;

        public BoxCalculatorController(IBoxCalculatorService calculatorService, ILogger<BoxCalculatorController> logger)
        {
            _calculatorService = calculatorService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var request = new BoxCalculatorRequest
            {
                // Box specifications (industry standard)
                Length = 12,
                Width = 10,
                Height = 8,
                BoardType = "3 Ply",
                Quantity = 1000,
                
                // Sheet size (industry standard for corrugated)
                SheetLength = 42,
                SheetWidth = 30,
                
                // Paper specifications for 3-ply corrugated box - REQUIRED VALUES ONLY
                Paper1GSM = 150,          // Top liner - REQUIRED
                Paper1RatePerKg = 50.00m, // Top liner rate - REQUIRED
                Paper2GSM = 125,          // Bottom liner - REQUIRED
                Paper2RatePerKg = 45.00m, // Bottom liner rate - REQUIRED  
                MediumGSM = 120,          // Medium - REQUIRED
                MediumRatePerKg = 42.00m, // Medium rate - REQUIRED
                
                // ALL OPTIONAL COSTS START WITH ZERO
                PrintingCostPerSheet = 0m,
                DieCuttingCostPerSheet = 0m,
                LaborCostPerBox = 0m,
                PinCostPerBox = 0m,
                TransportCostPerBox = 0m,
                
                // ALL BUSINESS PARAMETERS START WITH ZERO EXCEPT MINIMUM WASTAGE
                OverheadPercentage = 0m,
                ProfitMarginPercentage = 0m,  // ? FIXED - Consistent with frontend
                WastageFactorPercentage = 5.0m, // MINIMUM INDUSTRY STANDARD
                
                // GST Settings
                IncludeGST = false,  // START WITH NO GST
                GSTRate = 18.0m
            };

            ViewBag.BoardTypes = BoardTypeConstants.AvailableBoardTypes;
            ViewBag.GSMValues = BoardTypeConstants.StandardGSMValues;
            ViewBag.BoardConfigurations = BoardTypeConstants.BoardConfigurations;

            return View(request);
        }

        [HttpPost]
        public async Task<IActionResult> Calculate([FromBody] BoxCalculatorRequest request)
        {
            try
            {
                // Check if request is null
                if (request == null)
                {
                    return Json(new { 
                        success = false, 
                        message = "Invalid request data" 
                    });
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .Select(x => new { 
                            Field = x.Key, 
                            Errors = x.Value?.Errors.Select(e => e.ErrorMessage) 
                        });
                    
                    return Json(new { 
                        success = false, 
                        message = "Validation failed", 
                        errors = errors 
                    });
                }

                // Ensure required string properties are not null
                if (string.IsNullOrEmpty(request.BoardType))
                {
                    request.BoardType = "3 Ply"; // Default value
                }

                if (string.IsNullOrEmpty(request.FluteType))
                {
                    request.FluteType = "B"; // Default value
                }

                var result = await _calculatorService.CalculateBoxRateAsync(request);
                
                // Check if result is null
                if (result == null)
                {
                    return Json(new { 
                        success = false, 
                        message = "Calculation failed to produce results" 
                    });
                }
                
                return Json(new { 
                    success = true, 
                    result = result 
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error in box calculation");
                return Json(new { 
                    success = false, 
                    message = ex.Message 
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation in box calculation");
                return Json(new { 
                    success = false, 
                    message = ex.Message 
                });
            }
            catch (NullReferenceException ex)
            {
                _logger.LogError(ex, "Null reference error in box calculation");
                return Json(new { 
                    success = false, 
                    message = "Data validation error. Please ensure all required fields are filled." 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating box rate");
                return Json(new { 
                    success = false, 
                    message = "An error occurred while calculating the box rate. Please try again." 
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> LiveCalculate([FromBody] BoxCalculatorRequest request)
        {
            try
            {
                // Check if request is null
                if (request == null)
                {
                    return Json(new { success = false, message = "Invalid request data" });
                }

                // Basic validation for live preview - skip full model validation
                if (request.Length <= 0 || request.Width <= 0 || request.Height <= 0)
                {
                    return Json(new { success = false, message = "Invalid dimensions" });
                }

                if (request.Quantity <= 0)
                {
                    return Json(new { success = false, message = "Invalid quantity" });
                }

                // Ensure required string properties are not null
                if (string.IsNullOrEmpty(request.BoardType))
                {
                    request.BoardType = "3 Ply"; // Default value
                }

                if (string.IsNullOrEmpty(request.FluteType))
                {
                    request.FluteType = "B"; // Default value
                }

                // Perform calculation
                var result = await _calculatorService.CalculateBoxRateAsync(request);
                
                // Check if result is null
                if (result == null)
                {
                    return Json(new { success = false, message = "Calculation failed" });
                }

                // Check if SheetAnalysis is null
                if (result.SheetAnalysis == null)
                {
                    return Json(new { success = false, message = "Sheet analysis failed" });
                }

                // Check if CostBreakdown is null
                if (result.CostBreakdown == null)
                {
                    return Json(new { success = false, message = "Cost breakdown failed" });
                }

                // ? ENHANCED - Calculate Total GSM with accurate board-specific logic (Duplex removed)
                var boardConfig = BoardTypeConstants.BoardConfigurations[request.BoardType];
                var fluteFactor = BoardTypeConstants.FluteFactors.GetValueOrDefault(request.FluteType, 1.4m);
                
                // Calculate total GSM based on board type structure
                decimal totalGSM = 0m;
                switch (request.BoardType)
                {
                    case "3 Ply": // Single Wall: Top + Bottom + Medium
                        totalGSM = request.Paper1GSM + request.Paper2GSM + (request.MediumGSM * fluteFactor);
                        break;
                    case "5 Ply": // Double Wall: Top + Inner + Bottom + 2×Medium
                        totalGSM = request.Paper1GSM + (request.Paper1GSM * 0.85m) + request.Paper2GSM + (request.MediumGSM * fluteFactor * 2);
                        break;
                    case "7 Ply": // Triple Wall: Multiple liners + 3×Medium
                        totalGSM = request.Paper1GSM + (request.Paper1GSM * 0.9m) + (request.Paper2GSM * 0.85m) + request.Paper2GSM + (request.MediumGSM * fluteFactor * 3);
                        break;
                    default:
                        totalGSM = request.Paper1GSM + request.Paper2GSM + (request.MediumGSM * fluteFactor);
                        break;
                }
                
                // Calculate sheet weight
                var sheetAreaSqM = (result.SheetAnalysis.SheetArea / 1550m);
                var sheetWeight = (sheetAreaSqM * totalGSM) / 1000m;
                
                // Calculate sheets needed
                var sheetsNeeded = (int)Math.Ceiling(request.Quantity / (decimal)Math.Max(result.SheetAnalysis.TotalApps, 1));
                
                // Detailed breakdown with per-sheet and per-box costs
                return Json(new
                {
                    success = true,
                    livePricePerBox = result.FinalPricePerBoxWithGST,
                    apps = result.SheetAnalysis.TotalApps,
                    efficiency = result.MaterialEfficiency,
                    
                    // Material costs (per box)
                    materialCost = result.CostBreakdown.TotalMaterialCostPerBox,
                    paper1CostPerBox = result.CostBreakdown.Paper1CostPerBox,
                    paper2CostPerBox = result.CostBreakdown.Paper2CostPerBox,
                    mediumCostPerBox = result.CostBreakdown.MediumCostPerBox,
                    
                    // Processing costs (show unit as entered)
                    printingCostPerSheet = request.PrintingCostPerSheet,
                    printingCostPerBox = result.CostBreakdown.PrintingCostPerBox,
                    dieCuttingCostPerSheet = request.DieCuttingCostPerSheet,
                    dieCuttingCostPerBox = result.CostBreakdown.DieCuttingCostPerBox,
                    
                    processingCost = result.CostBreakdown.PrintingCostPerBox + result.CostBreakdown.DieCuttingCostPerBox,
                    
                    // Labor costs (per box)
                    laborCostPerBox = result.CostBreakdown.LaborCostPerBox,
                    pinCostPerBox = result.CostBreakdown.PinCostPerBox,
                    transportCostPerBox = result.CostBreakdown.TransportCostPerBox,
                    laborCost = result.CostBreakdown.LaborCostPerBox + result.CostBreakdown.PinCostPerBox + result.CostBreakdown.TransportCostPerBox,
                    
                    // Business costs
                    wastageCost = result.CostBreakdown.WastageCost,
                    overheadCost = result.CostBreakdown.OverheadCostPerBox,
                    profitCost = result.CostBreakdown.ProfitPerBox,
                    businessCost = result.CostBreakdown.OverheadCostPerBox + result.CostBreakdown.ProfitPerBox,
                    
                    // GST
                    gstAmount = result.CostBreakdown.GSTAmountPerBox,
                    
                    orderValue = result.TotalOrderValueWithGST,
                    sheetSize = $"{result.SheetAnalysis.SheetLength}\" × {result.SheetAnalysis.SheetWidth}\"",
                    boxLayout = $"{result.SheetAnalysis.BoxLayoutLength:F1}\" × {result.SheetAnalysis.BoxLayoutWidth:F1}\"",
                    totalGSM = (int)Math.Round(totalGSM),
                    sheetWeight = sheetWeight.ToString("F3"),
                    sheetsNeeded = sheetsNeeded,
                    
                    // Board configuration info
                    boardType = request.BoardType ?? "3 Ply",
                    fluteType = request.FluteType ?? "B",
                    isDuplex = boardConfig.IsDuplex
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogDebug(ex, "Live calculation validation warning");
                return Json(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogDebug(ex, "Live calculation operation warning");
                return Json(new { success = false, message = ex.Message });
            }
            catch (NullReferenceException ex)
            {
                _logger.LogError(ex, "Null reference error in live calculation");
                return Json(new { success = false, message = "Data validation error. Please check all required fields." });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Live calculation error");
                return Json(new { success = false, message = "Preview unavailable" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GetSheetLayoutVisualization([FromBody] BoxCalculatorRequest request)
        {
            try
            {
                var result = await _calculatorService.CalculateBoxRateAsync(request);
                
                var visualization = new
                {
                    sheetAnalysis = new
                    {
                        sheetLength = result.SheetAnalysis.SheetLength,
                        sheetWidth = result.SheetAnalysis.SheetWidth,
                        sheetArea = result.SheetAnalysis.SheetArea,
                        appsLength = result.SheetAnalysis.AppsLength,
                        appsWidth = result.SheetAnalysis.AppsWidth,
                        totalApps = result.SheetAnalysis.TotalApps,
                        utilization = result.SheetAnalysis.UtilizationPercentage,
                        boxLayoutLength = result.SheetAnalysis.BoxLayoutLength,
                        boxLayoutWidth = result.SheetAnalysis.BoxLayoutWidth,
                        usedArea = result.SheetAnalysis.UsedArea,
                        wasteArea = result.SheetAnalysis.WasteArea,
                        materialCostPerBox = result.SheetAnalysis.MaterialCostPerBox,
                        processingCostPerBox = result.SheetAnalysis.ProcessingCostPerBox,
                        totalCostPerBox = result.SheetAnalysis.TotalCostPerBox
                    },
                    costBreakdown = new
                    {
                        paper1Cost = result.CostBreakdown.Paper1CostPerBox,
                        paper2Cost = result.CostBreakdown.Paper2CostPerBox,
                        mediumCost = result.CostBreakdown.MediumCostPerBox,
                        totalMaterialCost = result.CostBreakdown.TotalMaterialCostPerBox,
                        wastageCost = result.CostBreakdown.WastageCost
                    }
                };

                return Json(new { 
                    success = true, 
                    visualization = visualization 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating sheet layout visualization");
                return Json(new { 
                    success = false, 
                    message = "An error occurred while generating the visualization." 
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetEditableCosts()
        {
            try
            {
                var costs = await _calculatorService.GetEditableCostItemsAsync();
                return Json(new { 
                    success = true, 
                    costs = costs 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving editable costs");
                return Json(new { 
                    success = false, 
                    message = "An error occurred while retrieving cost data." 
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCost([FromBody] UpdateCostRequest request)
        {
            try
            {
                await _calculatorService.UpdateCostItemAsync(request.ItemId, request.NewValue);
                return Json(new { 
                    success = true, 
                    message = "Cost updated successfully." 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cost item {ItemId} to {NewValue}", request.ItemId, request.NewValue);
                return Json(new { 
                    success = false, 
                    message = "An error occurred while updating the cost." 
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetBoardConfiguration(string boardType)
        {
            try
            {
                if (BoardTypeConstants.BoardConfigurations.TryGetValue(boardType, out var config))
                {
                    return Json(new { 
                        success = true, 
                        configuration = config 
                    });
                }
                
                return Json(new { 
                    success = false, 
                    message = "Invalid board type" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving board configuration for {BoardType}", boardType);
                return Json(new { 
                    success = false, 
                    message = "An error occurred while retrieving board configuration." 
                });
            }
        }

        [HttpGet]
        public IActionResult Help()
        {
            ViewBag.BoardConfigurations = BoardTypeConstants.BoardConfigurations;
            return View();
        }
    }

    public class UpdateCostRequest
    {
        public string ItemId { get; set; } = "";
        public decimal NewValue { get; set; }
    }
}
