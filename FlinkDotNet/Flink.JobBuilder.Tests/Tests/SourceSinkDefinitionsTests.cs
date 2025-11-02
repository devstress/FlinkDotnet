using Flink.JobBuilder.Models;

namespace Flink.JobBuilder.Tests.Tests;

[TestFixture]
public class SourceSinkDefinitionsTests
{
    #region Source Definition Tests

    [Test]
    public void HttpSourceDefinition_TypeProperty_ReturnsHttp()
    {
        var source = new HttpSourceDefinition();

        Assert.That(source.Type, Is.EqualTo("http"));
    }

    [Test]
    public void HttpSourceDefinition_SetAllProperties_ReturnsValues()
    {
        var headers = new Dictionary<string, string> { { "Auth", "Bearer token" } };
        var properties = new Dictionary<string, string> { { "key", "value" } };

        var source = new HttpSourceDefinition
        {
            Url = "http://api.example.com/data",
            Method = "POST",
            Headers = headers,
            Body = "{\"query\": \"test\"}",
            IntervalSeconds = 30,
            AuthTokenStateKey = "auth_token",
            Properties = properties
        };

        Assert.That(source.Url, Is.EqualTo("http://api.example.com/data"));
        Assert.That(source.Method, Is.EqualTo("POST"));
        Assert.That(source.Headers, Is.EqualTo(headers));
        Assert.That(source.Body, Is.EqualTo("{\"query\": \"test\"}"));
        Assert.That(source.IntervalSeconds, Is.EqualTo(30));
        Assert.That(source.AuthTokenStateKey, Is.EqualTo("auth_token"));
        Assert.That(source.Properties, Is.EqualTo(properties));
    }

    [Test]
    public void HttpSourceDefinition_DefaultMethod_IsGet()
    {
        var source = new HttpSourceDefinition();

        Assert.That(source.Method, Is.EqualTo("GET"));
    }

    [Test]
    public void HttpSourceDefinition_DefaultIntervalSeconds_Is60()
    {
        var source = new HttpSourceDefinition();

        Assert.That(source.IntervalSeconds, Is.EqualTo(60));
    }

    [Test]
    public void DatabaseSourceDefinition_TypeProperty_ReturnsDatabase()
    {
        var source = new DatabaseSourceDefinition();

        Assert.That(source.Type, Is.EqualTo("database"));
    }

    [Test]
    public void DatabaseSourceDefinition_SetAllProperties_ReturnsValues()
    {
        var properties = new Dictionary<string, string> { { "key", "value" } };

        var source = new DatabaseSourceDefinition
        {
            ConnectionString = "Server=localhost;Database=test",
            Query = "SELECT * FROM users WHERE active = true",
            DatabaseType = "mysql",
            PollingIntervalSeconds = 45,
            Properties = properties
        };

        Assert.That(source.ConnectionString, Is.EqualTo("Server=localhost;Database=test"));
        Assert.That(source.Query, Is.EqualTo("SELECT * FROM users WHERE active = true"));
        Assert.That(source.DatabaseType, Is.EqualTo("mysql"));
        Assert.That(source.PollingIntervalSeconds, Is.EqualTo(45));
        Assert.That(source.Properties, Is.EqualTo(properties));
    }

    [Test]
    public void DatabaseSourceDefinition_DefaultDatabaseType_IsPostgresql()
    {
        var source = new DatabaseSourceDefinition();

        Assert.That(source.DatabaseType, Is.EqualTo("postgresql"));
    }

    [Test]
    public void DatabaseSourceDefinition_DefaultPollingInterval_Is30()
    {
        var source = new DatabaseSourceDefinition();

        Assert.That(source.PollingIntervalSeconds, Is.EqualTo(30));
    }

    [Test]
    public void FileSourceDefinition_TypeProperty_ReturnsFile()
    {
        var source = new FileSourceDefinition();

        Assert.That(source.Type, Is.EqualTo("file"));
    }

    [Test]
    public void FileSourceDefinition_SetAllProperties_ReturnsValues()
    {
        var properties = new Dictionary<string, string> { { "key", "value" } };

        var source = new FileSourceDefinition
        {
            Path = "/data/input",
            Format = "json",
            Properties = properties
        };

        Assert.That(source.Path, Is.EqualTo("/data/input"));
        Assert.That(source.Format, Is.EqualTo("json"));
        Assert.That(source.Properties, Is.EqualTo(properties));
    }

    [Test]
    public void FileSourceDefinition_DefaultFormat_IsText()
    {
        var source = new FileSourceDefinition();

        Assert.That(source.Format, Is.EqualTo("text"));
    }

