# Day 13: Advanced Testing Strategies and Chaos Engineering

## 🗺️ Course Navigation
**[← Day 12: Advanced Streaming Patterns](../Day12-Advanced-Streaming-Patterns/)** | **[Course Overview](../README.md)** | **[Next: Day 14 - Capstone Project →](../Day14-Capstone-Project/)**

---

## Overview
Master advanced testing techniques including chaos engineering, property-based testing, contract testing, and production testing for building resilient streaming systems.

## Learning Objectives
- Implement chaos engineering principles for streaming applications
- Design property-based testing for complex stream processing logic
- Build comprehensive testing strategies including unit, integration, and end-to-end tests
- Create production testing frameworks with canary deployments
- Implement contract testing for microservices integration

## Real-World Context
Netflix's Chaos Engineering team uses Chaos Monkey and custom fault injection tools to test their streaming platform's resilience. They perform thousands of controlled failure experiments monthly, ensuring their system handles 200+ million global subscribers with 99.99% availability.

## Technical Deep Dive

### Chaos Engineering for Streaming Systems
```csharp
// Netflix-inspired chaos engineering framework for Flink applications
public class FlinkChaosEngineer
{
    private readonly IFlinkClusterManager clusterManager;
    private readonly INetworkChaos networkChaos;
    private readonly IResourceChaos resourceChaos;
    private readonly IMetricsCollector metricsCollector;
    private readonly IChaosExperimentTracker experimentTracker;
    
    public async Task<ChaosExperimentResult> RunChaosExperiment(ChaosExperiment experiment)
    {
        var experimentId = Guid.NewGuid();
        var startTime = DateTimeOffset.UtcNow;
        
        try
        {
            // Record baseline metrics before chaos
            var baselineMetrics = await CollectBaselineMetrics(experiment.TargetSystem);
            
            // Start monitoring for the experiment
            var monitoringTask = MonitorSystemDuringChaos(experimentId, experiment.Duration);
            
            // Apply chaos based on experiment type
            await ApplyChaos(experiment);
            
            // Let chaos run for specified duration
            await Task.Delay(experiment.Duration);
            
            // Stop chaos and collect results
            await StopChaos(experiment);
            var monitoringResults = await monitoringTask;
            
            // Analyze impact and resilience
            var analysisResult = AnalyzeResilienceImpact(baselineMetrics, monitoringResults);
            
            return new ChaosExperimentResult
            {
                ExperimentId = experimentId,
                Experiment = experiment,
                StartTime = startTime,
                Duration = DateTimeOffset.UtcNow - startTime,
                BaselineMetrics = baselineMetrics,
                ChaosMetrics = monitoringResults,
                ResilienceAnalysis = analysisResult,
                Success = analysisResult.SystemRemainedStable
            };
        }
        finally
        {
            // Ensure cleanup regardless of experiment outcome
            await EnsureSystemCleanup(experiment);
            await experimentTracker.RecordExperiment(experimentId, experiment);
        }
    }
    
    private async Task ApplyChaos(ChaosExperiment experiment)
    {
        switch (experiment.Type)
        {
            case ChaosType.TaskManagerFailure:
                await SimulateTaskManagerFailure(experiment as TaskManagerFailureExperiment);
                break;
                
            case ChaosType.NetworkPartition:
                await SimulateNetworkPartition(experiment as NetworkPartitionExperiment);
                break;
                
            case ChaosType.MemoryPressure:
                await SimulateMemoryPressure(experiment as MemoryPressureExperiment);
                break;
                
            case ChaosType.DiskFull:
                await SimulateDiskFull(experiment as DiskFullExperiment);
                break;
                
            case ChaosType.HighLatency:
                await SimulateHighLatency(experiment as HighLatencyExperiment);
                break;
                
            case ChaosType.PacketLoss:
                await SimulatePacketLoss(experiment as PacketLossExperiment);
                break;
        }
    }
    
    private async Task SimulateTaskManagerFailure(TaskManagerFailureExperiment experiment)
    {
        // Randomly select task managers to fail
        var taskManagers = await clusterManager.GetTaskManagers();
        var failureCount = (int)(taskManagers.Count * experiment.FailurePercentage);
        var selectedTaskManagers = taskManagers.OrderBy(x => Guid.NewGuid()).Take(failureCount);
        
        foreach (var taskManager in selectedTaskManagers)
        {
            if (experiment.FailureMode == FailureMode.Kill)
            {
                await clusterManager.KillTaskManager(taskManager.Id);
            }
            else if (experiment.FailureMode == FailureMode.Pause)
            {
                await clusterManager.PauseTaskManager(taskManager.Id);
            }
            
            // Record the failure for cleanup
            experimentTracker.RecordTaskManagerFailure(taskManager.Id, experiment.FailureMode);
        }
    }
    
    private async Task SimulateNetworkPartition(NetworkPartitionExperiment experiment)
    {
        // Create network partition between specified components
        foreach (var partition in experiment.Partitions)
        {
            await networkChaos.CreatePartition(partition.SourceNodes, partition.TargetNodes);
        }
    }
    
    private async Task SimulateMemoryPressure(MemoryPressureExperiment experiment)
    {
        // Consume memory on target task managers
        var targetNodes = await SelectTargetNodes(experiment.TargetSelector);
        
        foreach (var node in targetNodes)
        {
            await resourceChaos.ConsumeMemory(node, experiment.MemoryPressurePercentage);
        }
    }
}

// Property-based testing for stream processing functions
[TestFixture]
public class StreamProcessingPropertyTests
{
    [Test]
    public void TestWordCountProperties()
    {
        // Property: Word count should be commutative and associative
        Prop.ForAll<string[]>(words =>
        {
            var stream1 = CreateWordCountStream(words.Take(words.Length / 2));
            var stream2 = CreateWordCountStream(words.Skip(words.Length / 2));
            var combined = CombineWordCounts(stream1, stream2);
            
            var directCount = CreateWordCountStream(words);
            
            return WordCountsEqual(combined, directCount);
        }).QuickCheckThrowOnFailure();
    }
    
    [Test]
    public void TestEventTimeWindowProperties()
    {
        // Property: Events should be assigned to correct windows regardless of arrival order
        Prop.ForAll<Event[]>(events =>
        {
            var windowSize = TimeSpan.FromMinutes(5);
            
            // Process events in original order
            var orderedWindows = ProcessEventsInWindows(events, windowSize);
            
            // Process events in random order
            var shuffledEvents = events.OrderBy(x => Guid.NewGuid()).ToArray();
            var shuffledWindows = ProcessEventsInWindows(shuffledEvents, windowSize);
            
            // Results should be identical
            return WindowsEqual(orderedWindows, shuffledWindows);
        }).QuickCheckThrowOnFailure();
    }
    
    [Test]
    public void TestBackpressureProperties()
    {
        // Property: Under backpressure, no events should be lost
        Prop.ForAll<int>(eventCount =>
        {
            eventCount > 0 && eventCount < 10000
        }).Implies(eventCount =>
        {
            var inputEvents = GenerateTestEvents(eventCount);
            var processor = CreateBackpressureTestProcessor();
            
            var outputEvents = processor.ProcessWithBackpressure(inputEvents);
            
            // All events should eventually be processed
            return outputEvents.Count == inputEvents.Count &&
                   outputEvents.All(output => inputEvents.Any(input => 
                       EventsEqual(input, output)));
        }).QuickCheckThrowOnFailure();
    }
}
```

