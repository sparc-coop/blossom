using Microsoft.JSInterop;
using Sparc.Engine.Aura;

namespace Sparc.Blossom.Authentication;

public class PasskeyAuthenticator(IJSRuntime js, ISparcAura aura)
{
    const string ApiKey = "sparcengine:public:63cc565eb9544940ad6f2c387b228677";

    readonly Lazy<Task<IJSObjectReference>> Auth = js.Import(
        "/_content/Sparc.Blossom/LoginWithPasskey.razor.js");

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
        {
            var user = await aura.Login(token);
        }
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

        var user = await aura.Login(passkey);
    }
}
