using KickOffAPI.Models;
using KickOffAPI.Specifications;

public interface IBaseRepository<T> where T : class
{
    Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get entities using a filter specification with filtering, sorting, pagination
    /// </summary>
    Task<List<T>> GetBySpecificationAsync(FilterSpecification<T> specification, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get paginated entities using a filter specification
    /// </summary>
    Task<PaginatedResult<T>> GetPaginatedBySpecificationAsync(FilterSpecification<T> specification, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    
    void Update(T entity);
    void Delete(T entity);
    void DeleteRange(ICollection<T> entities);
}