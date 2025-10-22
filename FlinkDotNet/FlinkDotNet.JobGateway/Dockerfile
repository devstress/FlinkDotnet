# Dockerfile for FlinkDotNet Gateway
# This containerizes the Gateway to run in the same Docker network as Flink/Kafka/Prometheus
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["FlinkDotNet/FlinkDotNet.JobGateway/FlinkDotNet.JobGateway.csproj", "FlinkDotNet/FlinkDotNet.JobGateway/"]
COPY ["FlinkDotNet/Flink.JobBuilder/Flink.JobBuilder.csproj", "FlinkDotNet/Flink.JobBuilder/"]

# Restore dependencies
RUN dotnet restore "FlinkDotNet/FlinkDotNet.JobGateway/FlinkDotNet.JobGateway.csproj"

# Copy source code
COPY FlinkDotNet/ FlinkDotNet/

# Build
WORKDIR "/src/FlinkDotNet/FlinkDotNet.JobGateway"
RUN dotnet build "FlinkDotNet.JobGateway.csproj" -c Release -o /app/build -p:BuildFlinkRunner=false

FROM build AS publish
RUN dotnet publish "FlinkDotNet.JobGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false /p:BuildFlinkRunner=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Run as non-root user for security
USER app

# Set environment for container deployment
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "FlinkDotNet.JobGateway.dll"]