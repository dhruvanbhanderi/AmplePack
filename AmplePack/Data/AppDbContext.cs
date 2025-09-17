using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using AmplePack.Models;

namespace AmplePack.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Inventory> Inventories { get; set; }
        public DbSet<CustomerProduct> CustomerProducts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure decimal properties (database-agnostic)
            modelBuilder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderDetail>()
                .Property(od => od.PricePerBox)
                .HasPrecision(18, 2);

            // Configure Inventory properties
            modelBuilder.Entity<Inventory>()
                .Property(i => i.AvailableQuantity)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Inventory>()
                .Property(i => i.Quantity)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Inventory>()
                .Property(i => i.ReorderLevel)
                .HasPrecision(10, 2);

            modelBuilder.Entity<Inventory>()
                .Property(i => i.UnitPrice)
                .HasPrecision(18, 2);

            // Configure CustomerProduct decimal properties
            modelBuilder.Entity<CustomerProduct>()
                .Property(cp => cp.Length)
                .HasPrecision(10, 2);

            modelBuilder.Entity<CustomerProduct>()
                .Property(cp => cp.Width)
                .HasPrecision(10, 2);

            modelBuilder.Entity<CustomerProduct>()
                .Property(cp => cp.Height)
                .HasPrecision(10, 2);

            modelBuilder.Entity<CustomerProduct>()
                .Property(cp => cp.PricePerBox)
                .HasPrecision(18, 2);

            // Configure relationships
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CustomerId);

            // Configure CustomerProduct relationships
            modelBuilder.Entity<CustomerProduct>()
                .HasOne(cp => cp.Customer)
                .WithMany(c => c.CustomerProducts)
                .HasForeignKey(cp => cp.CustomerId);

            // Configure OrderDetail relationships
            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(od => od.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.CustomerProduct)
                .WithMany()
                .HasForeignKey(od => od.CustomerProductId)
                .OnDelete(DeleteBehavior.SetNull); // Changed to SetNull for SQLite compatibility

            // Configure indexes for better performance
            modelBuilder.Entity<CustomerProduct>()
                .HasIndex(cp => cp.CustomerId);

            modelBuilder.Entity<CustomerProduct>()
                .HasIndex(cp => new { cp.CustomerId, cp.ProductName })
                .IsUnique();

            modelBuilder.Entity<OrderDetail>()
                .HasIndex(od => od.CustomerProductId);
        }
    }
}