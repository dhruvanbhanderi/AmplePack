using AmplePack.Models;
using AmplePack.Services;
using Xunit;

namespace AmplePack.Tests.Services
{
    public class AdvancedBoxRateCalculatorServiceTests
    {
        private readonly AdvancedBoxRateCalculatorService _service;

        public AdvancedBoxRateCalculatorServiceTests()
        {
            _service = new AdvancedBoxRateCalculatorService();
        }

        [Fact]
        public void CalculateBoxRates_SingleLinerMode_ShouldMatchExpectedOutputs()
        {
            // Arrange - Single-Liner defaults from the Excel sheet
            var input = new BoxRateCalculatorInput
            {
                Mode = "single-liner"
            };
            input.ApplyModeDefaults();

            // Act
            var result = _service.CalculateBoxRates(input);

            // Assert
            Assert.Empty(result.Errors);
            Assert.Equal("single-liner", result.Mode);
            Assert.Contains("1000", result.Quantities.Keys);
            Assert.Contains("2000", result.Quantities.Keys);
            Assert.Contains("3000", result.Quantities.Keys);

            // Expected outputs from the requirements:
            // - 1000 QTY: 7.580809677 ?/box
            // - 2000 QTY: 6.673309677 ?/box  
            // - 3000 QTY: 6.389143011 ?/box

            var qty1000 = result.Quantities["1000"];
            var qty2000 = result.Quantities["2000"];
            var qty3000 = result.Quantities["3000"];

            // Check if Box Pes. with Extras and 10% Profit matches expected values (with tolerance)
            Assert.True(Math.Abs(qty1000.BoxPesWithExtras.WithProfit - 7.580809677m) < 0.001m,
                $"Expected ~7.580809677, got {qty1000.BoxPesWithExtras.WithProfit}");
            
            Assert.True(Math.Abs(qty2000.BoxPesWithExtras.WithProfit - 6.673309677m) < 0.001m,
                $"Expected ~6.673309677, got {qty2000.BoxPesWithExtras.WithProfit}");
            
            Assert.True(Math.Abs(qty3000.BoxPesWithExtras.WithProfit - 6.389143011m) < 0.001m,
                $"Expected ~6.389143011, got {qty3000.BoxPesWithExtras.WithProfit}");
        }

        [Fact]
        public void CalculateBoxRates_DualLinerMode_ShouldMatchExpectedOutputs()
        {
            // Arrange - Dual-Liner defaults from the Excel sheet
            var input = new BoxRateCalculatorInput
            {
                Mode = "dual-liner"
            };
            input.ApplyModeDefaults();

            // Act
            var result = _service.CalculateBoxRates(input);

            // Assert
            Assert.Empty(result.Errors);
            Assert.Equal("dual-liner", result.Mode);

            // Expected outputs from the requirements:
            // - 1000 QTIY: 20.463832258 ?/box
            // - 2000 QTY: 18.153832258 ?/box
            // - 3000 QTY: 17.017165591 ?/box

            var qty1000 = result.Quantities["1000"];
            var qty2000 = result.Quantities["2000"];
            var qty3000 = result.Quantities["3000"];

            // Check if Box Pes. with Extras and 10% Profit matches expected values (with tolerance)
            Assert.True(Math.Abs(qty1000.BoxPesWithExtras.WithProfit - 20.463832258m) < 0.001m,
                $"Expected ~20.463832258, got {qty1000.BoxPesWithExtras.WithProfit}");
            
            Assert.True(Math.Abs(qty2000.BoxPesWithExtras.WithProfit - 18.153832258m) < 0.001m,
                $"Expected ~18.153832258, got {qty2000.BoxPesWithExtras.WithProfit}");
            
            Assert.True(Math.Abs(qty3000.BoxPesWithExtras.WithProfit - 17.017165591m) < 0.001m,
                $"Expected ~17.017165591, got {qty3000.BoxPesWithExtras.WithProfit}");
        }

