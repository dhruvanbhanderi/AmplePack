# ?? AmplePack Performance Quick Reference

## **? Fast Queries Cheat Sheet**

### **Pagination (ALWAYS USE)**
```csharp
// Import
using AmplePack.Extensions;
using AmplePack.Models;

// In Controller
public async Task<IActionResult> List(int page = 1, int pageSize = 50)
{
    var result = await _context.Orders
        .Where(o => o.Status == "Active")
        .OrderByDescending(o => o.Date)
        .ToPagedResultAsync(page, pageSize);
    
    return View(result);
}

// In View
@model PagedResult<Order>
Showing @Model.StartRecord - @Model.EndRecord of @Model.TotalRecords
```

---

### **Caching (For Repeated Data)**
```csharp
// Inject
private readonly CacheService _cache;

// Use
var stats = await _cache.GetOrCreateShortAsync(
    "dashboard_stats",
    async () => {
        return await _context.Orders
            .Where(o => o.Status == "Completed")
            .SumAsync(o => o.TotalAmount);
    }
);

// Clear when data changes
_cache.Remove("dashboard_stats");
```

**Cache Duration Guide:**
- **Short (5 min):** Dashboard stats, current user data
- **Medium (15 min):** Dropdown lists, lookup tables
- **Long (1 hour):** Chart data, historical reports

---

### **Avoid N+1 Queries**
```csharp
// ? BAD - N+1 Query
var customers = await _context.Customers.ToListAsync();
foreach(var c in customers) {
    var total = c.Orders.Where(o => o.Status == "Completed").Sum(o => o.TotalAmount);
}

// ? GOOD - Single Query with Projection
var customers = await _context.Customers
    .Select(c => new CustomerSummary {
        Id = c.Id,
        Name = c.Name,
        TotalRevenue = c.Orders
            .Where(o => o.Status == "Completed")
            .Sum(o => o.TotalAmount)
    })
    .ToListAsync();
```

---

### **Efficient Includes**
```csharp
// ? BAD - Loads everything
var orders = await _context.Orders
    .Include(o => o.Customer)
        .ThenInclude(c => c.Orders) // Loads ALL customer orders!
    .ToListAsync();

// ? GOOD - Load only what's needed
var orders = await _context.Orders
    .Include(o => o.Customer)
    .Include(o => o.OrderDetails)
    .ToListAsync();

// ? BETTER - Use projection
var orders = await _context.Orders
    .Select(o => new {
        o.Id,
        o.Date,
        o.TotalAmount,
        CustomerName = o.Customer.Name
    })
    .ToListAsync();
```

---

### **Date Range Queries**
```csharp
// ? Use extension method
var orders = _context.Orders
    .ApplyDateRange(o => o.Date, startDate, endDate)
    .ToListAsync();

// Or manually (make sure to use indexes!)
var orders = await _context.Orders
    .Where(o => o.Date >= startDate && o.Date <= endDate)
    .ToListAsync();
```

---

## **?? Performance Monitoring**

### **Check Logs for Slow Requests**
```
? Normal: < 3 seconds
?? Warning: 3-10 seconds (optimize query)
?? Critical: > 10 seconds (major issue)
```

### **Enable Detailed Logging (Development Only)**
```json
// appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

---

## **?? Debugging Queries**

### **See Generated SQL**
```csharp
var query = _context.Orders
    .Where(o => o.Status == "Completed");

var sql = query.ToQueryString(); // EF Core 5.0+
Console.WriteLine(sql);
```

### **Count Queries (Should be minimal)**
```csharp
// Add to Program.cs (Development only)
builder.Services.AddDbContext<AppDbContext>(options => {
    options.UseSqlite(connectionString)
        .LogTo(Console.WriteLine, LogLevel.Information)
        .EnableSensitiveDataLogging();
});
```

---

## **? Code Review Checklist**

Before committing query changes:

- [ ] Uses pagination (`ToPagedResultAsync`)
- [ ] No `.ToList()` before `.Where()` filtering
- [ ] Uses projection (`.Select()`) for read-only data
- [ ] Caches repeated calculations
- [ ] Uses indexed columns in WHERE clauses
- [ ] Tests with 1000+ records
- [ ] No N+1 queries (check with logging)

---

## **?? Common Mistakes**

### **1. Loading Everything**
```csharp
// ? Loads ALL orders into memory
var orders = await _context.Orders.ToListAsync();
var completed = orders.Where(o => o.Status == "Completed");

// ? Filters in database
var completed = await _context.Orders
    .Where(o => o.Status == "Completed")
    .ToListAsync();
```

### **2. Forgetting Pagination**
```csharp
// ? No pagination - crashes with large data
public async Task<IActionResult> Index()
{
    var orders = await _context.Orders.ToListAsync();
    return View(orders);
}

// ? Paginated - scales to millions
public async Task<IActionResult> Index(int page = 1)
{
    var orders = await _context.Orders
        .ToPagedResultAsync(page, 50);
    return View(orders);
}
```

### **3. Over-including**
```csharp
// ? Loads too much data
var order = await _context.Orders
    .Include(o => o.Customer)
        .ThenInclude(c => c.Orders)
            .ThenInclude(o => o.OrderDetails)
    .FirstOrDefaultAsync(o => o.Id == id);

// ? Load only what's displayed
var order = await _context.Orders
    .Include(o => o.Customer)
    .Include(o => o.OrderDetails)
    .FirstOrDefaultAsync(o => o.Id == id);
```

---

## **?? Performance Targets**

| Operation | Target | Alert At |
|-----------|--------|----------|
| Page Load | < 2s | > 3s |
| API Call | < 500ms | > 1s |
| Report Generation | < 10s | > 30s |
| Database Query | < 100ms | > 500ms |

---

## **?? Quick Fixes**

### **Page Loading Slow?**
1. Check `PerformanceMonitoringMiddleware` logs
2. Enable EF query logging
3. Look for N+1 queries
4. Add missing indexes
5. Use caching

### **Report Timeout?**
1. Add pagination to query
2. Use projection (`.Select()`)
3. Move to background job (Hangfire)
4. Add query timeout extension

### **Memory Issues?**
1. Verify pagination is used
2. Check for `.ToList()` before filtering
3. Use projections instead of full entities
4. Clear cache if too large

---

## **?? Quick Links**

- Full Guide: `docs/PERFORMANCE_OPTIMIZATION_SUMMARY.md`
- Migration: `docs/DATABASE_MIGRATION_GUIDE.md`
- Production: `docs/PRODUCTION_READINESS_REPORT.md`
- EF Core Docs: https://learn.microsoft.com/ef/core/

---

**Print this and keep it handy!** ???

*Last Updated: January 2025*
