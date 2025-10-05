using Microsoft.Extensions.Configuration;

namespace AmplePack.UITests.Infrastructure
{
    /// <summary>
    /// Base class for Playwright tests with common setup and utilities
    /// </summary>
    public abstract class PlaywrightTestBase : IAsyncLifetime
    {
        protected IPlaywright Playwright { get; private set; } = null!;
        protected IBrowser Browser { get; private set; } = null!;
        protected IBrowserContext Context { get; private set; } = null!;
        protected IPage Page { get; private set; } = null!;
        protected IConfiguration Configuration { get; private set; } = null!;

        protected string BaseUrl => Configuration["BaseUrl"] ?? "https://localhost:5001";
        protected int TestTimeout => Configuration.GetValue<int>("TestTimeout", 30000);

        public async Task InitializeAsync()
        {
            // Load configuration
            Configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.test.json", optional: false)
                .Build();

            // Initialize Playwright
            Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            
            Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = Configuration.GetValue<bool>("Headless", true),
                SlowMo = Configuration.GetValue<int>("SlowMo", 0)
            });

            Context = await Browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = 1280, Height = 720 },
                IgnoreHTTPSErrors = true
            });

            Page = await Context.NewPageAsync();
            
            // Set default timeout
            Page.SetDefaultTimeout(TestTimeout);
        }

        public async Task DisposeAsync()
        {
            await Page?.CloseAsync()!;
            await Context?.CloseAsync()!;
            await Browser?.CloseAsync()!;
            Playwright?.Dispose();
        }

        /// <summary>
        /// Navigate to a specific page
        /// </summary>
        protected async Task NavigateToAsync(string path)
        {
            var url = $"{BaseUrl}{path}";
            await Page.GotoAsync(url);
        }

        /// <summary>
        /// Login with test credentials
        /// </summary>
        protected async Task LoginAsync(string email = null!, string password = null!)
        {
            email ??= Configuration["TestUser:Email"]!;
            password ??= Configuration["TestUser:Password"]!;

            await NavigateToAsync("/Account/Login");
            
            await Page.FillAsync("#Email", email);
            await Page.FillAsync("#Password", password);
            await Page.ClickAsync("button[type='submit']");
            
            // Wait for redirect after login
            await Page.WaitForURLAsync("**/");
        }

        /// <summary>
        /// Take a screenshot for debugging
        /// </summary>
        protected async Task TakeScreenshotAsync(string name)
        {
            var screenshotPath = Path.Combine("screenshots", $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath)!);
            await Page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath });
        }

        /// <summary>
        /// Wait for the page to load completely
        /// </summary>
        protected async Task WaitForPageLoadAsync()
        {
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        /// <summary>
        /// Fill a form field and wait for it to be filled
        /// </summary>
        protected async Task FillAndWaitAsync(string selector, string value)
        {
            await Page.FillAsync(selector, value);
            await Page.WaitForFunctionAsync($"document.querySelector('{selector}').value === '{value}'");
        }

        /// <summary>
        /// Click and wait for navigation
        /// </summary>
        protected async Task ClickAndWaitForNavigationAsync(string selector)
        {
            await Page.ClickAsync(selector);
            await WaitForPageLoadAsync();
        }
    }
}