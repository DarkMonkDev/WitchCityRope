using System.Collections.Generic;

namespace WitchCityRope.Core.Models
{
    /// <summary>
    /// Represents a paginated result set
    /// </summary>
    /// <typeparam name="T">The type of items in the result set</typeparam>
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}