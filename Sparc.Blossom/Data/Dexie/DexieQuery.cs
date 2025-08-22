using Ardalis.Specification;
using Microsoft.JSInterop;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;

namespace Sparc.Blossom.Data.Dexie;

public class DexieQuery<T>(DexieDatabase db)
{
    IJSObjectReference? Query;
    
    void CheckIndex(string propertyName)
    {
        var typeName = typeof(T).Name.ToLower();
        propertyName = propertyName.ToLower();
        if (!db.Repositories[typeName].Any(x => x == propertyName))
            throw new InvalidOperationException($"Property '{propertyName}' is not indexed in the database.");
    }

    public async Task<DexieQuery<T>> ApplyAsync(ISpecification<T> spec)
    {
        Query = await db.Set<T>();

        foreach (var where in spec.WhereExpressions)
            Query = await Where(Query, where);
        if (spec.Skip > 0)
            Query = await Query.InvokeAsync<IJSObjectReference>("offset", spec.Skip);
        if (spec.Take > 0)
            Query = await Query.InvokeAsync<IJSObjectReference>("limit", spec.Take);
        foreach (var order in spec.OrderExpressions)
            Query = await OrderBy(Query, order);

        return this;
    }

    public async Task<int> CountAsync()
    {
        Query ??= await db.Set<T>();
        var result = await Query.InvokeAsync<int>("count");
        return result;
    }

    public async Task<List<T>> ToListAsync()
    {
        Query ??= await db.Set<T>();
        var result = await Query.InvokeAsync<List<T>>("toArray");
        return result;
    }

    public async Task<IJSObjectReference> Where(IJSObjectReference set, WhereExpressionInfo<T> where)
    {
        var expression = new DexieQueryExpression();
        expression.Visit(where.Filter);

        set = await set.InvokeAsync<IJSObjectReference>("where", expression.Field);
        set = await set.InvokeAsync<IJSObjectReference>(expression.Op, expression.Value);
        return set;
    }

    public async Task<IJSObjectReference> OrderBy(IJSObjectReference set, OrderExpressionInfo<T> order)
    {
        var member = order.KeySelector.Body as MemberExpression
            ?? throw new InvalidOperationException("Order expression is not a property access.");

        var propertyName = member.Member.Name;
        CheckIndex(propertyName);

        set = await set.InvokeAsync<IJSObjectReference>("sortBy", propertyName);

        if (order.OrderType == OrderTypeEnum.OrderByDescending)
            set = await set.InvokeAsync<IJSObjectReference>("reverse");

        return set;
    }
}
