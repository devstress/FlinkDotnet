# Release Notes

<!-- 
  This file serves as a template for release notes when creating a new version.
  Before running a release workflow, update this file with the changes for the new version.
  The release workflow will use this content for the GitHub release.
-->

## Version: [VERSION_NUMBER]

<!-- Example: 1.0.0 -->

### Release Type
- [ ] Major Release (Breaking Changes)
- [ ] Minor Release (New Features)
- [ ] Patch Release (Bug Fixes)

---

## 🎯 Overview

<!-- Provide a brief overview of this release -->

---

## ✨ What's New

<!-- List new features added in this release -->

### New Features
- Feature 1: Description
- Feature 2: Description

### Improvements
- Improvement 1: Description
- Improvement 2: Description

---

## 🐛 Bug Fixes

<!-- List bugs fixed in this release -->

- Bug fix 1: Description
- Bug fix 2: Description

---

## 💥 Breaking Changes

<!-- List any breaking changes that require user action -->

- Breaking change 1: Description and migration path
- Breaking change 2: Description and migration path

---

## 📦 NuGet Packages

This release includes the following NuGet packages:

- **FlinkDotNet.Common** - Core common components
- **FlinkDotNet.DataStream** - DataStream API
- **Flink.JobBuilder** - Job builder with JSON IR generation

### Installation

```bash
dotnet add package FlinkDotNet.Common --version [VERSION_NUMBER]
dotnet add package FlinkDotNet.DataStream --version [VERSION_NUMBER]
dotnet add package Flink.JobBuilder --version [VERSION_NUMBER]
```

---

## 🐳 Docker Image

Docker image for FlinkDotNet JobGateway:

```bash
docker pull flinkdotnet/jobgateway:[VERSION_NUMBER]
docker run -p 8080:8080 flinkdotnet/jobgateway:[VERSION_NUMBER]
```

Or download from release assets:
```bash
docker load < jobgateway-[VERSION_NUMBER].tar.gz
```

---

## 📚 Documentation

<!-- Link to relevant documentation updates -->

- [Getting Started Guide](docs/wiki/Getting-Started.md)
- [API Reference](docs/api-reference.md)
- [Architecture & Use Cases](docs/architecture-and-usecases.md)

---

## 🔄 Migration Guide

<!-- If applicable, provide migration instructions from previous version -->

### Migrating from [PREVIOUS_VERSION] to [VERSION_NUMBER]

1. Step 1: Description
2. Step 2: Description
3. Step 3: Description

---

## ⚠️ Known Issues

<!-- List any known issues in this release -->

- Known issue 1: Description and workaround
- Known issue 2: Description and workaround

---

## 🙏 Contributors

<!-- Acknowledge contributors to this release -->

Thank you to all contributors who made this release possible!

---

## 📅 Release Date

<!-- Release date will be set automatically by the workflow -->

---

## 🔗 Additional Resources

- [Full Changelog](CHANGELOG.md)
- [Contributing Guidelines](CONTRIBUTING.md)
- [Release Workflows Documentation](docs/release-workflows.md)
