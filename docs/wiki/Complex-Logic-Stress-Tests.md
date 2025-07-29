# Complex Logic Stress Tests - Advanced Integration Testing

This document explains FLINK.NET's complex logic stress testing infrastructure using Flink.Net Gateway communication with Apache Flink that validates advanced integration scenarios including correlation ID matching, security token management, batch processing with HTTP endpoints, and comprehensive response verification.

## Overview

Complex Logic Stress Tests simulate real-world enterprise scenarios through Flink.Net Gateway where:
- Messages require correlation tracking across multiple processing stages via Apache Flink
- Security tokens need periodic renewal with thread-safe synchronization
- Batch processing optimizes HTTP endpoint communication through the gateway
- Response verification ensures data integrity across the complete pipeline

## Test Architecture

### Complete Processing Pipeline using Flink.Net Gateway

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│ .NET SDK        │───▶│ Flink.Net       │───▶│ Apache Flink    │
│ (Message        │HTTP│ Gateway         │    │ Cluster         │
│ Producer)       │    │ (ASP.NET Core)  │    │ (TaskManagers)  │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                               │                       │
                               ▼                       │
┌─────────────────┐    ┌─────────────────┐           ▼
│ Memory Store    │◀───│ HTTP Endpoint   │    ┌─────────────────┐
│ (Background     │    │ (Aspire Test)   │◀───│ Flink Message   │
│ Processing)     │    │                 │    │ Puller          │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                               ▲                       │
                               │                       ▼
                       ┌─────────────────┐    ┌─────────────────┐
                       │ Security Token  │    │ Output Queue    │
                       │ Manager         │    │ (1M messages)   │
                       └─────────────────┘    └─────────────────┘
```

### Key Components

1. **Flink.Net Gateway**: ASP.NET Core service managing Apache Flink job submission and execution
2. **Apache Flink Integration**: TaskManagers handle distributed message processing with correlation tracking
3. **Security Token Service**: Renew tokens every 10,000 messages with thread-safe locking
4. **Batch Processing**: Group 100 messages per HTTP request for optimal throughput through the gateway
5. **HTTP Endpoint**: Aspire Test-based service that saves processed messages to memory (no immediate responses)
6. **Memory Store**: Background processing storage where the HTTP endpoint saves processed messages
7. **Flink Message Puller**: Apache Flink component that actively pulls processed messages from endpoint memory
8. **Verification System**: Validate top 100 and last 100 messages for correctness

## BDD Test Scenarios Explained

### How We Implement BDD (Behavior-Driven Development)

Our BDD implementation follows the **Given-When-Then** pattern with comprehensive step definitions:

#### 1. Scenario Setup (Given)
```gherkin
Given I have Flink.Net Gateway configured for Apache Flink communication
And I have a security token service running with 10000 message renewal interval
And I have an HTTP endpoint running on Aspire Test infrastructure
And correlation ID tracking is initialized through Apache Flink streams
```

**What this means line by line:**
- **Line 1**: Configures Flink.Net Gateway as the communication bridge to Apache Flink cluster for job management
- **Line 2**: Initializes a security token service that will automatically renew authentication tokens every 10,000 processed messages
- **Line 3**: Starts an HTTP endpoint using Aspire Test infrastructure for realistic service simulation
- **Line 4**: Initializes correlation ID tracking system through Apache Flink streaming jobs to match requests with responses

#### 2. Test Execution (When)
```gherkin
When I produce 1000000 messages with unique correlation IDs via Flink.Net Gateway
And I start the Apache Flink streaming job with correlation ID subscription through the gateway
And the system processes messages in batches of 100 to the HTTP endpoint via Flink.Net Gateway
And the HTTP endpoint saves processed messages to memory for background processing
And Flink pulls processed messages from the endpoint memory using the message puller component
```

**What this means line by line:**
- **Line 1**: Produces exactly 1 million messages through Flink.Net Gateway, each with a unique correlation ID for tracking
- **Line 2**: Starts Apache Flink streaming job via the gateway that subscribes to correlation IDs for message mapping
- **Line 3**: Groups messages into batches of 100 and sends them to the HTTP endpoint through Flink.Net Gateway processing
- **Line 4**: HTTP endpoint saves the processed messages to memory storage in the background (no immediate responses)
- **Line 5**: Flink actively pulls processed messages from the endpoint's memory store using the message puller component

#### 3. Verification (Then)
```gherkin
Then all 1000000 messages should be processed with correlation ID matching
And security tokens should be renewed exactly 100 times (1M ÷ 10K)
And all batches should be successfully sent to the HTTP endpoint for background processing
And Flink should successfully pull all processed messages from the endpoint memory
And I can verify the top 100 processed messages are correct
And I can verify the last 100 processed messages are correct
```

**What this means line by line:**
- **Line 1**: Verifies that all 1 million messages were processed and their correlation IDs were successfully mapped
- **Line 2**: Confirms security token renewal happened exactly 100 times (1,000,000 messages ÷ 10,000 renewal interval)
- **Line 3**: Validates that all batches were successfully sent to the HTTP endpoint for background processing (no immediate responses expected)
- **Line 4**: Confirms that Flink successfully pulled all processed messages from the endpoint's memory store
- **Line 5**: Displays and validates the first 100 processed messages with their correlation data
- **Line 6**: Displays and validates the last 100 processed messages with their correlation data

### Advanced BDD Scenarios

#### Logical Queue Backpressure Handling
```gherkin
Scenario: Handle Backpressure in Logical Queue Processing
  Given the logical queue is configured with backpressure thresholds
  When message processing rate exceeds consumption capacity
  Then the system should apply backpressure automatically
  And message throughput should stabilize without data loss
  And queue depth should remain within configured limits
