using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Passwordless;
using Sparc.Blossom.Cloud.Tools;
using System.Security.Claims;
using Sparc.Blossom.Content;
using Sparc.Notifications.Twilio;

namespace Sparc.Blossom.Authentication;

public class BlossomPasswordlessAuthenticator<T> : BlossomDefaultAuthenticator<T>, IBlossomCloudApi
    where T : BlossomUser, new()
{
    IPasswordlessClient PasswordlessClient { get; }
    public FriendlyId FriendlyId { get; }
    public FriendlyUsername FriendlyUsername { get; }
    public HttpClient Client { get; }
    public TwilioService Twilio { get; }

    public BlossomPasswordlessAuthenticator(
        IPasswordlessClient _passwordlessClient,
        IOptions<PasswordlessOptions> options,
        IRepository<T> users,
        TwilioService twilio,
        FriendlyId friendlyId,
        FriendlyUsername friendlyUsername)
        : base(users)
    {
        PasswordlessClient = _passwordlessClient;
        Twilio = twilio;
        FriendlyId = friendlyId;
        FriendlyUsername = friendlyUsername;
        Client = new HttpClient
        {
            BaseAddress = new Uri("https://v4.passwordless.dev/")
        };
        Client.DefaultRequestHeaders.Add("ApiSecret", options.Value.ApiSecret);
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

        if (User!.Username == null || User!.Username == "User")
        {
            User.ChangeUsername(FriendlyUsername.GetRandomName());
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

    public async Task<BlossomUser> AddLanguageAsync(ClaimsPrincipal principal, Language language)
    {
        await base.GetUserAsync(principal);

        if (User is null)
            throw new InvalidOperationException("User not initialized");

        User.ChangeLanguage(language);
        await SaveAsync();

        return User;
    }

    public async Task<BlossomUser> UpdateUserAsync(ClaimsPrincipal principal, UpdateUserRequest request)
    {
        await base.GetUserAsync(principal);
        if (User is null)
            throw new InvalidOperationException("User not initialized");

        var shouldSave = false;

        if (!string.IsNullOrWhiteSpace(request.Username) && User.Username != request.Username)
        {
            User.Username = request.Username;
            shouldSave = true;
        }

        if (!string.IsNullOrWhiteSpace(request.Email) && User.Email != request.Email)
        {
            if (request.RequireEmailVerification)
            {                
                await SendVerificationCodeAsync(principal, request.Email);
            }
            else
            {
                User.Email = request.Email;
                shouldSave = true;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber) && User.PhoneNumber != request.PhoneNumber)
        {
            if (request.RequirePhoneVerification)
            {
                await SendVerificationCodeAsync(principal, request.PhoneNumber);
            }
            else
            {
                User.PhoneNumber = request.PhoneNumber;
                shouldSave = true;
            }
        }

        if (shouldSave)
            await SaveAsync();

        return User;
    }

    public async Task<BlossomUser> UpdateAvatarAsync(ClaimsPrincipal principal, UpdateAvatarRequest request)
    {
        await base.GetUserAsync(principal);
        if (User is null)
            throw new InvalidOperationException("User not initialized");

        var avatar = new UserAvatar(User.Avatar)
        {
            Id = User.Id,
            Name = request.Name ?? User.Avatar.Name,
            BackgroundColor = request.BackgroundColor ?? User.Avatar.BackgroundColor,
            Pronouns = request.Pronouns ?? User.Avatar.Pronouns,
            Description = request.Description ?? User.Avatar.Description,
            SkinTone = request.SkinTone ?? User.Avatar.SkinTone,
            Emoji = request.Emoji ?? User.Avatar.Emoji,
            Gender = request.Gender ?? User.Avatar.Gender
        };

        User.UpdateAvatar(avatar);
        await SaveAsync();
        return User;
    }


    public async Task SendVerificationCodeAsync(ClaimsPrincipal principal, string destination)
    {
        await base.GetUserAsync(principal);

        if (User is null)
            throw new InvalidOperationException("User not initialized");

        User.Revoke();
        User.EmailOrPhone = destination;

        var code = User.GenerateVerificationCode();
        var message = $"Your Sparc verification code is: {code}";
        var subject = "Sparc Verification Code";

        await Twilio.SendAsync(destination, message, subject);
        await SaveAsync();
    }

    public async Task<bool> VerifyCodeAsync(ClaimsPrincipal principal, string destination, string code)
    {
        await base.GetUserAsync(principal);

        if (User is null)
            throw new InvalidOperationException("User not initialized");

        var success = User.VerifyCode(code);

        if (success)
        {
            if (destination.Contains("@"))
                User.Email = destination;
            else
                User.PhoneNumber = destination;

            await SaveAsync();
        }

        return success;
    }

    public void Map(IEndpointRouteBuilder endpoints)
    {
        var auth = endpoints.MapGroup("/auth");
        auth.MapPost("login", async (BlossomPasswordlessAuthenticator<T> auth, ClaimsPrincipal principal, HttpContext context, string? emailOrToken = null) => await auth.Login(principal, context, emailOrToken));
        auth.MapPost("logout", async (BlossomPasswordlessAuthenticator<T> auth, ClaimsPrincipal principal, string? emailOrToken = null) => await auth.Logout(principal, emailOrToken));
        auth.MapGet("userinfo", async (BlossomPasswordlessAuthenticator<T> auth, ClaimsPrincipal principal) => await auth.GetAsync(principal));
        auth.MapPost("user-products", async (BlossomPasswordlessAuthenticator<T> auth, ClaimsPrincipal principal, [FromBody] AddProductRequest request) => await auth.AddProductAsync(principal, request.ProductName));
        auth.MapPost("update-user", async (BlossomPasswordlessAuthenticator<T> auth, ClaimsPrincipal principal, [FromBody] UpdateUserRequest request) => await auth.UpdateUserAsync(principal, request));
        auth.MapPost("user-languages", async (BlossomPasswordlessAuthenticator<T> auth, ClaimsPrincipal principal, [FromBody] Language language) => await auth.AddLanguageAsync(principal, language));
        auth.MapPost("verify-code", async (BlossomPasswordlessAuthenticator<T> auth, ClaimsPrincipal principal, [FromBody] VerificationRequest request) => await auth.VerifyCodeAsync(principal, request.EmailOrPhone, request.Code));
        auth.MapPost("update-avatar", async (BlossomPasswordlessAuthenticator<T> auth, ClaimsPrincipal principal, [FromBody] UpdateAvatarRequest request) => await auth.UpdateAvatarAsync(principal, request));

    }

    private async Task LoginWithPasswordless(HttpContext context)
    {
        throw new NotImplementedException();
    }
}