using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Sparc.Blossom.Authentication;

public sealed class BlossomAccessTokenProviderAccessor : IAccessTokenProviderAccessor
{
    private readonly IServiceProvider _provider;
    private IAccessTokenProvider? _tokenProvider;

    public BlossomAccessTokenProviderAccessor(IServiceProvider provider) => _provider = provider;

    public IAccessTokenProvider TokenProvider => _tokenProvider ??= _provider.GetRequiredService<IAccessTokenProvider>();
}