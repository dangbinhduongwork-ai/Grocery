namespace GroceryHub.SharedKernel;

public readonly record struct PageRequest
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 24;
    public const int MaxPageSize = 100;

    public PageRequest(int page = DefaultPage, int pageSize = DefaultPageSize)
    {
        Page = page < 1 ? DefaultPage : page;
        PageSize = Math.Clamp(pageSize <= 0 ? DefaultPageSize : pageSize, 1, MaxPageSize);
    }

    public int Page { get; }

    public int PageSize { get; }

    public int Skip => (Page - 1) * PageSize;
}

public sealed record PagedList<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages)
{
    public static PagedList<T> Create(IReadOnlyList<T> items, int page, int pageSize, int totalItems)
    {
        ArgumentNullException.ThrowIfNull(items);
        PageRequest normalized = new(page, pageSize);
        int normalizedTotal = Math.Max(0, totalItems);
        int totalPages = normalizedTotal == 0
            ? 0
            : (int)Math.Ceiling(normalizedTotal / (double)normalized.PageSize);

        return new PagedList<T>(items, normalized.Page, normalized.PageSize, normalizedTotal, totalPages);
    }

    public static PagedList<T> Empty(int page = PageRequest.DefaultPage, int pageSize = PageRequest.DefaultPageSize) =>
        Create([], page, pageSize, 0);
}
