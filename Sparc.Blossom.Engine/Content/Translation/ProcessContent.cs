using Microsoft.Azure.Cosmos.Linq;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Content;
using Sparc.Blossom.Data;
using Sparc.Blossom.Realtime;

namespace Sparc.Blossom.Billing;

public class ProcessContent(
    IRepository<SparcDomain> domains,
    IRepository<Page> pages,
    IRepository<UserCharge> charges) : BlossomOn<ContentPosted>
{
    public override async Task ExecuteAsync(ContentPosted item)
    {
        // Get owning user
        var domain = await domains.Query
            .Where(d => d.Domain == item.Content.Domain)
            .FirstOrDefaultAsync();

        await RegisterTovikUsage(item, domain);
        await RegisterPageView(item, domain);

        if (new Random().Next(10) == 1)
            await UpdateDomainStats(domain);
    }

    private async Task RegisterTovikUsage(ContentPosted item, SparcDomain? domain)
    {
        var userToCharge = domain?.TovikUserId ?? Guid.Empty.ToString();

        var charge = new UserCharge(userToCharge, item);
        await charges.AddAsync(charge);
    }

    private async Task RegisterPageView(ContentPosted item, SparcDomain? domain)
    {
        var page = await pages.Query
                    .Where(p => p.Domain == item.Content.Domain && p.Path == item.Content.SpaceId)
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

        domain.LastTranslatedDate = DateTime.UtcNow;
        domain.LastTranslatedLanguage = item.Content.Language.Id;
        await domains.UpdateAsync(domain);
    }

    private async Task UpdateDomainStats(SparcDomain? domain)
    {
        if (domain == null)
            return;

        domain.TovikUsage = await pages.Query.Where(x => x.Domain == domain.Domain && x.SpaceId != "*api*").CountAsync();

        var ppl = await pages.Query
            .Where(x => x.Domain == domain.Domain && x.SpaceId != "*api*")
            .Select(p => p.TovikUsage)
            .ToListAsync();

        domain.PagesPerLanguage = ppl
            .SelectMany(x => x.Keys)
            .GroupBy(g => g)
            .ToDictionary(g => g.Key, g => g.Count());

        await domains.UpdateAsync(domain);
    }


}
