using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AmplePack.Data;
using AmplePack.Models;
using System.Text.Json;

namespace AmplePack.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api")]
    public class ApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ApiController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/customers
        [HttpGet("customers")]
        public async Task<IActionResult> GetCustomers()
        {
            try
            {
                var customers = await _context.Customers
                    .Select(c => new
                    {
                        id = c.Id,
                        name = c.Name,
                        email = c.Email,
                        contact = c.Contact
                    })
                    .OrderBy(c => c.name)
                    .ToListAsync();

                return Ok(new { success = true, customers });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // GET: api/inventory
        [HttpGet("inventory")]
        public async Task<IActionResult> GetInventory()
        {
            try
            {
                var inventory = await _context.Inventories
                    .Select(i => new
                    {
                        id = i.Id,
                        itemName = i.ItemName,
                        category = i.Category,
                        availableQuantity = i.AvailableQuantity,
                        unit = i.Unit,
                        unitPrice = i.UnitPrice,
                        isLowStock = i.IsLowStock
                    })
                    .OrderBy(i => i.itemName)
                    .ToListAsync();

                return Ok(new { success = true, inventory });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // POST: api/inventory/quick-add
        [HttpPost("inventory/quick-add")]
        public async Task<IActionResult> QuickAddInventory([FromBody] InventoryAdjustmentRequest request)
        {
            try
            {
                var item = await _context.Inventories.FindAsync(request.ItemId);
                if (item == null)
                {
                    return NotFound(new { success = false, message = "Inventory item not found" });
                }

                if (request.Quantity <= 0)
                {
                    return BadRequest(new { success = false, message = "Quantity must be greater than 0" });
                }

                var oldQuantity = item.AvailableQuantity;

                // Update inventory
                item.AvailableQuantity += request.Quantity;
                _context.Update(item);
                await _context.SaveChangesAsync();

                // Log the change
                await LogInventoryChange(request.ItemId, "ADD", request.Quantity, request.Reason, oldQuantity, item.AvailableQuantity);

                return Ok(new 
                { 
                    success = true, 
                    newQuantity = item.AvailableQuantity,
                    message = $"Successfully added {request.Quantity} units"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // POST: api/inventory/quick-remove
        [HttpPost("inventory/quick-remove")]
        public async Task<IActionResult> QuickRemoveInventory([FromBody] InventoryAdjustmentRequest request)
        {
            try
            {
                var item = await _context.Inventories.FindAsync(request.ItemId);
                if (item == null)
                {
                    return NotFound(new { success = false, message = "Inventory item not found" });
                }

                if (request.Quantity <= 0)
                {
                    return BadRequest(new { success = false, message = "Quantity must be greater than 0" });
                }

                if (item.AvailableQuantity < request.Quantity)
                {
                    return BadRequest(new { success = false, message = "Insufficient inventory available" });
                }

                if (string.IsNullOrWhiteSpace(request.Reason))
                {
                    return BadRequest(new { success = false, message = "Reason is required for inventory removal" });
                }

                var oldQuantity = item.AvailableQuantity;

                // Update inventory
                item.AvailableQuantity -= request.Quantity;
                _context.Update(item);
                await _context.SaveChangesAsync();

                // Log the change with customer/order info if provided
                await LogInventoryChange(request.ItemId, "REMOVE", request.Quantity, request.Reason, oldQuantity, item.AvailableQuantity, request.CustomerId, request.OrderId);

                return Ok(new 
                { 
                    success = true, 
                    newQuantity = item.AvailableQuantity,
                    message = $"Successfully removed {request.Quantity} units"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // POST: api/audit/inventory
        [HttpPost("audit/inventory")]
        public async Task<IActionResult> LogInventoryAudit([FromBody] InventoryAuditLog auditLog)
        {
            try
            {
                await LogInventoryChange(
                    auditLog.ItemId, 
                    auditLog.Action, 
                    auditLog.Quantity, 
                    auditLog.Reason, 
                    null, 
                    null, 
                    null, 
                    null, 
                    auditLog.ChangedBy
                );

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // GET: api/audit/inventory/{itemId}
        [HttpGet("audit/inventory/{itemId}")]
        public async Task<IActionResult> GetInventoryAuditLog(int itemId)
        {
            try
            {
                var auditLogs = await _context.AuditLogs
                    .Where(al => al.EntityType == "Inventory" && al.EntityId == itemId)
                    .OrderByDescending(al => al.Timestamp)
                    .Take(50) // Limit to last 50 entries
                    .Select(al => new
                    {
                        id = al.Id,
                        action = al.Action,
                        field = al.Field,
                        oldValue = al.OldValue,
                        newValue = al.NewValue,
                        details = al.Details,
                        changedBy = al.ChangedBy,
                        timestamp = al.Timestamp,
                        reason = al.Reason
                    })
                    .ToListAsync();

                return Ok(new { success = true, auditLog = auditLogs });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // GET: api/audit/order/{orderId}
        [HttpGet("audit/order/{orderId}")]
        public async Task<IActionResult> GetOrderAuditLog(int orderId)
        {
            try
            {
                var auditLogs = await _context.AuditLogs
                    .Where(al => al.EntityType == "Order" && al.EntityId == orderId)
                    .OrderByDescending(al => al.Timestamp)
                    .Take(50) // Limit to last 50 entries
                    .Select(al => new
                    {
                        id = al.Id,
                        action = al.Action,
                        field = al.Field,
                        oldValue = al.OldValue,
                        newValue = al.NewValue,
                        details = al.Details,
                        changedBy = al.ChangedBy,
                        timestamp = al.Timestamp,
                        reason = al.Reason
                    })
                    .ToListAsync();

                return Ok(new { success = true, auditLog = auditLogs });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // GET: api/dashboard/statistics
        [HttpGet("dashboard/statistics")]
        public async Task<IActionResult> GetDashboardStatistics()
        {
            try
            {
                var totalOrders = await _context.Orders.CountAsync();
                var pendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending");
                var processingOrders = await _context.Orders.CountAsync(o => o.Status == "Processing");
                var completedOrders = await _context.Orders.CountAsync(o => o.Status == "Completed");
                
                var totalCustomers = await _context.Customers.CountAsync();
                var totalInventoryItems = await _context.Inventories.CountAsync();
                var lowStockItems = await _context.Inventories.CountAsync(i => i.IsLowStock);
                
                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;
                var monthlyRevenue = await _context.Orders
                    .Where(o => o.Status == "Completed" && o.Date.Month == currentMonth && o.Date.Year == currentYear)
                    .SumAsync(o => o.TotalAmount);

                var statistics = new
                {
                    orders = new
                    {
                        total = totalOrders,
                        pending = pendingOrders,
                        processing = processingOrders,
                        completed = completedOrders
                    },
                    customers = new
                    {
                        total = totalCustomers
                    },
                    inventory = new
                    {
                        total = totalInventoryItems,
                        lowStock = lowStockItems
                    },
                    revenue = new
                    {
                        monthly = monthlyRevenue
                    }
                };

                return Ok(new { success = true, statistics });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // Helper method to log inventory changes with full audit trail
        private async Task LogInventoryChange(
            int itemId, 
            string action, 
            decimal quantity, 
            string? reason, 
            decimal? oldQuantity = null, 
            decimal? newQuantity = null,
            int? customerId = null, 
            int? orderId = null, 
            string? changedBy = null)
        {
            try
            {
                var item = await _context.Inventories.FindAsync(itemId);
                var itemName = item?.ItemName ?? "Unknown Item";

                var details = new
                {
                    ItemName = itemName,
                    QuantityChanged = quantity,
                    CustomerId = customerId,
                    OrderId = orderId,
                    Reason = reason
                };

                var auditLog = new AuditLog
                {
                    EntityType = "Inventory",
                    EntityId = itemId,
                    Action = action,
                    Field = "AvailableQuantity",
                    OldValue = oldQuantity?.ToString("F2"),
                    NewValue = newQuantity?.ToString("F2"),
                    Details = JsonSerializer.Serialize(details),
                    ChangedBy = changedBy ?? User?.Identity?.Name ?? "System",
                    Timestamp = DateTime.UtcNow,
                    Reason = reason,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString()
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the main operation
                System.Diagnostics.Debug.WriteLine($"Failed to create audit log: {ex.Message}");
            }
        }

        // Helper method to log order status changes
        public async Task LogOrderStatusChange(int orderId, string fromStatus, string toStatus, string? changedBy = null, string? reason = null)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Customer)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                var details = new
                {
                    OrderId = orderId,
                    CustomerName = order?.Customer?.Name ?? "Unknown",
                    FromStatus = fromStatus,
                    ToStatus = toStatus
                };

                var auditLog = new AuditLog
                {
                    EntityType = "Order",
                    EntityId = orderId,
                    Action = "STATUS_CHANGE",
                    Field = "Status",
                    OldValue = fromStatus,
                    NewValue = toStatus,
                    Details = JsonSerializer.Serialize(details),
                    ChangedBy = changedBy ?? User?.Identity?.Name ?? "System",
                    Timestamp = DateTime.UtcNow,
                    Reason = reason,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString()
                };

                _context.AuditLogs.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the main operation
                System.Diagnostics.Debug.WriteLine($"Failed to create audit log: {ex.Message}");
            }
        }
    }

    // DTOs for API requests
    public class InventoryAdjustmentRequest
    {
        public int ItemId { get; set; }
        public decimal Quantity { get; set; }
        public string Reason { get; set; } = string.Empty;
        public int? CustomerId { get; set; }
        public int? OrderId { get; set; }
    }

    public class InventoryAuditLog
    {
        public int ItemId { get; set; }
        public string Action { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string ChangedBy { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}