package com.flinkdotnet.irrunner;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.fasterxml.jackson.datatype.jsr310.JavaTimeModule;
import org.apache.flink.api.common.eventtime.WatermarkStrategy;
import org.apache.flink.api.common.functions.FilterFunction;
import org.apache.flink.api.common.functions.MapFunction;
import org.apache.flink.api.common.serialization.SimpleStringSchema;
import org.apache.flink.connector.kafka.sink.KafkaRecordSerializationSchema;
import org.apache.flink.connector.kafka.sink.KafkaSink;
import org.apache.flink.connector.kafka.source.KafkaSource;
import org.apache.flink.connector.kafka.source.enumerator.initializer.OffsetsInitializer;
import org.apache.flink.streaming.api.datastream.DataStream;
import org.apache.flink.streaming.api.environment.StreamExecutionEnvironment;
import org.apache.flink.streaming.api.windowing.assigners.TumblingProcessingTimeWindows;
import org.apache.flink.streaming.api.windowing.time.Time;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.io.File;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.util.Base64;
import java.util.Properties;

/**
 * Main entry point for FlinkDotNet IR Runner.
 * Accepts IR (Intermediate Representation) from .NET SDK and builds DataStream topology.
 * 
 * Usage:
 *   java -jar flink-ir-runner.jar --ir-file path/to/ir.json
 *   java -jar flink-ir-runner.jar --ir-base64 <base64-encoded-ir>
 */
public class FlinkIRRunner {
    
    private static final Logger LOG = LoggerFactory.getLogger(FlinkIRRunner.class);
    private static final ObjectMapper objectMapper = new ObjectMapper();
    
    static {
        objectMapper.registerModule(new JavaTimeModule());
    }
    
    public static void main(String[] args) throws Exception {
        LOG.info("FlinkDotNet IR Runner v1.0.0 - Starting execution");
        
        if (args.length == 0) {
            printUsage();
            System.exit(1);
        }
        
        String irJson = null;
        
        // Parse command line arguments
        for (int i = 0; i < args.length; i++) {
            switch (args[i]) {
                case "--ir-file":
                    if (i + 1 < args.length) {
                        String filePath = args[i + 1];
                        LOG.info("Loading IR from file: {}", filePath);
                        irJson = Files.readString(Paths.get(filePath));
                        i++; // Skip next argument
                    } else {
                        LOG.error("--ir-file requires a file path argument");
                        System.exit(1);
                    }
                    break;
                case "--ir-base64":
                    if (i + 1 < args.length) {
                        String base64Ir = args[i + 1];
                        LOG.info("Loading IR from base64 argument");
                        irJson = new String(Base64.getDecoder().decode(base64Ir));
                        i++; // Skip next argument
                    } else {
                        LOG.error("--ir-base64 requires a base64 string argument");
                        System.exit(1);
                    }
                    break;
                case "--help":
                case "-h":
                    printUsage();
                    System.exit(0);
                    break;
                default:
                    LOG.warn("Unknown argument: {}", args[i]);
                    break;
            }
        }
        
        if (irJson == null) {
            LOG.error("No IR provided. Use --ir-file or --ir-base64");
            printUsage();
            System.exit(1);
        }
        
        try {
            // Parse IR JSON
            JsonNode irNode = objectMapper.readTree(irJson);
            LOG.info("Successfully parsed IR with {} operations", 
                irNode.has("operations") ? irNode.get("operations").size() : 0);
            
            // Build and execute Flink job
            buildAndExecuteJob(irNode);
            
        } catch (Exception e) {
            LOG.error("Failed to execute IR: {}", e.getMessage(), e);
            System.exit(1);
        }
    }
    
    private static void buildAndExecuteJob(JsonNode ir) throws Exception {
        // Create Flink execution environment
        StreamExecutionEnvironment env = StreamExecutionEnvironment.getExecutionEnvironment();
        
        // Extract job metadata
        JsonNode metadata = ir.get("metadata");
        String jobName = metadata != null && metadata.has("jobName") ? 
            metadata.get("jobName").asText() : "FlinkDotNet-Job";
        
        if (metadata != null && metadata.has("parallelism")) {
            int parallelism = metadata.get("parallelism").asInt();
            env.setParallelism(parallelism);
            LOG.info("Set job parallelism to: {}", parallelism);
        }
        
        // Build data stream from IR
        DataStream<String> stream = buildSource(env, ir.get("source"));
        
        // Apply operations
        if (ir.has("operations")) {
            for (JsonNode operation : ir.get("operations")) {
                stream = applyOperation(stream, operation);
            }
        }
        
        // Add sink
        addSink(stream, ir.get("sink"));
        
        // Execute job
        LOG.info("Executing Flink job: {}", jobName);
        env.execute(jobName);
    }
    
