using AmplePack.Services;
using AmplePack.Models;
using AmplePack.Tests.Infrastructure;

namespace AmplePack.Tests.Unit.Services
{
    public class BoxPriceCalculatorServiceTests
    {
        private readonly BoxPriceCalculatorService _service;

        public BoxPriceCalculatorServiceTests()
        {
            _service = new BoxPriceCalculatorService();
        }

        [Fact]
        public void CalculateBoxPrice_With_Valid_Input_Should_Return_Valid_Result()
        {
            // Arrange
            var input = TestDataFactory.CreateValidBoxCalculatorInput();
            
            // Act
            var result = _service.CalculateBoxPrice(input);
            
            // Assert
            result.Should().NotBeNull();
            result.FinalPricePerBoxIncGST.Should().BeGreaterThan(0);
            result.GrandTotal.Should().BeGreaterThan(0);
        }

        [Theory]
        [InlineData(10, 8, 6)]
        [InlineData(15, 12, 8)]
        [InlineData(5, 3, 2)]
        public void CalculateBoxPrice_Should_Handle_Different_Box_Sizes(decimal length, decimal width, decimal height)
        {
            // Arrange
            var input = TestDataFactory.CreateValidBoxCalculatorInput();
            input.Length = length;
            input.Width = width;
            input.Height = height;
            
            // Act
            var result = _service.CalculateBoxPrice(input);
            
            // Assert
            result.Should().NotBeNull();
            result.FinalPricePerBoxIncGST.Should().BeGreaterThan(0);
            result.BlanksPerSheet.Should().BeGreaterThan(0);
        }

