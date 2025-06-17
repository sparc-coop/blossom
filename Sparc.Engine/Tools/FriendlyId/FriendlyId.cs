
namespace Sparc.Engine;

public class FriendlyId(IWebHostEnvironment env)
{
    public string WordsPath { get; } = Path.Combine(env.ContentRootPath, "Tools/FriendlyId/words_alpha.txt");
    public IEnumerable<string> UnsafeWords { get; } = File.ReadLines(Path.Combine(env.ContentRootPath, "Tools/FriendlyId/words_officesafe.txt"));

    public string Create(int wordCount = 2, int numberCount = 0)
    {
        var words = Enumerable.Range(0, wordCount).Select(_ => GetRandomWord()).ToList();
        var numbers = Enumerable.Range(0, numberCount).Select(_ => new Random().Next(10)).Select(n => n.ToString()).ToList();
        
        var all = words.Concat(numbers).ToList();
        return string.Join("-", words) + string.Join("", numbers);
    }

    string GetRandomWord()
    {
        var random = new Random();
        var word = File.ReadLines(WordsPath)
            .Skip(random.Next(370000))
            .First()
            .Trim()
            .ToLower();

        // Check against office-unsafe words
        if (UnsafeWords.Any(x => x.ToLower() == word))
            return GetRandomWord();

        return word;
    }
}
