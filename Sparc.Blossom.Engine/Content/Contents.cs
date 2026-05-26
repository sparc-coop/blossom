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
    public async Task<ContentResponse> GetAll(ContentRequest request)
    {
        if (request.Options.OutputLanguage == null)
        {
            var user = await auth.GetAsync(principal);
            request.Options.OutputLanguage = user?.Avatar.Language;
        }

        var (sparcDomain, existing, needsTranslation) = await GetContent(request);

        if (request.Options.OutputLanguage == null || request.Content.Count == 0 || needsTranslation.Count == 0)
            return new(existing.Select(x => new TextContentLight(x)).ToList());

        request = request with { Content = needsTranslation };

        if (request.Options.RunInBackground)
        {
            request.Options.BackgroundId = Guid.NewGuid().ToString();
            await channels.Execute(request.Options.BackgroundId, async (Contents translator) => await translator.TranslateAsync(request, sparcDomain));
            return new(existing.Select(x => new TextContentLight(x)).ToList(), request.Options.BackgroundId);
        }

        var translations = await TranslateAsync(request, sparcDomain);
        return new(existing.Union(translations).Select(x => new TextContentLight(x)).ToList());
    }

    public async Task<TextContent> UpdateAsync(ContentRequest request)
    {
        try
        {
            var (sparcDomain, existing, needsTranslation) = await GetContent(request);
            foreach (var item in request.Content.Where(x => !string.IsNullOrWhiteSpace(x.Text)))
            {
                var updated = existing.FirstOrDefault(x => x.Id == item.Id);
                if (updated != null)
                    await content.ExecuteAsync(updated, x => x.SetText(item.Text!));
                else
                {
                    item.SetDomain(sparcDomain, request.Referrer!);
                    await content.UpdateAsync(item);
                }
            }
            return request.Content.FirstOrDefault();
        }
        catch (Exception e)
        {
            return new(request.Referrer, "", Language.Find("en"), e.Message + e.InnerException?.Message + e.StackTrace);
        }
    }

    async Task<(SparcDomain domain, List<TextContent> existing, List<TextContent> needsCreation)> GetContent(ContentRequest request)
    {
        var sparcDomain = await GetOrCreateDomain(request.Referrer!);
        request.Content.ForEach(x => x.SetDomain(sparcDomain, request.Referrer!));

        var ids = request.Content.Select(x => x.Id).ToList();
        var existing = await content.Query(sparcDomain.Domain)
            .Where(x => ids.Contains(x.Id) && x.Version == sparcDomain.Settings.Version)
            .ToListAsync();

        if (request.Options.OutputLanguage == null)
            return (sparcDomain, existing, []);

        var needsTranslation = request.Content
            .Where(content => !existing.Any(x => x.OriginalText == content.Text) && content.LanguageId[..2] != request.Options.OutputLanguage.LanguageId)
            .ToList();

        return (sparcDomain, existing, needsTranslation);
    }

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

        domain ??= await GetOrCreateDomain(request.Referrer!);

        request.Options.TovikSettings = domain.Settings;
        //if (domain.IsBeyondTranslationLimit())
        //    throw new Exception("You've reached your Tovik translation limit!");

        var translator = translators.OrderBy(x => x.Priority).First();
        var translations = await translator.TranslateAsync(request);
        await content.UpdateAsync(translations);

        return translations;
    }

    async Task<SparcDomain> GetOrCreateDomain(string domainName)
    {
        if (domainName.StartsWith("http"))
        {
            var uri = new Uri(domainName);
            domainName = uri.Host;
            if (!uri.IsDefaultPort)
                domainName += ":" + uri.Port;
        }

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

    async Task<Page> GetOrCreatePage(string domain, string path)
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

    async Task<Language?> Visit(TextContent content, Language? language)
    {

        try
        {
            var page = await GetOrCreatePage(content.Domain, content.SpaceId);

            if (page.LanguageDetectedDate == null && !string.IsNullOrWhiteSpace(content.Text))
                page.SetLanguage(await DetectLanguage([content]));

            if (language != null)
                page.RegisterVisit(language);

            await pages.UpdateAsync(page);

            return page.Language;
        }
        catch (Exception e)
        {
            return new(e.Message + e.InnerException?.Message + e.StackTrace);
        }
    }

    public Task<List<Language>> GetLanguagesAsync() => Task.FromResult(Language.All);

    internal async Task<Language> SetLanguage(Language language)
    {
        var user = await auth.GetAsync(principal);
        user.Avatar.Language = Language.Find(language.Id);
        await auth.UpdateAsync(principal, user.Avatar);
        return language;
    }

    internal async Task<Language?> DetectLanguage(List<TextContent> content)
    {
        var translator = translators.Where(x => x is ILanguageDetector).Cast<ILanguageDetector>().First();
        return await translator.DetectLanguageAsync(content);
    }

    static ContentRequest UpdateFromHttpRequest(ContentRequest request, HttpRequest http)
    {
        if (request.Options == null)
            request = request with { Options = new() };
        
        if (request.Options.OutputLanguage == null)
            request.Options.OutputLanguage = Language.Find(http.Headers.AcceptLanguage);

        return request with { Referrer = http.Headers.Referer };
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
        group.MapPost("", async (Contents translator, HttpRequest request, List<TextContent> contents) =>
        {
            var toLanguage = Language.Find(request.Headers.AcceptLanguage);
            var translationRequest = new ContentRequest(contents, new TranslationOptions { OutputLanguage = toLanguage }, request.Headers.Referer);
            var result = await translator.GetAll(translationRequest);

            return Results.Ok(result.Content);
        });

        group.MapPut("", async (Contents translator, HttpRequest request, ContentRequest content) =>
        {
            var result = await translator.UpdateAsync(content);
            return Results.Ok(result);
        });

        group.MapPost("stream", async (Contents translator, HttpRequest request, ContentRequest contentRequest) =>
        {
            contentRequest = UpdateFromHttpRequest(contentRequest, request);
            contentRequest.Options.RunInBackground = true;
            var result = await translator.GetAll(contentRequest);
            return Results.Ok(result);
        });

        group.MapGet("stream/{id}", async (string id, BlossomEvents channels) =>
        {
            return Results.ServerSentEvents(channels.GetSseStream(id));
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

        group.MapGet("languages", GetLanguagesAsync).CacheOutput(x => x.Expire(TimeSpan.FromHours(1)));
        group.MapGet("language", (ClaimsPrincipal principal, HttpRequest request) => Language.Find(principal.Get("language") ?? request.Headers.AcceptLanguage));
        group.MapPost("language", async (Contents translator, Language language) => await translator.SetLanguage(language));

        group.MapPost("api", async (Contents translator, TranslationApiRequest request, HttpRequest http) =>
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
            var translations = await translator.TranslateAsync(new ContentRequest(content, request.Options, domain.ToAbsoluteUrl()));
            var result = translations.Select(x => new TranslationApiResponse(x.OriginalText, x.Text, x.LanguageId));

            domain.TovikApiUsage += content.Sum(x => x.WordCount());
            await domains.UpdateAsync(domain);

            return Results.Ok(result);
        });
    }
}