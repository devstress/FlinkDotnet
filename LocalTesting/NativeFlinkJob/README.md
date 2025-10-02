# Native Flink Kafka Job - Infrastructure Validation

This is a standalone Apache Flink job using the official Flink Kafka connector to validate that the Aspire LocalTesting infrastructure is correctly configured.

## Purpose

Before debugging Gateway/IR issues, we need to prove the infrastructure works with a standard Flink job:
- ✅ Aspire DCP correctly configures Flink cluster
- ✅ Kafka is accessible from Flink containers at `kafka:9093`
- ✅ Messages flow through: Kafka Input → Flink Transform → Kafka Output

## Build

```bash
cd LocalTesting/NativeFlinkJob
mvn clean package
```

This creates: `target/native-flink-kafka-job-1.0.0.jar`

## Run via Flink REST API

```bash
# Upload JAR
curl -X POST -H "Expect:" -F "jarfile=@target/native-flink-kafka-job-1.0.0.jar" \
  http://localhost:8081/jars/upload

# Submit job (replace {jarId} with the ID from upload response)
curl -X POST http://localhost:8081/jars/{jarId}/run \
  -H "Content-Type: application/json" \
  -d '{
    "entryClass": "com.flinkdotnet.NativeKafkaJob",
    "programArgsList": [
      "--bootstrap-servers", "kafka:9093",
      "--input-topic", "lt.native.input",
      "--output-topic", "lt.native.output",
      "--group-id", "native-test-consumer"
    ],
    "parallelism": 1
  }'
```

## Test with C#

The `FlinkNativeKafkaInfrastructureTest.cs` integration test:
1. Starts Aspire infrastructure (Kafka + Flink)
2. Builds and submits this native JAR
3. Produces test messages
4. Verifies messages are transformed and consumed

If this test **PASSES**: Infrastructure is correct, debug Gateway
If this test **FAILS**: Fix infrastructure first

## Configuration

Default values (for LocalTesting environment):
- **Bootstrap Servers**: `kafka:9093` (Aspire DCP internal listener)
- **Input Topic**: `lt.native.input`
- **Output Topic**: `lt.native.output`
- **Group ID**: `native-flink-consumer`

Override with command-line args:
```bash
--bootstrap-servers kafka:9093
--input-topic my-input
--output-topic my-output
--group-id my-consumer-group
```

## Key Differences from FlinkJobRunner

1. **Uses official Flink Kafka Connector** (`flink-connector-kafka`) not raw Kafka clients
2. **Proper dependency management** - connector packaged in fat JAR
3. **Standard Flink APIs** - `KafkaSource` and `KafkaSink` builders
4. **No IR/JSON** - direct Java code, no intermediate representation

## Troubleshooting

**Build fails with missing dependencies**: 
- Ensure Maven can reach Maven Central
- Check Flink version compatibility (2.1.0)

**Job fails to start**:
- Check Flink JobManager logs: `docker logs flink-jobmanager`
- Verify bootstrap servers are accessible from Flink container

**No messages consumed**:
- Check Kafka topics exist
- Verify bootstrap servers (`kafka:9093` for containers, `localhost:{port}` for host)
- Check Flink job is in RUNNING state
- Look for exceptions in TaskManager logs

## Next Steps After Validation

Once this job works:
1. Compare its Kafka configuration with Gateway's IR-generated config
2. Identify what Gateway does differently
3. Fix Gateway to match working configuration