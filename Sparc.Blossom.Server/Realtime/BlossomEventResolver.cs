using MessagePack.Formatters;
using MessagePack.Resolvers;
using MessagePack;

namespace Sparc.Blossom.Realtime
{
    public static class BlossomEventResolver
    {
        public static readonly IFormatterResolver Instance =
            CompositeResolver.Create(
                new IMessagePackFormatter[]
                {
                new BlossomEntityChangedFormatter()
                },
                new IFormatterResolver[]
                {
                StandardResolver.Instance // Fallback to standard resolver for other types
                }
            );
    }

}
