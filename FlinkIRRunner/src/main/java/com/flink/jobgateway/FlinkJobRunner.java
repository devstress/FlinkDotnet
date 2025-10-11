package com.flink.jobgateway;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import com.fasterxml.jackson.annotation.JsonProperty;
import com.fasterxml.jackson.annotation.JsonSubTypes;
import com.fasterxml.jackson.annotation.JsonTypeInfo;
import com.fasterxml.jackson.databind.DeserializationFeature;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.apache.flink.streaming.api.datastream.DataStream;
import org.apache.flink.streaming.api.datastream.KeyedStream;
import org.apache.flink.streaming.api.datastream.SingleOutputStreamOperator;
import org.apache.flink.streaming.api.windowing.assigners.SlidingProcessingTimeWindows;
import org.apache.flink.streaming.api.windowing.assigners.TumblingProcessingTimeWindows;
import java.time.Duration;
import org.apache.flink.streaming.api.environment.StreamExecutionEnvironment;
import org.apache.flink.table.api.EnvironmentSettings;
import org.apache.flink.table.api.TableEnvironment;
import org.apache.flink.table.api.TableResult;
import org.apache.flink.streaming.api.functions.KeyedProcessFunction;
import org.apache.flink.streaming.api.functions.ProcessFunction;
import org.apache.flink.streaming.api.datastream.AsyncDataStream;
import org.apache.flink.streaming.api.functions.async.AsyncFunction;
import org.apache.flink.streaming.api.functions.async.ResultFuture;
import org.apache.flink.util.Collector;
import org.apache.flink.util.OutputTag;

import java.nio.charset.StandardCharsets;
import java.util.*;
import java.net.URI;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;

public class FlinkJobRunner {
    private static final Logger logger = LoggerFactory.getLogger(FlinkJobRunner.class);
    
