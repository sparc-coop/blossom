using System.ComponentModel;

namespace Sparc.Blossom;

public interface IBlossomEntityProxy : INotifyPropertyChanged
{
    object GenericId { get; }
    IRunner GenericRunner { get; }
}

public interface IBlossomEntityProxy<T>
{
    IRunner<T> Runner { get; set; }
}
