# Day 1: Building a Data Pipeline with Kafka and Apache Flink

> **Based on Baeldung Tutorial**: [https://www.baeldung.com/kafka-flink-data-pipeline](https://www.baeldung.com/kafka-flink-data-pipeline)
>
> This tutorial follows the **exact structure** of the Baeldung guide, adapted for **.NET 9**, **FlinkDotNet**, and **.NET Aspire**.

## 1. Overview

Apache Flink is a stream processing framework that can be used easily with .NET. Apache Kafka is a distributed stream processing system supporting high fault-tolerance.

In this tutorial, we're going to have a look at how to build a data pipeline using those two technologies.

**Key Adaptations for .NET:**
- Using **Confluent.Kafka** (.NET library) instead of Java Kafka client
- Using **FlinkDotNet** with IR-based job definitions instead of Java Flink API
- Using **.NET Aspire** for infrastructure orchestration instead of manual setup

## 2. Installation

To install and configure Apache Kafka and Flink, we use .NET Aspire which automates the setup.

### Starting Infrastructure

```bash
cd LocalTesting
dotnet run --project LocalTesting.FlinkSqlAppHost/LocalTesting.FlinkSqlAppHost.csproj
```

This starts:
- Apache Kafka broker (port 29092)
- Apache Flink cluster (JobManager + TaskManager)
- Flink Job Gateway (port 8080)

Wait approximately 45 seconds for all containers to be ready.

### Creating Kafka Topics

The demo automatically creates topics `flink_input` and `flink_output`:

```csharp
var topicsToCreate = new[]
{
    new TopicSpecification { 
        Name = "flink_input", 
        NumPartitions = 4, 
        ReplicationFactor = 1 
    },
    new TopicSpecification { 
        Name = "flink_output", 
        NumPartitions = 4, 
        ReplicationFactor = 1 
    }
};

using var admin = new AdminClientBuilder(adminConfig).Build();
await admin.CreateTopicsAsync(topicsToCreate);
```

For the sake of this tutorial, we'll use default configuration and default ports for Apache Kafka.

## 3. Flink Usage

Apache Flink allows a real-time stream processing technology. The framework allows using multiple third-party systems as stream sources or sinks.

In Flink – there are various connectors available:
- Apache Kafka (source/sink)
- Apache Cassandra (sink)
- Amazon Kinesis Streams (source/sink)
- Elasticsearch (sink)
- Hadoop FileSystem (sink)
- RabbitMQ (source/sink)
- Apache NiFi (source/sink)
- Twitter Streaming API (source)

To add Flink to our .NET project, we use FlinkDotNet packages:

```xml
<PackageReference Include="Confluent.Kafka" Version="2.3.0" />
<PackageReference Include="Serilog" Version="3.1.1" />
<PackageReference Include="Serilog.Sinks.Console" Version="5.0.1" />
```

Adding those dependencies will allow us to consume and produce to and from Kafka topics using .NET.

## 4. Kafka String Consumer

To consume data from Kafka with .NET, we need to provide a topic and a Kafka address. We should also provide a group id which will be used to hold offsets so we won't always read the whole data from the beginning.

Let's create a method that will make the creation of Kafka consumer easier:

```csharp
public static ConsumerConfig CreateConsumerConfig(
    string kafkaAddress, 
    string kafkaGroup)
{
    return new ConsumerConfig
    {
        BootstrapServers = kafkaAddress,
        GroupId = kafkaGroup,
        AutoOffsetReset = AutoOffsetReset.Earliest,
        EnableAutoCommit = false,
        BrokerAddressFamily = BrokerAddressFamily.V4,
        SecurityProtocol = SecurityProtocol.Plaintext
    };
}
```

This method takes kafkaAddress and kafkaGroup and creates the ConsumerConfig that will be used to consume data from given topic as a String.

## 5. Kafka String Producer

To produce data to Kafka, we need to provide Kafka address and topic that we want to use. Again, we can create a method that will help us to create producers:

```csharp
public static ProducerConfig CreateProducerConfig(string kafkaAddress)
{
    return new ProducerConfig
    {
        BootstrapServers = kafkaAddress,
        EnableIdempotence = true,
        Acks = Acks.All,
        LingerMs = 5,
        BrokerAddressFamily = BrokerAddressFamily.V4,
        SecurityProtocol = SecurityProtocol.Plaintext
    };
}
```

This method takes only kafkaAddress as an argument since there's no need to provide group id when we are producing to Kafka topic.

## 6. String Stream Processing

When we have a fully working consumer and producer, we can try to process data from Kafka and then save our results back to Kafka.

In this example, we're going to capitalize words in each Kafka entry and then write it back to Kafka using a Flink transformation.

Our Flink job definition (C# equivalent of Baeldung's Java Flink application):

```csharp
var flinkJobDefinition = new
{
    jobName = "string-capitalize-pipeline",
    sources = new[]
    {
        new {
            type = "kafka",
            name = "input-source",
            properties = new {
                topic = "flink_input",
                bootstrapServers = "localhost:29092",
                groupId = "baeldung",
                autoOffsetReset = "earliest"
            }
        }
    },
    transforms = new[]
    {
        new {
            type = "map",
            name = "capitalize-transform",
            function = "upper",  // Uppercase transformation
            from = "input-source"
        }
    },
    sinks = new[]
    {
        new {
            type = "kafka",
            name = "output-sink",
            properties = new {
                topic = "flink_output",
                bootstrapServers = "localhost:29092"
            },
            from = "capitalize-transform"
        }
    }
};
```

The application will read data from the `flink_input` topic, perform operations on the stream (uppercase transformation) and then save the results to the `flink_output` topic in Kafka.

### Complete Processing Pipeline

```csharp
public static async Task Capitalize()
{
    string inputTopic = "flink_input";
    string outputTopic = "flink_output";
    string consumerGroup = "baeldung";
    string address = "localhost:29092";

    // Submit Flink job
    var jobJson = JsonSerializer.Serialize(flinkJobDefinition);
    using var httpClient = new HttpClient();
    var content = new StringContent(jobJson, Encoding.UTF8, "application/json");
    await httpClient.PostAsync("http://localhost:8080/api/v1/jobs/submit", content);

    // Produce messages
    var producerConfig = CreateProducerConfig(address);
    using var producer = new ProducerBuilder<string, string>(producerConfig).Build();
    
    for (int i = 0; i < 100; i++)
    {
        await producer.ProduceAsync(inputTopic, 
            new Message<string, string> { 
                Key = $"key-{i}", 
                Value = $"message {i}" 
            });
    }

    // Consume processed messages
    var consumerConfig = CreateConsumerConfig(address, consumerGroup);
    using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
    consumer.Subscribe(outputTopic);

    var result = consumer.Consume(TimeSpan.FromSeconds(30));
    Console.WriteLine($"Received: {result.Message.Value}");  // Will be uppercase
}
```

We've seen how to deal with Strings using Flink and Kafka. But often it's required to perform operations on custom objects. We'll see how to do this in the next chapters.

## 7. Custom Object Deserialization

The following class represents a simple message with information about sender and recipient:

```csharp
public class InputMessage
{
    public string Sender { get; set; }
    public string Recipient { get; set; }
    public DateTime SentAt { get; set; }
    public string Message { get; set; }
}
```

Previously, we were using simple string deserialization, but now we want to deserialize data directly to custom objects.

To do this, we need a custom deserialization approach using System.Text.Json:

```csharp
public class InputMessageDeserializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static InputMessage Deserialize(byte[] bytes)
    {
        return JsonSerializer.Deserialize<InputMessage>(bytes, Options);
    }

    public static InputMessage Deserialize(string json)
    {
        return JsonSerializer.Deserialize<InputMessage>(json, Options);
    }
}
```

We are assuming here that the messages are held as JSON in Kafka.

## 8. Custom Object Serialization

Now, let's assume that we want our system to have a possibility of creating a backup of messages. We want the process to be automatic, and each backup should be composed of messages sent during one whole day.

Also, a backup message should have a unique id assigned.

For this purpose, we can create the following class:

```csharp
public class Backup
{
    public List<InputMessage> InputMessages { get; set; }
    public DateTime BackupTimestamp { get; set; }
    public Guid Uuid { get; set; }

    public Backup(List<InputMessage> inputMessages, DateTime backupTimestamp)
    {
        InputMessages = inputMessages;
        BackupTimestamp = backupTimestamp;
        Uuid = Guid.NewGuid();
    }
}
```

Please mind that the UUID generation mechanism isn't perfect, as it allows duplicates. However, this is enough for the scope of this example.

We want to save our Backup object as JSON to Kafka:

```csharp
public class BackupSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static byte[] Serialize(Backup backup)
    {
        var json = JsonSerializer.Serialize(backup, Options);
        return Encoding.UTF8.GetBytes(json);
    }
}
```

## 9. Timestamping Messages

Since we want to create a backup for all messages of each day, messages need a timestamp.

Flink provides three different time characteristics: EventTime, ProcessingTime, and IngestionTime.

In our case, we need to use the time at which the message has been sent, so we'll use EventTime.

In FlinkDotNet, we handle timestamps through the IR (Intermediate Representation) by configuring time characteristics:

```csharp
var flinkJobDefinition = new
{
    jobName = "backup-aggregator",
    timeCharacteristic = "EventTime",  // Use event time
    sources = new[]
    {
        new {
            type = "kafka",
            name = "input-source",
            properties = new {
                topic = "flink_input",
                bootstrapServers = "localhost:29092",
                groupId = "baeldung"
            },
            timestampField = "sentAt"  // Extract timestamps from this field
        }
    }
};
```

We need to ensure our InputMessage has the proper timestamp field that Flink can use for time-based operations.

## 10. Creating Time Windows

To assure that our backup gathers only messages sent during one day, we can use time windows in Flink.

In FlinkDotNet, we define windowing through the job configuration:

```csharp
var flinkJobDefinition = new
{
    jobName = "backup-aggregator",
    timeCharacteristic = "EventTime",
    sources = new[]
    {
        new {
            type = "kafka",
            name = "input-source",
            properties = new {
                topic = "flink_input",
                bootstrapServers = "localhost:29092",
                groupId = "baeldung"
            },
            timestampField = "sentAt"
        }
    },
    transforms = new[]
    {
        new {
            type = "timeWindow",
            name = "daily-window",
            from = "input-source",
            window = new {
                type = "tumbling",
                size = "24 hours"
            }
        },
        new {
            type = "aggregate",
            name = "backup-aggregator",
            from = "daily-window",
            aggregateFunction = "collectToBackup"
        }
    },
    sinks = new[]
    {
        new {
            type = "kafka",
            name = "backup-sink",
            properties = new {
                topic = "flink_output",
                bootstrapServers = "localhost:29092"
            },
            from = "backup-aggregator"
        }
    }
};
```

## 11. Aggregating Backups

After configuring proper timestamps and implementing our aggregation logic, we can process our Kafka input:

```csharp
public static async Task CreateBackup()
{
    string inputTopic = "flink_input";
    string outputTopic = "flink_output";
    string consumerGroup = "baeldung";
    string kafkaAddress = "localhost:29092";

    // Submit the aggregation job
    var jobJson = JsonSerializer.Serialize(flinkJobDefinition);
    using var httpClient = new HttpClient();
    var content = new StringContent(jobJson, Encoding.UTF8, "application/json");
    var response = await httpClient.PostAsync(
        "http://localhost:8080/api/v1/jobs/submit", 
        content);

    if (response.IsSuccessStatusCode)
    {
        Console.WriteLine("Backup aggregation job submitted successfully");
    }

    // Produce test messages with timestamps
    var producerConfig = CreateProducerConfig(kafkaAddress);
    using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

    for (int i = 0; i < 100; i++)
    {
        var inputMessage = new InputMessage
        {
            Sender = $"sender-{i}",
            Recipient = $"recipient-{i}",
            SentAt = DateTime.UtcNow,
            Message = $"Test message {i}"
        };

        var json = JsonSerializer.Serialize(inputMessage);
        await producer.ProduceAsync(inputTopic,
            new Message<string, string> { Key = $"key-{i}", Value = json });
    }

    // Consume backup results
    var consumerConfig = CreateConsumerConfig(kafkaAddress, consumerGroup);
    using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
    consumer.Subscribe(outputTopic);

    var result = consumer.Consume(TimeSpan.FromSeconds(30));
    if (result != null)
    {
        var backup = JsonSerializer.Deserialize<Backup>(result.Message.Value);
        Console.WriteLine($"Received backup with {backup.InputMessages.Count} messages");
        Console.WriteLine($"Backup ID: {backup.Uuid}");
        Console.WriteLine($"Backup timestamp: {backup.BackupTimestamp}");
    }
}
```

## 12. Conclusion

In this article, we've presented how to create a simple data pipeline with Apache Flink and Apache Kafka using .NET.

### What We Learned

Following the Baeldung tutorial structure, we covered:

1. ✅ **Installation** - Setting up Kafka and Flink using .NET Aspire
2. ✅ **String Processing** - Creating producers, consumers, and simple transformations
3. ✅ **Custom Objects** - Serializing and deserializing custom C# objects
4. ✅ **Time Windows** - Using event time and time-based aggregations
5. ✅ **Backup System** - Building a daily backup aggregation pipeline

### Key Differences from Baeldung Tutorial

| Baeldung (Java) | This Tutorial (.NET) |
|-----------------|----------------------|
| Java Kafka Client | Confluent.Kafka (.NET) |
| Java Flink API | FlinkDotNet IR-based jobs |
| Maven dependencies | NuGet packages |
| Manual Kafka/Flink setup | .NET Aspire orchestration |
| Java serialization | System.Text.Json |

### Running the Complete Demo

Navigate to the exercise directory and run:

```bash
cd LearningCourse/Day01-Kafka-Flink-Data-Pipeline/Exercise-Solutions/PipelineDemo
dotnet run -- demo
```

This executes all steps from the tutorial:
1. Submits Flink job to cluster
2. Produces messages to input topic
3. Processes data through Flink
4. Consumes results from output topic

### Additional Resources

- 📚 **Original Tutorial**: [Baeldung - Kafka and Apache Flink Data Pipeline](https://www.baeldung.com/kafka-flink-data-pipeline)
- 📖 **Apache Flink**: [https://flink.apache.org/](https://flink.apache.org/)
- 🔧 **Confluent Kafka .NET**: [https://docs.confluent.io/kafka-clients/dotnet/current/overview.html](https://docs.confluent.io/kafka-clients/dotnet/current/overview.html)
- 💻 **FlinkDotNet**: Repository documentation in `docs/` folder
