using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text.Json;
using LearningCourse.Common;

namespace Exercise111;

/// <summary>
/// Exercise 11.1: Authentication & Authorization with Real Kafka Infrastructure
/// 
/// Demonstrates enterprise-grade authentication patterns:
/// - JWT token generation and validation
/// - Role-based access control (RBAC)
/// - Real Kafka message authentication
/// - Comprehensive audit logging
/// - Token expiration and refresh handling
/// 
/// Architecture: Event Generator → Kafka (authenticated) → Validation → Audit Log
/// </summary>
class Program
{
    // Kafka bootstrap servers - discovered from Aspire/Docker infrastructure
    private static string? _kafkaBootstrapServers;
    
    private static async Task<string> GetKafkaBootstrapServersAsync()
    {
        if (_kafkaBootstrapServers != null)
            return _kafkaBootstrapServers;
            
        _kafkaBootstrapServers = await AspireServiceDiscovery.GetKafkaBootstrapServersAsync();
        return _kafkaBootstrapServers;
    }

    // Kafka topics
    private const string AuthenticatedTopic = "authentication-validated-events";
    private const string AuditTopic = "authentication-audit-log";
    private const string ConsumerGroup = "exercise111-consumer";

    // JWT configuration (in production, use Key Vault or HSM)
    private static readonly string JwtSigningKey = GenerateSecureKey();
    private static readonly SymmetricSecurityKey SecurityKey = new(Encoding.UTF8.GetBytes(JwtSigningKey));

    // Test scenarios
    private static readonly List<(string UserId, string Role, string Action)> TestScenarios = new()
    {
        ("user1", "admin", "DeleteData"),      // Admin can delete
        ("user2", "user", "WriteData"),        // User can write
        ("user3", "readonly", "ReadData"),     // Readonly can read
        ("user4", "readonly", "WriteData"),    // Should fail: Readonly cannot write
        ("user5", "user", "DeleteData")        // Should fail: User cannot delete
    };

    static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            Log.Information("================================================================================");
            Log.Information("  Exercise 11.1: Authentication & Authorization");
            Log.Information("================================================================================");
            Log.Information("");
            Log.Information("🎓 Learning Objectives:");
            Log.Information("   • Generate and validate JWT tokens");
            Log.Information("   • Implement role-based access control (RBAC)");
            Log.Information("   • Authenticate Kafka messages with tokens");
            Log.Information("   • Create comprehensive audit trails");
            Log.Information("   • Handle token expiration and validation");
            Log.Information("");
            // Discover Kafka endpoint
            var kafkaEndpoint = await GetKafkaBootstrapServersAsync();
            
            Log.Information("📊 Configuration:");
            Log.Information("   Kafka: {KafkaHost}", kafkaEndpoint);
            Log.Information("   JWT Algorithm: HMAC-SHA256");
            Log.Information("   Token Expiration: 15 minutes");
            Log.Information("");
            Log.Information("🔐 Access Control Matrix:");
            Log.Information("   Admin:    Read ✓  Write ✓  Delete ✓");
            Log.Information("   User:     Read ✓  Write ✓  Delete ✗");
            Log.Information("   Readonly: Read ✓  Write ✗  Delete ✗");
            Log.Information("");

            // Step 1: Verify infrastructure
            Log.Information(">> Step 1/6: Verifying Kafka is ready...");
            await WaitForKafkaReadyAsync();
            Log.Information("");

            Log.Information(">> Step 2/6: Creating Kafka topics...");
            await CreateTopicsAsync();
            Log.Information("");

            // Step 2: Generate JWT tokens for different roles
            Log.Information(">> Step 3/6: Generating JWT tokens for test users...");
            var tokens = GenerateJwtTokens();
            DisplayTokenInfo(tokens);
            Log.Information("");

            // Step 3: Send authenticated messages
            Log.Information(">> Step 4/6: Sending authenticated messages to Kafka...");
            await SendAuthenticatedMessagesAsync(tokens);
            Log.Information("");

            // Step 4: Validate and process messages
            Log.Information(">> Step 5/6: Validating tokens and enforcing access control...");
            var results = await ValidateAndProcessMessagesAsync();
            Log.Information("");

            // Step 5: Display audit log
            Log.Information(">> Step 6/6: Reviewing audit trail...");
            await DisplayAuditLogAsync();
            Log.Information("");

