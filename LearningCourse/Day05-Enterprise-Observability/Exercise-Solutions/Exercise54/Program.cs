using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Context;

/// <summary>
/// Enterprise-grade alert configuration implementation following Google SRE alerting principles.
/// Demonstrates SLI/SLO monitoring, error budget tracking, and production alerting strategies.
/// 
/// References:
/// - Google SRE Book: Alerting on SLOs
/// - Netflix Technology Blog: Alert Fatigue Prevention
/// - PagerDuty Best Practices for Enterprise Alerting
/// - Datadog SLI/SLO Implementation Patterns
/// </summary>
public class GoogleSREAlertingService
{
    private readonly ILogger<GoogleSREAlertingService> _logger;
    private readonly Random _deterministicRandom;

    // === GOOGLE SRE ALERTING PRINCIPLES ===
    // Based on Google SRE practices for production alerting systems
    
    // Service Level Objectives (SLOs) - Google production targets
    private readonly Dictionary<string, ServiceLevelObjective> _serviceObjectives = new()
    {
        { "api-gateway", new ServiceLevelObjective 
            { 
                AvailabilityTarget = 99.9, 
                LatencyP99Target = 100, 
                ErrorRateTarget = 0.1, 
                ThroughputTarget = 10000,
                ServiceName = "API Gateway"
            } 
        },
        { "user-service", new ServiceLevelObjective 
            { 
                AvailabilityTarget = 99.95, 
                LatencyP99Target = 50, 
                ErrorRateTarget = 0.05, 
                ThroughputTarget = 5000,
                ServiceName = "User Service"
            } 
        },
        { "payment-service", new ServiceLevelObjective 
            { 
                AvailabilityTarget = 99.99, 
                LatencyP99Target = 200, 
                ErrorRateTarget = 0.01, 
                ThroughputTarget = 1000,
                ServiceName = "Payment Service"
            } 
        },
        { "recommendation-engine", new ServiceLevelObjective 
            { 
                AvailabilityTarget = 99.5, 
                LatencyP99Target = 500, 
                ErrorRateTarget = 0.5, 
                ThroughputTarget = 15000,
                ServiceName = "Recommendation Engine"
            } 
        },
        { "content-delivery", new ServiceLevelObjective 
            { 
                AvailabilityTarget = 99.95, 
                LatencyP99Target = 75, 
                ErrorRateTarget = 0.05, 
                ThroughputTarget = 50000,
                ServiceName = "Content Delivery"
            } 
        }
    };

    // Alert severity levels following industry standards
    public enum AlertSeverity
    {
        Critical,   // Immediate action required, service down
        High,       // Action required within 1 hour
        Medium,     // Action required within 4 hours  
        Low,        // Action required within 24 hours
        Info        // Informational only
    }

    // Alert escalation policies
    private readonly Dictionary<AlertSeverity, AlertEscalationPolicy> _escalationPolicies = new()
    {
        { AlertSeverity.Critical, new AlertEscalationPolicy 
            { 
                InitialNotificationMinutes = 0,
                EscalationIntervalMinutes = 5,
                MaxEscalationLevels = 3,
                NotificationChannels = new[] { "pagerduty", "sms", "phone", "slack" }
            } 
        },
        { AlertSeverity.High, new AlertEscalationPolicy 
            { 
                InitialNotificationMinutes = 0,
                EscalationIntervalMinutes = 15,
                MaxEscalationLevels = 2,
                NotificationChannels = new[] { "pagerduty", "slack", "email" }
            } 
        },
        { AlertSeverity.Medium, new AlertEscalationPolicy 
            { 
                InitialNotificationMinutes = 0,
                EscalationIntervalMinutes = 60,
                MaxEscalationLevels = 1,
                NotificationChannels = new[] { "slack", "email" }
            } 
        },
        { AlertSeverity.Low, new AlertEscalationPolicy 
            { 
                InitialNotificationMinutes = 0,
                EscalationIntervalMinutes = 240,
                MaxEscalationLevels = 1,
                NotificationChannels = new[] { "email" }
            } 
        },
        { AlertSeverity.Info, new AlertEscalationPolicy 
            { 
                InitialNotificationMinutes = 0,
                EscalationIntervalMinutes = 0,
                MaxEscalationLevels = 0,
                NotificationChannels = new[] { "email" }
            } 
        }
    };

