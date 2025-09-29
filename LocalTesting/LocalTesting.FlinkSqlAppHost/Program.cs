// Basic environment setup
Environment.SetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT", "true");

// Set up Aspire dashboard configuration for testing
Environment.SetEnvironmentVariable("ASPNETCORE_URLS", "http://localhost:15888");
Environment.SetEnvironmentVariable("ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL", "http://localhost:16686");
Environment.SetEnvironmentVariable("ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL", "http://localhost:16687");

var diagnosticsVerbose = Environment.GetEnvironmentVariable("DIAGNOSTICS_VERBOSE") == "1";
if (diagnosticsVerbose)
{
    Console.WriteLine("[diag] DIAGNOSTICS_VERBOSE=1 enabled for LocalTesting.FlinkSqlAppHost startup diagnostics");
}

const int JobManagerHostPort = 18081;
const int JobManagerRpcPort = 8081;
const string JavaOpenOptions = "--add-opens=java.base/java.lang=ALL-UNNAMED --add-opens=java.base/java.net=ALL-UNNAMED --add-opens=java.base/java.io=ALL-UNNAMED --add-opens=java.base/java.nio=ALL-UNNAMED --add-opens=java.base/sun.nio.ch=ALL-UNNAMED --add-opens=java.base/java.lang.reflect=ALL-UNNAMED --add-opens=java.base/java.text=ALL-UNNAMED --add-opens=java.base/java.time=ALL-UNNAMED --add-opens=java.base/java.util=ALL-UNNAMED --add-opens=java.base/java.util.concurrent=ALL-UNNAMED --add-opens=java.base/java.util.concurrent.atomic=ALL-UNNAMED --add-opens=java.base/java.util.concurrent.locks=ALL-UNNAMED";

var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
var connectorsDir = Path.Combine(repoRoot, "LocalTesting", "connectors", "flink", "lib");

try
{
    Directory.CreateDirectory(connectorsDir);
    if (diagnosticsVerbose)
    {
        Console.WriteLine($"[diag] Connector directory ready at {connectorsDir}");
    }
}
catch (Exception ex)
{
    if (diagnosticsVerbose)
    {
        Console.WriteLine($"[diag][warn] Connector dir prep failed: {ex.Message}");
    }
}

var builder = DistributedApplication.CreateBuilder(args);

// Set up Kafka with optimized configuration for LocalTesting
var kafka = builder.AddKafka("kafka")
    .WithEnvironment("KAFKA_REST_SCHEMA_REGISTRY_URL", "")
    .WithEnvironment("SCHEMA_REGISTRY_URL", "")
    .WithEnvironment("KAFKA_UNUSED_SUPPRESS", "1")
    .WithEnvironment("KAFKA_HEAP_OPTS", "-Xmx1G -Xms1G")
    .WithEnvironment("KAFKA_CFG_LISTENERS", "PLAINTEXT://0.0.0.0:9092,EXTERNAL://0.0.0.0:29092,CONTROLLER://0.0.0.0:29093")
    .WithEnvironment("KAFKA_LISTENERS", "PLAINTEXT://0.0.0.0:9092,EXTERNAL://0.0.0.0:29092,CONTROLLER://0.0.0.0:29093")
    .WithEnvironment("KAFKA_CFG_ADVERTISED_LISTENERS", "PLAINTEXT://kafka:9092,EXTERNAL://localhost:29092")
    .WithEnvironment("KAFKA_ADVERTISED_LISTENERS", "PLAINTEXT://kafka:9092,EXTERNAL://localhost:29092")
    .WithEnvironment("KAFKA_CFG_LISTENER_SECURITY_PROTOCOL_MAP", "PLAINTEXT:PLAINTEXT,EXTERNAL:PLAINTEXT,CONTROLLER:PLAINTEXT")
    .WithEnvironment("KAFKA_LISTENER_SECURITY_PROTOCOL_MAP", "PLAINTEXT:PLAINTEXT,EXTERNAL:PLAINTEXT,CONTROLLER:PLAINTEXT")
    .WithEnvironment("KAFKA_CFG_INTER_BROKER_LISTENER_NAME", "PLAINTEXT")
    .WithEnvironment("KAFKA_INTER_BROKER_LISTENER_NAME", "PLAINTEXT")
    .WithEnvironment("KAFKA_CFG_CONTROLLER_LISTENER_NAMES", "CONTROLLER")
    .WithEnvironment("KAFKA_CONTROLLER_LISTENER_NAMES", "CONTROLLER")
    .WithEnvironment("KAFKA_CFG_CONTROLLER_QUORUM_VOTERS", "1@kafka:29093")
    .WithEnvironment("KAFKA_CONTROLLER_QUORUM_VOTERS", "1@kafka:29093");
