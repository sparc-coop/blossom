using System.Threading.Channels;

namespace Sparc.Blossom.Realtime;

public class BlossomChannel<T> where T : class
{
    readonly Channel<T> _queue;

    public BlossomChannel(int? capacity = null)
    {
        if (capacity == null)
            _queue = Channel.CreateUnbounded<T>(new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = false
            });
        else
        {
            var options = new BoundedChannelOptions(capacity.Value)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false
            };
            _queue = Channel.CreateBounded<T>(options);
        }
    }

    public ChannelReader<T> Reader => _queue.Reader;
    public ChannelWriter<T> Writer => _queue.Writer;
}