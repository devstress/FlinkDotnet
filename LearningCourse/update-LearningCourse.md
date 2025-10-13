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
- Check Docker Desktop or Podman is running
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

#### 2. ~~Missing global.json Files in Exercise Solutions~~ **OBSOLETE - DO NOT USE**

**CRITICAL UPDATE**: Do NOT create individual `global.json` files in exercise solution directories.

**Correct Approach**:
- **USE ROOT global.json ONLY**: All exercises inherit from the root `/global.json` file
- **Location**: `FlinkDotnet/global.json` (repository root)
- **Reason**: Ensures consistent .NET SDK version across entire repository
- **Impact of Wrong Approach**: Creates version conflicts and maintenance issues

**What NOT to Do**:
```
❌ WRONG - Do not create these:
Day02-Flink21-Fundamentals/Exercise-Solutions/Exercise21/global.json
Day02-Flink21-Fundamentals/Exercise-Solutions/Exercise22/global.json
```

**What to Do**:
```
✅ CORRECT - Use root global.json:
FlinkDotnet/global.json (repository root - already exists)
└── LearningCourse/
    └── DayXX-Topic-Name/
        └── Exercise-Solutions/
            └── ExerciseXY/ (inherits from root global.json)
```

**Root global.json Content**:
```json
{
  "sdk": {
    "version": "9.0.303",
    "rollForward": "latestFeature"
  }
}
```

**Validation**:
```bash
# Verify exercises use root global.json
cd LearningCourse/DayXX-Topic-Name/Exercise-Solutions/ExerciseXY
dotnet --version  # Should use version from root global.json (9.0.x)

# Check that NO local global.json exists
ls global.json  # Should fail (file not found)
```

**Previous Incorrect Guidance**:
- ~~"Every exercise solution MUST have a `global.json` file"~~ - **INCORRECT**
- ~~"Copy from template or existing exercise"~~ - **INCORRECT**

**Corrected Guidance**:
- **NEVER create exercise-level global.json files**
- **ALWAYS rely on root global.json**
- **VERIFY no local global.json files exist in exercises**

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

#### 11. Shared Infrastructure Setup Pattern (CRITICAL)
**Problem**: Each test assembly (Day01, Day02, etc.) launching its own separate Aspire infrastructure instance

**Impact**:
- Multiple Aspire instances running simultaneously (16+ containers instead of 8)
- Excessive resource consumption (memory, CPU, ports)
- Longer test execution time (each assembly waits for infrastructure startup)
- Port conflicts when parallel test execution attempted
- Kafka connectivity issues from infrastructure race conditions
- Inconsistent test results between sequential vs parallel execution

**Root Cause**:
- NUnit `[SetUpFixture]` runs independently per test assembly
- Each test assembly's SetUpFixture calls infrastructure setup
- Without idempotency, multiple infrastructure instances spawn
- Tests run against different infrastructure instances

**Solution - MANDATORY Shared Setup Pattern**:

1. **Create SetUpFixture in each test assembly** to call shared setup:
   ```csharp
   // In each DayXX.IntegrationTests/SetUpFixture.cs
   using LearningCourse.IntegrationTests;
   using NUnit.Framework;
   
   [SetUpFixture]
   public class SetUpFixture
   {
       [OneTimeSetUp]
       public void GlobalSetup()
       {
           LearningCourseTestBase.GlobalSetUp();
       }
   
       [OneTimeTearDown]
       public void GlobalTearDown()
       {
           LearningCourseTestBase.GlobalTearDown();
       }
   }
   ```

2. **Make GlobalSetUp/GlobalTearDown idempotent** in LearningCourseTestBase:
   ```csharp
   // In LearningCourse.IntegrationTests/LearningCourseTestBase.cs
   private static bool _isSetupComplete = false;
   private static readonly object _setupLock = new object();
   
   public static void GlobalSetUp()
   {
       lock (_setupLock)
       {
           if (_isSetupComplete)
           {
               return; // Already setup, skip
           }
           
           // Start infrastructure once
           StartInfrastructure();
           _isSetupComplete = true;
       }
   }
   
   public static void GlobalTearDown()
   {
       lock (_setupLock)
       {
           if (!_isSetupComplete)
           {
               return; // Already torn down, skip
           }
           
           // Stop infrastructure once
           StopInfrastructure();
           _isSetupComplete = false;
       }
   }
   ```

