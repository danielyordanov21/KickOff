using Microsoft.EntityFrameworkCore;
using KickOffAPI.Models;
using KickOffAPI.Specifications;

public class BaseRepository<T, TKey>(DbContext context) : IBaseRepository<T, TKey>
    where T : class
{
    private readonly DbSet<T> _dbSet = context.Set<T>();
    private readonly DbContext _context = context;

    public async Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _dbSet.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<T?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
        => await _dbSet.FindAsync([id], cancellationToken);

    /// <summary>
    /// Apply specification filters, sorting, and includes to a query
    /// </summary>
    public async Task<List<T>> GetBySpecificationAsync(FilterSpecification<T> specification, CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(specification);
        return await query.ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get paginated results using specification
    /// </summary>
    public async Task<PaginatedResult<T>> GetPaginatedBySpecificationAsync(
        FilterSpecification<T> specification,
        int pageNumber, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var baseQuery = ApplySpecification(specification, ignorePaging: true);
        var totalCount = await baseQuery.CountAsync(cancellationToken);

        specification.ApplyPaging(pageNumber, pageSize);

        var data = await ApplySpecification(specification).ToListAsync(cancellationToken);

        return new PaginatedResult<T>
        {
            Data = data,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// Apply filters, includes, sorting, and paging from specification to query
    /// </summary>
    private IQueryable<T> ApplySpecification(FilterSpecification<T> specification, bool ignorePaging = false)
    {
        IQueryable<T> query = _dbSet;

        query = specification.Includes.Aggregate(query, (current, include) => current.Include(include));
        query = specification.Filters.Aggregate(query, (current, filter) => current.Where(filter));

        if (specification.OrderBy.Count > 0)
        {
            IOrderedQueryable<T>? orderedQuery = null;

            foreach (var (keySelector, sortOrder) in specification.OrderBy)
            {
                orderedQuery = orderedQuery == null
                    ? (sortOrder == 0 ? query.OrderBy(keySelector) : query.OrderByDescending(keySelector))
                    : (sortOrder == 0 ? orderedQuery.ThenBy(keySelector) : orderedQuery.ThenByDescending(keySelector));
            }

            query = orderedQuery!;
        }

        if (!ignorePaging && specification.IsPagingEnabled)
        {
            if (specification.Skip.HasValue)
                query = query.Skip(specification.Skip.Value);

            if (specification.Take.HasValue)
                query = query.Take(specification.Take.Value);
        }

        if (!specification.IsTrackingEnabled)
            query = query.AsNoTracking();

        return query;
    }

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
        => await _dbSet.AddAsync(entity, cancellationToken);

    public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        => await _dbSet.AddRangeAsync(entities, cancellationToken);

    public void Update(T entity) 
        => _dbSet.Update(entity);

    public void Delete(T entity) 
        => _dbSet.Remove(entity);

    public void DeleteRange(ICollection<T> entities) 
        => _dbSet.RemoveRange(entities);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
