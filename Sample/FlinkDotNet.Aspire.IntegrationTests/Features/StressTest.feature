@stress_test @high_throughput
Feature: Stress Test - High Throughput Message Processing
  As a Flink.NET user
  I want to process 1 million messages through 100 partitions with FIFO guarantees
  So that I can validate high-throughput streaming performance

  Background:
    Given the Flink cluster is running
    And Redis is available for counters
    And Kafka topics are configured with 100 partitions
    And the FlinkConsumerGroup is ready

  @stress @fifo @exactly_once
  Scenario: Process 1 Million Messages with FIFO and Exactly-Once Semantics
    Given I have a Kafka input topic "stress-input" with 100 partitions
    And I have a Kafka output topic "stress-output" with 100 partitions
    And Redis counters are initialized
    When I produce 1,000,000 messages to the input topic across all partitions
    And I start the Flink streaming job with the following pipeline:
      | Step | Operation | Configuration |
      | 1 | KafkaSource | topic=stress-input, consumerGroup=stress-test-group |
      | 2 | RedisCounterMap | append counter to each message in ordered sequence |
      | 3 | FIFOProcessor | maintain message order per partition |
      | 4 | ExactlyOnceProcessor | ensure exactly-once delivery semantics |
      | 5 | KafkaSink | topic=stress-output, partitions=100 |
    Then all 1,000,000 messages should be processed successfully
    And all messages should maintain FIFO order within each partition
    And each message should have exactly one Redis counter appended
    And all output messages should be distributed across 100 output partitions
    And no messages should be duplicated or lost
    And the processing should complete within 30 minutes

  @stress @throughput @performance
  Scenario: Validate High-Throughput Performance Metrics
    Given I have the stress test pipeline configured
    When I process 1,000,000 messages through the pipeline
    Then the throughput should be at least 1,000 messages per second
    And the end-to-end latency should be less than 10 seconds per message batch
    And memory usage should remain stable throughout processing
    And CPU utilization should not exceed 80% sustained
    And Redis counter operations should complete within 50ms per message

  @stress @partition_distribution
  Scenario: Verify Equal Distribution Across Partitions
    Given I have 100 input partitions and 100 output partitions
    When I produce 1,000,000 messages evenly distributed across input partitions
    And the Flink job processes all messages
    Then each input partition should receive approximately 10,000 messages (±5%)
    And each output partition should receive approximately 10,000 messages (±5%)
    And message distribution should be balanced across all partitions
    And no partition should be empty or significantly over/under utilized

  @stress @concrete_fifo_verification @message_verification
  Scenario: Concrete FIFO Message Processing with Data Verification
    Given I produce 1,000,000 messages with sequential IDs to the input topic
    Then I should see 1,000,000 messages in Kafka input topic "stress-input"
    And I can verify the first 10 messages have IDs: 1, 2, 3, 4, 5, 6, 7, 8, 9, 10
    And I can verify the last 10 messages have IDs: 999991, 999992, 999993, 999994, 999995, 999996, 999997, 999998, 999999, 1000000
    When I submit the Flink job for FIFO processing
    And I wait for the job to process all messages
    Then I should see 1,000,000 messages processed with FIFO order maintained
    And the output topic should contain messages in the same sequential order
    And I can display the top 10 first processed stress messages table:
      | Message ID | Content | Headers |
      | 1          | Message content for ID 1: Sample streaming data payload with business logic applied | kafka.topic=stress-input; kafka.partition=0; correlation.id=corr-000001 |
      | 2          | Message content for ID 2: Sample streaming data payload with business logic applied | kafka.topic=stress-input; kafka.partition=1; correlation.id=corr-000002 |
      | 3          | Message content for ID 3: Sample streaming data payload with business logic applied | kafka.topic=stress-input; kafka.partition=2; correlation.id=corr-000003 |
      | 4          | Message content for ID 4: Sample streaming data payload with business logic applied | kafka.topic=stress-input; kafka.partition=3; correlation.id=corr-000004 |
      | 5          | Message content for ID 5: Sample streaming data payload with business logic applied | kafka.topic=stress-input; kafka.partition=4; correlation.id=corr-000005 |
      | 6          | Message content for ID 6: Sample streaming data payload with business logic applied | kafka.topic=stress-input; kafka.partition=5; correlation.id=corr-000006 |
      | 7          | Message content for ID 7: Sample streaming data payload with business logic applied | kafka.topic=stress-input; kafka.partition=6; correlation.id=corr-000007 |
      | 8          | Message content for ID 8: Sample streaming data payload with business logic applied | kafka.topic=stress-input; kafka.partition=7; correlation.id=corr-000008 |
      | 9          | Message content for ID 9: Sample streaming data payload with business logic applied | kafka.topic=stress-input; kafka.partition=8; correlation.id=corr-000009 |
      | 10         | Message content for ID 10: Sample streaming data payload with business logic applied | kafka.topic=stress-input; kafka.partition=9; correlation.id=corr-000010 |
    And I can display the top 10 last processed stress messages table:
      | Message ID | Content | Headers |
      | 999991     | Message content for ID 999991: Final streaming data payload processed through complete pipeline | kafka.topic=stress-output; kafka.partition=90; correlation.id=corr-999991 |
      | 999992     | Message content for ID 999992: Final streaming data payload processed through complete pipeline | kafka.topic=stress-output; kafka.partition=91; correlation.id=corr-999992 |
      | 999993     | Message content for ID 999993: Final streaming data payload processed through complete pipeline | kafka.topic=stress-output; kafka.partition=92; correlation.id=corr-999993 |
      | 999994     | Message content for ID 999994: Final streaming data payload processed through complete pipeline | kafka.topic=stress-output; kafka.partition=93; correlation.id=corr-999994 |
      | 999995     | Message content for ID 999995: Final streaming data payload processed through complete pipeline | kafka.topic=stress-output; kafka.partition=94; correlation.id=corr-999995 |
      | 999996     | Message content for ID 999996: Final streaming data payload processed through complete pipeline | kafka.topic=stress-output; kafka.partition=95; correlation.id=corr-999996 |
      | 999997     | Message content for ID 999997: Final streaming data payload processed through complete pipeline | kafka.topic=stress-output; kafka.partition=96; correlation.id=corr-999997 |
      | 999998     | Message content for ID 999998: Final streaming data payload processed through complete pipeline | kafka.topic=stress-output; kafka.partition=97; correlation.id=corr-999998 |
      | 999999     | Message content for ID 999999: Final streaming data payload processed through complete pipeline | kafka.topic=stress-output; kafka.partition=98; correlation.id=corr-999999 |
      | 1000000    | Message content for ID 1000000: Final streaming data payload processed through complete pipeline | kafka.topic=stress-output; kafka.partition=99; correlation.id=corr-1000000 |
    And the FIFO order verification should show 100% sequential order compliance