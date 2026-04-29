using Sparc.Blossom.Authentication;
using Sparc.Blossom.Data;
using Sparc.Blossom.Realtime;
using Sparc.Core;
using System.Security.Claims;

namespace Sparc.Blossom.Content;

public class Contents(
    IEnumerable<ITranslator> translators,
    IRepository<TextContent> content,
    IRepository<SparcDomain> domains,
    IRepository<Page> pages,
    DocumentTranslator documents,
    ClaimsPrincipal principal,
    SparcAuthenticator<BlossomUser> auth,
    BlossomEvents channels) : IBlossomEndpoints
{
    internal IEnumerable<ITranslator> Translators { get; } = translators;
    public IRepository<TextContent> Content { get; } = content;

    public Task<List<Language>> GetLanguagesAsync() => Task.FromResult(Language.All);

    public async Task<List<TextContent>> TranslateAsync(ContentRequest request, SparcDomain? domain = null)
    {
        if (request.Options.OutputLanguage == null)
        {
            var user = await auth.GetAsync(principal);
            request.Options.OutputLanguage = user?.Avatar.Language;
        }

        if (request.Options.OutputLanguage == null)
            return request.Content;

        if (request.Options.CrawlHtml)
        {
            foreach (var item in request.Content.Where(x => x.Text?.StartsWith("http") == true))
                item.Text = await new HtmlTranslator(item.Text!).TranslateAsync();
        }

        domain ??= await GetOrCreateDomain(request.Content.First().Domain);

        request.Options.TovikSettings = domain.Settings;
        //if (domain.IsBeyondTranslationLimit())
        //    throw new Exception("You've reached your Tovik translation limit!");

        var translator = Translators.OrderBy(x => x.Priority).First();
        var translations = await translator.TranslateAsync(request);
        await Content.UpdateAsync(translations);

        return translations;
    }

    public async Task<ContentResponse> GetAll(ContentRequest request)
    {
        if (request.Options.OutputLanguage == null)
        {
            var user = await auth.GetAsync(principal);
            request.Options.OutputLanguage = user?.Avatar.Language;
        }

        if (request.Options.OutputLanguage == null || request.Content.Count == 0)
            return new(request.Content);

        var domain = request.Content.First().Domain;
        var path = request.Content.First().SpaceId;
        var sparcDomain = await GetOrCreateDomain(domain);

        var ids = request.Content.Select(x => x.Id).ToList();
        var existing = await Content.Query(domain)
            .Where(x => ids.Contains(x.Id) && x.Version == sparcDomain.Settings.Version)
            .ToListAsync();

        var needsTranslation = request.Content
            .Where(content => !existing.Any(x => x.OriginalText == content.Text))
            .ToList();

        if (needsTranslation.Count == 0)
            return new(existing);

        request = request with { Content = needsTranslation };

        if (request.Options.RunInBackground)
        {
            request.Options.BackgroundId = Guid.NewGuid().ToString();
            await channels.Execute(request.Options.BackgroundId, async (Contents translator) => await translator.TranslateAsync(request, sparcDomain));
            return new(existing, request.Options.BackgroundId);
        }

        var translations = await TranslateAsync(request, sparcDomain);
        return new(existing.Union(translations).ToList());
    }

    private async Task<SparcDomain> GetOrCreateDomain(string domainName)
    {
        var domain = await domains.Query
            .Where(x => x.Domain == domainName)
            .FirstOrDefaultAsync();

        if (domain == null)
        {
            domain = new SparcDomain(domainName);
            await domains.AddAsync(domain);
        }

        return domain;
    }

    internal async Task<Language> SetLanguage(Language language)
    {
        var user = await auth.GetAsync(principal);
        user.Avatar.Language = Language.Find(language.Id);
        await auth.UpdateAsync(principal, user.Avatar);
        return language;
    }

    internal async Task<Language?> DetectLanguage(List<TextContent> content)
    {
        var translator = Translators.Where(x => x is ILanguageDetector).Cast<ILanguageDetector>().First();
        return await translator.DetectLanguageAsync(content);
    }

    private async Task<Page> GetOrCreatePage(string domain, string path)
    {
        var page = await pages.Query
            .Where(x => x.Domain == domain && x.Path == path)
            .FirstOrDefaultAsync();

        if (page == null)
        {
            page = new Page(domain, path);
            await pages.AddAsync(page);
        }

        return page;
    }

    private async Task<Language?> Visit(TextContent content, Language? language)
    {
        var page = await GetOrCreatePage(content.Domain, content.SpaceId);

        if (page.LanguageDetectedDate == null && !string.IsNullOrWhiteSpace(content.Text))
            page.SetLanguage(await DetectLanguage([content]));

        if (language != null)
            page.RegisterVisit(language);

        await pages.UpdateAsync(page);

        return page.Language;
    }

    public void Map(IEndpointRouteBuilder endpoints)
    {
        var content = endpoints.MapGroup("content").RequireCors("Tovik");
        var translate = endpoints.MapGroup("translate").RequireCors("Tovik");
        Map(content);
        Map(translate);
    }

    public void Map(RouteGroupBuilder group)
    {
        group.MapGet("languages", GetLanguagesAsync).CacheOutput(x => x.Expire(TimeSpan.FromHours(1)));
        group.MapGet("language", (ClaimsPrincipal principal, HttpRequest request) => Language.Find(principal.Get("language") ?? request.Headers.AcceptLanguage));
        group.MapPost("language", async (Contents translator, Language language) => await translator.SetLanguage(language));

        group.MapPost("all", async (Contents translator, HttpRequest request, List<TextContent> contents) =>
        {
            var toLanguage = Language.Find(request.Headers.AcceptLanguage);
            var translationRequest = new ContentRequest(contents, new TranslationOptions { OutputLanguage = toLanguage });
            var result = await translator.GetAll(translationRequest);

            return Results.Ok(result.Content);
        });

        group.MapPost("stream", async (Contents translator, HttpRequest request, ContentRequest contentRequest) =>
        {
            if (contentRequest.Options.OutputLanguage == null)
                contentRequest.Options.OutputLanguage = Language.Find(request.Headers.AcceptLanguage);

            contentRequest.Options.RunInBackground = true;
            var result = await translator.GetAll(contentRequest);

            return Results.Ok(result);
        });

        group.MapGet("stream/{id}", async (string id, BlossomEvents channels) =>
        {
            return Results.ServerSentEvents(channels.GetSseStream(id));
        });

        group.MapPost("untranslated", async (Contents translator, HttpRequest request, ContentRequest contentRequest) =>
        {
            var toLanguage = Language.Find(request.Headers.AcceptLanguage);
            if (contentRequest.Options.OutputLanguage == null && toLanguage != null)
                contentRequest.Options.OutputLanguage = toLanguage;

            var result = await translator.TranslateAsync(contentRequest);
            return Results.Ok(result);
        });

        group.MapPost("visit", async (Contents translator, HttpRequest request, TextContent content) =>
        {
            var language = await translator.Visit(content, Language.Find(request.Headers.AcceptLanguage));
            return Results.Ok(language);
        });

        group.MapGet("documents/{id}", async (Contents translator, HttpRequest request, string id, string domain, string? lang = null) =>
        {
            var language = Language.Find(lang ?? request.Headers.AcceptLanguage);
            var (page, content) = await documents.ExtractAsync(domain, id);
            var translationRequest = new ContentRequest(content, new(language!));
            var translations = await translator.GetAll(translationRequest);
            var result = await documents.ReplaceAsync(page, translations.Content);
            return Results.File(result, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", page.Name);
        });

        group.MapPost("", async (Contents translator, TranslationApiRequest request, HttpRequest http) =>
        {
            // Look up domain by API key in bearer auth
            var token = http.Headers.Authorization.ToString().Replace("Bearer ", "");
            if (string.IsNullOrWhiteSpace(token))
                return Results.Unauthorized();

            var hash = BlossomKey.SHA256(token);
            var domain = await domains.Query.Where(x => x.ApiKey != null && x.ApiKey.Hash == hash).FirstOrDefaultAsync();
            if (domain == null)
                return Results.Unauthorized();

            if (domain.TovikApiUsage > domain.Product("Tovik")?.MaxUsage * 1000)
                return Results.StatusCode(429);

            var fromLang = Language.Find(http.Headers.ContentLanguage.ToString()) ?? Language.Find("en");
            var toLang = Language.Find(http.Headers.AcceptLanguage.ToString());
            if (toLang != null)
                request.Options.OutputLanguage = toLang;

            var content = request.Content.Select(x => new TextContent(domain.Domain, "*api*", fromLang!, x)).ToList();
            var translations = await translator.TranslateAsync(new ContentRequest(content, request.Options));
            var result = translations.Select(x => new TranslationApiResponse(x.OriginalText, x.Text, x.LanguageId));

            domain.TovikApiUsage += content.Sum(x => x.WordCount());
            await domains.UpdateAsync(domain);

            return Results.Ok(result);
        });
    }
}