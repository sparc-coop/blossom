using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Sparc.Blossom.Authentication;

public class PasskeyAuthenticator(
    IJSRuntime js, 
    ISparcAura aura, 
    NavigationManager nav)
{
    const string ApiKey = "sparcaura:public:b227c6af0d244323aaab033cc9d392c8";

    readonly Lazy<Task<IJSObjectReference>> Auth = js.Import(
        "./Blossom/Authentication/Avatar/LoginWithPasskey.razor.js");

    public string? Message { get; private set; }

    public async Task InitializeAsync()
    {
        var js = await Auth.Value;
        await js.InvokeVoidAsync("initialize", ApiKey);
    }

    public async Task LoginAsync()
    {
        var js = await Auth.Value;
        var token = await js.InvokeAsync<string>("signInWithPasskey", null);
        if (!string.IsNullOrWhiteSpace(token))
            nav.NavigateTo(nav.GetUriWithQueryParameter("_auth", token), true);
    }

    public async Task RegisterAsync()
    {
        var code = await aura.Register();

        var js = await Auth.Value;
        var passkey = await js.InvokeAsync<string>("signUpWithPasskey", code.Code);
        if (string.IsNullOrWhiteSpace(passkey))
        {
            Message = "⚠️ Passkey signup aborted.";
            return;
        }

        nav.NavigateTo(nav.GetUriWithQueryParameter("_auth", passkey), true);
    }
}
