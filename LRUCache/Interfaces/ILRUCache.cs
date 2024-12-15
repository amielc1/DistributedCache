namespace LRUCache.Interfaces;

public interface ILRUCache<TKey, TValue>
{
    Task<TValue> GetAsync(TKey key);
    Task PutAsync(TKey key, TValue value);
    Task<bool> ContainsKeyAsync(TKey key);
    Task RemoveAsync(TKey key);
    Task<int> GetCountAsync();
}