### Contract Testing for Microservices
```csharp
// Pact-style contract testing for streaming microservices
public class StreamingServiceContractTests
{
    private readonly IPactBuilder pactBuilder;
    private readonly MockProviderService mockProvider;
    
    [Test]
    public async Task TestEventPublishingContract()
    {
        // Define contract for event publishing
        pactBuilder
            .ServiceConsumer("flink-stream-processor")
            .HasPactWith("event-gateway-service")
            .Given("Event gateway is available")
            .UponReceiving("A stream processing result event")
            .With(new ProviderServiceRequest
            {
                Method = HttpVerb.Post,
                Path = "/api/events/publish",
                Headers = new Dictionary<string, object>
                {
                    ["Content-Type"] = "application/json",
                    ["X-Event-Type"] = "ProcessingResult"
                },
                Body = new
                {
                    eventId = Guid.NewGuid(),
                    eventType = "ProcessingResult",
                    data = new
                    {
                        streamId = "user-interactions",
                        processedCount = 1000,
                        timestamp = DateTimeOffset.UtcNow
                    },
                    metadata = new
                    {
                        source = "flink-job-cluster",
                        version = "1.0.0"
                    }
                }
            })
            .WillRespondWith(new ProviderServiceResponse
            {
                Status = 202,
                Headers = new Dictionary<string, object>
                {
                    ["Content-Type"] = "application/json"
                },
                Body = new
                {
                    accepted = true,
                    eventId = Guid.NewGuid()
                }
            });
            
        // Execute contract test
        await pactBuilder.VerifyAsync(async () =>
        {
            var eventGatewayClient = new EventGatewayClient(mockProvider.BaseUri);
            var processingResult = new ProcessingResultEvent
            {
                StreamId = "user-interactions",
                ProcessedCount = 1000,
                Timestamp = DateTimeOffset.UtcNow
            };
            
            var response = await eventGatewayClient.PublishAsync(processingResult);
            
            Assert.That(response.Accepted, Is.True);
            Assert.That(response.EventId, Is.Not.Empty);
        });
    }
    
    [Test]
    public async Task TestStateReplicationContract()
    {
        // Contract for cross-region state replication
        pactBuilder
            .ServiceConsumer("flink-state-replicator")
            .HasPactWith("state-store-service")
            .Given("State store is available and empty")
            .UponReceiving("A state checkpoint replication request")
            .With(new ProviderServiceRequest
            {
                Method = HttpVerb.Post,
                Path = "/api/state/replicate",
                Headers = new Dictionary<string, object>
                {
                    ["Content-Type"] = "application/octet-stream",
                    ["X-Checkpoint-Id"] = "12345",
                    ["X-Source-Region"] = "us-east-1",
                    ["X-Target-Region"] = "eu-west-1"
                },
                Body = Convert.ToBase64String(GenerateCheckpointData())
            })
            .WillRespondWith(new ProviderServiceResponse
            {
                Status = 200,
                Headers = new Dictionary<string, object>
                {
                    ["Content-Type"] = "application/json"
                },
                Body = new
                {
                    replicated = true,
                    checkpointId = "12345",
                    replicationLatency = 150
                }
            });
        
        await pactBuilder.VerifyAsync(async () =>
        {
            var stateReplicator = new StateReplicationClient(mockProvider.BaseUri);
            var checkpointData = GenerateCheckpointData();
            
            var response = await stateReplicator.ReplicateAsync(
                checkpointId: "12345",
                sourceRegion: "us-east-1",
                targetRegion: "eu-west-1",
                data: checkpointData);
                
            Assert.That(response.Replicated, Is.True);
            Assert.That(response.ReplicationLatency, Is.LessThan(1000));
        });
    }
}
```

