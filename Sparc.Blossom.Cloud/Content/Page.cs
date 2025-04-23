using System.Text;

namespace Kori;

public record SourceContent(string PageId, string ContentId);
public record TranslateContentRequest(Dictionary<string, string> ContentDictionary, bool AsHtml, string LanguageId);
public record TranslateContentResponse(string Domain, string Path, string Id, string Language, Dictionary<string, Content> Content);

public class Page : BlossomEntity<string>
{
    public string Domain { get; private set; }
    public string Path { get; private set; }
    public string Name { get; private set; }
    public SourceContent? SourceContent { get; private set; }
    public List<Language> Languages { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? LastActiveDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public AudioContent? Audio { get; private set; }
    private ICollection<Content> Contents { get; set; } = [];

    internal Page(string pageId)
    {
        Id = pageId;
        Domain = new Uri(pageId).Host;
        Path = new Uri(pageId).AbsolutePath;
        Name = "New Page";
        Languages = [];
        StartDate = DateTime.UtcNow;
        LastActiveDate = DateTime.UtcNow;
    }

    private Page(string domain, string path)
    {
        Id = new Uri(new Uri(domain), path).ToString();
        Domain = domain;
        Path = path;
        Name = "New Page";
        Languages = [];
        StartDate = DateTime.UtcNow;
        LastActiveDate = DateTime.UtcNow;
    }

    public Page(string domain, string path, string name) : this(domain, path)
    {
        Name = name;
    }

    public Page(Uri uri, string name) : this(uri.Host, uri.AbsolutePath, name)
    {
    }

    internal Page(Page page, Content content) : this(page.Domain, page.Path, page.Name)
    {
        // Create a subpage from a message

        SourceContent = new(page.Id, content.Id);
        //Languages = page.Languages;
        //ActiveUsers = page.ActiveUsers;
        //Translations = page.Translations;
    }

    public void AddLanguage(Language language)
    {
        if (Languages.Any(x => x.Id == language.Id))
            return;

        Languages.Add(language);
    }

    internal async Task<ICollection<Content>> TranslateAsync(Language toLanguage, KoriTranslatorProvider provider)
    {
        var needsTranslation = Contents.Where(x => !x.HasTranslation(toLanguage)).ToList();
        if (needsTranslation.Count == 0)
            return Contents;

        var languages = needsTranslation.GroupBy(x => x.Language);
        foreach (var language in languages)
        {
            var translator = await provider.For(language.Key, toLanguage);
            if (translator == null)
                continue;

            var translatedContents = await translator.TranslateAsync(language, toLanguage);
            foreach (var translatedContent in translatedContents)
            {
                var existing = needsTranslation.FirstOrDefault(x => x.Id == translatedContent.SourceContentId);
                existing?.AddTranslation(translatedContent);
            }
        }

        return Contents;
    }

    internal async Task SpeakAsync(ISpeaker speaker, List<Content> contents)
    {
        Audio = await speaker.SpeakAsync(contents);
    }

    internal void Close()
    {
        EndDate = DateTime.UtcNow;
    }
    
    internal void UpdateName(string name) => Name = name;

    internal Task<ICollection<Content>> LoadOriginalContentAsync(IRepository<Content> repository)
    {
        Contents = repository.Query.Where(x => x.PageId == Id && x.SourceContentId == null).ToList();
        return Task.FromResult(Contents);
    }

    internal async Task<IEnumerable<Content>> LoadContentAsync(Language language, IRepository<Content> repository, KoriTranslatorProvider translator)
    {
        await LoadOriginalContentAsync(repository);
        var translatedContent = await TranslateAsync(language, translator);
        Contents = Contents.Union(translatedContent).ToList();
        return Contents;
    }
}