        [Fact]
        public void CalculateBoxRates_InvalidMode_ShouldReturnErrors()
        {
            // Arrange
            var input = new BoxRateCalculatorInput
            {
                Mode = "invalid-mode"
            };

            // Act
            var result = _service.CalculateBoxRates(input);

            // Assert
            Assert.NotEmpty(result.Errors);
            Assert.Contains("mode must be 'single-liner' or 'dual-liner'", result.Errors);
        }

        [Fact]
        public void CalculateBoxRates_ZeroDivisor_ShouldReturnErrors()
        {
            // Arrange
            var input = new BoxRateCalculatorInput
            {
                Mode = "single-liner",
                Divide1 = 0 // Invalid divisor
            };

            // Act
            var result = _service.CalculateBoxRates(input);

            // Assert
            Assert.NotEmpty(result.Errors);
            Assert.Contains("divide1 cannot be zero", result.Errors);
        }

        [Fact]
        public void CalculateBoxRates_DualLinerMissingSecondLiner_ShouldReturnErrors()
        {
            // Arrange
            var input = new BoxRateCalculatorInput
            {
                Mode = "dual-liner",
                Liner2Gsm = null, // Missing required field for dual-liner
                Liner2Rate = null
            };

            // Act
            var result = _service.CalculateBoxRates(input);

            // Assert
            Assert.NotEmpty(result.Errors);
            Assert.Contains("liner2_gsm and liner2_rate required for dual-liner mode", result.Errors);
        }

        [Fact]
        public void GetDefaultInputForMode_SingleLiner_ShouldReturnCorrectDefaults()
        {
            // Act
            var defaults = _service.GetDefaultInputForMode("single-liner");

            // Assert
            Assert.Equal("single-liner", defaults.Mode);
            Assert.Equal(20, defaults.Size1);
            Assert.Equal(21, defaults.Size2);
            Assert.Equal(230, defaults.DuplexGsm);
            Assert.Equal(58, defaults.DuplexRate);
            Assert.Equal(2, defaults.AppsDivisor);
        }

        [Fact]
        public void GetDefaultInputForMode_DualLiner_ShouldReturnCorrectDefaults()
        {
            // Act
            var defaults = _service.GetDefaultInputForMode("dual-liner");

            // Assert
            Assert.Equal("dual-liner", defaults.Mode);
            Assert.Equal(19, defaults.Size1);
            Assert.Equal(20, defaults.Size2);
            Assert.Equal(230, defaults.DuplexGsm);
            Assert.Equal(65, defaults.DuplexRate);
            Assert.Equal(1, defaults.AppsDivisor);
            Assert.Equal(290, defaults.Liner2Gsm);
            Assert.Equal(43, defaults.Liner2Rate);
        }

        [Fact]
        public void CalculateQuickEstimate_ValidInputs_ShouldReturnEstimate()
        {
            // Act
            var estimate = _service.CalculateQuickEstimate("single-liner", 20, 21, 1000);

            // Assert
            Assert.True(estimate > 0);
            // Should be approximately 7.58 based on the expected output
            Assert.True(Math.Abs(estimate - 7.580809677m) < 0.01m);
        }

        [Theory]
        [InlineData("single-liner")]
        [InlineData("dual-liner")]
        public void GetSupportedModes_ShouldReturnBothModes(string mode)
        {
            // Act
            var modes = _service.GetSupportedModes();

            // Assert
            Assert.Contains(mode, modes);
            Assert.Equal(2, modes.Count);
        }

        [Fact]
        public void ExportToExcelFormat_ValidResult_ShouldReturnFormattedText()
        {
            // Arrange
            var input = new BoxRateCalculatorInput { Mode = "single-liner" };
            input.ApplyModeDefaults();
            var result = _service.CalculateBoxRates(input);

            // Act
            var export = _service.ExportToExcelFormat(result);

            // Assert
            Assert.NotEmpty(export);
            Assert.Contains("Box Rate Calculator Results - SINGLE-LINER Mode", export);
            Assert.Contains("Quantity: 1000", export);
            Assert.Contains("Duplex:", export);
            // Check for the actual format used in the export
            Assert.Contains("Box Pes. (With Extras)", export);
        }
    }
}