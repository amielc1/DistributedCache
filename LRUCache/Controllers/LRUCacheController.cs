using LRUCache.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class LRUCacheController : ControllerBase
{
    private readonly ILRUCache<long, bool> _cache;
    private readonly ILogger<LRUCacheController> _logger;

    public LRUCacheController(ILRUCache<long, bool> cache, ILogger<LRUCacheController> logger)
    {
        _cache = cache;
        _logger = logger;

    }


    private async Task InitializeCacheAsync()
    {
        var primeNumbers = new List<long> { 31, 37, 41, 43, 47, 53, 59, 61, 67, 71 };
        var nonPrimeNumbers = new List<long> { 32, 38, 42, 44, 48, 54, 60, 62, 68, 72 };

        foreach (var number in primeNumbers)
        {
            await _cache.PutAsync(number, true);
        }

        foreach (var number in nonPrimeNumbers)
        {
            await _cache.PutAsync(number, false);
        }
    }

    [HttpPost(nameof(InitializeCache))]
    public async Task<IActionResult> InitializeCache()
    {
        _logger.LogInformation("Cache Initialization ");
        await InitializeCacheAsync();
        return Ok(0);
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> Get([FromRoute] long key)
    {
        try
        {
            var value = await _cache.GetAsync(key);
            _logger.LogInformation("Cache hit for {@Key} with value {@Value}", key, value);
            return Ok(value);
        }
        catch (KeyNotFoundException)
        {
            _logger.LogWarning("Cache miss for {@Key}", key);
            return NotFound(new { Message = $"Key '{key}' not found in cache." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while getting {@Key}", key);
            return StatusCode(500, new { Message = "An error occurred.", Details = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Put([FromBody] CacheEntry<long, bool> entry)
    {
        if (entry == null || entry.Key == null || entry.Value == null)
        {
            _logger.LogWarning("Invalid cache entry: {@Entry}", entry);
            return BadRequest(new { Message = "Key and value must be provided." });
        }

        try
        {
            await _cache.PutAsync(entry.Key, entry.Value);
            _logger.LogInformation("Added or updated cache entry {@Key} with value {@Value}", entry.Key, entry.Value);
            return Ok(new { Message = $"Key '{entry.Key}' added or updated in cache." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while putting {@Key} with value {@Value}", entry.Key, entry.Value);
            return StatusCode(500, new { Message = "An error occurred.", Details = ex.Message });
        }
    }

    [HttpGet("contains/{key}")]
    public async Task<IActionResult> ContainsKey([FromRoute] long key)
    {
        try
        {
            var exists = await _cache.ContainsKeyAsync(key);
            _logger.LogInformation("Checked existence of {@Key} - Exists: {@Exists}", key, exists);
            return Ok(new { Key = key, Exists = exists });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while checking {@Key}", key);
            return StatusCode(500, new { Message = "An error occurred.", Details = ex.Message });
        }
    }

    [HttpDelete("{key}")]
    public async Task<IActionResult> Remove([FromRoute] long key)
    {
        try
        {
            await _cache.RemoveAsync(key);
            _logger.LogInformation("Removed cache entry {@Key}", key);
            return Ok(new { Message = $"Key '{key}' removed from cache." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while removing {@Key}", key);
            return StatusCode(500, new { Message = "An error occurred.", Details = ex.Message });
        }
    }

    [HttpGet("count")]
    public async Task<IActionResult> GetCount()
    {
        try
        {
            var count = await _cache.GetCountAsync();
            _logger.LogInformation("Current cache count: {@Count}", count);
            return Ok(new { Count = count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving cache count.");
            return StatusCode(500, new { Message = "An error occurred.", Details = ex.Message });
        }
    }
}

public record CacheEntry<TKey, TValue>
{
    public TKey Key { get; init; }
    public TValue Value { get; init; }
}
