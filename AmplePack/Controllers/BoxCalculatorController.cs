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
            var model = new BoxCalculatorInput
            {
                // Set default box dimensions
                Length = 12,
                Width = 8,
                Height = 6,
                
                // Set default sheet dimensions
                SheetLength = 40,
                SheetWidth = 30,
                
                // Set default board specifications
                BoardGSM = 200,
                CompressionRatio = 1.32m,
                
                // Set default quantities and percentages
                Quantity = 1000,
                OverheadPercentage = 15,
                ProfitMarginPercentage = 20,
                DiscountPercentage = 0,
                WastePercentage = 5,
                
                // Set default options
                IncludeGST = true,
                ShowDetailedBreakdown = true,
                ShippingCostPerOrder = 0
            };
            
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

        /// <summary>
        /// API: Get real-time layout visualization data
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        public IActionResult GetLayoutVisualization([FromBody] LayoutVisualizationRequest request)
        {
            try
            {
                // Add detailed logging for debugging
                Console.WriteLine($"Received visualization request: L={request.Length}, W={request.Width}, H={request.Height}");
                Console.WriteLine($"Sheet: {request.SheetLength}x{request.SheetWidth}, GSM={request.BoardGSM}, CR={request.CompressionRatio}");

                // Validate input
                if (request.Length <= 0 || request.Width <= 0 || request.Height <= 0)
                {
                    return Json(new { success = false, message = "Invalid box dimensions" });
                }

                if (request.SheetLength <= 0 || request.SheetWidth <= 0)
                {
                    return Json(new { success = false, message = "Invalid sheet dimensions" });
                }

                var visualization = _calculatorService.GenerateLayoutVisualization(request);

                if (visualization == null)
                {
                    return Json(new { success = false, message = "Failed to generate visualization" });
                }

                Console.WriteLine($"Generated visualization: {visualization.TotalBlanksPerSheet} blanks, {visualization.EfficiencyPercentage:F1}% efficiency");

                return Json(new
                {
                    success = true,
                    visualization = new
                    {
                        // Sheet dimensions
                        sheetLengthMm = (double)Math.Round(visualization.SheetLengthMm, 1),
                        sheetWidthMm = (double)Math.Round(visualization.SheetWidthMm, 1),
                        sheetLengthInches = (double)Math.Round(visualization.SheetLengthInches, 2),
                        sheetWidthInches = (double)Math.Round(visualization.SheetWidthInches, 2),
                        
                        // Blank dimensions
                        blankLengthMm = (double)Math.Round(visualization.BlankLengthMm, 1),
                        blankWidthMm = (double)Math.Round(visualization.BlankWidthMm, 1),
                        blankLengthInches = (double)Math.Round(visualization.BlankLengthInches, 2),
                        blankWidthInches = (double)Math.Round(visualization.BlankWidthInches, 2),
                        
                        // Layout information
                        blanksPerRow = visualization.BlanksPerRow,
                        blanksPerColumn = visualization.BlanksPerColumn,
                        totalBlanksPerSheet = visualization.TotalBlanksPerSheet,
                        
                        // Efficiency metrics
                        efficiencyPercentage = (double)Math.Round(visualization.EfficiencyPercentage, 1),
                        wastePercentage = (double)Math.Round(visualization.WastePercentage, 1),
                        usedAreaSqM = (double)Math.Round(visualization.UsedAreaSqM, 4),
                        wastedAreaSqM = (double)Math.Round(visualization.WastedAreaSqM, 4),
                        
                        // Unused areas
                        unusedLengthMm = (double)Math.Round(visualization.UnusedLengthMm, 1),
                        unusedWidthMm = (double)Math.Round(visualization.UnusedWidthMm, 1),
                        
                        // Position data for rendering
                        blankPositions = visualization.BlankPositions.Select(bp => new
                        {
                            row = bp.Row,
                            column = bp.Column,
                            startXPercent = (double)Math.Round(bp.StartXPercent, 2),
                            startYPercent = (double)Math.Round(bp.StartYPercent, 2),
                            widthPercent = (double)Math.Round(bp.WidthPercent, 2),
                            heightPercent = (double)Math.Round(bp.HeightPercent, 2),
                            startXMm = (double)Math.Round(bp.StartXMm, 1),
                            startYMm = (double)Math.Round(bp.StartYMm, 1),
                            widthMm = (double)Math.Round(bp.WidthMm, 1),
                            heightMm = (double)Math.Round(bp.HeightMm, 1)
                        }).ToArray(),
                        
                        wasteAreas = visualization.WasteAreas.Select(wa => new
                        {
                            areaType = wa.AreaType,
                            startXPercent = (double)Math.Round(wa.StartXPercent, 2),
                            startYPercent = (double)Math.Round(wa.StartYPercent, 2),
                            widthPercent = (double)Math.Round(wa.WidthPercent, 2),
                            heightPercent = (double)Math.Round(wa.HeightPercent, 2),
                            areaSqM = (double)Math.Round(wa.AreaSqM, 4)
                        }).ToArray(),
                        
                        // Descriptions and suggestions
                        layoutDescription = visualization.LayoutDescription ?? string.Empty,
                        isOptimal = visualization.IsOptimal,
                        optimizationSuggestions = visualization.OptimizationSuggestions ?? new List<string>()
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Visualization error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        /// <summary>
        /// API: Get layout comparison for multiple sheet sizes
        /// </summary>
        [HttpPost]
        public IActionResult GetLayoutComparison([FromBody] LayoutVisualizationRequest request)
        {
            try
            {
                var comparisons = new List<object>();
                var standardSheets = SheetSpecs.GetStandardSheetSizes().Where(s => s.IsStandard).Take(6).ToList();

                foreach (var sheet in standardSheets)
                {
                    var tempRequest = new LayoutVisualizationRequest
                    {
                        Length = request.Length,
                        Width = request.Width,
                        Height = request.Height,
                        SheetLength = sheet.LengthInches,
                        SheetWidth = sheet.WidthInches,
                        BoardGSM = request.BoardGSM,
                        CompressionRatio = request.CompressionRatio,
                        Quantity = request.Quantity
                    };

                    var visualization = _calculatorService.GenerateLayoutVisualization(tempRequest);

                    comparisons.Add(new
                    {
                        sheetSize = sheet.SheetSize,
                        sheetLengthInches = sheet.LengthInches,
                        sheetWidthInches = sheet.WidthInches,
                        blanksPerSheet = visualization.TotalBlanksPerSheet,
                        efficiency = Math.Round(visualization.EfficiencyPercentage, 1),
                        waste = Math.Round(visualization.WastePercentage, 1),
                        layoutDescription = visualization.LayoutDescription,
                        isOptimal = visualization.IsOptimal,
                        sheetsRequired = visualization.TotalBlanksPerSheet > 0 ? 
                            (int)Math.Ceiling((double)request.Quantity / visualization.TotalBlanksPerSheet) : int.MaxValue
                    });
                }

                return Json(new
                {
                    success = true,
                    comparisons = comparisons.OrderByDescending(c => c.GetType().GetProperty("efficiency")?.GetValue(c, null)).ToList()
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Simple test endpoint for visualization debugging
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult TestVisualization()
        {
            try
            {
                var testRequest = new LayoutVisualizationRequest
                {
                    Length = 12,
                    Width = 8,
                    Height = 6,
                    SheetLength = 40,
                    SheetWidth = 30,
                    BoardGSM = 200,
                    CompressionRatio = 1.32m,
                    Quantity = 1000
                };

                var result = _calculatorService.GenerateLayoutVisualization(testRequest);

                return Json(new
                {
                    success = true,
                    message = "Test successful",
                    result = new
                    {
                        totalBlanks = result.TotalBlanksPerSheet,
                        efficiency = result.EfficiencyPercentage,
                        layout = $"{result.BlanksPerRow}x{result.BlanksPerColumn}",
                        description = result.LayoutDescription
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        /// <summary>
        /// Very simple test endpoint that just returns hardcoded data
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult TestVisualizationSimple()
        {
            return Json(new
            {
                success = true,
                visualization = new
                {
                    sheetLengthMm = 1016.0,
                    sheetWidthMm = 762.0,
                    sheetLengthInches = 40.0,
                    sheetWidthInches = 30.0,
                    blankLengthMm = 558.8,
                    blankWidthMm = 371.9,
                    blankLengthInches = 22.0,
                    blankWidthInches = 14.6,
                    blanksPerRow = 1,
                    blanksPerColumn = 2,
                    totalBlanksPerSheet = 2,
                    efficiencyPercentage = 53.8,
                    wastePercentage = 46.2,
                    layoutDescription = "1 × 2 layout (2 blanks per sheet, 53.8% efficiency)",
                    blankPositions = new object[]
                    {
                        new { row = 1, column = 1, startXPercent = 0.0, startYPercent = 0.0, widthPercent = 55.0, heightPercent = 48.8 },
                        new { row = 2, column = 1, startXPercent = 0.0, startYPercent = 48.8, widthPercent = 55.0, heightPercent = 48.8 }
                    },
                    wasteAreas = new object[]
                    {
                        new { areaType = "RightMargin", startXPercent = 55.0, startYPercent = 0.0, widthPercent = 45.0, heightPercent = 97.6 }
                    }
                }
            });
        }

        /// <summary>
        /// Test page for visualization debugging (no auth required)
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Test()
        {
            return PhysicalFile(
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "test-visualization.html"),
                "text/html");
        }
    }
}