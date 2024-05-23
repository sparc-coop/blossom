namespace Sparc.Blossom.Authenticator
{
    public enum LoginStates
    {
        LoggedOut,
        ReadyForLogin,
        VerifyingEmail,
        AwaitingMagicLink,
        VerifyingToken,
        LoggedIn,
        LoggingOut,
        Error
    }
}