    [Test]
    public void SqlSourceDefinition_TypeProperty_ReturnsSql()
    {
        var source = new SqlSourceDefinition();

        Assert.That(source.Type, Is.EqualTo("sql"));
    }

    [Test]
    public void SqlSourceDefinition_SetAllProperties_ReturnsValues()
    {
        var statements = new List<string> { "CREATE TABLE", "INSERT INTO" };
        var properties = new Dictionary<string, string> { { "key", "value" } };

        var source = new SqlSourceDefinition
        {
            Statements = statements,
            Mode = "batch",
            ExecutionMode = "gateway",
            Properties = properties
        };

        Assert.That(source.Statements, Is.EqualTo(statements));
        Assert.That(source.Mode, Is.EqualTo("batch"));
        Assert.That(source.ExecutionMode, Is.EqualTo("gateway"));
        Assert.That(source.Properties, Is.EqualTo(properties));
    }

    [Test]
    public void SqlSourceDefinition_DefaultMode_IsStreaming()
    {
        var source = new SqlSourceDefinition();

        Assert.That(source.Mode, Is.EqualTo("streaming"));
    }

    [Test]
    public void SqlSourceDefinition_DefaultExecutionMode_IsTableenv()
    {
        var source = new SqlSourceDefinition();

        Assert.That(source.ExecutionMode, Is.EqualTo("tableenv"));
    }

    [Test]
    public void KafkaSourceDefinition_StartingOffsets_CanBeSet()
    {
        var source = new KafkaSourceDefinition
        {
            Topic = "test-topic",
            StartingOffsets = "latest"
        };

        Assert.That(source.StartingOffsets, Is.EqualTo("latest"));
    }

    [Test]
    public void KafkaSourceDefinition_DefaultStartingOffsets_IsEarliest()
    {
        var source = new KafkaSourceDefinition();

        Assert.That(source.StartingOffsets, Is.EqualTo("earliest"));
    }

    [Test]
    public void KafkaSourceDefinition_Properties_CanStoreMultipleValues()
    {
        var source = new KafkaSourceDefinition();
        source.Properties["enable.auto.commit"] = "false";
        source.Properties["max.poll.records"] = "500";

        Assert.That(source.Properties["enable.auto.commit"], Is.EqualTo("false"));
        Assert.That(source.Properties["max.poll.records"], Is.EqualTo("500"));
    }

    [Test]
    public void KafkaSourceDefinition_TypeProperty_ReturnsKafka()
    {
        var source = new KafkaSourceDefinition();

        Assert.That(source.Type, Is.EqualTo("kafka"));
    }

    [Test]
    public void KafkaSourceDefinition_GroupId_SupportsNull()
    {
        var source = new KafkaSourceDefinition
        {
            Topic = "test-topic",
            GroupId = null
        };

        Assert.That(source.GroupId, Is.Null);
    }

    [Test]
    public void KafkaSourceDefinition_BootstrapServers_SupportsNull()
    {
        var source = new KafkaSourceDefinition
        {
            Topic = "test-topic",
            BootstrapServers = null
        };

        Assert.That(source.BootstrapServers, Is.Null);
    }

    #endregion

    #region Sink Definition Tests

    [Test]
    public void ConsoleSinkDefinition_TypeProperty_ReturnsConsole()
    {
        var sink = new ConsoleSinkDefinition();

        Assert.That(sink.Type, Is.EqualTo("console"));
    }

    [Test]
    public void ConsoleSinkDefinition_SetFormat_ReturnsValue()
    {
        var sink = new ConsoleSinkDefinition
        {
            Format = "text"
        };

        Assert.That(sink.Format, Is.EqualTo("text"));
    }

    [Test]
    public void ConsoleSinkDefinition_DefaultFormat_IsJson()
    {
        var sink = new ConsoleSinkDefinition();

        Assert.That(sink.Format, Is.EqualTo("json"));
    }

    [Test]
    public void FileSinkDefinition_TypeProperty_ReturnsFile()
    {
        var sink = new FileSinkDefinition();

        Assert.That(sink.Type, Is.EqualTo("file"));
    }

    [Test]
    public void FileSinkDefinition_SetAllProperties_ReturnsValues()
    {
        var properties = new Dictionary<string, string> { { "key", "value" } };

        var sink = new FileSinkDefinition
        {
            Path = "/data/output",
            Format = "csv",
            Properties = properties
        };

        Assert.That(sink.Path, Is.EqualTo("/data/output"));
        Assert.That(sink.Format, Is.EqualTo("csv"));
        Assert.That(sink.Properties, Is.EqualTo(properties));
    }

