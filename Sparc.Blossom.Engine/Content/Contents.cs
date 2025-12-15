using Sparc.Blossom.Authentication;
using Sparc.Blossom.Data;
using System.Globalization;
using System.Security.Claims;

namespace Sparc.Blossom.Content;

public class Contents(
    BlossomAggregateOptions<TextContent> options,
    IEnumerable<ITranslator> translators,
    IRepository<SparcDomain> domains,
    ClaimsPrincipal principal,
    SparcAuthenticator<BlossomUser> auth) : BlossomAggregate<TextContent>(options), IBlossomEndpoints
{
    public Task<List<Language>> GetLanguagesAsync() => Task.FromResult(Language.All);

    async Task<Language?> GetLanguageAsync(string language)
    {
        var languages = await GetLanguagesAsync();
        return languages.FirstOrDefault(x => x.Id == language);
    }

    public async Task<TextContent> Get(TextContent content, TranslationOptions? options = null)
    {
        var existing = await Repository.FindAsync(content.Domain, content.Id);
        if (existing != null)
            return existing;

        var translations = await Get([content], options)
            ?? throw new InvalidOperationException("Translation failed.");

        await PublishAsync(translations);
        return translations.First();
    }

    public async Task<List<TextContent>> Get(List<TextContent> content, TranslationOptions? options = null)
    {
        if (options == null)
            return await GetAll(content);
        
        options ??= new TranslationOptions();
        if (options.OutputLanguage == null)
        {
            var user = await auth.GetAsync(principal);
            options.OutputLanguage = user?.Avatar.Language;
        }

        if (!await CanTranslate(content))
            throw new Exception("You've reached your translation limit!");

        var translator = translators.OrderBy(x => x.Priority).First();
        var translations = await translator.TranslateAsync(content, options);
        await PublishAsync(translations);
        return translations;
    }


    public async Task<List<TextContent>> GetAll(List<TextContent> contents)
    {
        var domain = contents.First().Domain;
        var ids = contents.Select(x => x.Id).ToList();

        var existing = await Repository.Query(domain).Where(x => ids.Contains(x.Id)).ToListAsync();
        return existing;
    }


    private async Task<bool> CanTranslate(List<TextContent> contents)
    {
        var domainName = contents.FirstOrDefault()?.Domain;
        if (string.IsNullOrWhiteSpace(domainName))
            return true;

        var domain = await domains.Query
            .Where(x => x.Domain == domainName)
            .FirstOrDefaultAsync();

        if (domain == null)
        {
            domain = new SparcDomain(domainName);
            await domains.AddAsync(domain);
        }

        if (domain != null && domain.TovikUsage <= 500)
            return true;

        return false;
    }

    public async Task<TextContent?> TranslateAsync(TextContent message, Language toLanguage, string? additionalContext = null)
        => (await Get([message], new() { OutputLanguage = toLanguage, AdditionalContext = additionalContext })).FirstOrDefault();

    public async Task<List<TextContent>> TranslateAsync(IEnumerable<TextContent> messages, TranslationOptions options)
    {
        // Preprocess URLs
        foreach (var content in messages.Where(x => x.Text?.StartsWith("http") == true))
        {
            var html = new HtmlTranslator(content.Text!);
            content.Text = await html.TranslateAsync();
        }

        var translator = translators.OrderBy(x => x.Priority).First();
        var result = await translator.TranslateAsync(messages.ToList(), options);
        return result;
    }

    public async Task<T?> TranslateAsync<T>(TextContent message, TranslationOptions options)
        => (await TranslateAsync<T>([message], options)).FirstOrDefault();

    public async Task<List<T>> TranslateAsync<T>(IEnumerable<TextContent> messages, TranslationOptions options)
    {
        options.Schema = new(typeof(T));
        var translator = translators.OrderBy(x => x.Priority).First();
        var result = await translator.TranslateAsync(messages.ToList(), options);
        return result.Cast<T>().ToList();
    }

    internal async Task<string?> TranslateAsync(string text, string fromLanguage, string toLanguage)
    {
        if (fromLanguage == toLanguage)
            return text;

        var language = await GetLanguageAsync(toLanguage)
            ?? throw new ArgumentException($"Language {toLanguage} not found");

        var from = await GetLanguageAsync(fromLanguage);
        var message = new TextContent("", "", from!, text);
        var result = await Get([message], new() { OutputLanguage = language });
        return result?.FirstOrDefault()?.Text;
    }

    internal ITranslator GetBestTranslator(Language fromLanguage, Language toLanguage)
    {
        return translators
            .OrderBy(x => x.Priority)
            .FirstOrDefault(x => x.CanTranslate(fromLanguage, toLanguage))
            ?? throw new Exception($"No translator found for {fromLanguage.Id} to {toLanguage.Id}");
    }

    internal async Task<Language> SetLanguage(Language language)
    {
        var user = await auth.GetAsync(principal);
        user.Avatar.Language = Language.Find(language.Id);
        await auth.UpdateAsync(principal, user.Avatar);
        return language;
    }

    internal static BlossomRegion? GetLocale(string languageClaim)
    {
        var languages = Language.IdsFrom(languageClaim);

        try
        {
            var locale = languages
                .Where(x => x.Contains('-'))
                .Select(x => x.Split('-').Last())
                .Select(region => new RegionInfo(region))
                .FirstOrDefault();

            return locale == null ? null : new(locale);
        }
        catch
        {
            return null;
        }
    }

    async Task PublishAsync(IEnumerable<TextContent> contents)
    {
        if (Repository is CosmosDbSimpleRepository<TextContent> cosmos)
            foreach (var content in contents)
                await cosmos.Publish(content);
    }

    internal static IEnumerable<IEnumerable<T>> Batch<T>(IEnumerable<T> items,
                                                       int maxItems)
    {
        return items.Select((item, inx) => new { item, inx })
                    .GroupBy(x => x.inx / maxItems)
                    .Select(g => g.Select(x => x.item));
    }

    private static TranslationOptions? DefaultOptions(List<TextContent> content, string? userLanguage)
    {
        // Automatically translate to user's preferred language if not specified
        var language = Language.Find(userLanguage);
        if (language != null && content.Any(c => c.LanguageId != language.Id))
            return new TranslationOptions(language);

        return null;
    }

    public void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("content").RequireCors("Tovik");

        group.MapGet("languages", GetLanguagesAsync).CacheOutput(x => x.Expire(TimeSpan.FromHours(1)));
        group.MapGet("language", (ClaimsPrincipal principal, HttpRequest request) => Language.Find(principal.Get("language") ?? request.Headers.AcceptLanguage));
        group.MapPost("language", async (Contents contents, Language language) => await contents.SetLanguage(language));

        group.MapPost("", async (Contents contents, HttpRequest request, PostContentRequest getContent) =>
        {
            var options = getContent.Options ?? DefaultOptions(getContent.Content, request.Headers.AcceptLanguage);
            var result = await contents.Get(getContent.Content, options);
            return Results.Ok(result);
        });

        group.MapPost("{id}", async (Contents contents, HttpRequest request, PostSingleContentRequest singleContent, string id) =>
        {
            var options = singleContent.Options ?? DefaultOptions([singleContent.Content], request.Headers.AcceptLanguage);
            var result = await contents.Get(singleContent.Content, options);
            return Results.Ok(result);
        });
    }
}