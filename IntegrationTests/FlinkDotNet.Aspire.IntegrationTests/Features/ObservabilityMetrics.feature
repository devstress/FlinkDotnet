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