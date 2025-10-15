namespace Exercise103;

/// <summary>
/// Least Recently Used (LRU) Cache implementation
/// Thread-safe cache with automatic eviction of least recently used items
/// </summary>
public class LRUCache<TKey, TValue> where TKey : notnull
{
    private readonly int _capacity;
    private readonly Dictionary<TKey, LinkedListNode<CacheItem>> _cache;
    private readonly LinkedList<CacheItem> _lruList;
    private readonly object _lock = new();
    private long _hits;
    private long _misses;

    private class CacheItem
    {
        public TKey Key { get; set; }
        public TValue Value { get; set; }
        public DateTime LastAccessTime { get; set; }

        public CacheItem(TKey key, TValue value)
        {
            Key = key;
            Value = value;
            LastAccessTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Create a new LRU cache with specified capacity
    /// </summary>
    public LRUCache(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentException("Capacity must be positive", nameof(capacity));

        _capacity = capacity;
        _cache = new Dictionary<TKey, LinkedListNode<CacheItem>>(capacity);
        _lruList = new LinkedList<CacheItem>();
    }

    /// <summary>
    /// Get a value from the cache
    /// </summary>
    public bool TryGet(TKey key, out TValue? value)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var node))
            {
                // Move to front (most recently used)
                _lruList.Remove(node);
                _lruList.AddFirst(node);
                node.Value.LastAccessTime = DateTime.UtcNow;

                value = node.Value.Value;
                Interlocked.Increment(ref _hits);
                return true;
            }

            value = default;
            Interlocked.Increment(ref _misses);
            return false;
        }
    }

    /// <summary>
    /// Add or update a value in the cache
    /// </summary>
    public void Set(TKey key, TValue value)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var existingNode))
            {
                // Update existing item and move to front
                existingNode.Value.Value = value;
                existingNode.Value.LastAccessTime = DateTime.UtcNow;
                _lruList.Remove(existingNode);
                _lruList.AddFirst(existingNode);
            }
            else
            {
                // Evict least recently used if at capacity
                if (_cache.Count >= _capacity)
                {
                    var lruNode = _lruList.Last;
                    if (lruNode != null)
                    {
                        _cache.Remove(lruNode.Value.Key);
                        _lruList.RemoveLast();
                    }
                }

                // Add new item
                var newItem = new CacheItem(key, value);
                var newNode = _lruList.AddFirst(newItem);
                _cache[key] = newNode;
            }
        }
    }

    /// <summary>
    /// Get or add a value to the cache using a factory function
    /// </summary>
    public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
    {
        if (TryGet(key, out var value) && value != null)
        {
            return value;
        }

        var newValue = valueFactory(key);
        Set(key, newValue);
        return newValue;
    }

    /// <summary>
    /// Remove a specific key from the cache
    /// </summary>
    public bool Remove(TKey key)
    {
        lock (_lock)
        {
            if (_cache.TryGetValue(key, out var node))
            {
                _cache.Remove(key);
                _lruList.Remove(node);
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Clear all items from the cache
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _cache.Clear();
            _lruList.Clear();
        }
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    public (long Hits, long Misses, int Count, double HitRatio) GetStatistics()
    {
        var hits = Interlocked.Read(ref _hits);
        var misses = Interlocked.Read(ref _misses);
        var total = hits + misses;
        var hitRatio = total > 0 ? (double)hits / total * 100 : 0;

        int count;
        lock (_lock)
        {
            count = _cache.Count;
        }

        return (hits, misses, count, hitRatio);
    }

    /// <summary>
    /// Get the current size of the cache
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _cache.Count;
            }
        }
    }

    /// <summary>
    /// Get the maximum capacity of the cache
    /// </summary>
    public int Capacity => _capacity;
}