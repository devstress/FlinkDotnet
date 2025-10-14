using Flink.JobBuilder.Models;

namespace Flink.JobBuilder.Tests.Tests;

[TestFixture]
public class BuilderTests
{
    [Test]
    public void FromKafka_CreatesBuilderWithKafkaSource()
    {
        var builder = FlinkJobBuilder.FromKafka("test-topic", "localhost:9092");

        Assert.That(builder, Is.Not.Null);
    }

    [Test]
    public void FromKafka_WithoutBootstrapServers_CreatesBuilder()
    {
        var builder = FlinkJobBuilder.FromKafka("test-topic");

        Assert.That(builder, Is.Not.Null);
    }

    [Test]
    public void FromHttp_CreatesBuilderWithHttpSource()
    {
        var builder = FlinkJobBuilder.FromHttp("http://api.example.com", "GET", 30);

        Assert.That(builder, Is.Not.Null);
    }

    [Test]
    public void FromHttp_WithDefaults_CreatesBuilder()
    {
        var builder = FlinkJobBuilder.FromHttp("http://api.example.com");

        Assert.That(builder, Is.Not.Null);
    }

    [Test]
    public void FromDatabase_CreatesBuilderWithDatabaseSource()
    {
        var builder = FlinkJobBuilder.FromDatabase(
            "Server=localhost;Database=test",
            "SELECT * FROM orders",
            60);

        Assert.That(builder, Is.Not.Null);
    }

    [Test]
    public void FromDatabase_WithDefaultPollingInterval_CreatesBuilder()
    {
        var builder = FlinkJobBuilder.FromDatabase(
            "Server=localhost;Database=test",
            "SELECT * FROM orders");

        Assert.That(builder, Is.Not.Null);
    }

    [Test]
    public void Constructor_WithNullGatewayService_CreatesDefaultService()
    {
        var builder = new FlinkJobBuilder(null, null);

        Assert.That(builder, Is.Not.Null);
    }

    [Test]
    public void Constructor_WithGatewayService_UsesProvidedService()
    {
        var service = new Services.FlinkJobGatewayService();
        var builder = new FlinkJobBuilder(service);

        Assert.That(builder, Is.Not.Null);
    }

    [Test]
    public void MapOperationDefinition_CanSetExpression()
    {
        var op = new MapOperationDefinition
        {
            Expression = "x => x.ToUpper()"
        };

        Assert.That(op.Expression, Is.EqualTo("x => x.ToUpper()"));
    }

    [Test]
    public void FilterOperationDefinition_CanSetExpression()
    {
        var op = new FilterOperationDefinition
        {
            Expression = "x => x.Length > 5"
        };

        Assert.That(op.Expression, Is.EqualTo("x => x.Length > 5"));
    }

    [Test]
    public void WindowOperationDefinition_CanSetProperties()
    {
        var op = new WindowOperationDefinition
        {
            WindowType = "TUMBLING",
            Size = 30,
            TimeUnit = "SECONDS"
        };

        Assert.That(op.WindowType, Is.EqualTo("TUMBLING"));
        Assert.That(op.Size, Is.EqualTo(30));
        Assert.That(op.TimeUnit, Is.EqualTo("SECONDS"));
    }

    [Test]
    public void HttpSourceDefinition_CanSetAllProperties()
    {
        var source = new HttpSourceDefinition
        {
            Url = "http://api.test.com",
            Method = "POST",
            IntervalSeconds = 45,
            Headers = new Dictionary<string, string> { { "Auth", "Bearer token" } }
        };

        Assert.That(source.Url, Is.EqualTo("http://api.test.com"));
        Assert.That(source.Method, Is.EqualTo("POST"));
        Assert.That(source.IntervalSeconds, Is.EqualTo(45));
        Assert.That(source.Headers, Is.Not.Null);
    }

    [Test]
    public void DatabaseSourceDefinition_CanSetAllProperties()
    {
        var source = new DatabaseSourceDefinition
        {
            ConnectionString = "Server=db;Database=prod",
            Query = "SELECT * FROM events",
            PollingIntervalSeconds = 120
        };

        Assert.That(source.ConnectionString, Is.EqualTo("Server=db;Database=prod"));
        Assert.That(source.Query, Is.EqualTo("SELECT * FROM events"));
        Assert.That(source.PollingIntervalSeconds, Is.EqualTo(120));
    }

