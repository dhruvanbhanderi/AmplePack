using AmplePack.UITests.Infrastructure;

namespace AmplePack.UITests.Tests
{
    public class OrderManagementUITests : PlaywrightTestBase
    {
        [Fact]
        public async Task Orders_Index_Should_Load_Successfully()
        {
            // Arrange & Act
            await NavigateToAsync("/Orders");

            // Assert
            await Expect(Page).ToHaveTitleAsync(new Regex(".*Orders.*"));
            await Expect(Page.Locator("h1, h2")).ToContainTextAsync("Orders");
        }

        [Fact]
        public async Task Orders_Should_Display_Order_List()
        {
            // Arrange
            await NavigateToAsync("/Orders");

            // Act & Assert
            await Expect(Page.Locator("table, .order-list")).ToBeVisibleAsync();
            
            // Check for common table headers
            var hasTableHeaders = await Page.Locator("th").CountAsync() > 0;
            if (hasTableHeaders)
            {
                await Expect(Page.Locator("th")).ToContainTextAsync(new Regex("Order|Customer|Date|Status|Amount", RegexOptions.IgnoreCase));
            }
        }

        [Fact]
        public async Task Orders_Create_Form_Should_Be_Accessible()
        {
            // Arrange
            await NavigateToAsync("/Orders/Create");

            // Act & Assert
            await Expect(Page.Locator("h1, h2")).ToContainTextAsync(new Regex("Create.*Order", RegexOptions.IgnoreCase));
            await Expect(Page.Locator("form")).ToBeVisibleAsync();
            await Expect(Page.Locator("button[type='submit'], input[type='submit']")).ToBeVisibleAsync();
        }

        [Fact]
        public async Task Orders_Create_Should_Require_Customer_Selection()
        {
            // Arrange
            await NavigateToAsync("/Orders/Create");

            // Act - Try to submit without selecting customer
            await Page.ClickAsync("button[type='submit'], input[type='submit']");

            // Assert
            await Expect(Page.Locator(".field-validation-error, .error")).ToBeVisibleAsync();
        }

        [Fact]
        public async Task Orders_Filter_Should_Work_For_Different_Statuses()
        {
            // Arrange
            await NavigateToAsync("/Orders");

            // Act & Assert for each status filter
            var statuses = new[] { "Pending", "Processing", "Completed", "Cancelled" };

            foreach (var status in statuses)
            {
                // Try to find and click status filter
                var statusFilter = Page.Locator($"a:has-text('{status}'), button:has-text('{status}'), select option:has-text('{status}')");
                
                if (await statusFilter.CountAsync() > 0)
                {
                    await statusFilter.First.ClickAsync();
                    await WaitForPageLoadAsync();
                    
                    // Verify URL contains the filter parameter
                    var currentUrl = Page.Url;
                    currentUrl.Should().Contain($"status={status}", "URL should contain status filter");
                }
            }
        }

        [Fact]
        public async Task Orders_Search_Should_Filter_Results()
        {
            // Arrange
            await NavigateToAsync("/Orders");

            // Act
            var searchBox = Page.Locator("input[type='search'], input[name*='search'], #search");
            
            if (await searchBox.CountAsync() > 0)
            {
                await searchBox.FillAsync("test");
                
                // Try to find and click search button
                var searchButton = Page.Locator("button:has-text('Search'), input[type='submit'][value*='Search']");
                if (await searchButton.CountAsync() > 0)
                {
                    await searchButton.ClickAsync();
                    await WaitForPageLoadAsync();
                }
                else
                {
                    // Try pressing Enter if no search button
                    await searchBox.PressAsync("Enter");
                    await WaitForPageLoadAsync();
                }

                // Assert
                var currentUrl = Page.Url;
                currentUrl.Should().Contain("search", "URL should contain search parameter");
            }
        }

        [Fact]
        public async Task Orders_Details_Should_Show_Order_Information()
        {
            // Arrange
            await NavigateToAsync("/Orders");

            // Act
            var firstDetailsLink = Page.Locator("a:has-text('Details'), a:has-text('View')").First;
            
            if (await firstDetailsLink.CountAsync() > 0)
            {
                await firstDetailsLink.ClickAsync();
                await WaitForPageLoadAsync();

                // Assert
                await Expect(Page.Locator("h1, h2")).ToContainTextAsync(new Regex("Order.*Details|Details.*Order", RegexOptions.IgnoreCase));
                
                // Check for common order details
                var detailsVisible = await Page.Locator(".order-details, .details-container, dl, table").CountAsync() > 0;
                detailsVisible.Should().BeTrue("Order details should be visible");
            }
        }

