@backpressure_test @flow_control
Feature: Backpressure Test - Consumer Lag-Based Flow Control (LinkedIn Best Practices)
  As a Flink.NET user
  I want to implement consumer lag-based backpressure following LinkedIn's proven best practices
  So that I can achieve reliable, scalable stream processing at production scale

  Background:
    Given the Flink cluster is running with backpressure monitoring enabled
    And Kafka topics are configured for consumer lag-based backpressure testing
    And Consumer lag monitoring is configured with 5-second intervals
    And Dead Letter Queue (DLQ) topics are configured
    And Kafka Dashboard is available for monitoring

  @backpressure @consumer_lag @linkedin_approach
  Scenario: Consumer Lag-Based Backpressure with Dynamic Scaling (LinkedIn Best Practices)
    Given I have a Kafka setup with multiple clusters for different business domains
    And I configure consumer lag-based backpressure following LinkedIn best practices:
      | Setting | Value |
      | MaxConsumerLag | 10000 messages |
      | ScalingThreshold | 5000 messages lag |
      | QuotaEnforcement | Per-client, per-IP |
      | DynamicRebalancing | Enabled |
      | MonitoringInterval | 5 seconds |
    When I produce 1,000,000 messages with varying consumer processing speeds
    And I simulate different consumer scenarios:
      | Scenario | Consumer Count | Processing Rate | Expected Behavior |
      | Normal operation | 5 consumers | 50k msg/sec each | Steady processing |
      | Consumer failure | 4 consumers (1 fails) | Auto-rebalance | Automatic partition reassignment |
      | Processing slowdown | 5 consumers | 20k msg/sec each | Lag-based throttling |
      | Recovery | 5 consumers | 60k msg/sec each | Catch-up processing |
    Then the system should monitor consumer lag continuously
    And dynamic rebalancing should occur when lag exceeds 5000 messages
    And producer quotas should throttle fast producers when lag builds up
    And auto-scaling should add consumers when sustained lag is detected
    And I should observe the following advantages:
      | Advantage | Measurement |
      | Operational isolation | Different clusters handle different domains |
      | Quota enforcement | Producer rates limited to prevent lag buildup |
      | Dynamic scaling | Auto-scaling triggers within 30 seconds of lag threshold |
      | Mature operations | Established procedures for lag resolution |
    And the system should demonstrate production-ready characteristics:
      | Characteristic | Measurement |
      | Throughput | Sustained 900k+ msgs/sec aggregate |
      | Latency | p99 latency under 150ms |
      | Reliability | 99.9%+ uptime with proper monitoring |
      | Scalability | Linear scaling with consumer additions |

  @backpressure @dashboard_monitoring @kafka_management
  Scenario: Dashboard Monitoring and Kafka Topic Management
    Given I have Kafka dashboards configured for consumer lag monitoring
    And I have DLQ topics configured for failed message handling
    When I run consumer lag-based backpressure tests with monitoring enabled
    Then I should be able to monitor the following metrics in real-time:
      | Metric Category | Specific Metrics | Dashboard Panel |
      | Producer Metrics | Messages/sec, Bytes/sec, Error rate | Producer Performance |
      | Consumer Metrics | Lag, Processing rate, Rebalance frequency | Consumer Health |
      | Broker Metrics | Partition count, Disk usage, Network I/O | Broker Status |
      | Backpressure Metrics | Consumer lag depth, Throttling rate, Auto-scaling triggers | Flow Control |
      | DLQ Metrics | Failed message count, Retry attempts, Error patterns | Error Management |
    And I should be able to trigger the following management actions:
      | Action | Trigger Condition | Expected Outcome |
      | Scale consumers | Consumer lag > 10k messages | Additional consumers added |
      | Throttle producers | Lag growth rate exceeds threshold | Producer rate reduced |
      | Rebalance partitions | Uneven consumer load distribution | Load redistribution |
      | Process DLQ | Failed message accumulation | Manual review and reprocessing |
      | Alert operators | Critical lag thresholds exceeded | Notifications sent |
    And I should be able to design optimal Kafka topics following LinkedIn practices:
      | Topic Purpose | Partition Count | Replication Factor | Retention Policy |
      | High-throughput input | 16-32 | 3 | 7 days |
      | Processing intermediate | 16-32 | 3 | 1 day |
      | Final output | 8-16 | 3 | 30 days |
      | Dead letter queue | 4-8 | 3 | 90 days |
      | Consumer lag metrics | 4 | 3 | 1 day |

  @backpressure @dlq_management @rebalancing
  Scenario: DLQ Management and Consumer Rebalancing Strategies
    Given I have a multi-tier DLQ strategy configured:
      | DLQ Tier | Purpose | Retention | Processing Strategy |
      | Immediate DLQ | Temporary failures | 1 hour | Automatic retry every 5 minutes |
      | Retry DLQ | Repeated failures | 24 hours | Manual review and retry |
      | Dead DLQ | Permanent failures | 30 days | Manual intervention required |
    And I have consumer rebalancing configured with LinkedIn best practices:
      | Setting | Value | Purpose |
      | Session timeout | 30 seconds | Detect failed consumers |
      | Heartbeat interval | 3 seconds | Maintain consumer liveness |
      | Max poll interval | 5 minutes | Allow processing time |
      | Rebalance strategy | CooperativeSticky | Minimize partition movement |
    When I simulate various failure scenarios:
      | Scenario | Failure Type | Expected DLQ Behavior |
      | Network timeout | Transient | Route to Immediate DLQ for retry |
      | Data format error | Permanent | Route to Dead DLQ for manual review |
      | External service unavailable | Temporary | Route to Retry DLQ with backoff |
      | Consumer crash | Infrastructure | Trigger rebalancing, no DLQ |
      | Processing overload | Capacity | Apply backpressure, delay processing |
    Then the DLQ management should:
      | Function | Expected Behavior |
      | Automatic classification | Route messages to appropriate DLQ tier |
      | Retry orchestration | Process Immediate DLQ messages automatically |
      | Manual intervention | Provide tools for Retry and Dead DLQ processing |
      | Error pattern detection | Identify systematic issues for proactive fixes |
      | Capacity management | Monitor DLQ growth and alert on capacity issues |
    And consumer rebalancing should:
      | Function | Expected Behavior |
      | Failure detection | Detect consumer failures within 30 seconds |
      | Partition reassignment | Reassign partitions to healthy consumers |
      | Minimal disruption | Use cooperative rebalancing to minimize impact |
      | State preservation | Maintain processing state during rebalancing |
      | Load distribution | Ensure even partition distribution after rebalancing |

  @backpressure @network_bottlenecks @external_services
  Scenario: Network-Bound Consumer Bottleneck Handling (SFTP/FTP/HTTP)
    Given I have external service dependencies configured:
      | Service Type | Endpoint | Timeout | Circuit Breaker Config | Bulkhead Limit |
      | SFTP Server | sftp://upload.example.com | 10 seconds | 5 failures in 60s opens for 30s | 3 concurrent |
      | HTTP API | https://api.external.com | 5 seconds | 10 failures in 60s opens for 60s | 10 concurrent |
      | FTP Server | ftp://files.partner.com | 15 seconds | 3 failures in 30s opens for 120s | 2 concurrent |
    And I have ordered processing queues configured per partition:
      | Queue Type | Max Depth | Backpressure Trigger | Overflow Strategy |
      | SFTP Queue | 100 messages | 80 messages | Apply backpressure to consumer |
      | HTTP Queue | 500 messages | 400 messages | Apply backpressure to consumer |
      | FTP Queue | 50 messages | 40 messages | Apply backpressure to consumer |
    When I simulate consumer scenarios with external service bottlenecks:
      | Scenario | External Service State | Message Rate | Expected Behavior |
      | Normal operations | All services healthy | 1000 msg/sec | Normal processing, low queue depth |
      | SFTP slowdown | SFTP 50% slower | 1000 msg/sec | SFTP queue builds, backpressure applied |
      | HTTP service down | HTTP circuit open | 1000 msg/sec | HTTP messages routed to DLQ |
      | FTP timeout spike | FTP timeouts | 1000 msg/sec | FTP adaptive timeout increases |
      | Multiple service issues | SFTP slow, HTTP down | 1000 msg/sec | Independent bulkhead isolation |
    Then the network-bound backpressure should:
      | Function | Expected Behavior |
      | Queue depth monitoring | Trigger backpressure at 80% queue capacity |
      | Circuit breaker activation | Open circuit on service failure thresholds |
      | Bulkhead isolation | Isolate failures to specific service types |
      | Adaptive timeout | Adjust timeouts based on service performance |
      | Ordered processing | Maintain message order within partitions |
      | Fallback handling | Route failed messages to appropriate DLQ tier |
    And the system should maintain processing characteristics:
      | Characteristic | Target | Measurement |
      | Queue isolation | No cross-contamination | SFTP issues don't affect HTTP processing |
      | Recovery time | < 30 seconds | Service recovery detected and resumed |
      | Throughput preservation | > 80% during partial failures | Other services maintain throughput |
      | Order preservation | 100% within partition | No message reordering within partition |

  @backpressure @rate_limiting @finite_resources
  Scenario: Rate Limiting with Finite Partition and Cluster Resources
    Given I have finite resource constraints configured:
      | Resource Type | Total Capacity | Per-Consumer Limit | Priority Allocation |
      | Cluster CPU | 100 cores | 5 cores max | 60% critical, 30% normal, 10% batch |
      | Cluster Memory | 1TB | 50GB max | 60% critical, 30% normal, 10% batch |
      | Topic Partitions | 128 partitions | 32 partitions max | Dynamic based on load |
      | Network Bandwidth | 10 Gbps | 1 Gbps max | QoS-based allocation |
    And I have multi-tier rate limiting configured:
      | Tier | Scope | Rate Limit | Burst Allowance | Enforcement |
      | Global | Entire cluster | 10M msg/sec | 15M msg/sec for 30s | Hard limit |
      | Topic | Per topic | 1M msg/sec | 1.5M msg/sec for 10s | Throttling |
      | Consumer Group | Per group | 100K msg/sec | 150K msg/sec for 5s | Back-pressure |
      | Consumer | Per instance | 10K msg/sec | 15K msg/sec for 2s | Circuit breaker |
    When I simulate load scenarios with resource constraints:
      | Scenario | Load Pattern | Resource Pressure | Expected Rate Limiting |
      | Normal load | 5M msg/sec steady | 50% resource usage | No rate limiting applied |
      | Burst load | 12M msg/sec for 60s | 80% resource usage | Burst allowance then throttling |
      | Resource exhaustion | 15M msg/sec sustained | 95% resource usage | Hard rate limiting activated |
      | Priority workload | Critical + batch mixed | 90% resource usage | Batch throttled, critical preserved |
      | Consumer failure | 50% consumers crash | N/A | Automatic rebalancing and rate adjustment |
    And I have adaptive rate limiting configured with rebalancing integration:
      | Integration Point | Trigger Condition | Rate Limit Action | Rebalancing Action |
      | Consumer group scaling | Lag > 10K messages | Increase consumer rate limits | Add consumer instances |
      | Resource pressure | CPU/Memory > 80% | Reduce rate limits 20% | Redistribute partitions |
      | Service degradation | External service slow | Circuit breaker + DLQ | No rebalancing needed |
      | Cluster rebalancing | New consumers join | Recalculate rate limits | Redistribute partitions |
    Then the rate limiting should demonstrate:
      | Capability | Expected Behavior |
      | Hierarchical enforcement | Respect limits at all tiers (global → consumer) |
      | Burst accommodation | Allow temporary bursts within configured windows |
      | Priority preservation | Critical workloads get resources over batch workloads |
      | Adaptive adjustment | Rate limits adjust based on resource availability |
      | Rebalancing integration | Rate limits recalculated during rebalancing |
      | Fair allocation | Resources distributed fairly among equal-priority consumers |
    And the finite resource management should achieve:
      | Target | Measurement |
      | Resource utilization | 70-85% average, never >95% sustained |
      | Rate limit effectiveness | No consumer exceeds allocated rate limits |
      | Rebalancing efficiency | <30 seconds to complete rebalancing |
      | Priority compliance | Critical workloads always get minimum resources |
      | System stability | No resource starvation or cascading failures |

  @backpressure @integration_test @production_ready
  Scenario: Complete Backpressure Integration Test with World-Class Best Practices
    Given I have a production-ready backpressure system configured with:
      | Component | Configuration | World-Class Standard |
      | Consumer lag monitoring | 5-second intervals | LinkedIn production standard |
      | Network-bound handling | Circuit breakers + bulkheads | Netflix resilience patterns |
      | Rate limiting | Multi-tier with adaptation | LinkedIn finite resource management |
      | DLQ management | 3-tier strategy | Uber error handling patterns |
      | Monitoring | Real-time dashboards | Google SRE observability |
    And I have external dependencies that represent real-world bottlenecks:
      | Dependency Type | Simulated Characteristics | Failure Modes |
      | Legacy SFTP | 2-second average latency, 10% failure rate | Timeouts, connection refused |
      | REST API | 100ms average, rate limited at 1000 req/min | HTTP 429, 503 errors |
      | Database | 50ms average, connection pool of 20 | Connection exhaustion, deadlocks |
      | File system | 10ms average, disk I/O bound | Disk full, permission errors |
    When I execute a comprehensive load test simulating production conditions:
      | Test Phase | Duration | Message Rate | Failure Injection | Success Criteria |
      | Warmup | 5 minutes | 10K msg/sec | None | Stable baseline established |
      | Normal load | 15 minutes | 100K msg/sec | None | <5K message lag, <150ms p99 latency |
      | Stress test | 10 minutes | 500K msg/sec | None | Backpressure activates gracefully |
      | Chaos injection | 20 minutes | 100K msg/sec | Random service failures | System remains stable |
      | Recovery test | 10 minutes | 100K msg/sec | Services recover | Quick return to normal |
    Then the integrated backpressure system should achieve world-class standards:
      | World-Class Standard | Target | Actual Achievement |
      | LinkedIn throughput | >900K msg/sec sustained | Measured during test |
      | Netflix availability | >99.9% uptime | No service interruptions |
      | Uber recovery time | <60 seconds | Failure detection and recovery |
      | Google observability | Real-time metrics | Full system visibility |
      | Industry latency | <150ms p99 | End-to-end processing |
    And the system should demonstrate comprehensive coverage:
      | Coverage Area | Implementation | Validation Method |
      | Consumer lag backpressure | LinkedIn patterns | Traditional lag monitoring |
      | Network bottleneck handling | Netflix resilience | External service simulation |
      | Rate limiting integration | Multi-tier enforcement | Resource constraint testing |
      | DLQ error management | 3-tier strategy | Failure injection testing |
      | Monitoring and alerting | SRE practices | Dashboard and alert verification |
      | Production readiness | Industry standards | Full integration testing |

  @backpressure @token_workflow @http_endpoint_failure
  Scenario: Token-Based HTTP Workflow with Endpoint Failure Recovery
    Given I have a token-based HTTP workflow configured with:
      | Component | Configuration | Purpose |
      | Mock Token Provider | Single connection limit with lock | Simulate token acquisition bottleneck |
      | SQLite Token Storage | In-memory database | Persist tokens for reuse |
      | Secured HTTP Endpoint | Circuit breaker + timeout | Target service using security tokens |
      | Backpressure Monitor | Credit-based flow control | Monitor and control request flow |
    And I have concurrency constraints configured:
      | Resource | Limit | Enforcement |
      | Token Provider Connections | 1 concurrent | Mutex lock |
      | SQLite Write Operations | 5 concurrent | Connection pool |
      | Secured Endpoint Requests | 10 concurrent | Rate limiter |
    When I submit a dotnet job to Flink.net that:
      | Step | Action | Expected Behavior |
      | 1. Token Request | Request authentication token from mock HTTP provider | Single connection enforced |
      | 2. Token Storage | Save received token to SQLite database | Successful persistence |
      | 3. Service Call | Use token to authenticate requests to secured HTTP endpoint | Successful authorization |
      | 4. Endpoint Failure | Secured HTTP endpoint goes down during processing | Circuit breaker activation |
      | 5. Backpressure | System detects failures and applies backpressure upstream | Flow control activated |
      | 6. Recovery | Secured HTTP endpoint comes back online | Circuit breaker recovery |
      | 7. Resume | System detects recovery and resumes processing | Normal flow restored |
    Then the token-based workflow should demonstrate proper backpressure handling:
      | Backpressure Aspect | Expected Behavior | Validation Method |
      | Token acquisition throttling | Requests queue when token provider is busy | Monitor connection wait times |
      | SQLite transaction backpressure | Write operations block when connection pool full | Track transaction queue depth |
      | Circuit breaker activation | Requests fail fast when endpoint is down | Verify circuit state transitions |
      | Flow control propagation | Upstream processing slows when downstream fails | Monitor processing rate changes |
      | Recovery detection | System automatically resumes when endpoint recovers | Verify automatic flow restoration |
    And the system should maintain data consistency:
      | Consistency Aspect | Requirement | Validation |
      | Token reuse | Use cached tokens before requesting new ones | Verify SQLite token lookup |
      | Transaction integrity | No partial writes during failure scenarios | Validate SQLite transaction rollbacks |
      | Request ordering | Maintain order of secured requests within partitions | Check message sequence integrity |
      | State recovery | Proper state restoration after endpoint recovery | Verify processing continuation |
    And I should observe the following architecture behavior:
      | Architecture Component | Behavior During Failure | Behavior During Recovery |
      | Credit-based flow control | Reduces credits when endpoint fails | Gradually increases credits on recovery |
      | Token bucket rate limiter | Fills up when requests can't complete | Drains normally when endpoint recovers |
      | Circuit breaker | Opens on repeated failures | Closes on successful health checks |
      | SQLite connection pool | Maintains connections for token lookups | Resumes normal write operations |
      | Flink.NET job processing | Applies backpressure to input streams | Resumes normal processing throughput |

  @backpressure @partition_strategy @space_vs_time
  Scenario: Kafka Partitioning Strategy Analysis - Standard vs Million-Plus Partitions
    Given I have standard partitioning configured with 128 partitions per topic
    And I have quota enforcement configured at multiple levels:
      | Level | Rate Limit | Enforcement | Purpose |
      | Global | 10,000,000 | HardLimit | Cluster-wide protection |
      | Client | 1,000,000 | Throttling | Per-client fairness |
      | User | 100,000 | Backpressure | Per-user limits |
      | IP | 50,000 | CircuitBreaker | Per-IP protection |
    When I validate space versus time trade-offs for partition strategies  
    Then standard partitioning should demonstrate these advantages:
      | Advantage | Measurement |
      | Space: per partition, not per logical queue | Multiple logical queues share partition resources efficiently |
      | Scalability: millions of logical queues over finite partitions | 1M+ logical queues distributed across 128 partitions via consistent hashing |
      | Operational isolation | Separate clusters for different business domains (orders, payments, analytics) |
      | Quota enforcement | Hierarchical rate limiting at Global→Client→User→IP levels |
      | Dynamic scaling | Consumer lag monitoring triggers automatic rebalancing within 30 seconds |
    And standard partitioning should have these limitations:
      | Limitation | Impact |
      | Operational complexity | Requires 8+ configuration files, 5+ monitoring components, SRE expertise |
      | No true per-logical-queue rate limiting | Rate limits enforced per partition, logical queues share limits |
      | Short-term noisy neighbor impact | Burst traffic can temporarily impact other logical queues before mitigation |
    And I should observe quota enforcement at these levels:
      | Level | Rate Limit | Enforcement |
      | Global | 10,000,000 | Hard limit prevents cluster overload |
      | Client | 1,000,000 | Throttling maintains per-client fairness |
      | User | 100,000 | Backpressure signals upstream slowdown |
      | IP | 50,000 | Circuit breaker prevents abuse |

  @backpressure @million_partitions @noisy_neighbor @not_recommended
  Scenario: Million-Plus Partition Strategy Analysis (Not Recommended for Production)
    Given I have million-plus partitioning configured with 1,000,000 partitions
    And I acknowledge this approach is not recommended for production due to:
      | Issue | Impact |
      | Massive resource requirements | 100GB+ memory, exponential coordination overhead |
      | Limited operational tooling | Most tools don't support 1M+ partitions |
      | Resource inefficiency | 85%+ partitions remain underutilized |
      | Cluster limits | LinkedIn/Confluent recommend <300K partitions per cluster |
    When I validate space versus time trade-offs for partition strategies
    Then million-plus partitioning should show noisy neighbor isolation
    And million-plus partitioning should demonstrate resource inefficiency
    And the analysis should show:
      | Metric | Standard Partitioning | Million-Plus Partitioning | Winner |
      | Space efficiency | 80% | 15% | Standard |
      | Time efficiency | 90% | 99% | Million-Plus |
      | Cost efficiency | 85% | 20% | Standard |
      | Noisy neighbor isolation | 90% | 100% | Million-Plus |
      | Operational simplicity | High | Very Low | Standard |
      | Production readiness | Excellent | Poor | Standard |

  @backpressure @production_recommendation @world_class_practices
  Scenario: Production Partitioning Strategy Recommendation
    Given I am designing a production Kafka system following world-class practices
    And I need to choose between partitioning strategies:
      | Strategy | Partitions Per Topic | Logical Queues Per Partition | Resource Cost | Operational Complexity |
      | Standard | 16-128 | Multiple (100+) | Standard | Manageable |
      | Million-Plus | 1,000,000+ | One | 10x+ | Extremely High |
    When I evaluate the strategies against production requirements:
      | Requirement | Weight | Standard Score | Million-Plus Score |
      | Cost efficiency | 25% | 8.5/10 | 2.0/10 |
      | Operational complexity | 20% | 7.0/10 | 1.0/10 |
      | Scalability | 20% | 9.0/10 | 9.9/10 |
      | Noisy neighbor isolation | 15% | 8.0/10 | 10.0/10 |
      | Resource utilization | 10% | 8.0/10 | 1.5/10 |
      | Tool ecosystem support | 10% | 9.5/10 | 2.0/10 |
    Then the analysis should recommend standard partitioning with:
      | Recommendation | Rationale |
      | Use 16-128 partitions per topic | Optimal balance of throughput and operational simplicity |
      | Implement multi-tier rate limiting | Achieves 90% of noisy neighbor benefits at 10% of operational cost |
      | Use separate clusters for business domains | Provides operational isolation without partition explosion |
      | Invest in robust quota enforcement | Client/User/IP level quotas prevent most noisy neighbor issues |
      | Apply LinkedIn/Confluent best practices | Proven patterns from world-class deployments |
    And I should avoid million-plus partitioning unless:
      | Exception Scenario | Justification Required |
      | Perfect isolation is legally required | Regulatory compliance demands complete separation |
      | Unlimited operational budget | Can afford 10x+ infrastructure and operational costs |
      | Custom tooling ecosystem | Built entirely custom monitoring/management tools |

  @backpressure @message_verification @content_headers
  Scenario: Verify Top 10 and Last 10 Messages with Content and Headers - Backpressure Test
    Given I have processed 1,000,000 messages through the backpressure pipeline
    And all messages have been successfully handled with consumer lag-based backpressure
    When I retrieve the first 10 processed messages from the output topic
    Then I can display the top 10 first processed backpressure messages table:
      | Message ID | Content | Headers |
      | 1          | Backpressure msg 1: Consumer lag-based flow control applied successfully | kafka.topic=backpressure-input; consumer.lag=0; backpressure.applied=false |
      | 2          | Backpressure msg 2: Consumer lag-based flow control applied successfully | kafka.topic=backpressure-input; consumer.lag=0; backpressure.applied=false |
      | 3          | Backpressure msg 3: Consumer lag-based flow control applied successfully | kafka.topic=backpressure-input; consumer.lag=100; backpressure.applied=false |
      | 4          | Backpressure msg 4: Consumer lag-based flow control applied successfully | kafka.topic=backpressure-input; consumer.lag=200; backpressure.applied=false |
      | 5          | Backpressure msg 5: Consumer lag-based flow control applied successfully | kafka.topic=backpressure-input; consumer.lag=300; backpressure.applied=true |
      | 6          | Backpressure msg 6: Consumer lag-based flow control applied successfully | kafka.topic=backpressure-input; consumer.lag=400; backpressure.applied=true |
      | 7          | Backpressure msg 7: Consumer lag-based flow control applied successfully | kafka.topic=backpressure-input; consumer.lag=500; backpressure.applied=true |
      | 8          | Backpressure msg 8: Consumer lag-based flow control applied successfully | kafka.topic=backpressure-input; consumer.lag=600; backpressure.applied=true |
      | 9          | Backpressure msg 9: Consumer lag-based flow control applied successfully | kafka.topic=backpressure-input; consumer.lag=700; backpressure.applied=true |
      | 10         | Backpressure msg 10: Consumer lag-based flow control applied successfully | kafka.topic=backpressure-input; consumer.lag=800; backpressure.applied=true |
    When I retrieve the last 10 processed messages from the output topic
    Then I can display the top 10 last processed backpressure messages table:
      | Message ID | Content | Headers |
      | 999991     | Backpressure msg 999991: Final message after complete lag-based backpressure cycle | kafka.topic=backpressure-output; consumer.lag=50; backpressure.applied=false |
      | 999992     | Backpressure msg 999992: Final message after complete lag-based backpressure cycle | kafka.topic=backpressure-output; consumer.lag=45; backpressure.applied=false |
      | 999993     | Backpressure msg 999993: Final message after complete lag-based backpressure cycle | kafka.topic=backpressure-output; consumer.lag=40; backpressure.applied=false |
      | 999994     | Backpressure msg 999994: Final message after complete lag-based backpressure cycle | kafka.topic=backpressure-output; consumer.lag=35; backpressure.applied=false |
      | 999995     | Backpressure msg 999995: Final message after complete lag-based backpressure cycle | kafka.topic=backpressure-output; consumer.lag=30; backpressure.applied=false |
      | 999996     | Backpressure msg 999996: Final message after complete lag-based backpressure cycle | kafka.topic=backpressure-output; consumer.lag=25; backpressure.applied=false |
      | 999997     | Backpressure msg 999997: Final message after complete lag-based backpressure cycle | kafka.topic=backpressure-output; consumer.lag=20; backpressure.applied=false |
      | 999998     | Backpressure msg 999998: Final message after complete lag-based backpressure cycle | kafka.topic=backpressure-output; consumer.lag=15; backpressure.applied=false |
      | 999999     | Backpressure msg 999999: Final message after complete lag-based backpressure cycle | kafka.topic=backpressure-output; consumer.lag=10; backpressure.applied=false |
      | 1000000    | Backpressure msg 1000000: Final message after complete lag-based backpressure cycle | kafka.topic=backpressure-output; consumer.lag=0; backpressure.applied=false |
    And all messages should contain backpressure-specific content and headers
    And all headers should include consumer lag and backpressure application status