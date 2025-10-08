using NUnit.Framework;

namespace LearningCourse.IntegrationTests;

/// <summary>
/// Marker test class to prevent "no tests available" warning.
/// This project contains only base classes for other test projects to inherit from.
/// </summary>
[TestFixture]
internal class InfrastructureMarker
{
    [Test]
    public void TestInfrastructure_Available()
    {
        Assert.Pass("Test infrastructure available");
    }
}