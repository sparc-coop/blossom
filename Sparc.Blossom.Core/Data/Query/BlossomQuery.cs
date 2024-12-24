using Ardalis.Specification;
using System.Linq.Expressions;

namespace Sparc.Blossom;

public class BlossomQuery();

public class BlossomQuery<T> : Specification<T> where T : class
{
    public IRepository<T> Repository { get; }
    public BlossomQuery(IRepository<T> repository)
    {
        Query.AsNoTracking();
        Repository = repository;
    }

    public BlossomQuery<T> Where(Expression<Func<T, bool>> expression)
    {
        Query.Where(expression);
        return this;
    }

    public BlossomQuery<T> OrderBy(Expression<Func<T, object?>> expression, params Expression<Func<T, object?>>[] thenBy)
    {
        var query = Query.OrderBy(expression);

        foreach (var then in thenBy)
            query.ThenBy(then);

        return this;
    }

    public BlossomQuery<T> Include(string path)
    {
        Query.Include(path);
        return this;
    }

    public BlossomQuery<T> Include(Expression<Func<T, object>> expression)
    {
        Query.Include(expression);
        return this;
    }
    
    public BlossomQuery<T> SkipTake(int skip, int take)
    {
        Query.Skip(skip).Take(take);
        return this;
    }

    public BlossomQuery<T> WithOptions(BlossomQueryOptions options)
    {
        Query.WithOptions(options);
        return this;
    }

    protected void ForEach(Action<T> action)
    {
        Query.PostProcessingAction(x =>
        {
            foreach (var item in x)
                action(item);

            return x;
        });
    }

    public async Task<IEnumerable<T>> Execute() => await Repository.GetAllAsync(this);
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
    public static ISpecificationBuilder<T> WithOptions<T>(this ISpecificationBuilder<T> query, BlossomQueryOptions options) where T : class
    {
        query.Skip(options.Skip);
        if (options.Take.HasValue)
            query.Take(options.Take.Value);
        return query;
    }
}