**Validation**:
```bash
# Run all LearningCourse tests
dotnet test LearningCourse/IntegrationTests.sln --configuration Release

# During test execution, verify only ONE Aspire instance (8 containers)
docker ps --format "table {{.Names}}\t{{.Image}}"

# Should see exactly 8 containers:
# - kafka-1, kafka-2, kafka-3 (Kafka cluster)
# - flink-jobmanager, flink-taskmanager-1, flink-taskmanager-2, flink-taskmanager-3
# - temporal-server
```

**NUnit Parallel Execution Configuration**:
NUnit runs test assemblies in parallel by default. To ensure only ONE infrastructure instance:

Create `LearningCourse/runsettings.xml`:
```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <RunConfiguration>
    <!-- Run test assemblies sequentially -->
    <MaxCpuCount>1</MaxCpuCount>
    <DisableParallelization>true</DisableParallelization>
  </RunConfiguration>
  <NUnit>
    <!-- Prevent parallel test execution within assemblies -->
    <NumberOfTestWorkers>1</NumberOfTestWorkers>
  </NUnit>
</RunSettings>
```

Run tests with runsettings:
```bash
dotnet test LearningCourse/IntegrationTests.sln --settings LearningCourse/runsettings.xml --configuration Release
```

**Common Mistakes**:
- ❌ Not creating SetUpFixture in each test assembly
- ❌ Calling infrastructure setup directly from test constructor
- ❌ Not using locks for thread safety
- ❌ Not checking setup completion flag before starting infrastructure
- ❌ Assuming NUnit automatically shares setup across assemblies

**Reference Implementation**:
- `LearningCourse/Day01-Kafka-Flink-Data-Pipeline/Day01.IntegrationTests/SetUpFixture.cs`
- `LearningCourse/Day02-Flink21-Fundamentals/Day02.IntegrationTests/SetUpFixture.cs`
- `LearningCourse/LearningCourse.IntegrationTests/LearningCourseTestBase.cs` (lines with `_isSetupComplete` and `_setupLock`)

#### 12. Web Services vs Console Applications (CRITICAL)
**Problem**: Creating ASP.NET Core web services with `await app.RunAsync()` for Learning Course exercises

**Impact**:
- Integration tests timeout after 3 minutes waiting for process to exit
- Tests cannot validate exercise completion
- ALL tests fail with `System.TimeoutException`
- Blocks Learning Course progression
- Fundamentally incompatible with test expectations

**Root Cause**: 
- Web services run indefinitely with `await app.RunAsync()`
- Tests expect console applications that complete and exit with code 0
- Tests look for completion markers ("COMPLETED", "SUCCESS", "✅") in output
- Tests cannot validate functionality if exercise never terminates

**Solution - MANDATORY Console Application Pattern**:
- **NEVER use `await app.RunAsync()`** in Learning Course exercises
- **ALWAYS create console applications** that execute work and exit
- **ALWAYS print completion markers** so tests can validate
- **ALWAYS exit with code 0** on success, code 1 on failure

**Example - WRONG (Web Service)**:
```csharp
// ❌ BAD - Runs forever, tests timeout
var app = builder.Build();
app.MapGet("/health", () => "Healthy");
await app.RunAsync();  // Never terminates
```

**Example - CORRECT (Console Application)**:
```csharp
// ✅ GOOD - Does work, prints results, exits
Console.WriteLine(">> Step 1: Validating infrastructure...");
await ValidateInfrastructure();

Console.WriteLine(">> Step 2: Running tests...");
await RunTests();

Console.WriteLine("================================================================================");
Console.WriteLine("  EXERCISE COMPLETED SUCCESSFULLY!");
Console.WriteLine("================================================================================");
Console.WriteLine("✅ All validations passed");
Environment.Exit(0);  // Terminates cleanly
```

**Reference Implementation**: 
See `Day01-Kafka-Flink-Data-Pipeline/Exercise-Solutions/Exercise1-StringCapitalize/Program.cs` for correct pattern.

**Validation**:
- Exercise must complete within 3 minutes
- Output must contain "COMPLETED" or "SUCCESS" or "✅"
- Process must exit with code 0
- No `app.RunAsync()` or other indefinite loops

**Testing Your Exercise**:
```bash
# Exercise should complete and exit
cd LearningCourse/DayXX-Topic-Name/Exercise-Solutions/ExerciseXY
dotnet run

# Verify it exits (not stuck running)
echo $?  # Should print 0 (success exit code)
```


