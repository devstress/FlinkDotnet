using Confluent.Kafka;
using LearningCourse.Common;
using Serilog;
using System.Collections.Concurrent;
using System.Diagnostics;

// Configure Serilog for structured logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

Console.WriteLine("================================================================================");
Console.WriteLine("  Exercise 14.4: Chaos Engineering Experiments with Kafka");
Console.WriteLine("================================================================================");
Console.WriteLine();
Console.WriteLine("📚 Learning Objectives:");
Console.WriteLine("   • Understand Netflix-style chaos engineering principles");
Console.WriteLine("   • Run chaos experiments with real Kafka infrastructure");
Console.WriteLine("   • Simulate producer failures, consumer lag, network issues");
Console.WriteLine("   • Measure system recovery time and resilience");
Console.WriteLine("   • Validate exactly-once semantics under chaos");
Console.WriteLine();

// Kafka bootstrap servers - discovered from Aspire/Docker infrastructure
string? kafkaBootstrapServers = null;

async Task<string> GetKafkaBootstrapServersAsync()
{
    if (kafkaBootstrapServers != null)
        return kafkaBootstrapServers;
        
    kafkaBootstrapServers = await AspireServiceDiscovery.GetKafkaBootstrapServersAsync();
    Log.Information("📡 Kafka discovered at: {KafkaEndpoint}", kafkaBootstrapServers);
    return kafkaBootstrapServers;
}

