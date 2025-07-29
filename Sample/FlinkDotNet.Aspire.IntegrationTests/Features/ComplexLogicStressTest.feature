@complex_logic_test @integration_test
Feature: Complex Logic Stress Test - Advanced Integration with Correlation ID and HTTP Processing
  As a Flink.NET enterprise user
  I want to process 1 million messages with correlation ID tracking, security token management, batch HTTP processing, and response verification
  So that I can validate complex real-world enterprise streaming scenarios

  Background:
    Given the Aspire test environment is running with all required services
    And the HTTP endpoint is available for batch processing
    And the security token service is initialized
    And logical queues are configured with backpressure handling
    And correlation ID tracking system is ready

  @complex_logic @correlation_id_test @security_token_test @http_batch_test
  Scenario: Process 1 Million Messages with Complete Integration Pipeline
    Given I have a logical queue "complex-input" configured with backpressure handling
    And I have a logical queue "complex-output" for response processing
    And I have a security token service running with 10000 message renewal interval
    And I have an HTTP endpoint running on Aspire Test infrastructure at "/api/batch/process"
    And correlation ID tracking is initialized for 1000000 messages
    When I produce 1000000 messages with unique correlation IDs to the logical queue
    And I subscribe to correlation IDs for response matching
    And I start the Flink streaming job with the complex logic pipeline:
      | Step | Operation | Configuration |
      | 1 | KafkaSource | topic=complex-input, consumerGroup=complex-logic-group |
      | 2 | CorrelationIdSubscription | track all correlation IDs for message mapping |
      | 3 | SecurityTokenManager | renew token every 10000 messages with thread sync |
      | 4 | BatchProcessor | group 100 messages per batch for HTTP processing |
      | 5 | HttpEndpointProcessor | send batches to /api/batch/process endpoint for background mapping |
      | 6 | FlinkMessagePuller | use Flink to pull processed messages from endpoint memory |
      | 7 | SendingIdAssigner | assign SendingID property to each pulled message |
      | 8 | CorrelationMatcher | match pulled messages to original correlation IDs |
      | 9 | KafkaSink | topic=complex-output, maintain correlation tracking |
    Then all 1000000 messages should be processed with correlation ID matching
    And security tokens should be renewed exactly 100 times during processing
    And all 10000 batches should be successfully sent to the HTTP endpoint for background processing
    And Flink should successfully pull all processed messages from the endpoint memory
    And the SendingID property should be assigned to all 1000000 pulled messages
    And all pulled messages should be matched to their original correlation IDs
    And all response messages should be written to the output logical queue
    And I can verify the top 10 processed messages with their correlation data:
      | MessageID | Content | Headers |
      | 1         | Complex logic msg 1: Correlation tracked, security token renewed, HTTP batch processed | kafka.topic=complex-input; correlation.id=corr-000001; batch.number=1 |
      | 2         | Complex logic msg 2: Correlation tracked, security token renewed, HTTP batch processed | kafka.topic=complex-input; correlation.id=corr-000002; batch.number=1 |
      | 3         | Complex logic msg 3: Correlation tracked, security token renewed, HTTP batch processed | kafka.topic=complex-input; correlation.id=corr-000003; batch.number=1 |
      | 4         | Complex logic msg 4: Correlation tracked, security token renewed, HTTP batch processed | kafka.topic=complex-input; correlation.id=corr-000004; batch.number=1 |
      | 5         | Complex logic msg 5: Correlation tracked, security token renewed, HTTP batch processed | kafka.topic=complex-input; correlation.id=corr-000005; batch.number=1 |
      | 6         | Complex logic msg 6: Correlation tracked, security token renewed, HTTP batch processed | kafka.topic=complex-input; correlation.id=corr-000006; batch.number=1 |
      | 7         | Complex logic msg 7: Correlation tracked, security token renewed, HTTP batch processed | kafka.topic=complex-input; correlation.id=corr-000007; batch.number=1 |
      | 8         | Complex logic msg 8: Correlation tracked, security token renewed, HTTP batch processed | kafka.topic=complex-input; correlation.id=corr-000008; batch.number=1 |
      | 9         | Complex logic msg 9: Correlation tracked, security token renewed, HTTP batch processed | kafka.topic=complex-input; correlation.id=corr-000009; batch.number=1 |
      | 10        | Complex logic msg 10: Correlation tracked, security token renewed, HTTP batch processed | kafka.topic=complex-input; correlation.id=corr-000010; batch.number=1 |
    And I can verify the last 10 processed messages with their correlation data:
      | MessageID | Content | Headers |
      | 999991    | Complex logic msg 999991: Final correlation match with complete HTTP processing | kafka.topic=complex-output; correlation.id=corr-999991; batch.number=10000 |
      | 999992    | Complex logic msg 999992: Final correlation match with complete HTTP processing | kafka.topic=complex-output; correlation.id=corr-999992; batch.number=10000 |
      | 999993    | Complex logic msg 999993: Final correlation match with complete HTTP processing | kafka.topic=complex-output; correlation.id=corr-999993; batch.number=10000 |
      | 999994    | Complex logic msg 999994: Final correlation match with complete HTTP processing | kafka.topic=complex-output; correlation.id=corr-999994; batch.number=10000 |
      | 999995    | Complex logic msg 999995: Final correlation match with complete HTTP processing | kafka.topic=complex-output; correlation.id=corr-999995; batch.number=10000 |
      | 999996    | Complex logic msg 999996: Final correlation match with complete HTTP processing | kafka.topic=complex-output; correlation.id=corr-999996; batch.number=10000 |
      | 999997    | Complex logic msg 999997: Final correlation match with complete HTTP processing | kafka.topic=complex-output; correlation.id=corr-999997; batch.number=10000 |
      | 999998    | Complex logic msg 999998: Final correlation match with complete HTTP processing | kafka.topic=complex-output; correlation.id=corr-999998; batch.number=10000 |
      | 999999    | Complex logic msg 999999: Final correlation match with complete HTTP processing | kafka.topic=complex-output; correlation.id=corr-999999; batch.number=10000 |
      | 1000000   | Complex logic msg 1000000: Final correlation match with complete HTTP processing | kafka.topic=complex-output; correlation.id=corr-1000000; batch.number=10000 |
    And the correlation ID matching should show 100% success rate

  @complex_logic @backpressure_handling @logical_queue_test
  Scenario: Handle Backpressure in Logical Queue Processing
    Given the logical queue "complex-input" is configured with backpressure thresholds
    And the processing capacity is limited to simulate backpressure conditions
    When I produce messages at a rate exceeding the processing capacity
    And the logical queue approaches its configured limits
    Then the system should apply backpressure automatically
    And message production should slow down to match consumption rate
    And no messages should be lost during backpressure application
    And queue depth should remain within configured limits
    And processing should continue smoothly after backpressure is relieved



  @complex_logic @performance_verification @integration_validation
  Scenario: Validate Complete Integration Performance
    Given the complete complex logic pipeline is configured and running
    When 1000000 messages are processed through all integration steps
    Then the total processing time should be less than 5 minutes
    And memory usage should remain below 4GB throughout processing
    And CPU utilization should remain stable without spikes
    And no errors should occur in any processing stage
    And all messages should maintain data integrity through the pipeline
    And the system should gracefully handle any temporary service disruptions

  @complex_logic @message_verification @data_integrity
  Scenario: Verify Top and Last 100 Messages for Data Integrity
    Given 1000000 messages have been processed through the complete pipeline
    And correlation ID matching has been completed
    And all response messages have been written to the output queue
    When I retrieve the first 100 processed messages from the output queue
    Then I should see messages with IDs 1 through 100 in sequential order
    And each message should have the correct correlation ID (corr-000001 through corr-000100)
    And each message should have a valid SendingID assigned
    And each message should belong to batch number 1
    When I retrieve the last 100 processed messages from the output queue
    Then I should see messages with IDs 999901 through 1000000 in sequential order
    And each message should have the correct correlation ID (corr-999901 through corr-1000000)
    And each message should have a valid SendingID assigned
    And each message should belong to batch number 10000
    And all messages should have consistent data structure and formatting