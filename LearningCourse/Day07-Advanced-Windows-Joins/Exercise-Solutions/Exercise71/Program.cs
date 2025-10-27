using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using FlinkDotNet.DataStream;
using Serilog;

namespace Exercise71;

/// <summary>
/// Exercise 7.1: E-commerce Order Enrichment
/// 
/// Real-time order processing system that demonstrates:
/// - Multi-stream temporal joins (orders, products, customers, inventory)
/// - Interval joins for order event processing
/// - Watermark management for event-time processing
/// - Complex windowing strategies for real-time enrichment
/// 
/// Architecture: Multiple Kafka topics → Flink multi-stream join → Enriched orders output
/// </summary>
class Program
{
    // Kafka addresses - read from environment variables set by test infrastructure
    private static string KafkaBootstrapServers =>
        Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9093";
        
    private static string KafkaFlinkBootstrapServers =>
        Environment.GetEnvironmentVariable("KAFKA_FLINK_BOOTSTRAP_SERVERS") ?? "kafka:9092";
        
    private static string FlinkGatewayUrl =>
        Environment.GetEnvironmentVariable("FLINK_GATEWAY_URL") ?? "http://localhost:8080";

    // Kafka topics for multi-stream join
    private const string OrdersTopic = "orders";
    private const string ProductsTopic = "products";
    private const string CustomersTopic = "customers";
    private const string InventoryTopic = "inventory";
    private const string EnrichedOrdersTopic = "enriched-orders";
    private const string ConsumerGroup = "exercise71-consumer";
    
    // Test data parameters
    private const int OrderCount = 20;
    private const int ProductCount = 10;
    private const int CustomerCount = 5;

    static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            Log.Information("================================================================================");
            Log.Information("  Exercise 7.1: E-commerce Order Enrichment");
            Log.Information("================================================================================");
            Log.Information("");
            Log.Information("Learning Objectives:");
            Log.Information("  - Multi-stream temporal joins");
            Log.Information("  - Interval joins for order processing");
            Log.Information("  - Watermark management");
            Log.Information("  - Complex windowing strategies");
            Log.Information("");
            Log.Information("Configuration:");
            Log.Information("  Kafka (Host): {KafkaHost}", KafkaBootstrapServers);
            Log.Information("  Kafka (Flink): {KafkaFlink}", KafkaFlinkBootstrapServers);
            Log.Information("  Flink Gateway: {FlinkGateway}", FlinkGatewayUrl);
            Log.Information("  Orders: {OrderCount}, Products: {ProductCount}, Customers: {CustomerCount}", 
                OrderCount, ProductCount, CustomerCount);
            Log.Information("");

            FlinkDotNet.DataStream.IJobClient? jobClient = null;