    private static DataStream<String> buildSource(StreamExecutionEnvironment env, JsonNode source) {
        String sourceType = source.get("type").asText();
        
        switch (sourceType.toLowerCase()) {
            case "kafka":
                return buildKafkaSource(env, source);
            case "file":
                return buildFileSource(env, source);
            case "http":
                LOG.warn("HTTP source not yet implemented, using mock data");
                return env.fromElements("mock-http-data");
            case "database":
                LOG.warn("Database source not yet implemented, using mock data");
                return env.fromElements("mock-db-data");
            default:
                LOG.warn("Unknown source type: {}, using mock data", sourceType);
                return env.fromElements("mock-data");
        }
    }
    
    private static DataStream<String> buildKafkaSource(StreamExecutionEnvironment env, JsonNode source) {
        String topic = source.get("topic").asText();
        String bootstrapServers = source.has("bootstrapServers") ? 
            source.get("bootstrapServers").asText() : "localhost:9092";
        String groupId = source.has("groupId") ? 
            source.get("groupId").asText() : "flink-ir-runner";
        String startingOffsets = source.has("startingOffsets") ? 
            source.get("startingOffsets").asText() : "latest";
        
        LOG.info("Building Kafka source - Topic: {}, Bootstrap: {}, Group: {}, Offsets: {}", 
            topic, bootstrapServers, groupId, startingOffsets);
        
        OffsetsInitializer offsetsInitializer = "earliest".equals(startingOffsets) ? 
            OffsetsInitializer.earliest() : OffsetsInitializer.latest();
        
        KafkaSource<String> kafkaSource = KafkaSource.<String>builder()
            .setBootstrapServers(bootstrapServers)
            .setTopics(topic)
            .setGroupId(groupId)
            .setStartingOffsets(offsetsInitializer)
            .setValueOnlyDeserializer(new SimpleStringSchema())
            .build();
        
        return env.fromSource(kafkaSource, WatermarkStrategy.noWatermarks(), "Kafka Source");
    }
    
    private static DataStream<String> buildFileSource(StreamExecutionEnvironment env, JsonNode source) {
        String path = source.get("path").asText();
        LOG.info("Building file source - Path: {}", path);
        
        // Use readTextFile for simple text files
        return env.readTextFile(path);
    }
    
    private static DataStream<String> applyOperation(DataStream<String> stream, JsonNode operation) {
        String operationType = operation.get("type").asText();
        
        switch (operationType.toLowerCase()) {
            case "filter":
                return applyFilter(stream, operation);
            case "map":
                return applyMap(stream, operation);
            case "window":
                return applyWindow(stream, operation);
            case "timer":
                LOG.warn("Timer operation not yet implemented");
                return stream;
            case "groupby":
                LOG.info("GroupBy operation noted (affects windowing)");
                return stream;
            case "aggregate":
                LOG.warn("Aggregate operation not yet implemented");
                return stream;
            default:
                LOG.warn("Unknown operation type: {}", operationType);
                return stream;
        }
    }
    
    private static DataStream<String> applyFilter(DataStream<String> stream, JsonNode operation) {
        String expression = operation.get("expression").asText();
        LOG.info("Applying filter operation with expression: {}", expression);
        
        // Simple filter implementation - in production this would be more sophisticated
        return stream.filter(new FilterFunction<String>() {
            @Override
            public boolean filter(String value) {
                // Simple contains-based filtering for demonstration
                // In production, this would parse and evaluate the expression properly
                return value.contains("test") || value.length() > 5;
            }
        });
    }
    
    private static DataStream<String> applyMap(DataStream<String> stream, JsonNode operation) {
        String expression = operation.get("expression").asText();
        LOG.info("Applying map operation with expression: {}", expression);
        
        // Simple map implementation - in production this would be more sophisticated
        return stream.map(new MapFunction<String, String>() {
            @Override
            public String map(String value) {
                // Simple transformation for demonstration
                // In production, this would parse and evaluate the expression properly
                return value.toUpperCase() + "_MAPPED";
            }
        });
    }
    
