using System.Linq.Expressions;

namespace Application.Common.Abstractions.Persistence;

public static class AndAlsoHelper
{
    public static Expression<Func<T,bool>> True<T>()  => _ => true;
    public static Expression<Func<T,bool>> And<T>(this Expression<Func<T,bool>> left,
        Expression<Func<T,bool>> right)
    {
        var p = left.Parameters[0];
        var body = Expression.AndAlso(left.Body, new Replace(right.Parameters[0], p).Visit(right.Body)!);
        return Expression.Lambda<Func<T,bool>>(body, p);
    }
    private sealed class Replace(ParameterExpression from, ParameterExpression to) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == from ? to : node;
    }
}