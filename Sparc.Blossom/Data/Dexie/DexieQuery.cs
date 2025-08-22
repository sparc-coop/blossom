using Ardalis.Specification;
using Microsoft.JSInterop;
using System.Linq.Expressions;

namespace Sparc.Blossom;

public class DexieQuery<T>(DexieDatabase db)
{
    void CheckIndex(string propertyName)
    {
        var typeName = typeof(T).Name.ToLower();
        propertyName = propertyName.ToLower();
        if (!db.Repositories[typeName].Any(x => x == propertyName))
            throw new InvalidOperationException($"Property '{propertyName}' is not indexed in the database.");
    }

    public async Task<List<T>> ExecuteAsync(ISpecification<T> spec)
    {
        var query = await db.Set<T>();
        foreach (var where in spec.WhereExpressions)
            query = await Where(query, where);
        foreach (var order in spec.OrderExpressions)
            query = await OrderBy(query, order);

        var result = await query.InvokeAsync<List<T>>("toArray");
        return result;
    }

    public async Task<IJSObjectReference> Where(IJSObjectReference set, WhereExpressionInfo<T> where)
    {
        if (where.Filter.Body is BinaryExpression binary)
        {
            // Left: property access
            var member = binary.Left as MemberExpression;
            var propertyName = member?.Member.Name ?? throw new InvalidOperationException("Left side is not a property.");
            CheckIndex(propertyName);

            // Operator: ExpressionType (e.g., Equal, GreaterThan)
            var op = binary.NodeType switch
            {
                ExpressionType.Equal => "equals",
                ExpressionType.NotEqual => "notEqual",
                ExpressionType.GreaterThan => "above",
                ExpressionType.GreaterThanOrEqual => "aboveOrEqual",
                ExpressionType.LessThan => "below",
                ExpressionType.LessThanOrEqual => "belowOrEqual",
                _ => throw new NotSupportedException($"Operator {binary.NodeType} is not supported."),
            };

            // Right: constant or value
            object? value = null;
            if (binary.Right is ConstantExpression constExpr)
                value = constExpr.Value;
            else if (binary.Right is MemberExpression rightMember)
            {
                // Handles captured variables
                var objectMember = Expression.Lambda(rightMember).Compile().DynamicInvoke();
                value = objectMember;
            }

            set = await set.InvokeAsync<IJSObjectReference>("where", propertyName);
            set = await set.InvokeAsync<IJSObjectReference>(op, value);
            return set;
        }
        throw new NotSupportedException("Only simple binary expressions are supported.");
    }

    public async Task<IJSObjectReference> OrderBy(IJSObjectReference set, OrderExpressionInfo<T> order)
    {
        var member = order.KeySelector.Body as MemberExpression 
            ?? throw new InvalidOperationException("Order expression is not a property access.");
        
        var propertyName = member.Member.Name;
        CheckIndex(propertyName);

        set = await set.InvokeAsync<IJSObjectReference>("orderBy", propertyName);
        
        if (order.OrderType == OrderTypeEnum.OrderByDescending)
            set = await set.InvokeAsync<IJSObjectReference>("reverse");
        
        return set;
    }
}