    [Test]
    public void FileSinkDefinition_DefaultFormat_IsJson()
    {
        var sink = new FileSinkDefinition();

        Assert.That(sink.Format, Is.EqualTo("json"));
    }

    [Test]
    public void DatabaseSinkDefinition_TypeProperty_ReturnsDatabase()
    {
        var sink = new DatabaseSinkDefinition();

        Assert.That(sink.Type, Is.EqualTo("database"));
    }

    [Test]
    public void DatabaseSinkDefinition_SetAllProperties_ReturnsValues()
    {
        var properties = new Dictionary<string, string> { { "key", "value" } };

        var sink = new DatabaseSinkDefinition
        {
            ConnectionString = "Server=localhost;Database=test",
            Table = "output_table",
            DatabaseType = "sqlserver",
            Properties = properties
        };

        Assert.That(sink.ConnectionString, Is.EqualTo("Server=localhost;Database=test"));
        Assert.That(sink.Table, Is.EqualTo("output_table"));
        Assert.That(sink.DatabaseType, Is.EqualTo("sqlserver"));
        Assert.That(sink.Properties, Is.EqualTo(properties));
    }

    [Test]
    public void DatabaseSinkDefinition_DefaultDatabaseType_IsPostgresql()
    {
        var sink = new DatabaseSinkDefinition();

        Assert.That(sink.DatabaseType, Is.EqualTo("postgresql"));
    }

    [Test]
    public void HttpSinkDefinition_TypeProperty_ReturnsHttp()
    {
        var sink = new HttpSinkDefinition();

        Assert.That(sink.Type, Is.EqualTo("http"));
    }

    [Test]
    public void HttpSinkDefinition_SetAllProperties_ReturnsValues()
    {
        var headers = new Dictionary<string, string> { { "Content-Type", "application/json" } };
        var properties = new Dictionary<string, string> { { "key", "value" } };

        var sink = new HttpSinkDefinition
        {
            Url = "http://webhook.example.com",
            Method = "PUT",
            Headers = headers,
            Properties = properties
        };

        Assert.That(sink.Url, Is.EqualTo("http://webhook.example.com"));
        Assert.That(sink.Method, Is.EqualTo("PUT"));
        Assert.That(sink.Headers, Is.EqualTo(headers));
        Assert.That(sink.Properties, Is.EqualTo(properties));
    }

    [Test]
    public void HttpSinkDefinition_DefaultMethod_IsPost()
    {
        var sink = new HttpSinkDefinition();

        Assert.That(sink.Method, Is.EqualTo("POST"));
    }

    [Test]
    public void HttpSinkDefinition_BodyTemplate_SupportsNull()
    {
        var sink = new HttpSinkDefinition
        {
            Url = "http://webhook.example.com",
            BodyTemplate = null
        };

        Assert.That(sink.BodyTemplate, Is.Null);
    }

    [Test]
    public void HttpSinkDefinition_BodyTemplate_CanBeSet()
    {
        var sink = new HttpSinkDefinition
        {
            Url = "http://webhook.example.com",
            BodyTemplate = "{\"data\": \"{value}\"}"
        };

        Assert.That(sink.BodyTemplate, Is.EqualTo("{\"data\": \"{value}\"}"));
    }

    [Test]
    public void HttpSinkDefinition_AuthTokenStateKey_SupportsNull()
    {
        var sink = new HttpSinkDefinition
        {
            Url = "http://webhook.example.com",
            AuthTokenStateKey = null
        };

        Assert.That(sink.AuthTokenStateKey, Is.Null);
    }

    [Test]
    public void HttpSinkDefinition_AuthTokenStateKey_CanBeSet()
    {
        var sink = new HttpSinkDefinition
        {
            Url = "http://webhook.example.com",
            AuthTokenStateKey = "auth_token"
        };

        Assert.That(sink.AuthTokenStateKey, Is.EqualTo("auth_token"));
    }

    [Test]
    public void HttpSinkDefinition_DefaultTimeoutMs_Is5000()
    {
        var sink = new HttpSinkDefinition();

        Assert.That(sink.TimeoutMs, Is.EqualTo(5000));
    }

    [Test]
    public void HttpSinkDefinition_TimeoutMs_CanBeSet()
    {
        var sink = new HttpSinkDefinition
        {
            Url = "http://webhook.example.com",
            TimeoutMs = 10000
        };

        Assert.That(sink.TimeoutMs, Is.EqualTo(10000));
    }

