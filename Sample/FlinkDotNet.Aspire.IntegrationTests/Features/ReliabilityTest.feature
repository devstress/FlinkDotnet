@reliability_test @fault_tolerance
Feature: Reliability Test - Fault Tolerance and Recovery
  As a Flink.NET user
  I want to handle 10% failure rates with backpressure and rebalancing
  So that I can ensure system reliability under adverse conditions

  Background:
    Given the Flink cluster is running with fault tolerance enabled
    And Kafka topics are configured for reliability testing
    And Dead Letter Queue (DLQ) topic is available
    And Consumer group rebalancing is enabled

  @reliability @failure_injection @dlq
  Scenario: Handle 10% Message Failures with DLQ Processing
    Given I have a Kafka input topic "reliability-input" 
    And I have a Kafka output topic "reliability-output"
    And I have a Dead Letter Queue topic "reliability-dlq"
    And I configure a 10% artificial failure rate in message processing
    When I produce 1,000,000 messages to the input topic
    And I start the Flink streaming job with fault injection enabled:
      | Step | Operation | Configuration |
      | 1 | KafkaSource | topic=reliability-input, fault-tolerance=enabled |
      | 2 | FaultInjector | failure-rate=10%, failure-type=random |
      | 3 | BackpressureProcessor | handle slow processing scenarios |
      | 4 | RebalancingProcessor | support consumer group rebalancing |
      | 5 | ConditionalSink | success→reliability-output, failure→reliability-dlq |
    Then approximately 900,000 messages (90%) should be processed to output topic
    And approximately 100,000 messages (10%) should be sent to DLQ topic
    And the total message count should equal 1,000,000 (no lost messages)
    And processing should complete despite failures
    And system should maintain stability throughout the test

  @reliability @backpressure @rebalancing  
  Scenario: Handle Backpressure with Consumer Rebalancing
    Given I have a multi-partition Kafka setup
    And I configure slow processing to induce backpressure
    And Consumer group has multiple consumers for rebalancing
    When I start producing messages at high rate (5,000 msg/sec)
    And I configure processing to be slower than input rate (2,000 msg/sec)
    And I trigger consumer rebalancing during processing by:
      | Action | Timing | Expected Behavior |
      | Add consumer instance | After 100K messages | Partition reassignment |
      | Remove consumer instance | After 500K messages | Partition rebalancing |
      | Network partition simulation | After 750K messages | Failover and recovery |
    Then the system should handle backpressure gracefully
    And consumer rebalancing should occur without message loss
    And processing should resume after each rebalancing event
    And end-to-end message delivery should be maintained
    And no duplicate processing should occur during rebalancing

  @reliability @fault_recovery @checkpoint
  Scenario: Validate Fault Recovery from Checkpoints
    Given I have checkpointing enabled with 30-second intervals
    And I have a long-running processing job configured
    When I start processing 1,000,000 messages
    And I introduce system faults at different stages:
      | Fault Type | Timing | Recovery Expectation |
      | TaskManager failure | After 250K messages | Restart from last checkpoint |
      | Network partition | After 500K messages | Automatic reconnection |
      | Processing node failure | After 750K messages | Failover to healthy nodes |
    Then the system should recover from each fault automatically
    And processing should resume from the last successful checkpoint
    And no messages should be lost during fault recovery
    And the final output count should match input count (accounting for DLQ)
    And recovery time should be less than 2 minutes per fault

  @reliability @monitoring @metrics
  Scenario: Monitor System Health During Reliability Testing
    Given I have monitoring and metrics collection enabled
    When I run the reliability test with 10% failures
    Then I should be able to monitor:
      | Metric | Expected Behavior |
      | Message processing rate | Maintains target rate despite failures |
      | Error rate | Stays around 10% as configured |
      | Backpressure indicators | Shows when processing lags behind input |
      | Consumer lag | Remains within acceptable bounds |
      | DLQ message count | Accumulates failed messages correctly |
      | System resource usage | Remains stable under fault conditions |
    And alerts should trigger when error rates exceed thresholds
    And dashboards should show real-time processing health
    And historical metrics should be preserved for analysis

  @reliability @message_verification @content_headers
  Scenario: Verify Top 10 and Last 10 Messages with Content and Headers - Reliability Test  
    Given I have processed 1,000,000 messages through the reliability pipeline with 10% failures
    And all messages have been properly routed to success or DLQ topics
    When I retrieve the first 10 successfully processed messages from the output topic
    Then I can display the top 10 first processed reliability messages table:
      | Message ID | Content | Headers |
      | 1          | Reliability msg 1: Successfully processed through fault-tolerant pipeline | kafka.topic=reliability-output; fault.injected=false; dlq.routed=false |
      | 2          | Reliability msg 2: Successfully processed through fault-tolerant pipeline | kafka.topic=reliability-output; fault.injected=false; dlq.routed=false |
      | 3          | Reliability msg 3: Successfully processed through fault-tolerant pipeline | kafka.topic=reliability-output; fault.injected=false; dlq.routed=false |
      | 4          | Reliability msg 4: Successfully processed through fault-tolerant pipeline | kafka.topic=reliability-output; fault.injected=false; dlq.routed=false |
      | 5          | Reliability msg 5: Successfully processed through fault-tolerant pipeline | kafka.topic=reliability-output; fault.injected=false; dlq.routed=false |
      | 6          | Reliability msg 6: Successfully processed through fault-tolerant pipeline | kafka.topic=reliability-output; fault.injected=false; dlq.routed=false |
      | 7          | Reliability msg 7: Successfully processed through fault-tolerant pipeline | kafka.topic=reliability-output; fault.injected=false; dlq.routed=false |
      | 8          | Reliability msg 8: Successfully processed through fault-tolerant pipeline | kafka.topic=reliability-output; fault.injected=false; dlq.routed=false |
      | 9          | Reliability msg 9: Successfully processed through fault-tolerant pipeline | kafka.topic=reliability-output; fault.injected=false; dlq.routed=false |
      | 10         | Reliability msg 10: Successfully processed through fault-tolerant pipeline | kafka.topic=reliability-output; fault.injected=false; dlq.routed=false |
    When I retrieve the last 10 successfully processed messages from the output topic
    Then I can display the top 10 last processed reliability messages table:
      | Message ID | Content | Headers |
      | 999991     | Reliability msg 999991: Final success after complete fault tolerance testing | kafka.topic=reliability-output; fault.recovery=completed; checkpoint.restored=true |
      | 999992     | Reliability msg 999992: Final success after complete fault tolerance testing | kafka.topic=reliability-output; fault.recovery=completed; checkpoint.restored=true |
      | 999993     | Reliability msg 999993: Final success after complete fault tolerance testing | kafka.topic=reliability-output; fault.recovery=completed; checkpoint.restored=true |
      | 999994     | Reliability msg 999994: Final success after complete fault tolerance testing | kafka.topic=reliability-output; fault.recovery=completed; checkpoint.restored=true |
      | 999995     | Reliability msg 999995: Final success after complete fault tolerance testing | kafka.topic=reliability-output; fault.recovery=completed; checkpoint.restored=true |
      | 999996     | Reliability msg 999996: Final success after complete fault tolerance testing | kafka.topic=reliability-output; fault.recovery=completed; checkpoint.restored=true |
      | 999997     | Reliability msg 999997: Final success after complete fault tolerance testing | kafka.topic=reliability-output; fault.recovery=completed; checkpoint.restored=true |
      | 999998     | Reliability msg 999998: Final success after complete fault tolerance testing | kafka.topic=reliability-output; fault.recovery=completed; checkpoint.restored=true |
      | 999999     | Reliability msg 999999: Final success after complete fault tolerance testing | kafka.topic=reliability-output; fault.recovery=completed; checkpoint.restored=true |
      | 1000000    | Reliability msg 1000000: Final success after complete fault tolerance testing | kafka.topic=reliability-output; fault.recovery=completed; checkpoint.restored=true |
    And all messages should contain reliability-specific content and headers
    And all headers should include fault injection and recovery status