    public static void main(String[] args) throws Exception {
        logger.info("========================================");
        logger.info("FlinkJobRunner Starting");
        logger.info("========================================");
        
        Map<String, String> argMap = parseArgs(args);
        String base64 = argMap.getOrDefault("--irBase64", argMap.get("-ir"));
        if (base64 == null || base64.isEmpty()) {
            throw new IllegalArgumentException("Missing --irBase64 argument");
        }

        String json = new String(Base64.getDecoder().decode(base64), StandardCharsets.UTF_8);
        ObjectMapper mapper = new ObjectMapper()
                .configure(DeserializationFeature.FAIL_ON_UNKNOWN_PROPERTIES, false);
        JobDefinition ir = mapper.readValue(json, JobDefinition.class);

        StreamExecutionEnvironment env = StreamExecutionEnvironment.getExecutionEnvironment();
        env.getConfig().setParallelism(ir.metadata != null && ir.metadata.parallelism != null ? ir.metadata.parallelism : 1);
        DataStream<String> stream;

        if (ir.source instanceof SqlSourceDefinition) {
            SqlSourceDefinition s = (SqlSourceDefinition) ir.source;
            if (s.statements == null || s.statements.isEmpty()) {
                throw new IllegalArgumentException("SQL job requires at least one statement");
            }
            
            // Get the context classloader to ensure all JARs are accessible
            ClassLoader contextClassLoader = Thread.currentThread().getContextClassLoader();
            
            TableEnvironment tEnv = TableEnvironment.create(
                    EnvironmentSettings.newInstance()
                            .inStreamingMode()
                            .withClassLoader(contextClassLoader)
                            .build());
            boolean hasInsert = false;
            TableResult lastResult = null;
            for (String stmt : s.statements) {
                if (stmt != null && !stmt.isBlank()) {
                    lastResult = tEnv.executeSql(stmt);
                    if (stmt.trim().toUpperCase(Locale.ROOT).startsWith("INSERT")) {
                        hasInsert = true;
                    }
                }
            }
            if (hasInsert && lastResult != null) {
                // For SQL jobs with INSERT statements, the job is now running in Flink.
                // In web submission mode (via Flink REST API), we should NOT block or try to get results.
                // The job runs asynchronously in the Flink cluster.
                // Simply return and let the JVM exit - the job will continue running in Flink.
                if (lastResult.getJobClient().isPresent()) {
                    var jobClient = lastResult.getJobClient().get();
                    System.out.println("SQL INSERT job submitted successfully to Flink. Job ID: " + jobClient.getJobID());
                    System.out.println("Job is running in Flink cluster. Exiting FlinkJobRunner.");
                    // Do NOT call getJobExecutionResult() - that's not supported in web submission mode
                    // Do NOT park the thread - the job runs independently in Flink
                } else {
                    System.out.println("SQL job submitted (no job client available).");
                }
            }
            return; // No further DataStream processing for pure SQL jobs
        } else if (ir.source instanceof KafkaSourceDefinition) {
            KafkaSourceDefinition k = (KafkaSourceDefinition) ir.source;
            
            if (k.bootstrapServers == null || k.bootstrapServers.isEmpty()) {
                throw new RuntimeException("Kafka source bootstrapServers is required but was not provided");
            }
            
            String bootstrap = k.bootstrapServers;
            String groupId = orElse(k.groupId, "flinkdotnet-ir-runner");

            logger.info("============================================================");
            logger.info("[KAFKA SOURCE] Configuration:");
            logger.info("  - bootstrapServers field from JSON: {}", k.bootstrapServers);
            logger.info("  - FINAL bootstrap.servers: {}", bootstrap);
            logger.info("  - Topic: {}", k.topic);
            logger.info("  - GroupId: {}", groupId);
            logger.info("  - Starting offsets: {}", orElse(k.startingOffsets, "latest"));
            logger.info("  - KAFKA_BOOTSTRAP_SERVERS env var: {}", System.getenv("KAFKA_BOOTSTRAP_SERVERS"));
            logger.info("  - bootstrap.servers system property: {}", System.getProperty("bootstrap.servers"));
            logger.info("============================================================");

            Properties props = new Properties();
            props.put("bootstrap.servers", bootstrap);
            props.put("group.id", groupId);
            props.put("auto.offset.reset", orElse(k.startingOffsets, "latest"));
            
            logger.info("[KAFKA SOURCE] Creating Kafka consumer with properties:");
            logger.info("  - bootstrap.servers: {}", props.getProperty("bootstrap.servers"));
            logger.info("  - group.id: {}", props.getProperty("group.id"));
            logger.info("  - auto.offset.reset: {}", props.getProperty("auto.offset.reset"));

            stream = env.addSource(new KafkaStringSource(k.topic, props)).name("KafkaSource");
            logger.info("[KAFKA SOURCE] Source created successfully");
        } else {
            // Fallback source
            stream = env.fromElements("sample");
        }

        // Apply operations
        if (ir.operations != null) {
            int maxRetries = 0; List<Long> retryDelays = Collections.emptyList();
            for (Operation op : ir.operations) {
                if (op instanceof MapOperationDefinition) {
                    MapOperationDefinition m = (MapOperationDefinition) op;
                    String expr = orElse(m.expression, m.function, "identity");
                    logger.info("============================================================");
                    logger.info("[MAP OPERATION] Processing:");
                    logger.info("  - expression field from JSON: {}", m.expression);
                    logger.info("  - function field from JSON: {}", m.function);
                    logger.info("  - Resolved expression: {}", expr);
                    logger.info("  - Normalized (lowercase): {}", expr.toLowerCase(Locale.ROOT));
                    logger.info("============================================================");
                    
                    switch (expr.toLowerCase(Locale.ROOT)) {
                        case "upper":
                        case "toupper":
                            logger.info("[MAP OPERATION] ✓ Applying toUpperCase transformation");
                            stream = stream.map(String::toUpperCase);
                            break;
                        case "lower":
                        case "tolower":
                            logger.info("[MAP OPERATION] ✓ Applying toLowerCase transformation");
                            stream = stream.map(String::toLowerCase);
                            break;
                        default:
                            logger.info("[MAP OPERATION] ⚠ Using identity transformation (pass-through) for: {}", expr);
                            // identity or unrecognized: pass through
                            break;
                    }
                } else if (op instanceof FilterOperationDefinition) {
                    // naive filter: support 'nonempty' only
                    FilterOperationDefinition f = (FilterOperationDefinition) op;
                    String expr = orElse(f.expression, "");
                    if ("nonempty".equalsIgnoreCase(expr)) {
                        stream = stream.filter(s -> s != null && !s.isEmpty());
                    }
                } else if (op instanceof WindowOperationDefinition) {
                    WindowOperationDefinition w = (WindowOperationDefinition) op;
                    String unit = orElse(w.timeUnit, "SECONDS").toUpperCase(Locale.ROOT);
                    long size = Math.max(1, w.size);
                    long slide = w.slide != null ? Math.max(1, w.slide) : size;
                    Duration sizeDur = toDuration(size, unit);
                    Duration slideDur = toDuration(slide, unit);
                    // Key by value for a trivial keyed window
                    KeyedStream<String, String> keyed = stream.keyBy(v -> v);
                    if ("SLIDING".equalsIgnoreCase(w.windowType)) {
                        stream = keyed.window(SlidingProcessingTimeWindows.of(sizeDur, slideDur))
                                .reduce((a, b) -> b); // pass-through reducer
                    } else {
                        stream = keyed.window(TumblingProcessingTimeWindows.of(sizeDur))
                                .reduce((a, b) -> b);
                    }
                } else if (op instanceof TimerOperationDefinition) {
                    TimerOperationDefinition t = (TimerOperationDefinition) op;
                    long delay = Math.max(1, t.delayMs);
                    // Use keyed process to register a processing time timer; duplicate output on timer
                    KeyedStream<String, String> keyed = stream.keyBy(v -> v);
                    stream = keyed.process(new KeyedProcessFunction<String, String, String>() {
                        @Override
                        public void processElement(String value, Context ctx, Collector<String> out) throws Exception {
                            out.collect(value);
                            long ts = ctx.timerService().currentProcessingTime() + delay;
                            ctx.timerService().registerProcessingTimeTimer(ts);
                        }

                        @Override
                        public void onTimer(long timestamp, OnTimerContext ctx, Collector<String> out) throws Exception {
                            // emit a heartbeat or duplicate signal; here we no-op to avoid floods
                        }
                    });
                } else if (op instanceof RetryOperationDefinition) {
                    RetryOperationDefinition r = (RetryOperationDefinition) op;
                    maxRetries = Math.max(0, r.maxRetries);
                    retryDelays = r.delayMs != null ? r.delayMs : Collections.emptyList();
                } else if (op instanceof AsyncFunctionOperationDefinition) {
                    AsyncFunctionOperationDefinition a = (AsyncFunctionOperationDefinition) op;
                    if ("http".equalsIgnoreCase(a.functionType)) {
                        int timeoutMs = Math.max(1, a.timeoutMs);
                        stream = AsyncDataStream.unorderedWait(
                                stream,
                                new AsyncHttpFunction(a, maxRetries, retryDelays),
                                timeoutMs, java.util.concurrent.TimeUnit.MILLISECONDS, 1000);
                    }
                } else if (op instanceof StateOperationDefinition) {
                    StateOperationDefinition st = (StateOperationDefinition) op;
                    KeyedStream<String, String> keyed = stream.keyBy(v -> v);
                    stream = keyed.process(new StatefulTouchFunction(st));
                } else if (op instanceof SideOutputOperationDefinition) {
                    SideOutputOperationDefinition so = (SideOutputOperationDefinition) op;
                    final OutputTag<String> tag = new OutputTag<String>(so.outputTag){};
                    SingleOutputStreamOperator<String> main = stream.process(new ProcessFunction<String, String>() {
                        @Override
                        public void processElement(String value, Context ctx, Collector<String> out) {
                            if ("nonempty".equalsIgnoreCase(so.condition) && value != null && !value.isEmpty()) {
                                ctx.output(tag, value);
                            }
                            out.collect(value);
                        }
                    });
                    // attach sink to side output (Kafka-only supported here)
                    DataStream<String> side = main.getSideOutput(tag);
                    if (so.sideOutputSink != null && so.sideOutputSink.type != null && so.sideOutputSink.type.equals("kafka")) {
                        if (so.sideOutputSink.bootstrapServers == null || so.sideOutputSink.bootstrapServers.isEmpty()) {
                            throw new RuntimeException("Side output Kafka sink bootstrapServers is required but was not provided");
                        }
                        String bootstrap = so.sideOutputSink.bootstrapServers;
                        Properties props = new Properties();
                        props.put("bootstrap.servers", bootstrap);
                        side.addSink(new KafkaStringSink(so.sideOutputSink.topic, props)).name("SideKafkaSink:"+so.outputTag);
                    }
                    stream = main;
                } else if (op instanceof AggregateOperationDefinition) {
                    AggregateOperationDefinition agg = (AggregateOperationDefinition) op;
                    String aggType = orElse(agg.aggregationType, "COLLECT").toUpperCase(Locale.ROOT);
                    
                    logger.info("============================================================");
                    logger.info("[AGGREGATE OPERATION] Processing:");
                    logger.info("  - aggregationType: {}", aggType);
                    logger.info("  - field: {}", orElse(agg.field, "*"));
                    logger.info("  - windowSeconds: {}", agg.windowSeconds);
                    logger.info("============================================================");
                    
                    // For COLLECT aggregation, collect all strings in window into a JSON array
                    if ("COLLECT".equals(aggType)) {
                        // Use Jackson ObjectMapper for proper JSON handling
                        final ObjectMapper jsonMapper = new ObjectMapper();
                        
                        // Get window duration from metadata or default to 10 seconds for testing
                        long windowSeconds = agg.windowSeconds != null && agg.windowSeconds > 0 ? agg.windowSeconds : 10;
                        Duration windowDuration = Duration.ofSeconds(windowSeconds);
                        
                        logger.info("[AGGREGATE] Using window duration: {} seconds", windowSeconds);
                        
                        KeyedStream<String, String> keyed = stream.keyBy(v -> "all"); // Global window key
                        stream = keyed.window(TumblingProcessingTimeWindows.of(windowDuration))
                                .aggregate(new org.apache.flink.api.common.functions.AggregateFunction<String, java.util.List<com.fasterxml.jackson.databind.JsonNode>, String>() {
                                    @Override
                                    public java.util.List<com.fasterxml.jackson.databind.JsonNode> createAccumulator() {
                                        logger.info("[AGGREGATE] Creating new accumulator for COLLECT aggregation");
                                        return new java.util.ArrayList<>();
                                    }
                                    
                                    @Override
                                    public java.util.List<com.fasterxml.jackson.databind.JsonNode> add(String value, java.util.List<com.fasterxml.jackson.databind.JsonNode> accumulator) {
                                        try {
                                            // Parse JSON string to JsonNode to ensure valid JSON
                                            com.fasterxml.jackson.databind.JsonNode node = jsonMapper.readTree(value);
                                            accumulator.add(node);
                                            logger.debug("[AGGREGATE] Added message to accumulator, total count: {}", accumulator.size());
                                            return accumulator;
                                        } catch (Exception e) {
                                            logger.error("[AGGREGATE] Failed to parse JSON message: {}", value, e);
                                            // Skip invalid JSON messages
                                            return accumulator;
                                        }
                                    }
                                    
                                    @Override
                                    public String getResult(java.util.List<com.fasterxml.jackson.databind.JsonNode> accumulator) {
                                        try {
                                            logger.info("[AGGREGATE] Finalizing Backup with {} messages", accumulator.size());
                                            
                                            // Build Backup object using Jackson
                                            java.util.Map<String, Object> backup = new java.util.LinkedHashMap<>();
                                            backup.put("inputMessages", accumulator);
                                            backup.put("backupTimestamp", java.time.Instant.now().toString());
                                            backup.put("uuid", java.util.UUID.randomUUID().toString());
                                            
                                            String json = jsonMapper.writeValueAsString(backup);
                                            logger.info("[AGGREGATE] Generated Backup JSON: {}", json);
                                            return json;
                                        } catch (Exception e) {
                                            logger.error("[AGGREGATE] Failed to serialize Backup", e);
                                            return "{\"inputMessages\":[],\"backupTimestamp\":\"" +
                                                   java.time.Instant.now().toString() +
                                                   "\",\"uuid\":\"" + java.util.UUID.randomUUID().toString() + "\"}";
                                        }
                                    }
                                    
                                    @Override
                                    public java.util.List<com.fasterxml.jackson.databind.JsonNode> merge(java.util.List<com.fasterxml.jackson.databind.JsonNode> a,
                                                                                                        java.util.List<com.fasterxml.jackson.databind.JsonNode> b) {
                                        a.addAll(b);
                                        logger.debug("[AGGREGATE] Merged accumulators, total count: {}", a.size());
                                        return a;
                                    }
                                });
                        logger.info("[AGGREGATE OPERATION] ✓ COLLECT aggregation configured with Jackson JSON handling");
                    }
                }
            }
        }

        if (ir.sink instanceof KafkaSinkDefinition) {
            KafkaSinkDefinition s = (KafkaSinkDefinition) ir.sink;
            // Priority: Sink JSON field → Source JSON field
            String bootstrap = orElse(s.bootstrapServers,
                    (ir.source instanceof KafkaSourceDefinition) ? ((KafkaSourceDefinition) ir.source).bootstrapServers : null);
            
            if (bootstrap == null || bootstrap.isEmpty()) {
                throw new RuntimeException("Kafka sink bootstrapServers is required but was not provided");
            }

            logger.info("============================================================");
            logger.info("[KAFKA SINK] Configuration:");
            logger.info("  - bootstrapServers field from JSON: {}", s.bootstrapServers);
            logger.info("  - Source bootstrapServers: {}", ((ir.source instanceof KafkaSourceDefinition) ? ((KafkaSourceDefinition) ir.source).bootstrapServers : "N/A"));
            logger.info("  - FINAL bootstrap.servers: {}", bootstrap);
            logger.info("  - Topic: {}", s.topic);
            logger.info("============================================================");

            Properties props = new Properties();
            props.put("bootstrap.servers", bootstrap);
            
            logger.info("[KAFKA SINK] Creating Kafka producer with properties:");
            logger.info("  - bootstrap.servers: {}", props.getProperty("bootstrap.servers"));
            logger.info("  - Target topic: {}", s.topic);
            
            stream.addSink(new KafkaStringSink(s.topic, props)).name("KafkaSink");
            logger.info("[KAFKA SINK] Sink created successfully");
        } else {
            stream.print();
        }

        String jobName = ir.metadata != null && ir.metadata.jobName != null ? ir.metadata.jobName : "flinkdotnet-ir-job";
        env.execute(jobName);
    }

