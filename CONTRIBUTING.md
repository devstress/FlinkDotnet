# Contributing to FlinkDotNet

Thank you for your interest in contributing to FlinkDotNet!

## How to Become a Contributor

We welcome contributions from everyone! Our contribution process follows these steps:

1. **Discuss**: Start by opening a GitHub issue to discuss your idea or bugfix. This helps reach consensus on the approach and ensures your contribution will be accepted.

2. **Implement**: Once there's agreement on the approach, implement your changes following the project's coding standards.

3. **Review**: Submit a pull request and collaborate with reviewers to address feedback.

4. **Merge**: After approval, a maintainer will merge your contribution.

## How to Become a Maintainer

Maintainers are trusted contributors with commit access to the repository. To become a maintainer:

1. **Contribute Regularly**: Make consistent, high-quality contributions to the project through code, documentation, bug fixes, and community support.

2. **Engage with the Community**: Participate actively in GitHub discussions, help review pull requests, and support other contributors.

3. **Demonstrate Expertise**: Show deep understanding of the project architecture, coding standards, and best practices.

4. **Nomination**: Existing maintainers will nominate active contributors based on their sustained contributions and community involvement.

Maintainers are expected to uphold project standards, review contributions, and help guide the project's direction.

## Code Formatting and Quality

FlinkDotNet uses `dotnet format` to maintain consistent code style across the project. Before submitting a pull request:

### Running Code Formatting

Format your code using the .NET formatter:

```bash
# Format the FlinkDotNet solution
cd FlinkDotNet
dotnet format FlinkDotNet.sln

# Verify formatting without making changes
dotnet format FlinkDotNet.sln --verify-no-changes
```

### Code Style Rules

The project follows these code style guidelines:

- **EditorConfig**: All code style rules are defined in `.editorconfig` at the repository root
- **Analyzers**: SonarAnalyzer.CSharp and Roslynator analyzers are enabled for code quality
- **SOLID Principles**: Code should follow SOLID principles as outlined in `.github/copilot-instructions.md`

### Common Code Quality Checks

Before submitting your PR, ensure:

- ✅ Code builds without warnings: `dotnet build --configuration Release`
- ✅ Code is properly formatted: `dotnet format FlinkDotNet.sln --verify-no-changes`
- ✅ No SonarCloud or Roslyn analyzer warnings
- ✅ All tests pass (see below)

## Make Sure Tests Work

Before submitting your pull request, ensure all tests pass:

```bash
# Run unit tests
dotnet test FlinkDotnet/FlinkDotnet.sln

# Run integration tests
dotnet test LocalTesting/LocalTesting.sln
```

All tests must pass locally before submitting your PR. If tests fail, fix them before proceeding.

## Submit Your PR and Wait for Review

Once your changes are ready and all tests pass:

1. **Push your changes** to your fork
2. **Create a pull request** against the main branch
3. **Fill out the PR template** with details about your changes
4. **Wait for review** - maintainers will review your PR and provide feedback
5. **Address feedback** if requested
6. **Merge** - once approved, a maintainer will merge your contribution

---

Thank you for contributing to FlinkDotNet!