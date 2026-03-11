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
                // ? SIMPLIFIED - Only essential fields
                AppsPerSheet = 1, // ? CHANGED - Default 1 (minimum)
                BoardType = "3 Ply",
                Quantity = 1000,
                
                // Sheet size (industry standard for corrugated)
                SheetLength = 42,
                SheetWidth = 30,
                
                // Paper specifications for 3-ply corrugated box - REQUIRED VALUES ONLY
                Paper1GSM = 150,
                Paper1RatePerKg = 50.00m,
                Paper2GSM = 125,
                Paper2RatePerKg = 45.00m,
                MediumGSM = 120,
                MediumRatePerKg = 42.00m,
                
                // ALL OPTIONAL COSTS START WITH ZERO
                PrintingCostPerSheet = 0m,
                DieCuttingCostPerSheet = 0m,
                LaborCostPerBox = 0m,
                PinCostPerBox = 0m,
                LaminationCostPerBox = 0m,
                TransportCostPerBox = 0m,
                
                // ? REMOVED - OverheadPercentage and WastageFactorPercentage
                // Only profit margin remains
                ProfitMarginPercentage = 0m,
                
                // GST Settings
                IncludeGST = false,
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
                    message = $"Validation Error: {ex.Message}",
                    errorType = "validation"
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation in box calculation");
                return Json(new { 
                    success = false, 
                    message = $"Operation Error: {ex.Message}",
                    errorType = "operation"
                });
            }
            catch (NullReferenceException ex)
            {
                _logger.LogError(ex, "Null reference error in box calculation");
                return Json(new { 
                    success = false, 
                    message = "Data validation error. Please ensure all required fields are filled correctly.",
                    errorType = "data"
                });
            }
            catch (OverflowException ex)
            {
                _logger.LogError(ex, "Overflow in box calculation");
                return Json(new {
                    success = false,
                    message = "Calculation overflow: The order total is too large. Please reduce quantity or check pricing values.",
                    errorType = "overflow"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating box rate");
                return Json(new { 
                    success = false, 
                    message = "An unexpected error occurred during calculation. Please check your input values and try again.",
                    errorType = "general"
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

                // ? SIMPLIFIED - Basic validation for manual apps
                if (request.AppsPerSheet <= 0)
                {
                    return Json(new { success = false, message = "Invalid apps per sheet" });
                }

                if (request.Quantity <= 0)
                {
                    return Json(new { success = false, message = "Invalid quantity" });
                }

                // Ensure required string properties are not null
                if (string.IsNullOrEmpty(request.BoardType))
                {
                    request.BoardType = "3 Ply";
                }

                if (string.IsNullOrEmpty(request.FluteType))
                {
                    request.FluteType = "B";
                }

                // Perform calculation
                var result = await _calculatorService.CalculateBoxRateAsync(request);
                
                if (result == null)
                {
                    return Json(new { success = false, message = "Calculation failed" });
                }

                if (result.SheetAnalysis == null)
                {
                    return Json(new { success = false, message = "Sheet analysis failed" });
                }

                if (result.CostBreakdown == null)
                {
                    return Json(new { success = false, message = "Cost breakdown failed" });
                }

                // ? FIXED - Safe dictionary access with TryGetValue
                if (!BoardTypeConstants.BoardConfigurations.TryGetValue(request.BoardType, out var boardConfig))
                {
                    return Json(new { success = false, message = "Invalid board type configuration" });
                }
                
                // Get flute factor
                var fluteFactor = BoardTypeConstants.FluteFactors.GetValueOrDefault(request.FluteType, 1.4m);
                
                decimal totalGSM = 0m;
                switch (request.BoardType)
                {
                    case "3 Ply": // Single Wall
                        totalGSM = request.Paper1GSM + request.Paper2GSM + (request.MediumGSM * fluteFactor);
                        break;
                        
                    case "5 Ply": // Double Wall - CORRECTED to match service logic
                        {
                            var topLiner = request.Paper1GSM;
                            var innerLiner = request.Paper1GSM * CalculationConstants.FivePlyInnerLinerRatio;
                            var bottomLiner = request.Paper2GSM;
                            var medium1 = request.MediumGSM * fluteFactor;
                            var medium2 = request.MediumGSM * fluteFactor * CalculationConstants.FivePlySecondMediumRatio;
                            
                            totalGSM = topLiner + innerLiner + bottomLiner + medium1 + medium2;
                            break;
                        }
                        
                    case "7 Ply": // Triple Wall - CORRECTED to match service logic
                        {
                            var topLiner = request.Paper1GSM;
                            var innerLiner1 = request.Paper1GSM * CalculationConstants.SevenPlyInnerLiner1Ratio;
                            var innerLiner2 = request.Paper2GSM * CalculationConstants.SevenPlyInnerLiner2Ratio;
                            var bottomLiner = request.Paper2GSM;
                            var medium1 = request.MediumGSM * fluteFactor;
                            var medium2 = request.MediumGSM * fluteFactor * CalculationConstants.SevenPlySecondMediumRatio;
                            var medium3 = request.MediumGSM * fluteFactor * CalculationConstants.SevenPlyThirdMediumRatio;
                            
                            totalGSM = topLiner + innerLiner1 + innerLiner2 + bottomLiner + medium1 + medium2 + medium3;
                            break;
                        }
                        
                    default:
                        totalGSM = request.Paper1GSM + request.Paper2GSM + (request.MediumGSM * fluteFactor);
                        break;
                }
                
                var sheetAreaSqM = (result.SheetAnalysis.SheetArea / CalculationConstants.SquareInchesPerSquareMeter);
                var sheetWeight = (sheetAreaSqM * totalGSM) / 1000m;
                var sheetsNeeded = (int)Math.Ceiling(request.Quantity / (decimal)Math.Max(result.SheetAnalysis.TotalApps, 1));
                
                return Json(new
                {
                    success = true,
                    livePricePerBox = result.FinalPricePerBoxWithGST,
                    apps = result.SheetAnalysis.TotalApps,
                    
                    // Material costs (per box)
                    materialCost = result.CostBreakdown.TotalMaterialCostPerBox,
                    paper1CostPerBox = result.CostBreakdown.Paper1CostPerBox,
                    paper2CostPerBox = result.CostBreakdown.Paper2CostPerBox,
                    mediumCostPerBox = result.CostBreakdown.MediumCostPerBox,
                    
                    // Processing costs
                    printingCostPerSheet = request.PrintingCostPerSheet,
                    printingCostPerBox = result.CostBreakdown.PrintingCostPerBox,
                    dieCuttingCostPerSheet = request.DieCuttingCostPerSheet,
                    dieCuttingCostPerBox = result.CostBreakdown.DieCuttingCostPerBox,
                    
                    processingCost = result.CostBreakdown.PrintingCostPerBox + result.CostBreakdown.DieCuttingCostPerBox,
                    
                    // Labor costs (per box)
                    laborCostPerBox = result.CostBreakdown.LaborCostPerBox,
                    pinCostPerBox = result.CostBreakdown.PinCostPerBox,
                    laminationCostPerBox = result.CostBreakdown.LaminationCostPerBox,
                    transportCostPerBox = result.CostBreakdown.TransportCostPerBox,
                    laborCost = result.CostBreakdown.LaborCostPerBox + result.CostBreakdown.PinCostPerBox + result.CostBreakdown.LaminationCostPerBox + result.CostBreakdown.TransportCostPerBox,
                    
                    // Business costs
                    profitCost = result.CostBreakdown.ProfitPerBox,
                    businessCost = result.CostBreakdown.ProfitPerBox, // ? CHANGED - Only profit, no overhead/wastage
                    
                    // GST
                    gstAmount = result.CostBreakdown.GSTAmountPerBox,
                    
                    orderValue = result.TotalOrderValueWithGST,
                    sheetSize = $"{result.SheetAnalysis.SheetLength}\" × {result.SheetAnalysis.SheetWidth}\"",
                    totalGSM = (int)Math.Round(totalGSM),
                    sheetWeight = sheetWeight.ToString("F3"),
                    sheetsNeeded = sheetsNeeded,
                    
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
                
                // ? SIMPLIFIED - Remove references to removed properties
                var visualization = new
                {
                    sheetAnalysis = new
                    {
                        sheetLength = result.SheetAnalysis.SheetLength,
                        sheetWidth = result.SheetAnalysis.SheetWidth,
                        sheetArea = result.SheetAnalysis.SheetArea,
                        totalApps = result.SheetAnalysis.TotalApps,
                        materialCostPerBox = result.SheetAnalysis.MaterialCostPerBox,
                        processingCostPerBox = result.SheetAnalysis.ProcessingCostPerBox,
                        totalCostPerBox = result.SheetAnalysis.TotalCostPerBox
                    },
                    costBreakdown = new
                    {
                        paper1Cost = result.CostBreakdown.Paper1CostPerBox,
                        paper2Cost = result.CostBreakdown.Paper2CostPerBox,
                        mediumCost = result.CostBreakdown.MediumCostPerBox,
                        totalMaterialCost = result.CostBreakdown.TotalMaterialCostPerBox
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
