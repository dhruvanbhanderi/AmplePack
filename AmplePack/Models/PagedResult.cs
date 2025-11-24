using System;
using System.Collections.Generic;

namespace AmplePack.Models
{
    /// <summary>
    /// Generic paged result container for list operations
    /// Prevents memory exhaustion by loading data in chunks
    /// </summary>
    public class PagedResult<T>
    {
        public List<T> Data { get; set; } = new List<T>();
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public int StartRecord => (CurrentPage - 1) * PageSize + 1;
        public int EndRecord => Math.Min(CurrentPage * PageSize, TotalRecords);
    }
}
