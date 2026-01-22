using Microsoft.EntityFrameworkCore;

public class BaseRepository<T> : IBaseRepository<T> where T : class
{
    private readonly DbContext _context;
    private readonly DbSet<T> _dbSet;

    public BaseRepository(DbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public ICollection<T> GetAll()
    {
        try
        {
            return _dbSet.ToList();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error retrieving all entities: {ex.Message}");
        }
    }

    public T GetById(int id)
    {
        try
        {
            return _dbSet.Find(id)
                ?? throw new Exception("Entity not found.");
        }
        catch (Exception ex)
        {
            throw new Exception($"Error retrieving entity by ID: {ex.Message}");
        }
    }

    public void Add(T entity)
    {
        try
        {
            _dbSet.Add(entity);
            _context.SaveChanges();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error adding entity: {ex.Message}");
        }
    }

    public void AddRange(ICollection<T> entities)
    {
        try
        {
            _dbSet.AddRange(entities);
            _context.SaveChanges();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error adding entities: {ex.Message}");
        }
    }

    public void Update(T entity)
    {
        try
        {
            _dbSet.Update(entity);
            _context.SaveChanges();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error updating entity: {ex.Message}");
        }
    }

    public void Delete(T entity)
    {
        try
        {
            _dbSet.Remove(entity);
            _context.SaveChanges();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error deleting entity: {ex.Message}");
        }
    }

    public void DeleteRange(ICollection<T> entities)
    {
        try
        {
            _dbSet.RemoveRange(entities);
            _context.SaveChanges();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error deleting entities: {ex.Message}");
        }
    }
}