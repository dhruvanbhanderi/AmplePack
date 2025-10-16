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
                Length = 10,
                Width = 8,
                Height = 6,
                BoardType = "3 Ply",
                Quantity = 1000,
                
                // Single sheet size (industry standard)
                SheetLength = 40,
                SheetWidth = 30,
                
                // Paper specifications for 3-ply (default)
                Paper1GSM = 150,          // Outer liner
                Paper1RatePerKg = 45.00m,
                Paper2GSM = 0,            // Not required for 3-ply
                Paper2RatePerKg = 0,
                MediumGSM = 120,          // Corrugated medium
                MediumRatePerKg = 42.00m,
                
                // Processing costs
                PrintingCostPerSheet = 2.50m,
                DieCuttingCostPerSheet = 1.50m,
                LaborCostPerBox = 0.50m,
                
                // Business parameters
                OverheadPercentage = 15.0m,
                ProfitMarginPercentage = 20.0m,
                WastageFactorPercentage = 10.0m,
                IncludeGST = true,
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

                var result = await _calculatorService.CalculateBoxRateAsync(request);
                
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
