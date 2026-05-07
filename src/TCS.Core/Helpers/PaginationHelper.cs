namespace TCS.Core.Helpers;

public static class PaginationHelper
{
    public static Common.PagedResult<T> Paginate<T>(
        IReadOnlyList<T> allItems, int page, int pageSize, string? searchString = null)
    {
        var total = allItems.Count;
        var items = allItems.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new Common.PagedResult<T>
        {
            Items = items,
            TotalItems = total,
            TotalPages = (int)Math.Ceiling((double)total / pageSize),
            CurrentPage = page,
            PageSize = pageSize,
            SearchString = searchString
        };
    }
}