    public GoogleSREAlertingService(ILogger<GoogleSREAlertingService> logger)
    {
        _logger = logger;
        // Deterministic random for consistent educational outcomes
        _deterministicRandom = new Random(DateTime.Now.Hour * 60 + DateTime.Now.Minute);
    }

    public async Task RunAlertConfigurationDemo()
    {
        _logger.LogInformation("Starting Google SRE alert configuration demonstration");
        
        using (LogContext.PushProperty("DemoType", "alert_configuration"))
        using (LogContext.PushProperty("CompanyPattern", "google_sre"))
        using (LogContext.PushProperty("AlertingPhilosophy", "sli_slo_based"))
        {
            // Simulate various SRE alerting scenarios
            await SimulateSLIMonitoring();
            await SimulateErrorBudgetTracking();
            await SimulateAlertEscalation();
            await SimulateAlertFatiguePrevention();
        }
        
        _logger.LogInformation("Google SRE alert configuration demonstration completed");
    }

    /// <summary>
    /// Simulate Service Level Indicator (SLI) monitoring with real-time threshold checking
    /// Google SRE philosophy: Monitor what users experience, not internal system metrics
    /// </summary>
    private async Task SimulateSLIMonitoring()
    {
        _logger.LogInformation("Simulating SLI monitoring with user-facing metrics...");
        
        var monitoringRun = Guid.NewGuid().ToString("N")[..12];
        
        using (LogContext.PushProperty("MonitoringRun", monitoringRun))
        using (LogContext.PushProperty("SLIMonitoringType", "user_facing_metrics"))
        {
            foreach (var slo in _serviceObjectives)
            {
                var serviceName = slo.Key;
                var objective = slo.Value;
                
                await MonitorServiceSLIs(serviceName, objective);
                await Task.Delay(_deterministicRandom.Next(100, 300)); // Realistic monitoring interval
            }
        }
    }

    /// <summary>
    /// Monitor individual service SLIs against defined SLOs
    /// </summary>
    private async Task MonitorServiceSLIs(string serviceName, ServiceLevelObjective slo)
    {
        using (LogContext.PushProperty("ServiceName", serviceName))
        using (LogContext.PushProperty("SLOAvailability", slo.AvailabilityTarget))
        using (LogContext.PushProperty("SLOLatencyP99", slo.LatencyP99Target))
        {
            // Simulate current SLI measurements
            var currentAvailability = SimulateAvailabilityMeasurement(slo.AvailabilityTarget);
            var currentLatencyP99 = SimulateLatencyMeasurement(slo.LatencyP99Target);
            var currentErrorRate = SimulateErrorRateMeasurement(slo.ErrorRateTarget);
            var currentThroughput = SimulateThroughputMeasurement(slo.ThroughputTarget);
            
            _logger.LogInformation("SLI measurements collected",
                new { 
                    service = serviceName,
                    availability_percent = Math.Round(currentAvailability, 3),
                    latency_p99_ms = Math.Round(currentLatencyP99, 1),
                    error_rate_percent = Math.Round(currentErrorRate, 4),
                    throughput_rps = currentThroughput,
                    measurement_window_minutes = 5
                });

            // Check SLO compliance and generate alerts if needed
            await CheckSLOCompliance(serviceName, slo, currentAvailability, currentLatencyP99, currentErrorRate, currentThroughput);
        }
    }