            // Results Summary
            Log.Information("================================================================================");
            Log.Information("  Exercise 11.1 Results - Authentication & Authorization");
            Log.Information("================================================================================");
            
            var authorized = results.Count(r => r.Authorized);
            var denied = results.Count(r => !r.Authorized);
            var auditEntries = results.Count;

            Log.Information("  ✅ Key Achievements:");
            Log.Information("     • Generated {TokenCount} JWT tokens with different roles", tokens.Count);
            Log.Information("     • Processed {TotalRequests} authentication requests", results.Count);
            Log.Information("     • Authorized: {Authorized}, Denied: {Denied}", authorized, denied);
            Log.Information("     • Created {AuditEntries} audit log entries", auditEntries);
            Log.Information("");
            Log.Information("  🎓 Key Learnings:");
            Log.Information("     ✅ Real Kafka infrastructure with JWT authentication");
            Log.Information("     ✅ Role-based access control (RBAC) enforcement");
            Log.Information("     ✅ Token validation with expiration checking");
            Log.Information("     ✅ Comprehensive audit logging to Kafka");
            Log.Information("     ✅ Production-ready security patterns");
            Log.Information("");
            Log.Information("  💡 Production Insights:");
            Log.Information("     • JWT tokens provide stateless authentication");
            Log.Information("     • RBAC enables fine-grained access control");
            Log.Information("     • Audit trails are essential for compliance");
            Log.Information("     • Token expiration prevents replay attacks");
            Log.Information("     • Financial services use these patterns at scale");
            Log.Information("");
            Log.Information("[SUCCESS] EXERCISE COMPLETED SUCCESSFULLY!");
            Log.Information("================================================================================");

            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Exercise 11.1 failed with exception");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    /// <summary>
    /// Generate JWT tokens for test users with different roles
    /// </summary>
    private static Dictionary<string, string> GenerateJwtTokens()
    {
        var tokens = new Dictionary<string, string>();
        var tokenHandler = new JwtSecurityTokenHandler();

        foreach (var (userId, role, _) in TestScenarios.DistinctBy(s => s.UserId))
        {
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim(ClaimTypes.Role, role),
                    new Claim("generation_time", DateTimeOffset.UtcNow.ToString("o"))
                }),
                Expires = DateTime.UtcNow.AddMinutes(15),
                SigningCredentials = new SigningCredentials(SecurityKey, SecurityAlgorithms.HmacSha256Signature),
                Issuer = "Exercise111",
                Audience = "LearningCourse"
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);
            tokens[userId] = tokenString;

            Log.Information("   Generated token for {UserId} (Role: {Role})", userId, role);
        }

        return tokens;
    }

    /// <summary>
    /// Display token information (for educational purposes)
    /// </summary>
    private static void DisplayTokenInfo(Dictionary<string, string> tokens)
    {
        var firstToken = tokens.First().Value;
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(firstToken);

        Log.Information("");
        Log.Information("   📝 JWT Token Structure (Example):");
        Log.Information("      Header:  {{\"alg\":\"HS256\",\"typ\":\"JWT\"}}");
        Log.Information("      Payload: {{\"sub\":\"{Sub}\",\"role\":\"{Role}\"}}", 
            jwtToken.Subject, 
            jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value);
        Log.Information("      Signature: HMAC-SHA256(Header + Payload, Secret)");
        Log.Information("      Token Length: {Length} characters", firstToken.Length);
    }

    /// <summary>
    /// Send authenticated messages to Kafka
    /// </summary>
    private static async Task SendAuthenticatedMessagesAsync(Dictionary<string, string> tokens)
    {
        var kafkaEndpoint = await GetKafkaBootstrapServersAsync();
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = kafkaEndpoint,
            Acks = Acks.All
        };

        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

        foreach (var (userId, role, action) in TestScenarios)
        {
            var message = new AuthenticatedMessage
            {
                MessageId = Guid.NewGuid().ToString(),
                UserId = userId,
                Action = action,
                Data = new { Amount = 100, Resource = "account-123" },
                Timestamp = DateTimeOffset.UtcNow,
                JwtToken = tokens[userId]
            };

            var messageJson = JsonSerializer.Serialize(message);
            await producer.ProduceAsync(AuthenticatedTopic, new Message<string, string>
            {
                Key = userId,
                Value = messageJson
            });

            Log.Information("   Sent: {UserId} ({Role}) → {Action}", userId, role, action);
        }

        producer.Flush(TimeSpan.FromSeconds(10));
        Log.Information("   [SUCCESS] All authenticated messages sent to Kafka");
    }

    /// <summary>
    /// Validate tokens and enforce access control
    /// </summary>
    private static async Task<List<AuthorizationResult>> ValidateAndProcessMessagesAsync()
    {
        var kafkaEndpoint = await GetKafkaBootstrapServersAsync();
        var results = new List<AuthorizationResult>();
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = kafkaEndpoint,
            GroupId = ConsumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        consumer.Subscribe(AuthenticatedTopic);

        var tokenHandler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = SecurityKey,
            ValidateIssuer = true,
            ValidIssuer = "Exercise111",
            ValidateAudience = true,
            ValidAudience = "LearningCourse",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        var messagesProcessed = 0;
        var timeout = TimeSpan.FromSeconds(15);
        var stopwatch = Stopwatch.StartNew();

        while (messagesProcessed < TestScenarios.Count && stopwatch.Elapsed < timeout)
        {
            var consumeResult = consumer.Consume(TimeSpan.FromSeconds(2));
            if (consumeResult == null) continue;

            var message = JsonSerializer.Deserialize<AuthenticatedMessage>(consumeResult.Message.Value);
            if (message == null) continue;

            try
            {
                // Validate JWT token
                var principal = tokenHandler.ValidateToken(message.JwtToken, validationParameters, out _);
                var role = principal.FindFirst(ClaimTypes.Role)?.Value ?? "unknown";

                // Check authorization based on role and action
                var authorized = IsAuthorized(role, message.Action);

                var result = new AuthorizationResult
                {
                    UserId = message.UserId,
                    Role = role,
                    Action = message.Action,
                    Authorized = authorized,
                    Timestamp = DateTimeOffset.UtcNow
                };

                results.Add(result);

                // Log to audit topic
                await LogAuditEventAsync(result);

                var status = authorized ? "✓ AUTHORIZED" : "✗ DENIED";
                Log.Information("   {Status}: {UserId} ({Role}) → {Action}", 
                    status, message.UserId, role, message.Action);

                consumer.Commit(consumeResult);
                messagesProcessed++;
            }
            catch (SecurityTokenException ex)
            {
                Log.Warning("   ✗ Token validation failed for {UserId}: {Error}", 
                    message.UserId, ex.Message);
                
                var result = new AuthorizationResult
                {
                    UserId = message.UserId,
                    Action = message.Action,
                    Authorized = false,
                    Timestamp = DateTimeOffset.UtcNow,
                    FailureReason = ex.Message
                };
                results.Add(result);
                await LogAuditEventAsync(result);
                
                consumer.Commit(consumeResult);
                messagesProcessed++;
            }
        }

        Log.Information("   [SUCCESS] Processed {Count} authentication requests", messagesProcessed);
        return results;
    }

    /// <summary>
    /// Check if role is authorized for action
    /// </summary>
    private static bool IsAuthorized(string role, string action)
    {
        return role.ToLower() switch
        {
            "admin" => true, // Admin has full access
            "user" => action != "DeleteData", // User can read and write
            "readonly" => action == "ReadData", // Readonly can only read
            _ => false
        };
    }

    /// <summary>
    /// Log audit event to Kafka
    /// </summary>
    private static async Task LogAuditEventAsync(AuthorizationResult result)
    {
        var kafkaEndpoint = await GetKafkaBootstrapServersAsync();
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = kafkaEndpoint
        };

        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();
        
        var auditEntry = new AuditEntry
        {
            EventId = Guid.NewGuid().ToString(),
            UserId = result.UserId,
            Role = result.Role ?? "unknown",
            Action = result.Action,
            Authorized = result.Authorized,
            Timestamp = result.Timestamp,
            FailureReason = result.FailureReason
        };

        var auditJson = JsonSerializer.Serialize(auditEntry);
        await producer.ProduceAsync(AuditTopic, new Message<string, string>
        {
            Key = result.UserId,
            Value = auditJson
        });
    }

    /// <summary>
    /// Display audit log from Kafka
    /// </summary>
    private static async Task DisplayAuditLogAsync()
    {
        var kafkaEndpoint = await GetKafkaBootstrapServersAsync();
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = kafkaEndpoint,
            GroupId = $"{ConsumerGroup}-audit-reader",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        consumer.Subscribe(AuditTopic);

        Log.Information("");
        Log.Information("   📋 Audit Trail:");
        Log.Information("   " + new string('-', 80));

        var auditEntries = new List<AuditEntry>();
        var timeout = TimeSpan.FromSeconds(5);
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            var consumeResult = consumer.Consume(TimeSpan.FromSeconds(1));
            if (consumeResult == null) break;

            var auditEntry = JsonSerializer.Deserialize<AuditEntry>(consumeResult.Message.Value);
            if (auditEntry != null)
            {
                auditEntries.Add(auditEntry);
                
                var status = auditEntry.Authorized ? "✓" : "✗";
                Log.Information("   [{Time}] {Status} {UserId} ({Role}) → {Action}", 
                    auditEntry.Timestamp.ToString("HH:mm:ss"),
                    status,
                    auditEntry.UserId,
                    auditEntry.Role,
                    auditEntry.Action);
            }
        }

        Log.Information("   " + new string('-', 80));
        Log.Information("   Total audit entries: {Count}", auditEntries.Count);
        Log.Information("");
    }

    /// <summary>
    /// Create Kafka topics
    /// </summary>
    private static async Task CreateTopicsAsync()
    {
        var kafkaEndpoint = await GetKafkaBootstrapServersAsync();
        var adminConfig = new AdminClientConfig
        {
            BootstrapServers = kafkaEndpoint
        };

        using var admin = new AdminClientBuilder(adminConfig).Build();

        var topicsToCreate = new[]
        {
            new TopicSpecification { Name = AuthenticatedTopic, NumPartitions = 3, ReplicationFactor = 1 },
            new TopicSpecification { Name = AuditTopic, NumPartitions = 3, ReplicationFactor = 1 }
        };

        try
        {
            await admin.CreateTopicsAsync(topicsToCreate);
            Log.Information("   [SUCCESS] Topics created: {Topics}",
                string.Join(", ", topicsToCreate.Select(t => t.Name)));
        }
        catch (CreateTopicsException ex)
        {
            var errors = ex.Results.Where(r => r.Error.Code != ErrorCode.TopicAlreadyExists).ToList();
            if (!errors.Any())
            {
                Log.Information("   [SUCCESS] Topics already exist");
            }
            else
            {
                Log.Warning("Some topics failed to create: {Errors}",
                    string.Join(", ", errors.Select(e => e.Error.Reason)));
            }
        }
    }

    /// <summary>
    /// Wait for Kafka to be ready
    /// </summary>
    private static async Task WaitForKafkaReadyAsync()
    {
        var kafkaEndpoint = await GetKafkaBootstrapServersAsync();
        var timeout = TimeSpan.FromSeconds(30);
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            try
            {
                var adminConfig = new AdminClientConfig
                {
                    BootstrapServers = kafkaEndpoint,
                    SocketTimeoutMs = 3000
                };

                using var admin = new AdminClientBuilder(adminConfig).Build();
                var metadata = admin.GetMetadata(TimeSpan.FromSeconds(3));

                if (metadata?.Brokers?.Count > 0)
                {
                    Log.Information("   [SUCCESS] Kafka is ready with {BrokerCount} broker(s)", metadata.Brokers.Count);
                    return;
                }
            }
            catch
            {
                // Continue waiting
            }

            await Task.Delay(1000);
        }

        throw new TimeoutException($"Kafka not ready within {timeout.TotalSeconds} seconds");
    }

    /// <summary>
    /// Generate a secure random key for JWT signing
    /// </summary>
    private static string GenerateSecureKey()
    {
        // In production, use Key Vault or HSM
        // For exercise: generate 256-bit key
        var key = new byte[32]; // 256 bits
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(key);
        return Convert.ToBase64String(key);
    }
}

// Message models
public class AuthenticatedMessage
{
    public string MessageId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public object Data { get; set; } = new();
    public DateTimeOffset Timestamp { get; set; }
    public string JwtToken { get; set; } = string.Empty;
}

public class AuthorizationResult
{
    public string UserId { get; set; } = string.Empty;
    public string? Role { get; set; }
    public string Action { get; set; } = string.Empty;
    public bool Authorized { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string? FailureReason { get; set; }
}

public class AuditEntry
{
    public string EventId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public bool Authorized { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string? FailureReason { get; set; }
}
