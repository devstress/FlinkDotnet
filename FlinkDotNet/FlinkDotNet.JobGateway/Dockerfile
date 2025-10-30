# Dockerfile for FlinkDotNet Gateway
# This containerizes the Gateway to run in the same Docker network as Flink/Kafka/Prometheus
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8086

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Install Java and Maven for FlinkIRRunner build
RUN apt-get update && \
    apt-get install --no-install-recommends -y openjdk-17-jdk maven && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Set JAVA_HOME environment variable
ENV JAVA_HOME=/usr/lib/jvm/java-17-openjdk-amd64
ENV PATH="${JAVA_HOME}/bin:${PATH}"

WORKDIR /src

# Copy project files
COPY ["FlinkDotNet/FlinkDotNet.JobGateway/FlinkDotNet.JobGateway.csproj", "FlinkDotNet/FlinkDotNet.JobGateway/"]
COPY ["FlinkDotNet/Flink.JobBuilder/Flink.JobBuilder.csproj", "FlinkDotNet/Flink.JobBuilder/"]

# Restore dependencies
RUN dotnet restore "FlinkDotNet/FlinkDotNet.JobGateway/FlinkDotNet.JobGateway.csproj"

# Copy source code (including FlinkIRRunner for Java build)
COPY FlinkDotNet/ FlinkDotNet/
COPY FlinkIRRunner/ FlinkIRRunner/

# Build
WORKDIR "/src/FlinkDotNet/FlinkDotNet.JobGateway"
RUN dotnet build "FlinkDotNet.JobGateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FlinkDotNet.JobGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final

# Install Java and Maven for on-demand JAR building capability
# This must be done before switching to non-root user
RUN apt-get update && \
    apt-get install --no-install-recommends -y openjdk-17-jdk maven && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Set JAVA_HOME environment variable
ENV JAVA_HOME=/usr/lib/jvm/java-17-openjdk-amd64
ENV PATH="${JAVA_HOME}/bin:${PATH}"

WORKDIR /app
COPY --from=publish /app/publish .

# Copy FlinkIRRunner source for on-demand building
COPY --from=build /src/FlinkIRRunner /app/FlinkIRRunner

# Run as non-root user for security
USER app

# Set environment for container deployment
ENV ASPNETCORE_URLS=http://+:8086
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "FlinkDotNet.JobGateway.dll"]