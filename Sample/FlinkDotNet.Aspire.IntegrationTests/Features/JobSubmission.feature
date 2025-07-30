@job_submission @integration
Feature: Dotnet Job Submission to Flink
  As a .NET developer using FlinkDotnet framework
  I want to submit streaming jobs to Apache Flink cluster
  So that I can process real-time data streams with .NET business logic

  Background:
    Given the Flink cluster is running and healthy
    And the Job Gateway is accessible
    And Kafka topics are available for testing
    And Redis is available for state management

  @basic_submission @smoke
  Scenario: Submit Basic Streaming Job to Flink
    Given I create a basic streaming job using FlinkJobBuilder
    When I submit the job to Flink cluster via Job Gateway
    Then the job should be accepted and assigned a Flink job ID
    And the job status should be "RUNNING" within 30 seconds
    And I can monitor the job through Flink Web UI

  @job_monitoring @status_tracking
  Scenario: Monitor Job Status and Lifecycle
    Given I have submitted a streaming job to Flink
    When I query the job status through the Job Gateway
    Then I should receive current job status information
    And the status should include job ID, state, and execution details
    And I can track job metrics and performance data

  @error_handling @resilience
  Scenario: Handle Job Submission Errors Gracefully
    Given the Flink cluster is temporarily unavailable
    When I attempt to submit a job
    Then I should receive a clear error message
    And the error should indicate the specific failure reason
    And the client should provide retry guidance

  @complex_pipeline @real_world
  Scenario: Submit Complex Multi-Stage Pipeline
    Given I create a complex streaming pipeline with multiple operations:
      | Step | Operation | Configuration |
      | 1 | KafkaSource | topic=user-events, bootstrap.servers=localhost:9092 |
      | 2 | Filter | condition=eventType == 'purchase' |
      | 3 | Map | transformation=enrichWithUserData |
      | 4 | GroupBy | key=userId |
      | 5 | Window | type=tumbling, size=5minutes |
      | 6 | Aggregate | function=sum, field=amount |
      | 7 | KafkaSink | topic=user-purchase-summary |
    When I submit this complex pipeline to Flink
    Then the job should be successfully deployed with all stages
    And each stage should be properly connected in the execution graph
    And the job should process data through the entire pipeline

  @job_configuration @customization
  Scenario: Submit Job with Custom Configuration
    Given I create a streaming job with custom configuration:
      | Setting | Value |
      | parallelism | 4 |
      | checkpointing.interval | 60000 |
      | restart.strategy | fixed-delay |
      | max.restarts | 3 |
    When I submit the job with these configuration options
    Then the Flink job should respect the custom settings
    And the job should use the specified parallelism level
    And checkpointing should be configured as requested

  @validation @job_definition
  Scenario: Validate Job Definition Before Submission
    Given I create a streaming job with invalid configuration
    When I attempt to submit the job
    Then the Job Gateway should validate the job definition
    And return specific validation errors if the job is invalid
    And provide guidance on how to fix the issues
    And prevent submission of invalid jobs to Flink cluster

  @integration_demo @end_to_end
  Scenario: End-to-End Job Submission Demo
    Given I demonstrate the complete job submission workflow
    When I create a job using FlinkJobBuilder:
      """csharp
      var job = FlinkJobBuilder
          .FromKafka("demo-input")
          .Where("amount > 100")
          .GroupBy("region")
          .Aggregate("SUM", "amount")
          .ToKafka("demo-output");
      """
    And I submit the job with name "DemoStreamingJob"
    Then the job should be successfully submitted to Flink
    And I can verify the job is running in Flink Web UI
    And the job processes messages from input to output topic
    And I can see the generated Intermediate Representation (IR) JSON
    And the complete workflow demonstrates dotnet-to-Flink integration