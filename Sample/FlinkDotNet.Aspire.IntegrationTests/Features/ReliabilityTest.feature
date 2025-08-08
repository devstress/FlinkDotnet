@reliability_test @fault_tolerance
Feature: Reliability Test - Fault Tolerance and Recovery with Actor-Based Resilience
  As a Flink.NET user
  I want to handle 10% failure rates with backpressure, rebalancing, and multi-cluster actor resilience
  So that I can ensure system reliability under adverse conditions at enterprise scale

  Background:
    Given the Flink cluster is running with fault tolerance enabled
    And Kafka topics are configured for reliability testing
    And Dead Letter Queue (DLQ) topic is available
    And Consumer group rebalancing is enabled
    And FlinkDotNet ClusterManager actors are running for resilience
    And FlinkDotNet Resilience components are configured
    And Temporal workflows are available for failure recovery

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

  @reliability @actor_resilience @cluster_failure
  Scenario: Actor-Based Cluster Failure Detection and Recovery
    Given I have 50 cluster actors managing individual Flink clusters
    And each actor monitors cluster health with exponential backoff
    When 5 clusters fail unexpectedly due to infrastructure issues
    Then cluster actors should detect failures within 30 seconds
    And failed cluster actors should initiate immediate isolation procedures
    And healthy cluster actors should remain unaffected
    And failed clusters should be marked as unhealthy in Orchestra
    And automatic recovery workflows should be triggered via Temporal
    And failed clusters should be restored within 5 minutes
    And no cascade failures should propagate to other clusters

  @reliability @circuit_breaker @resilience_patterns
  Scenario: Circuit Breaker Activation Under Sustained Failures
    Given I have FlinkDotNet Resilience circuit breakers configured
    And circuit breakers monitor all external service calls
    When external service failure rate exceeds 20% for 2 minutes
    Then circuit breakers should transition to Open state
    And subsequent calls should be fast-failed without attempting connection
    And circuit breakers should periodically test service recovery
    And when service recovers, circuit breakers should transition to Closed state
    And normal operation should resume automatically
    And no resource exhaustion should occur during failure periods

  @reliability @retry_policies @exponential_backoff
  Scenario: Exponential Backoff Retry Policies for Transient Failures
    Given I have Polly-based retry policies configured for cluster operations
    And retry policies use exponential backoff with jitter
    When cluster operations encounter transient network failures
    Then first retry should occur after 1 second
    And subsequent retries should follow exponential backoff: 2s, 4s, 8s, 16s
    And jitter should be applied to prevent thundering herd effects
    And operations should eventually succeed when service recovers
    And excessive retry attempts should be prevented with max retry limits
    And retry statistics should be collected for monitoring

  @reliability @temporal_workflow_resilience @durable_execution
  Scenario: Temporal Workflow Resilience and State Persistence
    Given I have long-running Temporal workflows managing cluster orchestration
    And workflows maintain state for cluster lifecycle management
    When Temporal worker processes are restarted during workflow execution
    Then workflows should resume from their last persisted state
    And no workflow state should be lost during restarts
    And workflow execution should continue seamlessly
    And workflow history should be preserved for debugging
    And workflow timers and scheduled activities should be restored correctly
    And overall cluster orchestration should remain uninterrupted

  @reliability @health_monitoring @proactive_failure_detection
  Scenario: Proactive Health Monitoring and Failure Prevention
    Given I have continuous health monitoring across all cluster actors
    And health checkers validate cluster responsiveness every 10 seconds
    When cluster performance degrades but hasn't failed completely
    Then health monitoring should detect degradation patterns
    And proactive alerts should be triggered before complete failure
    And preventive actions should be taken to avoid total failure
    And cluster capacity should be adjusted based on health metrics
    And health trends should be analyzed for predictive maintenance

  @reliability @actor_isolation @failure_containment
  Scenario: Actor Isolation Prevents Cascade Failures
    Given I have 100 cluster actors in a fully connected mesh
    And each actor manages an independent cluster lifecycle
    When one cluster actor encounters a critical error
    Then the error should be contained within that specific actor
    And other actors should continue normal operation
    And no error propagation should occur across the actor system
    And failed actor should be quarantined and restarted independently
    And Orchestra should route traffic away from the failed cluster
    And system-wide availability should be maintained above 99%

  @reliability @multi_cluster_failover @automatic_recovery
  Scenario: Multi-Cluster Failover with Automatic Job Migration
    Given I have active jobs running on 20 different clusters
    And jobs are configured with failover capabilities
    When 3 clusters fail simultaneously due to infrastructure issues
    Then affected jobs should be automatically detected
    And job state should be saved to persistent storage
    And jobs should be migrated to healthy clusters within 60 seconds
    And migrated jobs should resume from their last checkpoint
    And no job state or progress should be lost during migration
    And end-to-end processing should continue with minimal disruption