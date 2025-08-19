using MediatR;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Content;
using Sparc.Blossom.Data;

namespace Sparc.Blossom.Billing;

public class BillToTovik(
    IRepository<BlossomUser> users,
    IRepository<SparcDomain> domains,
    IRepository<Page> pages,
    IRepository<UserCharge> charges) : INotificationHandler<TovikContentTranslated>
{
    public async Task Handle(TovikContentTranslated item, CancellationToken cancellationToken)
    {
        // Get owning user
        var domain = await domains.Query.Where(d => d.Domain == item.Content.Domain)
            .FirstOrDefaultAsync();

        await RegisterTovikUsage(item, domain);
        await RegisterPageView(item, domain);

        if (domain?.TovikUserId != null && new Random().Next(100) == 8)
            await CalculateTokenUsage(domain.TovikUserId);
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

        domain.TovikUsage = pages.Query.Where(x => x.Domain == item.Content.Domain).Count();
        await domains.UpdateAsync(domain);
    }

    private async Task CalculateTokenUsage(string userId)
    {
        var user = await users.Query
            .Where(u => u.Id == userId)
            .FirstOrDefaultAsync();

        if (user == null)
            return;

        var product = user?.Product("Tovik");
        if (product == null)
            return;

        var userDomains = await domains.Query
            .Where(d => d.TovikUserId == user!.Id)
            .Select(x => x.Domain)
            .ToListAsync();

        product.TotalUsage = pages.Query
            .Where(x => userDomains.Contains(x.Domain))
            .Count();

        await users.UpdateAsync(user!);

    }
}