#### 13. Multiple Test Assembly Anti-Pattern (CRITICAL - CONSOLIDATED STRUCTURE REQUIRED)
**Problem**: Creating separate test assemblies per day (Day01.IntegrationTests, Day02.IntegrationTests) instead of using a single consolidated test project

**Impact**:
- Multiple Aspire infrastructure instances launch simultaneously (one per test assembly)
- Excessive container proliferation (16+ containers instead of 8)
- Race conditions during infrastructure setup causing test failures
- Infrastructure torn down prematurely while other tests still running
- Kafka connectivity failures from timing issues
- Significantly longer test execution time
- Resource exhaustion on CI/CD systems
- Unpredictable test results between runs

**Root Cause**:
- NUnit runs test assemblies in parallel by default
- Each test assembly's `[SetUpFixture]` executes independently
- Even with idempotent setup patterns, timing issues persist
- Infrastructure lifecycle tied to individual assembly execution

**Solution - MANDATORY Single Test Assembly Pattern**:

**NEW STRUCTURE (Required as of 2025-01-13):**
```
LearningCourse/
├── LearningCourse.IntegrationTests/
│   ├── LearningCourse.IntegrationTests.csproj
│   ├── LearningCourseTestBase.cs (shared infrastructure)
│   ├── Day01Tests.cs (all Day 01 tests)
│   ├── Day02Tests.cs (all Day 02 tests)
│   ├── Day03Tests.cs (all Day 03 tests)
│   └── ... (one test file per day)
├── Day01-Kafka-Flink-Data-Pipeline/
│   └── Exercise-Solutions/ (NO test project)
├── Day02-Flink21-Fundamentals/
│   └── Exercise-Solutions/ (NO test project)
└── IntegrationTests.sln
```

**OLD STRUCTURE (DEPRECATED - DO NOT USE):**
```
❌ WRONG - Creates multiple test assemblies:
LearningCourse/
├── Day01-Kafka-Flink-Data-Pipeline/
│   ├── Day01.IntegrationTests/ ❌ Separate assembly
│   └── Exercise-Solutions/
├── Day02-Flink21-Fundamentals/
│   ├── Day02.IntegrationTests/ ❌ Separate assembly
│   └── Exercise-Solutions/
```

**Migration Steps for Existing Days**:

1. **Create consolidated test files** in `LearningCourse.IntegrationTests/`:
   ```bash
   cd LearningCourse/LearningCourse.IntegrationTests
   
   # Move Day01 tests
   # Copy content from Day01-Kafka-Flink-Data-Pipeline/Day01.IntegrationTests/ExerciseExecutionTests.cs
   # to new file Day01Tests.cs
   
   # Move Day02 tests
   # Copy content from Day02-Flink21-Fundamentals/Day02.IntegrationTests/ExerciseExecutionTests.cs
   # to new file Day02Tests.cs
   ```

2. **Update namespace and class names**:
   ```csharp
   // In Day01Tests.cs
   namespace LearningCourse.IntegrationTests;  // Changed from Day01.IntegrationTests
   
   public class Day01Tests : LearningCourseTestBase  // Changed from ExerciseExecutionTests
   {
       // Tests remain the same
   }
   ```

3. **Remove per-day test project directories**:
   ```bash
   rm -rf LearningCourse/Day01-Kafka-Flink-Data-Pipeline/Day01.IntegrationTests
   rm -rf LearningCourse/Day02-Flink21-Fundamentals/Day02.IntegrationTests
   ```

4. **Update solution file** to remove old test projects:
   - Remove `Day01.IntegrationTests` project entry
   - Remove `Day02.IntegrationTests` project entry
   - Move exercise project dependencies to `LearningCourse.IntegrationTests`
   - Update configuration platforms to remove old test projects

5. **Update LearningCourse.IntegrationTests.csproj** with exercise dependencies:
   ```xml
   <ItemGroup>
     <!-- Day01 exercise dependencies -->
     <ProjectReference Include="..\Day01-Kafka-Flink-Data-Pipeline\Exercise-Solutions\Exercise1-StringCapitalize\Exercise1-StringCapitalize.csproj" />
     <ProjectReference Include="..\Day01-Kafka-Flink-Data-Pipeline\Exercise-Solutions\Exercise2-BackupAggregator\Exercise2-BackupAggregator.csproj" />
     
     <!-- Day02 exercise dependencies -->
     <ProjectReference Include="..\Day02-Flink21-Fundamentals\Exercise-Solutions\Exercise21\Exercise21.csproj" />
     <ProjectReference Include="..\Day02-Flink21-Fundamentals\Exercise-Solutions\Exercise22\Exercise22.csproj" />
     <ProjectReference Include="..\Day02-Flink21-Fundamentals\Exercise-Solutions\Exercise23\Exercise23.csproj" />
     <ProjectReference Include="..\Day02-Flink21-Fundamentals\Exercise-Solutions\Exercise24\Exercise24.csproj" />
   </ItemGroup>
   ```

