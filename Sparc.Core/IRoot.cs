using System.Collections.Generic;

namespace Sparc.Core
{
    public interface IRoot<T>
    {
        T Id { get; set; }
    }
}