namespace DAL.Basic
{
    public interface IGenericRepository<T> where T : class
    {
        List<T> GetAll();
        Task<List<T>> GetAllAsync();
        void Create(T entity);
        Task<int> CreateAsync(T entity);
        void Update(T entity);
        Task<int> UpdateAsync(T entity);
        bool Remove(T entity);
        Task<bool> RemoveAsync(T entity);
        T GetById(int id);
        Task<T> GetByIdAsync(int id);
        T GetById(string code);
        Task<T> GetByIdAsync(string code);
        T GetById(Guid id);
        Task<T> GetByIdAsync(Guid id);
        void PrepareCreate(T entity);
        void PrepareUpdate(T entity);
        void PrepareRemove(T entity);
        int Save();
        Task<int> SaveAsync();
        IQueryable<T> Query();
        Task<IQueryable<T>> QueryAsync();
        void RemoveRange(IEnumerable<T> entities);
        Task<bool> RemoveRangeAsync(IEnumerable<T> entities);
    }
}