### Production Testing Framework
```csharp
// Facebook-style production testing with gradual rollout
public class ProductionTestingFramework
{
    private readonly ICanaryDeploymentManager canaryManager;
    private readonly IFeatureFlagService featureFlags;
    private readonly IMetricsCollector metrics;
    private readonly IAlertingService alerting;
    
    public async Task<ProductionTestResult> RunProductionTest(ProductionTest test)
    {
        var testId = Guid.NewGuid();
        
        try
        {
            // Phase 1: Deploy to canary environment (1% traffic)
            var canaryResult = await RunCanaryTest(test, testId, trafficPercentage: 1);
            if (!canaryResult.Success)
            {
                return new ProductionTestResult { Success = false, Phase = "Canary", Error = canaryResult.Error };
            }
            
            // Phase 2: Gradual rollout (5% traffic)
            var gradualResult = await RunGradualRollout(test, testId, trafficPercentage: 5);
            if (!gradualResult.Success)
            {
                await RollbackDeployment(testId);
                return new ProductionTestResult { Success = false, Phase = "Gradual", Error = gradualResult.Error };
            }
            
            // Phase 3: Extended testing (25% traffic)
            var extendedResult = await RunExtendedTest(test, testId, trafficPercentage: 25);
            if (!extendedResult.Success)
            {
                await RollbackDeployment(testId);
                return new ProductionTestResult { Success = false, Phase = "Extended", Error = extendedResult.Error };
            }
            
            // Phase 4: Full rollout
            await CompleteRollout(testId);
            
            return new ProductionTestResult 
            { 
                Success = true, 
                TestId = testId,
                CanaryMetrics = canaryResult.Metrics,
                ProductionMetrics = extendedResult.Metrics
            };
        }
        catch (Exception ex)
        {
            await EmergencyRollback(testId);
            await alerting.TriggerCriticalAlert(new ProductionTestFailureAlert
            {
                TestId = testId,
                TestName = test.Name,
                FailureReason = ex.Message,
                RequiresImmediateAction = true
            });
            
            throw;
        }
    }
    
    private async Task<CanaryTestResult> RunCanaryTest(ProductionTest test, Guid testId, int trafficPercentage)
    {
        // Deploy new version to canary servers
        await canaryManager.DeployCanary(test.DeploymentPackage, trafficPercentage);
        
        // Enable feature flag for canary traffic
        await featureFlags.EnableFeature(test.FeatureName, 
            new CanaryTargeting { TrafficPercentage = trafficPercentage });
        
        // Monitor for specified duration
        var monitoringTask = MonitorCanaryMetrics(testId, test.MonitoringDuration);
        await Task.Delay(test.MonitoringDuration);
        var monitoringResults = await monitoringTask;
        
        // Evaluate success criteria
        var success = EvaluateCanarySuccess(monitoringResults, test.SuccessCriteria);
        
        return new CanaryTestResult
        {
            Success = success,
            Metrics = monitoringResults,
            Error = success ? null : "Canary metrics did not meet success criteria"
        };
    }
    
    private async Task<MonitoringResults> MonitorCanaryMetrics(Guid testId, TimeSpan duration)
    {
        var results = new MonitoringResults();
        var endTime = DateTimeOffset.UtcNow.Add(duration);
        
        while (DateTimeOffset.UtcNow < endTime)
        {
            // Collect key metrics
            var currentMetrics = new
            {
                ErrorRate = await metrics.GetErrorRate("canary"),
                Latency = await metrics.GetLatency("canary", percentile: 95),
                Throughput = await metrics.GetThroughput("canary"),
                CPUUsage = await metrics.GetCPUUsage("canary"),
                MemoryUsage = await metrics.GetMemoryUsage("canary")
            };
            
            results.AddSnapshot(currentMetrics);
            
            // Check for immediate failure conditions
            if (currentMetrics.ErrorRate > 0.05) // 5% error rate threshold
            {
                results.EarlyTermination = true;
                results.EarlyTerminationReason = $"Error rate exceeded threshold: {currentMetrics.ErrorRate:P}";
                break;
            }
            
            await Task.Delay(TimeSpan.FromSeconds(30));
        }
        
        return results;
    }
}
```

