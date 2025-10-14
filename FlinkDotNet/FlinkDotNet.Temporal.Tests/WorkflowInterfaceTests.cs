using FlinkDotNet.Orchestration.Models;
using FlinkDotNet.Temporal.Models;
using FlinkDotNet.Temporal.Workflows;

namespace FlinkDotNet.Temporal.Tests;

/// <summary>
/// Tests validating the structure and contracts of Temporal workflow interfaces.
/// </summary>
[TestFixture]
public class WorkflowInterfaceTests
{
    [Test]
    public void IClusterOrchestratorWorkflow_Interface_IsDefined()
    {
        var interfaceType = typeof(IClusterOrchestratorWorkflow);

        Assert.Multiple(() =>
        {
            Assert.That(interfaceType.IsInterface, Is.True);
            Assert.That(interfaceType.IsPublic, Is.True);
        });
    }

    [Test]
    public void IClusterOrchestratorWorkflow_HasOrchestrateClustersAsyncMethod()
    {
        var interfaceType = typeof(IClusterOrchestratorWorkflow);
        var method = interfaceType.GetMethod("OrchestrateClustersAsync");

        Assert.Multiple(() =>
        {
            Assert.That(method, Is.Not.Null);
            Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task)));

            var parameters = method.GetParameters();
            Assert.That(parameters, Has.Length.EqualTo(1));
            Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(OrchestrationRequest)));
        });
    }

    [Test]
    public void IJobDistributionWorkflow_Interface_IsDefined()
    {
        var interfaceType = typeof(IJobDistributionWorkflow);

        Assert.Multiple(() =>
        {
            Assert.That(interfaceType.IsInterface, Is.True);
            Assert.That(interfaceType.IsPublic, Is.True);
        });
    }

    [Test]
    public void IJobDistributionWorkflow_HasDistributeJobsAsyncMethod()
    {
        var interfaceType = typeof(IJobDistributionWorkflow);
        var method = interfaceType.GetMethod("DistributeJobsAsync");

        Assert.Multiple(() =>
        {
            Assert.That(method, Is.Not.Null);
            Assert.That(method!.ReturnType.IsGenericType, Is.True);
            Assert.That(method.ReturnType.GetGenericTypeDefinition(), Is.EqualTo(typeof(Task<>)));
            Assert.That(method.ReturnType.GetGenericArguments()[0], Is.EqualTo(typeof(JobDistributionResult)));

            var parameters = method.GetParameters();
            Assert.That(parameters, Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void IClusterLifecycleWorkflow_Interface_IsDefined()
    {
        var interfaceType = typeof(IClusterLifecycleWorkflow);

        Assert.Multiple(() =>
        {
            Assert.That(interfaceType.IsInterface, Is.True);
            Assert.That(interfaceType.IsPublic, Is.True);
        });
    }

    [Test]
    public void IClusterLifecycleWorkflow_HasManageClusterLifecycleAsyncMethod()
    {
        var interfaceType = typeof(IClusterLifecycleWorkflow);
        var method = interfaceType.GetMethod("ManageClusterLifecycleAsync");

        Assert.Multiple(() =>
        {
            Assert.That(method, Is.Not.Null);
            Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task)));

            var parameters = method.GetParameters();
            Assert.That(parameters, Has.Length.EqualTo(1));
            Assert.That(parameters[0].ParameterType.Name, Is.EqualTo("ClusterConfiguration"));
            Assert.That(parameters[0].ParameterType.Namespace, Is.EqualTo("FlinkDotNet.Orchestration.Models"));
        });
    }

    [Test]
    public void IAutoScalingWorkflow_Interface_IsDefined()
    {
        var interfaceType = typeof(IAutoScalingWorkflow);

        Assert.Multiple(() =>
        {
            Assert.That(interfaceType.IsInterface, Is.True);
            Assert.That(interfaceType.IsPublic, Is.True);
        });
    }

    [Test]
    public void IAutoScalingWorkflow_HasAutoScaleClustersAsyncMethod()
    {
        var interfaceType = typeof(IAutoScalingWorkflow);
        var method = interfaceType.GetMethod("AutoScaleClustersAsync");

        Assert.Multiple(() =>
        {
            Assert.That(method, Is.Not.Null);
            Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task)));

            var parameters = method.GetParameters();
            Assert.That(parameters, Has.Length.EqualTo(1));
            Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(AutoScalingConfig)));
        });
    }

    [Test]
    public void IFailureRecoveryWorkflow_Interface_IsDefined()
    {
        var interfaceType = typeof(IFailureRecoveryWorkflow);

        Assert.Multiple(() =>
        {
            Assert.That(interfaceType.IsInterface, Is.True);
            Assert.That(interfaceType.IsPublic, Is.True);
        });
    }

    [Test]
    public void IFailureRecoveryWorkflow_HasHandleClusterFailureAsyncMethod()
    {
        var interfaceType = typeof(IFailureRecoveryWorkflow);
        var method = interfaceType.GetMethod("HandleClusterFailureAsync");

        Assert.Multiple(() =>
        {
            Assert.That(method, Is.Not.Null);
            Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task)));

            var parameters = method.GetParameters();
            Assert.That(parameters, Has.Length.EqualTo(2));
            Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(string)));
            Assert.That(parameters[1].ParameterType, Is.EqualTo(typeof(ClusterFailureInfo)));
        });
    }

    [Test]
    public void AllWorkflowInterfaces_AreInCorrectNamespace()
    {
        var interfaces = new[]
        {
            typeof(IClusterOrchestratorWorkflow),
            typeof(IJobDistributionWorkflow),
            typeof(IClusterLifecycleWorkflow),
            typeof(IAutoScalingWorkflow),
            typeof(IFailureRecoveryWorkflow)
        };

        foreach (var interfaceType in interfaces)
        {
            Assert.That(interfaceType.Namespace, Is.EqualTo("FlinkDotNet.Temporal.Workflows"));
        }
    }
}