    private static String orElse(String... values) {
        for (String v : values) {
            if (v != null && !v.isEmpty()) return v;
        }
        return null;
    }

    private static Duration toDuration(long amount, String unit) {
        switch (unit) {
            case "HOURS": return Duration.ofHours(amount);
            case "MINUTES": return Duration.ofMinutes(amount);
            default: return Duration.ofSeconds(amount);
        }
    }

    private static Map<String, String> parseArgs(String[] args) {
        Map<String, String> map = new HashMap<>();
        for (int i = 0; i < args.length; i++) {
            String a = args[i];
            if (a.startsWith("--")) {
                String key = a;
                String val = (i + 1 < args.length && !args[i + 1].startsWith("-")) ? args[++i] : "";
                map.put(key, val);
            } else if (a.startsWith("-")) {
                String key = a;
                String val = (i + 1 < args.length && !args[i + 1].startsWith("-")) ? args[++i] : "";
                map.put(key, val);
            }
        }
        return map;
    }

    // IR POJOs (subset sufficient for Kafka→Kafka + map/filter)
    @JsonIgnoreProperties(ignoreUnknown = true)
    public static class JobDefinition {
        public Source source;
        public List<Operation> operations;
        public Sink sink;
        public JobMetadata metadata;
    }

    @JsonIgnoreProperties(ignoreUnknown = true)
    public static class JobMetadata {
        public String jobId;
        public String jobName;
        public Integer parallelism;
    }

