namespace JobShadowing.Application.Interfaces
{
    public interface ICacheService
    {
        // Get cached value or execute factory if not found
        Task<T?> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);

        // Get value from cache
        T? Get<T>(string key);

        // Set value in cache
        void Set<T>(string key, T value, TimeSpan? expiration = null);

        // Remove value from cache
        void Remove(string key);

        // Remove all values matching a pattern
        void RemoveByPrefix(string prefix);
    }
}
