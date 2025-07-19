using System.Linq.Expressions;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking.Linq;

internal static class QueryExpressionChecker
{
    public static bool HasOrderBy(Expression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var visitor = new Visitor();
        visitor.Visit(expression);
        return visitor.ContainsOrderBy;
    }

    private sealed class Visitor : ExpressionVisitor
    {
        public bool ContainsOrderBy { get; private set; }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            ContainsOrderBy |= IsOrderByMethod(node);

            return base.VisitMethodCall(node);
        }

        private static bool IsOrderByMethod(MethodCallExpression node)
            => (node.Method.Name.Equals("OrderBy", StringComparison.Ordinal) || node.Method.Name.Equals("OrderByDescending", StringComparison.Ordinal))
               && (node.Method.DeclaringType?.Name.Equals("Queryable", StringComparison.Ordinal) ?? false)
               && (node.Method.DeclaringType?.Namespace?.Equals("System.Linq", StringComparison.Ordinal) ?? false);
    }
}
