using MediatR;
using Sparc.Blossom.Data;
using System.Security.Claims;

namespace Sparc.Engine;

public class TranslateContent(
    IRepository<TextContent> contents, 
    KoriTranslator translator,
    ClaimsPrincipal principal, 
    PouchData data)
    : INotificationHandler<PouchRevisionAdded>
{
    public async Task Handle(PouchRevisionAdded notification, CancellationToken cancellationToken)
    {
        var content = notification.Datum.Cast<TextContent>();
        var toLanguage = principal.Language();
        if (content == null || toLanguage == null)
            return;

        var existing = await contents.Query
            .Where(x => x.Domain == content.Domain && x.Id == content.Id)
            .CosmosFirstOrDefaultAsync();

        if (existing != null)
            content = existing;
        else
            await contents.AddAsync(content);

        var translation = await TranslateAsync(content, toLanguage);
        if (translation != null)
        {
            notification.Datum.Update(content);
            await data.UpsertAsync(content.Domain, translation);
        }
    }

    internal async Task<TextContent?> TranslateAsync(TextContent content, Language toLanguage)
    {
        var translation = await contents.Query
            .Where(x => x.Domain == content.Domain && x.SourceContentId == content.Id && x.Language.Id == toLanguage.Id)
            .CosmosFirstOrDefaultAsync();

        if (translation != null)
            return translation;

        translation = await translator.TranslateAsync(content, toLanguage);
        if (translation != null)
            await contents.AddAsync(translation);

        return translation;
    }
}

