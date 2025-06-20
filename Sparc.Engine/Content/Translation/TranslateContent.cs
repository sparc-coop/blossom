using MediatR;
using Sparc.Blossom.Data;
using System.Security.Claims;

namespace Sparc.Engine;

public class TranslateContent(Contents contents, ClaimsPrincipal principal, PouchData data)
    : INotificationHandler<PouchRevisionAdded>
{
    public async Task Handle(PouchRevisionAdded notification, CancellationToken cancellationToken)
    {
        var content = notification.Datum.Cast<TextContent>();
        var toLanguage = principal.Language();
        if (content == null || toLanguage == null)
            return;

        var translation = await contents.TranslateAsync(content, toLanguage);
        notification.Datum.Update(content);

        await data.UpsertAsync(content.Domain, translation);
    }
}

