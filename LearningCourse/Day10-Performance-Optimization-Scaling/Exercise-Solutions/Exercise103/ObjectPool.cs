using System.Collections.Concurrent;

namespace Exercise103;

/// <summary>
/// Generic object pool for reducing GC pressure through object reuse
/// Thread-safe implementation using ConcurrentBag
/// </summary>
public class ObjectPool<T> where T : class, new()
{
    private readonly ConcurrentBag<T> _objects = new();
    private readonly Func<T> _objectFactory;
    private readonly Action<T>? _resetAction;
    private readonly int _maxPoolSize;
    private int _currentSize;
    private long _hits;
    private long _misses;

    /// <summary>
    /// Create a new object pool
    /// </summary>
    /// <param name="objectFactory">Factory function to create new objects</param>
    /// <param name="resetAction">Optional action to reset objects before returning to pool</param>
    /// <param name="maxPoolSize">Maximum number of objects to keep in pool</param>
    public ObjectPool(Func<T> objectFactory, Action<T>? resetAction = null, int maxPoolSize = 100)
    {
        _objectFactory = objectFactory ?? throw new ArgumentNullException(nameof(objectFactory));
        _resetAction = resetAction;
        _maxPoolSize = maxPoolSize;
    }

    /// <summary>
    /// Acquire an object from the pool, creating a new one if necessary
    /// </summary>
    public T Acquire()
    {
        if (_objects.TryTake(out var obj))
        {
            Interlocked.Increment(ref _hits);
            Interlocked.Decrement(ref _currentSize);
            return obj;
        }

        Interlocked.Increment(ref _misses);
        return _objectFactory();
    }

    /// <summary>
    /// Return an object to the pool for reuse
    /// </summary>
    public void Release(T obj)
    {
        if (obj == null)
            return;

        // Reset object state if reset action provided
        _resetAction?.Invoke(obj);

        // Only add back to pool if we haven't exceeded max size
        if (_currentSize < _maxPoolSize)
        {
            _objects.Add(obj);
            Interlocked.Increment(ref _currentSize);
        }
    }

    /// <summary>
    /// Get pool statistics
    /// </summary>
    public (long Hits, long Misses, int CurrentSize, double HitRatio) GetStatistics()
    {
        var hits = Interlocked.Read(ref _hits);
        var misses = Interlocked.Read(ref _misses);
        var total = hits + misses;
        var hitRatio = total > 0 ? (double)hits / total * 100 : 0;

        return (hits, misses, _currentSize, hitRatio);
    }

    /// <summary>
    /// Clear all objects from the pool
    /// </summary>
    public void Clear()
    {
        while (_objects.TryTake(out _))
        {
            Interlocked.Decrement(ref _currentSize);
        }
    }

    /// <summary>
    /// Pre-populate the pool with objects
    /// </summary>
    public void Prewarm(int count)
    {
        count = Math.Min(count, _maxPoolSize);
        for (int i = 0; i < count; i++)
        {
            var obj = _objectFactory();
            Release(obj);
        }
    }
}

/// <summary>
/// Pooled object wrapper for automatic return to pool
/// Usage: using var pooled = pool.AcquireScoped();
/// </summary>
public class PooledObject<T> : IDisposable where T : class, new()
{
    private readonly ObjectPool<T> _pool;
    private T? _object;

    public PooledObject(ObjectPool<T> pool, T obj)
    {
        _pool = pool ?? throw new ArgumentNullException(nameof(pool));
        _object = obj ?? throw new ArgumentNullException(nameof(obj));
    }

    public T Object => _object ?? throw new ObjectDisposedException(nameof(PooledObject<T>));

    public void Dispose()
    {
        if (_object != null)
        {
            _pool.Release(_object);
            _object = null;
        }
    }
}

/// <summary>
/// Extension methods for ObjectPool
/// </summary>
public static class ObjectPoolExtensions
{
    /// <summary>
    /// Acquire an object with automatic return to pool on dispose
    /// </summary>
    public static PooledObject<T> AcquireScoped<T>(this ObjectPool<T> pool) where T : class, new()
    {
        var obj = pool.Acquire();
        return new PooledObject<T>(pool, obj);
    }
}