```

**Explanation:**
- **Backpressure Configuration**: Sets up Apache Flink backpressure mechanisms with specific thresholds
- **Rate Exceeding**: Simulates conditions where message production exceeds processing capacity
- **Automatic Backpressure**: Validates that the system automatically slows down production to match consumption
- **Stabilization**: Ensures throughput reaches a sustainable level without dropping messages
- **Queue Limits**: Verifies queue depth stays within configured boundaries to prevent memory issues

#### Security Token Synchronization
```gherkin
Scenario: Manage Security Token Renewal with Thread-Safe Locking
  Given multiple processing threads are handling messages concurrently
  When the 10000th message triggers token renewal
  Then only one thread should obtain the renewal lock
  And other threads should wait for the new token
  And all subsequent requests should use the renewed token
  And no authentication failures should occur during renewal
```

**Explanation:**
- **Concurrent Processing**: Multiple threads process messages simultaneously for high throughput
- **Renewal Trigger**: Every 10,000th message triggers the token renewal process
- **Lock Synchronization**: Only one thread can perform token renewal at a time to prevent conflicts
- **Thread Coordination**: Other threads wait for renewal completion before continuing
- **Token Usage**: All threads use the newly renewed token for subsequent requests
- **Failure Prevention**: No authentication errors occur during the token renewal window

#### HTTP Endpoint Background Processing
```gherkin
Scenario: Process Message Batches Through HTTP Endpoint with Background Storage
  Given messages are grouped into batches of exactly 100 messages
  When each batch is sent to the HTTP endpoint
  Then the endpoint should save all 100 messages to memory storage for background processing
  And no immediate response should be returned to the sender
  And each saved message should include the original correlation ID
  And a new SendingID property should be assigned to each saved message
  And processed messages should be available for Flink to pull from memory
```

**Explanation:**
- **Batch Grouping**: Messages are collected into groups of exactly 100 for efficient HTTP processing
- **HTTP Processing**: Each batch is sent as a single HTTP request to the endpoint
- **Background Storage**: The endpoint saves processed messages to memory without returning immediate responses
- **Correlation Preservation**: Original correlation IDs are maintained in the stored messages for tracking
- **SendingID Assignment**: A new SendingID property is added to each processed message in memory
- **Flink Pulling**: Processed messages are made available for Flink to actively pull from the endpoint's memory store

## Test Implementation Details

### Message Structure
```csharp
public class ComplexLogicMessage
{
    public string CorrelationId { get; set; }        // Unique tracking ID
    public long MessageId { get; set; }              // Sequential message number (1-1,000,000)
    public string Payload { get; set; }              // Message content
    public DateTime Timestamp { get; set; }          // Processing timestamp
    public string? SendingID { get; set; }           // Assigned during response processing
    public int BatchNumber { get; set; }             // Batch grouping identifier
}
```

### Security Token Management
```csharp
public class SecurityTokenManager
{
    private readonly SemaphoreSlim _renewalLock = new(1, 1);
    private string _currentToken;
    private int _messagesSinceRenewal = 0;
    
    public async Task<string> GetTokenAsync()
    {
        if (Interlocked.Increment(ref _messagesSinceRenewal) >= 10000)
        {
            await _renewalLock.WaitAsync();
            try
            {
                _currentToken = await RenewTokenAsync();
                Interlocked.Exchange(ref _messagesSinceRenewal, 0);
            }
            finally
            {
                _renewalLock.Release();
            }
        }
        return _currentToken;
    }
}
```

### HTTP Endpoint Implementation
```csharp
[ApiController]
[Route("api/[controller]")]
public class BatchProcessingController : ControllerBase
{
    private readonly IBatchProcessingService _batchService;
    
    public BatchProcessingController(IBatchProcessingService batchService)
    {
        _batchService = batchService;
    }

