namespace Sparc.Blossom;

public class BlossomQueryResult<T>(IEnumerable<T> items, int totalCount)
{
    public ICollection<T> Items { get; set; } = items.ToList();
    public int TotalCount { get; } = totalCount;
}
