namespace Sparc.Blossom.Cloud.Tools;

public class FriendlyUsername(IWebHostEnvironment env)
{
    public IEnumerable<string> NamesPath { get; } = File.ReadLines(Path.Combine(env.ContentRootPath, "Tools/FriendlyUsername/random_usernames.txt"));

    public string GetRandomName()
    {
        var lines = NamesPath.ToArray();
        if (lines.Length == 0)
            throw new InvalidOperationException("No usernames found.");

        var random = new Random();
        var index = random.Next(lines.Length);
        return lines[index];
    }
}
