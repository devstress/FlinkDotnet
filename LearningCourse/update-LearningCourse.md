# LearningCourse Update Template

This document provides templates and requirements for adding new days to the LearningCourse integration test suite. Follow the Day 01 pattern to maintain consistency across all learning modules.

## Table of Contents
- [Project Structure](#project-structure)
- [Step-by-Step Guide](#step-by-step-guide)
- [Test Class Template](#test-class-template)
- [Solution File Updates](#solution-file-updates)
- [Documentation Updates](#documentation-updates)

## Project Structure

Each Day should follow this structure:
```
LearningCourse/
├── DayXX-Topic-Name/
│   ├── DayXX.IntegrationTests/
│   │   ├── DayXX.IntegrationTests.csproj
│   │   └── ExerciseExecutionTests.cs
│   ├── Exercise-Solutions/
│   │   ├── Exercise1-Name/
│   │   │   ├── Exercise1-Name.csproj
│   │   │   └── Program.cs
│   │   └── Exercise2-Name/
│   │       ├── Exercise2-Name.csproj
│   │       └── Program.cs
│   └── README.md
└── IntegrationTests.sln
```

## Step-by-Step Guide

### Step 1: Create Test Project

```bash
# Navigate to LearningCourse directory
cd LearningCourse

# Create the integration test project
dotnet new nunit -n DayXX.IntegrationTests -o DayXX-Topic-Name/DayXX.IntegrationTests
```

### Step 2: Configure Test Project File

Update `DayXX.IntegrationTests.csproj` to match this template:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsTestProject>true</IsTestProject>
    <IsAspireIntegrationTest>true</IsAspireIntegrationTest>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.10.0" />
    <PackageReference Include="NUnit" Version="4.3.1" />
    <PackageReference Include="NUnit3TestAdapter" Version="4.6.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\LearningCourse.IntegrationTests\LearningCourse.IntegrationTests.csproj" />
  </ItemGroup>

  <ItemGroup>
    <!-- Use project references for proper build dependency tracking -->
    <ProjectReference Include="..\..\..\FlinkDotNet\FlinkDotNet\FlinkDotNet.csproj" />
    <ProjectReference Include="..\..\..\FlinkDotNet\Flink.JobBuilder\Flink.JobBuilder.csproj" />
    <ProjectReference Include="..\..\..\FlinkDotNet\FlinkDotNet.Common\FlinkDotNet.Common.csproj" />
    <ProjectReference Include="..\..\..\FlinkDotNet\FlinkDotNet.DataStream\FlinkDotNet.DataStream.csproj" />
    <ProjectReference Include="..\..\..\FlinkDotNet\FlinkDotNet.Table\FlinkDotNet.Table.csproj" />
    <ProjectReference Include="..\..\..\FlinkDotNet\FlinkDotNet.Util\FlinkDotNet.Util.csproj" />
  </ItemGroup>

</Project>
```

### Step 3: Create Test Class

Create `ExerciseExecutionTests.cs` following the template in the [Test Class Template](#test-class-template) section below.

### Step 4: Add to Solution

```bash
# Add the test project to the solution
cd LearningCourse
dotnet sln IntegrationTests.sln add DayXX-Topic-Name/DayXX.IntegrationTests/DayXX.IntegrationTests.csproj

# Add exercise solution projects (one command for all)
dotnet sln IntegrationTests.sln add \
  DayXX-Topic-Name/Exercise-Solutions/Exercise1-Name/Exercise1-Name.csproj \
  DayXX-Topic-Name/Exercise-Solutions/Exercise2-Name/Exercise2-Name.csproj
```

### Step 5: Add Project Dependencies

Manually edit `LearningCourse/IntegrationTests.sln` to add project dependencies (see [Solution File Updates](#solution-file-updates)).

### Step 6: Verify Build

```bash
# Build the solution
dotnet build LearningCourse/IntegrationTests.sln --configuration Release

# List tests to verify they're discovered
dotnet test LearningCourse/IntegrationTests.sln --list-tests --filter "FullyQualifiedName~DayXX"

# Run the tests
dotnet test LearningCourse/IntegrationTests.sln --configuration Release --filter "FullyQualifiedName~DayXX"
```

## Test Class Template

```csharp
using LearningCourse.IntegrationTests;
using NUnit.Framework;

namespace DayXX.IntegrationTests;

/// <summary>
/// Integration tests for Day XX: [Topic Name]
///
/// Reference: [URL to reference documentation]
///
/// These tests validate exercises based on [description]:
/// - Exercise X.1: [Name] - [Description]
/// - Exercise X.2: [Name] - [Description]
///
/// Implementation: Uses FlinkDotNet with .NET Aspire for infrastructure
/// </summary>
[TestFixture]
[Category("dayXX-topic-name")]
[Category("integration")]
public class ExerciseExecutionTests : LearningCourseTestBase
{
    private const string Exercise1Path = "DayXX-Topic-Name/Exercise-Solutions/Exercise1-Name";
    private const string Exercise2Path = "DayXX-Topic-Name/Exercise-Solutions/Exercise2-Name";
    private static readonly TimeSpan ExerciseTimeout = TimeSpan.FromMinutes(3);

    /// <summary>
    /// Exercise X.1: [Exercise Name]
    ///
    /// This test validates:
    /// - [Point 1]
    /// - [Point 2]
    /// - [Point 3]
    ///
    /// Expected: [Expected outcome]
    /// </summary>
    [Test]
    [Description("Exercise X.1: [Exercise Name]")]
    public async Task Exercise1_Name_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise X.1: [Exercise Name]");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine();
        TestContext.WriteLine("Reference: [Reference]");
        TestContext.WriteLine();
        TestContext.WriteLine("Testing concepts:");
        TestContext.WriteLine("  - [Concept 1]");
        TestContext.WriteLine("  - [Concept 2]");
        TestContext.WriteLine();

        var (exitCode, output, error) = await ExecuteExerciseAsync(
            Exercise1Path,
            Array.Empty<string>(),
            ExerciseTimeout);

        TestContext.WriteLine();
        TestContext.WriteLine("--------------------------------------------------------------------------------");
        TestContext.WriteLine("Test Validation");
        TestContext.WriteLine("--------------------------------------------------------------------------------");

        var validationChecks = BuildExercise1ValidationChecks(output);
        ValidateExerciseResults(validationChecks, output, error, "Exercise X.1");
        
        Assert.That(exitCode, Is.EqualTo(0),
            $"Exercise X.1 should complete successfully. Exit code: {exitCode}\nError: {error}");

        TestContext.WriteLine();
        TestContext.WriteLine("✅ Exercise X.1 completed successfully");
        TestContext.WriteLine("================================================================================");
    }

    private static Dictionary<string, (bool result, string failureMessage)> BuildExercise1ValidationChecks(string output)
    {
        return new Dictionary<string, (bool result, string failureMessage)>
        {
            ["Check 1"] = (output.Contains("expected text"), "Expected text not found"),
            ["Check 2"] = (output.Contains("another check"), "Another check failed"),
            ["Execution Completed"] = (output.Contains("COMPLETED") || output.Contains("SUCCESS"), "Exercise did not complete successfully")
        };
    }

    private static void ValidateExerciseResults(
        Dictionary<string, (bool result, string failureMessage)> validationChecks,
        string output,
        string error,
        string exerciseName)
    {
        var validationFailures = new List<string>();

        foreach (var (checkName, (result, failureMessage)) in validationChecks)
        {
            TestContext.WriteLine($"[CHECK] {checkName}: {result}");
            if (!result)
            {
                validationFailures.Add($"{checkName}: {failureMessage}");
            }
        }

        if (validationFailures.Any())
        {
            ReportValidationFailures(validationFailures, output, error, exerciseName);
        }
    }

    private static void ReportValidationFailures(
        List<string> validationFailures,
        string output,
        string error,
        string exerciseName)
    {
        TestContext.WriteLine();
        TestContext.WriteLine("❌ Validation failures detected:");
        foreach (var failure in validationFailures)
        {
            TestContext.WriteLine($"   - {failure}");
        }
        TestContext.WriteLine();

        PrintDebugOutput(output, error);

        Assert.Fail($"{exerciseName} validation failed. See output above for details.");
    }

    private static void PrintDebugOutput(string output, string error)
    {
        TestContext.WriteLine();
        TestContext.WriteLine("Full Output:");
        TestContext.WriteLine("--------------------------------------------------------------------------------");
        TestContext.WriteLine(output);
        TestContext.WriteLine("--------------------------------------------------------------------------------");

        if (!string.IsNullOrEmpty(error))
        {
            TestContext.WriteLine();
            TestContext.WriteLine("Error Output:");
            TestContext.WriteLine("--------------------------------------------------------------------------------");
            TestContext.WriteLine(error);
            TestContext.WriteLine("--------------------------------------------------------------------------------");
        }
    }
}
```

## Solution File Updates

After adding projects to the solution, manually edit `LearningCourse/IntegrationTests.sln` to:

1. **Add Solution Folder** (if not auto-created):
   ```xml
   Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "DayXX-Topic-Name", "DayXX-Topic-Name", "{GUID1}"
   EndProject
   ```

2. **Add Project Dependencies** to test project:
   ```xml
   Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "DayXX.IntegrationTests", "DayXX-Topic-Name\DayXX.IntegrationTests\DayXX.IntegrationTests.csproj", "{GUID2}"
       ProjectSection(ProjectDependencies) = postProject
           {EXERCISE1-GUID} = {EXERCISE1-GUID}
           {EXERCISE2-GUID} = {EXERCISE2-GUID}
       EndProjectSection
   EndProject
   ```

3. **Add Nested Projects** in `GlobalSection(NestedProjects)`:
   ```xml
   GlobalSection(NestedProjects) = preSolution
       {GUID2} = {GUID1}
       {EXERCISE1-GUID} = {GUID1}
       {EXERCISE2-GUID} = {GUID1}
   EndGlobalSection
   ```

**Example from Day01:**
```xml
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Day01.IntegrationTests", "Day01-Kafka-Flink-Data-Pipeline\Day01.IntegrationTests\Day01.IntegrationTests.csproj", "{0756DD46-8AA2-4F0B-9EB7-9A5E3BDB7BC0}"
    ProjectSection(ProjectDependencies) = postProject
        {557D7A56-2E8D-7942-5A8C-1B38781FA266} = {557D7A56-2E8D-7942-5A8C-1B38781FA266}
        {5FE81992-961C-E485-6CD4-1C6283F03029} = {5FE81992-961C-E485-6CD4-1C6283F03029}
    EndProjectSection
EndProject
```

## Documentation Updates

### 1. Update LearningCourse/README.md

Add quick start command:
```markdown
# Run ONLY DayXX tests
dotnet test LearningCourse/IntegrationTests.sln --configuration Release --filter "FullyQualifiedName~DayXX"
```

Add to Solution Structure:
```markdown
- **DayXX.IntegrationTests** - [Topic] learning course tests
```

Add to Test Projects section:
```markdown
### DayXX.IntegrationTests
Located in `LearningCourse/DayXX-Topic-Name/DayXX.IntegrationTests/`

Tests:
- `Exercise1_Name_ShouldExecuteSuccessfully` - [Description]
- `Exercise2_Name_ShouldExecuteSuccessfully` - [Description]

**Key Point**: DayXX tests inherit from `LearningCourseTestBase` and rely on the shared infrastructure.
```

### 2. Update DayXX-Topic-Name/README.md

Include instructions on how to run the integration tests for that specific day.

## Testing Your Changes

```bash
# Clean build
dotnet clean LearningCourse/IntegrationTests.sln
dotnet build LearningCourse/IntegrationTests.sln --configuration Release

# Verify tests are discovered
dotnet test LearningCourse/IntegrationTests.sln --list-tests --filter "FullyQualifiedName~DayXX"

# Run the tests
dotnet test LearningCourse/IntegrationTests.sln --configuration Release --filter "FullyQualifiedName~DayXX" --verbosity normal

# Run all LearningCourse tests
dotnet test LearningCourse/IntegrationTests.sln --configuration Release
```

## Checklist for New Day

- [ ] Created DayXX.IntegrationTests project with correct structure
- [ ] Updated .csproj file to match template
- [ ] Created ExerciseExecutionTests.cs with all exercise tests
- [ ] Added test project to IntegrationTests.sln
- [ ] Added exercise solution projects to IntegrationTests.sln
- [ ] Manually updated solution file to add project dependencies
- [ ] Tests build successfully
- [ ] Tests are discoverable via `--list-tests`
- [ ] Tests execute successfully
- [ ] Updated LearningCourse/README.md with new day
- [ ] Updated DayXX/README.md with test instructions
- [ ] Committed changes with clear commit message

## Common Issues

### Tests not discovered
- Ensure `[TestFixture]` attribute is on the class
- Ensure `[Test]` attribute is on test methods
- Check that project is added to solution file
- Rebuild solution

### Build errors
- Verify all project references are correct
- Check FlinkDotNet solution builds first
- Ensure .NET 9.0 SDK is installed

### Tests fail to execute
- Check Docker Desktop is running
- Ensure exercise solution projects exist and build
- Verify paths in test constants are correct
- Check logs for infrastructure issues

## Best Practices

1. **Follow Day01 Format Exactly**: Use Day01 as the reference implementation
2. **Validation Checks**: Create comprehensive validation checks for each exercise
3. **Error Messages**: Provide clear, actionable error messages
4. **Test Independence**: Each test should be independent and not rely on execution order
5. **Timeout Configuration**: Use appropriate timeouts based on exercise complexity
6. **Documentation**: Keep test documentation synchronized with exercise documentation
7. **Naming Conventions**: Use consistent naming across all days (DayXX, ExerciseX_Name)
8. **Categories**: Use appropriate NUnit categories for filtering and organization