    /// <summary>
    /// Check SLO compliance and trigger alerts for SLI violations
    /// </summary>
    private async Task CheckSLOCompliance(string serviceName, ServiceLevelObjective slo, double availability, double latency, double errorRate, double throughput)
    {
        var violations = new List<SLOViolation>();
        
        // Log throughput for monitoring but don't alert on it in this demo
        using (LogContext.PushProperty("ThroughputRps", throughput))
        {
        if (availability < slo.AvailabilityTarget)
        {
            violations.Add(new SLOViolation 
            { 
                MetricType = "availability",
                CurrentValue = availability,
                TargetValue = slo.AvailabilityTarget,
                Severity = GetAvailabilitySeverity(availability, slo.AvailabilityTarget)
            });
        }
        
        // Check latency SLO
        if (latency > slo.LatencyP99Target)
        {
            violations.Add(new SLOViolation 
            { 
                MetricType = "latency_p99",
                CurrentValue = latency,
                TargetValue = slo.LatencyP99Target,
                Severity = GetLatencySeverity(latency, slo.LatencyP99Target)
            });
        }
        
        // Check error rate SLO
        if (errorRate > slo.ErrorRateTarget)
        {
            violations.Add(new SLOViolation 
            { 
                MetricType = "error_rate",
                CurrentValue = errorRate,
                TargetValue = slo.ErrorRateTarget,
                Severity = GetErrorRateSeverity(errorRate, slo.ErrorRateTarget)
            });
        }

        // Process violations and trigger alerts
        foreach (var violation in violations)
        {
            await TriggerSLOViolationAlert(serviceName, slo, violation);
        }

        if (violations.Count == 0)
        {
            _logger.LogDebug("SLO compliance check passed",
                new { 
                    service = serviceName,
                    all_slos_met = true,
                    availability_margin = Math.Round(availability - slo.AvailabilityTarget, 3),
                    latency_margin = Math.Round(slo.LatencyP99Target - latency, 1),
                    error_rate_margin = Math.Round(slo.ErrorRateTarget - errorRate, 4)
                });
        }
        } // End using statement
    }

    /// <summary>
    /// Trigger SLO violation alert with appropriate severity and escalation
    /// </summary>
    private async Task TriggerSLOViolationAlert(string serviceName, ServiceLevelObjective slo, SLOViolation violation)
    {
        var alertId = Guid.NewGuid().ToString("N")[..8];
        var escalationPolicy = _escalationPolicies[violation.Severity];
        
        using (LogContext.PushProperty("AlertId", alertId))
        using (LogContext.PushProperty("ServiceName", serviceName))
        using (LogContext.PushProperty("ViolationType", violation.MetricType))
        {
            _logger.LogWarning("SLO violation detected - Alert triggered",
                new { 
                    alert_id = alertId,
                    service = serviceName,
                    service_display_name = slo.ServiceName,
                    violation_type = violation.MetricType,
                    current_value = violation.CurrentValue,
                    target_value = violation.TargetValue,
                    severity = violation.Severity.ToString().ToLower(),
                    deviation_percent = CalculateDeviationPercent(violation.CurrentValue, violation.TargetValue, violation.MetricType),
                    alert_triggered_at = DateTimeOffset.UtcNow,
                    notification_channels = escalationPolicy.NotificationChannels,
                    escalation_interval_minutes = escalationPolicy.EscalationIntervalMinutes
                });

            // Simulate alert notification processing
            await ProcessAlertNotifications(alertId, serviceName, violation, escalationPolicy);
        }
    }

    /// <summary>
    /// Process alert notifications through configured channels
    /// </summary>
    private async Task ProcessAlertNotifications(string alertId, string serviceName, SLOViolation violation, AlertEscalationPolicy policy)
    {
        foreach (var channel in policy.NotificationChannels)
        {
            await SendNotification(alertId, serviceName, violation, channel);
            await Task.Delay(_deterministicRandom.Next(10, 50)); // Simulate notification latency
        }
    }