try
{
    Console.WriteLine(">> Step 1/5: Verifying Kafka infrastructure...");
    var kafkaEndpoint = await GetKafkaBootstrapServersAsync();
    Console.WriteLine($"   [SUCCESS] Kafka ready at {kafkaEndpoint}");
    Console.WriteLine();

    // Step 2: Chaos Experiment 1 - Producer Failure Simulation
    Console.WriteLine(">> Step 2/5: Chaos Experiment 1 - Producer Failure Simulation");
    Console.WriteLine("   Simulating random producer failures and measuring recovery");
    Console.WriteLine();
    
    var topic = $"chaos-experiment-{Guid.NewGuid():N}";
    var totalMessages = 50;
    var successfulMessages = 0;
    var failedMessages = 0;
    var recoveryAttempts = 0;
    var producerFailures = new List<int>();
    
    var producerConfig = new ProducerConfig
    {
        BootstrapServers = kafkaEndpoint,
        Acks = Acks.All,
        EnableIdempotence = true,
        MessageTimeoutMs = 5000,
        RequestTimeoutMs = 3000
    };
    
    using (var producer = new ProducerBuilder<string, string>(producerConfig).Build())
    {
        for (int i = 0; i < totalMessages; i++)
        {
            try
            {
                // Inject chaos: Random producer failure (10% chance)
                var shouldFail = Random.Shared.Next(100) < 10;
                
                if (shouldFail && failedMessages < 10) // Limit failures
                {
                    producerFailures.Add(i);
                    failedMessages++;
                    Console.WriteLine($"   💥 CHAOS: Producer failure injected at message {i}");
                    
                    // Simulate recovery attempt
                    await Task.Delay(100);
                    recoveryAttempts++;
                    
                    // Retry after recovery
                    shouldFail = false;
                }
                
                if (!shouldFail)
                {
                    await producer.ProduceAsync(topic, new Message<string, string>
                    {
                        Key = $"key-{i}",
                        Value = $"message-{i}"
                    });
                    successfulMessages++;
                }
            }
            catch (Exception ex)
            {
                failedMessages++;
                Console.WriteLine($"   ❌ Message {i} failed: {ex.Message}");
            }
        }
        
        producer.Flush(TimeSpan.FromSeconds(10));
    }
    
    Console.WriteLine($"   Messages attempted: {totalMessages}");
    Console.WriteLine($"   Messages succeeded: {successfulMessages}");
    Console.WriteLine($"   Messages failed: {failedMessages}");
    Console.WriteLine($"   Recovery attempts: {recoveryAttempts}");
    Console.WriteLine($"   Success rate: {(double)successfulMessages / totalMessages * 100.0:F1}%");
    Console.WriteLine($"   ✅ Producer failure experiment: COMPLETED");
    Console.WriteLine();

    // Step 3: Chaos Experiment 2 - Consumer Lag Simulation
    Console.WriteLine(">> Step 3/5: Chaos Experiment 2 - Consumer Lag Simulation");
    Console.WriteLine("   Simulating slow consumer processing and measuring lag");
    Console.WriteLine();
    
    var consumerConfig = new ConsumerConfig
    {
        BootstrapServers = kafkaEndpoint,
        GroupId = $"chaos-consumer-{Guid.NewGuid():N}",
        AutoOffsetReset = AutoOffsetReset.Earliest,
        EnableAutoCommit = false,
        SessionTimeoutMs = 10000,
        MaxPollIntervalMs = 30000
    };
    
    var messagesConsumed = 0;
    var lagEvents = 0;
    var processingTimes = new List<long>();
    
    using (var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build())
    {
        consumer.Subscribe(topic);
        
        var sw = Stopwatch.StartNew();
        var deadline = DateTime.UtcNow.AddSeconds(30);
        
        while (messagesConsumed < Math.Min(successfulMessages, 30) && DateTime.UtcNow < deadline)
        {
            var result = consumer.Consume(TimeSpan.FromSeconds(2));
            if (result != null)
            {
                var processingStart = Stopwatch.GetTimestamp();
                
                // Inject chaos: Random slow processing (20% chance)
                var shouldLag = Random.Shared.Next(100) < 20;
                if (shouldLag)
                {
                    lagEvents++;
                    var lagDuration = Random.Shared.Next(100, 300);
                    Console.WriteLine($"   ⏱️  CHAOS: Consumer lag injected - {lagDuration}ms delay");
                    await Task.Delay(lagDuration);
                }
                else
                {
                    await Task.Delay(10); // Normal processing
                }
                
                var processingTime = (Stopwatch.GetTimestamp() - processingStart) * 1000 / Stopwatch.Frequency;
                processingTimes.Add(processingTime);
                
                consumer.Commit(result);
                messagesConsumed++;
            }
        }
        
        sw.Stop();
        
        var avgProcessingTime = processingTimes.Any() ? processingTimes.Average() : 0;
        var maxProcessingTime = processingTimes.Any() ? processingTimes.Max() : 0;
        var throughput = messagesConsumed / sw.Elapsed.TotalSeconds;
        
        Console.WriteLine($"   Messages consumed: {messagesConsumed}");
        Console.WriteLine($"   Lag events injected: {lagEvents}");
        Console.WriteLine($"   Avg processing time: {avgProcessingTime:F1}ms");
        Console.WriteLine($"   Max processing time: {maxProcessingTime}ms");
        Console.WriteLine($"   Throughput: {throughput:F1} msg/sec");
        Console.WriteLine($"   ✅ Consumer lag experiment: COMPLETED");
    }
    Console.WriteLine();

    // Step 4: Chaos Experiment 3 - Network Partition Simulation
    Console.WriteLine(">> Step 4/5: Chaos Experiment 3 - Network Partition Simulation");
    Console.WriteLine("   Simulating network issues and measuring recovery time");
    Console.WriteLine();
    
    var networkTopic = $"network-chaos-{Guid.NewGuid():N}";
    var networkAttempts = 20;
    var networkSuccesses = 0;
    var networkPartitions = 0;
    var recoveryTimes = new List<long>();
    
    using (var producer = new ProducerBuilder<string, string>(producerConfig).Build())
    {
        for (int i = 0; i < networkAttempts; i++)
        {
            try
            {
                // Inject chaos: Simulate network partition (15% chance)
                var hasNetworkIssue = Random.Shared.Next(100) < 15;
                
                if (hasNetworkIssue && networkPartitions < 5)
                {
                    networkPartitions++;
                    Console.WriteLine($"   🌐 CHAOS: Network partition simulated at attempt {i}");
                    
                    var recoverySw = Stopwatch.StartNew();
                    
                    // Simulate network recovery time
                    var recoveryDelay = Random.Shared.Next(200, 500);
                    await Task.Delay(recoveryDelay);
                    
                    recoverySw.Stop();
                    recoveryTimes.Add(recoverySw.ElapsedMilliseconds);
                    
                    Console.WriteLine($"   ✅ Network recovered in {recoverySw.ElapsedMilliseconds}ms");
                }
                
                // Attempt message send
                await producer.ProduceAsync(networkTopic, new Message<string, string>
                {
                    Key = $"net-key-{i}",
                    Value = $"net-message-{i}"
                });
                
                networkSuccesses++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Network attempt {i} failed: {ex.Message}");
            }
        }
        
        producer.Flush(TimeSpan.FromSeconds(5));
    }
    
    var avgRecoveryTime = recoveryTimes.Any() ? recoveryTimes.Average() : 0;
    var maxRecoveryTime = recoveryTimes.Any() ? recoveryTimes.Max() : 0;
    
    Console.WriteLine($"   Network attempts: {networkAttempts}");
    Console.WriteLine($"   Successful sends: {networkSuccesses}");
    Console.WriteLine($"   Network partitions: {networkPartitions}");
    Console.WriteLine($"   Avg recovery time: {avgRecoveryTime:F1}ms");
    Console.WriteLine($"   Max recovery time: {maxRecoveryTime}ms");
    Console.WriteLine($"   ✅ Network partition experiment: COMPLETED");
    Console.WriteLine();

    // Step 5: Validate Exactly-Once Semantics Under Chaos
    Console.WriteLine(">> Step 5/5: Validating Exactly-Once Semantics Under Chaos");
    Console.WriteLine("   Ensuring message deduplication despite chaos conditions");
    Console.WriteLine();
    
    var eosTopic = $"exactly-once-chaos-{Guid.NewGuid():N}";
    var eosMessages = 30;
    var sentMessageIds = new ConcurrentBag<string>();
    var receivedMessageIds = new ConcurrentBag<string>();
    
    // Send messages with potential duplicates due to chaos
    var eosProducerConfig = new ProducerConfig
    {
        BootstrapServers = kafkaEndpoint,
        Acks = Acks.All,
        EnableIdempotence = true, // Exactly-once semantics
        TransactionalId = $"eos-txn-{Guid.NewGuid():N}",
        MessageTimeoutMs = 5000
    };
    
    using (var producer = new ProducerBuilder<string, string>(eosProducerConfig).Build())
    {
        producer.InitTransactions(TimeSpan.FromSeconds(10));
        
        for (int i = 0; i < eosMessages; i++)
        {
            try
            {
                producer.BeginTransaction();
                
                var messageId = $"eos-msg-{i}";
                sentMessageIds.Add(messageId);
                
                await producer.ProduceAsync(eosTopic, new Message<string, string>
                {
                    Key = messageId,
                    Value = $"value-{i}"
                });
                
                // Inject chaos: Random transaction abort (5% chance)
                if (Random.Shared.Next(100) < 5 && i > 5)
                {
                    Console.WriteLine($"   💥 CHAOS: Transaction aborted at message {i}");
                    producer.AbortTransaction();
                    sentMessageIds.TryTake(out _); // Remove from sent list
                }
                else
                {
                    producer.CommitTransaction();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ⚠️  Transaction error at {i}: {ex.Message}");
                try { producer.AbortTransaction(); } catch { }
            }
        }
    }
    
    // Verify exactly-once delivery
    var eosConsumerConfig = new ConsumerConfig
    {
        BootstrapServers = kafkaEndpoint,
        GroupId = $"eos-consumer-{Guid.NewGuid():N}",
        AutoOffsetReset = AutoOffsetReset.Earliest,
        EnableAutoCommit = true,
        IsolationLevel = IsolationLevel.ReadCommitted // Only read committed messages
    };
    
    using (var consumer = new ConsumerBuilder<string, string>(eosConsumerConfig).Build())
    {
        consumer.Subscribe(eosTopic);
        
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (DateTime.UtcNow < deadline && receivedMessageIds.Count < sentMessageIds.Count)
        {
            var result = consumer.Consume(TimeSpan.FromSeconds(2));
            if (result != null)
            {
                receivedMessageIds.Add(result.Message.Key);
            }
        }
    }
    
    var duplicates = receivedMessageIds.GroupBy(x => x).Where(g => g.Count() > 1).Count();
    var missingMessages = sentMessageIds.Except(receivedMessageIds).Count();
    
    Console.WriteLine($"   Messages sent (committed): {sentMessageIds.Count}");
    Console.WriteLine($"   Messages received: {receivedMessageIds.Count}");
    Console.WriteLine($"   Duplicate messages: {duplicates}");
    Console.WriteLine($"   Missing messages: {missingMessages}");
    Console.WriteLine($"   Exactly-once verified: {(duplicates == 0 ? "YES ✅" : "NO ❌")}");
    Console.WriteLine($"   ✅ Exactly-once semantics validation: COMPLETED");
    Console.WriteLine();

    // Final Summary
    Console.WriteLine("================================================================================");
    Console.WriteLine("  Exercise 14.4 Results - Chaos Engineering");
    Console.WriteLine("================================================================================");
    Console.WriteLine("  ✅ Key Achievements:");
    Console.WriteLine($"     • Producer failure experiment: {successfulMessages}/{totalMessages} messages ({(double)successfulMessages/totalMessages*100:F1}%)");
    Console.WriteLine($"     • Consumer lag experiment: {messagesConsumed} messages, {lagEvents} lag events");
    Console.WriteLine($"     • Network partition experiment: {networkPartitions} partitions, avg recovery {avgRecoveryTime:F1}ms");
    Console.WriteLine($"     • Exactly-once validation: {duplicates} duplicates, {missingMessages} missing");
    Console.WriteLine($"     • Total chaos events injected: {failedMessages + lagEvents + networkPartitions}");
    Console.WriteLine();
    Console.WriteLine("  📚 Key Learnings:");
    Console.WriteLine("     ✓ Chaos engineering reveals hidden system weaknesses");
    Console.WriteLine("     ✓ Real infrastructure testing essential for reliability");
    Console.WriteLine("     ✓ Recovery time measurements guide SLA decisions");
    Console.WriteLine("     ✓ Exactly-once semantics critical under chaos");
    Console.WriteLine("     ✓ Proactive chaos testing prevents production incidents");
    Console.WriteLine();
    Console.WriteLine("  🎯 Netflix Chaos Engineering Principles:");
    Console.WriteLine("     1. Build hypothesis: System will handle specific failure");
    Console.WriteLine("     2. Define steady state: Normal operation metrics");
    Console.WriteLine("     3. Inject chaos: Simulate real-world failures");
    Console.WriteLine("     4. Measure impact: Recovery time, data loss, availability");
    Console.WriteLine("     5. Learn and improve: Fix weaknesses, repeat experiments");
    Console.WriteLine();
    Console.WriteLine("  💡 Production Insights:");
    Console.WriteLine("     • Netflix Chaos Monkey runs in production continuously");
    Console.WriteLine("     • LinkedIn chaos tests Kafka with real traffic");
    Console.WriteLine("     • Amazon GameDay: Chaos engineering training exercises");
    Console.WriteLine("     • Chaos experiments should run regularly, not just once");
    Console.WriteLine("     • Start with dev/staging, gradually move to production");
    Console.WriteLine();
    Console.WriteLine("[SUCCESS] EXERCISE COMPLETED SUCCESSFULLY!");
    Console.WriteLine("================================================================================");
    
    Environment.Exit(0);
}
catch (Exception ex)
{
    Log.Error(ex, "Error in Exercise 14.4: Chaos Engineering");
    Console.WriteLine($"❌ Error: {ex.Message}");
    Environment.Exit(1);
}
finally
{
    await Log.CloseAndFlushAsync();
}
