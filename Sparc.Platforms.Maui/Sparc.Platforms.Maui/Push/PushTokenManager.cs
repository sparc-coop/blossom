using System;

namespace Sparc.Platforms.Maui;

public class PushTokenManager
{
    public PushTokenManager(Func<string> onTokenChanged)
    {
        TokenChanged = onTokenChanged;
    }

    internal void UpdateToken(string token)
    {
        Token = token;
        TokenChanged();
    }

    public string Token { get; private set; }
    public Func<string> TokenChanged;
}
