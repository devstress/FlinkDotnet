using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Flink.JobBuilder.Flink
{
    /// <summary>
    /// Apache Flink-style Redis sink for atomic operations and exactly-once semantics
    /// Supports stress testing with atomic counters and set operations
    /// </summary>
    public class FlinkRedisSink : IDisposable
    {
        private readonly ILogger<FlinkRedisSink> _logger;
        private readonly object _lockObject = new();
        private bool _isDisposed;
        private readonly string _connectionString;
        private readonly Dictionary<string, object>? _redisConfig;
        private ConnectionMultiplexer? _muxer;
        private IDatabase? _db;

        // String constant to avoid S1192 warning
        private const string RedisNotInitializedError = "Redis not initialized. Call InitializeAsync().";

        public FlinkRedisSink(string connectionString, Dictionary<string, object>? redisConfig, ILogger<FlinkRedisSink> logger)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connectionString = connectionString;
            _redisConfig = redisConfig;

            _logger.LogInformation("FlinkRedisSink initialized with connection: {ConnectionString}, config options: {ConfigCount}",
                MaskConnectionString(connectionString), redisConfig?.Count ?? 0);
        }

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Initializing FlinkRedisSink with Flink-optimal settings");
            var options = ConfigurationOptions.Parse(_connectionString);
            SetDefaultOptions(options);
            ApplyCustomConfiguration(options);

            _muxer = await ConnectionMultiplexer.ConnectAsync(options).ConfigureAwait(false);
            _db = _muxer.GetDatabase();
            _logger.LogInformation("FlinkRedisSink initialization completed");
        }

        private static void SetDefaultOptions(ConfigurationOptions options)
        {
            options.AbortOnConnectFail = false;
            options.ConnectTimeout = 5000;
            options.SyncTimeout = 5000;
            options.ReconnectRetryPolicy = new ExponentialRetry(5000);
        }

        private void ApplyCustomConfiguration(ConfigurationOptions options)
        {
            if (_redisConfig == null)
                return;

            foreach (var config in _redisConfig)
            {
                ApplyConfigurationOption(options, config.Key, config.Value);
            }
        }

        private static void ApplyConfigurationOption(ConfigurationOptions options, string key, object value)
        {
            switch (key.ToLowerInvariant())
            {
                case "connecttimeout":
                    if (value is int timeout)
                        options.ConnectTimeout = timeout;
                    break;
                case "synctimeout":
                    if (value is int syncTimeout)
                        options.SyncTimeout = syncTimeout;
                    break;
                case "abortonconnectfail":
                    if (value is bool abortOnFail)
                        options.AbortOnConnectFail = abortOnFail;
                    break;
            }
        }

        public async Task<long> AtomicIncrementAsync(string key, long increment = 1, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            lock (_lockObject)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(nameof(FlinkRedisSink));
            }

            _logger.LogDebug("Performing atomic increment: key={Key}, increment={Increment}", key, increment);

            try
            {
                if (_db == null)
                    throw new InvalidOperationException(RedisNotInitializedError);
                var newValue = await _db.StringIncrementAsync(key, increment).ConfigureAwait(false);
                _logger.LogDebug("Atomic increment completed: key={Key}, newValue={NewValue}", key, newValue);
                return newValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to perform atomic increment for key: {Key}", key);
                throw new InvalidOperationException($"Redis atomic increment failed for key '{key}'", ex);
            }
        }

        public async Task<bool> AtomicSetAddAsync(string setKey, string member, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(setKey))
                throw new ArgumentException("Set key cannot be null or empty", nameof(setKey));
            if (string.IsNullOrEmpty(member))
                throw new ArgumentException("Member cannot be null or empty", nameof(member));

            lock (_lockObject)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(nameof(FlinkRedisSink));
            }

            _logger.LogDebug("Performing atomic set add: setKey={SetKey}, member={Member}", setKey, member);

            try
            {
                if (_db == null)
                    throw new InvalidOperationException(RedisNotInitializedError);
                var added = await _db.SetAddAsync(setKey, member).ConfigureAwait(false);
                _logger.LogDebug("Atomic set add completed: setKey={SetKey}, member={Member}, added={Added}", setKey, member, added);
                return added;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to perform atomic set add for setKey: {SetKey}, member: {Member}", setKey, member);
                throw new InvalidOperationException($"Redis atomic set add failed for setKey '{setKey}' and member '{member}'", ex);
            }
        }

        public async Task<bool> SetContainsAsync(string setKey, string member, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(setKey))
                throw new ArgumentException("Set key cannot be null or empty", nameof(setKey));
            if (string.IsNullOrEmpty(member))
                throw new ArgumentException("Member cannot be null or empty", nameof(member));

            lock (_lockObject)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(nameof(FlinkRedisSink));
            }

            try
            {
                if (_db == null)
                    throw new InvalidOperationException(RedisNotInitializedError);
                var exists = await _db.SetContainsAsync(setKey, member).ConfigureAwait(false);
                _logger.LogDebug("Set contains: setKey={SetKey}, member={Member}, exists={Exists}", setKey, member, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check set membership for setKey: {SetKey}, member: {Member}", setKey, member);
                throw new InvalidOperationException($"Redis set membership check failed for setKey '{setKey}' and member '{member}'", ex);
            }
        }

        public async Task<long> GetCounterValueAsync(string key, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("Key cannot be null or empty", nameof(key));

            lock (_lockObject)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(nameof(FlinkRedisSink));
            }

            try
            {
                if (_db == null)
                    throw new InvalidOperationException(RedisNotInitializedError);
                var value = await _db.StringGetAsync(key).ConfigureAwait(false);
                long result = 0;
                if (value.HasValue && long.TryParse(value.ToString(), out var parsed))
                    result = parsed;
                _logger.LogDebug("Get counter value: key={Key}, value={Value}", key, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get counter value for key: {Key}", key);
                throw new InvalidOperationException($"Redis get counter value failed for key '{key}'", ex);
            }
        }

        public async Task<long> GetSetSizeAsync(string setKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(setKey))
                throw new ArgumentException("Set key cannot be null or empty", nameof(setKey));

            lock (_lockObject)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(nameof(FlinkRedisSink));
            }

            try
            {
                if (_db == null)
                    throw new InvalidOperationException(RedisNotInitializedError);
                var size = await _db.SetLengthAsync(setKey).ConfigureAwait(false);
                _logger.LogDebug("Get set size: setKey={SetKey}, size={Size}", setKey, size);
                return size;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get set size for setKey: {SetKey}", setKey);
                throw new InvalidOperationException($"Redis get set size failed for setKey '{setKey}'", ex);
            }
        }

        public async Task<RedisTransactionResult> ExecuteTransactionAsync(IEnumerable<RedisOperation> operations, CancellationToken cancellationToken = default)
        {
            if (operations == null)
                throw new ArgumentNullException(nameof(operations));

            lock (_lockObject)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(nameof(FlinkRedisSink));
            }

            var operationList = new List<RedisOperation>(operations);
            _logger.LogDebug("Executing Redis transaction with {Count} operations", operationList.Count);

            try
            {
                if (_db == null)
                    throw new InvalidOperationException(RedisNotInitializedError);
                
                var tran = _db.CreateTransaction();
                var (pending, results) = AddOperationsToTransaction(tran, operationList);

                var committed = await tran.ExecuteAsync().ConfigureAwait(false);
                if (!committed)
                {
                    return new RedisTransactionResult { Success = false, ErrorMessage = "Transaction aborted" };
                }

                await Task.WhenAll(pending).ConfigureAwait(false);
                var materialized = await MaterializeResultsAsync(results);

                _logger.LogDebug("Redis transaction completed successfully with {Count} results", materialized.Count);
                return new RedisTransactionResult { Success = true, Results = materialized };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute Redis transaction");
                throw new InvalidOperationException("Redis transaction execution failed", ex);
            }
        }

        private static (List<Task> pending, List<object> results) AddOperationsToTransaction(
            ITransaction tran, List<RedisOperation> operations)
        {
            var pending = new List<Task>();
            var results = new List<object>();

            foreach (var op in operations)
            {
                switch (op.Type)
                {
                    case RedisOperationType.Increment:
                        var incTask = tran.StringIncrementAsync(op.Key!, op.Increment);
                        pending.Add(incTask);
                        results.Add(incTask);
                        break;
                    case RedisOperationType.SetAdd:
                        var saddTask = tran.SetAddAsync(op.Key!, op.Member!);
                        pending.Add(saddTask);
                        results.Add(saddTask);
                        break;
                    case RedisOperationType.Get:
                        var getTask = tran.StringGetAsync(op.Key!);
                        pending.Add(getTask);
                        results.Add(getTask);
                        break;
                    case RedisOperationType.Set:
                        var setTask = tran.StringSetAsync(op.Key!, op.Value?.ToString());
                        pending.Add(setTask);
                        results.Add(setTask);
                        break;
                    case RedisOperationType.Delete:
                        var delTask = tran.KeyDeleteAsync(op.Key!);
                        pending.Add(delTask);
                        results.Add(delTask);
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported operation type: {op.Type}");
                }
            }

            return (pending, results);
        }

        private static async Task<List<object>> MaterializeResultsAsync(List<object> results)
        {
            var materialized = new List<object>(results.Count);
            foreach (var r in results)
            {
                switch (r)
                {
                    case Task<long> tL:
                        materialized.Add(await tL.ConfigureAwait(false));
                        break;
                    case Task<bool> tB:
                        materialized.Add(await tB.ConfigureAwait(false));
                        break;
                    case Task<RedisValue> tV:
                        var v = await tV.ConfigureAwait(false);
                        materialized.Add(v.HasValue ? v.ToString()! : string.Empty);
                        break;
                    default:
                        materialized.Add(true);
                        break;
                }
            }
            return materialized;
        }

        private static string MaskConnectionString(string connectionString)
        {
            // Use "pwd" to avoid S2068 false positive for password detection
            const string passwordKey = "password=";
            const string maskedValue = "password=***";
            
            if (connectionString.Contains(passwordKey, StringComparison.OrdinalIgnoreCase))
            {
                var passwordIndex = connectionString.IndexOf(passwordKey, StringComparison.OrdinalIgnoreCase);
                return connectionString.Substring(0, passwordIndex) + maskedValue;
            }
            return connectionString;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed && disposing)
            {
                lock (_lockObject)
                {
                    _logger.LogInformation("Disposing FlinkRedisSink");
                    try
                    {
                        _muxer?.Close();
                    }
                    catch
                    {
                        // Close operation may fail if connection is already lost - this is non-fatal during disposal
                    }
                    try
                    {
                        _muxer?.Dispose();
                    }
                    catch
                    {
                        // Dispose operation may fail if resources are already released - this is non-fatal during disposal
                    }
                    _db = null;
                    _isDisposed = true;
                }
            }
        }
    }

    public enum RedisOperationType
    {
        Increment,
        SetAdd,
        Get,
        Set,
        Delete
    }

    public class RedisOperation
    {
        public RedisOperationType Type
        {
            get; set;
        }
        public string? Key
        {
            get; set;
        }
        public string? Member
        {
            get; set;
        }
        public object? Value
        {
            get; set;
        }
        public long Increment { get; set; } = 1;
    }

    public class RedisTransactionResult
    {
        public bool Success
        {
            get; set;
        }
        public List<object> Results { get; set; } = new();
        public string? ErrorMessage
        {
            get; set;
        }
    }
}
