using Microsoft.Extensions.Logging;
using Flink.JobBuilder.Backpressure;

namespace FlinkJobBuilder.Sample
{
    /// <summary>
    /// Examples demonstrating proper async patterns for Flink JobManager compatibility.
    /// 
    /// IMPORTANT: When code is submitted to Flink's JobManager for execution, the execution context
    /// changes from a local .NET environment to a distributed/serialized environment. This can cause
    /// issues with certain async patterns that work fine in unit tests but fail in production.
    /// </summary>
    public class FlinkJobManagerCompatibilityExamples
    {
        private readonly ILogger<FlinkJobManagerCompatibilityExamples> _logger;

        public FlinkJobManagerCompatibilityExamples(ILogger<FlinkJobManagerCompatibilityExamples> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// PROBLEMATIC PATTERN: This async pattern works in local tests but may fail when
        /// submitted to Flink JobManager because:
        /// 1. Async state machines don't serialize properly across job boundaries
        /// 2. Task scheduling context differs in distributed execution
        /// 3. SynchronizationContext may not be available in Flink execution environment
        /// </summary>
        public async Task ProblematicAsyncPattern_DoNotUse(IRateLimitingStrategy rateLimiter, string message)
        {
            _logger.LogWarning("⚠️  PROBLEMATIC PATTERN - Do not use this in Flink jobs!");
            
            // This pattern will work in unit tests but may fail in Flink JobManager execution
            if (await rateLimiter.TryAcquireAsync())
            {
                // Process your message
                await ProcessMessage(message);
                _logger.LogInformation($"Processed message: {message}");
            }
            else
            {
                _logger.LogWarning($"Rate limited - message dropped: {message}");
            }
        }

        /// <summary>
        /// RECOMMENDED PATTERN FOR FLINK JOBMANAGER: Use synchronous patterns that work
        /// reliably in distributed execution environments.
        /// 
        /// This approach:
        /// 1. Uses synchronous calls that don't rely on async state machines
        /// 2. Works consistently in both local and distributed execution
        /// 3. Maintains performance through efficient non-blocking implementations
        /// </summary>
        public void FlinkJobManagerCompatiblePattern_Recommended(IRateLimitingStrategy rateLimiter, string message)
        {
            _logger.LogInformation("✅ RECOMMENDED PATTERN - Safe for Flink JobManager execution");
            
            // Use synchronous TryAcquire instead of async for Flink compatibility
            if (rateLimiter.TryAcquire())
            {
                // Process synchronously or use Task.Run for CPU-bound work if needed
                ProcessMessageSync(message);
                _logger.LogInformation($"Processed message: {message}");
            }
            else
            {
                _logger.LogWarning($"Rate limited - message dropped: {message}");
            }
        }

        /// <summary>
        /// ALTERNATIVE PATTERN: If you must use async operations, wrap them properly
        /// for Flink JobManager compatibility using ConfigureAwait(false) and proper
        /// exception handling that doesn't rely on SynchronizationContext.
        /// </summary>
        public async Task AlternativeAsyncPattern_WithCaution(IRateLimitingStrategy rateLimiter, string message)
        {
            _logger.LogInformation("⚠️  ALTERNATIVE PATTERN - Use with caution in Flink jobs");
            
            try
            {
                // Use synchronous rate limiting check
                if (rateLimiter.TryAcquire())
                {
                    // If async processing is required, use ConfigureAwait(false)
                    // to avoid SynchronizationContext dependencies
                    await ProcessMessage(message).ConfigureAwait(false);
                    _logger.LogInformation($"Processed message: {message}");
                }
                else
                {
                    _logger.LogWarning($"Rate limited - message dropped: {message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing message: {message}");
                // Don't rethrow - handle gracefully for Flink job stability
            }
        }

        /// <summary>
        /// BULK PROCESSING PATTERN: For high-throughput scenarios, batch operations
        /// to reduce rate limiting overhead in distributed execution.
        /// </summary>
        public void BulkProcessingPattern_ForHighThroughput(IRateLimitingStrategy rateLimiter, string[] messages)
        {
            _logger.LogInformation("🚀 BULK PROCESSING PATTERN - Optimized for high throughput");
            
            // Calculate bulk permit requirement
            var requiredPermits = messages.Length;
            
            if (rateLimiter.TryAcquire(requiredPermits))
            {
                // Process all messages in the batch
                foreach (var message in messages)
                {
                    ProcessMessageSync(message);
                }
                _logger.LogInformation($"Bulk processed {messages.Length} messages");
            }
            else
            {
                _logger.LogWarning($"Rate limited - bulk batch of {messages.Length} messages dropped");
            }
        }

        /// <summary>
        /// Synchronous message processing method compatible with Flink JobManager execution
        /// </summary>
        private void ProcessMessageSync(string message)
        {
            // Simulate message processing work
            // In real scenarios, this would contain your business logic
            Thread.Sleep(10); // Simulate processing time
        }

        /// <summary>
        /// Async message processing method - use with caution in Flink jobs
        /// </summary>
        private async Task ProcessMessage(string message)
        {
            // Simulate async message processing work
            await Task.Delay(10).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Extension methods to provide Flink JobManager-compatible rate limiting patterns
    /// </summary>
    public static class FlinkJobManagerRateLimiterExtensions
    {
        /// <summary>
        /// Synchronous TryAcquire method compatible with Flink JobManager execution.
        /// Use this instead of TryAcquireAsync when submitting jobs to Flink.
        /// </summary>
        public static bool TryAcquire(this IRateLimitingStrategy rateLimiter, int permits = 1)
        {
            // Use the async method but get the result synchronously
            // This avoids async state machine issues in Flink execution
            var task = rateLimiter.TryAcquireAsync(permits);
            
            if (task.IsCompleted)
            {
                return task.Result;
            }
            
            // For cases where immediate result is not available,
            // return false to maintain non-blocking behavior in Flink jobs
            return false;
        }

        /// <summary>
        /// Bulk acquire method for high-throughput scenarios in Flink jobs
        /// </summary>
        public static bool TryAcquireBulk(this IRateLimitingStrategy rateLimiter, int permits)
        {
            // Try to acquire all permits at once for better performance
            return rateLimiter.TryAcquire(permits);
        }
    }
}