// Copyright 2025 FlinkDotNet
// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using FlinkDotNet.JobManager.Interfaces;
using Microsoft.Extensions.Options;

namespace FlinkDotNet.JobManager.Implementation;

/// <summary>
/// Background service that monitors TaskManager heartbeats and detects timeouts.
/// Automatically unregisters TaskManagers that fail to send heartbeats within the timeout period.
/// </summary>
public class HeartbeatMonitoringService : BackgroundService
{
    private readonly IResourceManager _resourceManager;
    private readonly ILogger<HeartbeatMonitoringService> _logger;
    private readonly HeartbeatConfiguration _configuration;

    public HeartbeatMonitoringService(
        IResourceManager resourceManager,
        IOptions<HeartbeatConfiguration> configuration,
        ILogger<HeartbeatMonitoringService> logger)
    {
        _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
        _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Heartbeat monitoring service started. Timeout: {Timeout}s, Check interval: {Interval}s",
            _configuration.TimeoutSeconds,
            _configuration.CheckIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckHeartbeatsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking heartbeats");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(_configuration.CheckIntervalSeconds),
                stoppingToken);
        }

        _logger.LogInformation("Heartbeat monitoring service stopped");
    }

    private async Task CheckHeartbeatsAsync(CancellationToken cancellationToken)
    {
        DateTime now = DateTime.UtcNow;
        TimeSpan timeout = TimeSpan.FromSeconds(_configuration.TimeoutSeconds);

        IEnumerable<string> taskManagers = _resourceManager.GetRegisteredTaskManagers();

        foreach (string taskManagerId in taskManagers)
        {
            DateTime? lastHeartbeat = _resourceManager.GetLastHeartbeat(taskManagerId);

            if (lastHeartbeat.HasValue)
            {
                TimeSpan timeSinceHeartbeat = now - lastHeartbeat.Value;

                if (timeSinceHeartbeat > timeout)
                {
                    _logger.LogWarning(
                        "TaskManager {TaskManagerId} heartbeat timeout. Last heartbeat: {LastHeartbeat}, " +
                        "Time since: {TimeSince}s, Timeout: {Timeout}s. Unregistering...",
                        taskManagerId,
                        lastHeartbeat.Value,
                        timeSinceHeartbeat.TotalSeconds,
                        timeout.TotalSeconds);

                    await _resourceManager.UnregisterTaskManagerAsync(taskManagerId, cancellationToken);

                    _logger.LogInformation(
                        "TaskManager {TaskManagerId} unregistered due to heartbeat timeout",
                        taskManagerId);
                }
            }
        }
    }
}

/// <summary>
/// Configuration options for heartbeat monitoring.
/// </summary>
public class HeartbeatConfiguration
{
    /// <summary>
    /// Section name in appsettings.json
    /// </summary>
    public const string SectionName = "Heartbeat";

    /// <summary>
    /// Heartbeat timeout in seconds. Default: 30 seconds.
    /// If a TaskManager doesn't send a heartbeat within this period, it will be unregistered.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Interval between heartbeat checks in seconds. Default: 10 seconds.
    /// The service will check for timeouts at this interval.
    /// </summary>
    public int CheckIntervalSeconds { get; set; } = 10;
}
