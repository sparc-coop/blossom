using Microsoft.AspNetCore.Http;
using Sparc.Blossom.Data;
using Sparc.Blossom.Api;
using System.Security.Claims;

namespace Sparc.Blossom;

public class BlossomContext<T>(BlossomDbContext<T> database, BlossomApiContext<T> api, IHttpContextAccessor http) where T : Entity
{
    public BlossomDbContext<T> Database { get; } = database;
    public BlossomApiContext<T> Api { get; } = api;
    public ClaimsPrincipal User => http.HttpContext?.User ?? new ClaimsPrincipal();

    
}
