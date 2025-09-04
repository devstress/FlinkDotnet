Feature: Observability Metrics Validation
  As a system administrator
  I want to run the flow and see the metrics
  So that I can monitor system performance

  @observability @metrics @simple
  Scenario: Simple Observability Flow
    When I run the entire flow
    Then we print the metrics to the console