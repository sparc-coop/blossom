using Ardalis.Specification;

namespace Sparc.Blossom.Data;

public class BlossomQuery();

public class BlossomQuery<T> : Specification<T> where T : class
{
    public BlossomQuery() => Query.AsNoTracking();
    
    protected void ForEach(Action<T> action)
    {
        Query.PostProcessingAction(x =>
        {
            foreach (var item in x)
                action(item);

            return x;
        });
    }
}

public class BlossomQuery<T, TResult> : Specification<T, TResult>
{
    protected void ForEach(Action<T> action)
    {
        Query.PostProcessingAction(x =>
        {
            foreach (var item in x)
                action(item);

            return x;
        });
    }
}
