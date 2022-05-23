using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Sparc.Kernel.Authentication;

public class SwaggerAuthorizeFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Get Authorize attribute
        var attributes = context.MethodInfo.DeclaringType!.GetCustomAttributes(true)
                                .Union(context.MethodInfo.GetCustomAttributes(true));

        if (attributes != null && attributes.OfType<AuthorizeAttribute>().Any() && !attributes.OfType<AllowAnonymousAttribute>().Any())
        {
            var attr = attributes.OfType<AuthorizeAttribute>().ToList()[0];

            // Add what should be show inside the security section
            IList<string> securityInfos = new List<string>
            {
                $"{nameof(AuthorizeAttribute.Policy)}:{attr.Policy}",
                $"{nameof(AuthorizeAttribute.Roles)}:{attr.Roles}",
                $"{nameof(AuthorizeAttribute.AuthenticationSchemes)}:{attr.AuthenticationSchemes}"
            };

            operation.Security = new List<OpenApiSecurityRequirement>()
                {
                    new OpenApiSecurityRequirement()
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Id = "bearer", // Must fit the defined Id of SecurityDefinition in global configuration
                                    Type = ReferenceType.SecurityScheme
                                }
                            },
                            securityInfos
                        }
                    }
                };
        }
        else
        {
            operation.Security.Clear();
        }
    }
}
