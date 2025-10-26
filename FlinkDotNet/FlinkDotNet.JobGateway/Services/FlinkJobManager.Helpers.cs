using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Flink.JobBuilder.Models;

namespace FlinkDotNet.JobGateway.Services;

public partial class FlinkJobManager
{
    private static string? FindExistingRunnerJar()
    {
        // Check if FLINK_RUNNER_JAR_PATH is set
        string? envPath = Environment.GetEnvironmentVariable("FLINK_RUNNER_JAR_PATH");
        if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
        {
            return envPath;
        }

        // Use Java 17 JAR for Flink 2.1.0 compatibility
        string[] names = new[] { "flink-ir-runner-java17.jar" };
        string[] baseDirs = new[]
        {
            Environment.CurrentDirectory,
            Path.Combine(Environment.CurrentDirectory, FlinkIRRunnerDirectory, "target")
        };

        string[] searchPaths = baseDirs.SelectMany(d => names.Select(n => Path.Combine(d, n))).ToArray();

        string? repoRoot = FindRepoRoot(Environment.CurrentDirectory);
        if (repoRoot != null)
        {
            string[] repoCandidates = new[]
            {
                Path.Combine(repoRoot, FlinkIRRunnerDirectory, "target"),
                repoRoot,
            };
            searchPaths = searchPaths.Concat(repoCandidates.SelectMany(d => names.Select(n => Path.Combine(d, n)))).ToArray();
        }

        return Array.Find(searchPaths, File.Exists);
    }

    private async Task<bool> CheckFlinkClusterHealthAsync()
    {
        try
        {
            HttpResponseMessage response = await this._httpClient.GetAsync("/v1/overview");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Cluster health check failed", ex);
        }
    }

