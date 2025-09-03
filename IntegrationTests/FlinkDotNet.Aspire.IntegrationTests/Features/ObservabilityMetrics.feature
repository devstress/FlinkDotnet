Feature: Observability Messages Per Second Metrics
  As a system administrator
  I want to monitor messages-per-second metrics across all layers
  So that I can ensure system performance and troubleshoot bottlenecks

  @observability @metrics @kafka @flink @temporal @flow
  Scenario: Validate Kafka Producer Messages Per Second Metrics
    Given LocalTesting infrastructure is running with observability enabled
    When I produce 1000 messages to Kafka topic "observability-test-input"
    Then Kafka producer messages per second metrics should be greater than 0
    And Prometheus should be able to scrape all observability metrics

  @observability @metrics @flink @processing
  Scenario: Validate Flink Job Processing Rate Metrics
    Given LocalTesting infrastructure is running with observability enabled
    When I produce 500 messages to Kafka topic "flink-input-test"
    And I start a Flink job to process messages
    Then Flink job processing rate metrics should be recorded
    And Prometheus should be able to scrape all observability metrics

  @observability @metrics @temporal @workflows
  Scenario: Validate Temporal Workflow Execution Rate Metrics
    Given LocalTesting infrastructure is running with observability enabled
    When I execute Temporal workflows
    Then Temporal workflow execution rate metrics should be recorded
    And Prometheus should be able to scrape all observability metrics

  @observability @metrics @flow @end-to-end
  Scenario: Validate End-to-End Flow Rate Metrics
    Given LocalTesting infrastructure is running with observability enabled
    When I produce 800 messages to Kafka topic "flow-test-input"
    And I start a Flink job to process messages
    And I execute Temporal workflows
    Then end-to-end flow rate metrics should show total throughput
    And Kafka producer messages per second metrics should be greater than 0
    And Flink job processing rate metrics should be recorded
    And Temporal workflow execution rate metrics should be recorded
    And Prometheus should be able to scrape all observability metrics

  @observability @metrics @comprehensive @performance
  Scenario: Comprehensive Messages Per Second Metrics Validation
    Given LocalTesting infrastructure is running with observability enabled
    When I produce 2000 messages to Kafka topic "comprehensive-test-input"
    And I start a Flink job to process messages
    And I execute Temporal workflows
    Then Kafka producer messages per second metrics should be greater than 0
    And Flink job processing rate metrics should be recorded
    And Temporal workflow execution rate metrics should be recorded
    And end-to-end flow rate metrics should show total throughput
    And Prometheus should be able to scrape all observability metrics

  @observability @message-state @tracking @flow
  Scenario: Track Message State Through Complete Pipeline
    Given LocalTesting infrastructure is running with observability enabled
    When I produce 100 messages to Kafka topic "state-tracking-test" with message state tracking enabled
    And I consume messages from Kafka topic "state-tracking-test"
    And I start a Flink job to process the consumed messages
    And I execute Temporal workflows for the processed messages
    Then I should be able to query message states for all produced messages
    And message states should progress from "Produced" to "Consumed" to "FlinkProcessing" to "Delivered"
    And message state summary should show correct counts for each state
    And message processing times should be recorded accurately

  @observability @message-state @query @filtering
  Scenario: Query Message States with Advanced Filtering
    Given LocalTesting infrastructure is running with observability enabled
    And I have produced 50 messages with tracking to topic "filter-test-topic"
    When I query message states filtered by topic "filter-test-topic"
    Then I should receive only messages for that topic
    When I query message states filtered by state "Produced"
    Then I should receive only messages in "Produced" state
    When I query message states with creation time filter
    Then I should receive only messages within the specified time range

  @observability @message-state @delivery @completion
  Scenario: Validate Message Delivery Status Tracking
    Given LocalTesting infrastructure is running with observability enabled
    When I produce 20 messages to Kafka topic "delivery-test" with tracking enabled
    And all messages complete the end-to-end processing pipeline
    Then all tracked messages should have final state "Delivered"
    And message state summary should show 20 delivered messages
    And average processing time should be calculated correctly
    And no messages should be in failed state

  @observability @message-state @failure @error-handling
  Scenario: Track Message Failures and Error States
    Given LocalTesting infrastructure is running with observability enabled
    When I produce 30 messages to Kafka topic "failure-test" with tracking enabled
    And I simulate processing failures for 30% of the messages
    Then failed messages should have state "Failed"
    And failed messages should contain error details
    And message state summary should show correct counts of failed vs delivered messages
    And I should be able to query only failed messages

  @observability @message-state @cleanup @maintenance
  Scenario: Message State Tracking Cleanup and Maintenance
    Given LocalTesting infrastructure is running with observability enabled
    And I have tracked messages that are older than 1 hour
    When I trigger cleanup of expired message tracking data
    Then expired messages should be removed from tracking
    And cleanup count should reflect the number of removed messages
    And active message tracking should remain unaffected