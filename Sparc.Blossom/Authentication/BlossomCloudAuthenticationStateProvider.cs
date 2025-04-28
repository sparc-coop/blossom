using Microsoft.AspNetCore.Components.Authorization;

namespace Sparc.Blossom.Authentication;

public class BlossomCloudAuthenticationStateProvider<T>(
    IBlossomCloud cloud,
    IRepository<BlossomUser> users,
    TimeProvider timeProvider
    ) 
    : AuthenticationStateProvider where T : BlossomUser
{
    public IBlossomCloud Cloud { get; } = cloud;
    public IRepository<BlossomUser> Users { get; } = users;
    public TimeProvider TimeProvider { get; } = timeProvider;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var user = Users.Query.FirstOrDefault();
        if (user == null)
        {
            user = new BlossomUser();
            await Users.AddAsync(user);
        }

        var principal = user.Login();
        var state = new AuthenticationState(principal);

        async void TimerCallback(object? _)
        {
            user = await Cloud.UserInfo();
            await Users.UpdateAsync(user);

            principal = user.Login();
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(principal)));
        }

        var timer = TimeProvider.CreateTimer(TimerCallback, null,
            TimeSpan.FromMilliseconds(1000),
            TimeSpan.FromMilliseconds(5000));

        return state;
    }
}
