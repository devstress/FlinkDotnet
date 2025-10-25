# FlinkDotNet Wiki

Welcome to the FlinkDotNet user documentation wiki. This directory contains comprehensive user instructions for the three main components of FlinkDotNet.

## User Instructions

### [FlinkDotNet Client User Instructions](FlinkDotNet-Client-User-Instructions.md)

Complete guide for using the FlinkDotNet NuGet package to build and submit Apache Flink streaming jobs from .NET applications.

**Topics covered:**
- Installation via NuGet
- Quick start examples
- DataStream API usage
- Transformations, windowing, and sinks
- Advanced patterns (state management, joins)
- Configuration options
- Testing strategies
- Performance optimization
- Best practices

**Best for**: Developers building Flink jobs in C# applications

---

### [FlinkDotNet.JobGateway User Instructions](FlinkDotNet-JobGateway-User-Instructions.md)

Complete guide for deploying and using the FlinkDotNet.JobGateway service for managing Flink jobs via REST API.

**Topics covered:**
- Standalone deployment (Windows/Linux)
- Building from source
- Configuration (environment variables, appsettings.json)
- Flink cluster connectivity
- REST API reference
- Monitoring and observability
- Production deployment (systemd, Windows Service)
- Security considerations
- Best practices

**Best for**: DevOps engineers and administrators deploying the gateway service

---

### [FlinkDotNet Docker Image User Instructions](FlinkDotNet-Docker-Image-User-Instructions.md)

Complete guide for deploying FlinkDotNet.JobGateway using Docker containers.

**Topics covered:**
- Docker quick start
- Docker Compose deployment
- Kubernetes deployment
- Docker Swarm deployment
- Advanced configuration
- Networking (bridge, host, overlay)
- Health checks and monitoring
- Security best practices
- Image variants and tags
- Building custom images

**Best for**: Container-based deployments and orchestration platforms

---

## Quick Navigation

| Component | Installation | Configuration | Deployment | API Reference |
|-----------|--------------|---------------|------------|---------------|
| **Client** | [NuGet Package](FlinkDotNet-Client-User-Instructions.md#installation) | [Settings](FlinkDotNet-Client-User-Instructions.md#configuration-options) | [N/A](FlinkDotNet-Client-User-Instructions.md) | [DataStream API](FlinkDotNet-Client-User-Instructions.md#api-usage) |
| **Gateway** | [Standalone](FlinkDotNet-JobGateway-User-Instructions.md#deployment-options) | [Environment Vars](FlinkDotNet-JobGateway-User-Instructions.md#environment-variables) | [Production](FlinkDotNet-JobGateway-User-Instructions.md#production-deployment) | [REST API](FlinkDotNet-JobGateway-User-Instructions.md#api-reference) |
| **Docker** | [Pull Image](FlinkDotNet-Docker-Image-User-Instructions.md#quick-start) | [Environment Vars](FlinkDotNet-Docker-Image-User-Instructions.md#environment-variables) | [Kubernetes](FlinkDotNet-Docker-Image-User-Instructions.md#scenario-3-kubernetes-deployment) | [Same as Gateway](FlinkDotNet-JobGateway-User-Instructions.md#api-reference) |

## Additional Resources

- **[Main README](../../README.md)** - Project overview and introduction
- **[Getting Started](../getting-started.md)** - Quick start guide for all components
- **[Architecture & Use Cases](../architecture-and-usecases.md)** - System design and patterns
- **[API Reference](../api-reference.md)** - Complete DataStream API documentation
- **[Features](../features.md)** - Complete feature list and capabilities
- **[Troubleshooting](../troubleshooting.md)** - Common issues and solutions
- **[Learning Course](../../LearningCourse/README.md)** - 15-day hands-on training

## Support

- **GitHub Issues**: https://github.com/devstress/FlinkDotnet/issues
- **Discussions**: https://github.com/devstress/FlinkDotnet/discussions
- **Documentation**: https://github.com/devstress/FlinkDotnet

## Contributing

See [CONTRIBUTING.md](../../CONTRIBUTING.md) for guidelines on contributing to FlinkDotNet documentation.
