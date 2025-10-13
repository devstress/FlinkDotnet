# LearningCourse Update Template

This document provides templates and requirements for adding new days to the LearningCourse integration test suite. Follow the Day 01 pattern to maintain consistency across all learning modules.

## Table of Contents
- [Project Structure](#project-structure)
- [Step-by-Step Guide](#step-by-step-guide)
- [Test Class Template](#test-class-template)
- [Solution File Updates](#solution-file-updates)
- [Documentation Updates](#documentation-updates)
- [Common Errors and Lessons Learned](#common-errors-and-lessons-learned)

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

Each day's README.md must follow the **Day01 structure** for consistency. Use the template below as a guide.

#### README.md Template Structure

Based on [`Day01-Kafka-Flink-Data-Pipeline/README.md`](Day01-Kafka-Flink-Data-Pipeline/README.md), each day's README must include:

```markdown
# Day [X]: [Full Topic Title]

> **Based on [Source Tutorial]**: [URL to original tutorial]
>
> This tutorial follows the **exact structure** of the [source] guide, adapted for **.NET 9**, **FlinkDotNet**, and **.NET Aspire**.

## 1. Overview

[Brief introduction to the topic - 2-3 paragraphs]

Apache Flink is [context about Flink relevant to this day's topic].

In this tutorial, we're going to [what the tutorial covers].

**Key Adaptations for .NET:**
- Using **[.NET library]** instead of [Java equivalent]
- Using **FlinkDotNet** with [specific API approach] instead of Java Flink API
- Using **.NET Aspire** for infrastructure orchestration instead of manual setup

## 2. Installation

To install and configure [required components], we use .NET Aspire which automates the setup.

### Starting Infrastructure

\`\`\`bash
cd LocalTesting
dotnet run --project LocalTesting.FlinkSqlAppHost/LocalTesting.FlinkSqlAppHost.csproj
\`\`\`

This starts:
- [Component 1] (port XXXX)
- [Component 2]
- [Component 3]

Wait approximately 45 seconds for all containers to be ready.

### [Additional Setup Steps if needed]

[Configuration code examples with explanations]

## 3. [Core Concept 1]

[Explanation of the first major concept]

**[Source Tutorial] Java API:**
\`\`\`java
// Original Java code from reference tutorial
\`\`\`

**FlinkDotNet C# API:**
\`\`\`csharp
// Equivalent C# implementation with FlinkDotNet
\`\`\`

### Key API Mappings

| [Source] Java | FlinkDotNet C# |
|---------------|----------------|
| `JavaMethod()` | `CSharpMethod()` |
| `JavaClass` | `CSharpClass` |

## 4-10. [Additional Sections Following Tutorial Structure]

[Mirror the original tutorial's section structure, adapting each section to .NET]

Each section should include:
- Conceptual explanation
- Java code reference (if from external tutorial)
- C# FlinkDotNet equivalent
- Code explanations and key differences

## [N]. [Exercise Section]

### Exercise [X].[Y]: [Exercise Title]

Exercise [X].[Y] demonstrates [what the exercise covers]:

**Implementation Highlights:**
- **[Feature 1]**: [Description]
- **[Feature 2]**: [Description]
- **[Feature 3]**: [Description]

**Testing Configuration:**
\`\`\`csharp
// Key configuration settings
\`\`\`

**Running Exercise [X].[Y]:**
\`\`\`bash
cd LearningCourse/DayXX-Topic-Name/Exercise-Solutions/Exercise[XY]-Name
dotnet run
\`\`\`

**What the exercise covers:**
1. [Point 1]
2. [Point 2]
3. [Point 3]

**Code Organization:**
- [\`Program.cs\`](Exercise-Solutions/Exercise[XY]-Name/Program.cs): [Description]
- [\`OtherFile.cs\`](Exercise-Solutions/Exercise[XY]-Name/OtherFile.cs): [Description]

**Key Insight:** [Important learning point from this exercise]

## [Final Section]. Conclusion

In this article, we've presented how to [summary of what was covered].

### What We Learned

Following the [source tutorial] structure, we covered:

1. ✅ **[Topic 1]** - [Brief description]
2. ✅ **[Topic 2]** - [Brief description]
3. ✅ **[Topic 3]** - [Brief description]

### Key Differences from [Source Tutorial]

| [Source] (Java) | This Tutorial (.NET) |
|-----------------|----------------------|
| Java Library | .NET Equivalent |
| Java Approach | FlinkDotNet Approach |

### Running the Complete Demo

Navigate to the exercise directory and run:

\`\`\`bash
cd LearningCourse/DayXX-Topic-Name/Exercise-Solutions/[MainDemo]
dotnet run
\`\`\`

This executes all steps from the tutorial:
1. [Step 1]
2. [Step 2]
3. [Step 3]

### Additional Resources

- 📚 **Original Tutorial**: [Title with URL]
- 📖 **Apache Flink**: [https://flink.apache.org/](https://flink.apache.org/)
- 🔧 **Relevant .NET Library**: [URL to documentation]
- 💻 **FlinkDotNet**: Repository documentation in \`docs/\` folder
```

#### README.md Structure Requirements

**MANDATORY SECTIONS (in order):**

1. **Title with Reference Attribution**
   - Format: `# Day [X]: [Topic Title]`
   - MUST include blockquote with original tutorial reference
   - MUST state adaptations for .NET 9, FlinkDotNet, and .NET Aspire

2. **Overview Section**
   - High-level introduction to the topic
   - Context about Apache Flink for this specific topic
   - What the tutorial will cover
   - **Key Adaptations for .NET** bullet list (mandatory)

3. **Installation Section**
   - .NET Aspire infrastructure startup instructions
   - List of components started with ports
   - Wait time guidance (typically ~45 seconds)
   - Additional setup steps if needed

4. **Core Tutorial Sections (4-N)**
   - MUST mirror original tutorial's structure
   - Each section includes:
     - Concept explanation
     - Java code reference (if from external source)
     - FlinkDotNet C# equivalent code
     - **Key API Mappings** table comparing Java vs C#

5. **Exercise Sections**
   - One subsection per exercise
   - Include: Implementation highlights, testing config, running instructions
   - **What the exercise covers** numbered list
   - **Code Organization** with file links and descriptions
   - **Key Insight** highlighting main learning point

6. **Conclusion Section**
   - Summary of what was learned
   - **What We Learned** checklist with checkmarks (✅)
   - **Key Differences** comparison table
   - **Running the Complete Demo** instructions
   - **Additional Resources** with emoji icons and links

#### Content Quality Standards

**Code Examples:**
- ALWAYS show both Java (from reference) and C# (FlinkDotNet) side-by-side
- Include explanatory comments in code blocks
- Use consistent formatting and indentation
- Highlight key differences and equivalents

**API Mappings:**
- Create comparison tables for all major API differences
- Show one-to-one method/class mappings
- Note conceptual differences when exact equivalents don't exist

**Exercise Documentation:**
- Each exercise MUST have dedicated subsection
- Link to actual exercise solution files using relative paths
- Provide clear running instructions with full paths
- Explain what concepts the exercise demonstrates

**External References:**
- ALWAYS attribute original tutorial with URL in header
- Link to relevant Apache Flink documentation
- Link to .NET library documentation used
- Include FlinkDotNet repository reference

#### Formatting Standards

**Use consistent emoji markers:**
- 📚 Original Tutorial links
- 📖 Apache Flink documentation
- 🔧 .NET library documentation
- 💻 FlinkDotNet references
- ✅ Completed items in checklists

**Code block language tags:**
- Use `bash` for shell commands
- Use `csharp` for C# code
- Use `java` for Java reference code
- Use `json` for JSON configurations
- Use `xml` for XML/project files

**Link formats:**
- External links: Full URLs with titles
- Internal links: Relative paths to exercise files
- Use descriptive link text, not "click here"

#### Quality Checklist for README.md

Before committing any Day README.md, verify:

- [ ] Title includes day number and full topic name
- [ ] Attribution blockquote with original tutorial URL present
- [ ] Overview includes "Key Adaptations for .NET" section
- [ ] Installation section includes .NET Aspire startup instructions
- [ ] All major sections from original tutorial are represented
- [ ] Java vs C# code comparisons shown for key concepts
- [ ] API mapping tables included where relevant
- [ ] Each exercise has dedicated subsection with running instructions
- [ ] Exercise code files linked using relative paths
- [ ] Conclusion includes "What We Learned" checklist
- [ ] "Key Differences" comparison table present
- [ ] "Running the Complete Demo" instructions included
- [ ] "Additional Resources" section with properly formatted links
- [ ] All code blocks have proper language tags
- [ ] Consistent emoji usage throughout
- [ ] No broken relative links to exercise files
- [ ] Proper markdown formatting (headers, lists, tables)

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

## Common Errors and Lessons Learned

This section documents all errors, mistakes, and structural issues encountered when updating Learning Courses. **ALWAYS review this section before starting any Learning Course update to avoid repeating known problems.**

### 🚨 CRITICAL ERRORS TO AVOID

#### 1. Exercise Numbering Inconsistencies
**Problem**: Using inconsistent exercise numbering schemes across different days (e.g., Exercise1, Exercise11, Exercise21 vs Exercise31, Exercise35, Exercise43)

**Impact**:
- Breaks automation scripts that expect sequential numbering
- Creates confusion in test naming and filtering
- Makes it difficult to track which exercises belong to which day

**Solution**:
- **ALWAYS use sequential numbering within each day**: Exercise[Day][1-4] (e.g., Day03: Exercise31, Exercise32, Exercise33, Exercise34)
- **NEVER skip numbers** in the sequence (don't go from Exercise31 to Exercise35)
- Update `ExerciseExecutionTests.cs` constants to match actual exercise numbers
- Validate numbering consistency before committing

**Example - WRONG:**
```
Day04-Production-Backpressure/
├── Exercise-Solutions/
│   ├── Exercise31/  ❌ Should be Exercise41
│   └── Exercise35/  ❌ Should be Exercise42
```

**Example - CORRECT:**
```
Day04-Production-Backpressure/
├── Exercise-Solutions/
│   ├── Exercise41/  ✅ Sequential numbering
│   └── Exercise42/  ✅ Follows pattern
```

#### 2. Missing global.json Files in Exercise Solutions
**Problem**: Forgetting to include `global.json` file in exercise solution directories

**Impact**:
- Exercises may build with wrong .NET SDK version
- Inconsistent behavior between local and CI environments
- Test failures due to version mismatches

**Solution**:
- **MANDATORY**: Every exercise solution MUST have a `global.json` file
- Copy from template or existing exercise
- Specify .NET 9.0 SDK version explicitly
- Validate presence before committing

**Template:**
```json
{
  "sdk": {
    "version": "9.0.303",
    "rollForward": "latestFeature"
  }
}
```

#### 3. Incorrect Project References in .csproj Files
**Problem**: Using outdated or incorrect project references, especially for FlinkDotNet core libraries

**Impact**:
- Build failures due to missing dependencies
- Runtime errors from missing assemblies
- Incompatibility with latest FlinkDotNet API changes

**Solution**:
- **ALWAYS copy project references from the template in this document**
- Include ALL core FlinkDotNet projects: FlinkDotNet, Flink.JobBuilder, FlinkDotNet.Common, FlinkDotNet.DataStream, FlinkDotNet.Table, FlinkDotNet.Util
- Use relative paths: `..\..\..\..\FlinkDotNet\[ProjectName]\[ProjectName].csproj`
- Test build after adding references

#### 4. Missing ProjectDependencies Section in Solution File
**Problem**: Not adding `ProjectSection(ProjectDependencies)` to test projects in `.sln` file

**Impact**:
- Exercise projects may not build before test projects
- Race conditions in parallel builds
- Intermittent build failures in CI

**Solution**:
- **MANDATORY**: Manually edit `.sln` file after adding projects
- Add `ProjectSection(ProjectDependencies)` to each test project
- List ALL exercise projects as dependencies
- Follow Day01 pattern exactly

**Template:**
```xml
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "DayXX.IntegrationTests", "...", "{GUID}"
    ProjectSection(ProjectDependencies) = postProject
        {EXERCISE1-GUID} = {EXERCISE1-GUID}
        {EXERCISE2-GUID} = {EXERCISE2-GUID}
    EndProjectSection
EndProject
```

#### 5. Incorrect Test Path Constants
**Problem**: Copy-pasting path constants without updating them for the new day

**Impact**:
- Tests try to execute wrong exercise projects
- Misleading test failures
- Wasted debugging time

**Solution**:
- **ALWAYS update path constants** after copy-pasting test template
- Verify paths match actual directory structure
- Use consistent naming: `ExerciseXPath` where X is the exercise number
- Test path correctness before running full test suite

**Example:**
```csharp
// WRONG - Day04 paths in Day05 tests
private const string Exercise1Path = "Day04-Production-Backpressure/Exercise-Solutions/Exercise41";

// CORRECT - Updated for Day05
private const string Exercise1Path = "Day05-Enterprise-Observability/Exercise-Solutions/Exercise51";
```

### ⚠️ STRUCTURAL ISSUES TO WATCH

#### 6. Inconsistent Solution Folder Organization
**Problem**: Not grouping related projects under solution folders

**Solution**:
- Create solution folder for each day: `DayXX-Topic-Name`
- Nest all day projects (test + exercises) under the folder
- Update `GlobalSection(NestedProjects)` in `.sln` file
- Maintain consistent organization across all days

#### 7. Missing or Outdated Test Descriptions
**Problem**: Generic or copy-pasted test descriptions that don't match actual exercise content

**Solution**:
- **ALWAYS update test documentation** to match exercise specifics
- Include reference URLs for original exercises
- List specific validation points being tested
- Keep descriptions synchronized with README.md

#### 8. Inadequate Test Validation Checks
**Problem**: Tests that only check exit code without validating actual functionality

**Solution**:
- Create comprehensive validation checks for each exercise
- Verify expected output strings are present
- Check for completion indicators
- Test for specific functionality markers
- Include multiple validation points per test

#### 9. Incorrect TargetFramework in .csproj
**Problem**: Using `net8.0` or other versions instead of `net9.0`

**Impact**:
- Incompatibility with project-wide .NET 9.0 requirements
- Build failures in CI
- Runtime errors from API differences

**Solution**:
- **MANDATORY**: All projects must target `net9.0`
- Check every `.csproj` file before committing
- Never downgrade framework versions
- Follow global.json SDK version

#### 10. Missing Test Categories
**Problem**: Not adding appropriate NUnit categories for filtering

**Solution**:
- Add day-specific category: `[Category("dayXX-topic-name")]`
- Add general category: `[Category("integration")]`
- Use lowercase, hyphenated naming
- Enables selective test execution

### 📋 PRE-UPDATE CHECKLIST

Before updating ANY Learning Course, verify:

- [ ] **Reviewed this "Common Errors" section completely**
- [ ] Checked exercise numbering follows sequential pattern (Day[N][1-4])
- [ ] Verified all exercise solutions have `global.json` files
- [ ] Confirmed `.csproj` includes all required FlinkDotNet project references
- [ ] Validated all projects target `net9.0` framework
- [ ] Reviewed Day01 implementation as reference
- [ ] Prepared to manually edit `.sln` file for ProjectDependencies
- [ ] Ready to update test path constants after copy-paste
- [ ] Planned comprehensive validation checks for each test
- [ ] Confirmed test descriptions match actual exercise content

### 🔍 POST-UPDATE VALIDATION

After completing Learning Course update, verify:

- [ ] Build succeeds: `dotnet build LearningCourse/IntegrationTests.sln --configuration Release`
- [ ] Tests discovered: `dotnet test --list-tests --filter "FullyQualifiedName~DayXX"`
- [ ] Tests execute successfully with expected output
- [ ] Solution file includes ProjectDependencies section
- [ ] All exercise paths in tests are correct
- [ ] Test documentation is accurate and complete
- [ ] Exercise numbering is sequential and consistent
- [ ] All `.csproj` files reference correct FlinkDotNet projects
- [ ] LearningCourse/README.md updated with new day
- [ ] Day-specific README.md includes test instructions

### 📝 LESSONS LEARNED LOG

**When you encounter a NEW error or issue not listed above:**

1. **Document it immediately** in this section
2. Include: Problem description, Impact, Root cause, Solution
3. Add specific code examples showing wrong vs correct approach
4. Update the Pre-Update Checklist if applicable
5. This creates institutional knowledge for future updates

**Template for new entries:**
```markdown
#### [Number]. [Short Problem Title]
**Problem**: [Detailed description of what went wrong]

**Impact**:
- [Effect 1]
- [Effect 2]

**Root Cause**: [Why it happened]

**Solution**:
- [Step 1]
- [Step 2]

**Example:**
[Code or structure showing wrong vs correct]
```

### 🎯 SUCCESS PATTERNS

These patterns have proven effective across multiple Learning Course updates:

1. **Start with Day01 as Template**: Copy Day01 structure and systematically update
2. **Update in Phases**: Create projects → Update configs → Add tests → Validate
3. **Validate Early and Often**: Build after each major change
4. **Use Consistent Naming**: Follow established patterns exactly
5. **Document as You Go**: Update this file when encountering new issues
6. **Test Locally First**: Never commit without local validation
7. **Review Checklist**: Use Pre-Update Checklist every time

### 🚀 AUTOMATION OPPORTUNITIES

Consider automating these repetitive tasks:

- Script to create new day structure with correct numbering
- Validation script to check exercise numbering consistency
- Tool to verify all exercises have required files (global.json, .csproj, Program.cs)
- Script to update solution file with ProjectDependencies
- Automated test to validate Learning Course structure compliance
