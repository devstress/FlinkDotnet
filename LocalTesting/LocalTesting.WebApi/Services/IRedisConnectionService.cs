using StackExchange.Redis;

namespace LocalTesting.WebApi.Services
{
    /// <summary>
    /// Service for managing Redis connections with non-blocking initialization
    /// </summary>
    public interface IRedisConnectionService
    {
        /// <summary>
        /// Gets the Redis connection asynchronously with retry logic and timeout
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>Redis connection multiplexer</returns>
        Task<IConnectionMultiplexer> GetConnectionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if Redis connection is available
        /// </summary>
        /// <returns>True if Redis is connected, false otherwise</returns>
        bool IsConnected { get; }

        /// <summary>
        /// Gets the current connection status
        /// </summary>
        string ConnectionStatus { get; }
    }
}