    private async Task<string> SubmitJobToFlinkClusterAsync(string irBase64, JobDefinition jobDefinition)
    {
        try
        {
            this.LogSectionHeader("📡 [FlinkJobManager] Submitting job to Flink JobManager",
                ("🌐 Target", this._httpClient.BaseAddress?.ToString() ?? "unknown"));

            string jarId = await this.EnsureRunnerJarAsync();
            this._logger.LogInformation("✅ Flink runner JAR ready: {JarId}", jarId);

            // DIAGNOSTIC: Log job definition bootstrap servers before submission
            KafkaSourceDefinition? kafkaSource = jobDefinition.Source as KafkaSourceDefinition;
            KafkaSinkDefinition? kafkaSink = jobDefinition.Sink as KafkaSinkDefinition;
            this._logger.LogInformation("📋 Job Kafka config: Source={SourceBootstrap}, Sink={SinkBootstrap}",
                kafkaSource?.BootstrapServers ?? "null",
                kafkaSink?.BootstrapServers ?? "null");

            // DIAGNOSTIC: Log environment variables that might override job definition
            this._logger.LogInformation("🌍 Gateway environment: KAFKA_BOOTSTRAP={KafkaBootstrap}",
                Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP") ?? "not set");
            this._logger.LogWarning("⚠️ CRITICAL: Flink containers have KAFKA_BOOTSTRAP set, which will override job definition bootstrap servers in FlinkJobRunner.java!");
            this._logger.LogWarning("⚠️ FlinkJobRunner.java line 93 uses: orElse(k.bootstrapServers, System.getenv(\"KAFKA_BOOTSTRAP\"), \"kafka:9092\")");
            this._logger.LogWarning("⚠️ This means if Flink container has KAFKA_BOOTSTRAP set, it will override the kafka:9092 value from job definition!");

            object runRequest = new
            {
                entryClass = "com.flink.jobgateway.FlinkJobRunner",
                programArgsList = new[] { "--irBase64", irBase64 },
                parallelism = jobDefinition.Metadata.Parallelism ?? 1,
                jobName = jobDefinition.Metadata.JobName ?? jobDefinition.Metadata.JobId
            };

            string requestJson = JsonSerializer.Serialize(runRequest);
            this._logger.LogDebug("📤 Flink run request: {RequestJson}", requestJson);

            using StringContent content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            // Validate jarId from Flink response to prevent injection attacks
            string sanitizedJarId = ValidateAndSanitizePathSegment(jarId);

            this._logger.LogInformation("🚀 POST {Endpoint}/v1/jars/{JarId}/run", this._httpClient.BaseAddress, jarId);
            using HttpResponseMessage response = await this._httpClient.PostAsync($"/v1/jars/{sanitizedJarId}/run", content);
            this._logger.LogInformation("📥 Response: {StatusCode} {ReasonPhrase}", (int) response.StatusCode, response.ReasonPhrase);

            if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.Accepted)
            {
                string err = await response.Content.ReadAsStringAsync();
                this._logger.LogError("❌ Flink job submission failed: {StatusCode} - {Error}", response.StatusCode, err);
                throw new InvalidOperationException($"Flink run failed: {response.StatusCode} - {err}");
            }

            string runContent = await response.Content.ReadAsStringAsync();
            this._logger.LogDebug("📥 Flink response body: {RunContent}", runContent);

            string? jobId = null;
            try
            {
                FlinkRunResponse? run = JsonSerializer.Deserialize<FlinkRunResponse>(runContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                jobId = run?.JobId;
                if (jobId != null)
                {
                    this._logger.LogInformation("✅ Extracted Flink JobId from response: {JobId}", jobId);
                }
            }
            catch (JsonException ex)
            {
                this._logger.LogDebug(ex, "⚠️ Failed to deserialize Flink run response when extracting job id");
            }

            jobId ??= TryGetJobIdFromHeaders(response);

            if (string.IsNullOrEmpty(jobId))
            {
                this._logger.LogWarning("⚠️ JobId not in response, attempting recovery...");
                string targetName = jobDefinition.Metadata.JobName ?? jobDefinition.Metadata.JobId;
                jobId = await this.TryRecoverFlinkJobIdAsync(targetName, TimeSpan.FromSeconds(30));
            }

            if (string.IsNullOrEmpty(jobId))
            {
                string errorMsg = $"Flink JobManager did not return a job ID. This indicates the job may not have started correctly. " +
                    $"Response status: {response.StatusCode}, Response body: {runContent}";
                this._logger.LogError("❌ {ErrorMessage}", errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            this.LogSectionHeader("✅ [FlinkJobManager] Job submitted to Flink successfully",
                ("🆔 Flink JobId", jobId));

            return jobId;
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "❌ Failed to submit jar to Flink REST API: {Message}", ex.Message);
            throw new InvalidOperationException($"Failed to submit jar to Flink REST API for job {jobDefinition.Metadata.JobId}", ex);
        }
    }

    private async Task<string> SubmitSqlGatewayJobAsync(SqlSourceDefinition sqlSource, JobDefinition jobDefinition)
    {
        this.LogSectionHeader("📡 [FlinkJobManager] Submitting SQL job to SQL Gateway",
            ("📋 JobId", jobDefinition.Metadata.JobId));

        try
        {
            using HttpClient sqlGatewayClient = this.CreateSqlGatewayClient();
            this._logger.LogInformation("🌐 SQL Gateway client created: {BaseAddress}", sqlGatewayClient.BaseAddress);

            this._logger.LogInformation("⏳ Waiting for SQL Gateway to be ready...");
            await this.WaitForSqlGatewayReadyAsync(sqlGatewayClient);
            this._logger.LogInformation("✅ SQL Gateway is ready");

            string sessionHandle = await this.CreateSqlGatewaySessionAsync(sqlGatewayClient, jobDefinition);
            this._logger.LogInformation("✅ SQL Gateway session created: {SessionHandle}", sessionHandle);

            string? lastJobId = await this.ExecuteSqlStatementsAsync(sqlGatewayClient, sessionHandle, sqlSource.Statements);

            // SQL Gateway jobs return session handle as tracking ID
            // This is expected behavior for SQL Gateway - it manages jobs within sessions
            string result = lastJobId ?? sessionHandle;

            if (string.IsNullOrEmpty(result))
            {
                this._logger.LogError("❌ SQL Gateway did not return a job ID or session handle");
                throw new InvalidOperationException("SQL Gateway did not return a job ID or session handle. This should not happen.");
            }

            this.LogSectionHeader("✅ [FlinkJobManager] SQL job submitted successfully",
                ("🆔 JobId/SessionHandle", result));

            return result;
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "❌ Failed to submit SQL job via SQL Gateway: {Message}", ex.Message);
            throw new InvalidOperationException("SQL Gateway submission failed. See inner exception for details.", ex);
        }
    }

    private HttpClient CreateSqlGatewayClient()
    {
        string sqlGatewayEndpoint = this.DiscoverSqlGatewayEndpoint();
        this._logger.LogInformation("Using SQL Gateway endpoint: {Endpoint}", sqlGatewayEndpoint);

        return new HttpClient
        {
            BaseAddress = new Uri(sqlGatewayEndpoint),
            Timeout = TimeSpan.FromMinutes(5)
        };
    }

    private async Task<string> CreateSqlGatewaySessionAsync(HttpClient client, JobDefinition jobDefinition)
    {
        string sessionName = jobDefinition.Metadata.JobName ?? jobDefinition.Metadata.JobId;
        this._logger.LogInformation("🚀 POST {BaseAddress}/v1/sessions (Creating session: {SessionName})", client.BaseAddress, sessionName);

        object sessionRequest = new
        {
            sessionName
        };
        string sessionJson = JsonSerializer.Serialize(sessionRequest);
        this._logger.LogDebug("📤 Request body: {SessionJson}", sessionJson);

        using StringContent sessionContent = new StringContent(sessionJson, Encoding.UTF8, "application/json");
        using HttpResponseMessage sessionResponse = await client.PostAsync("/v1/sessions", sessionContent);

        this._logger.LogInformation("📥 Response: {StatusCode} {ReasonPhrase}", (int) sessionResponse.StatusCode, sessionResponse.ReasonPhrase);

        if (!sessionResponse.IsSuccessStatusCode)
        {
            string errorContent = await sessionResponse.Content.ReadAsStringAsync();
            this._logger.LogError("❌ SQL Gateway session creation failed: {StatusCode} - {Error}",
                sessionResponse.StatusCode, errorContent);
            throw new InvalidOperationException($"SQL Gateway session creation failed: {sessionResponse.StatusCode} - {errorContent}");
        }

        string sessionResponseContent = await sessionResponse.Content.ReadAsStringAsync();
        this._logger.LogDebug("📥 Response body: {Response}", sessionResponseContent);

        string handle = this.ExtractSessionHandle(sessionResponseContent);
        this._logger.LogInformation("✅ Session handle extracted: {Handle}", handle);

        return handle;
    }

    private string ExtractSessionHandle(string sessionResponseContent)
    {
        try
        {
            JsonDocument sessionJson = JsonDocument.Parse(sessionResponseContent);
            if (sessionJson.RootElement.TryGetProperty("sessionHandle", out JsonElement handleProp))
            {
                string sessionHandle = handleProp.GetString() ?? throw new InvalidOperationException("Session handle is null");
                this._logger.LogInformation("SQL Gateway session handle: {SessionHandle}", sessionHandle);
                return sessionHandle;
            }
            throw new InvalidOperationException("Session response doesn't contain sessionHandle");
        }
        catch (JsonException ex)
        {
            this._logger.LogError(ex, "Failed to parse session response");
            throw new InvalidOperationException("Failed to parse SQL Gateway session response", ex);
        }
    }

    private async Task<string?> ExecuteSqlStatementsAsync(HttpClient client, string sessionHandle, List<string> statements)
    {
        string? lastJobId = null;

        foreach (string statement in statements)
        {
            if (string.IsNullOrWhiteSpace(statement))
            {
                continue;
            }

            lastJobId = await this.ExecuteSingleStatementAsync(client, sessionHandle, statement) ?? lastJobId;
        }

        if (string.IsNullOrEmpty(lastJobId))
        {
            this._logger.LogInformation("Using session handle as job ID: {JobId}", sessionHandle);
        }

        return lastJobId;
    }

    private async Task<string?> ExecuteSingleStatementAsync(HttpClient client, string sessionHandle, string statement)
    {
        this._logger.LogInformation("Executing SQL statement via Gateway: {Statement}",
            statement.Length > 100 ? statement.Substring(0, 100) + "..." : statement);

        object requestBody = new
        {
            statement = statement.Trim()
        };
        string jsonContent = JsonSerializer.Serialize(requestBody);
        using StringContent content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // Validate sessionHandle from SQL Gateway response to prevent injection attacks
        string sanitizedSessionHandle = ValidateAndSanitizePathSegment(sessionHandle);
        string statementEndpoint = $"/v1/sessions/{sanitizedSessionHandle}/statements";
        using HttpResponseMessage response = await client.PostAsync(statementEndpoint, content);

        if (!response.IsSuccessStatusCode)
        {
            string errorContent = await response.Content.ReadAsStringAsync();
            this._logger.LogError("SQL Gateway statement execution failed: {StatusCode} - {Error}",
                response.StatusCode, errorContent);
            throw new InvalidOperationException($"SQL Gateway execution failed: {response.StatusCode} - {errorContent}");
        }

        string responseContent = await response.Content.ReadAsStringAsync();
        this._logger.LogDebug("SQL Gateway response: {Response}", responseContent);

        return this.ExtractJobIdFromStatementResponse(responseContent);
    }

    private string? ExtractJobIdFromStatementResponse(string responseContent)
    {
        try
        {
            JsonDocument responseJson = JsonDocument.Parse(responseContent);
            if (responseJson.RootElement.TryGetProperty("operationHandle", out JsonElement opHandleProp))
            {
                string? jobId = opHandleProp.GetString();
                this._logger.LogInformation("SQL Gateway returned operation handle: {OperationHandle}", jobId);
                return jobId;
            }
            if (responseJson.RootElement.TryGetProperty("statementId", out JsonElement stmtIdProp))
            {
                string? jobId = stmtIdProp.GetString();
                this._logger.LogInformation("SQL Gateway returned statement ID: {StatementId}", jobId);
                return jobId;
            }
        }
        catch (JsonException ex)
        {
            this._logger.LogWarning(ex, "Could not parse SQL Gateway response for job ID");
        }
        return null;
    }

    private async Task<string> EnsureRunnerJarAsync()
    {
        string jarPath = await this.EnsureRunnerJarPathAsync();
        if (!File.Exists(jarPath))
        {
            throw new FileNotFoundException($"Runner jar not found at {jarPath}");
        }

        // Collect connector JARs and create a shaded JAR if needed
        List<string> connectorJars = this.CollectConnectorJars();
        if (connectorJars.Count > 0)
        {
            this._logger.LogInformation("Found {Count} connector JARs, creating shaded JAR", connectorJars.Count);
            jarPath = await this.CreateShadedJarAsync(jarPath, connectorJars);
        }

        using MultipartFormDataContent form = new MultipartFormDataContent();
        await using FileStream fs = File.OpenRead(jarPath);
        string fileName = Path.GetFileName(jarPath);
        form.Add(new StreamContent(fs), "jarfile", fileName);

        HttpResponseMessage uploadResp = await this._httpClient.PostAsync("/v1/jars/upload", form);
        if (!uploadResp.IsSuccessStatusCode)
        {
            string err = await uploadResp.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Jar upload failed: {uploadResp.StatusCode} - {err}");
        }

        string uploadPayload = await uploadResp.Content.ReadAsStringAsync();
        this._logger.LogInformation("Jar upload response payload: {Payload}", uploadPayload);

        if (!string.IsNullOrWhiteSpace(uploadPayload))
        {
            try
            {
                FlinkJarUploadResponse? uploadInfo = JsonSerializer.Deserialize<FlinkJarUploadResponse>(uploadPayload);
                string? jarId = uploadInfo?.Filename;
                if (!string.IsNullOrEmpty(jarId))
                {
                    jarId = Path.GetFileName(jarId.Replace('\\', '/'));
                    if (!string.IsNullOrEmpty(jarId))
                    {
                        this._logger.LogInformation("Flink accepted jar upload {JarFile} as {JarId}", fileName, jarId);
                        return jarId;
                    }
                }
            }
            catch (Exception ex)
            {
                this._logger.LogDebug(ex, "Failed to parse jar upload response: {Payload}", uploadPayload);
            }
        }

        return await this.WaitForJarRegistrationAsync(fileName);
    }

    private async Task<string> WaitForJarRegistrationAsync(string fileName, TimeSpan? timeout = null)
    {
        TimeSpan waitFor = timeout ?? TimeSpan.FromSeconds(30);
        DateTime deadline = DateTime.UtcNow + waitFor;
        List<string> lastKnownJars = new();

        while (DateTime.UtcNow < deadline)
        {
            string? jarId = await this.TryFindRegisteredJarAsync(fileName, lastKnownJars);
            if (jarId != null)
            {
                return jarId;
            }

            await Task.Delay(JarRegistrationPollingDelay);
        }

        return this.ThrowJarNotFoundError(fileName, waitFor, lastKnownJars);
    }

    private async Task<string?> TryFindRegisteredJarAsync(string fileName, List<string> lastKnownJars)
    {
        try
        {
            HttpResponseMessage listResp = await this._httpClient.GetAsync("/v1/jars");
            if (!listResp.IsSuccessStatusCode)
            {
                return null;
            }

            string listJson = await listResp.Content.ReadAsStringAsync();
            FlinkJarsList? jars = JsonSerializer.Deserialize<FlinkJarsList>(listJson);

            UpdateLastKnownJars(jars, lastKnownJars);
            return FindMatchingJar(jars, fileName);
        }
        catch (Exception ex)
        {
            this._logger.LogDebug(ex, "Polling for uploaded jar {JarFile} failed; will retry", fileName);
            return null;
        }
    }

    private static void UpdateLastKnownJars(FlinkJarsList? jars, List<string> lastKnownJars)
    {
        lastKnownJars.Clear();
        if (jars?.Files == null)
        {
            return;
        }

        lastKnownJars.AddRange(jars.Files.Select(f =>
            string.IsNullOrEmpty(f.Id) ? f.Name : $"{f.Name} ({f.Id})"));
    }

    private static string? FindMatchingJar(FlinkJarsList? jars, string fileName)
    {
        FlinkJarInfo? jar = jars?.Files?
            .OrderByDescending(f => f.Uploaded)
            .FirstOrDefault(f => string.Equals(f.Name, fileName, StringComparison.OrdinalIgnoreCase));

        return string.IsNullOrEmpty(jar?.Id) ? null : jar.Id;
    }

    private string ThrowJarNotFoundError(string fileName, TimeSpan waitFor, List<string> lastKnownJars)
    {
        string jarList = string.Join(", ", lastKnownJars);
        this._logger.LogError("Uploaded jar {JarFile} not found; last known jars: {JarList}", fileName, jarList);
        throw new InvalidOperationException(
            $"Uploaded jar '{fileName}' not found in Flink jar list within {waitFor.TotalSeconds:F0}s. Last seen: {jarList}");
    }

    private List<string> CollectConnectorJars()
    {
        List<string> connectorJars = new List<string>();
        List<string> searchPaths = new List<string>();

        string? connectorPath = Environment.GetEnvironmentVariable("FLINK_CONNECTOR_PATH");
        if (!string.IsNullOrEmpty(connectorPath))
        {
            searchPaths.Add(connectorPath);
        }

        // Standard container lib
        searchPaths.Add("/opt/flink/lib");

        // Try multiple strategies to find the connector JARs
        string? repoRoot = FindRepoRoot(Environment.CurrentDirectory);
        if (repoRoot != null)
        {
            searchPaths.Add(Path.Combine(repoRoot, "LocalTesting", "connectors", "flink", "lib"));
        }

        // Also try from AppDomain base directory (works better in Aspire scenarios)
        string? appRoot = FindRepoRoot(AppDomain.CurrentDomain.BaseDirectory);
        if (appRoot != null && appRoot != repoRoot)
        {
            searchPaths.Add(Path.Combine(appRoot, "LocalTesting", "connectors", "flink", "lib"));
        }

        this._logger.LogInformation("Searching for connector JARs in {Count} paths", searchPaths.Count);

        foreach (string searchPath in searchPaths.Distinct())
        {
            if (Directory.Exists(searchPath))
            {
                string[] jars = Directory.GetFiles(searchPath, "*.jar", SearchOption.TopDirectoryOnly);
                if (jars.Length > 0)
                {
                    connectorJars.AddRange(jars);
                    this._logger.LogInformation("Found {Count} connector JARs in {Path}", jars.Length, searchPath);
                }
            }
            else
            {
                this._logger.LogDebug("Connector path does not exist: {Path}", searchPath);
            }
        }

        if (connectorJars.Count == 0)
        {
            this._logger.LogWarning("No connector JARs found. SQL jobs may fail if they require Kafka/JSON connectors.");
            this._logger.LogWarning("Current directory: {Current}, AppDomain base: {AppBase}, Repo root: {RepoRoot}",
                Environment.CurrentDirectory, AppDomain.CurrentDomain.BaseDirectory, repoRoot ?? "not found");
        }

        return connectorJars.Distinct().ToList();
    }

    private async Task<string> CreateShadedJarAsync(string runnerJarPath, List<string> connectorJars)
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"flink-shaded-{Guid.NewGuid():N}");
        _ = Directory.CreateDirectory(tempDir);

