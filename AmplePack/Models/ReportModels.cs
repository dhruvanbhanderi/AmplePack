using System.ComponentModel.DataAnnotations;

namespace AmplePack.Models
{
    public class ExportFilterViewModel
    {
        [Display(Name = "Export Type")]
        public string ExportType { get; set; } = "orders"; // orders, invoices, customers, inventory, transactions

        [Display(Name = "Export Format")]
        public string ExportFormat { get; set; } = "pdf"; // pdf only

        [Display(Name = "Date From")]
        [DataType(DataType.Date)]
        public DateTime? DateFrom { get; set; }

        [Display(Name = "Date To")]
        [DataType(DataType.Date)]
        public DateTime? DateTo { get; set; }

        [Display(Name = "Quick Date Range")]
        public string QuickDateRange { get; set; } = ""; // thismonth, lastmonth, thisquarter, custom

        [Display(Name = "Order Status")]
        public string[]? OrderStatus { get; set; }

        [Display(Name = "Customer")]
        public int? CustomerId { get; set; }

        [Display(Name = "Product Type")]
        public string? ProductType { get; set; }

        [Display(Name = "Priority")]
        public string? Priority { get; set; }

        // Inventory-specific filters
        [Display(Name = "Category")]
        public string? Category { get; set; }

        [Display(Name = "Stock Status")]
        public string? StockStatus { get; set; }

        [Display(Name = "Minimum Value")]
        public decimal? MinValue { get; set; }

        // Customer-specific filters
        [Display(Name = "Customer Status")]
        public string? CustomerStatus { get; set; }

        [Display(Name = "Minimum Orders")]
        public int? MinOrders { get; set; }

        [Display(Name = "Include Summary")]
        public bool IncludeSummary { get; set; } = true;

        [Display(Name = "Include Charts")]
        public bool IncludeCharts { get; set; } = false;

        // Column selection for customization
        public List<string> SelectedColumns { get; set; } = new List<string>();
        public Dictionary<string, string> AvailableColumns { get; set; } = new Dictionary<string, string>();
    }

    public class ReportExportRequest
    {
        public string ReportType { get; set; } = string.Empty;
        public string ExportFormat { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Dictionary<string, object> Filters { get; set; } = new Dictionary<string, object>();
        public List<string> SelectedColumns { get; set; } = new List<string>();
        public bool IncludeSummary { get; set; } = true;
    }

    public class OrderReportData
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int ItemCount { get; set; }
        public string ProductSummary { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerContact { get; set; } = string.Empty;
    }

    public class CustomerReportData
    {
        public int CustomerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public int TotalProducts { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime? LastOrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class InventoryReportData
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal AvailableQuantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal TotalValue { get; set; }
        public decimal ReorderLevel { get; set; }
        public bool IsLowStock { get; set; }
        public string StockStatus { get; set; } = string.Empty;
    }

    public class TransactionReportData
    {
        public int TransactionId { get; set; }
        public DateTime Date { get; set; }
        public string Type { get; set; } = string.Empty; // Order, Refund, Adjustment
        public string CustomerName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
    }

    public class ReportSummary
    {
        public int TotalRecords { get; set; }
        public decimal TotalAmount { get; set; }
        public string PeriodDescription { get; set; } = string.Empty;
        public DateTime GeneratedOn { get; set; } = DateTime.Now;
        public string GeneratedBy { get; set; } = string.Empty;
        public Dictionary<string, object> AdditionalMetrics { get; set; } = new Dictionary<string, object>();
    }

    public class EnhancedReportsViewModel
    {
        public ExportFilterViewModel Filter { get; set; } = new ExportFilterViewModel();
        public List<Customer> Customers { get; set; } = new List<Customer>();
        public List<string> AvailableStatuses { get; set; } = new List<string> { "Pending", "Processing", "Completed", "Cancelled" };
        public List<string> ProductTypes { get; set; } = new List<string>();
        public List<string> Categories { get; set; } = new List<string>();
        
        // Quick stats for the current period
        public decimal CurrentPeriodRevenue { get; set; }
        public int CurrentPeriodOrders { get; set; }
        public int NewCustomers { get; set; }
        public int LowStockItems { get; set; }
    }

    // Reports Index View Models
    public class ReportsViewModel
    {
        // Sales Reports
        public decimal TotalRevenue { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public int TotalOrdersThisMonth { get; set; }
        public List<MonthlyRevenueData> MonthlyRevenueChart { get; set; } = new();

        // Customer Reports
        public int TotalCustomers { get; set; }
        public int NewCustomersThisMonth { get; set; }
        public List<Customer> TopCustomers { get; set; } = new();

        // Inventory Reports
        public int TotalInventoryItems { get; set; }
        public int LowStockCount { get; set; }
        public decimal TotalInventoryValue { get; set; }
        public List<Inventory> CriticalStockItems { get; set; } = new();

        // Order Status Reports
        public int PendingOrders { get; set; }
        public int ProcessingOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }
    }

    public class MonthlyRevenueData
    {
        public string Month { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
    }
}