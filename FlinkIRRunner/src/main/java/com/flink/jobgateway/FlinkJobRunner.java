package com.flink.jobgateway;

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
    public static void main(String[] args) throws Exception {
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
            String bootstrap = orElse(k.bootstrapServers, System.getenv("KAFKA_BOOTSTRAP"), "kafka:9092");
            String groupId = orElse(k.groupId, "flinkdotnet-ir-runner");

            System.out.println("============================================================");
            System.out.println("[KAFKA SOURCE] Configuration:");
            System.out.println("  - bootstrapServers field from JSON: " + k.bootstrapServers);
            System.out.println("  - KAFKA_BOOTSTRAP environment: " + System.getenv("KAFKA_BOOTSTRAP"));
            System.out.println("  - FINAL bootstrap.servers: " + bootstrap);
            System.out.println("  - Topic: " + k.topic);
            System.out.println("  - GroupId: " + groupId);
            System.out.println("  - Starting offsets: " + orElse(k.startingOffsets, "latest"));
            System.out.println("============================================================");

            Properties props = new Properties();
            props.put("bootstrap.servers", bootstrap);
            props.put("group.id", groupId);
            props.put("auto.offset.reset", orElse(k.startingOffsets, "latest"));
            
            System.out.println("[KAFKA SOURCE] Creating Kafka consumer with properties:");
            System.out.println("  - bootstrap.servers: " + props.getProperty("bootstrap.servers"));
            System.out.println("  - group.id: " + props.getProperty("group.id"));
            System.out.println("  - auto.offset.reset: " + props.getProperty("auto.offset.reset"));

            stream = env.addSource(new KafkaStringSource(k.topic, props)).name("KafkaSource");
            System.out.println("[KAFKA SOURCE] Source created successfully");
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
                    System.out.println("============================================================");
                    System.out.println("[MAP OPERATION] Processing:");
                    System.out.println("  - expression field from JSON: " + m.expression);
                    System.out.println("  - function field from JSON: " + m.function);
                    System.out.println("  - Resolved expression: " + expr);
                    System.out.println("  - Normalized (lowercase): " + expr.toLowerCase(Locale.ROOT));
                    System.out.println("============================================================");
                    
                    switch (expr.toLowerCase(Locale.ROOT)) {
                        case "upper":
                        case "toupper":
                            System.out.println("[MAP OPERATION] ✓ Applying toUpperCase transformation");
                            stream = stream.map(String::toUpperCase);
                            break;
                        case "lower":
                        case "tolower":
                            System.out.println("[MAP OPERATION] ✓ Applying toLowerCase transformation");
                            stream = stream.map(String::toLowerCase);
                            break;
                        default:
                            System.out.println("[MAP OPERATION] ⚠ Using identity transformation (pass-through) for: " + expr);
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
                        String bootstrap = orElse(so.sideOutputSink.bootstrapServers, System.getenv("KAFKA_BOOTSTRAP"), "kafka:9093");
                        Properties props = new Properties();
                        props.put("bootstrap.servers", bootstrap);
                        side.addSink(new KafkaStringSink(so.sideOutputSink.topic, props)).name("SideKafkaSink:"+so.outputTag);
                    }
                    stream = main;
                }
            }
        }

        if (ir.sink instanceof KafkaSinkDefinition) {
            KafkaSinkDefinition s = (KafkaSinkDefinition) ir.sink;
            String bootstrap = orElse(s.bootstrapServers,
                    (ir.source instanceof KafkaSourceDefinition) ? ((KafkaSourceDefinition) ir.source).bootstrapServers : null,
                    System.getenv("KAFKA_BOOTSTRAP"), "kafka:9092");

            System.out.println("============================================================");
            System.out.println("[KAFKA SINK] Configuration:");
            System.out.println("  - bootstrapServers field from JSON: " + s.bootstrapServers);
            System.out.println("  - Source bootstrapServers: " + ((ir.source instanceof KafkaSourceDefinition) ? ((KafkaSourceDefinition) ir.source).bootstrapServers : "N/A"));
            System.out.println("  - KAFKA_BOOTSTRAP environment: " + System.getenv("KAFKA_BOOTSTRAP"));
            System.out.println("  - FINAL bootstrap.servers: " + bootstrap);
            System.out.println("  - Topic: " + s.topic);
            System.out.println("============================================================");

            Properties props = new Properties();
            props.put("bootstrap.servers", bootstrap);
            
            System.out.println("[KAFKA SINK] Creating Kafka producer with properties:");
            System.out.println("  - bootstrap.servers: " + props.getProperty("bootstrap.servers"));
            System.out.println("  - Target topic: " + s.topic);
            
            stream.addSink(new KafkaStringSink(s.topic, props)).name("KafkaSink");
            System.out.println("[KAFKA SINK] Sink created successfully");
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
        @JsonSubTypes.Type(value = SideOutputOperationDefinition.class, name = "side-output")
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
            System.out.println("============================================================");
            System.out.println("[KAFKA SOURCE] Starting consumer...");
            System.out.println("  - Topic: " + topic);
            System.out.println("  - Bootstrap servers: " + props.getProperty("bootstrap.servers"));
            System.out.println("  - Group ID: " + props.getProperty("group.id"));
            System.out.println("  - Auto offset reset: " + props.getProperty("auto.offset.reset"));
            System.out.println("============================================================");
            
            try (org.apache.kafka.clients.consumer.KafkaConsumer<String, String> consumer = new org.apache.kafka.clients.consumer.KafkaConsumer<>(props, new org.apache.kafka.common.serialization.StringDeserializer(), new org.apache.kafka.common.serialization.StringDeserializer())) {
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
                        // Log every 10 seconds (20 polls * 500ms) to show we're still polling
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
                System.out.println("============================================================");
                System.out.println("[KAFKA SINK] Initializing producer...");
                System.out.println("  - Topic: " + topic);
                System.out.println("  - Bootstrap servers: " + props.getProperty("bootstrap.servers"));
                System.out.println("============================================================");
                
                try {
                    producer = new org.apache.kafka.clients.producer.KafkaProducer<>(props, new org.apache.kafka.common.serialization.StringSerializer(), new org.apache.kafka.common.serialization.StringSerializer());
                    System.out.println("[KAFKA SINK] ✓ Producer created successfully");
                } catch (Exception e) {
                    System.err.println("[KAFKA SINK] ✗ ERROR creating producer: " + e.getClass().getName() + ": " + e.getMessage());
                    e.printStackTrace();
                    throw e;
                }
            }
            
            try {
                System.out.println("[KAFKA SINK] Sending message to topic '" + topic + "': " + value);
                producer.send(new org.apache.kafka.clients.producer.ProducerRecord<>(topic, value));
                System.out.println("[KAFKA SINK] ✓ Message sent successfully");
            } catch (Exception e) {
                System.err.println("[KAFKA SINK] ✗ ERROR sending message: " + e.getClass().getName() + ": " + e.getMessage());
                e.printStackTrace();
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
