# Contributing to FlinkDotNet

Thank you for your interest in contributing to FlinkDotNet!

## How to Become a Contributor

We welcome contributions from everyone! Similar to Apache Flink, our contribution process follows these steps:

1. **Discuss**: Start by opening a GitHub issue to discuss your idea or bugfix. This helps reach consensus on the approach and ensures your contribution will be accepted.

2. **Implement**: Once there's agreement on the approach, implement your changes following the project's coding standards.

3. **Review**: Submit a pull request and collaborate with reviewers to address feedback.

4. **Merge**: After approval, a maintainer will merge your contribution.

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