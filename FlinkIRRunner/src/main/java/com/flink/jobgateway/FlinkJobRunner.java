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
import org.apache.flink.streaming.api.windowing.assigners.TumblingEventTimeWindows;
import org.apache.flink.api.common.eventtime.WatermarkStrategy;
import org.apache.flink.api.common.eventtime.SerializableTimestampAssigner;
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
        // DEBUG: Log environment variable for debugging log file location
        String logFilePath = System.getenv("LOG_FILE_PATH");
        System.out.println("========================================");
        System.out.println("FlinkJobRunner Starting");
        System.out.println("[DEBUG] LOG_FILE_PATH environment variable: " + logFilePath);
        System.out.println("[DEBUG] Current working directory: " + System.getProperty("user.dir"));
        System.out.println("[DEBUG] Java temp directory: " + System.getProperty("java.io.tmpdir"));
        System.out.println("========================================");
        
        logger.info("========================================");
        logger.info("FlinkJobRunner Starting");
        logger.info("[DEBUG] LOG_FILE_PATH environment variable: {}", logFilePath);
        logger.info("[DEBUG] Current working directory: {}", System.getProperty("user.dir"));
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

        logger.info("============================================================");
        logger.info("[FLINK ENVIRONMENT] Creating StreamExecutionEnvironment");
        logger.info("============================================================");
        StreamExecutionEnvironment env = StreamExecutionEnvironment.getExecutionEnvironment();
        int parallelism = ir.metadata != null && ir.metadata.parallelism != null ? ir.metadata.parallelism : 1;
        env.getConfig().setParallelism(parallelism);
        logger.info("[FLINK ENVIRONMENT] ✓ Environment created");
        logger.info("[FLINK ENVIRONMENT] ✓ Parallelism set to: {}", parallelism);
        logger.info("[FLINK ENVIRONMENT] Java equivalent: StreamExecutionEnvironment env = StreamExecutionEnvironment.getExecutionEnvironment();");
        logger.info("[FLINK ENVIRONMENT] Java equivalent: env.getConfig().setParallelism({});", parallelism);
        
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
            
            // Check if EventTime is configured
            boolean useEventTime = ir.metadata != null && ir.metadata.properties != null &&
                                 "EventTime".equals(ir.metadata.properties.get("timeCharacteristic"));

            logger.info("============================================================");
            logger.info("[KAFKA SOURCE] Configuration:");
            logger.info("  - bootstrapServers field from JSON: {}", k.bootstrapServers);
            logger.info("  - FINAL bootstrap.servers: {}", bootstrap);
            logger.info("  - Topic: {}", k.topic);
            logger.info("  - GroupId: {}", groupId);
            logger.info("  - Starting offsets: {}", orElse(k.startingOffsets, "latest"));
            logger.info("  - Time Characteristic: {}", useEventTime ? "EventTime" : "ProcessingTime");
            logger.info("  - KAFKA_BOOTSTRAP_SERVERS env var: {}", System.getenv("KAFKA_BOOTSTRAP_SERVERS"));
            logger.info("  - bootstrap.servers system property: {}", System.getProperty("bootstrap.servers"));
            logger.info("============================================================");

            Properties props = new Properties();
            props.put("bootstrap.servers", bootstrap);
            props.put("group.id", groupId);
            props.put("auto.offset.reset", orElse(k.startingOffsets, "latest"));
            // Enable auto-commit for consumer group offset tracking
            // This allows Kafka to track consumer lag properly
            props.put("enable.auto.commit", "true");
            props.put("auto.commit.interval.ms", "1000"); // Commit every 1 second
            
            logger.info("[KAFKA SOURCE] Creating Kafka consumer with properties:");
            logger.info("  - bootstrap.servers: {}", props.getProperty("bootstrap.servers"));
            logger.info("  - group.id: {}", props.getProperty("group.id"));
            logger.info("  - auto.offset.reset: {}", props.getProperty("auto.offset.reset"));
            logger.info("  - enable.auto.commit: {}", props.getProperty("enable.auto.commit"));
            logger.info("  - auto.commit.interval.ms: {}", props.getProperty("auto.commit.interval.ms"));

            logger.info("[KAFKA SOURCE] Adding source to Flink environment...");
            logger.info("[KAFKA SOURCE] Java equivalent: DataStream<String> stream = env.addSource(new KafkaStringSource(\"{}\", props)).name(\"KafkaSource\");", k.topic);
            stream = env.addSource(new KafkaStringSource(k.topic, props)).name("KafkaSource");
            logger.info("[KAFKA SOURCE] ✓ Source created successfully");
            logger.info("[KAFKA SOURCE] ✓ Stream type: DataStream<String>");
            
            // Apply EventTime watermark strategy if configured (Standard Flink pattern)
            if (useEventTime) {
                logger.info("[WATERMARK STRATEGY] Applying standard Flink EventTime watermark strategy");
                logger.info("[WATERMARK STRATEGY] Using forBoundedOutOfOrderness with 200ms tolerance");
                logger.info("[WATERMARK STRATEGY] Watermarks will lag 200ms behind max observed event timestamp");
                logger.info("[WATERMARK STRATEGY] Pattern: WatermarkStrategy.forBoundedOutOfOrderness(Duration.ofMillis(200))");
                
                stream = stream.assignTimestampsAndWatermarks(
                    WatermarkStrategy
                        .<String>forBoundedOutOfOrderness(Duration.ofMillis(200))
                        .withIdleness(Duration.ofSeconds(1))
                        .withTimestampAssigner(new SerializableTimestampAssigner<String>() {
                            @Override
                            public long extractTimestamp(String element, long recordTimestamp) {
                                try {
                                    // Parse JSON to extract sentAt timestamp (Baeldung InputMessage pattern)
                                    // Support both lowercase "sentAt" and uppercase "SENTAT"
                                    ObjectMapper mapper = new ObjectMapper();
                                    var node = mapper.readTree(element);
                                    String sentAtField = node.has("sentAt") ? "sentAt" :
                                                        node.has("SENTAT") ? "SENTAT" :
                                                        node.has("SentAt") ? "SentAt" : null;
                                    
                                    if (sentAtField != null) {
                                        String sentAt = node.get(sentAtField).asText();
                                        // Parse ISO 8601 timestamp
                                        java.time.Instant instant = java.time.Instant.parse(sentAt);
                                        long timestamp = instant.toEpochMilli();
                                        logger.info("[WATERMARK] Extracted event timestamp={} ({}) from {}: {}",
                                            timestamp, java.time.Instant.ofEpochMilli(timestamp), sentAtField, sentAt);
                                        return timestamp;
                                    } else {
                                        logger.warn("[WATERMARK] No timestamp field found in message (tried sentAt, SENTAT, SentAt)");
                                    }
                                } catch (Exception e) {
                                    logger.warn("[WATERMARK] Failed to extract timestamp from message: {}", e.getMessage());
                                }
                                // Fallback to Kafka record timestamp
                                logger.info("[WATERMARK] Using Kafka record timestamp: {} ({})",
                                    recordTimestamp, java.time.Instant.ofEpochMilli(recordTimestamp));
                                return recordTimestamp;
                            }
                        })
                );
                logger.info("[WATERMARK STRATEGY] ✓ Standard bounded out-of-orderness watermark strategy applied");
                logger.info("[WATERMARK STRATEGY] ✓ Watermarks will progress naturally with event time");
            }
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
                            logger.info("[MAP OPERATION] Java equivalent: stream = stream.map(String::toUpperCase);");
                            stream = stream.map(String::toUpperCase);
                            break;
                        case "lower":
                        case "tolower":
                            logger.info("[MAP OPERATION] ✓ Applying toLowerCase transformation");
                            logger.info("[MAP OPERATION] Java equivalent: stream = stream.map(String::toLowerCase);");
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
                    logger.info("  - windowCount: {}", agg.windowCount);
                    logger.info("============================================================");
                    
                    // For COLLECT aggregation, collect all strings in window into a JSON array
                    if ("COLLECT".equals(aggType)) {
                        // Use Jackson ObjectMapper for proper JSON handling
                        final ObjectMapper jsonMapper = new ObjectMapper();
                        
                        logger.info("[AGGREGATE] Baeldung pattern: Using timeWindowAll() for global aggregation across all parallel instances");
                        logger.info("[AGGREGATE] Java equivalent: stream.timeWindowAll(Time.hours(24)).aggregate(new BackupAggregator())");
                        
                        // Create the aggregate function once to reuse
                        org.apache.flink.api.common.functions.AggregateFunction<String, java.util.List<com.fasterxml.jackson.databind.JsonNode>, String> aggregateFunction =
                                new org.apache.flink.api.common.functions.AggregateFunction<String, java.util.List<com.fasterxml.jackson.databind.JsonNode>, String>() {
                                    @Override
                                    public java.util.List<com.fasterxml.jackson.databind.JsonNode> createAccumulator() {
                                        logger.info("[AGGREGATE] Creating new accumulator for COLLECT aggregation");
                                        return new java.util.ArrayList<>();
                                    }
                                    
                                    @Override
                                    public java.util.List<com.fasterxml.jackson.databind.JsonNode> add(String value, java.util.List<com.fasterxml.jackson.databind.JsonNode> accumulator) {
                                        try {
                                            logger.info("[AGGREGATE.ADD] *** CALLED *** Receiving message to add to accumulator");
                                            logger.info("[AGGREGATE.ADD] Current accumulator size: {}", accumulator.size());
                                            logger.info("[AGGREGATE.ADD] Message value: {}", value);
                                            
                                            // Parse JSON string to JsonNode to ensure valid JSON
                                            com.fasterxml.jackson.databind.JsonNode node = jsonMapper.readTree(value);
                                            accumulator.add(node);
                                            
                                            logger.info("[AGGREGATE.ADD] *** SUCCESS *** Message added! New accumulator size: {}", accumulator.size());
                                            return accumulator;
                                        } catch (Exception e) {
                                            logger.error("[AGGREGATE.ADD] *** FAILED *** Error parsing JSON message: {}", value, e);
                                            logger.error("[AGGREGATE.ADD] Exception: {}", e.getMessage());
                                            // Skip invalid JSON messages but log it
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
                                };
                        
                        // Choose window type: count-based or time-based
                        if (agg.windowCount != null && agg.windowCount > 0) {
                            // COUNT-BASED WINDOW (Exercise 2: aggregate 50 messages)
                            logger.info("[AGGREGATE] Using COUNT-based global window: {} messages", agg.windowCount);
                            logger.info("[AGGREGATE] Java equivalent: stream = stream.countWindowAll({}).aggregate(aggregateFunction);", agg.windowCount);
                            stream = stream.countWindowAll(agg.windowCount)
                                    .aggregate(aggregateFunction);
                            logger.info("[AGGREGATE OPERATION] ✓ COUNT-based COLLECT aggregation configured (Baeldung countWindowAll pattern)");
                        } else if (agg.windowSeconds != null && agg.windowSeconds > 0) {
                            // TIME-BASED WINDOW - Baeldung equivalent for Flink 2.x
                            // Baeldung (Flink 1.x): stream.timeWindowAll(Time.hours(24)).aggregate(aggregator)
                            // Flink 2.x equivalent: stream.windowAll(TumblingEventTimeWindows.of(Duration.ofHours(24))).aggregate(aggregator)
                            // Note: timeWindowAll() was removed in Flink 2.x, windowAll() is the replacement
                            // Both create identical 24-hour tumbling event-time windows
                            
                            // Check if EventTime is configured
                            boolean useEventTime = ir.metadata != null && ir.metadata.properties != null &&
                                                 "EventTime".equals(ir.metadata.properties.get("timeCharacteristic"));
                            
                            Duration windowDuration = Duration.ofSeconds(agg.windowSeconds);
                            long hours = agg.windowSeconds / 3600;
                            
                            logger.info("[AGGREGATE] Using TIME-based global window: {} seconds ({} hours)", agg.windowSeconds, hours);
                            logger.info("[AGGREGATE] Baeldung Flink 1.x code: inputMessagesStream.timeWindowAll(Time.hours({})).aggregate(new BackupAggregator())", hours);
                            logger.info("[AGGREGATE] Our Flink 2.x equivalent: inputMessagesStream.windowAll(TumblingEventTimeWindows.of(Duration.ofHours({}))).aggregate(aggregateFunction)", hours);
                            
                            if (useEventTime) {
                                // EventTime windows - Baeldung's actual behavior
                                logger.info("[AGGREGATE] Using EventTime windows - BAELDUNG BEHAVIOR (fires based on event timestamps and watermarks)");
                                stream = stream.windowAll(TumblingEventTimeWindows.of(windowDuration))
                                        .aggregate(aggregateFunction);
                                logger.info("[AGGREGATE OPERATION] ✓ Global EventTime window configured (Baeldung timeWindowAll equivalent)");
                            } else {
                                // ProcessingTime windows - for testing only
                                logger.info("[AGGREGATE] Using ProcessingTime windows - TESTING MODE (fires based on wall clock)");
                                stream = stream.windowAll(TumblingProcessingTimeWindows.of(windowDuration))
                                        .aggregate(aggregateFunction);
                                logger.info("[AGGREGATE OPERATION] ✓ Global ProcessingTime window configured (testing mode)");
                            }
                        } else {
                            // DEFAULT: 60-second (1 minute) global time window (ProcessingTime)
                            logger.warn("[AGGREGATE] No window specified, using default 60-second (1 minute) global time window");
                            stream = stream.windowAll(TumblingProcessingTimeWindows.of(Duration.ofSeconds(60)))
                                    .aggregate(aggregateFunction);
                            logger.info("[AGGREGATE OPERATION] ✓ Default global ProcessingTime window configured (60 seconds)");
                        }
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
            
            logger.info("[KAFKA SINK] Adding sink to stream...");
            logger.info("[KAFKA SINK] Java equivalent: stream.addSink(new KafkaStringSink(\"{}\", props)).name(\"KafkaSink\");", s.topic);
            stream.addSink(new KafkaStringSink(s.topic, props)).name("KafkaSink");
            logger.info("[KAFKA SINK] ✓ Sink created successfully");
        } else if (ir.sink instanceof UnifiedSinkV2Definition) {
            UnifiedSinkV2Definition s = (UnifiedSinkV2Definition) ir.sink;
            
            logger.info("============================================================");
            logger.info("[UNIFIED SINK V2] Configuration:");
            logger.info("  - Sink Type: {}", s.sinkType);
            logger.info("  - Semantics: {}", s.semantics);
            logger.info("  - Stateful: {}", s.stateful);
            logger.info("  - Writer Class: {}", s.writerConfig != null ? s.writerConfig.className : "N/A");
            logger.info("  - Committer Enabled: {}", s.committerConfig != null && s.committerConfig.enabled);
            logger.info("============================================================");
            
            // Currently only Kafka is supported via UnifiedSinkV2
            if ("kafka".equalsIgnoreCase(s.sinkType)) {
                // Extract Kafka configuration from writer config properties
                if (s.writerConfig == null || s.writerConfig.properties == null) {
                    throw new RuntimeException("UnifiedSinkV2 Kafka sink requires writerConfig.properties");
                }
                
                Object topicObj = s.writerConfig.properties.get("topic");
                Object bootstrapObj = s.writerConfig.properties.get("bootstrapServers");
                
                if (topicObj == null || bootstrapObj == null) {
                    // Fallback to source bootstrap servers if not provided
                    if (bootstrapObj == null && ir.source instanceof KafkaSourceDefinition) {
                        bootstrapObj = ((KafkaSourceDefinition) ir.source).bootstrapServers;
                    }
                    if (topicObj == null || bootstrapObj == null) {
                        throw new RuntimeException("UnifiedSinkV2 Kafka sink requires 'topic' and 'bootstrapServers' in writerConfig.properties");
                    }
                }
                
                String topic = topicObj.toString();
                String bootstrap = bootstrapObj.toString();
                
                logger.info("[UNIFIED SINK V2 - KAFKA] Using Kafka configuration:");
                logger.info("  - Topic: {}", topic);
                logger.info("  - Bootstrap Servers: {}", bootstrap);
                
                // Create Kafka producer properties
                Properties props = new Properties();
                props.put("bootstrap.servers", bootstrap);
                
                // Add transaction support if exactly-once semantics requested
                if ("exactly-once".equalsIgnoreCase(s.semantics) && s.committerConfig != null && s.committerConfig.enabled) {
                    logger.info("[UNIFIED SINK V2 - KAFKA] Enabling exactly-once semantics with transactions");
                    
                    // Get transaction prefix from committer config or use default
                    String txPrefix = "flink-";
                    if (s.committerConfig.properties != null && s.committerConfig.properties.containsKey("transactionPrefix")) {
                        txPrefix = s.committerConfig.properties.get("transactionPrefix").toString();
                    }
                    
                    // Note: For full exactly-once support, we would need to use Flink's Kafka connector
                    // For now, we'll use the simple sink with a note
                    logger.warn("[UNIFIED SINK V2 - KAFKA] Full exactly-once support requires Flink Kafka connector");
                    logger.warn("[UNIFIED SINK V2 - KAFKA] Currently using at-least-once semantics via simple sink");
                }
                
                // Create and attach the Kafka sink
                logger.info("[UNIFIED SINK V2 - KAFKA] Creating Kafka sink via Unified Sink API v2");
                UnifiedSinkV2KafkaWrapper kafkaSink = new UnifiedSinkV2KafkaWrapper(topic, props, s);
                stream.sinkTo(kafkaSink).name("UnifiedSinkV2-Kafka");
                logger.info("[UNIFIED SINK V2 - KAFKA] ✓ Sink created successfully");
            } else {
                throw new RuntimeException("UnifiedSinkV2 sinkType '" + s.sinkType + "' not yet supported. Currently only 'kafka' is implemented.");
            }
        } else {
            stream.print();
        }

        String jobName = ir.metadata != null && ir.metadata.jobName != null ? ir.metadata.jobName : "flinkdotnet-ir-job";
        logger.info("============================================================");
        logger.info("[FLINK EXECUTION] Starting job execution");
        logger.info("[FLINK EXECUTION] Job name: {}", jobName);
        logger.info("[FLINK EXECUTION] Java equivalent: env.execute(\"{}\");", jobName);
        logger.info("============================================================");
        logger.info("[PIPELINE SUMMARY] Complete Flink DataStream pipeline built:");
        logger.info("  1. Source: env.addSource(KafkaStringSource)");
        logger.info("  2. Operations: {} transformation(s) applied", ir.operations != null ? ir.operations.size() : 0);
        logger.info("  3. Sink: stream.addSink(KafkaStringSink)");
        logger.info("  4. Execute: env.execute(\"{}\");", jobName);
        logger.info("============================================================");
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
        public SinkDefinitionType sink;
        public JobMetadata metadata;
    }

    @JsonIgnoreProperties(ignoreUnknown = true)
    public static class JobMetadata {
        public String jobId;
        public String jobName;
        public Integer parallelism;
        public Map<String, String> properties;
    }

    @JsonTypeInfo(use = JsonTypeInfo.Id.NAME, include = JsonTypeInfo.As.EXISTING_PROPERTY, property = "type", visible = true)
@JsonSubTypes({
        @JsonSubTypes.Type(value = KafkaSourceDefinition.class, name = "kafka"),
        @JsonSubTypes.Type(value = SqlSourceDefinition.class, name = "sql")
})
public interface Source {}
    @JsonTypeInfo(use = JsonTypeInfo.Id.NAME, include = JsonTypeInfo.As.EXISTING_PROPERTY, property = "type", visible = true)
@JsonSubTypes({
        @JsonSubTypes.Type(value = KafkaSinkDefinition.class, name = "kafka"),
        @JsonSubTypes.Type(value = UnifiedSinkV2Definition.class, name = "unified_sink_v2")
})
public interface SinkDefinitionType {}
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
    public static class KafkaSinkDefinition implements SinkDefinitionType {
        public String type;
        public String topic;
        public String bootstrapServers;
        public String serializer;
    }

    @JsonIgnoreProperties(ignoreUnknown = true)
    public static class UnifiedSinkV2Definition implements SinkDefinitionType {
        public String type;
        public String sinkType;
        public SinkWriterConfig writerConfig;
        public SinkCommitterConfig committerConfig;
        public String semantics;
        public boolean stateful;
        public Map<String, String> properties;
    }

    @JsonIgnoreProperties(ignoreUnknown = true)
    public static class SinkWriterConfig {
        public String className;
        public Map<String, Object> properties;
    }

    @JsonIgnoreProperties(ignoreUnknown = true)
    public static class SinkCommitterConfig {
        public boolean enabled;
        public String className;
        public Map<String, Object> properties;
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
        public Integer windowCount;      // Window count for count-based windows (e.g., 50 messages)
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

    // Simple Kafka Sink using Kafka client with Prometheus metric tracking
    public static class KafkaStringSink implements org.apache.flink.streaming.api.functions.sink.legacy.SinkFunction<String> {
        private final String topic;
        private final Properties props;
        private transient org.apache.kafka.clients.producer.KafkaProducer<String, String> producer;
        // Note: Custom metrics would require RichSinkFunction which has API compatibility issues
        // For now, rely on Flink's built-in metrics (numRecordsIn/Out) for message tracking

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
                    logger.info("[PROMETHEUS TRACKING] Using Flink's built-in metrics for message tracking:");
                    logger.info("  - flink_taskmanager_job_task_operator_numRecordsIn");
                    logger.info("  - flink_taskmanager_job_task_operator_numRecordsOut");
                } catch (Exception e) {
                    logger.error("[KAFKA SINK] ✗ ERROR creating producer: {}: {}", e.getClass().getName(), e.getMessage(), e);
                    throw e;
                }
            }
            
            try {
                logger.debug("[KAFKA SINK] Sending message to topic '{}': {}", topic, value);
                var record = new org.apache.kafka.clients.producer.ProducerRecord<String, String>(topic, value);
                producer.send(record);
                logger.debug("[KAFKA SINK] ✓ Message sent successfully");
                
                // Track specific message ID (key-5000) for observability testing via logging
                // Messages follow format: "MESSAGE 5000" after uppercase transformation
                if (value != null && value.toUpperCase().contains("MESSAGE 5000")) {
                    logger.info("[PROMETHEUS TRACKING] ✓ Tracked message 'key-5000' processed!");
                    logger.info("[PROMETHEUS TRACKING]   Monitor via: flink_taskmanager_job_task_operator_numRecordsOut");
                    logger.info("[PROMETHEUS TRACKING]   Value: {}", value);
                }
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

    /**
     * UnifiedSinkV2 wrapper for Kafka that implements Flink's Sink API v2.
     * This bridges the C# Unified Sink API to Flink's native org.apache.flink.api.connector.sink2.Sink interface.
     */
    public static class UnifiedSinkV2KafkaWrapper implements org.apache.flink.api.connector.sink2.Sink<String> {
        private static final Logger logger = LoggerFactory.getLogger(UnifiedSinkV2KafkaWrapper.class);
        private final String topic;
        private final Properties kafkaProps;
        private final UnifiedSinkV2Definition config;

        public UnifiedSinkV2KafkaWrapper(String topic, Properties kafkaProps, UnifiedSinkV2Definition config) {
            this.topic = topic;
            this.kafkaProps = kafkaProps;
            this.config = config;
        }

        @Override
        public org.apache.flink.api.connector.sink2.SinkWriter<String> createWriter(org.apache.flink.api.connector.sink2.WriterInitContext context) throws java.io.IOException {
            logger.info("[UNIFIED SINK V2] Creating SinkWriter for Kafka");
            logger.info("[UNIFIED SINK V2]   - Topic: {}", topic);
            logger.info("[UNIFIED SINK V2]   - Subtask: {}/{}", 
                context.getTaskInfo().getIndexOfThisSubtask(), 
                context.getTaskInfo().getNumberOfParallelSubtasks());
            return new UnifiedSinkV2KafkaWriter(topic, kafkaProps, config);
        }
    }

    /**
     * SinkWriter implementation for Kafka using Unified Sink API v2.
     * Handles writing elements to Kafka and manages producer lifecycle.
     */
    public static class UnifiedSinkV2KafkaWriter implements org.apache.flink.api.connector.sink2.SinkWriter<String> {
        private static final Logger logger = LoggerFactory.getLogger(UnifiedSinkV2KafkaWriter.class);
        private final String topic;
        private final Properties kafkaProps;
        private final UnifiedSinkV2Definition config;
        private org.apache.kafka.clients.producer.KafkaProducer<String, String> producer;

        public UnifiedSinkV2KafkaWriter(String topic, Properties kafkaProps, UnifiedSinkV2Definition config) {
            this.topic = topic;
            this.kafkaProps = kafkaProps;
            this.config = config;
            
            // Initialize Kafka producer
            try {
                logger.info("[UNIFIED SINK V2 WRITER] Initializing Kafka producer");
                this.producer = new org.apache.kafka.clients.producer.KafkaProducer<>(
                    kafkaProps,
                    new org.apache.kafka.common.serialization.StringSerializer(),
                    new org.apache.kafka.common.serialization.StringSerializer()
                );
                logger.info("[UNIFIED SINK V2 WRITER] ✓ Kafka producer initialized successfully");
            } catch (Exception e) {
                logger.error("[UNIFIED SINK V2 WRITER] ✗ ERROR initializing producer: {}", e.getMessage(), e);
                throw new RuntimeException("Failed to initialize Kafka producer", e);
            }
        }

        @Override
        public void write(String element, Context context) {
            try {
                logger.debug("[UNIFIED SINK V2 WRITER] Writing element to topic '{}': {}", topic, element);
                var record = new org.apache.kafka.clients.producer.ProducerRecord<String, String>(topic, element);
                producer.send(record);
                logger.debug("[UNIFIED SINK V2 WRITER] ✓ Element written successfully");
            } catch (Exception e) {
                logger.error("[UNIFIED SINK V2 WRITER] ✗ ERROR writing element: {}", e.getMessage(), e);
                throw new RuntimeException("Failed to write element to Kafka", e);
            }
        }

        @Override
        public void flush(boolean endOfInput) {
            try {
                logger.debug("[UNIFIED SINK V2 WRITER] Flushing producer (endOfInput={})", endOfInput);
                if (producer != null) {
                    producer.flush();
                }
                logger.debug("[UNIFIED SINK V2 WRITER] ✓ Flush completed");
            } catch (Exception e) {
                logger.error("[UNIFIED SINK V2 WRITER] ✗ ERROR during flush: {}", e.getMessage(), e);
                throw new RuntimeException("Failed to flush Kafka producer", e);
            }
        }

        @Override
        public void close() {
            try {
                logger.info("[UNIFIED SINK V2 WRITER] Closing Kafka producer");
                if (producer != null) {
                    producer.flush();
                    producer.close();
                    logger.info("[UNIFIED SINK V2 WRITER] ✓ Producer closed successfully");
                }
            } catch (Exception e) {
                logger.error("[UNIFIED SINK V2 WRITER] ✗ ERROR closing producer: {}", e.getMessage(), e);
            }
        }
    }
}
