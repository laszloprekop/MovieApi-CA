namespace MovieCore.DTOs;

public record PaginationMeta(int Page, int PageSize, int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public record PagedResult<T>(IReadOnlyList<T> Data, PaginationMeta Meta);