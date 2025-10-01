using System.ComponentModel.DataAnnotations;

namespace AmplePack.ViewModels
{
    public class OrderFilterViewModel
    {
        public string? CustomerFilter { get; set; }
        public string? StatusFilter { get; set; }
        
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }
        
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }
        
        public string? SearchTerm { get; set; }
        
        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        
        // Sorting
        public string SortBy { get; set; } = "Date";
        public string SortOrder { get; set; } = "desc";
        
        // Export format
        public string? ExportFormat { get; set; }
        
        // Filter presets
        public bool ShowActiveOnly { get; set; } = true;
        public bool ShowOverdue { get; set; } = false;
        
        // Date range presets
        public string? DateRange { get; set; } // "today", "week", "month", "year", "custom"
        
        public void ApplyDateRangePreset()
        {
            var today = DateTime.Today;
            
            switch (DateRange?.ToLower())
            {
                case "today":
                    StartDate = today;
                    EndDate = today;
                    break;
                case "week":
                    var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
                    StartDate = startOfWeek;
                    EndDate = startOfWeek.AddDays(6);
                    break;
                case "month":
                    StartDate = new DateTime(today.Year, today.Month, 1);
                    EndDate = StartDate.Value.AddMonths(1).AddDays(-1);
                    break;
                case "year":
                    StartDate = new DateTime(today.Year, 1, 1);
                    EndDate = new DateTime(today.Year, 12, 31);
                    break;
                case "last30":
                    StartDate = today.AddDays(-30);
                    EndDate = today;
                    break;
                case "last90":
                    StartDate = today.AddDays(-90);
                    EndDate = today;
                    break;
            }
        }
        
        public bool HasFilters()
        {
            return !string.IsNullOrEmpty(CustomerFilter) ||
                   !string.IsNullOrEmpty(StatusFilter) ||
                   StartDate.HasValue ||
                   EndDate.HasValue ||
                   !string.IsNullOrEmpty(SearchTerm) ||
                   ShowOverdue;
        }
        
        public void ClearFilters()
        {
            CustomerFilter = null;
            StatusFilter = null;
            StartDate = null;
            EndDate = null;
            SearchTerm = null;
            ShowOverdue = false;
            DateRange = null;
            Page = 1;
        }
    }
    
    public class OrderListViewModel
    {
        public List<AmplePack.Models.Order> Orders { get; set; } = new();
        public OrderFilterViewModel Filter { get; set; } = new();
        public OrderStatisticsViewModel Stats { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageCount { get; set; }
        public bool HasNextPage => Filter.Page < PageCount;
        public bool HasPreviousPage => Filter.Page > 1;
        
        // Quick stats for the current filter
        public decimal TotalValue => Orders.Sum(o => o.TotalAmount);
        public int FilteredCount => Orders.Count;
    }
    
    public class OrderStatisticsViewModel
    {
        // Overall counts
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int ProcessingOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }
        public int OverdueOrders { get; set; }
        
        // Financial metrics
        public decimal TotalRevenue { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public decimal YearlyRevenue { get; set; }
        public decimal AverageOrderValue { get; set; }
        
        // Trends
        public List<OrderTrendItem> OrderTrends { get; set; } = new();
        public List<CustomerOrderSummary> TopCustomers { get; set; } = new();
        
        // Growth metrics
        public decimal RevenueGrowth { get; set; }
        public decimal OrderGrowth { get; set; }
        
        // Performance indicators
        public double AverageProcessingTime { get; set; } // in days
        public decimal CompletionRate { get; set; } // percentage
    }
    
    public class OrderTrendItem
    {
        public DateTime Date { get; set; }
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
        public string Period { get; set; } = string.Empty; // "daily", "weekly", "monthly"
    }
    
    public class CustomerOrderSummary
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal TotalValue { get; set; }
        public DateTime LastOrderDate { get; set; }
    }
}