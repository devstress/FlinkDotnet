package com.flinkdotnet;

import org.apache.flink.api.common.eventtime.WatermarkStrategy;
import org.apache.flink.api.common.serialization.SimpleStringSchema;
import org.apache.flink.connector.kafka.source.KafkaSource;
import org.apache.flink.connector.kafka.source.enumerator.initializer.OffsetsInitializer;
import org.apache.flink.connector.kafka.sink.KafkaRecordSerializationSchema;
import org.apache.flink.connector.kafka.sink.KafkaSink;
import org.apache.flink.streaming.api.datastream.DataStream;
import org.apache.flink.streaming.api.environment.StreamExecutionEnvironment;

/**
 * Native Apache Flink job to validate Aspire infrastructure setup.
 * 
 * This job demonstrates a simple Kafka -> Transform -> Kafka pipeline using
 * the official Flink Kafka connector (not raw Kafka clients).
 * 
 * Purpose:
 * - Prove that Aspire's Flink cluster can execute standard Flink jobs
 * - Validate Kafka connectivity with proper bootstrap servers
 * - Establish a known-good reference configuration
 * 
 * Usage:
 *   Build: mvn clean package
 *   Submit to Flink: Upload JAR via REST API or Flink UI
 *   
 * Configuration:
 *   Bootstrap servers, topics, and group ID are passed as command-line arguments
 *   or use defaults for LocalTesting environment.
 */
public class NativeKafkaJob {
    
    public static void main(String[] args) throws Exception {
        // Parse command-line arguments with defaults for LocalTesting
        final String bootstrapServers = getArgOrDefault(args, "--bootstrap-servers", "kafka:9093");
        final String inputTopic = getArgOrDefault(args, "--input-topic", "lt.native.input");
        final String outputTopic = getArgOrDefault(args, "--output-topic", "lt.native.output");
        final String groupId = getArgOrDefault(args, "--group-id", "native-flink-consumer");
        
        System.out.println("========================================");
        System.out.println("Native Flink Kafka Job - Infrastructure Validation");
        System.out.println("========================================");
        System.out.println("Configuration:");
        System.out.println("  Bootstrap Servers: " + bootstrapServers);
        System.out.println("  Input Topic: " + inputTopic);
        System.out.println("  Output Topic: " + outputTopic);
        System.out.println("  Group ID: " + groupId);
        System.out.println("========================================");
        
        // Create Flink execution environment
        final StreamExecutionEnvironment env = StreamExecutionEnvironment.getExecutionEnvironment();
        env.setParallelism(1); // Single parallelism for testing
        
        // Configure Kafka Source using official Flink Kafka Connector
        KafkaSource<String> source = KafkaSource.<String>builder()
            .setBootstrapServers(bootstrapServers)
            .setTopics(inputTopic)
            .setGroupId(groupId)
            .setStartingOffsets(OffsetsInitializer.earliest())
            .setValueOnlyDeserializer(new SimpleStringSchema())
            .build();
        
        System.out.println("✓ Kafka source configured with official Flink connector");
        
        // Configure Kafka Sink using official Flink Kafka Connector
        KafkaSink<String> sink = KafkaSink.<String>builder()
            .setBootstrapServers(bootstrapServers)
            .setRecordSerializer(KafkaRecordSerializationSchema.builder()
                .setTopic(outputTopic)
                .setValueSerializationSchema(new SimpleStringSchema())
                .build())
            .build();
        
        System.out.println("✓ Kafka sink configured with official Flink connector");
        
        // Build data stream pipeline with transformation
        DataStream<String> stream = env
            .fromSource(source, WatermarkStrategy.noWatermarks(), "Kafka Source")
            .map(value -> {
                String transformed = value.toUpperCase();
                System.out.println("[TRANSFORM] Input: '" + value + "' -> Output: '" + transformed + "'");
                return transformed;
            })
            .name("Uppercase Transform");
        
        // Write to Kafka sink
        stream.sinkTo(sink).name("Kafka Sink");
        
        System.out.println("✓ Pipeline configured: Kafka -> Uppercase Transform -> Kafka");
        System.out.println("Starting job execution...");
        
        // Execute the Flink job
        env.execute("Native Kafka Uppercase Job");
    }
    
    /**
     * Get command-line argument value or return default.
     */
    private static String getArgOrDefault(String[] args, String key, String defaultValue) {
        for (int i = 0; i < args.length - 1; i++) {
            if (args[i].equals(key)) {
                return args[i + 1];
            }
        }
        return defaultValue;
    }
}