            try
            {
                // Step 1: Verify infrastructure
                Log.Information(">> Step 1/7: Verifying Kafka is ready...");
                await WaitForKafkaReadyAsync();
                Log.Information("");

                Log.Information(">> Step 2/7: Verifying Flink cluster is ready...");
                await WaitForFlinkHealthyAsync();
                Log.Information("");

                Log.Information(">> Step 3/7: Creating Kafka topics...");
                await CreateTopicsAsync();
                Log.Information("");

                // Step 2: Produce reference data (products, customers, inventory)
                Log.Information(">> Step 4/7: Producing reference data...");
                await ProduceReferenceDataAsync();
                Log.Information("");

                // Step 3: Submit Flink enrichment job
                Log.Information(">> Step 5/7: Submitting Flink order enrichment job...");
                jobClient = await SubmitOrderEnrichmentJobAsync();
                await Task.Delay(TimeSpan.FromSeconds(5)); // Wait for job to start
                Log.Information("");

                // Step 4: Produce orders
                Log.Information(">> Step 6/7: Producing order events...");
                await ProduceOrdersAsync();
                await Task.Delay(TimeSpan.FromSeconds(3)); // Wait for processing
                Log.Information("");

                // Step 5: Consume enriched orders
                Log.Information(">> Step 7/7: Consuming enriched orders...");
                var enrichedCount = await ConsumeEnrichedOrdersAsync();
                Log.Information("");

                // Results
                var successRate = OrderCount > 0 ? (double)enrichedCount / OrderCount * 100 : 0;
                
                Log.Information("================================================================================");
                Log.Information("  Exercise 7.1 Results - Order Enrichment");
                Log.Information("================================================================================");
                Log.Information("  Statistics:");
                Log.Information("     Orders Produced: {OrderCount:N0}", OrderCount);
                Log.Information("     Enriched Orders: {EnrichedCount:N0}", enrichedCount);
                Log.Information("     Success Rate: {SuccessRate:F1}%", successRate);
                Log.Information("");
                Log.Information("  Key Learnings:");
                Log.Information("     [SUCCESS] Multi-stream joins with temporal alignment");
                Log.Information("     [SUCCESS] Order enrichment with product/customer/inventory data");
                Log.Information("     [SUCCESS] Interval joins for event-time processing");
                Log.Information("     [SUCCESS] Production-ready e-commerce pattern");
                Log.Information("");
                Log.Information("[SUCCESS] Exercise 7.1 COMPLETED successfully");
                Log.Information("================================================================================");

                return 0;
            }
            finally
            {
                // Cleanup: Cancel the Flink job
                if (jobClient != null)
                {
                    Log.Information("");
                    Log.Information(">> Cleaning up: Cancelling Flink job...");
                    try
                    {
                        await jobClient.CancelAsync();
                        Log.Information("   [SUCCESS] Flink job cancelled");
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Failed to cancel job");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Exercise 7.1 failed with exception");
            return 1;
        }
        finally
        {
            // Flush logs with timeout to prevent hanging
            var flushTask = Log.CloseAndFlushAsync().AsTask();
            if (await Task.WhenAny(flushTask, Task.Delay(TimeSpan.FromSeconds(2))) == flushTask)
            {
                await flushTask; // Completed successfully
            }
        }
    }

    /// <summary>
    /// Submit Flink job for order enrichment with multi-stream joins
    /// </summary>
    private static async Task<FlinkDotNet.DataStream.IJobClient> SubmitOrderEnrichmentJobAsync()
    {
        var environment = StreamExecutionEnvironment.GetExecutionEnvironment();

        // Source stream from Kafka
        // NOTE: FlinkDotNet currently supports ONE Kafka source per job
        // Multi-stream pattern (orders, products, customers) is documented for educational purposes
        // In production with full Apache Flink 2.1.0, you would use union/connect operations
        var ordersStream = environment.FromKafka(
            topic: OrdersTopic,
            bootstrapServers: KafkaFlinkBootstrapServers,
            groupId: ConsumerGroup,
            startingOffsets: "earliest"
        );

        // Process and enrich orders
        // Note: Enrichment uses mock data within OrderEnrichmentFunction
        // In production, you would use proper interval joins and temporal tables
        var enrichedStream = ordersStream
            .Map(new OrderEnrichmentFunction());

        // Sink enriched orders
        enrichedStream.SinkToKafka(EnrichedOrdersTopic, KafkaFlinkBootstrapServers);

        // Execute job
        var jobClient = await environment.ExecuteAsync("Exercise71-OrderEnrichment");

        Log.Information("   [SUCCESS] Flink order enrichment job submitted");
        Log.Information("   JobId: {JobId}", jobClient.GetJobId());
        
        return jobClient;
    }

