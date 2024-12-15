using LRUCache.Interfaces;
using LRUCache.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class LRUCacheMultithreadedTests
{
    private readonly ILogger<LRUCache<int, string>> _logger;

    public LRUCacheMultithreadedTests()
    {
        // Configure logging to Seq
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSeq("http://localhost:5341"); 
            loggingBuilder.SetMinimumLevel(LogLevel.Debug);
        });

        var serviceProvider = serviceCollection.BuildServiceProvider();
        _logger = serviceProvider.GetRequiredService<ILogger<LRUCache<int, string>>>();
    }

    private ILRUCache<int, string> CreateCache(int capacity)
    {
        return new LRUCache<int, string>(capacity, _logger);
    }

    [Fact]
    public async Task Multithreaded_PutAndGet_ShouldHandleConcurrentAccess()
    {
        // Arrange
        const int capacity = 100;
        const int totalThreads = 50;
        const int operationsPerThread = 100;
        var cache = CreateCache(capacity);

        _logger.LogInformation("Starting Multithreaded_PutAndGet test with {TotalThreads} threads", totalThreads);

        // Action: Perform concurrent writes and reads
        var tasks = Enumerable.Range(0, totalThreads).Select(threadId =>
            Task.Run(async () =>
            {
                for (int i = 0; i < operationsPerThread; i++)
                {
                    int key = threadId * 1000 + i; // Unique key per operation
                    string value = $"Value-{key}";

                    await cache.PutAsync(key, value);
                    var retrievedValue = await cache.GetAsync(key);

                    Assert.Equal(value, retrievedValue); // Validate that the value is consistent

                    _logger.LogDebug("Thread {ThreadId}: Successfully wrote and retrieved {Key}", threadId, key);
                }
            })
        );

        await Task.WhenAll(tasks);

        // Assert: Validate cache state
        var cacheCount = await cache.GetCountAsync();
        Assert.Equal(capacity, cacheCount); // Ensure capacity is not exceeded

        _logger.LogInformation("Completed Multithreaded_PutAndGet test successfully");
    }

    [Fact]
    public async Task Multithreaded_LRU_ShouldEvictCorrectly()
    {
        // Arrange
        const int capacity = 10;
        const int totalKeys = 100;
        var cache = CreateCache(capacity);

        _logger.LogInformation("Starting Multithreaded_LRU test with capacity {Capacity} and total keys {TotalKeys}", capacity, totalKeys);

        // Action: Perform concurrent writes
        var tasks = Enumerable.Range(0, totalKeys).Select(key =>
            Task.Run(async () =>
            {
                string value = $"Value-{key}";
                await cache.PutAsync(key, value);
                _logger.LogDebug("Added key {Key} to cache", key);
            })
        );

        await Task.WhenAll(tasks);

        // Assert: Validate that only the most recently used keys are retained
        for (int i = 0; i < totalKeys - capacity; i++)
        {
            bool exists = await cache.ContainsKeyAsync(i);
            Assert.False(exists); // Evicted keys should not exist
            _logger.LogDebug("Verified key {Key} is evicted as expected", i);
        }

        for (int i = totalKeys - capacity; i < totalKeys; i++)
        {
            bool exists = await cache.ContainsKeyAsync(i);
            Assert.True(exists); // Recent keys should exist
            _logger.LogDebug("Verified key {Key} is retained as expected", i);
        }

        _logger.LogInformation("Completed Multithreaded_LRU test successfully");
    }
}
