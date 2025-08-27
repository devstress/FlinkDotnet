# Exercise 3.5: Simple BackpressureQueue Implementation (Per-Customer)

## Overview

This exercise demonstrates a **simple per-customer semaphore-based backpressure approach** compared to the complex distributed rate limiting patterns shown in Exercises 3.1-3.4. 

**Architecture**: `Gateway(producer) → Kafka → Flink → Temporal(processor)`

**Key Feature**: `BackpressureQueue=2 per customer` limits each service to maximum 2 concurrent messages **per customer**

**Scale**: 2 Gateways, 4 Flink task managers, 4 Temporal instances

## Architecture Flow

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│              SIMPLE PER-CUSTOMER BACKPRESSURE QUEUE ARCHITECTURE               │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                 │
│  ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐              │
│  │ GATEWAY SERVICE │    │ FLINK PROCESSOR │    │ TEMPORAL SERVICE│              │
│  │                 │    │                 │    │                 │              │
│  │ BackpressureQ=2 │───▶│ BackpressureQ=2 │───▶│ BackpressureQ=2 │              │
│  │  PER CUSTOMER   │    │  PER CUSTOMER   │    │  PER CUSTOMER   │              │
│  │                 │    │                 │    │                 │              │
│  │ • Produces to   │    │ • Consumes from │    │ • Receives msgs │              │
│  │   Kafka         │    │   Kafka         │    │ • Discards msgs │              │
│  │ • Each customer │    │ • Routes by     │    │ • Each customer │              │
│  │   max 2 concur. │    │   customer      │    │   max 2 concur. │              │
│  │   messages      │    │ • Each customer │    │   messages      │              │
│  │                 │    │   max 2 concur. │    │                 │              │
│  │                 │    │   messages      │    │                 │              │
│  └─────────────────┘    └─────────────────┘    └─────────────────┘              │
│            │                       │                       │                    │
│            └──── KAFKA TOPIC ──────┼────── Partitioned ────┘                    │
│                                    │                                            │
│              ┌─────────────────────────────────────────────────────────────┐    │
│              │            NATURAL PER-CUSTOMER BACKPRESSURE FLOW          │    │
│              │                                                             │    │
│              │ 1. Temporal full for Customer A → Flink waits for A only   │    │
│              │ 2. Customer B messages still processed normally            │    │
│              │ 3. Per-customer isolation prevents blocking other customers│    │
│              │ 4. Fairer resource distribution across customers           │    │
│              └─────────────────────────────────────────────────────────────┘    │
│                                                                                 │
└─────────────────────────────────────────────────────────────────────────────────┘
```

## BackpressureQueue Configuration Management

### 🔧 Configuration System

The BackpressureQueue settings are managed through a centralized `BackpressureConfiguration` system that provides:

- **Global default** per-customer limits for all services
- **Service-specific overrides** for different components (Gateway, Flink, Temporal)
- **Runtime configuration** without code changes
- **Validation and monitoring** of configuration values

### Configuration Location and Management

**Primary Configuration Class**: [`BackpressureConfiguration.cs`](./BackpressureConfiguration.cs)

```csharp
// Default configuration - all services use 2 per customer
var config = BackpressureConfiguration.CreateDefault();

// Service-specific overrides example:
config.SetServiceOverride("Gateway", 3);    // Gateways can handle more load
config.SetServiceOverride("Temporal", 1);   // Temporal instances are more constrained

// Configuration is passed to all services
var gateway = new GatewayService(kafkaServer, topic, id, config, logger);
var flink = new FlinkProcessorService(kafkaServer, topic, group, id, endpoints, config, logger);
var temporal = new TemporalService(endpoints, config, logger);
```

### Configuration Options

| Setting | Purpose | Default | Override Example |
|---------|---------|---------|------------------|
| `DefaultMaxConcurrencyPerCustomer` | Global default for all services | 2 | 3 |
| `ServiceOverrides["Gateway"]` | Gateway-specific per-customer limit | Uses default | 4 |
| `ServiceOverrides["Flink"]` | Flink-specific per-customer limit | Uses default | 2 |
| `ServiceOverrides["Temporal"]` | Temporal-specific per-customer limit | Uses default | 1 |

### Runtime Configuration Management

**View Current Configuration:**
```csharp
var configInfo = backpressureConfig.GetConfigurationInfo();
Console.WriteLine($"Config: {configInfo}");
// Output: Default: 2 per customer | Effective: Gateway=2, Flink=2, Temporal=2
```

**Modify Configuration at Runtime:**
```csharp
// Increase Gateway capacity
backpressureConfig.SetServiceOverride("Gateway", 4);

