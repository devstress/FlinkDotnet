package com.flinkdotnet;

import org.apache.flink.streaming.api.datastream.DataStream;
import org.apache.flink.streaming.api.environment.StreamExecutionEnvironment;
import org.apache.kafka.clients.consumer.KafkaConsumer;
import org.apache.kafka.clients.producer.KafkaProducer;
import org.apache.kafka.common.serialization.StringDeserializer;
import org.apache.kafka.common.serialization.StringSerializer;

import java.util.Collections;
import java.util.Properties;

/**
 * Native Apache Flink job to validate Aspire infrastructure setup.
 * 
 * This job demonstrates a simple Kafka -> Transform -> Kafka pipeline using
 * the legacy Kafka client API (same approach as FlinkJobRunner).
 * 
 * Purpose:
 * - Prove that Aspire's Flink cluster can execute standard Flink jobs
 * - Validate Kafka connectivity with proper bootstrap servers
 * - Use the same Kafka client approach as FlinkJobRunner for consistency
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
        
        // Configure Kafka Source using legacy API (same as FlinkJobRunner)
        Properties sourceProps = new Properties();
        sourceProps.put("bootstrap.servers", bootstrapServers);
        sourceProps.put("group.id", groupId);
        sourceProps.put("auto.offset.reset", "earliest");
        
        System.out.println("✓ Kafka source configured with legacy Kafka client API");
        
        // Configure Kafka Sink using legacy API (same as FlinkJobRunner)
        Properties sinkProps = new Properties();
        sinkProps.put("bootstrap.servers", bootstrapServers);
        
        System.out.println("✓ Kafka sink configured with legacy Kafka client API");
        
        // Build data stream pipeline with transformation
        DataStream<String> stream = env
            .addSource(new KafkaStringSource(inputTopic, sourceProps))
            .name("Kafka Source")
            .map(value -> {
                String transformed = value.toUpperCase();
                System.out.println("[TRANSFORM] Input: '" + value + "' -> Output: '" + transformed + "'");
                return transformed;
            })
            .name("Uppercase Transform");
        
        // Write to Kafka sink
        stream.addSink(new KafkaStringSink(outputTopic, sinkProps)).name("Kafka Sink");
        
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

    /**
     * Legacy Kafka Source using Kafka Client API directly (same approach as FlinkJobRunner).
     * This approach bundles the Kafka client in the JAR and avoids classloader issues.
     */
    public static class KafkaStringSource implements org.apache.flink.streaming.api.functions.source.legacy.SourceFunction<String> {
        private final String topic;
        private final Properties props;
        private volatile boolean running = true;

        public KafkaStringSource(String topic, Properties props) {
            this.topic = topic;
            this.props = props;
        }

        @Override
        public void run(org.apache.flink.streaming.api.functions.source.legacy.SourceFunction.SourceContext<String> ctx) throws Exception {
            System.out.println("════════════════════════════════════════════════════════════");
            System.out.println("[KAFKA SOURCE] Starting consumer...");
            System.out.println("  - Topic: " + topic);
            System.out.println("  - Bootstrap servers: " + props.getProperty("bootstrap.servers"));
            System.out.println("  - Group ID: " + props.getProperty("group.id"));
            System.out.println("  - Auto offset reset: " + props.getProperty("auto.offset.reset"));
            System.out.println("════════════════════════════════════════════════════════════");
            
            try (KafkaConsumer<String, String> consumer = new KafkaConsumer<>(props, new StringDeserializer(), new StringDeserializer())) {
                System.out.println("[KAFKA SOURCE] ✓ Consumer created, subscribing to topic: " + topic);
                consumer.subscribe(Collections.singletonList(topic));
                System.out.println("[KAFKA SOURCE] ✓ Subscribed successfully, starting poll loop...");
                
                int pollCount = 0;
                int totalRecords = 0;
                
                while (running) {
                    var records = consumer.poll(java.time.Duration.ofMillis(500));
                    pollCount++;
                    
                    if (records.count() > 0) {
                        System.out.println("[KAFKA SOURCE] Poll #" + pollCount + ": Received " + records.count() + " records");
                        totalRecords += records.count();
                    } else if (pollCount % 20 == 0) {
                        System.out.println("[KAFKA SOURCE] Poll #" + pollCount + ": Still polling, total records so far: " + totalRecords);
                    }
                    
                    for (var rec : records) {
                        synchronized (ctx.getCheckpointLock()) {
                            System.out.println("[KAFKA SOURCE] Collecting record: " + rec.value());
                            ctx.collect(rec.value());
                        }
                    }
                }
                
                System.out.println("[KAFKA SOURCE] Stopped. Total records processed: " + totalRecords);
            } catch (Exception e) {
                System.err.println("[KAFKA SOURCE] ✗ ERROR: " + e.getClass().getName() + ": " + e.getMessage());
                e.printStackTrace();
                throw e;
            }
        }

        @Override
        public void cancel() {
            running = false;
        }
    }

    /**
     * Legacy Kafka Sink using Kafka Client API directly (same approach as FlinkJobRunner).
     * This approach bundles the Kafka client in the JAR and avoids classloader issues.
     */
    public static class KafkaStringSink implements org.apache.flink.streaming.api.functions.sink.legacy.SinkFunction<String> {
        private final String topic;
        private final Properties props;
        private transient KafkaProducer<String, String> producer;

        public KafkaStringSink(String topic, Properties props) {
            this.topic = topic;
            this.props = props;
        }

        @Override
        public void invoke(String value, org.apache.flink.streaming.api.functions.sink.legacy.SinkFunction.Context context) {
            if (producer == null) {
                System.out.println("════════════════════════════════════════════════════════════");
                System.out.println("[KAFKA SINK] Initializing producer...");
                System.out.println("  - Topic: " + topic);
                System.out.println("  - Bootstrap servers: " + props.getProperty("bootstrap.servers"));
                System.out.println("════════════════════════════════════════════════════════════");
                producer = new KafkaProducer<>(props, new StringSerializer(), new StringSerializer());
                System.out.println("[KAFKA SINK] ✓ Producer created successfully");
            }
            try {
                producer.send(new org.apache.kafka.clients.producer.ProducerRecord<>(topic, value));
                System.out.println("[KAFKA SINK] Sent: " + value);
            } catch (Exception e) {
                System.err.println("[KAFKA SINK] ✗ ERROR sending message: " + e.getMessage());
                e.printStackTrace();
                throw new RuntimeException("Failed to send message to Kafka", e);
            }
        }
    }
}