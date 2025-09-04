Feature: Observability End-to-End Flow Metrics
  As a system administrator
  I want to monitor messages-per-second metrics across the complete pipeline
  So that I can ensure system performance and troubleshoot bottlenecks

  @observability @metrics @comprehensive @end-to-end @flow
  Scenario: Comprehensive End-to-End Flow Metrics Validation
    Given LocalTesting infrastructure is running with observability enabled
    When I produce 1000000 messages to Kafka topic "comprehensive-test-input"
    And I start a Flink job to process messages
    And I execute Temporal workflows
    Then Kafka producer messages per second metrics should be greater than 0
    And Flink job processing rate metrics should be recorded
    And Temporal workflow execution rate metrics should be recorded
    And end-to-end flow rate metrics should show total throughput
    And Prometheus should be able to scrape all observability metrics