**Benefits of Consolidated Structure**:
- ✅ Single infrastructure instance for all tests
- ✅ Exactly 8 containers during test execution
- ✅ Faster test execution (no infrastructure startup per assembly)
- ✅ Reliable test results (no race conditions)
- ✅ Simpler maintenance (one test project to manage)
- ✅ Natural test categorization by day
- ✅ Easier CI/CD configuration

**Test File Template for New Days**:
```csharp
using NUnit.Framework;

namespace LearningCourse.IntegrationTests;

/// <summary>
/// Integration tests for Day [X]: [Topic Name]
/// 
/// Reference: [URL]
/// </summary>
[TestFixture]
[Category("day[X]-topic-name")]
[Category("integration")]
public class Day[X]Tests : LearningCourseTestBase
{
    private const string Exercise1Path = "Day[X]-Topic-Name/Exercise-Solutions/Exercise[X]1";
    private static readonly TimeSpan ExerciseTimeout = TimeSpan.FromMinutes(3);
    
    [Test]
    [Description("Exercise [X].1: [Name]")]
    public async Task Exercise1_Name_ShouldExecuteSuccessfully()
    {
        // Test implementation
    }
}
```

**Validation**:
```bash
# Build consolidated structure
dotnet build LearningCourse/IntegrationTests.sln --configuration Release

# Run all tests (should use single infrastructure)
dotnet test LearningCourse/LearningCourse.IntegrationTests/LearningCourse.IntegrationTests.csproj --configuration Release

# Verify only 8 containers running during tests
docker ps --format "table {{.Names}}\t{{.Image}}" | grep -E "kafka|flink|temporal"

# Expected output: exactly 8 containers
# kafka-1, kafka-2, kafka-3
# flink-jobmanager, flink-taskmanager-1, flink-taskmanager-2, flink-taskmanager-3
# temporal-server
```

**Common Mistakes**:
- ❌ Creating new per-day test projects (Day03.IntegrationTests, etc.)
- ❌ Not removing old per-day test project folders
- ❌ Not updating solution file to remove old test projects
- ❌ Not adding exercise dependencies to consolidated test project
- ❌ Forgetting to update namespace from DayXX.IntegrationTests to LearningCourse.IntegrationTests

**Reference Implementation**:
- `LearningCourse/LearningCourse.IntegrationTests/Day01Tests.cs` - Day 01 consolidated tests
- `LearningCourse/LearningCourse.IntegrationTests/Day02Tests.cs` - Day 02 consolidated tests
- `LearningCourse/LearningCourse.IntegrationTests/LearningCourseTestBase.cs` - Shared infrastructure base class

**MANDATORY for all new Learning Course days**: Use the consolidated test structure. DO NOT create per-day test assemblies.

- Use lowercase, hyphenated naming
- Enables selective test execution

### 📋 PRE-UPDATE CHECKLIST

Before updating ANY Learning Course, verify:

- [ ] **Reviewed this "Common Errors" section completely**
- [ ] Checked exercise numbering follows sequential pattern (Day[N][1-4])
- [ ] **Verified NO exercise-level global.json files exist** (use root global.json only)
- [ ] Confirmed `.csproj` includes all required FlinkDotNet project references
- [ ] Validated all projects target `net9.0` framework
- [ ] **Verified exercises are console applications, NOT web services** (no `app.RunAsync()`)
- [ ] **Planned completion markers** in exercise output ("COMPLETED", "SUCCESS", "✅")
- [ ] **Planned to create SetUpFixture.cs** in new test assembly for shared infrastructure
- [ ] Reviewed Day01 implementation as reference
- [ ] Prepared to manually edit `.sln` file for ProjectDependencies
- [ ] Ready to update test path constants after copy-paste
- [ ] Planned comprehensive validation checks for each test
- [ ] Confirmed test descriptions match actual exercise content

### 🔍 POST-UPDATE VALIDATION