        [Fact]
        public async Task Orders_Edit_Form_Should_Be_Accessible()
        {
            // Arrange
            await NavigateToAsync("/Orders");

            // Act
            var firstEditLink = Page.Locator("a:has-text('Edit')").First;
            
            if (await firstEditLink.CountAsync() > 0)
            {
                await firstEditLink.ClickAsync();
                await WaitForPageLoadAsync();

                // Assert
                await Expect(Page.Locator("h1, h2")).ToContainTextAsync(new Regex("Edit.*Order", RegexOptions.IgnoreCase));
                await Expect(Page.Locator("form")).ToBeVisibleAsync();
                await Expect(Page.Locator("button[type='submit'], input[type='submit']")).ToBeVisibleAsync();
            }
        }

        [Fact]
        public async Task Orders_Delete_Should_Show_Confirmation()
        {
            // Arrange
            await NavigateToAsync("/Orders");

            // Act
            var firstDeleteLink = Page.Locator("a:has-text('Delete')").First;
            
            if (await firstDeleteLink.CountAsync() > 0)
            {
                await firstDeleteLink.ClickAsync();
                await WaitForPageLoadAsync();

                // Assert
                await Expect(Page.Locator("h1, h2")).ToContainTextAsync(new Regex("Delete.*Order|Confirm.*Delete", RegexOptions.IgnoreCase));
                
                // Should have confirmation buttons
                var confirmButton = Page.Locator("button:has-text('Delete'), input[value*='Delete']");
                var cancelButton = Page.Locator("a:has-text('Cancel'), button:has-text('Cancel')");
                
                await Expect(confirmButton).ToBeVisibleAsync();
                await Expect(cancelButton).ToBeVisibleAsync();
            }
        }

        [Fact]
        public async Task Orders_Pagination_Should_Work_If_Present()
        {
            // Arrange
            await NavigateToAsync("/Orders");

            // Act & Assert
            var paginationLinks = Page.Locator(".pagination a, .pager a");
            
            if (await paginationLinks.CountAsync() > 0)
            {
                var nextLink = paginationLinks.Locator(":has-text('Next'), :has-text('>')").First;
                
                if (await nextLink.CountAsync() > 0)
                {
                    var currentUrl = Page.Url;
                    await nextLink.ClickAsync();
                    await WaitForPageLoadAsync();
                    
                    // URL should change to indicate pagination
                    var newUrl = Page.Url;
                    newUrl.Should().NotBe(currentUrl, "URL should change when navigating to next page");
                }
            }
        }

        [Fact]
        public async Task Orders_Should_Be_Responsive_On_Mobile()
        {
            // Arrange
            await Context.SetViewportSizeAsync(new ViewportSize { Width = 375, Height = 667 }); // iPhone size
            await NavigateToAsync("/Orders");

            // Act & Assert
            await Expect(Page.Locator("h1, h2")).ToBeVisibleAsync();
            
            // Check if table is responsive or has mobile layout
            var table = Page.Locator("table");
            if (await table.CountAsync() > 0)
            {
                // Table should either be scrollable or converted to mobile-friendly layout
                var isScrollable = await table.EvaluateAsync<bool>("el => el.scrollWidth > el.clientWidth");
                var hasMobileClass = await table.EvaluateAsync<bool>("el => el.classList.contains('table-responsive') || el.closest('.table-responsive') !== null");
                
                (isScrollable || hasMobileClass).Should().BeTrue("Table should be mobile-friendly");
            }
        }

        [Fact]
        public async Task Orders_Performance_Should_Load_Within_Time_Limit()
        {
            // Arrange
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            await NavigateToAsync("/Orders");
            await WaitForPageLoadAsync();

            // Assert
            stopwatch.Stop();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Should load within 5 seconds
            await Expect(Page.Locator("h1, h2")).ToBeVisibleAsync();
        }
    }
}