    @JsonTypeInfo(use = JsonTypeInfo.Id.NAME, include = JsonTypeInfo.As.EXISTING_PROPERTY, property = "type", visible = true)
@JsonSubTypes({
        @JsonSubTypes.Type(value = KafkaSourceDefinition.class, name = "kafka"),
        @JsonSubTypes.Type(value = SqlSourceDefinition.class, name = "sql")
})
public interface Source {}
    @JsonTypeInfo(use = JsonTypeInfo.Id.NAME, include = JsonTypeInfo.As.EXISTING_PROPERTY, property = "type", visible = true)
@JsonSubTypes({
        @JsonSubTypes.Type(value = KafkaSinkDefinition.class, name = "kafka")
})
public interface Sink {}
    @JsonTypeInfo(use = JsonTypeInfo.Id.NAME, include = JsonTypeInfo.As.EXISTING_PROPERTY, property = "type", visible = true)
@JsonSubTypes({
        @JsonSubTypes.Type(value = MapOperationDefinition.class, name = "map"),
        @JsonSubTypes.Type(value = FilterOperationDefinition.class, name = "filter"),
        @JsonSubTypes.Type(value = WindowOperationDefinition.class, name = "window"),
        @JsonSubTypes.Type(value = TimerOperationDefinition.class, name = "timer"),
        @JsonSubTypes.Type(value = RetryOperationDefinition.class, name = "retry"),
        @JsonSubTypes.Type(value = AsyncFunctionOperationDefinition.class, name = "async"),
        @JsonSubTypes.Type(value = StateOperationDefinition.class, name = "state"),
        @JsonSubTypes.Type(value = SideOutputOperationDefinition.class, name = "side-output"),
        @JsonSubTypes.Type(value = AggregateOperationDefinition.class, name = "aggregate")
})
public interface Operation {}

