using MediatR;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Content;
using Sparc.Blossom.Data;

namespace Sparc.Blossom.Billing;

public class BillToTovik(
    IRepository<BlossomUser> users,
    IRepository<SparcDomain> domains,
    IRepository<UserCharge> charges) : INotificationHandler<TovikContentTranslated>
{
    public async Task Handle(TovikContentTranslated item, CancellationToken cancellationToken)
    {
        // Get owning user
        var domain = await domains.Query.Where(d => d.Domain == item.Content.Domain)
            .FirstOrDefaultAsync();

        var userToCharge = domain?.TovikUserId ?? Guid.Empty.ToString();
        
        var charge = new UserCharge(userToCharge, item);
        await charges.AddAsync(charge);

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

        domain.TovikUsage = (int)charges.Query.Where(x => x.Domain == item.Content.Domain)
            .Sum(x => x.Amount);
        await domains.UpdateAsync(domain);
    }
}
