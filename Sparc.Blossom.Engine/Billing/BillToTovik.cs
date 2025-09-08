using MediatR;
using Microsoft.Azure.Cosmos.Linq;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Content;
using Sparc.Blossom.Data;

namespace Sparc.Blossom.Billing;

public class BillToTovikHandler(BlossomQueue<BillToTovik> biller)  : INotificationHandler<TovikContentTranslated>
{
    public async Task Handle(TovikContentTranslated notification, CancellationToken cancellationToken)
    {
        await biller.AddAsync(async (x, token) => await x.ExecuteAsync(notification, token));
    }
}

public class BillToTovik(
    IRepository<SparcDomain> domains,
    IRepository<Page> pages,
    IRepository<UserCharge> charges)
{
    public async Task ExecuteAsync(TovikContentTranslated item, CancellationToken cancellationToken)
    {
        // Get owning user
        var domain = await domains.Query.Where(d => d.Domain == item.Content.Domain)
            .FirstOrDefaultAsync();

        await RegisterTovikUsage(item, domain);
        await RegisterPageView(item, domain);
    }

    private async Task RegisterTovikUsage(TovikContentTranslated item, SparcDomain? domain)
    {
        var userToCharge = domain?.TovikUserId ?? Guid.Empty.ToString();

        var charge = new UserCharge(userToCharge, item);
        await charges.AddAsync(charge);
    }

    private async Task RegisterPageView(TovikContentTranslated item, SparcDomain? domain)
    {
        var page = await pages.Query
                    .Where(p => p.Domain == item.Content.Domain && p.Path == item.Content.Path)
                    .FirstOrDefaultAsync();

        if (page == null)
        {
            page = new Page(item.Content);
            await pages.AddAsync(page);
        }

        page.RegisterTovikUsage(item);
        await pages.UpdateAsync(page);

        if (domain == null)
            return;

        domain.TovikUsage = await pages.Query.Where(x => x.Domain == item.Content.Domain).CountAsync();

        var ppl = await pages.Query
            .Where(x => x.Domain == item.Content.Domain)
            .Select(p => p.TovikUsage)
            .ToListAsync();

        domain.PagesPerLanguage = ppl
            .SelectMany(x => x.Keys)
            .GroupBy(g => g)
            .ToDictionary(g => g.Key, g => g.Count());

        domain.LastTranslatedDate = DateTime.UtcNow;
        domain.LastTranslatedLanguage = item.Content.Language.Id;

        await domains.UpdateAsync(domain);
    }
}
