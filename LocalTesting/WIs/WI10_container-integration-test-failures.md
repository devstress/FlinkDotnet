# WI10: Container Integration Test Failures

**File**: `LocalTesting/WIs/WI10_container-integration-test-failures.md`
**Title**: Fix container integration test failures (7/9 tests failing)
**Description**: Integration tests failing due to container infrastructure issues - need to investigate and fix
**Priority**: High
**Component**: LocalTesting.IntegrationTests
**Type**: Bug Investigation
**Assignee**: GitHub Copilot
**Created**: 2025-01-28
**Status**: Investigation

## Lessons Applied from Previous WIs

### Previous WI References
- WI9_integration-test-failures.md - JAR selection and Java version compatibility
- WI8_maven-build-resilience.md - Maven build improvements

### Lessons Applied  
- Debug-first approach before making changes
- Check infrastructure (Docker, Kafka, Flink) before blaming code
- Run tests locally to reproduce issues

### Problems Prevented
- Not making changes without understanding root cause
- Avoiding infrastructure changes when code fix is sufficient

## Phase 1: Investigation

### Requirements
- Understand why integration tests are failing
- Fix root cause to make all 9 tests pass
- Ensure containers start properly

### Debug Information (MANDATORY - Update this section for every investigation)

**Problem Statement**:
- User reports: "7/9 integration tests was working before but now all failed"
- Request: "investigate the containers and fix them all"

**Environment Status**:
```bash
dotnet --version
# Output: 9.0.305 ✅

docker ps -a
# Output: No containers running ❌

docker network ls
# Output: Only default networks (bridge, host, none) ❌
```

**Build Attempt**:
```bash
cd LocalTesting && dotnet build LocalTesting.sln --configuration Release
```

**Build Error**:
```
error MSB3073: The command "JAVA_HOME='/usr/lib/jvm/temurin-17-jdk-amd64' 
PATH='/usr/lib/jvm/temurin-17-jdk-amd64/bin:$PATH' MAVEN_OPTS='...' 'mvn' 
-B package -DskipTests -Pjava17" exited with code 127.

/usr/bin/sh: 2: mvn: not found
```

**Root Cause Analysis**:
1. **Maven is installed**: `which mvn` → `/usr/bin/mvn` ✅
2. **Maven verification passes**: Build script confirms Maven 3.9.11 is available ✅
3. **Build fails**: MSBuild Exec command can't find `mvn` ❌

**Why Maven not found**:
- Line 224 in `Flink.JobGateway.csproj`: `<MavenCommand Condition="'$(MavenFoundExitCode)' == '0'">mvn</MavenCommand>`
- Line 302: `PATH='$(EffectiveJavaHome)/bin:$PATH'`
- **Problem**: When PATH is overridden in Exec command, the `$PATH` shell variable doesn't expand properly in MSBuild context
- MSBuild sets PATH to only `/usr/lib/jvm/temurin-17-jdk-amd64/bin:$PATH`
- The `$PATH` doesn't expand to include `/usr/bin` where `mvn` is located
- Result: Maven command `mvn` cannot be found

**Key Observations**:
1. Maven exists at `/usr/bin/mvn` (full path)
2. Maven verification check passes (finds Maven in PATH)
3. Maven command is set to relative `mvn` instead of full path `/usr/bin/mvn`
4. When PATH is overridden in Exec, relative `mvn` fails
5. Solution: Get full path to Maven when found, not just `mvn`

### Findings

**Root Cause**: Maven command path resolution issue
- Maven is detected successfully with `which mvn` 
- But stored as relative `mvn` instead of full path `/usr/bin/mvn`
- When Exec command overrides PATH, `mvn` is no longer findable
- Need to capture full path from `which mvn` output

**Solution Approach**:
1. Modify line ~167-175 to capture full Maven path, not just exit code
2. Use full Maven path instead of relative `mvn` on line 224
3. Ensure PATH override in Exec still works with full Maven path

## Phase 2: Design ✅

### Requirements
Fix Maven path resolution to use full path instead of relative command

### Architecture Decisions

**Change Location**: `FlinkDotNet/Flink.JobGateway/Flink.JobGateway.csproj`

**Problem 1: Maven command not using full path**
- Line 167-175: `which mvn` exit code captured, but not the full path output
- Line 224: Set `MavenCommand` to relative `mvn` instead of full path `/usr/bin/mvn`

**Problem 2: PATH override breaks Maven execution**
- Line 309: `PATH='$(EffectiveJavaHome)/bin:$PATH'`
- MSBuild's `Exec` task doesn't expand `$PATH` shell variable correctly
- Maven script needs system commands like `uname`, `ls`, `expr`, `dirname`
- When PATH is set to only Java bin directory, these commands not found

