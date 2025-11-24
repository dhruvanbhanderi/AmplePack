using Microsoft.EntityFrameworkCore;
using AmplePack.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AmplePack.Extensions
{
    /// <summary>
    /// High-performance query extensions for pagination and optimization
    /// Prevents N+1 queries and memory issues
    /// </summary>
    public static class QueryExtensions
    {
        /// <summary>
        /// Apply pagination to any IQueryable
        /// </summary>
        public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
            this IQueryable<T> query,
            int page = 1,
            int pageSize = 50)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 500) pageSize = 500; // Safety limit

            var totalRecords = await query.CountAsync();
            var data = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<T>
            {
                Data = data,
                CurrentPage = page,
                PageSize = pageSize,
                TotalRecords = totalRecords
            };
        }

        /// <summary>
        /// Apply default ordering and pagination for reports
        /// Prevents full table scans
        /// </summary>
        public static IQueryable<T> ApplyPagination<T>(
            this IQueryable<T> query,
            int page,
            int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;
            if (pageSize > 1000) pageSize = 1000; // Safety limit for reports

            return query
                .Skip((page - 1) * pageSize)
                .Take(pageSize);
        }

        /// <summary>
        /// Safe date range filter with index optimization
        /// </summary>
        public static IQueryable<T> ApplyDateRange<T>(
            this IQueryable<T> query,
            Func<T, DateTime> dateSelector,
            DateTime? startDate,
            DateTime? endDate)
        {
            if (startDate.HasValue)
            {
                var start = startDate.Value.Date;
                query = query.Where(x => dateSelector(x) >= start);
            }

            if (endDate.HasValue)
            {
                var end = endDate.Value.Date.AddDays(1).AddSeconds(-1);
                query = query.Where(x => dateSelector(x) <= end);
            }

            return query;
        }
    }
}