    @JsonIgnoreProperties(ignoreUnknown = true)
    public static class KafkaSourceDefinition implements Source {
        public String type;
        public String topic;
        public String bootstrapServers;
        public String groupId;
        @JsonProperty("startingOffsets")
        public String startingOffsets;
    }

    @JsonIgnoreProperties(ignoreUnknown = true)
    public static class SqlSourceDefinition implements Source {
        public String type;
        public List<String> statements;
        public String mode;
    }

    @JsonIgnoreProperties(ignoreUnknown = true)
    public static class KafkaSinkDefinition implements Sink {
        public String type;
        public String topic;
        public String bootstrapServers;
        public String serializer;
    }

    @JsonIgnoreProperties(ignoreUnknown = true)
    public static class MapOperationDefinition implements Operation {
        public String type;
        public String expression;
        public String function;  // Support both 'expression' and 'function' fields
    }

    @JsonIgnoreProperties(ignoreUnknown = true)
    public static class FilterOperationDefinition implements Operation {
        public String type;
        public String expression;
    }

    @JsonIgnoreProperties(ignoreUnknown = true)
    public static class WindowOperationDefinition implements Operation {
        public String type;
        public String windowType;
        public int size;
        public String timeUnit;
        public Integer slide;
    }

    @JsonIgnoreProperties(ignoreUnknown = true)
    public static class TimerOperationDefinition implements Operation {
        public String type;
        public String timerType;
        public long delayMs;
        public String timerName;
    }

