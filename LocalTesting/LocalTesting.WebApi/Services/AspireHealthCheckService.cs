using System.Net.Http;
using System.Text.Json;
using Confluent.Kafka;
using StackExchange.Redis;

namespace LocalTesting.WebApi.Services;

public class AspireHealthCheckService
{
    private readonly HttpClient _httpClient;
    private readonly IConnectionMultiplexer _redis;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AspireHealthCheckService> _logger;

    public AspireHealthCheckService(
        HttpClient httpClient, 
        IConnectionMultiplexer redis,
        IConfiguration configuration,
        ILogger<AspireHealthCheckService> logger)
    {
        _httpClient = httpClient;
        _redis = redis;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<Dictionary<string, object>> CheckAllServicesAsync()
    {
        _logger.LogInformation("🔍 Checking health of all Aspire services...");
        
        var results = new Dictionary<string, object>();
        var tasks = new List<Task>();

        // Check all services in parallel
        tasks.Add(CheckKafkaBrokersAsync().ContinueWith(t => results["kafkaBrokers"] = t.Result));
        tasks.Add(CheckRedisAsync().ContinueWith(t => results["redis"] = t.Result));
        tasks.Add(CheckFlinkJobManagerAsync().ContinueWith(t => results["flinkJobManager"] = t.Result));
        tasks.Add(CheckFlinkTaskManagersAsync().ContinueWith(t => results["flinkTaskManagers"] = t.Result));
        tasks.Add(CheckFlinkSqlGatewayAsync().ContinueWith(t => results["flinkSqlGateway"] = t.Result));
        tasks.Add(CheckKafkaUIAsync().ContinueWith(t => results["kafkaUI"] = t.Result));
        tasks.Add(CheckPrometheusAsync().ContinueWith(t => results["prometheus"] = t.Result));
        tasks.Add(CheckGrafanaAsync().ContinueWith(t => results["grafana"] = t.Result));
        tasks.Add(CheckTemporalAsync().ContinueWith(t => results["temporal"] = t.Result));
        tasks.Add(CheckTemporalUIAsync().ContinueWith(t => results["temporalUI"] = t.Result));

        await Task.WhenAll(tasks);

        var allHealthy = results.Values.All(r => r is ServiceHealthStatus status && status.IsHealthy);
        var healthyCount = results.Values.Count(r => r is ServiceHealthStatus status && status.IsHealthy);
        var totalCount = results.Count;

        _logger.LogInformation("✅ Service health check completed: {HealthyCount}/{TotalCount} services healthy (includes all infrastructure + monitoring services)", 
            healthyCount, totalCount);

        return new Dictionary<string, object>
        {
            ["overallHealth"] = new
            {
                IsHealthy = allHealthy,
                HealthyServices = healthyCount,
                TotalServices = totalCount,
                HealthPercentage = (double)healthyCount / totalCount * 100
            },
            ["services"] = results,
            ["timestamp"] = DateTime.UtcNow,
            ["checkDuration"] = "Real-time health check completed"
        };
    }

    private async Task<ServiceHealthStatus> CheckKafkaBrokersAsync()
    {
        try
        {
            var bootstrapServers = _configuration["KAFKA_BOOTSTRAP_SERVERS"] ?? "localhost:9092,localhost:9093,localhost:9094";
            
            using var adminClient = new AdminClientBuilder(new AdminClientConfig
            {
                BootstrapServers = bootstrapServers
            }).Build();

            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(5));
            var brokers = metadata.Brokers;

            var brokerStatuses = brokers.Select(b => new
            {
                BrokerId = b.BrokerId,
                Host = b.Host,
                Port = b.Port,
                IsHealthy = true // If we can get metadata, broker is healthy
            }).ToList();

            return new ServiceHealthStatus
            {
                ServiceName = "Kafka Brokers",
                IsHealthy = brokers.Count >= 3,
                Details = new Dictionary<string, object>
                {
                    ["brokerCount"] = brokers.Count,
                    ["expectedBrokers"] = 3,
                    ["brokers"] = brokerStatuses,
                    ["topicCount"] = metadata.Topics.Count
                },
                ResponseTime = TimeSpan.FromMilliseconds(100), // Approximation
                LastCheck = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning("❌ Kafka brokers health check failed: {Error}", ex.Message);
            return new ServiceHealthStatus
            {
                ServiceName = "Kafka Brokers",
                IsHealthy = false,
                ErrorMessage = ex.Message,
                LastCheck = DateTime.UtcNow
            };
        }
    }

    private async Task<ServiceHealthStatus> CheckRedisAsync()
    {
        try
        {
            var database = _redis.GetDatabase();
            var startTime = DateTime.UtcNow;
            
            // Simple ping test
            await database.PingAsync();
            var responseTime = DateTime.UtcNow - startTime;

            var server = _redis.GetServer(_redis.GetEndPoints().First());
            var info = await server.InfoAsync();

            return new ServiceHealthStatus
            {
                ServiceName = "Redis",
                IsHealthy = true,
                Details = new Dictionary<string, object>
                {
                    ["connected"] = _redis.IsConnected,
                    ["serverInfo"] = "Redis server connected successfully",
                    ["endpoints"] = _redis.GetEndPoints().Select(ep => ep.ToString()).ToArray()
                },
                ResponseTime = responseTime,
                LastCheck = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning("❌ Redis health check failed: {Error}", ex.Message);
            return new ServiceHealthStatus
            {
                ServiceName = "Redis",
                IsHealthy = false,
                ErrorMessage = ex.Message,
                LastCheck = DateTime.UtcNow
            };
        }
    }

    private async Task<ServiceHealthStatus> CheckFlinkJobManagerAsync()
    {
        try
        {
            var flinkUrl = _configuration["FLINK_JOBMANAGER_URL"] ?? "http://localhost:8081";
            var startTime = DateTime.UtcNow;
            
            var response = await _httpClient.GetAsync($"{flinkUrl}/overview");
            var responseTime = DateTime.UtcNow - startTime;
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var overview = JsonSerializer.Deserialize<JsonElement>(content);
                
                return new ServiceHealthStatus
                {
                    ServiceName = "Flink JobManager",
                    IsHealthy = true,
                    Details = new Dictionary<string, object>
                    {
                        ["version"] = overview.TryGetProperty("flink-version", out var v) ? v.GetString() : "Unknown",
                        ["taskManagers"] = overview.TryGetProperty("taskmanagers", out var tm) ? tm.GetInt32() : 0,
                        ["slotsTotal"] = overview.TryGetProperty("slots-total", out var st) ? st.GetInt32() : 0,
                        ["slotsAvailable"] = overview.TryGetProperty("slots-available", out var sa) ? sa.GetInt32() : 0,
                        ["jobsRunning"] = overview.TryGetProperty("jobs-running", out var jr) ? jr.GetInt32() : 0
                    },
                    ResponseTime = responseTime,
                    LastCheck = DateTime.UtcNow
                };
            }
            else
            {
                return new ServiceHealthStatus
                {
                    ServiceName = "Flink JobManager",
                    IsHealthy = false,
                    ErrorMessage = $"HTTP {response.StatusCode}: {response.ReasonPhrase}",
                    LastCheck = DateTime.UtcNow
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("❌ Flink JobManager health check failed: {Error}", ex.Message);
            return new ServiceHealthStatus
            {
                ServiceName = "Flink JobManager",
                IsHealthy = false,
                ErrorMessage = ex.Message,
                LastCheck = DateTime.UtcNow
            };
        }
    }

    private async Task<ServiceHealthStatus> CheckFlinkTaskManagersAsync()
    {
        try
        {
            var flinkUrl = _configuration["FLINK_JOBMANAGER_URL"] ?? "http://localhost:8081";
            var startTime = DateTime.UtcNow;
            
            var response = await _httpClient.GetAsync($"{flinkUrl}/taskmanagers");
            var responseTime = DateTime.UtcNow - startTime;
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var taskManagers = JsonSerializer.Deserialize<JsonElement>(content);
                
                var taskManagerArray = taskManagers.GetProperty("taskmanagers").EnumerateArray().ToList();
                var runningCount = taskManagerArray.Count(tm => 
                    tm.TryGetProperty("status", out var status) && 
                    status.GetString() == "RUNNING");

                return new ServiceHealthStatus
                {
                    ServiceName = "Flink TaskManagers",
                    IsHealthy = runningCount >= 3,
                    Details = new Dictionary<string, object>
                    {
                        ["totalTaskManagers"] = taskManagerArray.Count,
                        ["runningTaskManagers"] = runningCount,
                        ["expectedTaskManagers"] = 3,
                        ["taskManagers"] = taskManagerArray.Select(tm => new
                        {
                            Id = tm.TryGetProperty("id", out var id) ? id.GetString() : "Unknown",
                            Status = tm.TryGetProperty("status", out var status) ? status.GetString() : "Unknown",
                            SlotsTotal = tm.TryGetProperty("slotsNumber", out var slots) ? slots.GetInt32() : 0,
                            SlotsAvailable = tm.TryGetProperty("freeSlots", out var free) ? free.GetInt32() : 0
                        }).ToList()
                    },
                    ResponseTime = responseTime,
                    LastCheck = DateTime.UtcNow
                };
            }
            else
            {
                return new ServiceHealthStatus
                {
                    ServiceName = "Flink TaskManagers",
                    IsHealthy = false,
                    ErrorMessage = $"HTTP {response.StatusCode}: {response.ReasonPhrase}",
                    LastCheck = DateTime.UtcNow
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("❌ Flink TaskManagers health check failed: {Error}", ex.Message);
            return new ServiceHealthStatus
            {
                ServiceName = "Flink TaskManagers",
                IsHealthy = false,
                ErrorMessage = ex.Message,
                LastCheck = DateTime.UtcNow
            };
        }
    }

    private async Task<ServiceHealthStatus> CheckFlinkSqlGatewayAsync()
    {
        try
        {
            var sqlGatewayUrl = _configuration["FLINK_SQL_GATEWAY_URL"] ?? "http://localhost:8083";
            var startTime = DateTime.UtcNow;
            
            var response = await _httpClient.GetAsync($"{sqlGatewayUrl}/v1/info");
            var responseTime = DateTime.UtcNow - startTime;
            
            return new ServiceHealthStatus
            {
                ServiceName = "Flink SQL Gateway",
                IsHealthy = response.IsSuccessStatusCode,
                Details = new Dictionary<string, object>
                {
                    ["statusCode"] = (int)response.StatusCode,
                    ["endpoint"] = sqlGatewayUrl
                },
                ResponseTime = responseTime,
                LastCheck = DateTime.UtcNow,
                ErrorMessage = response.IsSuccessStatusCode ? null : $"HTTP {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning("❌ Flink SQL Gateway health check failed: {Error}", ex.Message);
            return new ServiceHealthStatus
            {
                ServiceName = "Flink SQL Gateway",
                IsHealthy = false,
                ErrorMessage = ex.Message,
                LastCheck = DateTime.UtcNow
            };
        }
    }

    private async Task<ServiceHealthStatus> CheckKafkaUIAsync()
    {
        try
        {
            var startTime = DateTime.UtcNow;
            var response = await _httpClient.GetAsync("http://localhost:8082/actuator/health");
            var responseTime = DateTime.UtcNow - startTime;
            
            return new ServiceHealthStatus
            {
                ServiceName = "Kafka UI",
                IsHealthy = response.IsSuccessStatusCode,
                Details = new Dictionary<string, object>
                {
                    ["statusCode"] = (int)response.StatusCode,
                    ["endpoint"] = "http://localhost:8082"
                },
                ResponseTime = responseTime,
                LastCheck = DateTime.UtcNow,
                ErrorMessage = response.IsSuccessStatusCode ? null : $"HTTP {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ServiceHealthStatus
            {
                ServiceName = "Kafka UI",
                IsHealthy = false,
                ErrorMessage = ex.Message,
                LastCheck = DateTime.UtcNow
            };
        }
    }

    private async Task<ServiceHealthStatus> CheckPrometheusAsync()
    {
        try
        {
            var startTime = DateTime.UtcNow;
            var response = await _httpClient.GetAsync("http://localhost:9090/-/healthy");
            var responseTime = DateTime.UtcNow - startTime;
            
            return new ServiceHealthStatus
            {
                ServiceName = "Prometheus",
                IsHealthy = response.IsSuccessStatusCode,
                Details = new Dictionary<string, object>
                {
                    ["statusCode"] = (int)response.StatusCode,
                    ["endpoint"] = "http://localhost:9090"
                },
                ResponseTime = responseTime,
                LastCheck = DateTime.UtcNow,
                ErrorMessage = response.IsSuccessStatusCode ? null : $"HTTP {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ServiceHealthStatus
            {
                ServiceName = "Prometheus",
                IsHealthy = false,
                ErrorMessage = ex.Message,
                LastCheck = DateTime.UtcNow
            };
        }
    }

    private async Task<ServiceHealthStatus> CheckGrafanaAsync()
    {
        try
        {
            var startTime = DateTime.UtcNow;
            var response = await _httpClient.GetAsync("http://localhost:3000/api/health");
            var responseTime = DateTime.UtcNow - startTime;
            
            return new ServiceHealthStatus
            {
                ServiceName = "Grafana",
                IsHealthy = response.IsSuccessStatusCode,
                Details = new Dictionary<string, object>
                {
                    ["statusCode"] = (int)response.StatusCode,
                    ["endpoint"] = "http://localhost:3000"
                },
                ResponseTime = responseTime,
                LastCheck = DateTime.UtcNow,
                ErrorMessage = response.IsSuccessStatusCode ? null : $"HTTP {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ServiceHealthStatus
            {
                ServiceName = "Grafana",
                IsHealthy = false,
                ErrorMessage = ex.Message,
                LastCheck = DateTime.UtcNow
            };
        }
    }

    private async Task<ServiceHealthStatus> CheckTemporalAsync()
    {
        try
        {
            var startTime = DateTime.UtcNow;
            var response = await _httpClient.GetAsync("http://localhost:7233/api/v1/namespaces");
            var responseTime = DateTime.UtcNow - startTime;
            
            return new ServiceHealthStatus
            {
                ServiceName = "Temporal Server",
                IsHealthy = response.IsSuccessStatusCode,
                Details = new Dictionary<string, object>
                {
                    ["statusCode"] = (int)response.StatusCode,
                    ["endpoint"] = "http://localhost:7233",
                    ["uiEndpoint"] = "http://localhost:8081"
                },
                ResponseTime = responseTime,
                LastCheck = DateTime.UtcNow,
                ErrorMessage = response.IsSuccessStatusCode ? null : $"HTTP {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ServiceHealthStatus
            {
                ServiceName = "Temporal Server",
                IsHealthy = false,
                ErrorMessage = ex.Message,
                LastCheck = DateTime.UtcNow
            };
        }
    }

    private async Task<ServiceHealthStatus> CheckTemporalUIAsync()
    {
        try
        {
            var startTime = DateTime.UtcNow;
            var response = await _httpClient.GetAsync("http://localhost:8084");
            var responseTime = DateTime.UtcNow - startTime;
            
            return new ServiceHealthStatus
            {
                ServiceName = "Temporal UI",
                IsHealthy = response.IsSuccessStatusCode,
                Details = new Dictionary<string, object>
                {
                    ["statusCode"] = (int)response.StatusCode,
                    ["endpoint"] = "http://localhost:8084",
                    ["description"] = "Temporal Web UI for workflow monitoring and debugging"
                },
                ResponseTime = responseTime,
                LastCheck = DateTime.UtcNow,
                ErrorMessage = response.IsSuccessStatusCode ? null : $"HTTP {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new ServiceHealthStatus
            {
                ServiceName = "Temporal UI",
                IsHealthy = false,
                ErrorMessage = ex.Message,
                LastCheck = DateTime.UtcNow
            };
        }
    }
}

public class ServiceHealthStatus
{
    public string ServiceName { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> Details { get; set; } = new();
    public TimeSpan ResponseTime { get; set; }
    public DateTime LastCheck { get; set; }
}