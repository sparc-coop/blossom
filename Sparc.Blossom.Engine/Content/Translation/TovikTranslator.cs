using HtmlAgilityPack;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Content.Tovik;
using Sparc.Blossom.Data;
using System.Globalization;
using System.Security.Claims;

namespace Sparc.Blossom.Content;

public class TovikTranslator(
    IEnumerable<ITranslator> translators,
    IRepository<TextContent> content,
    IRepository<SparcDomain> domains,
    IRepository<Page> pages,
    ClaimsPrincipal principal,
    SparcAuthenticator<BlossomUser> auth) : IBlossomEndpoints
{
    internal IEnumerable<ITranslator> Translators { get; } = translators;
    public IRepository<TextContent> Content { get; } = content;

    public Task<List<Language>> GetLanguagesAsync() => Task.FromResult(Language.All);

    async Task<Language?> GetLanguageAsync(string language)
    {
        var languages = await GetLanguagesAsync();
        return languages.FirstOrDefault(x => x.Id == language);
    }

    public async Task<TextContent> Get(TextContent content)
    {
        var user = await auth.GetAsync(principal);
        var toLanguage = user.Avatar.Language;

        if (toLanguage == null)
            return content;

        return await GetOrTranslateAsync(content, toLanguage);
    }

    private async Task<TextContent> GetOrTranslateAsync(TextContent content, Language toLanguage)
    {
        var existing = await Content.FindAsync(content.Domain, content.Id);

        if (existing != null)
            return existing;

        var translation = await TranslateAsync(content, toLanguage)
            ?? throw new InvalidOperationException("Translation failed.");

        await PublishAsync([translation]);
        return translation;
    }

    public async Task<List<TextContent>> SingleTranslate(TranslationRequest request, Language? toLanguage = null)
    {
        if (toLanguage == null)
        {
            var user = await auth.GetAsync(principal);
            toLanguage = user?.Avatar.Language;
        }

        if (toLanguage == null)
            return request.Content;

        if (!await CanTranslate(request.Content))
            throw new Exception("You've reached your Tovik translation limit!");

        var options = new TovikTranslationOptions { OutputLanguage = toLanguage, AdditionalContext = request.AdditionalContext };

        if (request.Model != null)
        {
            var translator = Translators
                .OrderBy(x => x.Priority == 0 ? 99 : x.Priority)
                .Where(x => x.CanTranslate(request.Content.First().Language, toLanguage))
                .FirstOrDefault();

            if (translator != null)
            {
                var liveTranslations = await translator.TranslateAsync(request.Content, options);
                await PublishAsync(liveTranslations);
                return liveTranslations;
            }
        }

        var translations = await TranslateAsync(request.Content, options);
        await PublishAsync(translations);

        return translations;
    }

    public async Task<TextContent> TranslateToEntity(TextContent content, TovikTranslationOptions options)
    {
        if (content.Text?.StartsWith("http") == true)
        {
            var handler = new HttpClientHandler()
            {
                AutomaticDecompression = System.Net.DecompressionMethods.All
            };

            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; https://engine.sparc.coop)");
            var html = await client.GetStringAsync(content.Text);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var body = doc.DocumentNode.SelectSingleNode("//body");
            // remove all script and style nodes
            foreach (var script in body.SelectNodes("//script|//style") ?? Enumerable.Empty<HtmlNode>())
                script.Remove();

            content.Text = body.InnerHtml;
        }

        var translator = Translators.OrderBy(x => x.Priority).First();
        var result = await translator.TranslateAsync(content, options);
        return result;
    }

    public async Task<List<TextContent>> GetAll(List<TextContent> contents, Language? toLanguage = null)
    {
        if (toLanguage == null)
        {
            var user = await auth.GetAsync(principal);
            toLanguage = user?.Avatar.Language;
        }

        if (toLanguage == null || !contents.Any())
            return contents;

        var domain = contents.First().Domain;
        var ids = contents.Select(x => x.Id).ToList();

        var existing = await Content.Query(domain).Where(x => ids.Contains(x.Id)).ToListAsync();
        return existing;
    }

    public async Task<List<TextContent>> BulkTranslate(List<TextContent> contents)
    {
        var user = await auth.GetAsync(principal);
        var toLanguage = user?.Avatar.Language;

        var results = await GetAll(contents, toLanguage);

        var needsTranslation = contents
            .Where(content => !results.Any(x => x.Id == content.Id))
            .ToList();

        if (!await CanTranslate(needsTranslation))
            throw new Exception("You've reached your Tovik translation limit!");

        var additionalContext = string.Join("\n", contents.Select(x => x.Text).OrderBy(x => Guid.NewGuid()).Take(20));
        var translations = await TranslateAsync(needsTranslation, new TovikTranslationOptions { OutputLanguage = toLanguage, AdditionalContext = additionalContext });
        await PublishAsync(translations);

        return results.Union(translations).ToList();
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
        => (await TranslateAsync([message], new() { OutputLanguage = toLanguage, AdditionalContext = additionalContext })).FirstOrDefault();

    public async Task<List<TextContent>> TranslateAsync(IEnumerable<TextContent> messages, TovikTranslationOptions options)
    {
        var translator = Translators.OrderBy(x => x.Priority).First();
        var result = await translator.TranslateAsync(messages.ToList(), options);
        return result;
    }

    internal async Task<string?> TranslateAsync(string text, string fromLanguage, string toLanguage)
    {
        if (fromLanguage == toLanguage)
            return text;

        var language = await GetLanguageAsync(toLanguage)
            ?? throw new ArgumentException($"Language {toLanguage} not found");

        var from = await GetLanguageAsync(fromLanguage);
        var message = new TextContent("", "", from!, text);
        var result = await TranslateAsync([message], new() { OutputLanguage = language });
        return result?.FirstOrDefault()?.Text;
    }

    internal ITranslator GetBestTranslator(Language fromLanguage, Language toLanguage)
    {
        return Translators
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

    private async Task Visit(Visit visit, Language language)
    {
        var page = await pages.Query
            .Where(x => x.Domain == visit.Domain && x.Path == visit.Path)
            .FirstOrDefaultAsync();

        if (page == null)
            return;

        page.RegisterVisit(language);
        await pages.UpdateAsync(page);
    }

    async Task PublishAsync(IEnumerable<TextContent> contents)
    {
        if (Content is CosmosDbSimpleRepository<TextContent> cosmos)
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

    public void Map(IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("translate").RequireCors("Tovik");
        group.MapPost("", async (TovikTranslator translator, HttpRequest request, TextContent content) => await translator.Get(content));
        group.MapGet("languages", GetLanguagesAsync).CacheOutput(x => x.Expire(TimeSpan.FromHours(1)));
        group.MapGet("language", (ClaimsPrincipal principal, HttpRequest request) => Language.Find(principal.Get("language") ?? request.Headers.AcceptLanguage));
        group.MapPost("language", async (TovikTranslator translator, Language language) => await translator.SetLanguage(language));
        group.MapPost("visit", async (TovikTranslator translator, HttpRequest request, Visit visit) =>
        {
            var language = Language.Find(request.Headers.AcceptLanguage);
            await translator.Visit(visit, language);
            return Results.Ok();
        });

        group.MapPost("untranslated", async (TovikTranslator translator, HttpRequest request, TranslationRequest translationRequest) =>
        {
            var toLanguage = Language.Find(request.Headers.AcceptLanguage);
            var result = await translator.SingleTranslate(translationRequest, toLanguage);
            return Results.Ok(result);
        });
        group.MapPost("all", async (TovikTranslator translator, HttpRequest request, List<TextContent> contents) =>
        {
            var toLanguage = Language.Find(request.Headers.AcceptLanguage);
            var result = await translator.GetAll(contents, toLanguage);
            return Results.Ok(result);
        });
        group.MapPost("bulk", async (TovikTranslator translator, List<TextContent> contents) =>
        {
            try
            {
                return Results.Ok(await translator.BulkTranslate(contents));
            }
            catch (Exception ex) when (ex.Message.Contains("limit"))
            {
                return Results.StatusCode(429);
            }
        });
        group.MapPost("entity", async (TovikTranslator translator, TovikTranslationRequest request) =>
        {
            var result = await translator.TranslateToEntity(request.Content, request.Options);
            return Results.Ok(result);
        });
    }
}