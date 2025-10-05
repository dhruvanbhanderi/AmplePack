using AmplePack.UITests.Infrastructure;

namespace AmplePack.UITests.Tests
{
    public class BoxCalculatorUITests : PlaywrightTestBase
    {
        [Fact]
        public async Task BoxCalculator_Page_Should_Load_Successfully()
        {
            // Arrange & Act
            await NavigateToAsync("/BoxCalculator");

            // Assert
            await Expect(Page).ToHaveTitleAsync(new Regex(".*Box Calculator.*"));
            await Expect(Page.Locator("h1")).ToContainTextAsync("Box Calculator");
        }

        [Fact]
        public async Task BoxCalculator_Form_Should_Have_All_Required_Fields()
        {
            // Arrange
            await NavigateToAsync("/BoxCalculator");

            // Act & Assert
            await Expect(Page.Locator("#Length")).ToBeVisibleAsync();
            await Expect(Page.Locator("#Width")).ToBeVisibleAsync();
            await Expect(Page.Locator("#Height")).ToBeVisibleAsync();
            await Expect(Page.Locator("#BoardGSM")).ToBeVisibleAsync();
            await Expect(Page.Locator("#Quantity")).ToBeVisibleAsync();
            await Expect(Page.Locator("#SheetLength")).ToBeVisibleAsync();
            await Expect(Page.Locator("#SheetWidth")).ToBeVisibleAsync();
            await Expect(Page.Locator("button[type='submit']")).ToBeVisibleAsync();
        }

        [Fact]
        public async Task BoxCalculator_Should_Calculate_Price_With_Valid_Input()
        {
            // Arrange
            await NavigateToAsync("/BoxCalculator");

            // Act
            await FillAndWaitAsync("#Length", "10");
            await FillAndWaitAsync("#Width", "8");
            await FillAndWaitAsync("#Height", "6");
            await FillAndWaitAsync("#BoardGSM", "150");
            await FillAndWaitAsync("#Quantity", "1000");
            await FillAndWaitAsync("#SheetLength", "40");
            await FillAndWaitAsync("#SheetWidth", "30");

            await Page.ClickAsync("button[type='submit']");

            // Assert
            await Page.WaitForSelectorAsync("#calculation-results", new PageWaitForSelectorOptions 
            { 
                Timeout = 10000 
            });

            await Expect(Page.Locator("#calculation-results")).ToBeVisibleAsync();
            await Expect(Page.Locator(".final-price")).ToBeVisibleAsync();
        }

        [Fact]
        public async Task BoxCalculator_Should_Show_Validation_Errors_For_Invalid_Input()
        {
            // Arrange
            await NavigateToAsync("/BoxCalculator");

            // Act - Submit form with empty required fields
            await Page.ClickAsync("button[type='submit']");

            // Assert
            await Expect(Page.Locator(".field-validation-error")).ToBeVisibleAsync();
        }

        [Fact]
        public async Task BoxCalculator_Should_Show_Layout_Visualization()
        {
            // Arrange
            await NavigateToAsync("/BoxCalculator");

            // Act
            await FillAndWaitAsync("#Length", "10");
            await FillAndWaitAsync("#Width", "8");
            await FillAndWaitAsync("#Height", "6");
            await FillAndWaitAsync("#BoardGSM", "150");
            await FillAndWaitAsync("#Quantity", "1000");
            await FillAndWaitAsync("#SheetLength", "40");
            await FillAndWaitAsync("#SheetWidth", "30");

            await Page.ClickAsync("button[type='submit']");

            // Wait for calculation to complete
            await Page.WaitForSelectorAsync("#calculation-results");

            // Click on layout visualization tab/button
            var layoutButton = Page.Locator("#show-layout-btn");
            if (await layoutButton.IsVisibleAsync())
            {
                await layoutButton.ClickAsync();
                
                // Assert
                await Expect(Page.Locator("#layout-visualization")).ToBeVisibleAsync();
                await Expect(Page.Locator(".sheet-container")).ToBeVisibleAsync();
                await Expect(Page.Locator(".box-blank")).ToBeVisibleAsync();
            }
        }