    @JsonIgnoreProperties(ignoreUnknown = true)
    public static class RetryOperationDefinition implements Operation {
        public String type;
        public int maxRetries;
        public List<Long> delayMs;
    }

    @JsonIgnoreProperties(ignoreUnknown = true)
    public static class AsyncFunctionOperationDefinition implements Operation {
        public String type;
        public String functionType;
        public String url;
        public String method;
        public Map<String,String> headers;
        public String bodyTemplate;
        public int timeoutMs;
        public int maxRetries; // not used; prefer Retry op
    }

    @JsonIgnoreProperties(ignoreUnknown = true)
    public static class StateOperationDefinition implements Operation {
        public String type;
        public String stateType;
        public String stateKey;
        public Long ttlMs;
        public String defaultValue;
    }

    @JsonIgnoreProperties(ignoreUnknown = true)
    public static class SideOutputOperationDefinition implements Operation {
        public String type;
        public String outputTag;
        public String condition;
        public KafkaSinkDefinition sideOutputSink;
    }

    @JsonIgnoreProperties(ignoreUnknown = true)
    public static class AggregateOperationDefinition implements Operation {
        public String type;
        public String aggregationType;  // COLLECT, SUM, COUNT, etc.
        public String field;             // Field to aggregate, or "*" for all
        public Long windowSeconds;       // Window duration in seconds (default: 10 for testing, 86400 for production)
    }

