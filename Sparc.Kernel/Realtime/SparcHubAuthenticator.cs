using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace Sparc.Realtime;

public class SparcHubAuthenticator : IPostConfigureOptions<JwtBearerOptions>
{
    public SparcHubAuthenticator(string hubName)
    {
        HubName = hubName;
    }

    public string HubName { get; }

    public void PostConfigure(string? name, JwtBearerOptions options)
    {
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (string.IsNullOrEmpty(context.Token))
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;

                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments($"/{HubName}"))
                    {
                        context.Token = accessToken;
                    }
                }
                return Task.CompletedTask;
            }
        };
    }
}