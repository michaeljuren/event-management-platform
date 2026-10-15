using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace RaceDay.Api.Dtos;

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

/// <summary>Query-string paging parameters shared by list endpoints.</summary>
public class PageQuery
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}

public static class PagingExtensions
{
    public static async Task<PagedResult<TOut>> ToPagedResultAsync<TIn, TOut>(
        this IQueryable<TIn> query, PageQuery paging, Func<TIn, TOut> map, CancellationToken ct = default)
    {
        var total = await query.CountAsync(ct);
        var items = await query.Skip((paging.Page - 1) * paging.PageSize).Take(paging.PageSize).ToListAsync(ct);
        return new PagedResult<TOut>(items.Select(map).ToList(), paging.Page, paging.PageSize, total);
    }
}
