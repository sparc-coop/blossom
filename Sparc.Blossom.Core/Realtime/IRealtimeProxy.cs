namespace Sparc.Blossom.Realtime;

public enum ConnectionStates
{
    Disconnected,
    Connecting,
    Reconnecting,
    Connected
}

public interface IRealtimeProxy : IAsyncDisposable
{
    void Initialize(bool isActive);
    Task ConnectAsync();
    ConnectionStates ConnectionState { get; }
}
