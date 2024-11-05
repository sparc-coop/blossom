namespace Sparc.Blossom.Authentication;

public enum LoginStates
{
    NotInitialized,
    LoggedOut,
    ReadyForLogin,
    VerifyingEmail,
    AwaitingMagicLink,
    VerifyingToken,
    LoggedIn,
    LoggingOut,
    Error
}