using System;
using System.Collections.Generic;
namespace ProviderCatalogService.DTOs;

public class PaginatedResponse<T>
{
    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages => (int)Math.Ceiling((double)TotalCount / (PageSize > 0 ? PageSize : 10));

    public bool HasPreviousPage => Page > 1;

    public bool HasNextPage => Page < TotalPages;

    public IEnumerable<T> Items { get; set; } = new List<T>();
}