### Comprehensive Testing Pipeline
```csharp
// Google-style comprehensive testing pipeline
public class StreamingTestPipeline
{
    public async Task<TestPipelineResult> RunComprehensiveTests(StreamingApplication application)
    {
        var pipeline = new TestPipelineBuilder()
            .AddPhase("Unit Tests", RunUnitTests)
            .AddPhase("Integration Tests", RunIntegrationTests)
            .AddPhase("Contract Tests", RunContractTests)
            .AddPhase("Performance Tests", RunPerformanceTests)
            .AddPhase("Chaos Tests", RunChaosTests)
            .AddPhase("Security Tests", RunSecurityTests)
            .AddPhase("End-to-End Tests", RunEndToEndTests)
            .Build();
            
        return await pipeline.ExecuteAsync(application);
    }
    
    private async Task<PhaseResult> RunUnitTests(StreamingApplication application)
    {
        var testRunner = new XUnitTestRunner();
        var testResults = await testRunner.RunTestsAsync(
            testAssemblies: application.GetTestAssemblies(),
            testFilter: "Category=Unit",
            parallelism: 8
        );
        
        return new PhaseResult
        {
            Success = testResults.FailedTests == 0,
            TestCount = testResults.TotalTests,
            Duration = testResults.Duration,
            Coverage = testResults.CodeCoverage,
            Details = testResults.Summary
        };
    }
    
    private async Task<PhaseResult> RunPerformanceTests(StreamingApplication application)
    {
        var performanceTests = new[]
        {
            new ThroughputTest { TargetThroughput = 100_000, Duration = TimeSpan.FromMinutes(5) },
            new LatencyTest { MaxP99Latency = TimeSpan.FromMilliseconds(100), Duration = TimeSpan.FromMinutes(3) },
            new BackpressureTest { MaxBackpressureRatio = 0.1, Duration = TimeSpan.FromMinutes(2) },
            new MemoryLeakTest { MaxMemoryGrowth = 0.05, Duration = TimeSpan.FromMinutes(10) }
        };
        
        var results = new List<PerformanceTestResult>();
        foreach (var test in performanceTests)
        {
            var result = await RunPerformanceTest(application, test);
            results.Add(result);
        }
        
        return new PhaseResult
        {
            Success = results.All(r => r.Success),
            TestCount = results.Count,
            Duration = results.Sum(r => r.Duration.TotalSeconds),
            Details = $"Performance tests: {results.Count(r => r.Success)}/{results.Count} passed"
        };
    }
    
    private async Task<PhaseResult> RunChaosTests(StreamingApplication application)
    {
        var chaosExperiments = new[]
        {
            new TaskManagerFailureExperiment { FailurePercentage = 0.3, Duration = TimeSpan.FromMinutes(5) },
            new NetworkPartitionExperiment { PartitionPercentage = 0.2, Duration = TimeSpan.FromMinutes(3) },
            new MemoryPressureExperiment { MemoryPressurePercentage = 0.8, Duration = TimeSpan.FromMinutes(2) },
            new HighLatencyExperiment { LatencyIncrease = TimeSpan.FromMilliseconds(500), Duration = TimeSpan.FromMinutes(4) }
        };
        
        var chaosEngineer = new FlinkChaosEngineer();
        var results = new List<ChaosExperimentResult>();
        
        foreach (var experiment in chaosExperiments)
        {
            var result = await chaosEngineer.RunChaosExperiment(experiment);
            results.Add(result);
        }
        
        return new PhaseResult
        {
            Success = results.All(r => r.Success),
            TestCount = results.Count,
            Duration = results.Sum(r => r.Duration.TotalSeconds),
            Details = $"Chaos experiments: {results.Count(r => r.Success)}/{results.Count} passed"
        };
    }
}
```

