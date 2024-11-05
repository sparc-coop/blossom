
namespace Sparc.Blossom.PublicTools.FriendlyId
{
    public class FriendlyId(IWebHostEnvironment env)
    {
        public string WordsPath { get; } = Path.Combine(env.ContentRootPath, "FriendlyId/words_alpha.txt");
        public IEnumerable<string> UnsafeWords { get; } = File.ReadLines(Path.Combine(env.ContentRootPath, "FriendlyId/words_officesafe.txt"));

        public string Create()
        {
            return $"{GetRandomWord()}-{GetRandomWord()}";
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
}