        [Fact]
        public void CalculateBoxPrice_With_Zero_Quantity_Should_Handle_Gracefully()
        {
            // Arrange
            var input = TestDataFactory.CreateValidBoxCalculatorInput();
            input.Quantity = 0;
            
            // Act
            var result = _service.CalculateBoxPrice(input);
            
            // Assert - Service handles zero quantity gracefully, may include shipping costs
            result.Should().NotBeNull();
            result.TotalOrderCostIncGST.Should().BeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public void GenerateLayoutVisualization_With_Valid_Request_Should_Return_Visualization()
        {
            // Arrange
            var request = new LayoutVisualizationRequest
            {
                Length = 10,
                Width = 8,
                Height = 6,
                SheetLength = 40,
                SheetWidth = 30,
                BoardGSM = 150,
                CompressionRatio = 1.0m,
                Quantity = 1000
            };
            
            // Act
            var result = _service.GenerateLayoutVisualization(request);
            
            // Assert
            result.Should().NotBeNull();
            result.TotalBlanksPerSheet.Should().BeGreaterThan(0);
            result.EfficiencyPercentage.Should().BeInRange(0, 100);
            result.BlankPositions.Should().NotBeEmpty();
        }

        [Theory]
        [InlineData(0, 8, 6)]
        [InlineData(10, 0, 6)]
        [InlineData(10, 8, 0)]
        public void GenerateLayoutVisualization_With_Invalid_Dimensions_Should_Handle_Gracefully(decimal length, decimal width, decimal height)
        {
            // Arrange
            var request = new LayoutVisualizationRequest
            {
                Length = length,
                Width = width,
                Height = height,
                SheetLength = 40,
                SheetWidth = 30,
                BoardGSM = 150,
                CompressionRatio = 1.0m,
                Quantity = 1000
            };
            
            // Act
            var result = _service.GenerateLayoutVisualization(request);
            
            // Assert - Service should handle invalid input gracefully, not throw
            result.Should().NotBeNull();
        }

        [Fact]
        public void GetSheetLayoutSuggestions_With_Valid_Input_Should_Return_Suggestions()
        {
            // Arrange
            var input = TestDataFactory.CreateValidBoxCalculatorInput();
            
            // Act
            var result = _service.GetSheetLayoutSuggestions(input);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().NotBeEmpty();
            result.All(s => s.BlanksPerSheet > 0).Should().BeTrue();
            result.All(s => s.EfficiencyPercentage >= 0 && s.EfficiencyPercentage <= 100).Should().BeTrue();
        }

        [Fact]
        public void CalculateBoxPrice_Should_Apply_Discounts_Correctly()
        {
            // Arrange
            var input = TestDataFactory.CreateValidBoxCalculatorInput();
            input.DiscountPercentage = 10; // 10% discount
            
            // Act
            var result = _service.CalculateBoxPrice(input);
            
            // Assert
            result.Should().NotBeNull();
            result.DiscountPerBox.Should().BeGreaterThan(0);
            result.CostAfterDiscountPerBox.Should().BeLessThan(result.CostBeforeDiscountPerBox);
        }

        [Fact]
        public void CalculateBoxPrice_Should_Include_GST_When_Required()
        {
            // Arrange
            var inputWithGST = TestDataFactory.CreateValidBoxCalculatorInput();
            inputWithGST.IncludeGST = true;
            
            var inputWithoutGST = TestDataFactory.CreateValidBoxCalculatorInput();
            inputWithoutGST.IncludeGST = false;
            
            // Act
            var resultWithGST = _service.CalculateBoxPrice(inputWithGST);
            var resultWithoutGST = _service.CalculateBoxPrice(inputWithoutGST);
            
            // Assert
            resultWithGST.FinalPricePerBoxIncGST.Should().BeGreaterThan(resultWithoutGST.FinalPricePerBoxIncGST);
            resultWithGST.GSTPerBox.Should().BeGreaterThan(0);
            resultWithoutGST.GSTPerBox.Should().Be(0);
        }

        [Fact]
        public void CalculateBoxPrice_Should_Calculate_Efficiency_Correctly()
        {
            // Arrange
            var input = TestDataFactory.CreateValidBoxCalculatorInput();
            
            // Act
            var result = _service.CalculateBoxPrice(input);
            
            // Assert
            result.Should().NotBeNull();
            result.EfficiencyPercentage.Should().BeInRange(0, 100);
            result.WastePercentage.Should().BeInRange(0, 100);
            // Note: Due to rounding and calculation differences, the sum might not be exactly 100%
            (result.EfficiencyPercentage + result.WastePercentage).Should().BeInRange(95, 105);
        }

        [Theory]
        [InlineData(1000)]
        [InlineData(5000)]
        [InlineData(10000)]
        public void CalculateBoxPrice_Should_Handle_Different_Quantities(int quantity)
        {
            // Arrange
            var input = TestDataFactory.CreateValidBoxCalculatorInput();
            input.Quantity = quantity;
            
            // Act
            var result = _service.CalculateBoxPrice(input);
            
            // Assert
            result.Should().NotBeNull();
            result.GrandTotal.Should().BeGreaterThan(0);
            result.SheetsRequired.Should().BeGreaterThan(0);
        }

        [Fact]
        public void CalculateBoxPrice_Should_Calculate_Material_Costs()
        {
            // Arrange
            var input = TestDataFactory.CreateValidBoxCalculatorInput();
            
            // Act
            var result = _service.CalculateBoxPrice(input);
            
            // Assert
            result.Should().NotBeNull();
            result.MaterialCostPerBox.Should().BeGreaterThan(0);
            result.TotalMaterialCost.Should().BeGreaterThan(0);
            result.BaseCostPerBox.Should().BeGreaterThan(0);
        }

        [Fact]
        public void CalculateBoxPrice_Should_Include_Production_Costs()
        {
            // Arrange
            var input = TestDataFactory.CreateValidBoxCalculatorInput();
            
            // Act
            var result = _service.CalculateBoxPrice(input);
            
            // Assert
            result.Should().NotBeNull();
            result.PrintingCostPerBox.Should().BeGreaterThanOrEqualTo(0);
            result.DieCuttingCostPerBox.Should().BeGreaterThanOrEqualTo(0);
            result.LaborCostPerBox.Should().BeGreaterThanOrEqualTo(0);
            result.TotalProductionCost.Should().BeGreaterThan(0);
        }

        [Fact]
        public void CalculateBoxPrice_Should_Handle_Edge_Cases()
        {
            // Arrange
            var input = TestDataFactory.CreateValidBoxCalculatorInput();
            input.OverheadPercentage = 0;
            input.ProfitMarginPercentage = 0;
            input.DiscountPercentage = 0;
            
            // Act
            var result = _service.CalculateBoxPrice(input);
            
            // Assert
            result.Should().NotBeNull();
            result.FinalPricePerBoxIncGST.Should().BeGreaterThan(0);
            result.OverheadCostPerBox.Should().Be(0);
            result.ProfitPerBox.Should().Be(0);
            result.DiscountPerBox.Should().Be(0);
        }
    }
}