    [HttpPost("process")]
    public async Task<IActionResult> ProcessBatch([FromBody] ComplexLogicMessage[] batch)
    {
        if (batch.Length != 100)
            return BadRequest("Batch must contain exactly 100 messages");
            
        // Save batch to memory for background processing (no immediate response)
        await _batchService.SaveBatchToMemoryAsync(batch);
        
        // Return success status without processed data
        return Ok(new { Status = "Batch saved for background processing", MessageCount = batch.Length });
    }
    
    [HttpGet("pull")]
    public async Task<IActionResult> PullProcessedMessages([FromQuery] int maxMessages = 100)
    {
        // Allow Flink to pull processed messages from memory
        var messages = await _batchService.PullProcessedMessagesAsync(maxMessages);
        return Ok(messages);
    }
}
```

## Running Complex Logic Stress Tests

### Prerequisites
1. **Aspire Environment**: Start with `cd Sample/FlinkDotNet.Aspire.AppHost && dotnet run`
2. **Docker Services**: Ensure Kafka, Redis, and other dependencies are running
3. **Test Infrastructure**: HTTP endpoint and token service must be available

### Execution Commands
```bash
# Run all complex logic stress tests
cd Sample/FlinkDotNet.Aspire.IntegrationTests
dotnet test --filter "Category=complex_logic_test"

# Run specific complex logic scenarios
dotnet test --filter "TestCategory=correlation_id_test"
dotnet test --filter "TestCategory=security_token_test"
dotnet test --filter "TestCategory=batch_processing_test"

# Run with detailed output for debugging
dotnet test --filter "Category=complex_logic_test" --logger "console;verbosity=detailed"
```

### Performance Expectations
- **Processing Time**: 1 million messages processed within 5 minutes
- **Token Renewals**: Exactly 100 renewals (every 10,000 messages)
- **Batch Count**: 10,000 batches (1M messages ÷ 100 messages per batch)
- **Correlation Matching**: 100% success rate
- **Memory Usage**: < 4GB peak during processing
- **HTTP Response Time**: < 200ms per batch on average

## Verification Output

### Top 100 Messages Example
```
╔═══════════╦═══════════════╦═══════════════╦═══════════╦═══════════════╗
║ MessageID ║ CorrelationID ║ SendingID     ║ BatchNum  ║ ProcessTime   ║
╠═══════════╬═══════════════╬═══════════════╬═══════════╬═══════════════╣
║ 1         ║ corr-000001   ║ send-000001   ║ 1         ║ 10:15:30.123  ║
║ 2         ║ corr-000002   ║ send-000002   ║ 1         ║ 10:15:30.124  ║
║ 3         ║ corr-000003   ║ send-000003   ║ 1         ║ 10:15:30.125  ║
║ ...       ║ ...           ║ ...           ║ ...       ║ ...           ║
║ 100       ║ corr-000100   ║ send-000100   ║ 1         ║ 10:15:30.223  ║
╚═══════════╩═══════════════╩═══════════════╩═══════════╩═══════════════╝
```

### Last 100 Messages Example
```
╔═══════════╦═══════════════╦═══════════════╦═══════════╦═══════════════╗
║ MessageID ║ CorrelationID ║ SendingID     ║ BatchNum  ║ ProcessTime   ║
╠═══════════╬═══════════════╬═══════════════╬═══════════╬═══════════════╣
║ 999901    ║ corr-999901   ║ send-999901   ║ 10000     ║ 10:20:15.891  ║
║ 999902    ║ corr-999902   ║ send-999902   ║ 10000     ║ 10:20:15.892  ║
║ 999903    ║ corr-999903   ║ send-999903   ║ 10000     ║ 10:20:15.893  ║
║ ...       ║ ...           ║ ...           ║ ...       ║ ...           ║
║ 1000000   ║ corr-1000000  ║ send-1000000  ║ 10000     ║ 10:20:15.999  ║
╚═══════════╩═══════════════╩═══════════════╩═══════════╩═══════════════╝
```

## Integration with Basic Stress Tests

Complex Logic Stress Tests complement our [Basic Stress Tests](Stress-Tests-Overview.md) by adding:
- **Business Logic Complexity**: Real-world enterprise scenarios vs. simple throughput
- **Integration Testing**: Multi-service coordination vs. single-component testing  
- **Correlation Tracking**: Message lifecycle management vs. fire-and-forget processing
- **Security Considerations**: Token management vs. unauthenticated processing
- **Response Verification**: End-to-end validation vs. throughput measurement

Both test suites are essential for comprehensive FLINK.NET validation and should be run together for complete system verification.

---
[Back to Stress Tests Overview](Stress-Tests-Overview.md) | [Back to Wiki Home](Home.md)