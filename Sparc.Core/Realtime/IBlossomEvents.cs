namespace Sparc.Blossom.Realtime;

public interface IBlossomEvents
{
    IAsyncEnumerable<BlossomEvent> Watch(string source);
    Task Publish<T>(T ev, CancellationToken cancellationToken = default) where T : BlossomEvent;
    Task Publish<T>(string source, T ev, CancellationToken cancellationToken = default);
    Task<string> Execute(Delegate value);
}
