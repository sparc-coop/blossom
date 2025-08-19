using MediatR;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Content;
using Sparc.Blossom.Data;
using Twilio.Rest;

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
        await CalculateTokenUsage(item, domain);
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

    private async Task CalculateTokenUsage(TovikContentTranslated item, SparcDomain? domain)
    {
        if (domain == null)
            return;

        // Every 100th or so call, recalculate usage and bill Tovik user
        var random = new Random().Next(100);
        if (random != 8)
            return;

        var user = await users.Query
            .Where(u => u.Id == domain.TovikUserId)
            .FirstOrDefaultAsync();

        var product = user?.Product("Tovik");
        if (product == null)
            return;

        product.TotalUsage = charges.Query
            .Where(x => x.UserId == domain.TovikUserId)
            .Sum(x => x.Amount);
        await users.UpdateAsync(user!);

    }
}
