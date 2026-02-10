using Microsoft.EntityFrameworkCore;
using KickOffAPI.Models;
using KickOffAPI.Specifications;

public class BaseRepository<T>(DbContext context) : IBaseRepository<T> where T : class
{
    private readonly DbSet<T> _dbSet = context.Set<T>();
    private readonly DbContext _context = context;

    public async Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    public async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(id, cancellationToken);
    }

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
    public async Task<PaginatedResult<T>> GetPaginatedBySpecificationAsync(FilterSpecification<T> specification, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(specification);
        
        var totalCount = await query.CountAsync(cancellationToken);
        
        specification.ApplyPaging(pageNumber, pageSize);
        var query2 = ApplySpecification(specification);
        
        var data = await query2.ToListAsync(cancellationToken);
        
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
    private IQueryable<T> ApplySpecification(FilterSpecification<T> specification)
    {
        IQueryable<T> query = _dbSet;

        // Apply includes
        query = specification.Includes.Aggregate(query, (current, include) => current.Include(include));

        // Apply filters
        query = specification.Filters.Aggregate(query, (current, filter) => current.Where(filter));

        // Apply sorting
        if (specification.OrderBy.Count > 0)
        {
            var first = true;
            IOrderedQueryable<T> orderedQuery = null!;

            foreach (var (keySelector, sortOrder) in specification.OrderBy)
            {
                if (first)
                {
                    orderedQuery = sortOrder == 0 
                        ? query.OrderBy(keySelector) 
                        : query.OrderByDescending(keySelector);
                    first = false;
                }
                else
                {
                    orderedQuery = sortOrder == 0 
                        ? orderedQuery.ThenBy(keySelector) 
                        : orderedQuery.ThenByDescending(keySelector);
                }
            }
            
            query = orderedQuery;
        }

        // Apply paging
        if (specification.IsPagingEnabled)
        {
            if (specification.Skip.HasValue)
                query = query.Skip(specification.Skip.Value);
            
            if (specification.Take.HasValue)
                query = query.Take(specification.Take.Value);
        }

        // Apply tracking
        if (!specification.IsTrackingEnabled)
            query = query.AsNoTracking();

        return query;
    }

    public async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddRangeAsync(entities, cancellationToken);
    }

    public void Update(T entity) => _dbSet.Update(entity);

    public void Delete(T entity) => _dbSet.Remove(entity);

    public void DeleteRange(ICollection<T> entities) => _dbSet.RemoveRange(entities);
}