After completing Learning Course update, verify:

- [ ] Build succeeds: `dotnet build LearningCourse/IntegrationTests.sln --configuration Release`
- [ ] Tests discovered: `dotnet test --list-tests --filter "FullyQualifiedName~DayXX"`
- [ ] **Tests execute successfully** with expected output (no timeouts)
- [ ] **All exercises complete within 3 minutes** (no indefinite loops)
- [ ] **Exercise output contains completion markers** ("COMPLETED", "SUCCESS", "✅")
- [ ] **Exercises exit with code 0** (verified by running `dotnet run` manually)
- [ ] **SetUpFixture.cs created** in DayXX.IntegrationTests calling shared setup
- [ ] **Only ONE Aspire instance runs** when executing all LearningCourse tests (verify with `docker ps`)
- [ ] **Container count is 8** (3 Kafka + 4 Flink + 1 Temporal) during full test run
- [ ] Solution file includes ProjectDependencies section
- [ ] All exercise paths in tests are correct
- [ ] Test documentation is accurate and complete
- [ ] Exercise numbering is sequential and consistent
- [ ] All `.csproj` files reference correct FlinkDotNet projects
- [ ] **NO exercise-level global.json files exist** (verified removed if present)
- [ ] **Root global.json specifies correct .NET version** (9.0.303+)
- [ ] **NO web services** (`app.RunAsync()`) in exercise code
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

## LocalTesting Infrastructure Integration

### ⚠️ CRITICAL: Extra Components for Learning Courses

**When Learning Courses require additional infrastructure components beyond the base LocalTesting stack:**

#### ExtraComponentsFromLearningCourse Flag

**Purpose**: Some Learning Course days require additional observability or infrastructure components (Grafana, Prometheus, OpenTelemetry, etc.) that are not part of the base LocalTesting stack.

**Implementation**:

1. **Add Flag to LocalTesting AppHost**:
   ```csharp
   // In LocalTesting.FlinkSqlAppHost/Program.cs
   var builder = DistributedApplication.CreateBuilder(args);
   
   // Check for extra components flag
   var useExtraComponents = builder.Configuration.GetValue<bool>("ExtraComponentsFromLearningCourse", false);
   
   if (useExtraComponents)
   {
       // Add Grafana for observability
       var grafana = builder.AddContainer("grafana", "grafana/grafana")
           .WithHttpEndpoint(port: 3000, targetPort: 3000, name: "grafana-ui")
           .WithEnvironment("GF_SECURITY_ADMIN_PASSWORD", "admin")
           .WithEnvironment("GF_SECURITY_ADMIN_USER", "admin");
       
       // Add Prometheus for metrics
       var prometheus = builder.AddContainer("prometheus", "prom/prometheus")
           .WithHttpEndpoint(port: 9090, targetPort: 9090, name: "prometheus-ui")
           .WithBindMount("./prometheus.yml", "/etc/prometheus/prometheus.yml");
       
       // Add OpenTelemetry Collector
       var otelCollector = builder.AddContainer("otel-collector", "otel/opentelemetry-collector")
           .WithHttpEndpoint(port: 4317, targetPort: 4317, name: "otel-grpc")
           .WithHttpEndpoint(port: 4318, targetPort: 4318, name: "otel-http");
   }
   ```

2. **Enable in appsettings.json**:
   ```json
   {
     "ExtraComponentsFromLearningCourse": true
   }
   ```

3. **Enable via Command Line**:
   ```bash
   dotnet run --project LocalTesting.FlinkSqlAppHost --ExtraComponentsFromLearningCourse=true
   ```

#### When to Use Extra Components

**Required for these Learning Course days:**
- **Day02-Flink21-Fundamentals**: Requires Grafana, Prometheus for SRE observability exercises
- **Day05-Enterprise-Observability**: Requires full observability stack (Grafana, Prometheus, OpenTelemetry)
- **Day08-Stress-Testing**: Requires Grafana for performance monitoring during load tests
- **Day14-Advanced-Testing-Chaos-Engineering**: Requires monitoring stack for chaos experiment observation

**Base LocalTesting stack includes:**
- Apache Flink cluster (JobManager + TaskManagers)
- Apache Kafka cluster (3 brokers)
- Temporal workflow engine
- PostgreSQL database
- Redis cache

**Extra components add:**
- Grafana dashboards for visualization
- Prometheus for metrics collection
- OpenTelemetry Collector for distributed tracing
- Additional monitoring and observability tools

