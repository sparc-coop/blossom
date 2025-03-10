using Ardalis.Specification;

namespace Sparc.Blossom.Data;

public record MangoQuery(Dictionary<string, Dictionary<string, object?>> Selector, List<string>? Fields, List<Dictionary<string, string>>? Sort, int? Limit, int? Skip);

public partial class PouchDbSpecification<T>(ISpecification<T> spec)
{
    public MangoQuery Query { get; } = new MangoQuery(
            PouchDbSpecification<T>.GenerateSelector(spec.WhereExpressions),
            null,
            PouchDbSpecification<T>.GenerateSort(spec.OrderExpressions),
            spec.Take ?? 25,
            spec.Skip);

    private static Dictionary<string, Dictionary<string, object?>> GenerateSelector(IEnumerable<WhereExpressionInfo<T>> criteria)
    {
        if (!criteria.Any())
            return [];

        var selector = new Dictionary<string, Dictionary<string, object?>>();

        foreach (var where in criteria)
        {
            var visitor = new MangoQueryExpressionVisitor();
            visitor.Visit(where.Filter);
            selector.Add(ToCamelCase(visitor.Field), new Dictionary<string, object?> { { visitor.Op, visitor.Value } });
        }

        return selector;
    }

    private static List<Dictionary<string, string>>? GenerateSort(IEnumerable<OrderExpressionInfo<T>>? orderExpressions)
    {
        if (orderExpressions == null || !orderExpressions.Any())
            return null;

        var sort = orderExpressions.Select(order =>
        {
            var field = order.KeySelector.Body.ToString().Split('.').Last();
            var direction = order.OrderType == OrderTypeEnum.OrderBy ? "asc" : "desc";
            return new Dictionary<string, string> { { ToCamelCase(field), direction } };
        });

        return sort.ToList();
    }

    static string ToCamelCase(string value) => char.ToLowerInvariant(value[0]) + value[1..];
}