kafka = kafka.WithEnvironment(context =>
{
    var env = context.EnvironmentVariables;
    env["KAFKA_CFG_LISTENERS"] = "PLAINTEXT://:9092,EXTERNAL://:29092,CONTROLLER://:29093";
    env["KAFKA_LISTENERS"] = "PLAINTEXT://:9092,EXTERNAL://:29092,CONTROLLER://:29093";
    env["KAFKA_CFG_ADVERTISED_LISTENERS"] = "PLAINTEXT://kafka:9092,EXTERNAL://localhost:29092,CONTROLLER://kafka:29093";
    env["KAFKA_ADVERTISED_LISTENERS"] = "PLAINTEXT://kafka:9092,EXTERNAL://localhost:29092,CONTROLLER://kafka:29093";
    env["KAFKA_CFG_LISTENER_SECURITY_PROTOCOL_MAP"] = "PLAINTEXT:PLAINTEXT,EXTERNAL:PLAINTEXT,CONTROLLER:PLAINTEXT";
    env["KAFKA_LISTENER_SECURITY_PROTOCOL_MAP"] = "PLAINTEXT:PLAINTEXT,EXTERNAL:PLAINTEXT,CONTROLLER:PLAINTEXT";
    env["KAFKA_CFG_INTER_BROKER_LISTENER_NAME"] = "PLAINTEXT";
    env["KAFKA_INTER_BROKER_LISTENER_NAME"] = "PLAINTEXT";
    env["KAFKA_CFG_CONTROLLER_LISTENER_NAMES"] = "CONTROLLER";
    env["KAFKA_CONTROLLER_LISTENER_NAMES"] = "CONTROLLER";
    env["KAFKA_CFG_CONTROLLER_QUORUM_VOTERS"] = "1@kafka:29093";
    env["KAFKA_CONTROLLER_QUORUM_VOTERS"] = "1@kafka:29093";
});