        [Theory]
        [InlineData("5", "3", "2")]
        [InlineData("15", "12", "8")]
        [InlineData("20", "15", "10")]
        public async Task BoxCalculator_Should_Handle_Different_Box_Sizes(string length, string width, string height)
        {
            // Arrange
            await NavigateToAsync("/BoxCalculator");

            // Act
            await FillAndWaitAsync("#Length", length);
            await FillAndWaitAsync("#Width", width);
            await FillAndWaitAsync("#Height", height);
            await FillAndWaitAsync("#BoardGSM", "150");
            await FillAndWaitAsync("#Quantity", "1000");
            await FillAndWaitAsync("#SheetLength", "40");
            await FillAndWaitAsync("#SheetWidth", "30");

            await Page.ClickAsync("button[type='submit']");

            // Assert
            await Page.WaitForSelectorAsync("#calculation-results");
            await Expect(Page.Locator("#calculation-results")).ToBeVisibleAsync();
            await Expect(Page.Locator(".final-price")).ToBeVisibleAsync();

            // Verify the price is a valid number
            var priceText = await Page.Locator(".final-price").TextContentAsync();
            priceText.Should().NotBeNullOrEmpty();
            priceText.Should().MatchRegex(@"\d+\.\d{2}"); // Should be a decimal with 2 places
        }

        [Fact]
        public async Task BoxCalculator_Should_Reset_Form_When_Reset_Button_Clicked()
        {
            // Arrange
            await NavigateToAsync("/BoxCalculator");

            // Act - Fill form
            await FillAndWaitAsync("#Length", "10");
            await FillAndWaitAsync("#Width", "8");
            await FillAndWaitAsync("#Height", "6");

            // Click reset button if it exists
            var resetButton = Page.Locator("button[type='reset'], .reset-btn");
            if (await resetButton.IsVisibleAsync())
            {
                await resetButton.ClickAsync();

                // Assert
                await Expect(Page.Locator("#Length")).ToHaveValueAsync("");
                await Expect(Page.Locator("#Width")).ToHaveValueAsync("");
                await Expect(Page.Locator("#Height")).ToHaveValueAsync("");
            }
        }

        [Fact]
        public async Task BoxCalculator_Should_Be_Responsive_On_Mobile()
        {
            // Arrange
            await Context.SetViewportSizeAsync(new ViewportSize { Width = 375, Height = 667 }); // iPhone size
            await NavigateToAsync("/BoxCalculator");

            // Act & Assert
            await Expect(Page.Locator("h1")).ToBeVisibleAsync();
            await Expect(Page.Locator("#Length")).ToBeVisibleAsync();
            await Expect(Page.Locator("button[type='submit']")).ToBeVisibleAsync();

            // Verify form is still usable
            await FillAndWaitAsync("#Length", "10");
            await FillAndWaitAsync("#Width", "8");
            await FillAndWaitAsync("#Height", "6");
            await FillAndWaitAsync("#BoardGSM", "150");
            await FillAndWaitAsync("#Quantity", "100");
            await FillAndWaitAsync("#SheetLength", "40");
            await FillAndWaitAsync("#SheetWidth", "30");

            await Page.ClickAsync("button[type='submit']");
            await Page.WaitForSelectorAsync("#calculation-results");
            await Expect(Page.Locator("#calculation-results")).ToBeVisibleAsync();
        }

        [Fact]
        public async Task BoxCalculator_Performance_Should_Complete_Within_Time_Limit()
        {
            // Arrange
            await NavigateToAsync("/BoxCalculator");

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            await FillAndWaitAsync("#Length", "10");
            await FillAndWaitAsync("#Width", "8");
            await FillAndWaitAsync("#Height", "6");
            await FillAndWaitAsync("#BoardGSM", "150");
            await FillAndWaitAsync("#Quantity", "1000");
            await FillAndWaitAsync("#SheetLength", "40");
            await FillAndWaitAsync("#SheetWidth", "30");

            await Page.ClickAsync("button[type='submit']");
            await Page.WaitForSelectorAsync("#calculation-results");

            // Assert
            stopwatch.Stop();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should complete within 5 seconds
            await Expect(Page.Locator("#calculation-results")).ToBeVisibleAsync();
        }
    }
}