// Reduce Temporal capacity for testing
backpressureConfig.SetServiceOverride("Temporal", 1);

// Remove override (fall back to default)
backpressureConfig.RemoveServiceOverride("Gateway");
```

**Configuration Validation:**
```csharp
// Validates configuration and logs warnings for potential issues
backpressureConfig.ValidateAndLog(logger);
```

**Example Output:**
```
BackpressureQueue Configuration:
  Default per customer: 2
  Service-specific overrides:
    Gateway: 4 per customer
    Temporal: 1 per customer
  Effective settings per service type:
    Gateway: 4 per customer
    Flink: 2 per customer
    Temporal: 1 per customer
```

### Integration Points

1. **Service Initialization**: All services receive `BackpressureConfiguration` and use it to create appropriately configured `BackpressureQueue` instances

2. **Test Scenarios**: Each test scenario includes its configuration for repeatability:
   ```csharp
   var scenario = new TestScenario {
       Name = "High Load Test",
       BackpressureConfig = config  // Passed to all services
   };
   ```

3. **Monitoring**: Runtime statistics include configuration info for debugging

### Configuration Benefits

- **🎯 Flexibility**: Different services can have different per-customer limits
- **🔍 Observability**: Clear visibility into current configuration
- **⚙️ Testability**: Easy to test different configurations
- **🛡️ Safety**: Validation prevents invalid configurations
- **📊 Monitoring**: Configuration info included in runtime statistics

## Test Scenarios

The exercise tests three specific configurations as requested:

| Scenario | TargetMessages | Customers | TopicPartitionCount | BackpressureQueue |
|----------|---------------|-----------|---------------------|-------------------|
| 1        | 3,000,000     | 300       | 4                  | 2 per customer    |
| 2        | 1,000,000     | 300       | 8                  | 2 per customer    |
| 3        | 1,000,000     | 300       | 16                 | 2 per customer    |

## Simple vs Complex Backpressure: Comparison Analysis

### 🟢 Simple Per-Customer BackpressureQueue Approach (This Exercise)

**When to Use:**
- Single-cluster deployments
- Predictable load patterns  
- Teams preferring operational simplicity
- Applications where natural backpressure flow is sufficient

**Advantages:**
- ✅ **Per-Customer Isolation**: Each customer has independent processing limits
- ✅ **Fairness**: One customer cannot block others from processing
- ✅ **Simplicity**: Easy to understand and debug - semaphore per customer
- ✅ **Natural Flow**: Backpressure propagates naturally through the system
- ✅ **No Distributed State**: No coordination overhead between instances
- ✅ **Fast Implementation**: Quick to implement and test
- ✅ **Predictable Behavior**: Clear concurrent processing limits per customer
- ✅ **Resource Bounded**: Hard limits prevent resource exhaustion per customer

**Disadvantages:**
- ❌ **Less Adaptive**: Fixed concurrency limits per customer, not adaptive to system load
- ❌ **No Global Coordination**: Each service limits independently per customer
- ❌ **Memory Overhead**: Maintains semaphores for each active customer
- ❌ **No Cross-Customer Balancing**: Cannot redistribute unused capacity between customers

### 🟡 Complex Distributed Rate Limiting (Exercises 3.1-3.4)

**When to Use:**
- Multi-region deployments
- Variable load patterns requiring adaptive scaling
- Enterprise systems requiring cross-tenant fairness
- Systems needing sophisticated quota management

**Advantages (Netflix/Uber/LinkedIn patterns):**
- ✅ **Adaptive**: Dynamically adjusts to system load and conditions  
- ✅ **Global Coordination**: Fair resource allocation across regions/instances
- ✅ **Fine-Grained**: Per-customer, per-tenant, per-endpoint controls
- ✅ **Enterprise Scale**: Handles millions of operations with coordination
- ✅ **Fault Tolerant**: Graceful degradation during failures
- ✅ **Advanced Policies**: Support for complex business logic in rate limiting

**Disadvantages:**
- ❌ **Complexity**: Requires Redis/coordination infrastructure
- ❌ **Operational Overhead**: More components to monitor and maintain
- ❌ **Latency Overhead**: Network calls for quota checking
- ❌ **Learning Curve**: Harder to understand and debug

## Implementation Highlights

### BackpressureQueue Core Implementation (Per-Customer)

```csharp
public class BackpressureQueue : IDisposable
{
    private readonly int _maxConcurrencyPerCustomer;
    private readonly ConcurrentDictionary<int, SemaphoreSlim> _customerSemaphores;
    
    public BackpressureQueue(int maxConcurrencyPerCustomer, string serviceName)
    {
        _maxConcurrencyPerCustomer = maxConcurrencyPerCustomer;
        _customerSemaphores = new ConcurrentDictionary<int, SemaphoreSlim>();
        // ...
    }

