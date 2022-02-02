using System.Collections.Generic;

namespace Sparc.Core
{
    public class Root
    {
    }

    public class Root<T> : Root, IRoot<T> where T : notnull
    {
        public Root()
        {
            Id = default!;
        }
        
        public Root(T id) => Id = id;
        
        public virtual T Id { get; set; }
    }
}