#### Configuration Best Practices

**1. Environment-Specific Configuration**:
```json
{
  "Environments": {
    "Development": {
      "ExtraComponentsFromLearningCourse": false
    },
    "LearningCourse": {
      "ExtraComponentsFromLearningCourse": true
    },
    "Production": {
      "ExtraComponentsFromLearningCourse": false
    }
  }
}
```

**2. Day-Specific Requirements**:
Document in each day's README.md which extra components are needed:

```markdown
## Prerequisites

### Infrastructure Requirements
- Base LocalTesting stack (always required)
- **Extra Components**: Grafana, Prometheus (required for this day)

### Starting Infrastructure
\`\`\`bash
# Enable extra components for this Learning Course day
dotnet run --project LocalTesting.FlinkSqlAppHost --ExtraComponentsFromLearningCourse=true
\`\`\`
```

**3. Test Configuration**:
Update integration tests to check for required components:

```csharp
[SetUp]
public async Task Setup()
{
    // Verify base stack is running
    await VerifyFlinkClusterAsync();
    await VerifyKafkaClusterAsync();
    
    // Verify extra components if required by this day
    if (RequiresExtraComponents)
    {
        await VerifyGrafanaAsync();
        await VerifyPrometheusAsync();
        await VerifyOpenTelemetryAsync();
    }
}
```

#### Documentation Requirements

**MANDATORY: Update these files when adding extra components:**

1. **Day README.md** - Document infrastructure requirements:
   ```markdown
   ## Infrastructure Setup
   
   This Learning Course day requires additional observability components.
   
   **Required Components**:
   - ✅ Base LocalTesting stack (Flink, Kafka, Temporal)
   - ✅ Grafana (visualization)
   - ✅ Prometheus (metrics)
   - ✅ OpenTelemetry (tracing)
   
   **Start with Extra Components**:
   \`\`\`bash
   dotnet run --project ../../LocalTesting/LocalTesting.FlinkSqlAppHost --ExtraComponentsFromLearningCourse=true
   \`\`\`
   ```

2. **Exercise README.md** - Specify component access:
   ```markdown
   ## Accessing Observability Components
   
   - Grafana Dashboard: http://localhost:3000 (admin/admin)
   - Prometheus UI: http://localhost:9090
   - OpenTelemetry Endpoint: http://localhost:4318
   ```

3. **Integration Test Comments** - Document component dependencies:
   ```csharp
   /// <summary>
   /// Exercise 2.3: Observability Dashboard
   ///
   /// REQUIRES: ExtraComponentsFromLearningCourse=true
   /// - Grafana for dashboard visualization
   /// - Prometheus for metrics collection
   /// - OpenTelemetry for distributed tracing
   /// </summary>
   ```

#### Troubleshooting Extra Components

**Common Issues**:

1. **Components Not Starting**:
   ```bash
   # Verify flag is enabled
   dotnet run --project LocalTesting.FlinkSqlAppHost --ExtraComponentsFromLearningCourse=true
   
   # Check Docker containers
   docker ps | grep -E "grafana|prometheus|otel"
   ```

2. **Port Conflicts**:
   - Grafana default: 3000 (check with `netstat -an | findstr 3000`)
   - Prometheus default: 9090
   - OpenTelemetry: 4317 (gRPC), 4318 (HTTP)

3. **Configuration Issues**:
   ```bash
   # Verify appsettings.json
   cat LocalTesting.FlinkSqlAppHost/appsettings.json | grep ExtraComponents
   
   # Check environment variables
   echo $ExtraComponentsFromLearningCourse
   ```

#### Pre-Update Checklist Addition

Add to existing checklist when creating new Learning Course days:

- [ ] **Determined if extra components are needed** for this day's exercises
- [ ] **Documented component requirements** in day README.md
- [ ] **Updated exercise READMEs** with component access URLs
- [ ] **Added component verification** to integration test setup
- [ ] **Tested with ExtraComponentsFromLearningCourse=true** flag enabled
- [ ] **Verified all component health checks** pass before exercise execution

#### Post-Update Validation Addition

Add to existing validation when completing Learning Course days:

- [ ] **Extra components start successfully** when flag is enabled
- [ ] **Component health checks pass** (Grafana, Prometheus, OpenTelemetry)
- [ ] **Exercise tests pass** with extra components running
- [ ] **Documentation includes** component access instructions
- [ ] **Troubleshooting guide updated** for component-specific issues
