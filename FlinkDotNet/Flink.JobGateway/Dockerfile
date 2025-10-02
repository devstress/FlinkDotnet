FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

# Install Java 17 and Maven
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    openjdk-17-jdk \
    maven \
    && rm -rf /var/lib/apt/lists/*

# Set JAVA_HOME
ENV JAVA_HOME=/usr/lib/jvm/java-17-openjdk-amd64

WORKDIR /src

# Copy FlinkIRRunner project first
COPY ["FlinkIRRunner/pom.xml", "FlinkIRRunner/"]
COPY ["FlinkIRRunner/src/", "FlinkIRRunner/src/"]

# Build FlinkIRRunner
WORKDIR "/src/FlinkIRRunner"
RUN mvn -q -DskipTests package

# Copy and restore .NET projects
WORKDIR /src
COPY ["FlinkDotNet/Flink.JobGateway/Flink.JobGateway.csproj", "Flink.JobGateway/"]
COPY ["FlinkDotNet/Flink.JobBuilder/Flink.JobBuilder.csproj", "Flink.JobBuilder/"]
RUN dotnet restore "Flink.JobGateway/Flink.JobGateway.csproj"

# Copy the rest of the .NET code
COPY FlinkDotNet/ .

# Build the Gateway
WORKDIR "/src/Flink.JobGateway"
RUN dotnet build "Flink.JobGateway.csproj" -c Release -o /app/build

FROM build AS publish
WORKDIR "/src/Flink.JobGateway"
RUN dotnet publish "Flink.JobGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Copy the FlinkIRRunner JAR to the publish directory
RUN mkdir -p /app/publish/FlinkIRRunner && \
    cp /src/FlinkIRRunner/target/flink-ir-runner.jar /app/publish/

FROM base AS final
WORKDIR /app

# Install Java runtime for the final image
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    openjdk-17-jre-headless \
    && rm -rf /var/lib/apt/lists/*

# Set JAVA_HOME in the final image
ENV JAVA_HOME=/usr/lib/jvm/java-17-openjdk-amd64

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Flink.JobGateway.dll"]