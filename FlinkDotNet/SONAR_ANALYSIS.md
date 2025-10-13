# Local SonarQube Analysis Guide

This guide explains how to run SonarQube analysis locally without needing to visit SonarCloud.

## Overview

The FlinkDotNet project includes local SonarQube analysis capabilities, allowing you to:
- Run quality analysis on your local machine
- View code quality issues without internet access
- Validate changes before pushing to CI/CD
- Optionally upload results to SonarCloud

## Prerequisites

- .NET 9.0 SDK or later
- PowerShell 7+ (for Windows) or Bash (for Linux/macOS)
- `dotnet-sonarscanner` tool (auto-installed by scripts)

## Quick Start

### Windows (PowerShell)

```powershell
# Navigate to FlinkDotNet directory
cd FlinkDotNet

# Run local analysis (no upload to SonarCloud)
./run-sonar-analysis.ps1

# Run analysis and upload to SonarCloud
./run-sonar-analysis.ps1 -SonarToken "your-sonarcloud-token"

# Skip tests (faster analysis)
./run-sonar-analysis.ps1 -SkipTests
```

### Linux/macOS (Bash)

```bash
# Navigate to FlinkDotNet directory
cd FlinkDotNet

# Run local analysis (no upload to SonarCloud)
./run-sonar-analysis.sh

# Run analysis and upload to SonarCloud
./run-sonar-analysis.sh your-sonarcloud-token

# Skip tests (faster analysis)
./run-sonar-analysis.sh --skip-tests
```

## What the Scripts Do

1. **Install/Update dotnet-sonarscanner**: Ensures the SonarQube scanner is available
2. **Clean Build**: Removes previous build artifacts
3. **Begin Analysis**: Starts SonarQube analysis session
4. **Build Solution**: Compiles the FlinkDotNet solution
5. **Run Tests**: Executes unit tests with code coverage (unless skipped)
6. **Complete Analysis**: Finalizes analysis and generates reports
7. **Save Results**: Stores analysis results locally in `.sonarqube/` directory

## Viewing Results

### Local Results

After running the analysis, results are saved to:
- `.sonarqube/` directory in the FlinkDotNet folder
- Check this directory for issue reports and metrics

### SonarCloud Results

If you provided a SonarCloud token:
- Results are uploaded to: https://sonarcloud.io/dashboard?id=devstress_flinkdotnet
- You can view detailed analysis, trends, and historical data

## Configuration

### sonar-project.properties

The `sonar-project.properties` file contains project configuration:
- Project key and organization
- Source code locations
- Exclusions (bin, obj, test results)
- Coverage report paths

You can customize this file to adjust analysis behavior.

## Common Issues

### Scanner Not Found

If `dotnet-sonarscanner` is not found after installation:
```bash
# Add to PATH (Linux/macOS)
export PATH="$PATH:$HOME/.dotnet/tools"

# Add to PATH (Windows PowerShell)
$env:Path += ";$HOME\.dotnet\tools"
```

### Coverage Reports Not Found

Ensure tests run successfully:
```bash
dotnet test FlinkDotNet.sln --collect:"XPlat Code Coverage"
```

### Authentication Errors

If uploading to SonarCloud fails:
- Verify your SonarCloud token is valid
- Check token has appropriate permissions
- Ensure you have access to the `devstress` organization

## Integration with CI/CD

The analysis scripts complement the CI/CD pipeline:
- **Local**: Use these scripts for rapid feedback during development
- **CI/CD**: The `.github/workflows/unit-tests.yml` workflow runs automatically on push/PR

## SonarQube Rules

Current focus areas:
- Code coverage: Target 80%+ line coverage
- Code smells: All suppressions removed
- Security: No vulnerabilities
- Maintainability: Follow .NET best practices

## Additional Resources

- [SonarCloud Dashboard](https://sonarcloud.io/dashboard?id=devstress_flinkdotnet)
- [SonarQube Documentation](https://docs.sonarqube.org/)
- [.NET Scanner Documentation](https://docs.sonarqube.org/latest/analysis/scan/sonarscanner-for-msbuild/)

## Support

For issues or questions:
1. Check the `.sonarqube/` directory for detailed logs
2. Review the SonarCloud dashboard for detailed analysis
3. Refer to the project's main README for general setup
