using Ardalis.Specification;

namespace Sparc.Blossom.Data;

public class BlossomQuery<T> : Specification<T>
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
