using Sparc.Blossom.Spaces;
using Sparc.Core;
using System.Text.Json.Serialization;

namespace Sparc.Blossom.Content;

public class Page : BlossomSpace
{
    public string Path { get; set; }
    public Dictionary<string, int> TovikUsage { get; set; } = [];
    public Dictionary<string, int> Visits { get; set; } = [];
    public AudioContent? Audio { get; set; }
    public string Domain { get { return SpaceId; } set { SpaceId = value; } }
    
    [JsonConstructor]
    private Page() : base()
    { 
        Id = string.Empty;
        SpaceId = string.Empty;
        Path = string.Empty;
        Name = string.Empty;
    }

    private Page(string domain, string path) : base(domain, path)
    {
        Id = BlossomKey.SHA256($"{domain}:{path}");
        SpaceId = domain;
        Path = path;
        Name = Id;
    }

    public Page(string domain, string path, string name) : this(domain, path)
    {
        Name = name;
    }

    public Page(TextContent content) : this(content.Domain, content.SpaceId)
    { }

    public Page(Uri uri, string name) : this(uri.Host, uri.AbsolutePath, name)
    {
    }

    public void RegisterTovikUsage(ContentPosted content)
    {
        if (TovikUsage.ContainsKey(content.Content.LanguageId))
            TovikUsage[content.Content.LanguageId] += content.TokenCount;
        else
            TovikUsage[content.Content.LanguageId] = content.TokenCount;
    }

    public void RegisterVisit(Language language)
    {
        LastActiveDate = DateTime.UtcNow;
        if (Visits.ContainsKey(language.Id))
            Visits[language.Id] += 1;
        else
            Visits[language.Id] = 1;
    }

    public string AbsolutePath(string? language = null) => $"https://{SpaceId}{Path}" + (language == null ? "" : $"?lang={language}");

    internal async Task SpeakAsync(ISpeaker speaker, List<TextContent> contents)
    {
        Audio = await speaker.SpeakAsync(contents);
    }

    internal void Close()
    {
        EndDate = DateTime.UtcNow;
    }
    
    public void UpdateName(string name) => Name = name;
}

