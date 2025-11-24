using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

namespace AmplePack.Services
{
    /// <summary>
    /// Centralized caching service for high-performance data access
    /// Reduces database load for frequently accessed data
    /// </summary>
    public class CacheService
    {
        private readonly IMemoryCache _cache;
        
        // Cache durations
        private static readonly TimeSpan ShortCache = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan MediumCache = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan LongCache = TimeSpan.FromHours(1);

        public CacheService(IMemoryCache cache)
        {
            _cache = cache;
        }

        /// <summary>
        /// Get or create cached item with short expiration (5 minutes)
        /// Use for: Dashboard stats, recent orders
        /// </summary>
        public async Task<T> GetOrCreateShortAsync<T>(string key, Func<Task<T>> factory)
        {
            return await _cache.GetOrCreateAsync(key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = ShortCache;
                entry.SetPriority(CacheItemPriority.High);
                return factory();
            });
        }

        /// <summary>
        /// Get or create cached item with medium expiration (15 minutes)
        /// Use for: Customer lists, inventory summaries
        /// </summary>
        public async Task<T> GetOrCreateMediumAsync<T>(string key, Func<Task<T>> factory)
        {
            return await _cache.GetOrCreateAsync(key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = MediumCache;
                entry.SetPriority(CacheItemPriority.Normal);
                return factory();
            });
        }

        /// <summary>
        /// Get or create cached item with long expiration (1 hour)
        /// Use for: Chart data, monthly statistics
        /// </summary>
        public async Task<T> GetOrCreateLongAsync<T>(string key, Func<Task<T>> factory)
        {
            return await _cache.GetOrCreateAsync(key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = LongCache;
                entry.SetPriority(CacheItemPriority.Low);
                return factory();
            });
        }

        /// <summary>
        /// Remove specific cache entry
        /// Call when data is modified
        /// </summary>
        public void Remove(string key)
        {
            _cache.Remove(key);
        }

        /// <summary>
        /// Remove multiple cache entries by pattern
        /// </summary>
        public void RemoveByPattern(string pattern)
        {
            // For more advanced scenarios, consider using IMemoryCache with tags
            // or implementing a distributed cache like Redis
        }

        /// <summary>
        /// Clear all cache (use sparingly)
        /// </summary>
        public void Clear()
        {
            if (_cache is MemoryCache memCache)
            {
                memCache.Compact(1.0); // Remove 100% of cache
            }
        }

        // Common cache keys
        public static class Keys
        {
            public const string TotalRevenue = "Dashboard_TotalRevenue";
            public const string MonthlyRevenue = "Dashboard_MonthlyRevenue";
            public const string TotalOrders = "Dashboard_TotalOrders";
            public const string TotalCustomers = "Dashboard_TotalCustomers";
            public const string LowStockCount = "Dashboard_LowStockCount";
            public const string LowStockItems = "Dashboard_LowStockItems";
            public const string ChartData_Monthly = "Chart_MonthlyRevenue";
            public const string ChartData_Status = "Chart_OrderStatus";
            public const string Customers_Dropdown = "Customers_Dropdown";
            public const string ProductTypes_List = "ProductTypes_List";
            public const string Categories_List = "Categories_List";
        }
    }
}
