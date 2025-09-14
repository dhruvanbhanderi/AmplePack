using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using AmplePack.Data;
using AmplePack.Models;
using Microsoft.Extensions.Logging;

namespace AmplePack.Pages_Customers
{
    public class CreateModel : PageModel
    {
        private readonly AmplePack.Data.AppDbContext _context;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(AmplePack.Data.AppDbContext context, ILogger<CreateModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public Customer Customer { get; set; } = default!;

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            _logger.LogInformation("OnPostAsync called for Customer Create");
            _logger.LogInformation($"Customer data: Name={Customer?.Name}, Email={Customer?.Email}, Contact={Customer?.Contact}, Address={Customer?.Address}");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid");
                foreach (var error in ModelState)
                {
                    foreach (var subError in error.Value.Errors)
                    {
                        _logger.LogWarning($"Validation Error in {error.Key}: {subError.ErrorMessage}");
                    }
                }
                return Page();
            }

            try
            {
                _logger.LogInformation("Adding customer to context");
                _context.Customers.Add(Customer);
                
                _logger.LogInformation("Saving changes to database");
                var result = await _context.SaveChangesAsync();
                _logger.LogInformation($"SaveChangesAsync returned: {result}");

                if (result > 0)
                {
                    _logger.LogInformation($"Customer saved successfully with ID: {Customer.Id}");
                }

                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving customer to database");
                ModelState.AddModelError("", $"Unable to save customer: {ex.Message}");
                return Page();
            }
        }
    }
}