**Proposed Fix 1**: Capture full Maven path from `which mvn` output
```xml
<!-- Before -->
<Exec Command="which mvn" ...>
  <Output TaskParameter="ExitCode" PropertyName="MavenFoundExitCode" />
</Exec>

<!-- After -->
<Exec Command="which mvn" ...>
  <Output TaskParameter="ExitCode" PropertyName="MavenFoundExitCode" />
  <Output TaskParameter="ConsoleOutput" PropertyName="MavenFullPathRaw" />
</Exec>

<!-- Then clean and use full path -->
<PropertyGroup>
  <MavenFullPath>$(MavenFullPathRaw.Trim().Split(...)[0].Trim())</MavenFullPath>
  <MavenCommand>$(MavenFullPath)</MavenCommand>
</PropertyGroup>
```

**Proposed Fix 2**: Use `/bin/bash -c` to ensure proper shell expansion
```xml
<!-- Before -->
<MavenJava17Command>JAVA_HOME='...' PATH='...:$PATH' ... 'mvn' ...</MavenJava17Command>

<!-- After -->
<MavenJava17Command>/bin/bash -c "JAVA_HOME='...' PATH='...:$PATH' ... '$(MavenCommand)' ..."</MavenJava17Command>
```

**Impact**:
- Maven full path captured: `/usr/bin/mvn`
- Shell properly expands `$PATH` variable in bash context
- Maven script can access system commands
- Build succeeds with Java 17 JAR compilation

### Why This Approach
- **Minimal changes**: Two small modifications to .csproj
- **Root cause fix**: Solves both path resolution and shell expansion issues
- **Backward compatible**: Fallback to `mvn` if full path not captured
- **Cross-platform**: Same fix works for both Linux/macOS and Windows
- **No infrastructure changes**: Works with existing Maven installation

