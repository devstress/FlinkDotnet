using Confluent.Kafka;

namespace Flink.JobBuilder.Backpressure;

/// <summary>
/// Factory interface for creating Kafka producers and consumers.
/// This abstraction allows for dependency injection and testing with mocks.
/// </summary>
public interface IKafkaClientFactory
{
    /// <summary>
    /// Creates a Kafka producer with the specified configuration.
    /// </summary>
    /// <typeparam name="TKey">Key type</typeparam>
    /// <typeparam name="TValue">Value type</typeparam>
    /// <param name="config">Producer configuration</param>
    /// <returns>Kafka producer instance</returns>
    IProducer<TKey, TValue> CreateProducer<TKey, TValue>(ProducerConfig config);

    /// <summary>
    /// Creates a Kafka consumer with the specified configuration.
    /// </summary>
    /// <typeparam name="TKey">Key type</typeparam>
    /// <typeparam name="TValue">Value type</typeparam>
    /// <param name="config">Consumer configuration</param>
    /// <returns>Kafka consumer instance</returns>
    IConsumer<TKey, TValue> CreateConsumer<TKey, TValue>(ConsumerConfig config);
}

/// <summary>
/// Default implementation of IKafkaClientFactory that creates real Kafka clients.
/// </summary>
public class DefaultKafkaClientFactory : IKafkaClientFactory
{
    /// <inheritdoc />
    public IProducer<TKey, TValue> CreateProducer<TKey, TValue>(ProducerConfig config)
    {
        return new ProducerBuilder<TKey, TValue>(config).Build();
    }

    /// <inheritdoc />
    public IConsumer<TKey, TValue> CreateConsumer<TKey, TValue>(ConsumerConfig config)
    {
        return new ConsumerBuilder<TKey, TValue>(config).Build();
    }
}
