using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;

namespace Microsoft.EntityFrameworkCore.ConcurrentChunking;

internal static class EntityQueryRootExpressionExtractor
{
    public static EntityQueryRootExpression? Extract(Expression expression)
    {
        var visitor = new Visitor();
        visitor.Visit(expression);
        return visitor.EntityQueryRootExpression;
    }

    private sealed class Visitor : ExpressionVisitor
    {
        public EntityQueryRootExpression? EntityQueryRootExpression { get; private set; }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Arguments.Count > 0 && node.Arguments[0] is EntityQueryRootExpression entityQueryRootExpression)
            {
                EntityQueryRootExpression ??= entityQueryRootExpression;
            }

            return base.VisitMethodCall(node);
        }
    }
}
