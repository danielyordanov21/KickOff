public interface IBaseRepository<T> where T : class
{
    ICollection<T> GetAll();
    T GetById(int id);
    void Add(T entity);
    void AddRange(ICollection<T> entities);
    void Update(T entity);
    void Delete(T entity);
    void DeleteRange(ICollection<T> entities);
}