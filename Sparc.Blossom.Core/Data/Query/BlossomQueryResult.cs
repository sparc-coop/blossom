namespace Sparc.Blossom.Api;

public class BlossomQueryResult<T>(IEnumerable<T> items, int totalCount)
{
    public ICollection<T> Items { get; set; } = items.ToList();
    public int TotalCount { get; } = totalCount;
    public BlossomAggregateMetadata? Metadata { get; set; } = new(typeof(T));
}