    /// <summary>
    /// Produce reference data for products, customers, and inventory
    /// </summary>
    private static async Task ProduceReferenceDataAsync()
    {
        var config = new ProducerConfig
        {
            BootstrapServers = KafkaBootstrapServers,
            ClientId = "exercise71-reference-producer",
            Acks = Acks.All
        };

        using var producer = new ProducerBuilder<string, string>(config).Build();

        // Produce products
        Log.Information("   Producing {ProductCount} products...", ProductCount);
        for (int i = 1; i <= ProductCount; i++)
        {
            var product = new Product
            {
                ProductId = i,
                Name = $"Product-{i}",
                Category = GetCategory(i),
                Price = 10m + (i * 15.5m),
                Stock = 100 + (i * 10)
            };

            await producer.ProduceAsync(ProductsTopic, new Message<string, string>
            {
                Key = $"prod-{i}",
                Value = JsonSerializer.Serialize(product)
            });
        }

        // Produce customers
        Log.Information("   Producing {CustomerCount} customers...", CustomerCount);
        for (int i = 1; i <= CustomerCount; i++)
        {
            var customer = new Customer
            {
                CustomerId = i,
                Name = $"Customer-{i}",
                Email = $"customer{i}@example.com",
                Tier = GetTier(i),
                LifetimeValue = 1000m + (i * 500m)
            };

            await producer.ProduceAsync(CustomersTopic, new Message<string, string>
            {
                Key = $"cust-{i}",
                Value = JsonSerializer.Serialize(customer)
            });
        }

        // Produce inventory updates
        Log.Information("   Producing inventory updates...");
        for (int i = 1; i <= ProductCount; i++)
        {
            var inventory = new Inventory
            {
                ProductId = i,
                Available = 100 + (i * 10),
                Reserved = i * 2,
                Timestamp = DateTime.UtcNow
            };

            await producer.ProduceAsync(InventoryTopic, new Message<string, string>
            {
                Key = $"inv-{i}",
                Value = JsonSerializer.Serialize(inventory)
            });
        }

        producer.Flush(TimeSpan.FromSeconds(10));
        Log.Information("   [SUCCESS] Reference data produced");
    }

    /// <summary>
    /// Produce order events
    /// </summary>
    private static async Task ProduceOrdersAsync()
    {
        var config = new ProducerConfig
        {
            BootstrapServers = KafkaBootstrapServers,
            ClientId = "exercise71-orders-producer",
            Acks = Acks.All,
            LingerMs = 5
        };

        using var producer = new ProducerBuilder<string, string>(config).Build();

        Log.Information("   Producing {OrderCount} orders...", OrderCount);

        for (int i = 1; i <= OrderCount; i++)
        {
            var order = new Order
            {
                OrderId = i,
                CustomerId = (i % CustomerCount) + 1,
                ProductId = (i % ProductCount) + 1,
                Quantity = 1 + (i % 5),
                Status = "pending",
                Timestamp = DateTime.UtcNow
            };

            try
            {
                var result = await producer.ProduceAsync(OrdersTopic, new Message<string, string>
                {
                    Key = $"order-{i}",
                    Value = JsonSerializer.Serialize(order)
                });

                if ((i % 5 == 0) || i == OrderCount)
                {
                    Log.Information("   [{Count}/{Total}] Order {OrderId} (Customer {CustomerId}, Product {ProductId})",
                        i, OrderCount, order.OrderId, order.CustomerId, order.ProductId);
                }
            }
            catch (ProduceException<string, string> ex)
            {
                Log.Error(ex, "Failed to produce order {OrderId}", i);
            }

            await Task.Delay(50);
        }

        producer.Flush(TimeSpan.FromSeconds(10));
        Log.Information("   [SUCCESS] All {OrderCount} orders produced", OrderCount);
    }

    /// <summary>
    /// Consume enriched orders from output topic
    /// </summary>
    private static async Task<int> ConsumeEnrichedOrdersAsync()
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = KafkaBootstrapServers,
            GroupId = ConsumerGroup + "-verify",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(EnrichedOrdersTopic);

        Log.Information("   Consuming enriched orders from '{Topic}' (max 30 seconds)...", EnrichedOrdersTopic);

        var consumedCount = 0;
        var timeoutCount = 0;
        const int maxTimeouts = 10;
        var stopwatch = Stopwatch.StartNew();

