# Summary

|||
|:---|:---|
| Generated on: | 10/13/2025 - 16:03:22 |
| Coverage date: | 10/13/2025 - 16:02:54 |
| Parser: | MultiReport (2x Cobertura) |
| Assemblies: | 2 |
| Classes: | 97 |
| Files: | 24 |
| **Line coverage:** | 4.1% (88 of 2107) |
| Covered lines: | 88 |
| Uncovered lines: | 2019 |
| Coverable lines: | 2107 |
| Total lines: | 8630 |
| **Branch coverage:** | 6.4% (59 of 909) |
| Covered branches: | 59 |
| Total branches: | 909 |
| **Method coverage:** | [Feature is only available for sponsors](https://reportgenerator.io/pro) |

# Risk Hotspots

| **Assembly** | **Class** | **Method** | **Crap Score** | **Cyclomatic complexity** |
|:---|:---|:---|---:|---:|
| Flink.JobBuilder | Flink.JobBuilder.Backpressure.DefaultKafkaConsumerLagMonitor | ComputeLagForGroup(...) | 506 | 22 || Flink.JobBuilder | Flink.JobBuilder.Demo.RateLimitingDemo | DemonstrateMultiTierRateLimiting() | 342 | 18 || FlinkDotNet.JobGateway | FlinkDotNet.JobGateway.Services.FlinkJobManager | CollectConnectorJars() | 342 | 18 || Flink.JobBuilder | Flink.JobBuilder.Backpressure.TokenBucketRateLimiter | Dispose(...) | 272 | 16 || FlinkDotNet.JobGateway | FlinkDotNet.JobGateway.Services.FlinkJobManager | TryGetJobIdFromHeaders(...) | 272 | 16 || Flink.JobBuilder | Flink.JobBuilder.Services.JobDefinitionValidator | ValidateOperation(...) | 267 | 24 || Flink.JobBuilder | Flink.JobBuilder.Extensions.JobDefinitionExtensions | ValidateSink(...) | 210 | 14 || FlinkDotNet.JobGateway | FlinkDotNet.JobGateway.Services.FlinkJobManager | ExtractJobIdFromOverviewElement(...) | 210 | 14 || FlinkDotNet.JobGateway | FlinkDotNet.JobGateway.Services.FlinkJobManager | MatchJobEntry(...) | 210 | 14 || Flink.JobBuilder | Flink.JobBuilder.Backpressure.LagBasedRateLimiter | Dispose(...) | 156 | 12 || Flink.JobBuilder | Flink.JobBuilder.Backpressure.LagBasedRateLimiter | .ctor(...) | 156 | 12 || Flink.JobBuilder | Flink.JobBuilder.Backpressure.MultiTierRateLimiter | IsApplicableTier(...) | 156 | 12 || Flink.JobBuilder | Flink.JobBuilder.Flink.FlinkRedisSink | ApplyConfigurationOption(...) | 156 | 12 || Flink.JobBuilder | Flink.JobBuilder.Services.JobDefinitionValidator | ValidateRetryOperation(...) | 156 | 12 || Flink.JobBuilder | Flink.JobBuilder.Backpressure.BufferPool<T> | .ctor(...) | 110 | 10 || Flink.JobBuilder | Flink.JobBuilder.Backpressure.KafkaRateLimiterStateStorage | Dispose(...) | 110 | 10 || Flink.JobBuilder | Flink.JobBuilder.Backpressure.MultiTierRateLimiter | Dispose(...) | 110 | 10 || Flink.JobBuilder | Flink.JobBuilder.Backpressure.TokenBucketRateLimiter | .ctor(...) | 110 | 10 || Flink.JobBuilder | Flink.JobBuilder.Demo.RateLimitingDemo | DemonstrateTokenBucketRateLimiter() | 110 | 10 || Flink.JobBuilder | Flink.JobBuilder.Extensions.JobDefinitionExtensions | ValidateOperation(...) | 110 | 10 || Flink.JobBuilder | Flink.JobBuilder.Services.JobDefinitionValidator | ValidateStateOperation(...) | 110 | 10 || FlinkDotNet.JobGateway | FlinkDotNet.JobGateway.Services.FlinkJobManager | LogKafkaConfiguration(...) | 110 | 10 || FlinkDotNet.JobGateway | FlinkDotNet.JobGateway.Services.FlinkJobManager | ValidateBasicProperties(...) | 110 | 10 || FlinkDotNet.JobGateway | FlinkDotNet.JobGateway.Services.FlinkJobManager | ValidateSource(...) | 110 | 10 || FlinkDotNet.JobGateway | FlinkDotNet.JobGateway.Services.FlinkJobManager | ValidateSink(...) | 110 | 10 || Flink.JobBuilder | Flink.JobBuilder.Backpressure.BufferPool<T> | Dispose(...) | 72 | 8 || Flink.JobBuilder | Flink.JobBuilder.Backpressure.DefaultKafkaConsumerLagMonitor | RefreshLagData(...) | 72 | 8 || Flink.JobBuilder | Flink.JobBuilder.Backpressure.MultiTierRateLimiter | TryAcquire(...) | 72 | 8 || Flink.JobBuilder | Flink.JobBuilder.Backpressure.MultiTierRateLimiter | OnAdaptiveAdjustment(...) | 72 | 8 || Flink.JobBuilder | Flink.JobBuilder.Backpressure.SlidingWindowRateLimiter | CalculateWaitTime(...) | 72 | 8 || Flink.JobBuilder | Flink.JobBuilder.Extensions.JobDefinitionExtensions | ValidateSource(...) | 72 | 8 || Flink.JobBuilder | Flink.JobBuilder.Services.FlinkJobGatewayService | CreateLogger() | 72 | 8 || FlinkDotNet.JobGateway | FlinkDotNet.JobGateway.Controllers.JobsController | EnsureJobMetadata(...) | 72 | 8 || FlinkDotNet.JobGateway | FlinkDotNet.JobGateway.Services.FlinkJobManager | DiscoverFlinkEndpoint() | 72 | 8 || FlinkDotNet.JobGateway | FlinkDotNet.JobGateway.Services.FlinkJobManager | DiscoverSqlGatewayEndpoint() | 72 | 8 || FlinkDotNet.JobGateway | FlinkDotNet.JobGateway.Services.FlinkJobManager | LogOperations(...) | 72 | 8 || FlinkDotNet.JobGateway | FlinkDotNet.JobGateway.Services.FlinkJobManager | FindExistingRunnerJar() | 72 | 8 || FlinkDotNet.JobGateway | FlinkDotNet.JobGateway.Services.FlinkJobManager | FindMatchingJar(...) | 72 | 8 || FlinkDotNet.JobGateway | FlinkDotNet.JobGateway.Services.FlinkJobManager | CollectServiceFilesFromRunnerJar(...) | 72 | 8 || FlinkDotNet.JobGateway | FlinkDotNet.JobGateway.Services.FlinkJobManager | MergeConnectorJar(...) | 72 | 8 || FlinkDotNet.JobGateway | FlinkDotNet.JobGateway.Services.FlinkJobManager | MergeServiceFile(...) | 72 | 8 || FlinkDotNet.JobGateway | FlinkDotNet.JobGateway.Services.FlinkJobManager | ProcessCheckpointTimestamps(...) | 72 | 8 || Flink.JobBuilder | Flink.JobBuilder.Services.JobDefinitionValidator | ValidateSink(...) | 61 | 10 || Flink.JobBuilder | Flink.JobBuilder.Backpressure.DefaultKafkaConsumerLagMonitor | Dispose(...) | 42 | 6 || Flink.JobBuilder | Flink.JobBuilder.Backpressure.MultiTierRateLimiter | CalculateOptimalWaitTime(...) | 42 | 6 || Flink.JobBuilder | Flink.JobBuilder.Backpressure.MultiTierRateLimiter | ValidateTierHierarchy(...) | 42 | 6 || Flink.JobBuilder | Flink.JobBuilder.Backpressure.SlidingWindowRateLimiter | TryAcquireAsync(...) | 42 | 6 || Flink.JobBuilder | Flink.JobBuilder.Backpressure.SlidingWindowRateLimiter | TryAcquire(...) | 42 | 6 || Flink.JobBuilder | Flink.JobBuilder.Extensions.JobDefinitionExtensions | Validate(...) | 42 | 6 || Flink.JobBuilder | Flink.JobBuilder.Flink.FlinkKafkaConsumerGroup | RestoreState(...) | 42 | 6 || Flink.JobBuilder | Flink.JobBuilder.Flink.FlinkKafkaConsumerGroup | ValidateFlinkConfiguration() | 42 | 6 || Flink.JobBuilder | Flink.JobBuilder.Flink.FlinkRedisSink | Dispose(...) | 42 | 6 || Flink.JobBuilder | Flink.JobBuilder.Flink.FlinkRedisSink | .ctor(...) | 42 | 6 || Flink.JobBuilder | Flink.JobBuilder.FlinkJobBuilder | BuildJobDefinition() | 42 | 6 || Flink.JobBuilder | Flink.JobBuilder.Services.FlinkJobGatewayService | Dispose(...) | 42 | 6 || Flink.JobBuilder | Flink.JobBuilder.Services.JobDefinitionValidator | ValidateDatabaseSource(...) | 42 | 6 || Flink.JobBuilder | Flink.JobBuilder.Services.JobDefinitionValidator | ValidateGroupByOperation(...) | 42 | 6 || Flink.JobBuilder | Flink.JobBuilder.Services.JobDefinitionValidator | ValidateAggregateOperation(...) | 42 | 6 || Flink.JobBuilder | Flink.JobBuilder.Services.JobDefinitionValidator | ValidateJoinOperation(...) | 42 | 6 || Flink.JobBuilder | Flink.JobBuilder.Services.JobDefinitionValidator | ValidateSideOutputOperation(...) | 42 | 6 || Flink.JobBuilder | Flink.JobBuilder.Services.JobDefinitionValidator | ValidateHttpSink(...) | 42 | 6 || FlinkDotNet.JobGateway | FlinkDotNet.JobGateway.Services.FlinkJobManager | WriteMergedServiceFiles(...) | 42 | 6 || FlinkDotNet.JobGateway | FlinkDotNet.JobGateway.Services.FlinkJobManager | FindRepoRoot(...) | 42 | 6 || FlinkDotNet.JobGateway | FlinkDotNet.JobGateway.Services.FlinkJobManager | TryGetStringProperty(...) | 42 | 6 || FlinkDotNet.JobGateway | FlinkDotNet.JobGateway.Services.FlinkJobManager | ProcessCheckpointCounts(...) | 42 | 6 || Flink.JobBuilder | Flink.JobBuilder.Services.JobDefinitionValidator | ValidateSource(...) | 32 | 10 |
# Coverage

| **Name** | **Covered** | **Uncovered** | **Coverable** | **Total** | **Line coverage** | **Covered** | **Total** | **Branch coverage** |
|:---|---:|---:|---:|---:|---:|---:|---:|---:|
| **Flink.JobBuilder** | **88** | **1595** | **1683** | **30088** | **5.2%** | **59** | **669** | **8.8%** |
| Flink.JobBuilder.Backpressure.AutoScaler | 0 | 1 | 1 | 256 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.BackpressureTestRunner | 0 | 1 | 1 | 256 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.BufferedItem<T> | 0 | 1 | 1 | 313 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.BufferPool<T> | 0 | 36 | 36 | 313 | 0% | 0 | 18 | 0% |
| Flink.JobBuilder.Backpressure.ComprehensiveLoadTester | 0 | 8 | 8 | 256 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.ConsistentHashPartitionManager | 0 | 10 | 10 | 256 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.ConsumerLagMonitor | 0 | 6 | 6 | 256 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.ConsumerScenario | 0 | 4 | 4 | 256 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.ConsumerScenarioExecutor | 0 | 1 | 1 | 256 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.DashboardManager | 0 | 1 | 1 | 256 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.DefaultKafkaConsumerLagMonitor | 0 | 86 | 86 | 596 | 0% | 0 | 42 | 0% |
| Flink.JobBuilder.Backpressure.DlqManager | 0 | 2 | 2 | 256 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.FailureSimulator | 0 | 1 | 1 | 256 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.FairPartitionDistributor | 0 | 3 | 3 | 256 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.FiniteResourceManager | 0 | 6 | 6 | 256 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.InMemoryRateLimiterStateStorage | 0 | 42 | 42 | 154 | 0% | 0 | 14 | 0% |
| Flink.JobBuilder.Backpressure.KafkaConfig | 0 | 2 | 2 | 385 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.KafkaPerformanceConfig | 0 | 4 | 4 | 385 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.KafkaRateLimiterStateStorage | 0 | 78 | 78 | 385 | 0% | 0 | 16 | 0% |
| Flink.JobBuilder.Backpressure.KafkaSecurityConfig | 0 | 1 | 1 | 385 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.LagBasedRateLimiter | 0 | 94 | 94 | 596 | 0% | 0 | 42 | 0% |
| Flink.JobBuilder.Backpressure.LagBasedWaitingRequest | 0 | 1 | 1 | 596 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.LoadTestPhase | 0 | 5 | 5 | 256 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.LoadTestPhaseExecution | 0 | 5 | 5 | 256 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.LoadTestResult | 0 | 1 | 1 | 256 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.LocalJobManagerRateLimiterCoordinator | 0 | 11 | 11 | 103 | 0% | 0 | 2 | 0% |
| Flink.JobBuilder.Backpressure.ManagementActionManager | 0 | 1 | 1 | 256 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.MetricsValidator | 0 | 1 | 1 | 256 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.MonitoringManager | 0 | 1 | 1 | 256 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.MultiClusterKafkaManager | 0 | 1 | 1 | 256 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.MultiTierRateLimiter | 0 | 144 | 144 | 535 | 0% | 0 | 73 | 0% |
| Flink.JobBuilder.Backpressure.NetworkBottleneckSimulator | 0 | 1 | 1 | 256 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.NetworkBoundBackpressureController | 0 | 7 | 7 | 256 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.NoisyNeighborManager | 0 | 3 | 3 | 256 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.OperationsManager | 0 | 1 | 1 | 256 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.ProcessingCharacteristicValidator | 0 | 1 | 1 | 256 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.ProductionReadinessValidator | 0 | 1 | 1 | 256 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.RateLimiterFactory | 0 | 54 | 54 | 255 | 0% | 0 | 2 | 0% |
| Flink.JobBuilder.Backpressure.RateLimiterState | 0 | 3 | 3 | 70 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.RateLimitingContext | 0 | 1 | 1 | 535 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.RateLimitingTier | 0 | 2 | 2 | 95 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.ResourceConstrainedScenario | 0 | 2 | 2 | 256 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.SlidingWindowRateLimiter | 0 | 72 | 72 | 230 | 0% | 0 | 30 | 0% |
| Flink.JobBuilder.Backpressure.StorageBackendInfo | 0 | 3 | 3 | 70 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.TokenBucketRateLimiter | 0 | 101 | 101 | 465 | 0% | 0 | 38 | 0% |
| Flink.JobBuilder.Backpressure.TopicDesignValidator | 0 | 1 | 1 | 256 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.VariableSpeedProducer | 0 | 1 | 1 | 256 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.WaitingRequest | 0 | 1 | 1 | 465 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.WorldClassStandardValidator | 0 | 1 | 1 | 256 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Demo.RateLimitingDemo | 0 | 59 | 59 | 230 | 0% | 0 | 28 | 0% |
| Flink.JobBuilder.Extensions.FlinkJobBuilderExtensions | 0 | 3 | 3 | 231 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Extensions.JobDefinitionExtensions | 0 | 44 | 44 | 231 | 0% | 0 | 40 | 0% |
| Flink.JobBuilder.Extensions.JobValidationResult | 0 | 2 | 2 | 231 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Extensions.ServiceCollectionExtensions | 0 | 9 | 9 | 231 | 0% | 0 | 2 | 0% |
| Flink.JobBuilder.Flink.ConsumeResult | 0 | 1 | 1 | 280 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Flink.FlinkKafkaConsumerGroup | 0 | 65 | 65 | 280 | 0% | 0 | 20 | 0% |
| Flink.JobBuilder.Flink.FlinkRedisSink | 0 | 48 | 48 | 373 | 0% | 0 | 30 | 0% |
| Flink.JobBuilder.Flink.RedisOperation | 0 | 1 | 1 | 373 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Flink.RedisTransactionResult | 0 | 1 | 1 | 373 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Flink.TopicPartition | 0 | 2 | 2 | 280 | 0% | 0 | 0 |  |
| Flink.JobBuilder.FlinkJobBuilder | 0 | 180 | 180 | 566 | 0% | 0 | 22 | 0% |
| Flink.JobBuilder.Models.AggregateOperationDefinition | 0 | 3 | 3 | 410 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Models.AsyncFunctionOperationDefinition | 0 | 8 | 8 | 410 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Models.ConsoleSinkDefinition | 0 | 2 | 2 | 410 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Models.DatabaseSinkDefinition | 0 | 5 | 5 | 410 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Models.DatabaseSourceDefinition | 0 | 6 | 6 | 410 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Models.FileSinkDefinition | 0 | 4 | 4 | 410 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Models.FileSourceDefinition | 0 | 4 | 4 | 410 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Models.FilterOperationDefinition | 1 | 1 | 2 | 410 | 50% | 0 | 0 |  |
| Flink.JobBuilder.Models.FlinkJobGatewayConfiguration | 0 | 4 | 4 | 113 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Models.GroupByOperationDefinition | 0 | 2 | 2 | 410 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Models.HttpSinkDefinition | 0 | 6 | 6 | 410 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Models.HttpSourceDefinition | 0 | 6 | 6 | 410 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Models.JobDefinition | 2 | 0 | 2 | 410 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Models.JobExecutionResult | 0 | 3 | 3 | 113 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Models.JobMetadata | 3 | 0 | 3 | 410 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Models.JobMetrics | 0 | 2 | 2 | 113 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Models.JobStatus | 0 | 4 | 4 | 113 | 0% | 0 | 4 | 0% |
| Flink.JobBuilder.Models.JobSubmissionResult | 0 | 18 | 18 | 113 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Models.JoinOperationDefinition | 0 | 4 | 4 | 410 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Models.KafkaSinkDefinition | 3 | 1 | 4 | 410 | 75% | 0 | 0 |  |
| Flink.JobBuilder.Models.KafkaSourceDefinition | 3 | 1 | 4 | 410 | 75% | 0 | 0 |  |
| Flink.JobBuilder.Models.MapOperationDefinition | 1 | 1 | 2 | 410 | 50% | 0 | 0 |  |
| Flink.JobBuilder.Models.ProcessFunctionOperationDefinition | 0 | 6 | 6 | 410 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Models.RedisSinkDefinition | 0 | 4 | 4 | 410 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Models.RetryOperationDefinition | 0 | 5 | 5 | 410 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Models.SideOutputOperationDefinition | 0 | 4 | 4 | 410 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Models.SqlSourceDefinition | 4 | 1 | 5 | 410 | 80% | 0 | 0 |  |
| Flink.JobBuilder.Models.StateOperationDefinition | 0 | 5 | 5 | 410 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Models.TimerOperationDefinition | 2 | 1 | 3 | 410 | 66.6% | 0 | 0 |  |
| Flink.JobBuilder.Models.WindowOperationDefinition | 2 | 1 | 3 | 410 | 66.6% | 0 | 0 |  |
| Flink.JobBuilder.Services.FlinkJobGatewayService | 0 | 110 | 110 | 485 | 0% | 0 | 46 | 0% |
| Flink.JobBuilder.Services.IrValidationResult | 2 | 0 | 2 | 377 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Services.JobDefinitionValidator | 65 | 143 | 208 | 377 | 31.2% | 59 | 200 | 29.5% |
| **FlinkDotNet.JobGateway** | **0** | **424** | **424** | **2108** | **0%** | **0** | **240** | **0%** |
| FlinkDotNet.JobGateway.Controllers.JobsController | 0 | 33 | 33 | 269 | 0% | 0 | 12 | 0% |
| FlinkDotNet.JobGateway.ModelStateLoggingFilter | 0 | 10 | 10 | 220 | 0% | 0 | 2 | 0% |
| FlinkDotNet.JobGateway.Services.FlinkJobManager | 0 | 381 | 381 | 1619 | 0% | 0 | 226 | 0% |

