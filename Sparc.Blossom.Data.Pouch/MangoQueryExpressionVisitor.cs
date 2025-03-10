using System.Linq.Expressions;

namespace Sparc.Blossom.Data;

public partial class PouchDbSpecification<T>
{
    class MangoQueryExpressionVisitor : ExpressionVisitor
    {
        public string Field { get; private set; }
        public string Op { get; private set; }
        public object? Value { get; private set; }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            Op = node.NodeType switch
            {
                ExpressionType.Equal => "$eq",
                ExpressionType.NotEqual => "$ne",
                ExpressionType.GreaterThan => "$gt",
                ExpressionType.GreaterThanOrEqual => "$gte",
                ExpressionType.LessThan => "$lt",
                ExpressionType.LessThanOrEqual => "$lte",
                ExpressionType.AndAlso => "$and",
                ExpressionType.OrElse => "$or",
                _ => throw new NotSupportedException($"The binary operator '{node.NodeType}' is not supported"),
            };

            Visit(node.Left);
            Visit(node.Right);

            return node;
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType == ExpressionType.Not)
            {
                Op = "$not";
            }
            Visit(node.Operand);
            return node;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            Value = node.Value;
            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            Field = node.Member.Name;
            return base.VisitMember(node);
        }
    }
}
