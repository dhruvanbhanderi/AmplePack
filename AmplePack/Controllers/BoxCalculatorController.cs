using AmplePack.Models;
using AmplePack.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AmplePack.Controllers
{
    [Authorize]
    public class BoxCalculatorController : Controller
    {
        private readonly AdvancedBoxRateCalculatorService _calculatorService;
        private readonly ILogger<BoxCalculatorController> _logger;

        public BoxCalculatorController(
            AdvancedBoxRateCalculatorService calculatorService,
            ILogger<BoxCalculatorController> logger)
        {
            _calculatorService = calculatorService;
            _logger = logger;
        }

        public IActionResult Index(string? mode = null)
        {
            try
            {
                var supportedModes = _calculatorService.GetSupportedModes();
                
                // Set default mode if provided
                BoxRateCalculatorInput input;
                if (!string.IsNullOrEmpty(mode) && supportedModes.Contains(mode.ToLower()))
                {
                    input = _calculatorService.GetDefaultInputForMode(mode);
                }
                else
                {
                    input = _calculatorService.GetDefaultInputForMode("single-liner");
                }

                var viewModel = new BoxCalculatorViewModel
                {
                    Input = input,
                    SupportedModes = supportedModes,
                    ShowResults = false
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading box calculator page");
                ModelState.AddModelError("", "Error loading calculator. Please try again.");
                
                var fallbackViewModel = new BoxCalculatorViewModel
                {
                    Input = new BoxRateCalculatorInput { Mode = "single-liner" },
                    SupportedModes = new List<string> { "single-liner", "dual-liner" },
                    ShowResults = false
                };
                
                return View(fallbackViewModel);
            }
        }

        [HttpPost]
        public IActionResult Calculate(BoxRateCalculatorInput input)
        {
            try
            {
                var supportedModes = _calculatorService.GetSupportedModes();
                
                if (!ModelState.IsValid)
                {
                    var viewModel = new BoxCalculatorViewModel
                    {
                        Input = input,
                        SupportedModes = supportedModes,
                        ShowResults = false
                    };
                    return View("Index", viewModel);
                }

                _logger.LogInformation("Calculating box rates for mode: {Mode}", input.Mode);

                var result = _calculatorService.CalculateBoxRates(input);

                if (result.Errors.Any())
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                }

                var calculatedViewModel = new BoxCalculatorViewModel
                {
                    Input = input,
                    Result = result,
                    SupportedModes = supportedModes,
                    ShowResults = true
                };

                return View("Index", calculatedViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating box rates");
                ModelState.AddModelError("", $"Calculation error: {ex.Message}");
                
                var errorViewModel = new BoxCalculatorViewModel
                {
                    Input = input,
                    SupportedModes = _calculatorService.GetSupportedModes(),
                    ShowResults = false
                };
                
                return View("Index", errorViewModel);
            }
        }

        [HttpPost]
        public IActionResult LoadDefaults(BoxRateCalculatorInput input)
        {
            try
            {
                var defaultInput = _calculatorService.GetDefaultInputForMode(input.Mode);
                var supportedModes = _calculatorService.GetSupportedModes();
                
                var viewModel = new BoxCalculatorViewModel
                {
                    Input = defaultInput,
                    SupportedModes = supportedModes,
                    ShowResults = false
                };

                return View("Index", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading defaults for mode: {Mode}", input.Mode);
                ModelState.AddModelError("", "Error loading defaults. Please try again.");
                
                var errorViewModel = new BoxCalculatorViewModel
                {
                    Input = input,
                    SupportedModes = _calculatorService.GetSupportedModes(),
                    ShowResults = false
                };
                
                return View("Index", errorViewModel);
            }
        }

        [HttpPost]
        public IActionResult Export(BoxRateCalculatorInput input)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest("Invalid input data");
                }

                var result = _calculatorService.CalculateBoxRates(input);
                
                if (result.Errors.Any())
                {
                    return BadRequest($"Calculation errors: {string.Join(", ", result.Errors)}");
                }

                var exportText = _calculatorService.ExportToExcelFormat(result);
                var fileName = $"BoxRateCalculation_{input.Mode}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                
                return File(System.Text.Encoding.UTF8.GetBytes(exportText), "text/plain", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting calculation");
                return BadRequest($"Export error: {ex.Message}");
            }
        }
    }
}