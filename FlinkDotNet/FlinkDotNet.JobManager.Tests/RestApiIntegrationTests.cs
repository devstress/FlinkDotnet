// Copyright 2025 FlinkDotNet
// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Json;
using FlinkDotNet.JobManager.Models;
using FlinkDotNet.JobManager.Models.Requests;
using FlinkDotNet.JobManager.Models.Responses;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Temporalio.Client;

namespace FlinkDotNet.JobManager.Tests;

/// <summary>
/// Factory that replaces the Temporal client with a mock to enable in-process HTTP tests.
/// </summary>
public class JobManagerWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove the real Temporal client registration (which requires a running server)
            ServiceDescriptor? temporalDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ITemporalClient));
            if (temporalDescriptor != null)
            {
                services.Remove(temporalDescriptor);
            }

            // Register a mock Temporal client so the DI container is satisfied
            services.AddSingleton<ITemporalClient>(new Mock<ITemporalClient>().Object);
        });

        builder.UseEnvironment("Testing");
    }
}

/// <summary>
/// End-to-end HTTP-level integration tests for the JobManager REST API.
/// Tests run against the full ASP.NET Core pipeline using WebApplicationFactory
/// with a mocked Temporal client (no external Temporal server required).
/// </summary>
public class RestApiIntegrationTests : IClassFixture<JobManagerWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RestApiIntegrationTests(JobManagerWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Health check
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRoot_ReturnsOkWithHealthInfo()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("FlinkDotNet.JobManager");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Job submission
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SubmitJob_WithValidRequest_Returns200WithJobId()
    {
        // Arrange
        SubmitJobRequest request = CreateValidSubmitJobRequest("Http-Test-Job");

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/jobs/submit", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        SubmitJobResponse? result = await response.Content.ReadFromJsonAsync<SubmitJobResponse>();
        result.Should().NotBeNull();
        result!.JobId.Should().NotBeNullOrEmpty();
        result.State.Should().Be(JobExecutionState.Created);
        result.SubmittedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task SubmitJob_WithEmptyJobName_Returns400()
    {
        // Arrange
        SubmitJobRequest request = new()
        {
            JobName = "", // invalid
            Vertices =
            [
                new JobVertexRequest { OperatorName = "source", OperatorType = "Source", Parallelism = 1 }
            ]
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/jobs/submit", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SubmitJob_WithNoVertices_Returns400()
    {
        // Arrange
        SubmitJobRequest request = new()
        {
            JobName = "Empty Job",
            Vertices = [] // no vertices - invalid
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/jobs/submit", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SubmitJob_WithInvalidOperatorType_Returns400()
    {
        // Arrange
        SubmitJobRequest request = new()
        {
            JobName = "Bad Operator Job",
            Vertices =
            [
                new JobVertexRequest
                {
                    OperatorName = "bad",
                    OperatorType = "NonExistentOperatorType",
                    Parallelism = 1
                }
            ]
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/jobs/submit", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Job status retrieval
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetJobStatus_AfterSubmission_ReturnsJobDetails()
    {
        // Arrange - submit a job first
        SubmitJobRequest submitRequest = CreateValidSubmitJobRequest("Status-Test-Job");
        HttpResponseMessage submitResponse = await _client.PostAsJsonAsync("/api/jobs/submit", submitRequest);
        submitResponse.EnsureSuccessStatusCode();
        SubmitJobResponse? submitResult = await submitResponse.Content.ReadFromJsonAsync<SubmitJobResponse>();
        string jobId = submitResult!.JobId;

        // Act
        HttpResponseMessage statusResponse = await _client.GetAsync($"/api/jobs/{jobId}/status");

        // Assert
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        JobStatusResponse? status = await statusResponse.Content.ReadFromJsonAsync<JobStatusResponse>();
        status.Should().NotBeNull();
        status!.JobId.Should().Be(jobId);
        status.JobName.Should().Be("Status-Test-Job");
    }

    [Fact]
    public async Task GetJobStatus_ForNonExistentJob_Returns404()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/jobs/non-existent-job-id/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Job listing
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListJobs_ReturnsJobListResponse()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/jobs");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        JobListResponse? jobList = await response.Content.ReadFromJsonAsync<JobListResponse>();
        jobList.Should().NotBeNull();
        jobList!.TotalJobs.Should().BeGreaterThanOrEqualTo(0);
        jobList.Jobs.Should().NotBeNull();
    }

    [Fact]
    public async Task ListJobs_AfterMultipleSubmissions_IncludesAllJobs()
    {
        // Arrange - create a unique factory per test to isolate state
        using JobManagerWebApplicationFactory factory = new();
        using HttpClient isolatedClient = factory.CreateClient();

        // Submit multiple jobs
        for (int i = 1; i <= 3; i++)
        {
            SubmitJobRequest req = CreateValidSubmitJobRequest($"List-Test-Job-{i}");
            HttpResponseMessage submitResp = await isolatedClient.PostAsJsonAsync("/api/jobs/submit", req);
            submitResp.EnsureSuccessStatusCode();
        }

        // Act
        HttpResponseMessage listResponse = await isolatedClient.GetAsync("/api/jobs");

        // Assert
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        JobListResponse? jobList = await listResponse.Content.ReadFromJsonAsync<JobListResponse>();
        jobList.Should().NotBeNull();
        jobList!.TotalJobs.Should().BeGreaterThanOrEqualTo(3);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Job cancellation
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CancelJob_ForExistingJob_Returns200()
    {
        // Arrange - submit a job first
        SubmitJobRequest submitRequest = CreateValidSubmitJobRequest("Cancel-Test-Job");
        HttpResponseMessage submitResponse = await _client.PostAsJsonAsync("/api/jobs/submit", submitRequest);
        submitResponse.EnsureSuccessStatusCode();
        SubmitJobResponse? submitResult = await submitResponse.Content.ReadFromJsonAsync<SubmitJobResponse>();
        string jobId = submitResult!.JobId;

        // Act
        HttpResponseMessage cancelResponse = await _client.PostAsync($"/api/jobs/{jobId}/cancel", null);

        // Assert
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        string content = await cancelResponse.Content.ReadAsStringAsync();
        content.Should().Contain(jobId);
    }

    [Fact]
    public async Task CancelJob_ForNonExistentJob_Returns404()
    {
        // Act
        HttpResponseMessage response = await _client.PostAsync("/api/jobs/non-existent-id/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CancelJob_ThenGetStatus_ReflectsCancelledState()
    {
        // Arrange - submit a job in an isolated factory to avoid shared-state races
        using JobManagerWebApplicationFactory factory = new();
        using HttpClient isolatedClient = factory.CreateClient();

        SubmitJobRequest submitRequest = CreateValidSubmitJobRequest("Cancel-Status-Job");
        HttpResponseMessage submitResponse = await isolatedClient.PostAsJsonAsync("/api/jobs/submit", submitRequest);
        submitResponse.EnsureSuccessStatusCode();
        SubmitJobResponse? submitResult = await submitResponse.Content.ReadFromJsonAsync<SubmitJobResponse>();
        string jobId = submitResult!.JobId;

        // Act - cancel (the dispatcher awaits cancellation before returning 200)
        HttpResponseMessage cancelResponse = await isolatedClient.PostAsync($"/api/jobs/{jobId}/cancel", null);
        cancelResponse.EnsureSuccessStatusCode();

        // Poll for terminal state with a generous timeout to avoid flakiness
        JobStatusResponse? status = await PollForJobStateAsync(
            isolatedClient, jobId,
            [JobExecutionState.Canceling, JobExecutionState.Canceled, JobExecutionState.Failed],
            maxWaitMs: 2000);

        // Assert
        status.Should().NotBeNull();
        status!.State.Should().BeOneOf(
            JobExecutionState.Canceling,
            JobExecutionState.Canceled,
            JobExecutionState.Failed);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TaskManager registration
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterTaskManager_WithValidRequest_Returns200()
    {
        // Arrange
        var request = new { TaskManagerId = "tm-http-test-1", NumberOfSlots = 4 };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/taskmanagers/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("tm-http-test-1");
    }

    [Fact]
    public async Task RegisterTaskManager_AppearsInTaskManagerList()
    {
        // Arrange
        using JobManagerWebApplicationFactory factory = new();
        using HttpClient isolatedClient = factory.CreateClient();
        var request = new { TaskManagerId = "tm-list-test-1", NumberOfSlots = 4 };

        // Act - register
        HttpResponseMessage regResponse = await isolatedClient.PostAsJsonAsync("/api/taskmanagers/register", request);
        regResponse.EnsureSuccessStatusCode();

        // Assert - appears in list
        HttpResponseMessage listResponse = await isolatedClient.GetAsync("/api/taskmanagers");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        string listContent = await listResponse.Content.ReadAsStringAsync();
        listContent.Should().Contain("tm-list-test-1");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Heartbeat
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendHeartbeat_ForRegisteredTaskManager_Returns200()
    {
        // Arrange - register first
        using JobManagerWebApplicationFactory factory = new();
        using HttpClient isolatedClient = factory.CreateClient();
        string taskManagerId = "tm-heartbeat-test";
        var request = new { TaskManagerId = taskManagerId, NumberOfSlots = 2 };
        HttpResponseMessage regResponse = await isolatedClient.PostAsJsonAsync("/api/taskmanagers/register", request);
        regResponse.EnsureSuccessStatusCode();

        // Act - send heartbeat
        HttpResponseMessage hbResponse = await isolatedClient.PostAsync(
            $"/api/taskmanagers/{taskManagerId}/heartbeat", null);

        // Assert
        hbResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        string content = await hbResponse.Content.ReadAsStringAsync();
        content.Should().Contain(taskManagerId);
        content.Should().Contain("Heartbeat recorded");
    }

    [Fact]
    public async Task SendHeartbeat_ForUnknownTaskManager_Returns200()
    {
        // The current implementation always returns 200 for heartbeats.
        // This test validates that behaviour - heartbeats are idempotent.

        // Act
        HttpResponseMessage response = await _client.PostAsync(
            "/api/taskmanagers/tm-unknown-hb/heartbeat", null);

        // Assert - heartbeat endpoint is lenient (records anyway)
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TaskManager unregistration
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UnregisterTaskManager_ForRegisteredManager_Returns200()
    {
        // Arrange
        using JobManagerWebApplicationFactory factory = new();
        using HttpClient isolatedClient = factory.CreateClient();
        string taskManagerId = "tm-unreg-test";
        var request = new { TaskManagerId = taskManagerId, NumberOfSlots = 2 };
        await isolatedClient.PostAsJsonAsync("/api/taskmanagers/register", request);

        // Act
        HttpResponseMessage response = await isolatedClient.PostAsync(
            $"/api/taskmanagers/{taskManagerId}/unregister", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UnregisterTaskManager_ForNonExistentManager_Returns404()
    {
        // Act
        HttpResponseMessage response = await _client.PostAsync(
            "/api/taskmanagers/non-existent-tm/unregister", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Cluster overview
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetClusterOverview_ReturnsClusterStatistics()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/overview");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ClusterOverviewResponse? overview = await response.Content.ReadFromJsonAsync<ClusterOverviewResponse>();
        overview.Should().NotBeNull();
        overview!.TaskManagers.Should().BeGreaterThanOrEqualTo(0);
        overview.TotalSlots.Should().BeGreaterThanOrEqualTo(0);
        overview.AvailableSlots.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task GetClusterOverview_AfterRegistration_ShowsCorrectCounts()
    {
        // Arrange - use an isolated factory
        using JobManagerWebApplicationFactory factory = new();
        using HttpClient isolatedClient = factory.CreateClient();

        // Register two TaskManagers with 4 slots each
        await isolatedClient.PostAsJsonAsync("/api/taskmanagers/register",
            new { TaskManagerId = "tm-ov-1", NumberOfSlots = 4 });
        await isolatedClient.PostAsJsonAsync("/api/taskmanagers/register",
            new { TaskManagerId = "tm-ov-2", NumberOfSlots = 4 });

        // Act
        HttpResponseMessage response = await isolatedClient.GetAsync("/api/overview");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ClusterOverviewResponse? overview = await response.Content.ReadFromJsonAsync<ClusterOverviewResponse>();

        // Assert
        overview.Should().NotBeNull();
        overview!.TaskManagers.Should().Be(2);
        overview.TotalSlots.Should().Be(8);
        overview.AvailableSlots.Should().Be(8); // none allocated yet
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Full end-to-end scenario: register TaskManagers → submit job → check status
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task FullEndToEndScenario_RegisterTaskManagers_SubmitJob_CheckStatus()
    {
        // Arrange
        using JobManagerWebApplicationFactory factory = new();
        using HttpClient isolatedClient = factory.CreateClient();

        // Step 1: Register two TaskManagers
        HttpResponseMessage reg1 = await isolatedClient.PostAsJsonAsync("/api/taskmanagers/register",
            new { TaskManagerId = "tm-e2e-1", NumberOfSlots = 4 });
        HttpResponseMessage reg2 = await isolatedClient.PostAsJsonAsync("/api/taskmanagers/register",
            new { TaskManagerId = "tm-e2e-2", NumberOfSlots = 4 });
        reg1.StatusCode.Should().Be(HttpStatusCode.OK);
        reg2.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 2: Submit a streaming job
        SubmitJobRequest jobRequest = CreateValidStreamingJobRequest("E2E-Integration-Job");
        HttpResponseMessage submitResponse = await isolatedClient.PostAsJsonAsync("/api/jobs/submit", jobRequest);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        SubmitJobResponse? submitResult = await submitResponse.Content.ReadFromJsonAsync<SubmitJobResponse>();
        string jobId = submitResult!.JobId;

        // Step 3: Verify job is tracked
        HttpResponseMessage statusResponse = await isolatedClient.GetAsync($"/api/jobs/{jobId}/status");
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        JobStatusResponse? status = await statusResponse.Content.ReadFromJsonAsync<JobStatusResponse>();
        status!.JobId.Should().Be(jobId);
        status.JobName.Should().Be("E2E-Integration-Job");

        // Step 4: Verify job appears in list
        HttpResponseMessage listResponse = await isolatedClient.GetAsync("/api/jobs");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        JobListResponse? jobList = await listResponse.Content.ReadFromJsonAsync<JobListResponse>();
        jobList!.Jobs.Should().Contain(j => j.JobId == jobId);

        // Step 5: Verify cluster overview reflects submitted job
        HttpResponseMessage overviewResponse = await isolatedClient.GetAsync("/api/overview");
        overviewResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        ClusterOverviewResponse? overview = await overviewResponse.Content.ReadFromJsonAsync<ClusterOverviewResponse>();
        overview!.TaskManagers.Should().Be(2);
    }

    [Fact]
    public async Task MultipleTaskManagersScenario_DistributesResourcesCorrectly()
    {
        // Arrange
        using JobManagerWebApplicationFactory factory = new();
        using HttpClient isolatedClient = factory.CreateClient();

        // Register 3 TaskManagers with different slot counts
        await isolatedClient.PostAsJsonAsync("/api/taskmanagers/register",
            new { TaskManagerId = "tm-multi-1", NumberOfSlots = 2 });
        await isolatedClient.PostAsJsonAsync("/api/taskmanagers/register",
            new { TaskManagerId = "tm-multi-2", NumberOfSlots = 3 });
        await isolatedClient.PostAsJsonAsync("/api/taskmanagers/register",
            new { TaskManagerId = "tm-multi-3", NumberOfSlots = 5 });

        // Assert cluster shows correct totals
        HttpResponseMessage overviewResponse = await isolatedClient.GetAsync("/api/overview");
        ClusterOverviewResponse? overview = await overviewResponse.Content.ReadFromJsonAsync<ClusterOverviewResponse>();
        overview!.TaskManagers.Should().Be(3);
        overview.TotalSlots.Should().Be(10);  // 2 + 3 + 5
        overview.AvailableSlots.Should().Be(10);
    }

    [Fact]
    public async Task HeartbeatScenario_MultipleHeartbeats_AllSucceed()
    {
        // Arrange
        using JobManagerWebApplicationFactory factory = new();
        using HttpClient isolatedClient = factory.CreateClient();
        string taskManagerId = "tm-multi-hb";

        await isolatedClient.PostAsJsonAsync("/api/taskmanagers/register",
            new { TaskManagerId = taskManagerId, NumberOfSlots = 4 });

        // Act - send 5 heartbeats
        for (int i = 0; i < 5; i++)
        {
            HttpResponseMessage hb = await isolatedClient.PostAsync(
                $"/api/taskmanagers/{taskManagerId}/heartbeat", null);
            hb.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Polls the job status endpoint until it reports one of the expected states,
    /// or the timeout elapses.  Avoids brittle fixed-delay waits.
    /// </summary>
    private static async Task<JobStatusResponse?> PollForJobStateAsync(
        HttpClient client,
        string jobId,
        JobExecutionState[] expectedStates,
        int maxWaitMs = 2000)
    {
        int elapsed = 0;
        const int intervalMs = 50;

        while (elapsed < maxWaitMs)
        {
            HttpResponseMessage response = await client.GetAsync($"/api/jobs/{jobId}/status");
            if (response.IsSuccessStatusCode)
            {
                JobStatusResponse? status = await response.Content.ReadFromJsonAsync<JobStatusResponse>();
                if (status != null && expectedStates.Contains(status.State))
                {
                    return status;
                }
            }

            await Task.Delay(intervalMs);
            elapsed += intervalMs;
        }

        // Return last seen status (may not be in expected states - caller will assert)
        HttpResponseMessage lastResponse = await client.GetAsync($"/api/jobs/{jobId}/status");
        return lastResponse.IsSuccessStatusCode
            ? await lastResponse.Content.ReadFromJsonAsync<JobStatusResponse>()
            : null;
    }

    private static SubmitJobRequest CreateValidSubmitJobRequest(string jobName)
    {
        return new SubmitJobRequest
        {
            JobName = jobName,
            MaxParallelism = 4,
            Vertices =
            [
                new JobVertexRequest
                {
                    OperatorName = "source",
                    OperatorType = "Source",
                    Parallelism = 1
                },
                new JobVertexRequest
                {
                    OperatorName = "map",
                    OperatorType = "Map",
                    Parallelism = 2
                }
            ],
            Edges =
            [
                new JobEdgeRequest
                {
                    SourceVertexIndex = 0,
                    TargetVertexIndex = 1,
                    Strategy = "Forward"
                }
            ]
        };
    }

    private static SubmitJobRequest CreateValidStreamingJobRequest(string jobName)
    {
        return new SubmitJobRequest
        {
            JobName = jobName,
            MaxParallelism = 8,
            Vertices =
            [
                new JobVertexRequest
                {
                    OperatorName = "kafka-source",
                    OperatorType = "Source",
                    Parallelism = 2
                },
                new JobVertexRequest
                {
                    OperatorName = "transform-map",
                    OperatorType = "Map",
                    Parallelism = 4,
                    OperatorLogic = "x => x.ToUpper()"
                },
                new JobVertexRequest
                {
                    OperatorName = "console-sink",
                    OperatorType = "Sink",
                    Parallelism = 2
                }
            ],
            Edges =
            [
                new JobEdgeRequest
                {
                    SourceVertexIndex = 0,
                    TargetVertexIndex = 1,
                    Strategy = "Rebalance"
                },
                new JobEdgeRequest
                {
                    SourceVertexIndex = 1,
                    TargetVertexIndex = 2,
                    Strategy = "Forward"
                }
            ]
        };
    }
}
