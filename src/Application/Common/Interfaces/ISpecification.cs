using System.Linq.Expressions;
using Application.Common.Abstractions.Persistence;

namespace Application.Common.Interfaces;

public interface ISpecification<T>
{
    Expression<Func<T, bool>> Criteria { get; }
    List<Expression<Func<T, object>>> Includes { get; }
    Expression<Func<T, object>>? OrderBy { get; }
    Expression<Func<T, object>>? OrderByDescending { get; }
    int? Take { get; }
    int? Skip { get; }
    bool IsPagingEnabled { get; }
}

public abstract class Specification<T>(Expression<Func<T, bool>> criteria) : ISpecification<T>
{
    protected Specification() : this(x => true) { }
    
    public Expression<Func<T, bool>> Criteria { get; private set; } = criteria;
    public List<Expression<Func<T, object>>> Includes { get; } = new();
    public Expression<Func<T, object>>? OrderBy { get; private set; }
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }
    public int? Take { get; private set; }
    public int? Skip { get; private set; }
    public bool IsPagingEnabled { get; private set; } = false;

    #region Helpers

    protected void AddInclude(Expression<Func<T, object>> includeExpression) => Includes.Add(includeExpression);
    
    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
    }
    
    protected void ApplyOrderBy(Expression<Func<T, object>> orderByExpression) => OrderBy = orderByExpression;
    protected void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescExpression) => OrderByDescending = orderByDescExpression;

    protected void ApplyCriteria(Expression<Func<T, bool>> criteria) => Criteria = criteria;
    
    #endregion

}