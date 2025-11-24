# Database Migration Guide - SQLite to SQL Server

## ?? CRITICAL: SQLite is NOT recommended for production factory use

### Why Migrate?

**SQLite Limitations:**
- File-based locking (no concurrent writes)
- Maximum practical size: 2-4 GB
- No advanced indexing strategies
- Limited concurrency support
- No connection pooling

**SQL Server Benefits:**
- Handles millions of records
- Concurrent read/write operations
- Advanced query optimization
- Full ACID compliance
- Connection pooling
- Backup & recovery tools

---

## Migration Steps

### 1. Install SQL Server Package

```bash
# Remove SQLite package
dotnet remove package Microsoft.EntityFrameworkCore.Sqlite

# Add SQL Server package
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

### 2. Update appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=AmplePack;Trusted_Connection=True;MultipleActiveResultSets=true"
  }
}
```

**For Production (Azure SQL/SQL Server):**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server.database.windows.net;Database=AmplePack;User Id=your-user;Password=your-password;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  }
}
```

### 3. Update Program.cs

**Current (SQLite):**
```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Data Source=AmplePack.sqlite"));
```

**New (SQL Server):**
```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptionsAction: sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
            sqlOptions.CommandTimeout(60); // 60 seconds for large queries
        }));
```

### 4. Create Migration

```bash
# Remove existing migrations (if any)
rm -rf Migrations/

# Create new migration for SQL Server
dotnet ef migrations add InitialCreate --project AmplePack

# Apply migration
dotnet ef database update --project AmplePack
```

### 5. Data Migration (If You Have Existing Data)

**Option A: Manual Export/Import**
```bash
# Export from SQLite
sqlite3 AmplePack.sqlite .dump > backup.sql

# Import to SQL Server (use SQL Server Management Studio)
# Or use a tool like:
# - SQLite to SQL Server migration tool
# - DbUp for scripted migrations
```

**Option B: Code-based Migration**
```csharp
// Create a migration service
public class DataMigrationService
{
    public async Task MigrateSqliteToSqlServer(
        AppDbContext sqliteContext, 
        AppDbContext sqlServerContext)
    {
        // Migrate Customers
        var customers = await sqliteContext.Customers.ToListAsync();
        sqlServerContext.Customers.AddRange(customers);
        await sqlServerContext.SaveChangesAsync();
        
        // Migrate Orders (with FK relationships)
        var orders = await sqliteContext.Orders.ToListAsync();
        sqlServerContext.Orders.AddRange(orders);
        await sqlServerContext.SaveChangesAsync();
        
        // Continue for other tables...
    }
}
```

---

## Performance Configuration

### AppDbContext Optimizations

```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    if (optionsBuilder.IsConfigured) return;
    
    optionsBuilder
        .UseSqlServer(connectionString, options =>
        {
            options.MaxBatchSize(100); // Batch inserts for performance
            options.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        })
        .EnableSensitiveDataLogging(false) // Production: false
        .EnableDetailedErrors(false) // Production: false
        .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking); // Read-only queries
}
```

### Connection Pooling (Automatic in SQL Server)

SQL Server automatically handles connection pooling. Verify in connection string:
```
MultipleActiveResultSets=true;Min Pool Size=5;Max Pool Size=100;
```

---

## Alternative: PostgreSQL (Open Source)

### Why PostgreSQL?

- Free and open source
- Excellent performance
- Strong community support
- Advanced features (JSON, full-text search)

### Setup PostgreSQL

```bash
# Install package
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL

# Update Program.cs
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

# Connection string format
"Host=localhost;Database=AmplePack;Username=postgres;Password=your-password"
```

---

## Testing Before Production

### 1. Test with LocalDB (SQL Server)

```bash
# Install SQL Server LocalDB
# Download from: https://learn.microsoft.com/sql/database-engine/configure-windows/sql-server-express-localdb

# Test connection
sqlcmd -S "(localdb)\mssqllocaldb" -Q "SELECT @@VERSION"
```

### 2. Load Testing

```csharp
// Create sample data
public async Task SeedLargeDataset(AppDbContext context)
{
    // Create 10,000 customers
    var customers = Enumerable.Range(1, 10000)
        .Select(i => new Customer 
        { 
            Name = $"Customer {i}",
            Email = $"customer{i}@example.com"
        })
        .ToList();
    
    // Batch insert (SQL Server optimization)
    context.Customers.AddRange(customers);
    await context.SaveChangesAsync();
    
    // Create 100,000 orders
    // ...
}
```

### 3. Performance Benchmarks

Run these queries before and after migration:

```sql
-- Test 1: Order count by customer
SELECT CustomerId, COUNT(*) 
FROM Orders 
GROUP BY CustomerId 
ORDER BY COUNT(*) DESC;

-- Test 2: Revenue aggregation
SELECT YEAR(Date) as Year, MONTH(Date) as Month, SUM(TotalAmount) as Revenue
FROM Orders
WHERE Status = 'Completed'
GROUP BY YEAR(Date), MONTH(Date)
ORDER BY Year DESC, Month DESC;

-- Test 3: Complex join
SELECT c.Name, COUNT(o.Id) as OrderCount, SUM(o.TotalAmount) as TotalRevenue
FROM Customers c
LEFT JOIN Orders o ON c.Id = o.CustomerId
GROUP BY c.Name
ORDER BY TotalRevenue DESC;
```

---

## Rollback Plan

If migration fails:

1. **Keep SQLite database as backup**
```bash
cp AmplePack.sqlite AmplePack.sqlite.backup
```

2. **Revert Program.cs changes**

3. **Remove SQL Server migrations**
```bash
dotnet ef migrations remove
```

4. **Restore SQLite configuration**

---

## Production Deployment Checklist

- [ ] Database server is properly configured
- [ ] Connection string is in secure storage (Azure Key Vault, environment variables)
- [ ] Indexes are created (check AppDbContext.OnModelCreating)
- [ ] Connection pooling is enabled
- [ ] Backup strategy is in place
- [ ] Monitoring/logging is configured
- [ ] Load testing completed successfully
- [ ] Rollback plan is documented
- [ ] Team is trained on new database

---

## Next Steps

After migration, implement:

1. **Caching Layer** (Already implemented: CacheService)
2. **Background Jobs** (Recommended: Hangfire)
3. **API Rate Limiting**
4. **Health Checks**
5. **Database Monitoring**

---

## Support Resources

- SQL Server Documentation: https://learn.microsoft.com/sql/
- EF Core Migrations: https://learn.microsoft.com/ef/core/managing-schemas/migrations/
- PostgreSQL Alternative: https://www.postgresql.org/docs/

---

**Estimated Migration Time:** 2-4 hours (depending on data size)

**Recommended Approach:** Test on staging environment first, then production
