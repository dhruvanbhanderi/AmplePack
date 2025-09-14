using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AmplePack.Pages_Orders
{
    public class CreateModel : PageModel
    {
        public IActionResult OnGet()
        {
            // Redirect to the new workflow-based order creation
            return RedirectToPage("/Orders/CreateWorkflow");
        }
    }
}
