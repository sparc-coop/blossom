
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Data;

namespace Sparc.Engine;

public class Contents(BlossomAggregateOptions<TextContent> options, KoriTranslator translator, SparcEngineAuthenticator<BlossomUser> auth) 
    : BlossomAggregate<TextContent>(options), IBlossomEndpoints
{
    public BlossomQuery<TextContent> Search(string searchTerm) => Query().Where(content =>
         ((content.Text != null && content.Text.ToLower().Contains(searchTerm) == true) ||
         (content.OriginalText != null && content.OriginalText.ToLower().Contains(searchTerm) == true) ||
         (content.Domain != null && content.Domain.ToLower().Contains(searchTerm) == true) ||
         (content.Path != null && content.Path.ToLower().Contains(searchTerm) == true)));

    //public async Task<IEnumerable<TextContent>> GetAll(string pageId, string? fallbackLanguageId = null)
    //{
    //    var language = User.Language(fallbackLanguageId);
    //    var page = await Repository.Query.FindAsync(pageId);

    //    if (language == null || page == null)
    //        return [];

    //    //var content = await page.LoadContentAsync(language, Repository, translator);
 

    //    return [];
    //}

    public async Task<TextContent> Get(TextContent content)
    {
        var user = await auth.GetAsync(User);
        var toLanguage = user?.Avatar.Language;

        var newId = TextContent.IdHash(content.Text, toLanguage);
        var existing = await Repository.Query
            .Where(x => x.Domain == content.Domain && x.Id == newId)
            .CosmosFirstOrDefaultAsync();

        if (existing != null)
            return existing;
        
        var translation = await translator.TranslateAsync(content, toLanguage);
        if (translation == null)
            throw new InvalidOperationException("Translation failed.");

        await Repository.AddAsync(translation);
        return translation;
    }

    public BlossomQuery<TextContent> All(string pageId) => Query().Where(content => content.PageId == pageId && content.SourceContentId == null);

    public async Task<IEnumerable<Language>> Languages()
        => await translator.GetLanguagesAsync();

    public void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("translate");
        group.MapPost("", async (HttpRequest request, TextContent content) => await Get(content));
        group.MapGet("languages", Languages).CacheOutput(x => x.Expire(TimeSpan.FromHours(1)));
    }
}
