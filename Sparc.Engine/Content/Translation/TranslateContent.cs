using MediatR;
using Sparc.Blossom.Data;
using System.Security.Claims;

namespace Sparc.Engine;

public class TranslateContent(KoriTranslator translator, ClaimsPrincipal principal) 
    : INotificationHandler<PouchRevisionAdded>
{
    public async Task Handle(PouchRevisionAdded notification, CancellationToken cancellationToken)
    {
        var content = notification.Datum.Cast<TextContent>();
        if (content == null)
            return;

        var newContent = await translator.TranslateAsync(content, principal.Language());

        notification.Datum.Update(content);
    }
}

