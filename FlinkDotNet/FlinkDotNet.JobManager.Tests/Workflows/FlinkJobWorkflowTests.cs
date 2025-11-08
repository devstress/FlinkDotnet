//  Licensed to the Apache Software Foundation (ASF) under one
//  or more contributor license agreements.  See the NOTICE file
//  distributed with this work for additional information
//  regarding copyright ownership.  The ASF licenses this file
//  to you under the Apache License, Version 2.0 (the
//  "License"); you may not use this file except in compliance
//  with the License.  You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

using FlinkDotNet.JobManager.Models;
using FlinkDotNet.JobManager.Workflows;
using Temporalio.Client;
using Temporalio.Testing;
using Temporalio.Worker;

namespace FlinkDotNet.JobManager.Tests.Workflows;

/// <summary>
/// Tests for FlinkJobWorkflow - Temporal workflow orchestration
/// Phase 4: Temporal Integration - TDD Tests
/// </summary>
public class FlinkJobWorkflowTests
{
    [Fact]
    public async Task ExecuteJobAsync_SimpleJobGraph_CompletesSuccessfully()
    {
        // Arrange: Create test environment and simple job
        await using WorkflowEnvironment env = await WorkflowEnvironment.StartTimeSkippingAsync();

        using TemporalWorker worker = new(
            env.Client,
            new TemporalWorkerOptions("test-task-queue")
                .AddWorkflow<FlinkJobWorkflow>());

        await worker.ExecuteAsync(async () =>
        {
            JobGraph jobGraph = new()
            {
                JobId = "test-job-1",
                JobName = "Simple Job",
                Vertices =
                [
                    new JobVertex
                    {
                        VertexId = "source-1",
                        OperatorName = "Source",
                        Parallelism = 1
                    },
                    new JobVertex
                    {
                        VertexId = "sink-1",
                        OperatorName = "Sink",
                        Parallelism = 1
                    }
                ]
            };

            // Act: Execute workflow
            WorkflowHandle<FlinkJobWorkflow, JobExecutionResult> handle =
                await env.Client.StartWorkflowAsync(
                    (FlinkJobWorkflow wf) => wf.ExecuteJobAsync(jobGraph),
                    new WorkflowOptions(id: "test-workflow-1", taskQueue: "test-task-queue"));

            JobExecutionResult result = await handle.GetResultAsync();

            // Assert: Job completed successfully
            Assert.True(result.Success, "Job should complete successfully");
            Assert.Equal("test-job-1", result.JobId);
            Assert.Equal(JobExecutionState.Finished, result.State);
            Assert.Null(result.ErrorMessage);
        });
    }

    [Fact]
    public async Task ExecuteJobAsync_MultipleVertices_CreatesExecutionGraph()
    {
        // Arrange: Create job with multiple vertices
        await using WorkflowEnvironment env = await WorkflowEnvironment.StartTimeSkippingAsync();

        using TemporalWorker worker = new(
            env.Client,
            new TemporalWorkerOptions("test-task-queue")
                .AddWorkflow<FlinkJobWorkflow>());

        await worker.ExecuteAsync(async () =>
        {
            JobGraph jobGraph = new()
            {
                JobId = "test-job-2",
                JobName = "Multi-Vertex Job",
                Vertices =
                [
                    new JobVertex
                    {
                        VertexId = "source-1",
                        OperatorName = "Source",
                        Parallelism = 2
                    },
                    new JobVertex
                    {
                        VertexId = "map-1",
                        OperatorName = "Map",
                        Parallelism = 2
                    },
                    new JobVertex
                    {
                        VertexId = "sink-1",
                        OperatorName = "Sink",
                        Parallelism = 1
                    }
                ]
            };

            // Act: Start workflow
            WorkflowHandle<FlinkJobWorkflow, JobExecutionResult> handle =
                await env.Client.StartWorkflowAsync(
                    (FlinkJobWorkflow wf) => wf.ExecuteJobAsync(jobGraph),
                    new WorkflowOptions(id: "test-workflow-2", taskQueue: "test-task-queue"));

            // Assert: Query execution graph size
            Dictionary<string, ExecutionState> taskStates =
                await handle.QueryAsync(wf => wf.GetTaskStates());

            // Total tasks: 2 (source) + 2 (map) + 1 (sink) = 5
            Assert.Equal(5, taskStates.Count);
        });
    }

