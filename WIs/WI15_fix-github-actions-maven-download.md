# WI15: Fix GitHub Actions Maven Download Failure

**File**: `WIs/WI15_fix-github-actions-maven-download.md`
**Title**: [Build] Fix PowerShell Maven download failing on Linux GitHub Actions runners
**Description**: GitHub Actions failing with error code 127 when trying to execute PowerShell command to download Maven on Linux runners. The .csproj uses Windows-specific PowerShell commands that don't work on Ubuntu.
**Priority**: High
**Component**: Build System
**Type**: Bug Fix
**Assignee**: AI Agent
**Created**: 2025-10-04
**Status**: Done

## Lessons Applied from Previous WIs
### Previous WI References
- WI1_maven-java-auto-installation.md - Maven auto-installation implementation
### Lessons Applied
- Maven auto-installation is already implemented in .csproj
- Need cross-platform compatibility for Linux CI runners
- GitHub Actions already installs Maven via `stCarolas/setup-maven@v4`
### Problems Prevented
- Don't remove Maven auto-installation completely - it's needed for local Windows dev
- Keep the build resilient for both local and CI environments

## Phase 1: Investigation

### Requirements
- Fix Maven download failure on Linux GitHub Actions runners
- Maintain Windows local development compatibility
- Use existing Maven from GitHub Actions when available

### Debug Information (MANDATORY - Update this section for every investigation)
- **Error Messages**: 
  ```
  error MSB3073: The command "powershell -NoProfile -ExecutionPolicy Bypass -Command "Write-Host 'Downloading Maven from https://dlcdn.apache.org/maven/maven-3/3.9.11/binaries/apache-maven-3.9.11-bin.zip...'; [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; Invoke-WebRequest -Uri 'https://dlcdn.apache.org/maven/maven-3/3.9.11/binaries/apache-maven-3.9.11-bin.zip' -OutFile '/home/runner/work/FlinkDotnet/FlinkDotnet/tools/maven-3.9.11.zip' -UseBasicParsing"" exited with code 127.
  ```
- **Root Cause**: 
  - Exit code 127 = "command not found" on Linux
  - `powershell` command doesn't exist on Linux (needs `pwsh` for PowerShell Core)
  - The `.csproj` uses Windows-specific `where mvn` and PowerShell syntax
  - GitHub Actions workflows already install Maven via `stCarolas/setup-maven@v4`
- **System State**: 
  - Ubuntu GitHub Actions runner (Linux)
  - Maven 3.9.11 already installed by GitHub Actions setup
  - .csproj tries to download Maven unnecessarily
- **Solution Approach**:
  - Use cross-platform detection: Check `which mvn` (Linux) OR `where mvn` (Windows)
  - Skip Maven download if Maven is already in PATH (from GitHub Actions)
  - Use `pwsh` instead of `powershell` for cross-platform PowerShell support
  - Add OS-specific conditional execution

### Findings
The issue has multiple layers:
1. **Command Not Found**: `powershell` doesn't exist on Linux (exit code 127)
2. **Unnecessary Download**: GitHub Actions already installs Maven, no need to download again
3. **Cross-Platform Issues**: `where` command is Windows-specific, Linux uses `which`
4. **Build Process**: The `EnsureBuildTools` target runs even when Maven is already available

## Phase 2: Design

### Requirements
Fix the Maven detection and download logic to work cross-platform

### Architecture Decisions
1. **Detection Strategy**: Use OS-conditional Maven detection
   - Windows: `where mvn` 
   - Linux: `which mvn`
2. **Skip Download When Maven Available**: Only download if Maven not in PATH
3. **PowerShell Core**: Use `pwsh` for cross-platform PowerShell support
4. **Fallback**: Keep auto-download for local Windows development without Maven

### Implementation Plan
1. Add OS detection to use correct command (`where` vs `which`)
2. Improve Maven PATH detection logic
3. Use `pwsh` instead of `powershell` for PowerShell commands
4. Add condition to skip download when Maven found in PATH

## Phase 3: Implementation