    [Test]
    public void RedisSinkDefinition_TypeProperty_ReturnsRedis()
    {
        var sink = new RedisSinkDefinition();

        Assert.That(sink.Type, Is.EqualTo("redis"));
    }

    [Test]
    public void RedisSinkDefinition_SetAllProperties_ReturnsValues()
    {
        var configuration = new Dictionary<string, object> { { "ttl", 3600 } };

        var sink = new RedisSinkDefinition
        {
            ConnectionString = "redis://localhost:6379",
            Key = "flink:{id}",
            OperationType = "set",
            Configuration = configuration
        };

        Assert.That(sink.ConnectionString, Is.EqualTo("redis://localhost:6379"));
        Assert.That(sink.Key, Is.EqualTo("flink:{id}"));
        Assert.That(sink.OperationType, Is.EqualTo("set"));
        Assert.That(sink.Configuration, Is.EqualTo(configuration));
    }

    [Test]
    public void RedisSinkDefinition_DefaultOperationType_IsIncrement()
    {
        var sink = new RedisSinkDefinition();

        Assert.That(sink.OperationType, Is.EqualTo("increment"));
    }

    [Test]
    public void RedisSinkDefinition_Key_SupportsNull()
    {
        var sink = new RedisSinkDefinition
        {
            Key = null
        };

        Assert.That(sink.Key, Is.Null);
    }

    [Test]
    public void KafkaSinkDefinition_Serializer_CanBeSet()
    {
        var sink = new KafkaSinkDefinition
        {
            Topic = "output-topic",
            Serializer = "avro"
        };

        Assert.That(sink.Serializer, Is.EqualTo("avro"));
    }

    [Test]
    public void KafkaSinkDefinition_DefaultSerializer_IsJson()
    {
        var sink = new KafkaSinkDefinition();

        Assert.That(sink.Serializer, Is.EqualTo("json"));
    }

    [Test]
    public void KafkaSinkDefinition_TypeProperty_ReturnsKafka()
    {
        var sink = new KafkaSinkDefinition();

        Assert.That(sink.Type, Is.EqualTo("kafka"));
    }

    [Test]
    public void KafkaSinkDefinition_BootstrapServers_SupportsNull()
    {
        var sink = new KafkaSinkDefinition
        {
            Topic = "output-topic",
            BootstrapServers = null
        };

        Assert.That(sink.BootstrapServers, Is.Null);
    }

    [Test]
    public void KafkaSinkDefinition_Properties_CanStoreMultipleValues()
    {
        var sink = new KafkaSinkDefinition();
        sink.Properties["compression.type"] = "gzip";
        sink.Properties["batch.size"] = "16384";

        Assert.That(sink.Properties["compression.type"], Is.EqualTo("gzip"));
        Assert.That(sink.Properties["batch.size"], Is.EqualTo("16384"));
    }

    #endregion

    #region JobMetadata Tests

    [Test]
    public void JobMetadata_JobName_SupportsNull()
    {
        var metadata = new JobMetadata
        {
                        JobName = null
        };

        Assert.That(metadata.JobName, Is.Null);
    }

    [Test]
    public void JobMetadata_JobName_CanBeSet()
    {
        var metadata = new JobMetadata
        {
                        JobName = "My Flink Job"
        };

        Assert.That(metadata.JobName, Is.EqualTo("My Flink Job"));
    }

    [Test]
    public void JobMetadata_Parallelism_SupportsNull()
    {
        var metadata = new JobMetadata
        {
            Parallelism = null
        };

        Assert.That(metadata.Parallelism, Is.Null);
    }

    [Test]
    public void JobMetadata_Parallelism_CanBeSet()
    {
        var metadata = new JobMetadata
        {
            Parallelism = 4
        };

        Assert.That(metadata.Parallelism, Is.EqualTo(4));
    }

    [Test]
    public void JobMetadata_CreatedAt_CanBeSet()
    {
        var timestamp = DateTime.UtcNow;
        var metadata = new JobMetadata
        {
            CreatedAt = timestamp
        };

        Assert.That(metadata.CreatedAt, Is.EqualTo(timestamp));
    }

    [Test]
    public void JobMetadata_Properties_CanStoreMultipleValues()
    {
        var metadata = new JobMetadata();
        metadata.Properties["environment"] = "production";
        metadata.Properties["owner"] = "team-data";

        Assert.That(metadata.Properties["environment"], Is.EqualTo("production"));
        Assert.That(metadata.Properties["owner"], Is.EqualTo("team-data"));
    }

    #endregion
}
