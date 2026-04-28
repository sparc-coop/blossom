namespace Sparc.Blossom.Realtime;

public abstract class BlossomOn<T>
{
    public abstract Task ExecuteAsync(T item);
}