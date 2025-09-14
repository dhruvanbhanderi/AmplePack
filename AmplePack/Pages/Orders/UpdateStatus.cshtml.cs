using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AmplePack.Data;

namespace AmplePack.Pages_Orders
{
    public class UpdateStatusModel : PageModel
    {
        private readonly AppDbContext _context;

        public UpdateStatusModel(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> OnPostAsync([FromBody] UpdateStatusRequest request)
        {
            try
            {
                var order = await _context.Orders.FindAsync(request.OrderId);
                if (order == null)
                {
                    return NotFound();
                }

                order.Status = request.Status;
                await _context.SaveChangesAsync();

                return new JsonResult(new { success = true, message = "Order status updated successfully" });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = "Failed to update order status" }) 
                { 
                    StatusCode = 500 
                };
            }
        }
    }

    public class UpdateStatusRequest
    {
        public int OrderId { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}