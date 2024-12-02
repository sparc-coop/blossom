namespace Sparc.Blossom.Api;

public class BlossomQueryOptions(int skip = 0, int? take = null, string? searchTerm = null)
{
    public int Skip { get; } = skip;
    public int? Take { get; } = take;
    public string? SearchTerm { get; } = searchTerm;
}
