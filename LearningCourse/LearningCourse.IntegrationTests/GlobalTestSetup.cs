using NUnit.Framework;

/// <summary>
/// Global setup fixture for all LearningCourse integration tests.
/// This ensures Aspire infrastructure is started ONCE for all test assemblies.
///
/// IMPORTANT: This must be in the root namespace (no namespace declaration)
/// to apply to ALL test namespaces (Day01.IntegrationTests, Day02.IntegrationTests, etc.)
/// </summary>
[SetUpFixture]
public class GlobalTestSetup
{
    [OneTimeSetUp]
    public static async Task GlobalSetup()
    {
        await LearningCourse.IntegrationTests.LearningCourseTestBase.GlobalSetUp();
    }

    [OneTimeTearDown]
    public static void GlobalTeardown()
    {
        LearningCourse.IntegrationTests.LearningCourseTestBase.GlobalTearDown();
    }
}