    [Fact]
    public async Task CancelJobSignalAsync_RunningJob_CancelsAllTasks()
    {
        // Arrange: Start a long-running job
        await using WorkflowEnvironment env = await WorkflowEnvironment.StartTimeSkippingAsync();

        using TemporalWorker worker = new(
            env.Client,
            new TemporalWorkerOptions("test-task-queue")
                .AddWorkflow<FlinkJobWorkflow>());

        await worker.ExecuteAsync(async () =>
        {
            JobGraph jobGraph = new()
            {
                JobId = "test-job-3",
                JobName = "Long Running Job",
                Vertices =
                [
                    new JobVertex
                    {
                        VertexId = "source-1",
                        OperatorName = "Source",
                        Parallelism = 1
                    }
                ]
            };

            WorkflowHandle<FlinkJobWorkflow, JobExecutionResult> handle =
                await env.Client.StartWorkflowAsync(
                    (FlinkJobWorkflow wf) => wf.ExecuteJobAsync(jobGraph),
                    new WorkflowOptions(id: "test-workflow-3", taskQueue: "test-task-queue"));

            // Act: Send cancel signal while workflow is running
            await handle.SignalAsync(wf => wf.CancelJobSignalAsync());

            // Allow time for cancellation processing
            await env.DelayAsync(TimeSpan.FromSeconds(1));

            // Assert: Query job state
            JobExecutionState state = await handle.QueryAsync(wf => wf.GetJobState());
            Assert.Equal(JobExecutionState.Canceled, state);

            // Assert: All task states should be canceled
            Dictionary<string, ExecutionState> taskStates =
                await handle.QueryAsync(wf => wf.GetTaskStates());

            Assert.All(taskStates.Values, state => Assert.Equal(ExecutionState.Canceled, state));
        });
    }

    [Fact]
    public async Task GetJobState_Query_ReturnsCurrentState()
    {
        // Arrange: Start workflow
        await using WorkflowEnvironment env = await WorkflowEnvironment.StartTimeSkippingAsync();

        using TemporalWorker worker = new(
            env.Client,
            new TemporalWorkerOptions("test-task-queue")
                .AddWorkflow<FlinkJobWorkflow>());

        await worker.ExecuteAsync(async () =>
        {
            JobGraph jobGraph = new()
            {
                JobId = "test-job-4",
                JobName = "State Query Job",
                Vertices =
                [
                    new JobVertex
                    {
                        VertexId = "source-1",
                        OperatorName = "Source",
                        Parallelism = 1
                    }
                ]
            };

            WorkflowHandle<FlinkJobWorkflow, JobExecutionResult> handle =
                await env.Client.StartWorkflowAsync(
                    (FlinkJobWorkflow wf) => wf.ExecuteJobAsync(jobGraph),
                    new WorkflowOptions(id: "test-workflow-4", taskQueue: "test-task-queue"));

            // Act: Query job state immediately
            JobExecutionState state = await handle.QueryAsync(wf => wf.GetJobState());

            // Assert: Should be Running or Created
            Assert.True(state == JobExecutionState.Created || state == JobExecutionState.Running,
                $"Job should be in Created or Running state, but was {state}");
        });
    }

    [Fact(Skip = "Test implementation pending - requires activity integration")]
    public async Task ExecuteJobAsync_WorkflowFailure_ReturnsFailedResult()
    {
        // NOTE: This test will be updated in implementation phase
        // to test actual failure scenarios with activities
        await Task.CompletedTask;
    }

    [Fact(Skip = "Test implementation pending - requires activity retry configuration")]
    public async Task ExecuteJobAsync_TaskRetry_RecoversFromTransientFailure()
    {
        // NOTE: This test will be updated in implementation phase
        // to test retry behavior with real activities
        await Task.CompletedTask;
    }

    [Fact(Skip = "Test implementation pending - requires checkpoint implementation")]
    public async Task ExecuteJobAsync_StateRecovery_ResumesFromCheckpoint()
    {
        // NOTE: This test will be updated in implementation phase
        // to test checkpoint and recovery
        await Task.CompletedTask;
    }

    [Fact]
    public async Task GetTaskStates_Query_ReturnsAllTaskStates()
    {
        // Arrange: Create workflow with multiple tasks
        await using WorkflowEnvironment env = await WorkflowEnvironment.StartTimeSkippingAsync();

        using TemporalWorker worker = new(
            env.Client,
            new TemporalWorkerOptions("test-task-queue")
                .AddWorkflow<FlinkJobWorkflow>());

        await worker.ExecuteAsync(async () =>
        {
            JobGraph jobGraph = new()
            {
                JobId = "test-job-5",
                JobName = "Task States Job",
                Vertices =
                [
                    new JobVertex
                    {
                        VertexId = "source-1",
                        OperatorName = "Source",
                        Parallelism = 2
                    },
                    new JobVertex
                    {
                        VertexId = "sink-1",
                        OperatorName = "Sink",
                        Parallelism = 1
                    }
                ]
            };

            WorkflowHandle<FlinkJobWorkflow, JobExecutionResult> handle =
                await env.Client.StartWorkflowAsync(
                    (FlinkJobWorkflow wf) => wf.ExecuteJobAsync(jobGraph),
                    new WorkflowOptions(id: "test-workflow-5", taskQueue: "test-task-queue"));

            // Act: Query task states
            Dictionary<string, ExecutionState> taskStates =
                await handle.QueryAsync(wf => wf.GetTaskStates());

            // Assert: Should have 3 tasks (2 source + 1 sink)
            Assert.Equal(3, taskStates.Count);
            Assert.Contains(taskStates.Keys, key => key.Contains("source-1"));
            Assert.Contains(taskStates.Keys, key => key.Contains("sink-1"));
        });
    }
}
