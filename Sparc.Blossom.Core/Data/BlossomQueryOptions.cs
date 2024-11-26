namespace Sparc.Blossom.Api;

public class BlossomQueryOptions(int skip = 0, int? take = null)
{
    public int Skip { get; } = skip;
    public int? Take { get; } = take;
}

public class BlossomQueryResult<T>(IEnumerable<T> items, int totalCount)
{
    public ICollection<T> Items { get; set; } = items.ToList();
    public int TotalCount { get; } = totalCount;
}