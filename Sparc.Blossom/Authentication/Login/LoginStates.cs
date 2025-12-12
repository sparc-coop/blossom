namespace Sparc.Blossom.Authentication;

public enum LoginStates
{
    NotInitialized,
    LoggedOut,
    ReadyForLogin,
    VerifyingEmail,
    AwaitingMagicLink,
    AwaitingPasskey,
    VerifyingToken,
    LoggedIn,
    LoggingOut,
    Error
}
