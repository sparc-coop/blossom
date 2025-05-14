using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public class BlossomCloudAuthenticator<T>(
    IRepository<T> users,
    IBlossomCloud cloud,
    TimeProvider timeProvider,
    IJSRuntime js) 
    : BlossomDefaultAuthenticator<T>(users)
    where T : BlossomUser, new()
{
    public Lazy<Task<IJSObjectReference>> Js { get; } = new(() => js.InvokeAsync<IJSObjectReference>("import", "./_content/Sparc.Blossom/BlossomCloudAuthenticator.js").AsTask());

    private ITimer? Timer;

    const string PublicKey = "blossomcloud:public:3a16c78de07641e5b82f270d278ace2b";

    public override async Task<BlossomUser> LoginAsync(ClaimsPrincipal principal, string? emailOrToken = null)
    {
        User = await cloud.UserInfo();

        // If the BlossomUser is already attached to Passwordless, they're logged in because their cookie is valid
        if (User.ExternalId != null)
            return User;

        // 2. BlossomUser is not yet attached to Passwordless. Look for discoverable passkeys on their device.
        emailOrToken ??= await SignInWithPasswordlessAsync();

        // 3. No discoverable passkeys. Sign them up via Passwordless.
        if (string.IsNullOrEmpty(emailOrToken))
        {
            await SignUpWithPasswordlessAsync();
            
        }

        return new BlossomUser();
    }

    public async Task<AuthenticationState> PollAsync(int everyXSeconds)
    {
        var state = await base.GetAuthenticationStateAsync();

        async void TimerCallback(object? _)
        {
            User = await cloud.UserInfo();
            await Users.UpdateAsync((T)User);

            var principal = User.Login();
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(principal)));
        }

        if (Timer != null)
            await StopPolling();
            
        Timer = timeProvider.CreateTimer(TimerCallback, null,
            TimeSpan.FromMilliseconds(1000),
            TimeSpan.FromSeconds(everyXSeconds));

        return state;
    }

    public async Task StopPolling()
    {
        if (Timer != null)
        { 
            await Timer.DisposeAsync();
            Timer = null;
        }
    }

    private async Task InitPasswordlessAsync()
    {
        var js = await Js.Value;
        await js.InvokeVoidAsync("init", PublicKey);
    }

    private async Task<string> SignInWithPasswordlessAsync(BlossomUser? user = null)
    {
        await InitPasswordlessAsync();
        var js = await Js.Value;
        return await js.InvokeAsync<string>("signInWithPasskey", user?.Username);
    }

    private async Task<string> SignUpWithPasswordlessAsync()
    {
        await InitPasswordlessAsync();
        var js = await Js.Value;

        User = await cloud.Login();
        var result = await js.InvokeAsync<string>("signUpWithPasskey", User.Token);
        if (!string.IsNullOrEmpty(result))
        {
            await cloud.Login(result);
        }

        return "abc";
    }
}
