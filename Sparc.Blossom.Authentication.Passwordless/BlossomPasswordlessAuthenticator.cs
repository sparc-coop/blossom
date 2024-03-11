
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Passwordless.Net;

namespace Sparc.Blossom.Authentication.Passwordless
{
    public class BlossomPasswordlessAuthenticator<TUser>(
        UserManager<TUser> userManager,
        SignInManager<TUser> signInManager,
        IPasswordlessClient passwordlessClient,
        IHttpContextAccessor http)
        : BlossomAuthenticator where TUser : BlossomUser, new()
    {
        public UserManager<TUser> UserManager { get; } = userManager;
        public SignInManager<TUser> SignInManager { get; } = signInManager;
        public IHttpContextAccessor Http { get; } = http;
        public IPasswordlessClient PasswordlessClient { get; } = passwordlessClient;

        public override async Task<BlossomUser?> GetAsync()
        {
            var principal = Http.HttpContext?.User ??
                throw new InvalidOperationException($"{nameof(GetAsync)} requires access to an {nameof(HttpContext)}.");

            var user = await UserManager.GetUserAsync(principal) ?? throw new Exception("/");
            return user;
        }

        public override async Task<BlossomUser?> LoginAsync(string token)
        {
            var verifiedUser = await PasswordlessClient.VerifyTokenAsync(token);
            if (verifiedUser?.Success == true)
            {
                var user = await GetOrCreateAsync(verifiedUser.UserId);
                await SignInManager.SignInAsync(user, true);
                return user;
            }

            return null;
        }

        public override async Task<BlossomUser?> RegisterAsync(string userName)
        {
            var user = await GetOrCreateAsync(userName);
            var payload = new RegisterOptions(user.Id, user.Identity.UserName!)
            {
                Aliases = [user.Identity.UserName!]
            };
            
            var token = await PasswordlessClient.CreateRegisterTokenAsync(payload);
            user.Identity.SecurityStamp = token.Token;
            return user;
        }

        private async Task<TUser> GetOrCreateAsync(string username)
        {
            var user = Guid.TryParse(username, out Guid id)
                ? await UserManager.FindByIdAsync(username)
                : await UserManager.FindByNameAsync(username);

            if (user == null)
            {
                if (id != Guid.Empty)
                    throw new ArgumentException($"User ID {id} not found.");

                user = new();
                user.Identity.UserName = username;
                await UserManager.CreateAsync(user);
            }

            return user;
        }
    }
}
