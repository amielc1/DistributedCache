using System;
using System.Threading.Tasks;
using Xunit;
using LRUCache.Interfaces;
using LRUCache.Services; 
using Moq;
using Microsoft.Extensions.Logging;

public class LRUCacheTests
{
    private ILRUCache<int, string> CreateCache(int capacity)
    {
        // Create a mock logger for testing
        var loggerMock = new Mock<ILogger<LRUCache<int, string>>>();
        return new LRUCache<int, string>(capacity, loggerMock.Object);
    }

    [Fact]
    public async Task PutAsync_ShouldStoreAndRetrieveValue()
    {
        // Arrange
        var cache = CreateCache(10);

        // Act
        await cache.PutAsync(1, "Value1");
        var result = await cache.GetAsync(1);

        // Assert
        Assert.Equal("Value1", result);
    }

    [Fact]
    public async Task GetAsync_ShouldThrowWhenKeyDoesNotExist()
    {
        // Arrange
        var cache = CreateCache(10);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => cache.GetAsync(99));
    }

    [Fact]
    public async Task ContainsKeyAsync_ShouldReturnTrueForExistingKey()
    {
        // Arrange
        var cache = CreateCache(10);
        await cache.PutAsync(1, "Value1");

        // Act
        var exists = await cache.ContainsKeyAsync(1);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ContainsKeyAsync_ShouldReturnFalseForNonExistingKey()
    {
        // Arrange
        var cache = CreateCache(10);

        // Act
        var exists = await cache.ContainsKeyAsync(1);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task RemoveAsync_ShouldRemoveKey()
    {
        // Arrange
        var cache = CreateCache(10);
        await cache.PutAsync(1, "Value1");

        // Act
        await cache.RemoveAsync(1);
        var exists = await cache.ContainsKeyAsync(1);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task GetCountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        var cache = CreateCache(10);
        await cache.PutAsync(1, "Value1");
        await cache.PutAsync(2, "Value2");

        // Act
        var count = await cache.GetCountAsync();

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task PutAsync_ShouldEvictLeastRecentlyUsedItem()
    {
        // Arrange
        var cache = CreateCache(2); // Capacity is 2
        await cache.PutAsync(1, "Value1");
        await cache.PutAsync(2, "Value2");

        // Act
        await cache.PutAsync(3, "Value3"); // Should evict key 1 (LRU)
        var containsKey1 = await cache.ContainsKeyAsync(1);
        var containsKey2 = await cache.ContainsKeyAsync(2);
        var containsKey3 = await cache.ContainsKeyAsync(3);

        // Assert
        Assert.False(containsKey1); // Key 1 was evicted
        Assert.True(containsKey2);
        Assert.True(containsKey3);
    }

    [Fact]
    public async Task PutAsync_ShouldUpdateExistingKey()
    {
        // Arrange
        var cache = CreateCache(10);
        await cache.PutAsync(1, "Value1");

        // Act
        await cache.PutAsync(1, "UpdatedValue1");
        var result = await cache.GetAsync(1);

        // Assert
        Assert.Equal("UpdatedValue1", result);
    }

    [Fact]
    public async Task LRUBehavior_ShouldKeepMostRecentlyUsedItem()
    {
        // Arrange
        var cache = CreateCache(2);
        await cache.PutAsync(1, "Value1");
        await cache.PutAsync(2, "Value2");

        // Act
        await cache.GetAsync(1); // Access key 1 to mark it as recently used
        await cache.PutAsync(3, "Value3"); // Should evict key 2 (now LRU)

        var containsKey1 = await cache.ContainsKeyAsync(1);
        var containsKey2 = await cache.ContainsKeyAsync(2);
        var containsKey3 = await cache.ContainsKeyAsync(3);

        // Assert
        Assert.True(containsKey1); // Key 1 is recently used, should remain
        Assert.False(containsKey2); // Key 2 was evicted
        Assert.True(containsKey3);
    }
}
