using StackExchange.Redis;
using LocalTesting.Shared.Constants;

namespace LocalTesting.WebApi.Services
{
    /// <summary>
    /// Non-blocking Redis connection service with circuit breaker pattern
    /// </summary>
    public class RedisConnectionService : IRedisConnectionService, IDisposable
    {
        private readonly ILogger<RedisConnectionService> _logger;
        private readonly IConfiguration _configuration;
        private readonly SemaphoreSlim _connectionSemaphore = new(1, 1);
        private IConnectionMultiplexer? _connection;
        private DateTime _lastConnectionAttempt = DateTime.MinValue;
        private readonly TimeSpan _connectionRetryInterval = TimeSpan.FromSeconds(30);
        private bool _disposed = false;

        public RedisConnectionService(ILogger<RedisConnectionService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public bool IsConnected => _connection?.IsConnected == true;

        public string ConnectionStatus
        {
            get
            {
                if (_connection == null)
                    return "Not initialized";
                if (_connection.IsConnected)
                    return "Connected";
                return "Disconnected";
            }
        }

        public async Task<IConnectionMultiplexer> GetConnectionAsync(CancellationToken cancellationToken = default)
        {
            // If already connected, return existing connection
            if (_connection?.IsConnected == true)
            {
                return _connection;
            }

            // Use semaphore to ensure only one connection attempt at a time
            await _connectionSemaphore.WaitAsync(cancellationToken);
            try
            {
                // Double-check pattern - connection might have been established while waiting
                if (_connection?.IsConnected == true)
                {
                    return _connection;
                }

                // Check if we should retry based on circuit breaker logic
                if (DateTime.UtcNow - _lastConnectionAttempt < _connectionRetryInterval)
                {
                    throw new InvalidOperationException(
                        $"Redis connection failed recently. Retry available in {(_connectionRetryInterval - (DateTime.UtcNow - _lastConnectionAttempt)).TotalSeconds:F0} seconds.");
                }

                _lastConnectionAttempt = DateTime.UtcNow;
                
                return await EstablishConnectionAsync(cancellationToken);
            }
            finally
            {
                _connectionSemaphore.Release();
            }
        }

        private async Task<IConnectionMultiplexer> EstablishConnectionAsync(CancellationToken cancellationToken)
        {
            var connectionString = _configuration.GetConnectionString("redis") ?? PortConstants.RedisConnectionString();
            _logger.LogInformation("Attempting to establish Redis connection: {ConnectionString}", connectionString);

            try
            {
                var configurationOptions = ConfigurationOptions.Parse(connectionString);
                configurationOptions.ConnectTimeout = 5000; // 5 second timeout
                configurationOptions.SyncTimeout = 5000;
                configurationOptions.AsyncTimeout = 5000;
                configurationOptions.ConnectRetry = 2; // Limited retries
                configurationOptions.AbortOnConnectFail = false; // Don't abort on connection failure

                // Use Task.Run to avoid blocking and apply timeout
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(10)); // 10 second overall timeout

                var connection = await Task.Run(async () =>
                {
                    return await ConnectionMultiplexer.ConnectAsync(configurationOptions);
                }, timeoutCts.Token);

                // Dispose old connection if exists
                _connection?.Dispose();
                _connection = connection;

                _logger.LogInformation("Redis connection established successfully");
                return connection;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Redis connection attempt was cancelled");
                throw;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Redis connection attempt timed out after 10 seconds");
                throw new TimeoutException("Redis connection timed out after 10 seconds");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to establish Redis connection. Application will continue with degraded functionality.");
                throw; // Let caller handle the exception
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _connection?.Dispose();
                _connectionSemaphore?.Dispose();
                _disposed = true;
            }
        }
    }
}