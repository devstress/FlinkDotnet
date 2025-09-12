using System.Collections.Concurrent;
using System.Diagnostics;

namespace BackPressure.Common;

public class PerformanceMetrics
{
    private readonly ConcurrentQueue<double> _messageTimes = new();
    private readonly ConcurrentQueue<long> _timestamps = new();
    private readonly ConcurrentQueue<long> _outTimestamps = new();
    private long _messagesIn = 0;
    private long _messagesOut = 0;
    private long _throttleEvents = 0;
    private double _maxMessageLatency = 0;
    private double _minMessageLatency = double.MaxValue;
    private readonly Stopwatch _totalStopwatch = new();
    private readonly object _lockObject = new();
    private readonly string _throttleEventName;
    
    public PerformanceMetrics(string throttleEventName = "Back Pressure")
    {
        _throttleEventName = throttleEventName;
    }
    
    public long MessagesIn => _messagesIn;
    public long MessagesOut => _messagesOut;
    public long ThrottleEvents => _throttleEvents;
    
    public long BackPressureEvents => _throttleEvents;
    public long RateLimitEvents => _throttleEvents;
    
    public double MaxMessageLatency => _maxMessageLatency;
    public double MinMessageLatency => _minMessageLatency == double.MaxValue ? 0 : _minMessageLatency;
    public TimeSpan TotalElapsed => _totalStopwatch.Elapsed;
    
    public void Start()
    {
        _totalStopwatch.Start();
    }
    
    public void Stop()
    {
        _totalStopwatch.Stop();
    }
    
    public void IncrementIn()
    {
        Interlocked.Increment(ref _messagesIn);
        _timestamps.Enqueue(Stopwatch.GetTimestamp());
        
        while (_timestamps.Count > 10000)
        {
            _timestamps.TryDequeue(out _);
        }
    }
    
    public void IncrementThrottle()
    {
        Interlocked.Increment(ref _throttleEvents);
    }
    
    public void IncrementBackPressure() => IncrementThrottle();
    public void IncrementRateLimit() => IncrementThrottle();
    
    public void IncrementOut(double latencyMs)
    {
        Interlocked.Increment(ref _messagesOut);

        while (_messageTimes.Count > 10000)
        {
            _messageTimes.TryDequeue(out _);
        }

        _messageTimes.Enqueue(latencyMs);
        var now = Stopwatch.GetTimestamp();
        _outTimestamps.Enqueue(now);
        while (_outTimestamps.Count > 10000)
        {
            _outTimestamps.TryDequeue(out _);
        }
        
        lock (_lockObject)
        {
            if (latencyMs > _maxMessageLatency)
            {
                _maxMessageLatency = latencyMs;
            }
            
            if (latencyMs < _minMessageLatency)
            {
                _minMessageLatency = latencyMs;
            }
        }
    }
    
    public double GetAverageLatency()
    {
        var times = _messageTimes.ToArray();
        return times.Length > 0 ? times.Average() : 0;
    }
    
    public double GetMedianLatency()
    {
        var times = _messageTimes.ToArray();
        if (times.Length == 0) return 0;
        
        Array.Sort(times);
        int mid = times.Length / 2;
        return times.Length % 2 == 0 ? (times[mid - 1] + times[mid]) / 2 : times[mid];
    }
    
    public double GetPercentileLatency(double percentile)
    {
        var times = _messageTimes.ToArray();
        if (times.Length == 0) return 0;
        
        Array.Sort(times);
        int index = Math.Min((int)Math.Ceiling(times.Length * percentile / 100) - 1, times.Length - 1);
        return times[index];
    }
    
    public double GetThroughputPerSecond()
    {
        var totalSeconds = _totalStopwatch.Elapsed.TotalSeconds;
        return totalSeconds > 0 ? _messagesOut / totalSeconds : 0;
    }
    
    public double GetCurrentThroughputPerSecond(int windowSeconds = 10)
    {
        var currentTime = Stopwatch.GetTimestamp();
        var windowStart = currentTime - (windowSeconds * Stopwatch.Frequency);

        var recentTimestamps = _timestamps.ToArray()
            .Where(ts => ts >= windowStart)
            .Count();

        return recentTimestamps / (double)windowSeconds;
    }
    public double GetCurrentOutThroughputPerSecond(int windowSeconds = 10)
    {
        var currentTime = Stopwatch.GetTimestamp();
        var windowStart = currentTime - (windowSeconds * Stopwatch.Frequency);

        var recentTimestamps = _outTimestamps.ToArray()
            .Where(ts => ts >= windowStart)
            .Count();

        return recentTimestamps / (double)windowSeconds;
    }
    
    public double GetThrottleRate()
    {
        var totalOperations = _messagesIn + _throttleEvents;
        return totalOperations > 0 ? (_throttleEvents / (double)totalOperations) * 100 : 0;
    }
    
    public double GetBackPressureRate() => GetThrottleRate();
    public double GetRateLimitRate() => GetThrottleRate();

	public void PrintSummary(string testMode = "")
	{
		if (string.IsNullOrEmpty(testMode))
		{
			testMode = "Back Pressure";
		}

		var throughput = GetThroughputPerSecond();
		var messagesOut = _messagesOut;

		Console.WriteLine($"{testMode} Back Pressure test completed: {messagesOut:N0} messages processed at {throughput:F2} msgs/sec");
	}

	public void Reset()
	{
		Interlocked.Exchange(ref _messagesIn, 0);
		Interlocked.Exchange(ref _messagesOut, 0);
		Interlocked.Exchange(ref _throttleEvents, 0);
		
		lock (_lockObject)
		{
			_maxMessageLatency = 0.0;
			_minMessageLatency = double.MaxValue;
		}
		
        while (_messageTimes.TryDequeue(out _)) { }
        while (_timestamps.TryDequeue(out _)) { }
        while (_outTimestamps.TryDequeue(out _)) { }
        
        _totalStopwatch.Reset();
    }


}
