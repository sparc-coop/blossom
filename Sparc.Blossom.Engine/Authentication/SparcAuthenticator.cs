using Passwordless;
using Sparc.Blossom.Billing;
using Sparc.Blossom.Content;
using Sparc.Blossom.Data;
using Sparc.Blossom.Realtime;
using Sparc.Core;
using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public class SparcAuthenticator<T>(
    IPasswordlessClient _passwordlessClient,
    IRepository<T> users,
    TwilioService twilio,
    FriendlyId friendlyId,
    IHttpContextAccessor http,
    SparcTokens tokens,
    IRepository<SparcDomain> domains)
    : BlossomDefaultAuthenticator<T>(users), IBlossomEndpoints
    where T : BlossomUser, new()
{
    T? SparcUser;

    public override async Task<ClaimsPrincipal> LoginAsync(ClaimsPrincipal principal)
    {
        SparcUser = await GetUserAsync(principal);
        SparcUser.Login();
        UpdateFromHttpContext(principal);
        await users.UpdateAsync(SparcUser!);

        var priorUser = BlossomUser.FromPrincipal(principal);
        var newPrincipal = SparcUser.ToPrincipal();

        if (!priorUser.Equals(SparcUser) && http.HttpContext != null)
        {
            http.HttpContext.User = newPrincipal;
        }

        return newPrincipal;
    }

    public override async Task<ClaimsPrincipal> LoginAsync(ClaimsPrincipal principal, string authenticationType, string externalId)
    {
        if (authenticationType != "Bearer")
            return await base.LoginAsync(principal, authenticationType, externalId);

        var hash = BlossomKey.SHA256(externalId);
        var matchingDomain = await domains.Query
            .Where(x => x.ApiKey != null && x.ApiKey.Hash == hash)
            .FirstOrDefaultAsync() 
            ?? throw new Exception("Invalid API key.");

        var user = await users.FindAsync(matchingDomain.TovikUserId ?? matchingDomain.Users.First());
        principal = user!.ToPrincipal(authenticationType, hash);
        await users.UpdateAsync(user);
        return principal;
    }

    public async Task<BlossomLogin> DoLogin(ClaimsPrincipal principal, string? emailOrToken = null)
    {
        Message = null;

        // 1. Convert the ClaimsPrincipal from the cookie into a BlossomUser
        SparcUser = await GetUserAsync(principal);

        try
        {
            if (emailOrToken != null)
            {
                // Verify Authentication Token or Register
                if (emailOrToken.StartsWith("verify"))
                    await VerifyTokenAsync(emailOrToken);
                else if (emailOrToken.StartsWith("totp"))
                    await VerifyTotpAsync(emailOrToken);
                else
                {
                    var authenticationType = TwilioService.IsValidEmail(emailOrToken) ? "Email" : "Phone";
                    var identity = SparcUser.GetOrCreateIdentity(authenticationType, emailOrToken);
                    await UpdateAsync(SparcUser);

                    if (!identity.IsVerified)
                        await SendVerificationCodeAsync(SparcUser, identity);
                }
            }
        }
        catch
        {
            return new(SparcUser);
        }

        return tokens.Create(SparcUser);
    }

    private async Task<BlossomUser> VerifyTotpAsync(string emailOrToken)
    {
        var (userId, identityId) = SparcCodes.Verify(emailOrToken)
                        ?? throw new InvalidOperationException("Invalid TOTP code.");

        var matchingUser = await users.Query
            .Where(x => x.Id == userId)
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("User not found for the provided TOTP code.");

        var identity = matchingUser.Identities.FirstOrDefault(i => i.Id == identityId)
            ?? throw new InvalidOperationException("Identity not found for the provided TOTP code.");

        identity.IsVerified = true;
        await UpdateAsync(matchingUser);

        await LoginAsync(matchingUser.ToPrincipal());
        return matchingUser;
    }

    public async Task<SparcCode> Register(ClaimsPrincipal principal, BlossomIdentity? identity = null)
    {
        SparcUser = await GetUserAsync(principal);
        if (identity?.Type == "Email")
        {
            SparcUser.GetOrCreateIdentity(identity.Type, identity.Id);
            await UpdateAsync(SparcUser);
            return new SparcCode();
        }

        var passwordlessToken = await StartPasskeyRegistrationAsync(SparcUser);
        return new SparcCode(passwordlessToken);
    }

    private async Task<BlossomUser> VerifyTokenAsync(string token)
    {
        VerifiedUser? verifiedUser;
        try
        {
            verifiedUser = await _passwordlessClient.VerifyAuthenticationTokenAsync(token);
        }
        catch (Exception ex)
        {
            Message = $"Token verification failed: {ex.Message}";
            LoginState = LoginStates.Error;
            throw new InvalidOperationException(Message, ex);
        }

        if (verifiedUser?.Success != true)
        {
            Message = "Invalid or expired token.";
            LoginState = LoginStates.Error;
            throw new InvalidOperationException(Message);
        }

        SparcUser = await users.FindAsync(verifiedUser.UserId);

        if (SparcUser == null)
        {
            Message = "User not found for the provided token.";
            LoginState = LoginStates.Error;
            throw new InvalidOperationException(Message);
        }

        var identity = SparcUser.GetOrCreateIdentity("Passwordless", verifiedUser.UserId);
        identity.IsVerified = true;
        await SaveAsync();
        return SparcUser;
    }

    public async Task<BlossomUser> DoLogout(ClaimsPrincipal principal, string? emailOrToken = null)
    {
        SparcUser = await GetUserAsync(principal);
        SparcUser.Logout();
        await SaveAsync();

        var login = await DoLogin(new(new ClaimsIdentity()));
        return login.User;
    }

    private async Task<string> StartPasskeyRegistrationAsync(BlossomUser user)
    {
        var options = new RegisterOptions(user.Id, user.Avatar.Username)
        {
            Aliases = [user.Avatar.Username]
        };

        var registerToken = await _passwordlessClient.CreateRegisterTokenAsync(options);
        return registerToken.Token;
    }

    private async Task SaveAsync()
    {
        await users.UpdateAsync(SparcUser!);
        await LoginAsync(SparcUser!.ToPrincipal());
        User = SparcUser;
    }

    public async Task<BlossomUser> UpdateAsync(BlossomUser user)
    {
        await users.UpdateAsync((T)user);
        return user;
    }

    public override async Task<BlossomUser> UpdateAsync(ClaimsPrincipal principal, BlossomAvatar avatar)
    {
        await base.UpdateAsync(principal, avatar);
        await LoginAsync(principal);
        return User!;
    }

    public async Task SendVerificationCodeAsync(BlossomUser user, BlossomIdentity identity)
    {
        identity.Revoke();

        var code = SparcCodes.Generate(user, identity);
        var message = $"Your Sparc verification code is: {code!.Code}";
        var subject = "Sparc Verification Code";

        await twilio.SendAsync(identity.Id, message, subject);
        await SaveAsync();
    }

    //internal async Task<SparcCode?> GetSparcCode(ClaimsPrincipal principal)
    //{
    //    var user = await GetAsync(principal);
    //    return SparcCodes.Generate(user);
    //}

    private void UpdateFromHttpContext(ClaimsPrincipal principal)
    {
        if (http?.HttpContext != null && User != null)
        {
            User.LastPageVisited = http.HttpContext.Request.Path;

            if (string.IsNullOrWhiteSpace(User.Avatar.Username))
            {
                User.ChangeUsername(friendlyId.Create(1, 2));
            }

            var acceptLanguage = http.HttpContext.Request.Headers.AcceptLanguage;
            if (User.Avatar.Language == null && !string.IsNullOrWhiteSpace(acceptLanguage))
            {
                var newLanguage = Language.Find(acceptLanguage!);
                if (newLanguage != null)
                    User.ChangeLanguage(newLanguage);

                var newLocale = Contents.GetLocale(acceptLanguage!);
                if (newLocale != null)
                    User.Avatar.Locale = newLocale;

                if (User.Avatar.Currency == null)
                {
                    var languageId = Language.IdsFrom(acceptLanguage).FirstOrDefault();
                    if (languageId != null)
                        User.Avatar.Currency = SparcCurrency.From(languageId);
                }
            }
        }
    }

    public void Map(IEndpointRouteBuilder endpoints)
    {
        var auth = endpoints.MapGroup("/aura").RequireCors("Auth");
        //auth.MapGet("login", DoLogin);
        //auth.MapGet("logout", DoLogout);
        auth.MapPost("register", async (SparcAuthenticator<T> auth, ClaimsPrincipal principal, BlossomIdentity? identity = null) => await auth.Register(principal, identity));
        auth.MapPost("login", async (SparcAuthenticator<T> auth, ClaimsPrincipal principal, string? emailOrToken = null) => await auth.DoLogin(principal, emailOrToken));
        auth.MapPost("logout", async (SparcAuthenticator<T> auth, ClaimsPrincipal principal, string? emailOrToken = null) => await auth.DoLogout(principal, emailOrToken));
        auth.MapGet("userinfo", async (SparcAuthenticator<T> auth, ClaimsPrincipal principal) => await GetAsync(principal));
        auth.MapPost("userinfo", async (SparcAuthenticator<T> auth, ClaimsPrincipal principal, BlossomAvatar avatar) => await auth.UpdateAsync(principal, avatar));
        //auth.MapGet("code", async (SparcAuthenticator<T> auth, ClaimsPrincipal principal) => await GetSparcCode(principal));
    }
}