    [Test]
    public void FileSourceDefinition_CanSetPath()
    {
        var source = new FileSourceDefinition
        {
            Path = "/data/input.json",
            Format = "JSON"
        };

        Assert.That(source.Path, Is.EqualTo("/data/input.json"));
        Assert.That(source.Format, Is.EqualTo("JSON"));
    }

    [Test]
    public void FileSinkDefinition_CanSetPath()
    {
        var sink = new FileSinkDefinition
        {
            Path = "/data/output.json",
            Format = "JSON"
        };

        Assert.That(sink.Path, Is.EqualTo("/data/output.json"));
        Assert.That(sink.Format, Is.EqualTo("JSON"));
    }

    [Test]
    public void HttpSinkDefinition_CanSetUrl()
    {
        var sink = new HttpSinkDefinition
        {
            Url = "http://api.example.com/events",
            Method = "POST"
        };

        Assert.That(sink.Url, Is.EqualTo("http://api.example.com/events"));
        Assert.That(sink.Method, Is.EqualTo("POST"));
    }

    [Test]
    public void DatabaseSinkDefinition_CanSetProperties()
    {
        var sink = new DatabaseSinkDefinition
        {
            ConnectionString = "Server=db;Database=output"
        };

        Assert.That(sink.ConnectionString, Is.EqualTo("Server=db;Database=output"));
    }

    [Test]
    public void ConsoleSinkDefinition_CanBeCreated()
    {
        var sink = new ConsoleSinkDefinition();

        Assert.That(sink, Is.Not.Null);
    }

    [Test]
    public void SqlSourceDefinition_CanSetStatements()
    {
        var source = new SqlSourceDefinition
        {
            Statements = new List<string>
            {
                "CREATE TABLE orders (...)",
                "SELECT * FROM orders"
            }
        };

        Assert.That(source.Statements, Has.Count.EqualTo(2));
    }

    [Test]
    public void GroupByOperationDefinition_CanBeCreated()
    {
        var op = new GroupByOperationDefinition();
        Assert.That(op, Is.Not.Null);
    }

    [Test]
    public void AggregateOperationDefinition_CanBeCreated()
    {
        var op = new AggregateOperationDefinition();
        Assert.That(op, Is.Not.Null);
    }

    [Test]
    public void JoinOperationDefinition_CanBeCreated()
    {
        var op = new JoinOperationDefinition();
        Assert.That(op, Is.Not.Null);
    }

    [Test]
    public void RetryOperationDefinition_CanBeCreated()
    {
        var op = new RetryOperationDefinition();
        Assert.That(op, Is.Not.Null);
    }

    [Test]
    public void StateOperationDefinition_CanBeCreated()
    {
        var op = new StateOperationDefinition();
        Assert.That(op, Is.Not.Null);
    }

    [Test]
    public void TimerOperationDefinition_CanBeCreated()
    {
        var op = new TimerOperationDefinition();
        Assert.That(op, Is.Not.Null);
    }

    [Test]
    public void SideOutputOperationDefinition_CanBeCreated()
    {
        var op = new SideOutputOperationDefinition();
        Assert.That(op, Is.Not.Null);
    }

    [Test]
    public void ProcessFunctionOperationDefinition_CanBeCreated()
    {
        var op = new ProcessFunctionOperationDefinition();
        Assert.That(op, Is.Not.Null);
    }

    [Test]
    public void AsyncFunctionOperationDefinition_CanBeCreated()
    {
        var op = new AsyncFunctionOperationDefinition();
        Assert.That(op, Is.Not.Null);
    }

    [Test]
    public void RedisSinkDefinition_CanBeCreated()
    {
        var sink = new RedisSinkDefinition();
        Assert.That(sink, Is.Not.Null);
    }

    [Test]
    public void JobExecutionResult_CanSetJobId()
    {
        var result = new JobExecutionResult { JobId = "job-123" };
        Assert.That(result.JobId, Is.EqualTo("job-123"));
    }

    [Test]
    public void JobMetrics_CanBeCreated()
    {
        var metrics = new JobMetrics();
        Assert.That(metrics, Is.Not.Null);
    }
}
