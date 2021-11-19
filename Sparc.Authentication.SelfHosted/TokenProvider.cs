using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Sparc.Core;

namespace Sparc.Authentication.SelfHosted
{
    public class DefaultTokenProvider : ITokenProvider
    {
        public DefaultTokenProvider(IHttpContextAccessor accessor)
        {
            if (accessor?.HttpContext?.User?.Identity?.IsAuthenticated == false)
                accessor.HttpContext.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme).Wait();

            Token = accessor?.HttpContext?.User?.FindFirst("sub")?.Value;
        }

        public string? Token { get; set; }
    }
}
