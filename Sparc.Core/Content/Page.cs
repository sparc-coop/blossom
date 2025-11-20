using Sparc.Core;
using System.Text.Json.Serialization;

namespace Sparc.Blossom.Content;

public record SourceContent(string PageId, string ContentId);

public class ContentSpace : BlossomEntity<string>
{
    public string Domain { get; set; }

    [JsonConstructor]
    protected ContentSpace()
    {
        Id = string.Empty;
        Domain = string.Empty;
    }

    public ContentSpace(string domain)
    {
        Domain = domain;
    }
}

public class Page : ContentSpace
{
    public string Path { get; set; }
    public string Name { get; set; }
    public SourceContent? SourceContent { get; set; }
    public Dictionary<string, int> TovikUsage { get; set; } = [];
    public Dictionary<string, int> Visits { get; set; } = [];
    public DateTime DateRegistered { get; set; } = DateTime.UtcNow;
    public DateTime? LastActiveDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public AudioContent? Audio { get; set; }
    private ICollection<TextContent> Contents { get; set; } = [];

    [JsonConstructor]
    private Page() : base()
    { 
        Id = string.Empty;
        Domain = string.Empty;
        Path = string.Empty;
        Name = string.Empty;
    }

    private Page(string domain, string path) : base(domain)
    {
        Id = BlossomHash.MD5($"{domain}:{path}");
        Domain = domain;
        Path = path;
        Name = Id;
    }

    public Page(string domain, string path, string name) : this(domain, path)
    {
        Name = name;
    }

    public Page(TextContent content) : this(content.Domain, content.Path)
    { }

    public Page(Uri uri, string name) : this(uri.Host, uri.AbsolutePath, name)
    {
    }

    internal Page(Page page, TextContent content) : this(page.Domain, page.Path, page.Name)
    {
        // Create a subpage from a message

        SourceContent = new(page.Id, content.Id);
        //Languages = page.Languages;
        //ActiveUsers = page.ActiveUsers;
        //Translations = page.Translations;
    }

    public void RegisterTovikUsage(TovikContentTranslated content)
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

    //internal async Task<ICollection<TextContent>> TranslateAsync(Language toLanguage, BlossomTranslator provider)
    //{
    //    var needsTranslation = Contents.Where(x => !x.HasTranslation(toLanguage)).ToList();
    //    if (needsTranslation.Count == 0)
    //        return Contents;

    //    var languages = needsTranslation.GroupBy(x => x.Language);
    //    foreach (var language in languages)
    //    {
    //        var translator = await provider.For(language.Key, toLanguage);
    //        if (translator == null)
    //            continue;

    //        var translatedContents = await translator.TranslateAsync(language, toLanguage);
    //        foreach (var translatedContent in translatedContents)
    //        {
    //            var existing = needsTranslation.FirstOrDefault(x => x.Id == translatedContent.SourceContentId);
    //            existing?.AddTranslation(translatedContent);
    //        }
    //    }

    //    return Contents;
    //}

    public string AbsolutePath(string? language = null) => $"https://{Domain}{Path}" + (language == null ? "" : $"?lang={language}");

    internal async Task SpeakAsync(ISpeaker speaker, List<TextContent> contents)
    {
        Audio = await speaker.SpeakAsync(contents);
    }

    internal void Close()
    {
        EndDate = DateTime.UtcNow;
    }
    
    public void UpdateName(string name) => Name = name;

    public Task<ICollection<TextContent>> LoadOriginalContentAsync(IRepository<TextContent> repository)
    {
        Contents = repository.Query.Where(x => x.Path == Id && x.SourceContentId == null).ToList();
        return Task.FromResult(Contents);
    }

    //public async Task<IEnumerable<TextContent>> LoadContentAsync(Language language, IRepository<TextContent> repository, BlossomTranslator translator)
    //{
    //    await LoadOriginalContentAsync(repository);
    //    var translatedContent = await TranslateAsync(language, translator);
    //    Contents = Contents.Union(translatedContent).ToList();
    //    return Contents;
    //}
}

