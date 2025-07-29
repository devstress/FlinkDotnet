FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["FlinkDotNet/Flink.JobGateway/Flink.JobGateway.csproj", "Flink.JobGateway/"]
COPY ["FlinkDotNet/Flink.JobBuilder/Flink.JobBuilder.csproj", "Flink.JobBuilder/"]
RUN dotnet restore "Flink.JobGateway/Flink.JobGateway.csproj"
COPY FlinkDotNet/ .
WORKDIR "/src/Flink.JobGateway"
RUN dotnet build "Flink.JobGateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Flink.JobGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Flink.JobGateway.dll"]