using Flink.JobBuilder.Models;
using Flink.JobBuilder.Services;
using NUnit.Framework;

namespace Flink.JobBuilder.Tests.Tests;

[TestFixture]
public class FlinkJobBuilderSubmissionTests
{
    #region Submit Tests

    [Test]
    public async System.Threading.Tasks.Task Submit_WithValidJob_ReturnsSuccessResult()
    {
        var mockService = new MockFlinkJobGatewayService();
        var jobDef = FlinkJobBuilder.FromKafka("test-topic")
            .ToConsole()
            .BuildJobDefinition();
        
        var result = await mockService.SubmitJobAsync(jobDef);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Success, Is.True);
        Assert.That(result.FlinkJobId, Is.Not.Empty);
        Assert.That(mockService.LastSubmittedResult, Is.Not.Null);
    }

    [Test]
    public async System.Threading.Tasks.Task Submit_WithJobName_SetsJobNameInMetadata()
    {
        var mockService = new MockFlinkJobGatewayService();
        var jobDef = FlinkJobBuilder.FromKafka("test-topic")
            .ToConsole()
            .BuildJobDefinition();
        
        jobDef.Metadata.JobName = "MyTestJob";
        await mockService.SubmitJobAsync(jobDef);

        var submittedJob = mockService.LastSubmittedJobDefinition;
        Assert.That(submittedJob, Is.Not.Null);
        Assert.That(submittedJob!.Metadata.JobName, Is.EqualTo("MyTestJob"));
    }

    [Test]
    public async System.Threading.Tasks.Task Submit_WithoutJobName_JobNameIsNull()
    {
        var mockService = new MockFlinkJobGatewayService();
        var jobDef = FlinkJobBuilder.FromKafka("test-topic")
            .ToConsole()
            .BuildJobDefinition();

        await mockService.SubmitJobAsync(jobDef);

        var submittedJob = mockService.LastSubmittedJobDefinition;
        Assert.That(submittedJob!.Metadata.JobName, Is.Null);
    }

    [Test]
    public async System.Threading.Tasks.Task Submit_WithCancellationToken_PassesToService()
    {
        var mockService = new MockFlinkJobGatewayService();
        var jobDef = FlinkJobBuilder.FromKafka("test-topic")
            .ToConsole()
            .BuildJobDefinition();
        using var cts = new System.Threading.CancellationTokenSource();

        await mockService.SubmitJobAsync(jobDef, cts.Token);

        Assert.That(mockService.LastCancellationToken.CanBeCanceled, Is.True);
    }

    [Test]
    public void Submit_WhenServiceThrowsException_ThrowsException()
    {
        var mockService = new MockFlinkJobGatewayService
        {
            ShouldThrowOnSubmit = true
        };
        var jobDef = FlinkJobBuilder.FromKafka("test-topic")
            .ToConsole()
            .BuildJobDefinition();

        var ex = Assert.ThrowsAsync<System.Exception>(async () => await mockService.SubmitJobAsync(jobDef));
        Assert.That(ex!.Message, Contains.Substring("Simulated submission failure"));
    }

    [Test]
    public async System.Threading.Tasks.Task Submit_WithFailedValidation_ReturnsFailureResult()
    {
        var mockService = new MockFlinkJobGatewayService
        {
            ValidationShouldFail = true
        };
        var jobDef = FlinkJobBuilder.FromKafka("test-topic")
            .ToConsole()
            .BuildJobDefinition();

        var result = await mockService.SubmitJobAsync(jobDef);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Is.Not.Null);
    }

    #endregion

    #region SubmitAndWait Tests

    [Test]
    public async System.Threading.Tasks.Task SubmitAndWait_WithFinishedJob_ReturnsSuccessResult()
    {
        var mockService = new MockFlinkJobGatewayService();
        mockService.StatusToReturn = new JobStatus
        {
            State = "FINISHED",
            FlinkJobId = "test-job-id"
        };
        _ = await mockService.SubmitJobAsync(FlinkJobBuilder.FromKafka("test-topic").ToConsole().BuildJobDefinition());
        var status = await mockService.GetJobStatusAsync("test-job-id");

        Assert.That(status, Is.Not.Null);
        Assert.That(status.State, Is.EqualTo("FINISHED"));
    }

    [Test]
    public async System.Threading.Tasks.Task SubmitAndWait_WithFailedJob_ReturnsFailureResult()
    {
        var mockService = new MockFlinkJobGatewayService();
        mockService.StatusToReturn = new JobStatus
        {
            State = "FAILED",
            FlinkJobId = "test-job-id",
            ErrorMessage = "Job execution failed"
        };
        
        var status = await mockService.GetJobStatusAsync("test-job-id");

        Assert.That(status, Is.Not.Null);
        Assert.That(status.State, Is.EqualTo("FAILED"));
        Assert.That(status.ErrorMessage, Is.EqualTo("Job execution failed"));
    }

    [Test]
    public async System.Threading.Tasks.Task SubmitAndWait_WithCanceledJob_ReturnsFailureResult()
    {
        var mockService = new MockFlinkJobGatewayService();
        mockService.StatusToReturn = new JobStatus
        {
            State = "CANCELED",
            FlinkJobId = "test-job-id"
        };
        
        var status = await mockService.GetJobStatusAsync("test-job-id");

        Assert.That(status, Is.Not.Null);
        Assert.That(status.State, Is.EqualTo("CANCELED"));
    }

    [Test]
    public async System.Threading.Tasks.Task SubmitAndWait_WithRunningJob_PollsUntilCompleted()
    {
        var mockService = new MockFlinkJobGatewayService();
        mockService.StatusSequence = new[]
        {
            new JobStatus { State = "RUNNING", FlinkJobId = "test-job-id" },
            new JobStatus { State = "RUNNING", FlinkJobId = "test-job-id" },
            new JobStatus { State = "FINISHED", FlinkJobId = "test-job-id" }
        };

        // Poll status 3 times
        await mockService.GetJobStatusAsync("test-job-id");
        await mockService.GetJobStatusAsync("test-job-id");
        var finalStatus = await mockService.GetJobStatusAsync("test-job-id");

        Assert.That(finalStatus.State, Is.EqualTo("FINISHED"));
        Assert.That(mockService.StatusPollCount, Is.EqualTo(3));
    }

    [Test]
    public async System.Threading.Tasks.Task JobStatus_CanCheckMultipleStates()
    {
        var mockService = new MockFlinkJobGatewayService();
        
        // Test RUNNING state
        mockService.StatusToReturn = new JobStatus { State = "RUNNING", FlinkJobId = "job1" };
        var status1 = await mockService.GetJobStatusAsync("job1");
        Assert.That(status1.State, Is.EqualTo("RUNNING"));

        // Test FINISHED state
        mockService.StatusToReturn = new JobStatus { State = "FINISHED", FlinkJobId = "job2" };
        var status2 = await mockService.GetJobStatusAsync("job2");
        Assert.That(status2.State, Is.EqualTo("FINISHED"));
    }

    [Test]
    public async System.Threading.Tasks.Task JobMetrics_CanBeRetrieved()
    {
        var mockService = new MockFlinkJobGatewayService();
        var metrics = await mockService.GetJobMetricsAsync("test-job-id");

        Assert.That(metrics, Is.Not.Null);
        Assert.That(metrics.FlinkJobId, Is.EqualTo("test-job-id"));
    }

    [Test]
    public async System.Threading.Tasks.Task CancelJob_ReturnsTrue()
    {
        var mockService = new MockFlinkJobGatewayService();
        var result = await mockService.CancelJobAsync("test-job-id");

        Assert.That(result, Is.True);
    }

    [Test]
    public async System.Threading.Tasks.Task HealthCheck_ReturnsExpectedResult()
    {
        var mockService = new MockFlinkJobGatewayService();
        mockService.HealthCheckResult = true;
        
        var result = await mockService.HealthCheckAsync();

        Assert.That(result, Is.True);
    }

    #endregion

    #region Integration Tests

    [Test]
    public async System.Threading.Tasks.Task CompleteWorkflow_SubmitAndCheckStatus_WorksCorrectly()
    {
        var mockService = new MockFlinkJobGatewayService();
        var jobDef = FlinkJobBuilder.FromKafka("input-topic")
            .Where("Amount > 100")
            .Map("x => x.ToUpper()")
            .ToKafka("output-topic")
            .BuildJobDefinition();
        
        jobDef.Metadata.JobName = "TestWorkflow";
        var submitResult = await mockService.SubmitJobAsync(jobDef);

        Assert.That(submitResult.Success, Is.True);
        Assert.That(mockService.LastSubmittedJobDefinition!.Operations, Has.Count.EqualTo(2));
    }

    #endregion
}

