using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AmplePack.Models;
using AmplePack.Services;

namespace AmplePack.Controllers
{
    /// <summary>
    /// Controller for corrugated box price calculation
    /// Provides interface for box pricing with detailed cost breakdown
    /// </summary>
    [Authorize]
    public class BoxCalculatorController : Controller
    {
        private readonly BoxPriceCalculatorService _calculatorService;

        public BoxCalculatorController(BoxPriceCalculatorService calculatorService)
        {
            _calculatorService = calculatorService;
        }

        /// <summary>
        /// GET: Display the box calculator form
        /// </summary>
        public IActionResult Index()
        {
            var model = new BoxCalculatorInput();
            
            // Set default values from standard rates
            var standardRates = MaterialRates.GetStandardRates().FirstOrDefault();
            if (standardRates != null)
            {
                model.BoardRatePerSqM = standardRates.BoardRatePerSqM;
                model.PrintingCostPerSqM = standardRates.PrintingRatePerSqM;
                model.DieCuttingCostPerSqM = standardRates.DieCuttingRatePerSqM;
                model.LaborCostPerBox = standardRates.LaborRatePerBox;
            }

            // Populate ViewBag data for dropdowns
            ViewBag.BoardTypes = BoardSpecs.GetStandardBoardTypes();
            ViewBag.MaterialRates = MaterialRates.GetStandardRates();
            ViewBag.SheetSizes = SheetSpecs.GetStandardSheetSizes();

            return View(model);
        }

        /// <summary>
        /// POST: Calculate box price based on input parameters
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Index(BoxCalculatorInput input)
        {
            // Populate ViewBag data for dropdowns (needed for form re-rendering)
            ViewBag.BoardTypes = BoardSpecs.GetStandardBoardTypes();
            ViewBag.MaterialRates = MaterialRates.GetStandardRates();
            ViewBag.SheetSizes = SheetSpecs.GetStandardSheetSizes();

            if (!ModelState.IsValid)
            {
                return View(input);
            }

            try
            {
                // Calculate box price
                var result = _calculatorService.CalculateBoxPrice(input);

                // Get sheet layout suggestions
                var layoutSuggestions = _calculatorService.GetSheetLayoutSuggestions(input);
                ViewBag.LayoutSuggestions = layoutSuggestions;

                // Pass result to view
                ViewBag.Result = result;
                ViewBag.HasResult = true;

                // Success message
                TempData["SuccessMessage"] = "Box price calculated successfully!";

                return View(input);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Calculation error: {ex.Message}");
                return View(input);
            }
        }

        /// <summary>
        /// API: Get board specifications for a board type
        /// </summary>
        [HttpGet]
        public IActionResult GetBoardSpecs(string boardType)
        {
            var boardSpecs = BoardSpecs.GetStandardBoardTypes()
                .FirstOrDefault(b => b.BoardType == boardType);

            if (boardSpecs == null)
            {
                return Json(new { success = false, message = "Board type not found" });
            }

            return Json(new
            {
                success = true,
                boardSpecs = new
                {
                    boardType = boardSpecs.BoardType,
                    defaultCompressionRatio = boardSpecs.DefaultCompressionRatio,
                    defaultGSM = boardSpecs.DefaultGSM,
                    description = boardSpecs.Description
                }
            });
        }

        /// <summary>
        /// API: Get material rates for a rate category
        /// </summary>
        [HttpGet]
        public IActionResult GetMaterialRates(string rateCategory)
        {
            var rates = MaterialRates.GetStandardRates()
                .FirstOrDefault(r => r.RateCategory == rateCategory);

            if (rates == null)
            {
                return Json(new { success = false, message = "Rate category not found" });
            }

            return Json(new
            {
                success = true,
                rates = new
                {
                    rateCategory = rates.RateCategory,
                    boardRatePerSqM = rates.BoardRatePerSqM,
                    printingRatePerSqM = rates.PrintingRatePerSqM,
                    dieCuttingRatePerSqM = rates.DieCuttingRatePerSqM,
                    laborRatePerBox = rates.LaborRatePerBox,
                    region = rates.Region
                }
            });
        }

        /// <summary>
        /// API: Get sheet specifications for a sheet size
        /// </summary>
        [HttpGet]
        public IActionResult GetSheetSpecs(string sheetSize)
        {
            var sheetSpecs = SheetSpecs.GetStandardSheetSizes()
                .FirstOrDefault(s => s.SheetSize == sheetSize);

            if (sheetSpecs == null)
            {
                return Json(new { success = false, message = "Sheet size not found" });
            }

            return Json(new
            {
                success = true,
                sheetSpecs = new
                {
                    sheetSize = sheetSpecs.SheetSize,
                    lengthMm = sheetSpecs.LengthMm,
                    widthMm = sheetSpecs.WidthMm,
                    description = sheetSpecs.Description,
                    isStandard = sheetSpecs.IsStandard
                }
            });
        }

        /// <summary>
        /// API: Quick calculation endpoint for AJAX requests
        /// </summary>
        [HttpPost]
        public IActionResult QuickCalculate([FromBody] BoxCalculatorInput input)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return Json(new { success = false, errors = errors });
            }

            try
            {
                var result = _calculatorService.CalculateBoxPrice(input);

                return Json(new
                {
                    success = true,
                    result = new
                    {
                        finalPricePerBoxIncGST = result.FinalPricePerBoxIncGST,
                        grandTotal = result.GrandTotal,
                        efficiency = result.EfficiencyPercentage,
                        waste = result.WastePercentage,
                        blanksPerSheet = result.BlanksPerSheet,
                        sheetsRequired = result.SheetsRequired,
                        warnings = result.Warnings
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Export calculation results to PDF
        /// </summary>
        [HttpPost]
        public IActionResult ExportToPdf(BoxCalculatorInput input)
        {
            try
            {
                var result = _calculatorService.CalculateBoxPrice(input);

                // For now, return the result as JSON until PDF library is properly configured
                return Json(new
                {
                    success = true,
                    message = "PDF export functionality will be available soon!",
                    result = new
                    {
                        input = input,
                        calculation = result,
                        summary = new
                        {
                            finalPricePerBox = result.FinalPricePerBoxIncGST,
                            grandTotal = result.GrandTotal,
                            efficiency = result.EfficiencyPercentage,
                            sheetsRequired = result.SheetsRequired
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Get optimization suggestions for better pricing
        /// </summary>
        [HttpPost]
        public IActionResult GetOptimizationSuggestions([FromBody] BoxCalculatorInput input)
        {
            try
            {
                var suggestions = _calculatorService.GetSheetLayoutSuggestions(input);

                return Json(new
                {
                    success = true,
                    suggestions = suggestions.Take(5).Select(s => new
                    {
                        sheetSize = s.SheetSize,
                        blanksPerSheet = s.BlanksPerSheet,
                        efficiency = Math.Round(s.EfficiencyPercentage, 1),
                        sheetsRequired = s.SheetsRequired,
                        description = s.LayoutDescription
                    })
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}