### Alternatives Considered
1. **Don't override PATH in Exec**: Rejected - JAVA_HOME must be in PATH for Maven
2. **Include system PATH in override**: Rejected - complex and error-prone
3. **Use Maven from tools/**: Rejected - Maven already installed via CI
4. **Hardcode Maven path**: Rejected - not portable across environments

## Phase 3: TDD/BDD ✅

### Test Specifications
No new tests needed - fix resolves build failures

### Validation Approach
1. Build FlinkDotNet.sln to ensure Gateway builds
2. Build LocalTesting.sln to ensure all dependencies build
3. Verify Maven JAR is created
4. Run integration tests to confirm containers start

## Phase 4: Implementation ✅

### Code Changes

**File**: `FlinkDotNet/Flink.JobGateway/Flink.JobGateway.csproj`

**Change 1**: Lines 156-176 - Capture Maven full path from which/where commands

**Before**:
```xml
<!-- Check if Maven exists in PATH (Windows) -->
<Exec Command="where mvn" ...>
  <Output TaskParameter="ExitCode" PropertyName="MavenFoundExitCode" />
</Exec>

<!-- Check if Maven exists in PATH (Linux/macOS) -->
<Exec Command="which mvn" ...>
  <Output TaskParameter="ExitCode" PropertyName="MavenFoundExitCode" />
</Exec>
```

**After**:
```xml
<!-- Check if Maven exists in PATH and get full path (Windows) -->
<Exec Command="where mvn" ...>
  <Output TaskParameter="ExitCode" PropertyName="MavenFoundExitCode" />
  <Output TaskParameter="ConsoleOutput" PropertyName="MavenFullPathRaw" />
</Exec>

<!-- Check if Maven exists in PATH and get full path (Linux/macOS) -->
<Exec Command="which mvn" ...>
  <Output TaskParameter="ExitCode" PropertyName="MavenFoundExitCode" />
  <Output TaskParameter="ConsoleOutput" PropertyName="MavenFullPathRaw" />
</Exec>
```

**Change 2**: Lines 221-235 - Use full Maven path instead of relative command

**Before**:
```xml
<PropertyGroup>
  <!-- Priority 1: Use system Maven if found in PATH -->
  <MavenCommand Condition="'$(MavenFoundExitCode)' == '0'">mvn</MavenCommand>
  ...
</PropertyGroup>
```

**After**:
```xml
<PropertyGroup>
  <!-- Clean up Maven path output (remove trailing newlines/whitespace) -->
  <MavenFullPath Condition="'$(MavenFullPathRaw)' != ''">$([System.String]::Copy('$(MavenFullPathRaw)').Trim().Split(&#xD;&#xA;, System.StringSplitOptions.RemoveEmptyEntries)[0].Trim())</MavenFullPath>
  
  <!-- Priority 1: Use full path to system Maven if found in PATH -->
  <MavenCommand Condition="'$(MavenFoundExitCode)' == '0' AND '$(MavenFullPath)' != ''">$(MavenFullPath)</MavenCommand>
  <!-- Priority 2: Use relative mvn if found but full path not captured -->
  <MavenCommand Condition="'$(MavenCommand)' == '' AND '$(MavenFoundExitCode)' == '0'">mvn</MavenCommand>
  ...
</PropertyGroup>
```

**Change 3**: Line 309 - Use bash -c wrapper for proper shell variable expansion

**Before**:
```xml
<MavenJava17Command Condition="'$(IsLinux)' == 'true' OR '$(IsMacOS)' == 'true'">JAVA_HOME='$(EffectiveJavaHome)' PATH='$(EffectiveJavaHome)/bin:$PATH' MAVEN_OPTS='...' '$(MavenCommand)' -B package -DskipTests -Pjava17</MavenJava17Command>
```

**After**:
```xml
<MavenJava17Command Condition="'$(IsLinux)' == 'true' OR '$(IsMacOS)' == 'true'">/bin/bash -c "JAVA_HOME='$(EffectiveJavaHome)' PATH='$(EffectiveJavaHome)/bin:$PATH' MAVEN_OPTS='...' '$(MavenCommand)' -B package -DskipTests -Pjava17"</MavenJava17Command>
```

### Build Validation

**Build Results**: ✅ SUCCESS
```
Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:28.85
```

**Maven Build Output**:
```
(Flink.JobGateway) Build tools verification complete. Maven: /usr/bin/mvn
(Flink.JobGateway) Building FlinkIRRunner (Java 17 compatibility)...
[INFO] Scanning for projects...
[INFO] Building Flink IR Runner 1.0.0
...
[INFO] BUILD SUCCESS
[INFO] Total time:  5.935 s
```

**JAR Output Validated**:
- FlinkIRRunner JAR built successfully
- LocalTesting solution builds completely
- All projects compile without errors

## Phase 5: Testing & Validation

### Test Results

**Build Status**: ✅ SUCCESS
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:28.85
```

**Integration Test Results**: ❌ ALL 9 TESTS FAILED

**Test Summary**:
- Total: 9 tests
- Passed: 0 tests
- Failed: 9 tests
- Duration: 157.4s

**Key Observations**:
1. ✅ Containers START successfully (Flink JobManager, TaskManager)
2. ✅ Jobs SUBMIT successfully and reach RUNNING state
3. ❌ Kafka container NOT FOUND by diagnostic tests
4. ❌ NO messages consumed from Kafka topics (0 messages received)
5. ❌ Flink jobs running but not processing data

**Infrastructure Status During Tests**:
- Flink JobManager: ✅ Ready and accessible at http://localhost:44217/
- Flink TaskManager: ✅ Connected and ready
- Gateway: ✅ Ready and accessible at http://localhost:8080/
- Kafka: ❌ **Container not found** - Root cause of all test failures
- SQL Gateway: ℹ️ Not started (expected, optional component)

**Error Pattern**:
```
%3|ERROR|rdkafka#producer-1| localhost:33011/bootstrap: Disconnected while requesting ApiVersion
%3|ERROR|rdkafka#producer-1| 1/1 brokers are down
```

**Diagnostic Test Output**:
```
❌ NETWORK DIAGNOSTIC TEST FAILED
Error: No Kafka container found
```

**Docker Container Check** (after tests):
```bash
$ docker ps -a
CONTAINER ID   IMAGE     COMMAND   CREATED   STATUS    PORTS     NAMES
# No containers found
```

### Root Cause Analysis

**Problem**: Kafka container not starting or not visible to Docker CLI

**Possible Causes**:
1. **Aspire uses different container runtime**: Aspire might be using internal DCP instead of Docker
2. **Containers cleanup too early**: Aspire cleans up containers after tests finish
3. **Kafka image pull failure**: Kafka container image not available or failing to start
4. **Aspire configuration issue**: AddKafka() not working as expected in test environment
5. **Port conflict**: Kafka port allocation conflicting with system ports

**Evidence**:
- Tests report "Kafka resource reported healthy" but container not findable
- Port 33011 allocated by Aspire but broker not responding
- All Flink infrastructure works, only Kafka missing
- Diagnostic test specifically fails on "No Kafka container found"

**Next Steps for Investigation**:
1. Check Aspire logs during test execution
2. Verify Kafka container image availability
3. Test Kafka container startup manually outside Aspire
4. Check if Aspire DCP is using Podman instead of Docker
5. Verify AddKafka() generates correct container configuration

### Performance Impact
- Build time: ✅ Improved from failure to 28.85s
- Test execution time: 157.4s (all tests run to timeout waiting for Kafka messages)
- Infrastructure startup: Successful for Flink, failing for Kafka

## Phase 6: Kafka Container Investigation

### Current Status
- ✅ Maven path issue FIXED
- ✅ Build successful
- ✅ Flink containers starting
- ❌ Kafka container NOT starting - requires further investigation

### Problem Identified
The original problem "7/9 tests failing" has revealed TWO issues:
1. **Maven path issue** (FIXED) - preventing build from completing
2. **Kafka container issue** (OPEN) - preventing tests from passing

The Maven fix was necessary but not sufficient. The Kafka container is the remaining blocker.