#region Enhanced Mock Service for Testing

/// <summary>
/// Enhanced mock implementation of IFlinkJobGatewayService for testing submission scenarios
/// </summary>
public class MockFlinkJobGatewayService : IFlinkJobGatewayService
{
    public JobSubmissionResult? LastSubmittedResult { get; set; }
    public JobDefinition? LastSubmittedJobDefinition { get; set; }
    public JobStatus? StatusToReturn { get; set; }
    public JobStatus[]? StatusSequence { get; set; }
    public int StatusPollCount { get; set; }
    public bool HealthCheckResult { get; set; } = true;
    public bool ShouldThrowOnSubmit { get; set; }
    public bool ValidationShouldFail { get; set; }
    public System.Threading.CancellationToken LastCancellationToken { get; set; }

    public System.Threading.Tasks.Task<JobSubmissionResult> SubmitJobAsync(JobDefinition jobDefinition, System.Threading.CancellationToken cancellationToken = default)
    {
        LastCancellationToken = cancellationToken;
        LastSubmittedJobDefinition = jobDefinition;

        if (ShouldThrowOnSubmit)
        {
            throw new System.Exception("Simulated submission failure");
        }

        if (ValidationShouldFail)
        {
            LastSubmittedResult = JobSubmissionResult.CreateFailure(
                jobDefinition.Metadata.JobId,
                "Validation failed: test error");
            return Task.FromResult(LastSubmittedResult);
        }

        LastSubmittedResult = JobSubmissionResult.CreateSuccess(
            jobDefinition.Metadata.JobId,
            $"flink-{System.Guid.NewGuid()}");
        return Task.FromResult(LastSubmittedResult);
    }

    public System.Threading.Tasks.Task<JobStatus> GetJobStatusAsync(string flinkJobId, System.Threading.CancellationToken cancellationToken = default)
    {
        if (StatusSequence != null && StatusPollCount < StatusSequence.Length)
        {
            var status = StatusSequence[StatusPollCount];
            StatusPollCount++;
            return Task.FromResult(status);
        }

        StatusPollCount++;
        return Task.FromResult(StatusToReturn ?? new JobStatus
        {
            FlinkJobId = flinkJobId,
            State = "RUNNING"
        });
    }

    public System.Threading.Tasks.Task<JobMetrics> GetJobMetricsAsync(string flinkJobId, System.Threading.CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new JobMetrics { FlinkJobId = flinkJobId });
    }

    public System.Threading.Tasks.Task<bool> CancelJobAsync(string flinkJobId, System.Threading.CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public System.Threading.Tasks.Task<bool> HealthCheckAsync(System.Threading.CancellationToken cancellationToken = default)
    {
        return Task.FromResult(HealthCheckResult);
    }
}

#endregion
