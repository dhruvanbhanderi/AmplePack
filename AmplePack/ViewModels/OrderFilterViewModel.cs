using System.ComponentModel.DataAnnotations;

namespace AmplePack.ViewModels
{
    public class OrderFilterViewModel
    {
        public string? SearchTerm { get; set; }
        public string? Status { get; set; }
        public int? CustomerId { get; set; }
        public string? Priority { get; set; }
        
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }
        
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }
        
        public string? DatePeriod { get; set; } // "thisMonth", "lastMonth", "thisQuarter", "custom"
        
        // Sorting
        public string? SortBy { get; set; } = "Date";
        public string? SortOrder { get; set; } = "desc";
        public string? SecondarySortBy { get; set; }
        public string? SecondarySortOrder { get; set; }
        
        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        
        // Export options
        public string? ExportFormat { get; set; } // "excel", "pdf", "csv"
        public string[]? SelectedColumns { get; set; }
        
        // Quick filters
        public bool ShowOnlyMyOrders { get; set; }
        public bool ShowHighPriority { get; set; }
        public bool ShowOverdue { get; set; }
    }

    public class OrderListViewModel
    {
        public List<OrderSummaryDto> Orders { get; set; } = new();
        public OrderFilterViewModel Filter { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageCount { get; set; }
        public OrderStatsDto Stats { get; set; } = new();
        public List<CustomerSummaryDto> Customers { get; set; } = new();
    }

    public class OrderSummaryDto
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int ItemCount { get; set; }
        public string ItemsSummary { get; set; } = string.Empty;
        public string Priority { get; set; } = "Normal";
        public DateTime? DeliveryDate { get; set; }
        public bool IsOverdue { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class OrderStatsDto
    {
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int ProcessingOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }
        public int OverdueOrders { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public decimal YearlyRevenue { get; set; }
        public decimal AverageOrderValue { get; set; }
        public List<OrderTrendDto> OrderTrends { get; set; } = new();
        public List<TopCustomerDto> TopCustomers { get; set; } = new();
    }

    public class OrderTrendDto
    {
        public DateTime Date { get; set; }
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class TopCustomerDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal TotalValue { get; set; }
    }

    public class CustomerSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public DateTime? LastOrderDate { get; set; }
        public decimal TotalValue { get; set; }
        public List<RecentOrderDto> RecentOrders { get; set; } = new();
    }

    public class RecentOrderDto
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
    }
}