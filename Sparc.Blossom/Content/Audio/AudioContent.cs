namespace Sparc.Blossom.Content;

public record AudioContent(string? Url, long Duration, string Voice)
{
    public List<Word> Words { get; set; } = [];
}