## Hands-On Exercises

### Exercise 1: Chaos Engineering Experiment
Design and implement a chaos engineering experiment that:
- Simulates network partitions between Kafka and Flink
- Measures system recovery time and data consistency
- Validates exactly-once processing guarantees under failure
- Creates automated recovery procedures

### Exercise 2: Property-Based Testing Suite
Build a comprehensive property-based testing suite that:
- Tests stream processing invariants under all input conditions
- Validates windowing and aggregation properties
- Tests serialization/deserialization roundtrip properties
- Ensures backpressure handling correctness

### Exercise 3: Production Testing Pipeline
Create a production testing framework that:
- Implements canary deployments with gradual rollout
- Monitors key business and technical metrics
- Provides automated rollback on failure detection
- Integrates with feature flags for safe experimentation

## Testing Infrastructure

### Test Environment Automation
```csharp
// Infrastructure as Code for testing environments
public class TestEnvironmentProvisioner
{
    public async Task<TestEnvironment> ProvisionTestEnvironment(TestEnvironmentSpec spec)
    {
        // Create isolated test environment
        var environment = new TestEnvironmentBuilder()
            .WithFlinkCluster(spec.FlinkConfiguration)
            .WithKafkaCluster(spec.KafkaConfiguration)
            .WithRedisCluster(spec.RedisConfiguration)
            .WithMonitoring(spec.MonitoringConfiguration)
            .WithNetworkIsolation(spec.NetworkPolicy)
            .Build();
            
        await environment.ProvisionAsync();
        
        // Validate environment readiness
        await ValidateEnvironmentHealth(environment);
        
        return environment;
    }
    
    private async Task ValidateEnvironmentHealth(TestEnvironment environment)
    {
        var healthChecks = new[]
        {
            () => environment.FlinkCluster.IsHealthyAsync(),
            () => environment.KafkaCluster.IsHealthyAsync(),
            () => environment.RedisCluster.IsHealthyAsync(),
            () => environment.Monitoring.IsHealthyAsync()
        };
        
        var results = await Task.WhenAll(healthChecks.Select(check => check()));
        
        if (!results.All(healthy => healthy))
        {
            throw new TestEnvironmentException("Test environment failed health checks");
        }
    }
}
```

## Architecture Integration
- Integrate chaos engineering with CI/CD pipelines
- Set up automated test environment provisioning
- Configure comprehensive monitoring for test results
- Implement test result analytics and trending

