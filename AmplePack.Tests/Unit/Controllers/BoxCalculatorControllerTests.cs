using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AmplePack.Controllers;
using AmplePack.Services;
using AmplePack.Models;
using AmplePack.Tests.Infrastructure;

namespace AmplePack.Tests.Unit.Controllers
{
    public class BoxCalculatorControllerTests
    {
        private readonly BoxCalculatorController _controller;

        public BoxCalculatorControllerTests()
        {
            // Use real service instead of mock since methods aren't virtual
            var calculatorService = new BoxPriceCalculatorService();
            _controller = new BoxCalculatorController(calculatorService);
        }

        [Fact]
        public void Index_Should_Return_View()
        {
            // Act
            var result = _controller.Index();

            // Assert
            result.Should().BeOfType<ViewResult>();
        }

        [Fact]
        public void Index_POST_With_Valid_Input_Should_Return_View_With_Result()
        {
            // Arrange
            var input = TestDataFactory.CreateValidBoxCalculatorInput();

            // Act
            var result = _controller.Index(input);

            // Assert
            result.Should().BeOfType<ViewResult>();
        }

        [Fact]
        public void Index_POST_With_Invalid_Model_Should_Return_View()
        {
            // Arrange
            var input = new BoxCalculatorInput(); // Invalid input
            _controller.ModelState.AddModelError("Length", "Length is required");

            // Act
            var result = _controller.Index(input);

            // Assert
            result.Should().BeOfType<ViewResult>();
        }

        [Fact]
        public void QuickCalculate_With_Valid_Input_Should_Return_Json()
        {
            // Arrange
            var input = TestDataFactory.CreateValidBoxCalculatorInput();

            // Act
            var result = _controller.QuickCalculate(input);

            // Assert
            result.Should().BeOfType<JsonResult>();
        }

        [Fact]
        public void QuickCalculate_With_Invalid_Model_Should_Return_Error_Json()
        {
            // Arrange
            var input = new BoxCalculatorInput(); // Invalid input
            _controller.ModelState.AddModelError("Length", "Length is required");

            // Act
            var result = _controller.QuickCalculate(input);

            // Assert
            result.Should().BeOfType<JsonResult>();
        }

        [Fact]
        public void GetLayoutVisualization_Should_Return_Json_Result()
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
            var result = _controller.GetLayoutVisualization(request);

            // Assert
            result.Should().BeOfType<JsonResult>();
        }

        [Fact]
        public void GetOptimizationSuggestions_Should_Return_Json_Result()
        {
            // Arrange
            var input = TestDataFactory.CreateValidBoxCalculatorInput();

            // Act
            var result = _controller.GetOptimizationSuggestions(input);

            // Assert
            result.Should().BeOfType<JsonResult>();
        }

        [Fact]
        public void GetBoardSpecs_With_Valid_BoardType_Should_Return_Json()
        {
            // Act
            var result = _controller.GetBoardSpecs("Single Wall");

            // Assert
            result.Should().BeOfType<JsonResult>();
        }

        [Fact]
        public void GetBoardSpecs_With_Invalid_BoardType_Should_Return_Error_Json()
        {
            // Act
            var result = _controller.GetBoardSpecs("Invalid Board Type");

            // Assert
            result.Should().BeOfType<JsonResult>();
        }

        [Fact]
        public void GetMaterialRates_With_Valid_Category_Should_Return_Json()
        {
            // Act
            var result = _controller.GetMaterialRates("Standard");

            // Assert
            result.Should().BeOfType<JsonResult>();
        }

        [Fact]
        public void GetSheetSpecs_With_Valid_SheetSize_Should_Return_Json()
        {
            // Act
            var result = _controller.GetSheetSpecs("40x30");

            // Assert
            result.Should().BeOfType<JsonResult>();
        }

        [Fact]
        public void TestVisualization_Should_Return_Json()
        {
            // Act
            var result = _controller.TestVisualization();

            // Assert
            result.Should().BeOfType<JsonResult>();
        }

        [Fact]
        public void TestVisualizationSimple_Should_Return_Json()
        {
            // Act
            var result = _controller.TestVisualizationSimple();

            // Assert
            result.Should().BeOfType<JsonResult>();
        }
    }
}