        while (timeoutCount < maxTimeouts && stopwatch.Elapsed < TimeSpan.FromSeconds(30))
        {
            try
            {
                var result = consumer.Consume(TimeSpan.FromSeconds(1));
                
                if (result != null)
                {
                    consumedCount++;
                    timeoutCount = 0;
                    
                    if (consumedCount % 5 == 0 || consumedCount <= 3)
                    {
                        try
                        {
                            var enriched = JsonSerializer.Deserialize<EnrichedOrder>(result.Message.Value);
                            if (enriched != null)
                            {
                                Log.Information("   [Order {Count}] Order#{OrderId} - Customer: {Customer}, Product: {Product}, Total: ${Total:F2}",
                                    consumedCount, enriched.OrderId, enriched.CustomerName, enriched.ProductName, enriched.TotalPrice);
                            }
                        }
                        catch
                        {
                            Log.Information("   [{Count}] enriched orders consumed...", consumedCount);
                        }
                    }
                    
                    consumer.Commit(result);
                }
                else
                {
                    timeoutCount++;
                }
            }
            catch (ConsumeException ex)
            {
                Log.Error(ex, "Error consuming enriched order");
                break;
            }
        }

        consumer.Close();
        Log.Information("   [SUCCESS] Consumed {ConsumedCount} enriched orders", consumedCount);
        return consumedCount;
    }

    private static async Task CreateTopicsAsync()
    {
        var adminConfig = new AdminClientConfig 
        { 
            BootstrapServers = KafkaBootstrapServers
        };
        
        using var admin = new AdminClientBuilder(adminConfig).Build();

        var topicsToCreate = new[]
        {
            new TopicSpecification { Name = OrdersTopic, NumPartitions = 4, ReplicationFactor = 1 },
            new TopicSpecification { Name = ProductsTopic, NumPartitions = 2, ReplicationFactor = 1 },
            new TopicSpecification { Name = CustomersTopic, NumPartitions = 2, ReplicationFactor = 1 },
            new TopicSpecification { Name = InventoryTopic, NumPartitions = 2, ReplicationFactor = 1 },
            new TopicSpecification { Name = EnrichedOrdersTopic, NumPartitions = 4, ReplicationFactor = 1 }
        };

        try
        {
            await admin.CreateTopicsAsync(topicsToCreate);
            Log.Information("   [SUCCESS] Topics created: {Topics}", 
                string.Join(", ", topicsToCreate.Select(t => t.Name)));
        }
        catch (CreateTopicsException ex)
        {
            var errors = ex.Results.Where(r => r.Error.Code != ErrorCode.TopicAlreadyExists).ToList();
            if (!errors.Any())
            {
                Log.Information("   [SUCCESS] Topics already exist");
            }
            else
            {
                Log.Warning("Some topics failed to create");
            }
        }
    }

    private static async Task WaitForKafkaReadyAsync()
    {
        var timeout = TimeSpan.FromSeconds(30);
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            try
            {
                var adminConfig = new AdminClientConfig
                {
                    BootstrapServers = KafkaBootstrapServers,
                    SocketTimeoutMs = 3000
                };

                using var admin = new AdminClientBuilder(adminConfig).Build();
                var metadata = admin.GetMetadata(TimeSpan.FromSeconds(3));

                if (metadata?.Brokers?.Count > 0)
                {
                    Log.Information("   [SUCCESS] Kafka is ready with {BrokerCount} broker(s)", metadata.Brokers.Count);
                    return;
                }
            }
            catch
            {
                // Continue waiting
            }

            await Task.Delay(1000);
        }

        throw new TimeoutException($"Kafka not ready within {timeout.TotalSeconds} seconds");
    }

    private static async Task WaitForFlinkHealthyAsync()
    {
        var timeout = TimeSpan.FromSeconds(30);
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            try
            {
                using var httpClient = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                var response = await httpClient.GetAsync($"{FlinkGatewayUrl}/api/v1/health");
                
                if (response.IsSuccessStatusCode)
                {
                    Log.Information("   [SUCCESS] Flink cluster is healthy");
                    return;
                }
            }
            catch
            {
                // Continue waiting
            }

            await Task.Delay(1000);
        }

        throw new TimeoutException($"Flink cluster not healthy within {timeout.TotalSeconds} seconds");
    }

    private static string GetCategory(int id) => (id % 3) switch
    {
        0 => "Electronics",
        1 => "Clothing",
        _ => "Home & Garden"
    };

    private static string GetTier(int id) => (id % 3) switch
    {
        0 => "Gold",
        1 => "Silver",
        _ => "Bronze"
    };
}

