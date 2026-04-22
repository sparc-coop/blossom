namespace Sparc.Blossom.Realtime;

public interface IBlossomChannels
{
    IAsyncEnumerable<BlossomEvent> Watch(string source);
    Task Publish<T>(T ev, CancellationToken cancellationToken = default);
    Task Publish<T>(string source, T ev, CancellationToken cancellationToken = default);
    Task<string> Execute(Delegate value);
}
