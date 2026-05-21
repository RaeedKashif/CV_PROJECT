using System.Linq.Expressions;

namespace ChequePrintingSystem.Application.Abstractions;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> ListPagedAsync(
        Expression<Func<T, bool>>? predicate,
        Func<IQueryable<T>, IOrderedQueryable<T>> orderBy,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Delete(T entity);
}