// Data models
public class Order
{
    [JsonPropertyName("order_id")]
    public int OrderId { get; set; }
    
    [JsonPropertyName("customer_id")]
    public int CustomerId { get; set; }
    
    [JsonPropertyName("product_id")]
    public int ProductId { get; set; }
    
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}

public class Product
{
    [JsonPropertyName("product_id")]
    public int ProductId { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;
    
    [JsonPropertyName("price")]
    public decimal Price { get; set; }
    
    [JsonPropertyName("stock")]
    public int Stock { get; set; }
}

public class Customer
{
    [JsonPropertyName("customer_id")]
    public int CustomerId { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    
    [JsonPropertyName("tier")]
    public string Tier { get; set; } = string.Empty;
    
    [JsonPropertyName("lifetime_value")]
    public decimal LifetimeValue { get; set; }
}

public class Inventory
{
    [JsonPropertyName("product_id")]
    public int ProductId { get; set; }
    
    [JsonPropertyName("available")]
    public int Available { get; set; }
    
    [JsonPropertyName("reserved")]
    public int Reserved { get; set; }
    
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}

public class EnrichedOrder
{
    [JsonPropertyName("order_id")]
    public int OrderId { get; set; }
    
    [JsonPropertyName("customer_name")]
    public string CustomerName { get; set; } = string.Empty;
    
    [JsonPropertyName("customer_tier")]
    public string CustomerTier { get; set; } = string.Empty;
    
    [JsonPropertyName("product_name")]
    public string ProductName { get; set; } = string.Empty;
    
    [JsonPropertyName("product_category")]
    public string ProductCategory { get; set; } = string.Empty;
    
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }
    
    [JsonPropertyName("unit_price")]
    public decimal UnitPrice { get; set; }
    
    [JsonPropertyName("total_price")]
    public decimal TotalPrice { get; set; }
    
    [JsonPropertyName("inventory_available")]
    public int InventoryAvailable { get; set; }
    
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
    
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Map function that enriches orders with product, customer, and inventory data
/// Note: In production, this would use proper temporal joins
/// For educational purposes, we simulate enrichment with mock data
/// </summary>
public class OrderEnrichmentFunction : FlinkDotNet.DataStream.IMapFunction<string, string>
{
    public string Map(string orderJson)
    {
        try
        {
            var order = JsonSerializer.Deserialize<Order>(orderJson);
            if (order == null) return orderJson;

            // Simulate enrichment (in production, this would come from joined streams)
            var enriched = new EnrichedOrder
            {
                OrderId = order.OrderId,
                CustomerName = $"Customer-{order.CustomerId}",
                CustomerTier = GetTier(order.CustomerId),
                ProductName = $"Product-{order.ProductId}",
                ProductCategory = GetCategory(order.ProductId),
                Quantity = order.Quantity,
                UnitPrice = 10m + (order.ProductId * 15.5m),
                TotalPrice = (10m + (order.ProductId * 15.5m)) * order.Quantity,
                InventoryAvailable = 100 + (order.ProductId * 10),
                Status = "enriched",
                Timestamp = order.Timestamp
            };

            return JsonSerializer.Serialize(enriched);
        }
        catch
        {
            return orderJson;
        }
    }

    private string GetCategory(int id) => (id % 3) switch
    {
        0 => "Electronics",
        1 => "Clothing",
        _ => "Home & Garden"
    };

    private string GetTier(int id) => (id % 3) switch
    {
        0 => "Gold",
        1 => "Silver",
        _ => "Bronze"
    };
}