    public async Task<MessageSlot?> TryAcquireAsync(int customerId, string messageId, CancellationToken cancellationToken = default)
    {
        // Get or create semaphore for this customer
        var semaphore = _customerSemaphores.GetOrAdd(customerId, 
            _ => new SemaphoreSlim(_maxConcurrencyPerCustomer, _maxConcurrencyPerCustomer));

        // Non-blocking attempt - immediate backpressure if customer limit reached
        if (await semaphore.WaitAsync(0, cancellationToken))
        {
            return new MessageSlot(this, messageId, customerId);
        }
        return null; // Backpressure applied for this customer
    }
}
```

### Service Implementation Pattern

Each service (Gateway, Flink, Temporal) follows the same per-customer pattern:

```csharp
// Try to acquire processing slot for specific customer
using var slot = await _backpressureQueue.TryAcquireAsync(message.CustomerId, messageId, cancellationToken);

if (slot == null)
{
    // Apply backpressure - drop message or don't commit offset
    return false;
}

// Process message within the slot
await ProcessMessage();
// Slot automatically released on dispose
```

## Quick Start

### Prerequisites
- .NET 9.0 SDK
- Kafka cluster (for real testing) 

### Build and Run

```bash
# Build the exercise
cd Exercise35
dotnet build

# Run the exercise with demo scenarios
dotnet run

# Run with specific scenario
dotnet run -- --scenario 1
```

### Expected Output

```
🚀 Exercise 3.5: Simple BackpressureQueue Implementation
================================================================================
Architecture: Gateway → Kafka → Flink → Temporal
BackpressureQueue=2 for all services
Scale: 2 Gateways, 4 Flink TaskManagers, 4 Temporal instances

📋 Test Scenarios Overview:
  • Scenario 1: High Volume
    Messages: 3,000,000 | Customers: 300 | Partitions: 4 | BackpressureQueue: 2
  • Scenario 2: Medium Volume, More Partitions
    Messages: 1,000,000 | Customers: 300 | Partitions: 8 | BackpressureQueue: 2
  • Scenario 3: Medium Volume, Max Partitions
    Messages: 1,000,000 | Customers: 300 | Partitions: 16 | BackpressureQueue: 2

🔬 Running Scenario 1: High Volume...
📊 Scenario 1: High Volume Results:
  Duration: 00:30
  Messages Sent: 1,250
  Messages Processed: 1,100
  Messages Dropped (Backpressure): 150
  Success Rate: 88.0%
  Throughput: 42 msg/sec
  Service Statistics:
    Gateway-0: 85.5% utilization, 2/2 active
    Gateway-1: 79.2% utilization, 2/2 active
    Flink-TM-0: 92.1% utilization, 2/2 active
    Flink-TM-1: 88.7% utilization, 2/2 active
    Temporal-0: 95.3% utilization, 2/2 active
    Temporal-1: 91.8% utilization, 2/2 active
```

## Key Insights

### 1. Natural Backpressure Propagation
- When Temporal is full → Flink stops committing offsets → Kafka consumer lag increases
- When Flink is full → Kafka naturally applies backpressure to producers  
- When Gateway is full → Client requests queue or timeout

### 2. Predictable Performance
- With BackpressureQueue=2, you get predictable maximum resource usage
- Easy to capacity plan: 2 Gateways × 2 concurrent = 4 max producer operations
- Clear bottleneck identification through utilization metrics

### 3. Operational Simplicity  
- No external dependencies (Redis, distributed coordination)
- Simple monitoring: just check semaphore counts
- Easy debugging: clear processing slot allocation

## Comparison Summary

| Aspect | Simple BackpressureQueue | Netflix/Uber Distributed |
|--------|-------------------------|---------------------------|
| **Complexity** | Low | High |
| **Dependencies** | None | Redis, coordination |
| **Adaptability** | Fixed limits | Dynamic adaptation |
| **Global Fairness** | No | Yes |
| **Operational Cost** | Low | High |
| **Learning Curve** | Easy | Steep |
| **Use Case** | Single cluster, predictable load | Multi-region, variable load |
| **Implementation Time** | Hours | Weeks |

## Conclusion

**Choose Simple BackpressureQueue when:**
- You have predictable load patterns
- Single-cluster deployment
- Team prefers operational simplicity
- Quick implementation is needed

**Choose Distributed Rate Limiting when:**
- Multi-region deployments
- Need global fairness guarantees  
- Variable load requiring adaptation
- Enterprise-scale coordination needed

Both approaches are valid - the choice depends on your specific requirements for complexity vs. functionality trade-offs.