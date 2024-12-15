using LRUCache.Interfaces;
using System.Collections.Concurrent;

namespace LRUCache.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

public class LRUCache<TKey, TValue> : ILRUCache<TKey, TValue>
{
    private readonly int _capacity;
    private readonly ConcurrentDictionary<TKey, LinkedListNode<CacheItem>> _cacheMap;
    private readonly LinkedList<CacheItem> _cacheList = new LinkedList<CacheItem>();
    private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
    private readonly ILogger<LRUCache<TKey, TValue>> _logger;

    public LRUCache(int capacity, ILogger<LRUCache<TKey, TValue>> logger)
    {
        if (capacity <= 0)
        {
            throw new ArgumentException("Capacity must be greater than 0.");
        }

        _capacity = capacity;
        _cacheMap = new ConcurrentDictionary<TKey, LinkedListNode<CacheItem>>(Environment.ProcessorCount, capacity);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TValue> GetAsync(TKey key)
    {
        try
        {
            _lock.EnterUpgradeableReadLock();

            if (_cacheMap.TryGetValue(key, out var node))
            {
                // Move the accessed item to the front of the list
                _lock.EnterWriteLock();
                try
                {
                    _cacheList.Remove(node);
                    _cacheList.AddFirst(node);
                }
                finally
                {
                    _lock.ExitWriteLock();
                }

                _logger.LogInformation("Cache hit for key {@Key}", key);
                return await Task.FromResult(node.Value.Value);
            }

            _logger.LogWarning("Cache miss for key {@Key}", key);
            throw new KeyNotFoundException("The key is not present in the cache.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting key {@Key}", key);
            throw;
        }
        finally
        {
            _lock.ExitUpgradeableReadLock();
        }
    }

    public async Task PutAsync(TKey key, TValue value)
    {
        try
        {
            _lock.EnterWriteLock();
            try
            {
                if (_cacheMap.TryGetValue(key, out var existingNode))
                {
                    existingNode.Value.Value = value;
                    _cacheList.Remove(existingNode);
                    _cacheList.AddFirst(existingNode);
                    _logger.LogInformation("Updated key {@Key} in cache with value {@Value}", key, value);
                }
                else
                {
                    if (_cacheList.Count >= _capacity)
                    {
                        var lruItem = _cacheList.Last;
                        if (lruItem != null)
                        {
                            _cacheMap.TryRemove(lruItem.Value.Key, out _);
                            _cacheList.RemoveLast();
                            _logger.LogInformation("Evicted least recently used key {@EvictedKey}", lruItem.Value.Key);
                        }
                    }

                    var newItem = new CacheItem(key, value);
                    var newNode = new LinkedListNode<CacheItem>(newItem);
                    _cacheList.AddFirst(newNode);
                    _cacheMap[key] = newNode;
                    _logger.LogInformation("Added key {@Key} to cache with value {@Value}", key, value);
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while putting key {@Key} with value {@Value}", key, value);
            throw;
        }
    }

    public async Task<bool> ContainsKeyAsync(TKey key)
    {
        try
        {
            _lock.EnterReadLock();
            try
            {
                var exists = _cacheMap.ContainsKey(key);
                _logger.LogInformation("Checked existence of key {@Key} - Found: {@Exists}", key, exists);
                return await Task.FromResult(exists);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while checking key {@Key}", key);
            throw;
        }
    }

    public async Task RemoveAsync(TKey key)
    {
        try
        {
            _lock.EnterWriteLock();
            try
            {
                if (_cacheMap.TryRemove(key, out var node))
                {
                    _cacheList.Remove(node);
                    _logger.LogInformation("Removed key {@Key} from cache", key);
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while removing key {@Key}", key);
            throw;
        }
    }

    public async Task<int> GetCountAsync()
    {
        try
        {
            _lock.EnterReadLock();
            try
            {
                var count = _cacheList.Count;
                _logger.LogInformation("Current cache count: {@Count}", count);
                return await Task.FromResult(count);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting cache count.");
            throw;
        }
    }

    private class CacheItem
    {
        public TKey Key { get; }
        public TValue Value { get; set; }

        public CacheItem(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
    }
}

