using Microsoft.AspNetCore.Mvc;
using Passwordless;
using Sparc.Blossom.Authentication;
using System.Security.Claims;

namespace Sparc.Engine;

public class SparcEngineAuthenticator<T> : BlossomDefaultAuthenticator<T>, IBlossomEndpoints
    where T : BlossomUser, new()
{
    IPasswordlessClient PasswordlessClient { get; }
    public FriendlyId FriendlyId { get; }
    public KoriTranslator Translator { get; }
    public HttpClient Client { get; }
    public const string PublicKey = "sparcengine:public:63cc565eb9544940ad6f2c387b228677";

    public SparcEngineAuthenticator(
        IPasswordlessClient _passwordlessClient,
        IConfiguration config,
        IRepository<T> users,
        FriendlyId friendlyId,
        KoriTranslator translator)
        : base(users)
    {
        PasswordlessClient = _passwordlessClient;
        FriendlyId = friendlyId;
        Translator = translator;
        Client = new HttpClient
        {
            BaseAddress = new Uri("https://v4.passwordless.dev/")
        };
        Client.DefaultRequestHeaders.Add("ApiSecret", config.GetConnectionString("Passwordless"));
    }

    public override async Task<ClaimsPrincipal> LoginAsync(ClaimsPrincipal principal)
    {
        var user = await GetAsync(principal);
        principal = user.Login();
        await Users.UpdateAsync((T)user);
        return principal;
    }

    public async Task<BlossomUser> Login(ClaimsPrincipal principal, HttpContext context, string? emailOrToken = null)
    {
        Message = null;

        // 1. Convert the ClaimsPrincipal from the cookie into a BlossomUser
        // If the BlossomUser is already attached to Passwordless, they're logged in because their cookie is valid
        User = await GetAsync(principal);

        if (User.ExternalId != null)
            return User;

        // Verify Authentication Token or Register
        if (emailOrToken != null && emailOrToken.StartsWith("verify"))
        {
            var passwordlessUser = await PasswordlessClient.VerifyAuthenticationTokenAsync(emailOrToken);

            if (passwordlessUser?.Success == true)
            {
                //var parentUser = Users.Query.Where(x => x.ExternalId == User.UserId && x.ParentUserId == null).FirstOrDefault();
                var parentUser = Users.Query.Where(x => x.ExternalId == passwordlessUser.UserId && x.ParentUserId == null).FirstOrDefault();
                if (parentUser == null)
                {
                    User.ExternalId = passwordlessUser.UserId;

                    await SaveAsync();
                    return User;
                }
                else
                {
                    User.SetParentUser(parentUser);

                    await SaveAsync();
                    return User;
                }
            }
        }

        var passwordlessToken = await SignUpWithPasswordlessAsync(User);
        User.SetToken(passwordlessToken);
        return User;
    }

    public async Task<BlossomUser> Logout(ClaimsPrincipal principal, string? emailOrToken = null)
    {
        var user = await GetAsync(principal);

        user.Logout();
        await SaveAsync();

        return user;
    }

    private async Task<string> SignUpWithPasswordlessAsync(BlossomUser user)
    {
        var registerToken = await PasswordlessClient.CreateRegisterTokenAsync(new RegisterOptions(user.Id, user.Username)
        {
            Aliases = [user.Username]
        });

        return registerToken.Token;
    }

    private async Task<bool> HasPasskeys(string? externalId)
    {
        if (externalId == null)
            return false;

        var credentials = await PasswordlessClient.ListCredentialsAsync(externalId);
        return credentials.Any();
    }

    private async Task<bool> SendMagicLinkAsync(BlossomUser user, string urlTemplate, int timeToLive = 3600)
        => await PostAsync("magic-links/send", new
        {
            emailAddress = user.Username,
            urlTemplate,
            userId = user.Id,
            timeToLive
        });

    private async Task<bool> PostAsync(string url, object payload)
    {
        var response = await Client.PostAsJsonAsync(url, payload);
        return response.IsSuccessStatusCode;
    }

    private async Task LoginWithTokenAsync(string token)
    {
        if (User == null)
            throw new Exception("User not initialized");

        var passwordlessUser = await PasswordlessClient.VerifyAuthenticationTokenAsync(token);
        if (passwordlessUser?.Success != true)
            throw new Exception("Unable to verify token");

        var hasPasskeys = await HasPasskeys(passwordlessUser.UserId);
        if (!hasPasskeys)
        {
            var result = await SignUpWithPasswordlessAsync(User);
        }

        var parentUser = Users.Query.FirstOrDefault(x => x.ExternalId == passwordlessUser.UserId && x.ParentUserId == null);
        if (parentUser != null)
        {
            User.SetParentUser(parentUser);
            await SaveAsync();
            User = parentUser;
        }
        else
        {
            User.Login("Passwordless", passwordlessUser.UserId);
        }

        await SaveAsync();
    }

    public override async IAsyncEnumerable<LoginStates> Logout(ClaimsPrincipal principal)
    {
        var user = await GetAsync(principal);

        user.Logout();
        await SaveAsync();

        yield return LoginStates.LoggedOut;
    }

    protected override async Task<BlossomUser> GetUserAsync(ClaimsPrincipal principal)
    {
        await base.GetUserAsync(principal);

        if (User!.Username == null)
        {
            User.ChangeUsername(FriendlyId.Create(1, 2));
            await SaveAsync();
        }

        return User;
    }

    private async Task SaveAsync()
    {
        await Users.UpdateAsync((T)User!);
    }

    public async Task<BlossomUser> AddProductAsync(ClaimsPrincipal principal, string productName)
    {
        await base.GetUserAsync(principal);

        if (User is null)
            throw new InvalidOperationException("User not initialized");

        bool alreadyHasProduct = User.Products.Any(p => p.ProductName.Equals(productName, StringComparison.OrdinalIgnoreCase));

        if (!alreadyHasProduct)
        {
            User.AddProduct(productName);
            await SaveAsync();
        }

        return User;
    }

    public void Map(IEndpointRouteBuilder endpoints)
    {
        var auth = endpoints.MapGroup("/auth");
        auth.MapPost("login", async (SparcEngineAuthenticator<T> auth, ClaimsPrincipal principal, HttpContext context, string? emailOrToken = null) => await auth.Login(principal, context, emailOrToken));
        auth.MapPost("logout", async (SparcEngineAuthenticator<T> auth, ClaimsPrincipal principal, string? emailOrToken = null) => await auth.Logout(principal, emailOrToken));
        auth.MapGet("userinfo", async (SparcEngineAuthenticator<T> auth, ClaimsPrincipal principal) => await auth.GetAsync(principal));

        var user = endpoints.MapGroup("/user");
        user.MapGet("language", async (SparcEngineAuthenticator<T> auth, ClaimsPrincipal principal, HttpRequest request) =>
        {
            await auth.GetUserAsync(principal);
            var language = auth.User?.Avatar?.Language;
            if (language == null && !string.IsNullOrWhiteSpace(request.Headers.AcceptLanguage))
            {
                Translator.SetLanguage(auth.User!, request.Headers.AcceptLanguage);
                await auth.SaveAsync();
            }
            return auth.User?.Avatar?.Language;
        });
        user.MapPost("language", async (SparcEngineAuthenticator<T> auth, ClaimsPrincipal principal, Language language) => await auth.SetLanguageAsync(principal, language));
        auth.MapPost("user-products", async (SparcEngineAuthenticator<T> auth, ClaimsPrincipal principal, [FromBody] AddProductRequest request) => await auth.AddProductAsync(principal, request.ProductName));
    }

    private async Task<BlossomUser> SetLanguageAsync(ClaimsPrincipal principal, Language language)
    {
        await base.GetUserAsync(principal);

        if (User is null)
            throw new InvalidOperationException("User not initialized");

        Translator.SetLanguage(User, language.Id);
        await SaveAsync();

        return User;
    }
}