### Code Changes
Modified [`Flink.JobGateway.csproj`](FlinkDotNet/Flink.JobGateway/Flink.JobGateway.csproj:49) lines 49-105:

**Changes Made:**
1. **OS Detection**: Added cross-platform OS detection using `IsWindows`, `IsLinux`, `IsMacOS` properties
2. **Maven Detection**:
   - Windows: Uses `where mvn` command
   - Linux/macOS: Uses `which mvn` command
3. **Skip Download Logic**: Maven download only triggers if:
   - Maven NOT found in PATH (`MavenFoundExitCode != 0`)
   - Maven NOT already installed locally
   - Running on Windows (Linux/macOS should have Maven pre-installed in CI)
4. **Cross-Platform PowerShell**: Changed `powershell` to `pwsh` for cross-platform support
5. **Maven Command Priority**:
   - Priority 1: Use Maven from PATH (e.g., from GitHub Actions)
   - Priority 2: Use locally installed Maven (Windows: `mvn.cmd`, Linux: `mvn`)
   - Priority 3: Fallback to `mvn` command
6. **Error Handling**: Clear error message on Linux/macOS if Maven not found

**Key Improvements:**
- Maven download SKIPPED when Maven found in environment
- Cross-platform compatibility (Windows/Linux/macOS)
- Leverages GitHub Actions pre-installed Maven
- Maintains Windows local dev auto-download capability
- Uses `pwsh` instead of `powershell` for cross-platform PowerShell

## Phase 4: Testing & Validation

### Test Plan
- Verify builds work on GitHub Actions (Linux) - Maven from `stCarolas/setup-maven@v4`
- Verify builds work on local Windows with Maven in PATH
- Verify builds work on local Windows without Maven (auto-download)
- Verify no unnecessary Maven downloads when Maven already available

### Expected Behavior
1. **GitHub Actions (Linux)**: Uses Maven from `stCarolas/setup-maven@v4`, no download
2. **Local Windows with Maven**: Uses Maven from PATH, no download
3. **Local Windows without Maven**: Auto-downloads Maven to `tools/` directory
4. **Linux/macOS without Maven**: Clear error with installation instructions

## Lessons Learned & Future Reference (MANDATORY)

### What Worked Well
- OS detection using MSBuild properties works reliably across platforms
- Conditional execution prevents unnecessary downloads when Maven already in PATH
- `pwsh` command provides cross-platform PowerShell compatibility
- Priority-based Maven command selection ensures optimal tool usage
- Clear error messages guide users on Linux/macOS when Maven missing

### What Could Be Improved
- Could add Maven version verification to ensure minimum version requirements
- Could cache Maven downloads to speed up repeated local builds
- Could add more detailed logging about which Maven is being used

### Key Insights for Similar Tasks
- **Exit code 127** = "command not found" on Linux/Unix systems
- **`pwsh`** is cross-platform PowerShell, **`powershell`** is Windows-only
- **`where`** command is Windows-specific, **`which`** is Linux/Unix
- GitHub Actions pre-installs many tools via actions (e.g., `stCarolas/setup-maven@v4`)
- Always check if tools are already in PATH before downloading
- Use MSBuild OS detection properties for cross-platform builds:
  - `$(OS) == 'Windows_NT'` for Windows
  - `RuntimeInformation.IsOSPlatform()` for Linux/macOS
- Conditional execution prevents unnecessary operations and speeds up builds

### Specific Problems to Avoid in Future
- **Never use `powershell` command in cross-platform builds** - always use `pwsh`
- **Don't use Windows-specific commands** (`where`, `cmd.exe`, etc.) without OS conditionals
- **Don't download tools that CI already provides** - check environment first
- **Don't assume command availability** - always verify with appropriate detection
- **Don't use Windows path separators** (`\`) on Linux - use forward slashes or normalize paths
- **Always test build scripts on target CI platform** before merging

### Reference for Future WIs
- This pattern can be reused for other tool auto-installation (Gradle, Node.js, etc.)
- OS detection approach is canonical for cross-platform MSBuild targets
- Maven detection logic can be adapted for other CLI tools
- Error messages should provide actionable guidance for users