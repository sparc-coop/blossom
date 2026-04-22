namespace Sparc.Blossom.Realtime;

public interface IBlossomChannels
{
    IAsyncEnumerable<BlossomEvent> Watch(string source);
    Task<string> Publish<T>(T ev, CancellationToken cancellationToken = default);
    Task<string> Publish<T>(string source, T ev, CancellationToken cancellationToken = default);
    Task<string> Execute(Delegate value);
}
