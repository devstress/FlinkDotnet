# Summary

|||
|:---|:---|
| Generated on: | 10/14/2025 - 18:21:11 |
| Coverage date: | 10/14/2025 - 18:19:55 - 10/14/2025 - 18:21:00 |
| Parser: | MultiReport (5x Cobertura) |
| Assemblies: | 6 |
| Classes: | 135 |
| Files: | 38 |
| **Line coverage:** | 80.2% (2369 of 2953) |
| Covered lines: | 2369 |
| Uncovered lines: | 584 |
| Coverable lines: | 2953 |
| Total lines: | 14032 |
| **Branch coverage:** | 68.6% (830 of 1209) |
| Covered branches: | 830 |
| Total branches: | 1209 |
| **Method coverage:** | [Feature is only available for sponsors](https://reportgenerator.io/pro) |

# Risk Hotspots

| **Assembly** | **Class** | **Method** | **Crap Score** | **Cyclomatic complexity** |
|:---|:---|:---|---:|---:|
| Flink.JobBuilder | Flink.JobBuilder.Backpressure.DefaultKafkaConsumerLagMonitor | ComputeLagForGroup(...) | 403 | 22 || Flink.JobBuilder | Flink.JobBuilder.Demo.RateLimitingDemo | DemonstrateMultiTierRateLimiting() | 342 | 18 || FlinkDotNet.JobGateway | FlinkDotNet.JobGateway.Services.FlinkJobManager | CollectConnectorJars() | 342 | 18 || FlinkDotNet.JobGateway | FlinkDotNet.JobGateway.Services.FlinkJobManager | TryGetJobIdFromHeaders(...) | 272 | 16 || FlinkDotNet.JobGateway | FlinkDotNet.JobGateway.Services.FlinkJobManager | ExtractJobIdFromOverviewElement(...) | 210 | 14 || FlinkDotNet.JobGateway | FlinkDotNet.JobGateway.Services.FlinkJobManager | MatchJobEntry(...) | 210 | 14 || Flink.JobBuilder | Flink.JobBuilder.Demo.RateLimitingDemo | DemonstrateTokenBucketRateLimiter() | 110 | 10 || Flink.JobBuilder | Flink.JobBuilder.Flink.FlinkRedisSink | AddOperationsToTransaction(...) | 110 | 10 || Flink.JobBuilder | Flink.JobBuilder.Backpressure.SlidingWindowRateLimiter | CalculateWaitTime(...) | 72 | 8 || FlinkDotNet.JobGateway | FlinkDotNet.JobGateway.Services.FlinkJobManager | FindMatchingJar(...) | 72 | 8 || FlinkDotNet.JobGateway | FlinkDotNet.JobGateway.Services.FlinkJobManager | CollectServiceFilesFromRunnerJar(...) | 72 | 8 || FlinkDotNet.JobGateway | FlinkDotNet.JobGateway.Services.FlinkJobManager | MergeConnectorJar(...) | 72 | 8 || FlinkDotNet.JobGateway | FlinkDotNet.JobGateway.Services.FlinkJobManager | MergeServiceFile(...) | 72 | 8 || FlinkDotNet.JobGateway | FlinkDotNet.JobGateway.Services.FlinkJobManager | ProcessCheckpointTimestamps(...) | 72 | 8 || Flink.JobBuilder | Flink.JobBuilder.Backpressure.MultiTierRateLimiter | IsApplicableTier(...) | 43 | 12 || Flink.JobBuilder | Flink.JobBuilder.Backpressure.MultiTierRateLimiter | CalculateOptimalWaitTime(...) | 42 | 6 || FlinkDotNet.JobGateway | FlinkDotNet.JobGateway.Services.FlinkJobManager | FindExistingRunnerJar() | 42 | 6 || FlinkDotNet.JobGateway | FlinkDotNet.JobGateway.Services.FlinkJobManager | WriteMergedServiceFiles(...) | 42 | 6 || FlinkDotNet.JobGateway | FlinkDotNet.JobGateway.Services.FlinkJobManager | FindRepoRoot(...) | 42 | 6 || FlinkDotNet.JobGateway | FlinkDotNet.JobGateway.Services.FlinkJobManager | TryGetStringProperty(...) | 42 | 6 || FlinkDotNet.JobGateway | FlinkDotNet.JobGateway.Services.FlinkJobManager | ProcessCheckpointCounts(...) | 42 | 6 || Flink.JobBuilder | Flink.JobBuilder.Services.JobDefinitionValidator | ValidateOperation(...) | 24 | 24 || Flink.JobBuilder | Flink.JobBuilder.Backpressure.TokenBucketRateLimiter | Dispose(...) | 23 | 16 || FlinkDotNet.DataStream | FlinkDotNet.DataStream.OperationCapture | TranslateMapOperation(...) | 16 | 16 |
# Coverage

| **Name** | **Covered** | **Uncovered** | **Coverable** | **Total** | **Line coverage** | **Covered** | **Total** | **Branch coverage** |
|:---|---:|---:|---:|---:|---:|---:|---:|---:|
| **Flink.JobBuilder** | **1461** | **299** | **1760** | **35591** | **83%** | **519** | **681** | **76.2%** |
| Flink.JobBuilder.Backpressure.AutoScaler | 1 | 0 | 1 | 316 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.BackpressureTestRunner | 1 | 0 | 1 | 316 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.BufferedItem<T> | 1 | 0 | 1 | 353 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.BufferPool<T> | 37 | 2 | 39 | 353 | 94.8% | 16 | 18 | 88.8% |
| Flink.JobBuilder.Backpressure.ComprehensiveLoadTester | 8 | 0 | 8 | 316 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.ConsistentHashPartitionManager | 10 | 0 | 10 | 316 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.ConsumerLagMonitor | 6 | 0 | 6 | 316 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.ConsumerScenario | 4 | 0 | 4 | 316 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.ConsumerScenarioExecutor | 1 | 0 | 1 | 316 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.DashboardManager | 1 | 0 | 1 | 316 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.DefaultKafkaClientFactory | 2 | 0 | 2 | 46 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.DefaultKafkaConsumerLagMonitor | 37 | 56 | 93 | 625 | 39.7% | 16 | 42 | 38% |
| Flink.JobBuilder.Backpressure.DlqManager | 2 | 0 | 2 | 316 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.FailureSimulator | 1 | 0 | 1 | 316 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.FairPartitionDistributor | 7 | 0 | 7 | 316 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.FiniteResourceManager | 6 | 0 | 6 | 316 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.InMemoryRateLimiterStateStorage | 46 | 0 | 46 | 158 | 100% | 14 | 14 | 100% |
| Flink.JobBuilder.Backpressure.KafkaConfig | 2 | 0 | 2 | 410 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.KafkaPerformanceConfig | 4 | 0 | 4 | 410 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.KafkaRateLimiterStateStorage | 63 | 15 | 78 | 410 | 80.7% | 11 | 18 | 61.1% |
| Flink.JobBuilder.Backpressure.KafkaSecurityConfig | 1 | 0 | 1 | 410 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.LagBasedRateLimiter | 81 | 20 | 101 | 625 | 80.1% | 28 | 42 | 66.6% |
| Flink.JobBuilder.Backpressure.LagBasedWaitingRequest | 0 | 1 | 1 | 625 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.LoadTestPhase | 5 | 0 | 5 | 316 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.LoadTestPhaseExecution | 5 | 0 | 5 | 316 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.LoadTestResult | 1 | 0 | 1 | 316 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.LocalJobManagerRateLimiterCoordinator | 12 | 0 | 12 | 104 | 100% | 2 | 2 | 100% |
| Flink.JobBuilder.Backpressure.ManagementActionManager | 1 | 0 | 1 | 316 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.MetricsValidator | 1 | 0 | 1 | 316 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.MonitoringManager | 1 | 0 | 1 | 316 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.MultiClusterKafkaManager | 1 | 0 | 1 | 316 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.MultiTierRateLimiter | 82 | 68 | 150 | 553 | 54.6% | 35 | 73 | 47.9% |
| Flink.JobBuilder.Backpressure.NetworkBottleneckSimulator | 1 | 0 | 1 | 316 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.NetworkBoundBackpressureController | 7 | 0 | 7 | 316 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.NoisyNeighborManager | 8 | 0 | 8 | 316 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.OperationsManager | 1 | 0 | 1 | 316 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.ProcessingCharacteristicValidator | 1 | 0 | 1 | 316 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.ProductionReadinessValidator | 1 | 0 | 1 | 316 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.RateLimiterFactory | 50 | 4 | 54 | 255 | 92.5% | 1 | 2 | 50% |
| Flink.JobBuilder.Backpressure.RateLimiterState | 3 | 0 | 3 | 103 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.RateLimitingContext | 1 | 0 | 1 | 553 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.RateLimitingTier | 2 | 0 | 2 | 113 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.ResourceConstrainedScenario | 2 | 0 | 2 | 316 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.SlidingWindowRateLimiter | 57 | 18 | 75 | 234 | 76% | 18 | 30 | 60% |
| Flink.JobBuilder.Backpressure.StorageBackendInfo | 3 | 0 | 3 | 103 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.TokenBucketRateLimiter | 92 | 15 | 107 | 483 | 85.9% | 31 | 38 | 81.5% |
| Flink.JobBuilder.Backpressure.TopicDesignValidator | 1 | 0 | 1 | 316 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.VariableSpeedProducer | 0 | 1 | 1 | 316 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.WaitingRequest | 1 | 0 | 1 | 483 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Backpressure.WorldClassStandardValidator | 0 | 1 | 1 | 316 | 0% | 0 | 0 |  |
| Flink.JobBuilder.Demo.RateLimitingDemo | 0 | 59 | 59 | 228 | 0% | 0 | 28 | 0% |
| Flink.JobBuilder.Extensions.FlinkJobBuilderExtensions | 3 | 0 | 3 | 234 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Extensions.JobDefinitionExtensions | 44 | 0 | 44 | 234 | 100% | 40 | 40 | 100% |
| Flink.JobBuilder.Extensions.JobValidationResult | 2 | 0 | 2 | 234 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Extensions.ServiceCollectionExtensions | 9 | 0 | 9 | 234 | 100% | 2 | 2 | 100% |
| Flink.JobBuilder.Flink.ConsumeResult | 1 | 0 | 1 | 298 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Flink.FlinkKafkaConsumerGroup | 65 | 0 | 65 | 298 | 100% | 20 | 20 | 100% |
| Flink.JobBuilder.Flink.FlinkRedisSink | 48 | 30 | 78 | 432 | 61.5% | 30 | 40 | 75% |
| Flink.JobBuilder.Flink.RedisOperation | 1 | 0 | 1 | 432 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Flink.RedisTransactionResult | 1 | 0 | 1 | 432 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Flink.TopicPartition | 2 | 0 | 2 | 298 | 100% | 0 | 0 |  |
| Flink.JobBuilder.FlinkJobBuilder | 180 | 0 | 180 | 566 | 100% | 22 | 22 | 100% |
| Flink.JobBuilder.Models.AggregateOperationDefinition | 3 | 0 | 3 | 518 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Models.AsyncFunctionOperationDefinition | 8 | 0 | 8 | 518 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Models.ConsoleSinkDefinition | 2 | 0 | 2 | 518 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Models.DatabaseSinkDefinition | 5 | 0 | 5 | 518 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Models.DatabaseSourceDefinition | 6 | 0 | 6 | 518 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Models.FileSinkDefinition | 4 | 0 | 4 | 518 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Models.FileSourceDefinition | 4 | 0 | 4 | 518 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Models.FilterOperationDefinition | 2 | 0 | 2 | 518 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Models.FlinkJobGatewayConfiguration | 4 | 0 | 4 | 182 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Models.GroupByOperationDefinition | 2 | 0 | 2 | 518 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Models.HttpSinkDefinition | 6 | 0 | 6 | 518 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Models.HttpSourceDefinition | 6 | 0 | 6 | 518 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Models.JobDefinition | 2 | 0 | 2 | 518 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Models.JobExecutionResult | 3 | 0 | 3 | 182 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Models.JobMetadata | 3 | 0 | 3 | 518 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Models.JobMetrics | 2 | 0 | 2 | 182 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Models.JobStatus | 4 | 0 | 4 | 182 | 100% | 4 | 4 | 100% |
| Flink.JobBuilder.Models.JobSubmissionResult | 18 | 0 | 18 | 182 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Models.JoinOperationDefinition | 4 | 0 | 4 | 518 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Models.KafkaSinkDefinition | 4 | 0 | 4 | 518 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Models.KafkaSourceDefinition | 4 | 0 | 4 | 518 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Models.MapOperationDefinition | 2 | 0 | 2 | 518 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Models.ProcessFunctionOperationDefinition | 6 | 0 | 6 | 518 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Models.RedisSinkDefinition | 4 | 0 | 4 | 518 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Models.RetryOperationDefinition | 5 | 0 | 5 | 518 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Models.SideOutputOperationDefinition | 4 | 0 | 4 | 518 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Models.SqlSourceDefinition | 5 | 0 | 5 | 518 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Models.StateOperationDefinition | 5 | 0 | 5 | 518 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Models.TimerOperationDefinition | 3 | 0 | 3 | 518 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Models.WindowOperationDefinition | 3 | 0 | 3 | 518 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Services.FlinkJobGatewayService | 103 | 6 | 109 | 484 | 94.4% | 37 | 46 | 80.4% |
| Flink.JobBuilder.Services.IrValidationResult | 2 | 0 | 2 | 377 | 100% | 0 | 0 |  |
| Flink.JobBuilder.Services.JobDefinitionValidator | 205 | 3 | 208 | 377 | 98.5% | 192 | 200 | 96% |
| **FlinkDotNet.ClusterManager** | **58** | **0** | **58** | **449** | **100%** | **15** | **16** | **93.7%** |
| FlinkDotNet.ClusterManager.Actors.FlinkClusterActor | 58 | 0 | 58 | 449 | 100% | 15 | 16 | 93.7% |
| **FlinkDotNet.Common** | **82** | **0** | **82** | **491** | **100%** | **30** | **32** | **93.7%** |
| FlinkDotNet.Common.Configuration | 47 | 0 | 47 | 240 | 100% | 30 | 32 | 93.7% |
| FlinkDotNet.Common.ExecutionConfig | 35 | 0 | 35 | 251 | 100% | 0 | 0 |  |
| **FlinkDotNet.DataStream** | **590** | **11** | **601** | **17098** | **98.1%** | **200** | **236** | **84.7%** |
| FlinkDotNet.DataStream.AggregatedSourceFunction<T1, T2, T3> | 4 | 0 | 4 | 931 | 100% | 2 | 4 | 50% |
| FlinkDotNet.DataStream.AggregatingStateDescriptor<T1, T2, T3> | 3 | 0 | 3 | 344 | 100% | 2 | 2 | 100% |
| FlinkDotNet.DataStream.AllWindowedStream<T> | 27 | 0 | 27 | 931 | 100% | 8 | 12 | 66.6% |
| FlinkDotNet.DataStream.CapturedOperation | 1 | 0 | 1 | 416 | 100% | 0 | 0 |  |
| FlinkDotNet.DataStream.DataStream<T> | 119 | 6 | 125 | 931 | 95.2% | 70 | 82 | 85.3% |
| FlinkDotNet.DataStream.DataStreamExtensions | 1 | 0 | 1 | 288 | 100% | 0 | 0 |  |
| FlinkDotNet.DataStream.FilteredSourceFunction<T> | 4 | 0 | 4 | 931 | 100% | 4 | 4 | 100% |
| FlinkDotNet.DataStream.FlatMappedSourceFunction<T1, T2> | 4 | 0 | 4 | 931 | 100% | 4 | 4 | 100% |
| FlinkDotNet.DataStream.IAsyncFunction<T1, T2> | 2 | 0 | 2 | 406 | 100% | 0 | 0 |  |
| FlinkDotNet.DataStream.JobClient | 16 | 0 | 16 | 770 | 100% | 7 | 8 | 87.5% |
| FlinkDotNet.DataStream.JobExecutionResult | 2 | 0 | 2 | 770 | 100% | 0 | 0 |  |
| FlinkDotNet.DataStream.JobStatus | 3 | 0 | 3 | 770 | 100% | 0 | 0 |  |
| FlinkDotNet.DataStream.KafkaSinkFunction<T> | 8 | 0 | 8 | 288 | 100% | 0 | 0 |  |
| FlinkDotNet.DataStream.KafkaSourceFunction<T> | 16 | 0 | 16 | 79 | 100% | 4 | 8 | 50% |
| FlinkDotNet.DataStream.KafkaSourceFunctionExtensions | 3 | 0 | 3 | 288 | 100% | 0 | 0 |  |
| FlinkDotNet.DataStream.KeyedStream<T1, T2> | 7 | 0 | 7 | 931 | 100% | 0 | 0 |  |
| FlinkDotNet.DataStream.ListStateDescriptor<T> | 3 | 0 | 3 | 344 | 100% | 0 | 0 |  |
| FlinkDotNet.DataStream.MappedSourceFunction<T1, T2> | 4 | 0 | 4 | 931 | 100% | 4 | 4 | 100% |
| FlinkDotNet.DataStream.MapStateDescriptor<T1, T2> | 4 | 0 | 4 | 344 | 100% | 0 | 0 |  |
| FlinkDotNet.DataStream.OperationCapture | 200 | 3 | 203 | 416 | 98.5% | 65 | 76 | 85.5% |
| FlinkDotNet.DataStream.OutputTag<T> | 7 | 0 | 7 | 406 | 100% | 4 | 4 | 100% |
| FlinkDotNet.DataStream.ReducingStateDescriptor<T> | 3 | 0 | 3 | 344 | 100% | 2 | 2 | 100% |
| FlinkDotNet.DataStream.SavepointResult | 2 | 0 | 2 | 770 | 100% | 0 | 0 |  |
| FlinkDotNet.DataStream.StateDescriptor | 3 | 0 | 3 | 344 | 100% | 2 | 2 | 100% |
| FlinkDotNet.DataStream.StopWithSavepointResult | 2 | 0 | 2 | 770 | 100% | 0 | 0 |  |
| FlinkDotNet.DataStream.StreamExecutionEnvironment | 108 | 2 | 110 | 770 | 98.1% | 22 | 24 | 91.6% |
| FlinkDotNet.DataStream.StreamExecutionEnvironmentExtensions | 3 | 0 | 3 | 288 | 100% | 0 | 0 |  |
| FlinkDotNet.DataStream.Time | 15 | 0 | 15 | 159 | 100% | 0 | 0 |  |
| FlinkDotNet.DataStream.TypeInformation<T> | 6 | 0 | 6 | 288 | 100% | 0 | 0 |  |
| FlinkDotNet.DataStream.ValueStateDescriptor<T> | 3 | 0 | 3 | 344 | 100% | 0 | 0 |  |
| FlinkDotNet.DataStream.Watermark | 5 | 0 | 5 | 159 | 100% | 0 | 0 |  |
| FlinkDotNet.DataStream.WindowDefinition | 2 | 0 | 2 | 416 | 100% | 0 | 0 |  |
| **FlinkDotNet.JobGateway** | **170** | **274** | **444** | **2166** | **38.2%** | **62** | **240** | **25.8%** |
| FlinkDotNet.JobGateway.Controllers.JobsController | 40 | 0 | 40 | 283 | 100% | 10 | 12 | 83.3% |
| FlinkDotNet.JobGateway.ModelStateLoggingFilter | 10 | 0 | 10 | 222 | 100% | 2 | 2 | 100% |
| FlinkDotNet.JobGateway.Services.FlinkJobManager | 120 | 274 | 394 | 1661 | 30.4% | 50 | 226 | 22.1% |
| **FlinkDotNet.Orchestration** | **8** | **0** | **8** | **461** | **100%** | **4** | **4** | **100%** |
| FlinkDotNet.Orchestration.Services.ClusterActorBridge | 4 | 0 | 4 | 115 | 100% | 2 | 2 | 100% |
| FlinkDotNet.Orchestration.Services.FlinkOrchestra | 4 | 0 | 4 | 346 | 100% | 2 | 2 | 100% |

