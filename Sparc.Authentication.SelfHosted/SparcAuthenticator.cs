using IdentityServer4;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Sparc.Core;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Sparc.Authentication.SelfHosted
{
    public abstract class SparcAuthenticator
    {
        public SparcAuthenticator(IIdentityServerInteractionService interaction, IHttpContextAccessor httpContextAccessor)
        {
            Interaction = interaction;
            Context = httpContextAccessor.HttpContext;
        }

        public IIdentityServerInteractionService Interaction { get; }
        public HttpContext? Context { get; }
        public AuthorizationRequest? AuthorizationContext { get; private set; }
        public bool IsTrusted { get; private set; }

        public async Task InitializeAsync(string returnUrl)
        {
            AuthorizationContext = await Interaction.GetAuthorizationContextAsync(returnUrl);
            IsTrusted = AuthorizationContext != null;
        }

        public abstract Task<List<Claim>> GetClaimsAsync(string userId);

        public abstract Task<bool> IsActiveAsync(string userId);

        public abstract Task<bool> LoginAsync(string userName, string password);
        public abstract string? GetUserId(ClaimsPrincipal? principal);

        protected async Task CompleteLoginAsync(string userId, string userName)
        {
            var identity = new IdentityServerUser(userId)
            {
                DisplayName = userName
            };

            var props = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
            };

            await Context.SignInAsync(identity, props);
        }
    }
}