    /// <summary>
    /// Send alert notification to specific channel
    /// </summary>
    private async Task SendNotification(string alertId, string serviceName, SLOViolation violation, string channel)
    {
        var notificationId = Guid.NewGuid().ToString("N")[..8];
        var deliverySuccess = _deterministicRandom.NextDouble() > 0.05; // 95% delivery success rate
        
        using (LogContext.PushProperty("NotificationId", notificationId))
        using (LogContext.PushProperty("NotificationChannel", channel))
        {
            if (deliverySuccess)
            {
                _logger.LogInformation("Alert notification sent successfully",
                    new { 
                        notification_id = notificationId,
                        alert_id = alertId,
                        channel = channel,
                        service = serviceName,
                        violation_type = violation.MetricType,
                        severity = violation.Severity.ToString().ToLower(),
                        delivery_time_ms = _deterministicRandom.Next(100, 2000),
                        recipient_count = GetChannelRecipientCount(channel),
                        notification_format = GetNotificationFormat(channel)
                    });
            }
            else
            {
                _logger.LogError("Alert notification delivery failed",
                    new { 
                        notification_id = notificationId,
                        alert_id = alertId,
                        channel = channel,
                        failure_reason = GetNotificationFailureReason(),
                        retry_scheduled = true,
                        retry_delay_minutes = 2
                    });
            }
        }
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Simulate error budget tracking following Google SRE methodology
    /// Error Budget = 1 - SLO (e.g., 99.9% SLO = 0.1% error budget)
    /// </summary>
    private async Task SimulateErrorBudgetTracking()
    {
        _logger.LogInformation("Simulating error budget tracking with SLO burn rate analysis...");
        
        var budgetTrackingRun = Guid.NewGuid().ToString("N")[..12];
        
        using (LogContext.PushProperty("BudgetTrackingRun", budgetTrackingRun))
        using (LogContext.PushProperty("ErrorBudgetPeriod", "30_days"))
        {
            foreach (var slo in _serviceObjectives)
            {
                await TrackServiceErrorBudget(slo.Key, slo.Value);
                await Task.Delay(_deterministicRandom.Next(50, 150));
            }
        }
    }

    /// <summary>
    /// Track error budget consumption for individual service
    /// </summary>
    private async Task TrackServiceErrorBudget(string serviceName, ServiceLevelObjective slo)
    {
        // Calculate error budget (1 - SLO)
        var errorBudgetPercent = 100 - slo.AvailabilityTarget;
        var errorBudgetMinutesPerMonth = errorBudgetPercent * 0.01 * 30 * 24 * 60; // Monthly budget in minutes
        
        // Simulate current error budget consumption
        var consumedBudgetMinutes = _deterministicRandom.NextDouble() * errorBudgetMinutesPerMonth;
        var budgetConsumptionPercent = (consumedBudgetMinutes / errorBudgetMinutesPerMonth) * 100;
        var burnRate = CalculateBurnRate(budgetConsumptionPercent, 30); // Days into month
        
        using (LogContext.PushProperty("ServiceName", serviceName))
        using (LogContext.PushProperty("ErrorBudgetPercent", errorBudgetPercent))
        {
            _logger.LogInformation("Error budget tracking update",
                new { 
                    service = serviceName,
                    slo_availability_target = slo.AvailabilityTarget,
                    error_budget_percent = Math.Round(errorBudgetPercent, 3),
                    error_budget_minutes_per_month = Math.Round(errorBudgetMinutesPerMonth, 1),
                    consumed_budget_minutes = Math.Round(consumedBudgetMinutes, 1),
                    budget_consumption_percent = Math.Round(budgetConsumptionPercent, 2),
                    current_burn_rate = Math.Round(burnRate, 3),
                    days_remaining_at_current_rate = Math.Round(CalculateDaysRemaining(budgetConsumptionPercent, burnRate), 1),
                    budget_status = GetBudgetStatus(budgetConsumptionPercent)
                });

            // Check for error budget alerts
            await CheckErrorBudgetAlerts(serviceName, slo, budgetConsumptionPercent, burnRate);
        }
    }

    /// <summary>
    /// Check error budget consumption and trigger burn rate alerts
    /// </summary>
    private async Task CheckErrorBudgetAlerts(string serviceName, ServiceLevelObjective slo, double budgetConsumptionPercent, double burnRate)
    {
        // Alert thresholds based on Google SRE best practices
        if (budgetConsumptionPercent > 75)
        {
            await TriggerErrorBudgetAlert(serviceName, slo, "high_consumption", AlertSeverity.High, 
                $"Error budget {budgetConsumptionPercent:F1}% consumed - approaching exhaustion");
        }
        else if (burnRate > 5.0) // Burning budget 5x faster than expected
        {
            await TriggerErrorBudgetAlert(serviceName, slo, "high_burn_rate", AlertSeverity.Medium,
                $"Error budget burn rate {burnRate:F2}x - unsustainable consumption");
        }
        else if (budgetConsumptionPercent > 50)
        {
            await TriggerErrorBudgetAlert(serviceName, slo, "moderate_consumption", AlertSeverity.Low,
                $"Error budget {budgetConsumptionPercent:F1}% consumed - monitor closely");
        }
    }

    /// <summary>
    /// Trigger error budget alert
    /// </summary>
    private async Task TriggerErrorBudgetAlert(string serviceName, ServiceLevelObjective slo, string alertType, AlertSeverity severity, string description)
    {
        var alertId = Guid.NewGuid().ToString("N")[..8];
        
        _logger.LogWarning("Error budget alert triggered",
            new { 
                alert_id = alertId,
                service = serviceName,
                alert_type = alertType,
                severity = severity.ToString().ToLower(),
                description = description,
                slo_target = slo.AvailabilityTarget,
                recommended_actions = GetErrorBudgetRecommendations(alertType),
                alert_triggered_at = DateTimeOffset.UtcNow
            });
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Simulate alert escalation scenarios with realistic timing
    /// </summary>
    private async Task SimulateAlertEscalation()
    {
        _logger.LogInformation("Simulating alert escalation scenarios...");
        
        var escalationRun = Guid.NewGuid().ToString("N")[..12];
        
        using (LogContext.PushProperty("EscalationRun", escalationRun))
        {
            // Simulate critical alert that requires escalation
            await SimulateCriticalAlertEscalation();
            await Task.Delay(1000);
            
            // Simulate high severity alert escalation
            await SimulateHighSeverityAlertEscalation();
            await Task.Delay(1000);
        }
    }

    /// <summary>
    /// Simulate critical alert escalation (service down scenario)
    /// </summary>
    private async Task SimulateCriticalAlertEscalation()
    {
        var alertId = Guid.NewGuid().ToString("N")[..8];
        var serviceName = "payment-service";
        
        using (LogContext.PushProperty("AlertId", alertId))
        using (LogContext.PushProperty("EscalationScenario", "critical_service_down"))
        {
            _logger.LogError("CRITICAL: Service completely down - Initiating escalation",
                new { 
                    alert_id = alertId,
                    service = serviceName,
                    severity = "critical",
                    availability = 0.0,
                    impact = "complete_service_outage",
                    customer_impact = "all_payments_failing",
                    business_impact = "revenue_loss",
                    escalation_level = 1,
                    notification_channels = new[] { "pagerduty", "sms", "phone", "slack" }
                });

            await Task.Delay(300); // 5 minutes in simulation time

            // Simulate no acknowledgment - escalate to level 2
            _logger.LogError("CRITICAL: Alert not acknowledged - Escalating to Level 2",
                new { 
                    alert_id = alertId,
                    escalation_level = 2,
                    escalation_reason = "no_acknowledgment_after_5_minutes",
                    additional_recipients = new[] { "engineering_manager", "director_of_engineering" },
                    escalation_channels = new[] { "phone", "sms", "executive_slack" }
                });

            await Task.Delay(300); // Another 5 minutes

            // Simulate acknowledgment and resolution
            _logger.LogInformation("CRITICAL: Alert acknowledged - Resolution in progress",
                new { 
                    alert_id = alertId,
                    acknowledged_by = "senior_engineer_alice",
                    acknowledgment_time_minutes = 10,
                    estimated_resolution_time_minutes = 30,
                    mitigation_actions = new[] { "restart_service_cluster", "database_failover", "traffic_rerouting" }
                });
        }
    }

    /// <summary>
    /// Simulate high severity alert escalation
    /// </summary>
    private async Task SimulateHighSeverityAlertEscalation()
    {
        var alertId = Guid.NewGuid().ToString("N")[..8];
        var serviceName = "recommendation-engine";
        
        using (LogContext.PushProperty("AlertId", alertId))
        using (LogContext.PushProperty("EscalationScenario", "high_latency_spike"))
        {
            _logger.LogWarning("HIGH: Latency spike detected - Monitoring for escalation",
                new { 
                    alert_id = alertId,
                    service = serviceName,
                    severity = "high",
                    latency_p99_ms = 1250,
                    latency_target_ms = 500,
                    deviation_percent = 150,
                    customer_impact = "degraded_recommendation_performance",
                    business_impact = "reduced_user_engagement",
                    escalation_level = 1
                });

            await Task.Delay(200); // 15 minutes in simulation time

            // Simulate quick acknowledgment and resolution
            _logger.LogInformation("HIGH: Alert acknowledged and resolved",
                new { 
                    alert_id = alertId,
                    acknowledged_by = "ml_engineer_bob",
                    acknowledgment_time_minutes = 8,
                    resolution_time_minutes = 15,
                    root_cause = "model_inference_timeout",
                    resolution_actions = new[] { "model_cache_refresh", "inference_timeout_increase", "load_balancer_adjustment" }
                });
        }
    }

    /// <summary>
    /// Simulate alert fatigue prevention strategies
    /// Google SRE principle: Alerts should be actionable, important, and rare
    /// </summary>
    private async Task SimulateAlertFatiguePrevention()
    {
        _logger.LogInformation("Simulating alert fatigue prevention strategies...");
        
        var fatiguePreventionRun = Guid.NewGuid().ToString("N")[..12];
        
        using (LogContext.PushProperty("FatiguePreventionRun", fatiguePreventionRun))
        {
            await SimulateAlertDeduplication();
            await SimulateAlertSuppression();
            await SimulateAlertTuning();
        }
    }

    /// <summary>
    /// Simulate alert deduplication to prevent duplicate notifications
    /// </summary>
    private async Task SimulateAlertDeduplication()
    {
        using (LogContext.PushProperty("Strategy", "alert_deduplication"))
        {
            _logger.LogInformation("Alert deduplication analysis",
                new { 
                    duplicate_alerts_detected = 5,
                    original_alert_id = "abc12345",
                    duplicate_alert_ids = new[] { "def67890", "ghi24681", "jkl13579", "mno86420", "pqr97531" },
                    deduplication_window_minutes = 10,
                    notifications_prevented = 15,
                    alert_volume_reduction_percent = 75,
                    deduplication_criteria = new[] { "same_service", "same_metric", "similar_threshold_breach" }
                });
        }
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Simulate intelligent alert suppression during maintenance windows
    /// </summary>
    private async Task SimulateAlertSuppression()
    {
        using (LogContext.PushProperty("Strategy", "intelligent_suppression"))
        {
            _logger.LogInformation("Alert suppression during maintenance window",
                new { 
                    maintenance_window_active = true,
                    maintenance_start = DateTimeOffset.UtcNow.AddMinutes(-30),
                    maintenance_end = DateTimeOffset.UtcNow.AddMinutes(60),
                    affected_services = new[] { "user-service", "content-delivery" },
                    alerts_suppressed = 12,
                    suppression_criteria = new[] { "planned_maintenance", "expected_degradation", "non_customer_impacting" },
                    monitoring_continues = true,
                    escalation_overrides = new[] { "security_alerts", "data_loss_alerts" }
                });
        }
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Simulate alert threshold tuning based on historical data
    /// </summary>
    private async Task SimulateAlertTuning()
    {
        using (LogContext.PushProperty("Strategy", "threshold_tuning"))
        {
            _logger.LogInformation("Alert threshold tuning analysis",
                new { 
                    analysis_period_days = 30,
                    false_positive_rate_before = 15.2,
                    false_positive_rate_after = 3.8,
                    alert_volume_reduction_percent = 45,
                    tuned_thresholds = new object[] 
                    {
                        new { metric = "cpu_utilization", old_threshold = 70, new_threshold = 85 },
                        new { metric = "error_rate", old_threshold = 1.0, new_threshold = 2.5 },
                        new { metric = "response_time_p95", old_threshold = 100, new_threshold = 150 }
                    },
                    tuning_methodology = "statistical_analysis_with_seasonal_adjustment",
                    confidence_level = 95.5
                });
        }
        
        await Task.CompletedTask;
    }

    // Helper methods for realistic data simulation
    private double SimulateAvailabilityMeasurement(double target)
    {
        // Most services perform close to target with occasional dips
        var baseAvailability = target + (_deterministicRandom.NextDouble() - 0.5) * 0.2;
        // Occasional significant issues (5% chance)
        if (_deterministicRandom.NextDouble() < 0.05)
        {
            baseAvailability -= _deterministicRandom.NextDouble() * 2.0; // Up to 2% availability drop
        }
        return Math.Max(0, Math.Min(100, baseAvailability));
    }

    private double SimulateLatencyMeasurement(double target)
    {
        // Latency usually varies around target with occasional spikes
        var baseLatency = target * (0.7 + _deterministicRandom.NextDouble() * 0.6); // ±30% variation
        // Occasional latency spikes (10% chance)
        if (_deterministicRandom.NextDouble() < 0.1)
        {
            baseLatency *= 1.5 + _deterministicRandom.NextDouble() * 2.0; // 1.5x to 3.5x spike
        }
        return Math.Max(0, baseLatency);
    }

    private double SimulateErrorRateMeasurement(double target)
    {
        // Error rate usually below target with occasional spikes
        var baseErrorRate = target * _deterministicRandom.NextDouble() * 0.8; // Usually below target
        // Occasional error spikes (8% chance)
        if (_deterministicRandom.NextDouble() < 0.08)
        {
            baseErrorRate = target * (2.0 + _deterministicRandom.NextDouble() * 5.0); // 2x to 7x spike
        }
        return Math.Max(0, baseErrorRate);
    }

    private int SimulateThroughputMeasurement(int target)
    {
        // Throughput varies based on load patterns
        var variation = 0.3 + _deterministicRandom.NextDouble() * 0.4; // 30% to 70% of target
        return (int)(target * variation);
    }

    private AlertSeverity GetAvailabilitySeverity(double current, double target)
    {
        var deviation = target - current;
        return deviation switch
        {
            > 5.0 => AlertSeverity.Critical,  // >5% availability loss
            > 1.0 => AlertSeverity.High,      // >1% availability loss  
            > 0.5 => AlertSeverity.Medium,    // >0.5% availability loss
            > 0.1 => AlertSeverity.Low,       // >0.1% availability loss
            _ => AlertSeverity.Info
        };
    }

    private AlertSeverity GetLatencySeverity(double current, double target)
    {
        var ratio = current / target;
        return ratio switch
        {
            > 5.0 => AlertSeverity.Critical,  // >5x target latency
            > 3.0 => AlertSeverity.High,      // >3x target latency
            > 2.0 => AlertSeverity.Medium,    // >2x target latency
            > 1.5 => AlertSeverity.Low,       // >1.5x target latency
            _ => AlertSeverity.Info
        };
    }

    private AlertSeverity GetErrorRateSeverity(double current, double target)
    {
        var ratio = current / target;
        return ratio switch
        {
            > 10.0 => AlertSeverity.Critical, // >10x target error rate
            > 5.0 => AlertSeverity.High,      // >5x target error rate
            > 3.0 => AlertSeverity.Medium,    // >3x target error rate
            > 2.0 => AlertSeverity.Low,       // >2x target error rate
            _ => AlertSeverity.Info
        };
    }

    private double CalculateDeviationPercent(double current, double target, string metricType)
    {
        return metricType switch
        {
            "availability" => Math.Round(((target - current) / target) * 100, 2),
            "latency_p99" => Math.Round(((current - target) / target) * 100, 2),
            "error_rate" => Math.Round(((current - target) / target) * 100, 2),
            _ => 0.0
        };
    }

    private double CalculateBurnRate(double consumptionPercent, int daysIntoMonth)
    {
        // Burn rate = (consumption% / days_elapsed) / (100% / total_days_in_month)
        return (consumptionPercent / daysIntoMonth) / (100.0 / 30.0);
    }

    private double CalculateDaysRemaining(double consumptionPercent, double burnRate)
    {
        if (burnRate <= 0) return double.PositiveInfinity;
        return (100.0 - consumptionPercent) / (burnRate * 100.0 / 30.0);
    }

    private string GetBudgetStatus(double consumptionPercent) => consumptionPercent switch
    {
        > 90 => "critical",
        > 75 => "warning", 
        > 50 => "caution",
        _ => "healthy"
    };

    private string[] GetErrorBudgetRecommendations(string alertType) => alertType switch
    {
        "high_consumption" => new[] { "review_incident_impact", "implement_additional_monitoring", "consider_feature_rollback" },
        "high_burn_rate" => new[] { "investigate_recent_changes", "check_deployment_correlation", "review_error_patterns" },
        "moderate_consumption" => new[] { "monitor_trends", "review_slo_appropriateness", "plan_reliability_improvements" },
        _ => new[] { "continue_monitoring" }
    };

    private int GetChannelRecipientCount(string channel) => channel switch
    {
        "pagerduty" => 3,
        "sms" => 2,
        "phone" => 1,
        "slack" => 15,
        "email" => 8,
        _ => 1
    };

    private string GetNotificationFormat(string channel) => channel switch
    {
        "pagerduty" => "structured_incident",
        "sms" => "concise_text",
        "phone" => "voice_alert",
        "slack" => "rich_message_card",
        "email" => "detailed_html",
        _ => "plain_text"
    };

    private string GetNotificationFailureReason() =>
        new[] { "rate_limit_exceeded", "service_temporarily_unavailable", "invalid_recipient", "network_timeout", "authentication_failed" }[_deterministicRandom.Next(5)];
}

// Supporting data structures
public class ServiceLevelObjective
{
    public string ServiceName { get; set; } = "";
    public double AvailabilityTarget { get; set; }  // Percentage (e.g., 99.9)
    public double LatencyP99Target { get; set; }    // Milliseconds
    public double ErrorRateTarget { get; set; }     // Percentage (e.g., 0.1)
    public int ThroughputTarget { get; set; }       // Requests per second
}

public class AlertEscalationPolicy
{
    public int InitialNotificationMinutes { get; set; }
    public int EscalationIntervalMinutes { get; set; }
    public int MaxEscalationLevels { get; set; }
    public string[] NotificationChannels { get; set; } = Array.Empty<string>();
}

public class SLOViolation
{
    public string MetricType { get; set; } = "";
    public double CurrentValue { get; set; }
    public double TargetValue { get; set; }
    public GoogleSREAlertingService.AlertSeverity Severity { get; set; }
}

/// <summary>
/// Console application entry point demonstrating enterprise alert configuration patterns
/// </summary>
class Program
{
    // === OBSERVABILITY ENDPOINTS - Environment Variable Pattern ===
    // These endpoints are configurable via environment variables for Aspire dynamic port mapping
    private static string GrafanaUrl =>
        Environment.GetEnvironmentVariable("GRAFANA_URL") ?? "http://localhost:18010";
    
    private static string PrometheusUrl =>
        Environment.GetEnvironmentVariable("PROMETHEUS_URL") ?? "http://localhost:18006";

    static async Task Main(string[] args)
    {
        // Configure comprehensive logging
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: 
                "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.File("logs/exercise44-alert-configuration-.log", 
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .CreateLogger();

        Console.WriteLine("🚀 Day 4 Exercise 4.4: Google SRE Alert Configuration");
        Console.WriteLine("=======================================================");
        Console.WriteLine("📊 Google SRE alert configuration and SLO monitoring");
        Console.WriteLine("🏢 SLI/SLO based alerting with error budget tracking");
        Console.WriteLine("📈 Alert escalation and fatigue prevention strategies");
        Console.WriteLine("🔔 Production-grade alerting best practices");
        Console.WriteLine($"📊 Grafana Dashboard: {GrafanaUrl}");
        Console.WriteLine($"🔍 Prometheus Metrics: {PrometheusUrl}");
        Console.WriteLine("");

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddSingleton<GoogleSREAlertingService>();
            })
            .UseSerilog()
            .Build();

        try
        {
            Log.Information("Starting Exercise 4.4: Alert Configuration with Google SRE patterns");
            
            var alertingService = host.Services.GetRequiredService<GoogleSREAlertingService>();
            await alertingService.RunAlertConfigurationDemo();
            
            Console.WriteLine("\n✅ Alert configuration demonstration completed successfully!");
            Console.WriteLine("📋 Key learning outcomes:");
            Console.WriteLine("   • SLI/SLO based alerting strategies");
            Console.WriteLine("   • Error budget tracking and burn rate analysis");
            Console.WriteLine("   • Alert escalation policies and procedures");
            Console.WriteLine("   • Alert fatigue prevention techniques");
            Console.WriteLine("   • Production-grade notification systems");
            Console.WriteLine("   • Google SRE alerting best practices");
            Console.WriteLine("\n📁 Log files generated:");
            Console.WriteLine("   • logs/exercise44-alert-configuration-*.log");
            Console.WriteLine("");
            Console.WriteLine("🔍 View alerts at:");
            Console.WriteLine($"   📊 Grafana Dashboard: {GrafanaUrl}");
            Console.WriteLine($"   🔍 Prometheus Metrics: {PrometheusUrl}");
            
            Log.Information("Exercise 4.4: Alert Configuration completed successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in Exercise 4.4: Alert Configuration");
            Console.WriteLine($"❌ Error: {ex.Message}");
            Environment.ExitCode = 1;
        }
        finally
        {
            await host.StopAsync();
            await Log.CloseAndFlushAsync();
        }
    }
}