## References
- [Netflix Chaos Engineering](https://netflix.github.io/chaosmonkey/)
- [Property-Based Testing with QuickCheck](https://hypothesis.works/articles/what-is-property-based-testing/)
- [Pact Contract Testing](https://docs.pact.io/)
- [Google's Testing Best Practices](https://testing.googleblog.com/)
- [Microsoft's Chaos Engineering Guide](https://docs.microsoft.com/en-us/azure/architecture/framework/resiliency/chaos-engineering)

## Next Steps
Day 14 focuses on the final capstone project where all concepts are integrated into a comprehensive real-world streaming application.
---

## 🗺️ Course Navigation
**[← Day 12: Advanced Streaming Patterns](../Day12-Advanced-Streaming-Patterns/)** | **[Course Overview](../README.md)** | **[Next: Day 14 - Capstone Project →](../Day14-Capstone-Project/)**

**Course Progress**: Day 13 of 14 Complete ✅

## Running Exercises Manually

The exercises can be run manually outside of the integration tests. This requires starting the infrastructure and setting environment variables that are normally discovered automatically by the test framework.

### Step 1: Start Infrastructure

From the repository root, start the LocalTesting infrastructure in LearningCourse mode:

```bash
# Linux/macOS
cd LocalTesting
./run-learningcourse.sh

# Windows (PowerShell)
cd LocalTesting
$env:LEARNINGCOURSE="true"
dotnet run --project LocalTesting.FlinkSqlAppHost --configuration Release
```

This starts:
- Apache Flink cluster (JobManager + TaskManager + SQL Gateway)
- Apache Kafka with JMX metrics
- FlinkDotNet Gateway (port 8086)
- Temporal workflow server (optional, for Day06+)
- Redis (for state management)
- Prometheus (metrics collection)
- Grafana (metrics visualization)

Wait approximately 60 seconds for all containers to be ready.

### Step 2: Discover Service Endpoints

The infrastructure uses dynamic port allocation. You need to discover the actual ports assigned:

1. **Open Aspire Dashboard**: The AppHost will display a URL like `http://localhost:15000`
2. **Find Kafka Port**: Look for "kafka" service, note the host port (e.g., `localhost:32785`)
3. **Find Flink JobManager Port**: Look for "flink-jobmanager-jm-http" service, note the port (e.g., `localhost:32787`)

### Step 3: Set Environment Variables

Before running an exercise, set these environment variables:

```bash
# Linux/macOS
export KAFKA_BOOTSTRAP_SERVERS="localhost:XXXXX"  # Replace XXXXX with discovered Kafka host port
export KAFKA_FLINK_BOOTSTRAP_SERVERS="kafka:9093"  # Fixed container-to-container address
export FLINK_JOB_GATEWAY_URL="http://localhost:8086/"  # Fixed JobGateway port
export FLINK_JOBMANAGER_URL="http://localhost:YYYYY"  # Replace YYYYY with discovered Flink port

# Windows (PowerShell)
$env:KAFKA_BOOTSTRAP_SERVERS="localhost:XXXXX"
$env:KAFKA_FLINK_BOOTSTRAP_SERVERS="kafka:9093"
$env:FLINK_JOB_GATEWAY_URL="http://localhost:8086/"
$env:FLINK_JOBMANAGER_URL="http://localhost:YYYYY"
```

**Optional environment variables** (depending on the exercise):
```bash
# For Day06 Temporal exercises
export TEMPORAL_ENDPOINT="localhost:ZZZZZ"  # Replace with discovered Temporal port

# For exercises using Redis
export REDIS_ENDPOINT="localhost:WWWWW"  # Replace with discovered Redis port
```

### Step 4: Run Exercise

Navigate to the exercise directory and run:

```bash
cd Day01-Kafka-Flink-Data-Pipeline/Exercise-Solutions/Exercise1-StringCapitalize
dotnet run --configuration Release
```

### Environment Variable Reference

| Variable | Purpose | Example Value |
|----------|---------|---------------|
| `KAFKA_BOOTSTRAP_SERVERS` | Kafka address for producer/consumer on host | `localhost:32785` |
| `KAFKA_FLINK_BOOTSTRAP_SERVERS` | Kafka address for Flink jobs (container-to-container) | `kafka:9093` |
| `FLINK_JOB_GATEWAY_URL` | FlinkDotNet Gateway endpoint for job submission | `http://localhost:8086/` |
| `FLINK_JOBMANAGER_URL` | Flink JobManager REST API for health checks | `http://localhost:32787` |
| `TEMPORAL_ENDPOINT` | Temporal server endpoint (Day06+) | `localhost:32789` |
| `REDIS_ENDPOINT` | Redis endpoint for state management | `localhost:32783` |

### Why Dynamic Ports?

The test infrastructure uses .NET Aspire which assigns dynamic ports to avoid conflicts. This is why you need to discover ports from the Aspire Dashboard rather than using hardcoded values.

### Alternative: Use Integration Tests

For automated testing with automatic port discovery, use the integration test framework:

```bash
# Run all Day01 tests
dotnet test LearningCourse/IntegrationTests.sln --filter "FullyQualifiedName~Day01Tests"
```

The integration tests automatically:
- Start the infrastructure
- Discover service endpoints
- Set environment variables
- Run exercises
- Validate results
- Clean up resources