    // Simple Kafka Source using Kafka client
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
            logger.info("============================================================");
            logger.info("[KAFKA SOURCE] Starting consumer...");
            logger.info("  - Topic: {}", topic);
            logger.info("  - Bootstrap servers: {}", props.getProperty("bootstrap.servers"));
            logger.info("  - Group ID: {}", props.getProperty("group.id"));
            logger.info("  - Auto offset reset: {}", props.getProperty("auto.offset.reset"));
            logger.info("============================================================");
            
            try (org.apache.kafka.clients.consumer.KafkaConsumer<String, String> consumer = new org.apache.kafka.clients.consumer.KafkaConsumer<>(props, new org.apache.kafka.common.serialization.StringDeserializer(), new org.apache.kafka.common.serialization.StringDeserializer())) {
                logger.info("[KAFKA SOURCE] ✓ Consumer created, subscribing to topic: {}", topic);
                consumer.subscribe(Collections.singletonList(topic));
                logger.info("[KAFKA SOURCE] ✓ Subscribed successfully, starting poll loop...");
                
                int pollCount = 0;
                int totalRecords = 0;
                
                while (running) {
                    var records = consumer.poll(java.time.Duration.ofMillis(500));
                    pollCount++;
                    
                    if (records.count() > 0) {
                        logger.info("[KAFKA SOURCE] Poll #{}: Received {} records", pollCount, records.count());
                        totalRecords += records.count();
                    } else if (pollCount % 20 == 0) {
                        // Log every 10 seconds (20 polls * 500ms) to show we're still polling
                        logger.info("[KAFKA SOURCE] Poll #{}: Still polling, total records so far: {}", pollCount, totalRecords);
                    }
                    
                    for (var rec : records) {
                        synchronized (ctx.getCheckpointLock()) {
                            logger.debug("[KAFKA SOURCE] Collecting record: {}", rec.value());
                            ctx.collect(rec.value());
                        }
                    }
                }
                
                logger.info("[KAFKA SOURCE] Stopped. Total records processed: {}", totalRecords);
            } catch (Exception e) {
                logger.error("[KAFKA SOURCE] ✗ ERROR: {}: {}", e.getClass().getName(), e.getMessage(), e);
                throw e;
            }
        }

        @Override
        public void cancel() {
            running = false;
        }
    }

    // Simple Kafka Sink using Kafka client
    public static class KafkaStringSink implements org.apache.flink.streaming.api.functions.sink.legacy.SinkFunction<String> {
        private final String topic;
        private final Properties props;
        private transient org.apache.kafka.clients.producer.KafkaProducer<String, String> producer;

        public KafkaStringSink(String topic, Properties props) {
            this.topic = topic;
            this.props = props;
        }

        @Override
        public void invoke(String value, org.apache.flink.streaming.api.functions.sink.legacy.SinkFunction.Context context) {
            if (producer == null) {
                logger.info("============================================================");
                logger.info("[KAFKA SINK] Initializing producer...");
                logger.info("  - Topic: {}", topic);
                logger.info("  - Bootstrap servers: {}", props.getProperty("bootstrap.servers"));
                logger.info("============================================================");
                
                try {
                    producer = new org.apache.kafka.clients.producer.KafkaProducer<>(props, new org.apache.kafka.common.serialization.StringSerializer(), new org.apache.kafka.common.serialization.StringSerializer());
                    logger.info("[KAFKA SINK] ✓ Producer created successfully");
                } catch (Exception e) {
                    logger.error("[KAFKA SINK] ✗ ERROR creating producer: {}: {}", e.getClass().getName(), e.getMessage(), e);
                    throw e;
                }
            }
            
            try {
                logger.debug("[KAFKA SINK] Sending message to topic '{}': {}", topic, value);
                producer.send(new org.apache.kafka.clients.producer.ProducerRecord<>(topic, value));
                logger.debug("[KAFKA SINK] ✓ Message sent successfully");
            } catch (Exception e) {
                logger.error("[KAFKA SINK] ✗ ERROR sending message: {}: {}", e.getClass().getName(), e.getMessage(), e);
                throw e;
            }
        }

        @Override
        public void finish() {
            if (producer != null) {
                producer.flush();
                producer.close();
            }
        }
    }

    // Async HTTP function
    public static class AsyncHttpFunction implements AsyncFunction<String, String> {
        private final AsyncFunctionOperationDefinition cfg;
        private final int maxRetries;
        private final List<Long> delays;
        private transient HttpClient client;

        public AsyncHttpFunction(AsyncFunctionOperationDefinition cfg, int maxRetries, List<Long> delays) {
            this.cfg = cfg;
            this.maxRetries = maxRetries;
            this.delays = delays != null ? delays : Collections.emptyList();
        }

        @Override
        public void asyncInvoke(String input, ResultFuture<String> result) throws Exception {
            if (client == null) client = HttpClient.newBuilder().build();
            String url = cfg.url != null ? cfg.url : "";
            if (url.isEmpty()) { result.complete(Collections.singleton(input)); return; }
            HttpRequest.Builder rb = HttpRequest.newBuilder().uri(URI.create(url));
            String method = cfg.method != null ? cfg.method.toUpperCase(Locale.ROOT) : "GET";
            String body = cfg.bodyTemplate != null ? cfg.bodyTemplate.replace("${value}", input) : null;
            switch (method) {
                case "POST": rb.POST(HttpRequest.BodyPublishers.ofString(body != null ? body : input)); break;
                case "PUT": rb.PUT(HttpRequest.BodyPublishers.ofString(body != null ? body : input)); break;
                default: rb.GET(); break;
            }
            if (cfg.headers != null) {
                for (var e : cfg.headers.entrySet()) rb.header(e.getKey(), e.getValue());
            }
            int timeout = Math.max(1, cfg.timeoutMs);
            rb.timeout(Duration.ofMillis(timeout));
            HttpRequest req = rb.build();

            // retry loop (simple synchronous backoff inside asyncInvoke)
            int attempts = 0; Exception lastEx = null;
            while (attempts <= maxRetries) {
                try {
                    HttpResponse<String> resp = client.send(req, HttpResponse.BodyHandlers.ofString());
                    if (resp.statusCode() >= 200 && resp.statusCode() < 300) {
                        // Replace with response or passthrough
                        result.complete(Collections.singleton(resp.body()));
                        return;
                    }
                } catch (Exception ex) {
                    lastEx = ex;
                }
                if (attempts == maxRetries) break;
                long delay = (attempts < delays.size()) ? Math.max(1, delays.get(attempts)) : 1000L;
                try { Thread.sleep(delay); } catch (InterruptedException ie) { Thread.currentThread().interrupt(); break; }
                attempts++;
            }
            // On failure, pass through original value
            result.complete(Collections.singleton(input));
        }
    }

    // Stateful touch function to exercise state and TTL
    public static class StatefulTouchFunction extends KeyedProcessFunction<String, String, String> {
        private final StateOperationDefinition st;
        private transient org.apache.flink.api.common.state.ValueState<String> state;

        public StatefulTouchFunction(StateOperationDefinition st) { this.st = st; }

        public void open(org.apache.flink.configuration.Configuration parameters) throws Exception {
            var desc = new org.apache.flink.api.common.state.ValueStateDescriptor<String>(
                    st.stateKey != null ? st.stateKey : "state", String.class);
            if (st.ttlMs != null && st.ttlMs > 0) {
                var cfg = org.apache.flink.api.common.state.StateTtlConfig
                        .newBuilder(java.time.Duration.ofMillis(st.ttlMs))
                        .setUpdateType(org.apache.flink.api.common.state.StateTtlConfig.UpdateType.OnCreateAndWrite)
                        .build();
                desc.enableTimeToLive(cfg);
            }
            state = getRuntimeContext().getState(desc);
        }

        @Override
        public void processElement(String value, Context ctx, Collector<String> out) throws Exception {
            state.update(value);
            out.collect(value);
        }
    }
}