    private static DataStream<String> applyWindow(DataStream<String> stream, JsonNode operation) {
        String windowType = operation.get("windowType").asText();
        int size = operation.get("size").asInt();
        String timeUnit = operation.has("timeUnit") ? operation.get("timeUnit").asText() : "MINUTES";
        
        LOG.info("Applying {} window - Size: {} {}", windowType, size, timeUnit);
        
        Time windowSize;
        switch (timeUnit.toUpperCase()) {
            case "SECONDS":
                windowSize = Time.seconds(size);
                break;
            case "MINUTES":
                windowSize = Time.minutes(size);
                break;
            case "HOURS":
                windowSize = Time.hours(size);
                break;
            default:
                LOG.warn("Unknown time unit: {}, defaulting to minutes", timeUnit);
                windowSize = Time.minutes(size);
                break;
        }
        
        // Apply tumbling window (basic implementation)
        // Note: This assumes the stream has been keyed previously
        try {
            return stream.keyBy(value -> value.hashCode() % 10)
                .window(TumblingProcessingTimeWindows.of(windowSize))
                .reduce((value1, value2) -> value1 + "," + value2);
        } catch (Exception e) {
            LOG.warn("Window operation failed, returning original stream: {}", e.getMessage());
            return stream;
        }
    }
    
    private static void addSink(DataStream<String> stream, JsonNode sink) {
        String sinkType = sink.get("type").asText();
        
        switch (sinkType.toLowerCase()) {
            case "kafka":
                addKafkaSink(stream, sink);
                break;
            case "console":
                addConsoleSink(stream, sink);
                break;
            case "file":
                addFileSink(stream, sink);
                break;
            case "database":
                LOG.warn("Database sink not yet implemented, using console sink");
                addConsoleSink(stream, sink);
                break;
            case "http":
                LOG.warn("HTTP sink not yet implemented, using console sink");
                addConsoleSink(stream, sink);
                break;
            case "redis":
                LOG.warn("Redis sink not yet implemented, using console sink");
                addConsoleSink(stream, sink);
                break;
            default:
                LOG.warn("Unknown sink type: {}, using console sink", sinkType);
                addConsoleSink(stream, sink);
                break;
        }
    }
    
    private static void addKafkaSink(DataStream<String> stream, JsonNode sink) {
        String topic = sink.get("topic").asText();
        String bootstrapServers = sink.has("bootstrapServers") ? 
            sink.get("bootstrapServers").asText() : "localhost:9092";
        
        LOG.info("Adding Kafka sink - Topic: {}, Bootstrap: {}", topic, bootstrapServers);
        
        KafkaSink<String> kafkaSink = KafkaSink.<String>builder()
            .setBootstrapServers(bootstrapServers)
            .setRecordSerializer(KafkaRecordSerializationSchema.builder()
                .setTopic(topic)
                .setValueSerializationSchema(new SimpleStringSchema())
                .build())
            .build();
        
        stream.sinkTo(kafkaSink);
    }
    
    private static void addConsoleSink(DataStream<String> stream, JsonNode sink) {
        LOG.info("Adding console sink");
        stream.print();
    }
    
    private static void addFileSink(DataStream<String> stream, JsonNode sink) {
        String path = sink.get("path").asText();
        LOG.info("Adding file sink - Path: {}", path);
        
        // Simple file sink implementation
        stream.writeAsText(path);
    }
    
    private static void printUsage() {
        System.out.println("FlinkDotNet IR Runner v1.0.0");
        System.out.println("Executes FlinkDotNet Intermediate Representation (IR) as Apache Flink DataStream jobs");
        System.out.println();
        System.out.println("Usage:");
        System.out.println("  java -jar flink-ir-runner.jar --ir-file <path-to-ir.json>");
        System.out.println("  java -jar flink-ir-runner.jar --ir-base64 <base64-encoded-ir>");
        System.out.println();
        System.out.println("Options:");
        System.out.println("  --ir-file <path>     Load IR from JSON file");
        System.out.println("  --ir-base64 <data>   Load IR from base64-encoded string");
        System.out.println("  --help, -h           Show this help message");
        System.out.println();
        System.out.println("Examples:");
        System.out.println("  java -jar flink-ir-runner.jar --ir-file /path/to/job.json");
        System.out.println("  java -jar flink-ir-runner.jar --ir-base64 eyJ0eXBlIjoiam9iIn0=");
    }
}