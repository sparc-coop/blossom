using Sparc.Blossom;

namespace Sparc.MCN.Users;

public class Users(BlossomAggregateOptions<User> options) : BlossomAggregate<User>(options)
{
    public BlossomQuery<User> GetAllUsers()
        => Query().OrderByDescending(x => x.LastName);

    public BlossomQuery<User> GetUserById(string id)
    => Query().Where(x => x.Id == id);
}