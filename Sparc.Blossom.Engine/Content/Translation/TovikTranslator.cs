using Sparc.Blossom.Authentication;
using Sparc.Blossom.Data;
using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Claims;

namespace Sparc.Blossom.Content;

public class TovikTranslator(
    IEnumerable<ITranslator> translators,
    IRepository<TextContent> content,
    IRepository<SparcDomain> domains,
    IRepository<BlossomUser> users,
    ClaimsPrincipal principal,
    SparcAuthenticator<BlossomUser> auth) : IBlossomEndpoints
{
    internal static List<Language>? Languages;

    internal IEnumerable<ITranslator> Translators { get; } = translators;
    public IRepository<TextContent> Content { get; } = content;

    public async Task<List<Language>> GetLanguagesAsync()
    {
        if (Languages == null)
        {
            Languages = [];
            foreach (var translator in Translators.OrderBy(x => x.Priority))
            {
                var languages = await translator.GetLanguagesAsync();
                Languages.AddRange(languages.Where(x => !Languages.Any(y => y.Matches(x))));
            }
        }

        Languages = Languages.OrderBy(x => x.DisplayName)
            .ThenBy(x => x.DialectId == null ? 1 : 0)
            .ToList();

        return Languages;
    }

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
        var newId = TextContent.IdHash(content.Text, toLanguage);
        var existing = await Content.Query
            .Where(x => x.Domain == content.Domain && x.Id == newId)
            .FirstOrDefaultAsync();

        if (existing != null)
            return existing;

        var translation = await TranslateAsync(content, toLanguage)
            ?? throw new InvalidOperationException("Translation failed.");

        await Content.AddAsync(translation);
        return translation;
    }

    public async Task<List<TextContent>> BulkTranslate(List<TextContent> contents)
    {
        var user = await auth.GetAsync(principal);

        var toLanguage = user?.Avatar.Language;
        if (toLanguage == null)
            return contents;

        var results = new ConcurrentBag<TextContent>();
        await Parallel.ForEachAsync(contents, async (content, _) =>
        {
            var newId = TextContent.IdHash(content.Text, toLanguage);
            var existing = await Content.Query
                .Where(x => x.Domain == content.Domain && x.Id == newId)
                .FirstOrDefaultAsync();

            if (existing != null)
                results.Add(existing);
        });

        var needsTranslation = contents
            .Where(content => !results.Any(x => x.Id == TextContent.IdHash(content.Text, toLanguage)))
            .ToList();

        if (!await CanTranslate(needsTranslation))
            throw new Exception("You've reached your Tovik translation limit!");

        var additionalContext = string.Join("\n", contents.Select(x => x.Text));
        var translations = await TranslateAsync(needsTranslation, [toLanguage], additionalContext);
        await Content.AddAsync(translations);

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

        if (domain?.TovikUsage < 1000)
            return true;

        if (domain?.TovikUserId != null)
        {
            var domainOwner = await users.FindAsync(domain.TovikUserId);
            var tovik = domainOwner?.Product("Tovik");
            if (tovik == null || tovik.HasExceededUsage)
                return false;
            else
                return true;
        }

        return false;
    }

    public async Task<TextContent?> TranslateAsync(TextContent message, Language toLanguage, string? additionalContext = null)
        => (await TranslateAsync([message], [toLanguage], additionalContext)).FirstOrDefault();

    public async Task<List<TextContent>> TranslateAsync(IEnumerable<TextContent> messages, List<Language> toLanguages, string? additionalContext = null)
    {
        var translatedMessages = new List<TextContent>();

        foreach (var toLanguage in toLanguages)
        {
            var translators = messages.GroupBy(x => GetBestTranslator(x.Language, toLanguage));
            foreach (var messageList in translators)
            {
                var result = await messageList.Key.TranslateAsync(messageList.ToList(), [toLanguage], additionalContext);
                foreach (var message in result)
                    translatedMessages.Add(message);
            }
        }

        return translatedMessages.ToList();
    }

    internal async Task<string?> TranslateAsync(string text, string fromLanguage, string toLanguage)
    {
        if (fromLanguage == toLanguage)
            return text;

        var language = await GetLanguageAsync(toLanguage)
            ?? throw new ArgumentException($"Language {toLanguage} not found");

        var from = await GetLanguageAsync(fromLanguage);
        var message = new TextContent("", from!, text);
        var result = await TranslateAsync([message], [language]);
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
        user.Avatar.Language = GetLanguage(language.Id);
        await auth.UpdateAsync(principal, user.Avatar);
        return language;
    }

    internal static List<string> GetLanguageIds(string? languageClaim)
    {
        if (string.IsNullOrWhiteSpace(languageClaim))
            return [];

        var languages = languageClaim
            .Split(',')
            .Select(l => l.Split(';')[0].Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        if (languages.Count == 0)
            return [];

        return languages;
    }

    internal static Language? GetLanguage(string? languageClaim)
    {
        if (Languages == null || string.IsNullOrWhiteSpace(languageClaim))
            return null;

        var languages = GetLanguageIds(languageClaim);
        // Try to find a matching language in LanguagesSpoken or create a new one
        foreach (var langCode in languages)
        {
            // Try to match by Id or DialectId
            var match = Languages
                .OrderBy(x => x.DialectId != null ? 0 : 1)
                .FirstOrDefault(l => l.Matches(langCode));

            if (match != null)
                return match;
        }

        return null;
    }

    internal static BlossomRegion? GetLocale(string languageClaim)
    {
        var languages = GetLanguageIds(languageClaim);

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
        group.MapGet("language", (ClaimsPrincipal principal, HttpRequest request) => GetLanguage(principal.Get("language") ?? request.Headers.AcceptLanguage));
        group.MapPost("language", async (TovikTranslator translator, Language language) => await translator.SetLanguage(language));
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
    }
}