if (diagnosticsVerbose)
{
    var envProp = kafka.Resource.GetType().GetProperty("EnvironmentVariables");
    if (envProp?.GetValue(kafka.Resource) is System.Collections.Generic.IDictionary<string, string> env)
    {
        Console.WriteLine("[diag] Kafka container environment:");
        foreach (var kv in env)
        {
            Console.WriteLine($"[diag]   {kv.Key}={kv.Value}");
        }
    }
    else
    {
        Console.WriteLine("[diag] Unable to reflect Kafka environment dictionary");
        Console.WriteLine($"[diag] Kafka resource type: {kafka.Resource.GetType()} from {kafka.Resource.GetType().Assembly.Location}");
        foreach (var prop in kafka.Resource.GetType().GetProperties())
        {
            Console.WriteLine($"[diag] Resource property: {prop.Name} ({prop.PropertyType})");
            if (prop.Name == "Annotations" && prop.GetValue(kafka.Resource) is System.Collections.IEnumerable annotations)
            {
                foreach (var ann in annotations)
                {
                    if (ann is null) continue;
                    Console.WriteLine($"[diag]   Annotation: {ann}");
                    var annType = ann.GetType();
                    foreach (var ap in annType.GetProperties())
                    {
                        object value;
                        try
                        {
                            value = ap.GetValue(ann);
                        }
                        catch
                        {
                            continue;
                        }
                        Console.WriteLine($"[diag]     {ap.Name}={value}");
                        if (ap.Name == "Callback" && value is MulticastDelegate del)
                        {
                            var parameters = del.Method.GetParameters();
                            if (parameters.Length == 1)
                            {
                                var ctxType = parameters[0].ParameterType;
                                Console.WriteLine($"[diag]     Callback parameter type: {ctxType}");
                                foreach (var ctor in ctxType.GetConstructors())
                                {
                                    Console.WriteLine($"[diag]     ctor: {ctor}");
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}


var includeGatewaySetting = Environment.GetEnvironmentVariable("INCLUDE_FLINK_GATEWAY");
var includeGateway = includeGatewaySetting switch
{
    null => true,
    "" => true,
    var s when string.Equals(s, "0", StringComparison.OrdinalIgnoreCase) => false,
    var s when string.Equals(s, "false", StringComparison.OrdinalIgnoreCase) => false,
    var s when string.Equals(s, "no", StringComparison.OrdinalIgnoreCase) => false,
    _ => true
};

// Set up Flink JobManager (single instance) with compatible JVM options
var jobManager = builder.AddContainer("flink-jobmanager", "flink:2.1.0-java17")
    .WithEndpoint("jobmanager-ui", endpoint =>
    {
        endpoint.Port = JobManagerHostPort;
        endpoint.TargetPort = JobManagerRpcPort;
        endpoint.UriScheme = "http";
        endpoint.IsProxied = false;
        endpoint.IsExternal = true;
    })
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithEnvironment("KAFKA_BOOTSTRAP", "kafka:9092")
    .WithEnvironment("FLINK_PROPERTIES", 
        "jobmanager.rpc.address: flink-jobmanager\n" +
        "rest.address: 0.0.0.0\n" +
        "rest.bind-address: 0.0.0.0\n" +
        "parallelism.default: 1\n" +
        "rest.port: 8081\n" +
        "rest.bind-port: 8081\n" +
        "jobmanager.memory.process.size: 1600m\n" +
        "env.java.opts.all: --add-opens=java.base/java.lang=ALL-UNNAMED --add-opens=java.base/java.net=ALL-UNNAMED --add-opens=java.base/java.io=ALL-UNNAMED --add-opens=java.base/java.nio=ALL-UNNAMED --add-opens=java.base/sun.nio.ch=ALL-UNNAMED --add-opens=java.base/java.lang.reflect=ALL-UNNAMED --add-opens=java.base/java.text=ALL-UNNAMED --add-opens=java.base/java.time=ALL-UNNAMED --add-opens=java.base/java.util=ALL-UNNAMED --add-opens=java.base/java.util.concurrent=ALL-UNNAMED --add-opens=java.base/java.util.concurrent.atomic=ALL-UNNAMED --add-opens=java.base/java.util.concurrent.locks=ALL-UNNAMED\n")
    .WithEnvironment("JAVA_TOOL_OPTIONS", JavaOpenOptions)
    .WithBindMount(connectorsDir, "/opt/flink/usrlib", isReadOnly: true)
    .WithArgs("jobmanager");

// Set up Flink TaskManager (single instance) with compatible JVM options
builder.AddContainer("flink-taskmanager", "flink:2.1.0-java17")
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithEnvironment("KAFKA_BOOTSTRAP", "kafka:9092")
    .WithEnvironment("TASK_MANAGER_NUMBER_OF_TASK_SLOTS", "2") // Allow parallel processing
    .WithEnvironment("FLINK_PROPERTIES", 
        "jobmanager.rpc.address: flink-jobmanager\n" +
        "rest.address: 0.0.0.0\n" +
        "rest.bind-address: 0.0.0.0\n" +
        "parallelism.default: 1\n" +
        "taskmanager.memory.process.size: 1728m\n" +
        "taskmanager.numberOfTaskSlots: 2\n" +
        "env.java.opts.all: --add-opens=java.base/java.lang=ALL-UNNAMED --add-opens=java.base/java.net=ALL-UNNAMED --add-opens=java.base/java.io=ALL-UNNAMED --add-opens=java.base/java.nio=ALL-UNNAMED --add-opens=java.base/sun.nio.ch=ALL-UNNAMED --add-opens=java.base/java.lang.reflect=ALL-UNNAMED --add-opens=java.base/java.text=ALL-UNNAMED --add-opens=java.base/java.time=ALL-UNNAMED --add-opens=java.base/java.util=ALL-UNNAMED --add-opens=java.base/java.util.concurrent=ALL-UNNAMED --add-opens=java.base/java.util.concurrent.atomic=ALL-UNNAMED --add-opens=java.base/java.util.concurrent.locks=ALL-UNNAMED\n")
    .WithEnvironment("JAVA_TOOL_OPTIONS", JavaOpenOptions)
    .WithBindMount(connectorsDir, "/opt/flink/usrlib", isReadOnly: true)
    .WithArgs("taskmanager")
    .WaitFor(jobManager);

if (includeGateway)
{
    builder.AddProject("flink-job-gateway", "../../FlinkDotNet/Flink.JobGateway/Flink.JobGateway.csproj")
        .WithEndpoint("http", endpoint =>
        {
            endpoint.Port = 8080;
            endpoint.TargetPort = 8080;
            endpoint.UriScheme = "http";
            endpoint.IsProxied = false;
            endpoint.IsExternal = true;
        }, createIfNotExists: false)
        .WithEnvironment("ASPNETCORE_URLS", "http://0.0.0.0:8080")
        .WithEnvironment("FLINK_CLUSTER_HOST", "localhost")
        .WithEnvironment("FLINK_CLUSTER_PORT", JobManagerHostPort.ToString())
        .WithEnvironment("FLINK_CONNECTOR_PATH", connectorsDir)
        .WithEnvironment("KAFKA_BOOTSTRAP", "kafka:9092")
        .WaitFor(jobManager)
        .WaitFor(kafka);
}
else if (diagnosticsVerbose)
{
    Console.WriteLine("[diag] INCLUDE_FLINK_GATEWAY toggled off; skipping Flink.JobGateway start");
}

await builder.Build().RunAsync();






