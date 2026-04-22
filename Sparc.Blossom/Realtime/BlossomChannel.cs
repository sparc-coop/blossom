using System.Threading.Channels;

namespace Sparc.Blossom.Realtime;

public class BlossomChannel<T> where T : class
{
    readonly Channel<T> _queue;

    public BlossomChannel(int capacity = 200)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _queue = Channel.CreateBounded<T>(options);
    }

    public ChannelReader<T> Reader => _queue.Reader;
    public ChannelWriter<T> Writer => _queue.Writer;
}