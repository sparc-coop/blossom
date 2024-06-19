namespace Sparc.Blossom.Authentication;

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
