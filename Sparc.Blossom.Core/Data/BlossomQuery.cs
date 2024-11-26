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

public static class BlossomQueryExtensions
{
    public static ISpecificationBuilder<T> WithOptions<T>(this ISpecificationBuilder<T> query, Api.BlossomQueryOptions options) where T : class
    {
        query.Skip(options.Skip);
        if (options.Take.HasValue)
            query.Take(options.Take.Value);
        return query;
    }
}