        try
        {
            string shadedJarPath = Path.Combine(tempDir, "flink-ir-runner-shaded.jar");
            await this.CombineJarsAsync(runnerJarPath, connectorJars, shadedJarPath);
            return shadedJarPath;
        }
        catch
        {
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch (Exception cleanupEx)
            {
                // Non-fatal cleanup failure; continue throwing original error
                this._logger.LogDebug(cleanupEx, "Temp directory cleanup failed: {TempDir}", tempDir);
            }
            throw;
        }
    }

    private Task CombineJarsAsync(string runnerJarPath, List<string> connectorJars, string outputPath)
    {
        this._logger.LogInformation("Combining runner JAR with {Count} connector JARs into shaded JAR", connectorJars.Count);
        File.Copy(runnerJarPath, outputPath, true);

        Dictionary<string, HashSet<string>> serviceFiles = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        using (ZipArchive outputZip = ZipFile.Open(outputPath, ZipArchiveMode.Update))
        {
            HashSet<string> existingEntries = new HashSet<string>(
                outputZip.Entries.Select(e => e.FullName),
                StringComparer.OrdinalIgnoreCase);

            CollectServiceFilesFromRunnerJar(outputZip, serviceFiles);
            this.MergeConnectorJars(outputZip, connectorJars, existingEntries, serviceFiles);
            this.WriteMergedServiceFiles(outputZip, serviceFiles);
        }

        this._logger.LogInformation("Created shaded JAR at {Path}", outputPath);
        return Task.CompletedTask;
    }

    private static void CollectServiceFilesFromRunnerJar(ZipArchive outputZip, Dictionary<string, HashSet<string>> serviceFiles)
    {
        foreach (ZipArchiveEntry entry in outputZip.Entries
            .Where(e => e.FullName.StartsWith("META-INF/services/", StringComparison.OrdinalIgnoreCase) && !e.FullName.EndsWith('/')))
        {
            using Stream stream = entry.Open();
            using StreamReader reader = new StreamReader(stream);
            HashSet<string> lines = new HashSet<string>();
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                string trimmed = line.Trim();
                if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith('#'))
                {
                    _ = lines.Add(line);
                }
            }
            serviceFiles[entry.FullName] = lines;
        }
    }

    private void MergeConnectorJars(ZipArchive outputZip, List<string> connectorJars,
        HashSet<string> existingEntries, Dictionary<string, HashSet<string>> serviceFiles)
    {
        foreach (string connectorJar in connectorJars)
        {
            if (!File.Exists(connectorJar))
            {
                this._logger.LogWarning("Connector JAR not found, skipping: {Path}", connectorJar);
                continue;
            }

            this._logger.LogDebug("Merging connector JAR: {Path}", connectorJar);
            int entriesAdded = MergeConnectorJar(outputZip, connectorJar, existingEntries, serviceFiles);
            this._logger.LogInformation("Added {Count} entries from connector JAR: {Name}",
                entriesAdded, Path.GetFileName(connectorJar));
        }
    }

    private static int MergeConnectorJar(ZipArchive outputZip, string connectorJar,
        HashSet<string> existingEntries, Dictionary<string, HashSet<string>> serviceFiles)
    {
        int entriesAdded = 0;
        using ZipArchive connectorZip = ZipFile.OpenRead(connectorJar);

        foreach (ZipArchiveEntry entry in connectorZip.Entries)
        {
            if (entry.FullName.EndsWith('/'))
            {
                continue;
            }

            if (entry.FullName.StartsWith("META-INF/services/", StringComparison.OrdinalIgnoreCase))
            {
                MergeServiceFile(entry, serviceFiles);
                continue;
            }

            if (existingEntries.Contains(entry.FullName))
            {
                continue;
            }

            CopyZipEntry(entry, outputZip);
            _ = existingEntries.Add(entry.FullName);
            entriesAdded++;
        }

        return entriesAdded;
    }

    private static void MergeServiceFile(ZipArchiveEntry entry, Dictionary<string, HashSet<string>> serviceFiles)
    {
        using var stream = entry.Open();
        using var reader = new StreamReader(stream);

        if (!serviceFiles.ContainsKey(entry.FullName))
        {
            serviceFiles[entry.FullName] = new HashSet<string>();
        }

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            string trimmed = line.Trim();
            if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith('#'))
            {
                _ = serviceFiles[entry.FullName].Add(line);
            }
        }
    }

    private static void CopyZipEntry(ZipArchiveEntry sourceEntry, ZipArchive destinationZip)
    {
        ZipArchiveEntry newEntry = destinationZip.CreateEntry(sourceEntry.FullName, CompressionLevel.Optimal);
        using Stream sourceStream = sourceEntry.Open();
        using Stream destStream = newEntry.Open();
        sourceStream.CopyTo(destStream);
    }

    private void WriteMergedServiceFiles(ZipArchive outputZip, Dictionary<string, HashSet<string>> serviceFiles)
    {
        foreach ((string servicePath, HashSet<string> serviceLines) in serviceFiles)
        {
            ZipArchiveEntry? oldEntry = outputZip.Entries.FirstOrDefault(e =>
                e.FullName.Equals(servicePath, StringComparison.OrdinalIgnoreCase));
            oldEntry?.Delete();

            ZipArchiveEntry newEntry = outputZip.CreateEntry(servicePath, CompressionLevel.Optimal);
            using StreamWriter writer = new StreamWriter(newEntry.Open());
            foreach (string line in serviceLines.OrderBy(l => l))
            {
                writer.WriteLine(line);
            }

            this._logger.LogDebug("Merged service file {Path} with {Count} providers",
                servicePath, serviceLines.Count);
        }
    }

    private static string? FindRepoRoot(string start)
    {
        DirectoryInfo? dir = new DirectoryInfo(start);
        while (dir != null)
        {
            string pom = Path.Combine(dir.FullName, FlinkIRRunnerDirectory, "pom.xml");
            string globalJson = Path.Combine(dir.FullName, "global.json");
            if (File.Exists(pom) && File.Exists(globalJson))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        return null;
    }

    private async Task<string?> TryRecoverFlinkJobIdAsync(string? jobName, TimeSpan timeout)
    {
        if (string.IsNullOrWhiteSpace(jobName))
        {
            return null;
        }

        Stopwatch sw = Stopwatch.StartNew();
        string[] overviewEndpoints = new[] { "/v1/jobs/overview", "/jobs/overview" };

        while (sw.Elapsed < timeout)
        {
            string? jobId = await this.TryRecoverFromEndpointsAsync(jobName, overviewEndpoints);
            if (jobId != null)
            {
                return jobId;
            }

            await Task.Delay(JobRecoveryPollingDelay);
        }

        return null;
    }

    private async Task<string?> TryRecoverFromEndpointsAsync(string jobName, string[] endpoints)
    {
        foreach (string endpoint in endpoints)
        {
            string? jobId = await this.TryRecoverFromSingleEndpointAsync(jobName, endpoint);
            if (jobId != null)
            {
                return jobId;
            }
        }
        return null;
    }

    private async Task<string?> TryRecoverFromSingleEndpointAsync(string jobName, string endpoint)
    {
        try
        {
            using HttpResponseMessage response = await this._httpClient.GetAsync(endpoint);
            if (!response.IsSuccessStatusCode)
            {
                this._logger.LogDebug("Jobs overview endpoint {Endpoint} returned {StatusCode}", endpoint, response.StatusCode);
                return null;
            }

            string payload = await response.Content.ReadAsStringAsync();
            this._logger.LogInformation("Jobs overview response from {Endpoint} while recovering job id: {Payload}", endpoint, payload);

            string? recovered = ExtractJobIdFromOverviewPayload(payload, jobName);
            if (!string.IsNullOrEmpty(recovered))
            {
                this._logger.LogInformation("Recovered job id {FlinkJobId} for job {JobName} via {Endpoint}", recovered, jobName, endpoint);
                return recovered;
            }
        }
        catch (Exception ex)
        {
            this._logger.LogDebug(ex, "Failed to recover job id for {JobName} via {Endpoint}; will retry", jobName, endpoint);
        }
        return null;
    }

    private static string? ExtractJobIdFromOverviewPayload(string payload, string jobName)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(payload);
            return ExtractJobIdFromOverviewElement(document.RootElement, jobName);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ExtractJobIdFromOverviewElement(JsonElement element, string jobName)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement child in element.EnumerateArray())
            {
                string? nested = ExtractJobIdFromOverviewElement(child, jobName);
                if (!string.IsNullOrEmpty(nested))
                {
                    return nested;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Object)
        {
            string? match = MatchJobEntry(element, jobName);
            if (!string.IsNullOrEmpty(match))
            {
                return match;
            }

            foreach (JsonProperty property in element.EnumerateObject())
            {
                string? nested = ExtractJobIdFromOverviewElement(property.Value, jobName);
                if (!string.IsNullOrEmpty(nested))
                {
                    return nested;
                }
            }
        }

        return null;
    }

    private static string? MatchJobEntry(JsonElement element, string jobName)
    {
        if (!TryGetStringProperty(element, "name", out var name) && !TryGetStringProperty(element, "jobName", out name))
        {
            return null;
        }

        if (!string.Equals(name, jobName, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return TryGetStringProperty(element, "jid", out var jobId) ||
            TryGetStringProperty(element, "jobId", out jobId) ||
            TryGetStringProperty(element, "jobid", out jobId) ||
            TryGetStringProperty(element, "id", out jobId)
            ? jobId
            : null;
    }

    private static bool TryGetStringProperty(JsonElement element, string propertyName, out string? value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase)
                && property.Value.ValueKind == JsonValueKind.String)
            {
                value = property.Value.GetString();
                return true;
            }
        }

        value = null;
        return false;
    }

    private static string? TryGetJobIdFromHeaders(HttpResponseMessage response)
    {
        if (response.Headers.Location is Uri location)
        {
            string? jobId = ExtractJobIdFromPath(location.ToString());
            if (!string.IsNullOrEmpty(jobId))
            {
                return jobId;
            }
        }

        if (response.Headers.TryGetValues("Location", out IEnumerable<string>? locations))
        {
            foreach (string value in locations)
            {
                string? jobId = ExtractJobIdFromPath(value);
                if (!string.IsNullOrEmpty(jobId))
                {
                    return jobId;
                }
            }
        }

        foreach (string headerName in new[] { "X-Flink-JobID", "X-Flink-Job-Id", "Flink-Job-Id", "Flink-JobId" })
        {
            if (response.Headers.TryGetValues(headerName, out IEnumerable<string>? headerValues))
            {
                string? value = headerValues.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
                if (!string.IsNullOrEmpty(value))
                {
                    return value.Trim();
                }
            }
        }

        return null;

        static string? ExtractJobIdFromPath(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            string trimmed = value.Split('?', 2)[0].Trim().Trim('/');
            if (string.IsNullOrEmpty(trimmed))
            {
                return null;
            }

            string[] segments = trimmed.Split('/', StringSplitOptions.RemoveEmptyEntries);
            string? last = segments.LastOrDefault();
            return string.IsNullOrEmpty(last) || last.Equals("jobs", StringComparison.OrdinalIgnoreCase)
                ? null
                : last;
        }
    }

    private JobValidationResult ValidateJobDefinition(JobDefinition jobDefinition)
    {
        List<string> errors = new List<string>();

        ValidateBasicProperties(jobDefinition, errors);
        ValidateSource(jobDefinition.Source, errors);
        ValidateSink(jobDefinition.Sink, errors);

        return new JobValidationResult { IsValid = errors.Count == 0, Errors = errors };
    }

    private static void ValidateBasicProperties(JobDefinition jobDefinition, List<string> errors)
    {
        if (jobDefinition.Metadata == null)
        {
            errors.Add("Job metadata is required");
        }
        else if (string.IsNullOrEmpty(jobDefinition.Metadata.JobId))
        {
            errors.Add("Job ID is required");
        }

        if (jobDefinition.Source == null)
        {
            errors.Add("Job source is required");
        }

        bool isSqlJob = jobDefinition.Source is SqlSourceDefinition;
        if (jobDefinition.Sink != null || isSqlJob)
        {
            return;
        }

        errors.Add("Job sink is required");
    }

    private static void ValidateSource(object? source, List<string> errors)
    {
        if (source == null)
        {
            return;
        }

        switch (source)
        {
            case KafkaSourceDefinition kafkaSource when string.IsNullOrEmpty(kafkaSource.Topic):
                errors.Add("Kafka source must specify a topic");
                break;
            case FileSourceDefinition fileSource when string.IsNullOrEmpty(fileSource.Path):
                errors.Add("File source must specify a path");
                break;
            default:
                // Source is valid or not one of the validated types
                break;
        }
    }

    private static void ValidateSink(object? sink, List<string> errors)
    {
        if (sink == null)
        {
            return;
        }

        switch (sink)
        {
            case KafkaSinkDefinition kafkaSink:
                if (string.IsNullOrEmpty(kafkaSink.Topic))
                {
                    errors.Add("Kafka sink must specify a topic");
                }
                break;
            case FileSinkDefinition fileSink:
                if (string.IsNullOrEmpty(fileSink.Path))
                {
                    errors.Add("File sink must specify a path");
                }
                break;
            default:
                // Sink is valid or not one of the validated types
                break;
        }
    }

    /// <summary>
    /// Validates and sanitizes a path segment to prevent path traversal and URL injection attacks.
    /// Only allows alphanumeric characters, hyphens, and underscores.
    /// Rejects path traversal sequences, special characters, and URL-encoded attacks.
    /// </summary>
    /// <param name="segment">The path segment to validate.</param>
    /// <param name="parameterName">
    /// The name of the parameter being validated. This is automatically populated via CallerArgumentExpression 
    /// and should not be provided by callers. It will capture the argument expression from the call site 
    /// (e.g., "flinkJobId" when called as ValidateAndSanitizePathSegment(flinkJobId)).
    /// </param>
    /// <returns>URL-encoded safe path segment.</returns>
    /// <exception cref="ArgumentException">Thrown when segment contains invalid characters or is null/empty.</exception>
    private static string ValidateAndSanitizePathSegment(string segment, [CallerArgumentExpression(nameof(segment))] string? parameterName = null)
    {
        if (string.IsNullOrWhiteSpace(segment))
        {
            throw new ArgumentException($"Path parameter '{parameterName}' cannot be null or empty.", parameterName);
        }

        // Check for path traversal sequences
        if (segment.Contains("..") || segment.Contains("./") || segment.Contains(".\\"))
        {
            throw new ArgumentException($"Path parameter '{parameterName}' contains invalid path traversal sequence: {segment}", parameterName);
        }

        // Check for invalid characters - only allow alphanumeric, hyphens, underscores, and dots
        // This prevents: /, \, ?, #, @, :, and other special characters
        // Dots are allowed for file extensions (e.g., .jar)
        char invalidChar = segment.FirstOrDefault(c => !char.IsLetterOrDigit(c) && c != '-' && c != '_' && c != '.');
        if (invalidChar != '\0')
        {
            throw new ArgumentException($"Path parameter '{parameterName}' contains invalid character '{invalidChar}'. Only alphanumeric, hyphens, underscores, and dots are allowed.", parameterName);
        }

        // Return URL-encoded segment for additional safety
        return Uri.EscapeDataString(segment);
    }
}
