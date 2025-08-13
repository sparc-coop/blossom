
namespace Sparc.Blossom.Authentication;

public class FriendlyId
{
    public string WordsPath { get; }
    public static List<string> UnsafeWords { get; private set; } = [];

    public FriendlyId(IWebHostEnvironment env)
    {
        WordsPath = Path.Combine(env.ContentRootPath, "Aura/FriendlyId/words_alpha.txt");
        UnsafeWords = File.ReadLines(Path.Combine(env.ContentRootPath, "Aura/FriendlyId/words_officesafe.txt"))
            .Select(x => x.ToLower())
            .ToList();
    }

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
        if (UnsafeWords.Any(x => x == word))
            return GetRandomWord();

        return word;
    }
}
