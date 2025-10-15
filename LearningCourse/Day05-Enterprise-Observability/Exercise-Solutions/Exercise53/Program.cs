using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Context;
using Serilog.Formatting.Compact;
using Serilog.Events;

/// <summary>
/// Enterprise-grade log aggregation implementation following ELK (Elasticsearch, Logstash, Kibana) patterns.
/// Demonstrates structured logging, correlation IDs, and centralized log management for high-volume systems.
/// 
/// References:
/// - ELK Stack Best Practices for Enterprise Logging
/// - Grafana Loki Log Aggregation Architecture
/// - Splunk Enterprise Logging Standards
/// - Twitter's Real-time Log Processing Pipeline
/// </summary>
public class EnterpriseLogAggregationService
{
    private readonly ILogger<EnterpriseLogAggregationService> _logger;
    private readonly Random _deterministicRandom;

    // === ENTERPRISE LOG CATEGORIES AND VOLUMES ===
    // Based on real-world enterprise logging patterns from Twitter, LinkedIn, and Uber
    
    private readonly Dictionary<string, LogEventLevel> _logCategories = new()
    {
        { "authentication", LogEventLevel.Information },
        { "authorization", LogEventLevel.Warning },
        { "business_transaction", LogEventLevel.Information },
        { "performance_metric", LogEventLevel.Debug },
        { "security_event", LogEventLevel.Warning },
        { "system_health", LogEventLevel.Information },
        { "user_activity", LogEventLevel.Information },
        { "api_request", LogEventLevel.Debug },
        { "database_query", LogEventLevel.Debug },
        { "cache_operation", LogEventLevel.Verbose },
        { "message_queue", LogEventLevel.Information },
        { "external_service", LogEventLevel.Information },
        { "audit_trail", LogEventLevel.Information },
        { "error_tracking", LogEventLevel.Error },
        { "compliance_log", LogEventLevel.Warning }
    };

    // Realistic enterprise application components generating logs
    private readonly string[] _applicationComponents = 
    {
        "user-authentication-service",
        "content-delivery-api",
        "recommendation-engine", 
        "payment-processing-service",
        "notification-dispatcher",
        "real-time-analytics",
        "fraud-detection-system",
        "content-moderation-ai",
        "search-indexing-service",
        "social-graph-processor"
    };

    // Enterprise environments with different log volumes
    private readonly Dictionary<string, double> _environmentLogMultipliers = new()
    {
        { "production", 1.0 },      // Baseline volume
        { "staging", 0.3 },         // 30% of production volume
        { "development", 0.1 },     // 10% of production volume
        { "testing", 0.05 },        // 5% of production volume
        { "performance", 2.5 }      // 250% during load testing
    };

    public EnterpriseLogAggregationService(ILogger<EnterpriseLogAggregationService> logger)
    {
        _logger = logger;
        // Deterministic random for consistent educational outcomes
        _deterministicRandom = new Random(DateTime.Now.Hour * 60 + DateTime.Now.Minute);
    }

    public async Task RunLogAggregationDemo()
    {
        _logger.LogInformation("Starting enterprise log aggregation demonstration");
        
        using (LogContext.PushProperty("DemoType", "log_aggregation"))
        using (LogContext.PushProperty("CompanyPattern", "enterprise_elk_stack"))
        using (LogContext.PushProperty("LogFormat", "structured_json"))
        {
            // Simulate various enterprise logging scenarios
            await SimulateHighVolumeBusinessLogs();
            await SimulateSecurityAndComplianceLogs();
            await SimulatePerformanceAndDiagnosticLogs();
            await SimulateErrorTrackingAndAlerting();
        }
        
        _logger.LogInformation("Enterprise log aggregation demonstration completed");
    }

    /// <summary>
    /// Simulate high-volume business transaction logs (Twitter scale: 500M tweets/day)
    /// Demonstrates structured logging for business-critical operations
    /// </summary>
    private async Task SimulateHighVolumeBusinessLogs()
    {
        _logger.LogInformation("Simulating high-volume business transaction logs...");
        
        var correlationId = Guid.NewGuid().ToString("N")[..16];
        var sessionId = Guid.NewGuid().ToString("N")[..12];
        
        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("SessionId", sessionId))
        using (LogContext.PushProperty("LogCategory", "business_transaction"))
        using (LogContext.PushProperty("Environment", "production"))
        {
            // Simulate Twitter-scale content creation and engagement
            await SimulateContentCreationLogs(correlationId);
            await SimulateUserEngagementLogs(correlationId);
            await SimulateRecommendationEngineLogs();
            await SimulateBusinessMetricsLogs();
        }
    }

    /// <summary>
    /// Simulate content creation logs (Twitter: 500M tweets/day, Instagram: 100M photos/day)
    /// </summary>
    private async Task SimulateContentCreationLogs(string correlationId)
    {
        var userId = _deterministicRandom.Next(1000000, 99999999);
        var contentId = Guid.NewGuid().ToString("N")[..16];
        
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("ContentId", contentId))
        using (LogContext.PushProperty("Component", "content-creation-pipeline"))
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            // Content validation and processing
            _logger.LogInformation("Content creation initiated",
                new { 
                    content_type = "text_post",
                    content_length = _deterministicRandom.Next(50, 280),
                    media_attachments = _deterministicRandom.Next(0, 4),
                    processing_pipeline = "content_validation"
                });

            await Task.Delay(_deterministicRandom.Next(10, 50)); // Simulate processing time

            // AI content moderation
            var moderationScore = _deterministicRandom.NextDouble();
            _logger.LogInformation("Content moderation completed",
                new { 
                    moderation_score = Math.Round(moderationScore, 3),
                    moderation_result = moderationScore > 0.8 ? "approved" : "flagged_for_review",
                    ai_model_version = "content_moderator_v3.2",
                    processing_time_ms = _deterministicRandom.Next(15, 85)
                });

            // Content publishing
            if (moderationScore > 0.8)
            {
                _logger.LogInformation("Content published successfully",
                    new { 
                        publication_timestamp = DateTimeOffset.UtcNow,
                        visibility = "public",
                        indexing_queue = "real_time_search",
                        cdn_distribution = "global"
                    });
            }
            else
            {
                _logger.LogWarning("Content flagged for manual review",
                    new { 
                        flagging_reason = "potential_policy_violation",
                        review_queue = "human_moderation",
                        priority = "standard",
                        estimated_review_time_hours = 2
                    });
            }
        }
    }

    /// <summary>
    /// Simulate user engagement logs (Twitter: 1B interactions/day)
    /// </summary>
    private async Task SimulateUserEngagementLogs(string correlationId)
    {
        var engagementTypes = new[] { "like", "retweet", "reply", "share", "bookmark", "view" };
        var userId = _deterministicRandom.Next(1000000, 99999999);
        var targetContentId = Guid.NewGuid().ToString("N")[..16];

        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("TargetContentId", targetContentId))
        using (LogContext.PushProperty("Component", "engagement-tracking"))
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            for (int i = 0; i < 5; i++) // Simulate multiple engagement events
            {
                var engagementType = engagementTypes[_deterministicRandom.Next(engagementTypes.Length)];
                var engagementWeight = GetEngagementWeight(engagementType);
                
                _logger.LogInformation("User engagement recorded",
                    new { 
                        engagement_type = engagementType,
                        engagement_weight = engagementWeight,
                        device_type = GetRandomDeviceType(),
                        location_country = GetRandomCountry(),
                        session_duration_seconds = _deterministicRandom.Next(30, 3600),
                        is_organic = _deterministicRandom.NextDouble() > 0.15 // 85% organic engagement
                    });

                await Task.Delay(_deterministicRandom.Next(5, 25)); // Simulate real-time processing
            }

            // Aggregate engagement metrics
            _logger.LogInformation("Engagement session completed",
                new { 
                    total_engagements = 5,
                    session_quality_score = Math.Round(_deterministicRandom.NextDouble() * 100, 1),
                    algorithmic_boost_applied = _deterministicRandom.NextDouble() > 0.7,
                    revenue_attribution_cents = _deterministicRandom.Next(0, 50)
                });
        }
    }

    /// <summary>
    /// Simulate recommendation engine logs (Netflix: 80% of viewing from recommendations)
    /// </summary>
    private async Task SimulateRecommendationEngineLogs()
    {
        var userId = _deterministicRandom.Next(1000000, 99999999);
        var algorithmVersion = "recommendation_engine_v4.7";

        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("Component", "recommendation-engine"))
        using (LogContext.PushProperty("AlgorithmVersion", algorithmVersion))
        {
            // User profile analysis
            _logger.LogDebug("User profile analysis started",
                new { 
                    profile_dimensions = 847,
                    interaction_history_days = 90,
                    content_preferences = new[] { "technology", "entertainment", "sports" },
                    demographic_segment = "millennial_tech_professional"
                });

            await Task.Delay(_deterministicRandom.Next(20, 80)); // ML inference time

            // Content scoring and ranking
            var candidateContents = _deterministicRandom.Next(1000, 5000);
            _logger.LogDebug("Content scoring completed",
                new { 
                    candidate_content_count = candidateContents,
                    ml_model_latency_ms = _deterministicRandom.Next(45, 150),
                    feature_extraction_time_ms = _deterministicRandom.Next(15, 60),
                    ranking_algorithm = "collaborative_filtering_v3"
                });

            // Recommendation delivery
            var recommendedItems = _deterministicRandom.Next(10, 50);
            _logger.LogInformation("Recommendations generated",
                new { 
                    recommended_items_count = recommendedItems,
                    diversity_score = Math.Round(_deterministicRandom.NextDouble(), 3),
                    freshness_weight = 0.3,
                    personalization_strength = Math.Round(_deterministicRandom.NextDouble(), 3),
                    expected_ctr = Math.Round(_deterministicRandom.NextDouble() * 0.15, 4) // 0-15% CTR
                });

            // A/B testing attribution
            var abTestVariant = _deterministicRandom.NextDouble() > 0.5 ? "variant_a" : "variant_b";
            _logger.LogInformation("A/B test attribution recorded",
                new { 
                    experiment_id = "rec_algorithm_test_2024_q1",
                    variant = abTestVariant,
                    user_cohort = "power_users",
                    statistical_significance = true
                });
        }
    }

    /// <summary>
    /// Simulate business metrics logs for real-time dashboards
    /// </summary>
    private async Task SimulateBusinessMetricsLogs()
    {
        using (LogContext.PushProperty("Component", "business-metrics-collector"))
        using (LogContext.PushProperty("MetricType", "real_time_kpi"))
        {
            // Revenue metrics
            _logger.LogInformation("Revenue metrics collected",
                new { 
                    metric_name = "hourly_revenue",
                    value_usd = _deterministicRandom.Next(50000, 500000),
                    currency = "USD",
                    region = "north_america",
                    business_unit = "core_platform",
                    growth_vs_previous_hour_percent = Math.Round((_deterministicRandom.NextDouble() - 0.5) * 20, 2)
                });

            await Task.Delay(10);

            // User acquisition metrics
            _logger.LogInformation("User acquisition metrics collected",
                new { 
                    metric_name = "new_user_signups",
                    value = _deterministicRandom.Next(1000, 10000),
                    acquisition_channel = GetRandomAcquisitionChannel(),
                    conversion_rate_percent = Math.Round(_deterministicRandom.NextDouble() * 5, 2),
                    cost_per_acquisition_usd = Math.Round(_deterministicRandom.NextDouble() * 25, 2)
                });

            await Task.Delay(10);

            // Operational metrics
            _logger.LogInformation("Operational metrics collected",
                new { 
                    metric_name = "system_health_score",
                    value = Math.Round(_deterministicRandom.NextDouble() * 100, 1),
                    uptime_percent = 99.97,
                    active_incidents = _deterministicRandom.Next(0, 3),
                    performance_sla_compliance_percent = Math.Round(95 + _deterministicRandom.NextDouble() * 5, 2)
                });
        }
    }

    /// <summary>
    /// Simulate security and compliance logs (critical for enterprise environments)
    /// </summary>
    private async Task SimulateSecurityAndComplianceLogs()
    {
        _logger.LogInformation("Simulating security and compliance logs...");
        
        var securityCorrelationId = Guid.NewGuid().ToString("N")[..16];
        
        using (LogContext.PushProperty("CorrelationId", securityCorrelationId))
        using (LogContext.PushProperty("LogCategory", "security_audit"))
        using (LogContext.PushProperty("ComplianceFramework", "SOC2_GDPR"))
        {
            await SimulateAuthenticationLogs();
            await SimulateAuthorizationLogs();
            await SimulateFraudDetectionLogs();
            await SimulateComplianceAuditLogs();
        }
    }

    /// <summary>
    /// Simulate authentication logs with security events
    /// </summary>
    private async Task SimulateAuthenticationLogs()
    {
        var userId = _deterministicRandom.Next(1000000, 99999999);
        var sessionToken = Guid.NewGuid().ToString("N")[..24];
        
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("SessionToken", sessionToken))
        using (LogContext.PushProperty("Component", "authentication-service"))
        {
            // Login attempt
            var isSuccessfulLogin = _deterministicRandom.NextDouble() > 0.05; // 95% success rate
            var ipAddress = GenerateRandomIPAddress();
            var userAgent = GetRandomUserAgent();
            
            if (isSuccessfulLogin)
            {
                _logger.LogInformation("User authentication successful",
                    new { 
                        authentication_method = "oauth2",
                        ip_address = ipAddress,
                        user_agent = userAgent,
                        location_country = GetRandomCountry(),
                        device_fingerprint = Guid.NewGuid().ToString("N")[..16],
                        risk_score = Math.Round(_deterministicRandom.NextDouble() * 100, 1),
                        mfa_enabled = _deterministicRandom.NextDouble() > 0.3 // 70% have MFA
                    });
            }
            else
            {
                var failureReason = GetRandomAuthFailureReason();
                _logger.LogWarning("User authentication failed",
                    new { 
                        failure_reason = failureReason,
                        ip_address = ipAddress,
                        user_agent = userAgent,
                        attempt_count = _deterministicRandom.Next(1, 5),
                        account_locked = failureReason == "too_many_attempts",
                        security_alert_triggered = true
                    });
            }

            await Task.Delay(_deterministicRandom.Next(10, 50));
        }
    }

    /// <summary>
    /// Simulate authorization logs for resource access
    /// </summary>
    private async Task SimulateAuthorizationLogs()
    {
        var userId = _deterministicRandom.Next(1000000, 99999999);
        var resourceId = Guid.NewGuid().ToString("N")[..12];
        
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("ResourceId", resourceId))
        using (LogContext.PushProperty("Component", "authorization-service"))
        {
            var action = GetRandomAction();
            var resource = GetRandomResource();
            var isAuthorized = _deterministicRandom.NextDouble() > 0.15; // 85% authorized
            
            _logger.LogInformation("Authorization check performed",
                new { 
                    action = action,
                    resource = resource,
                    authorization_result = isAuthorized ? "granted" : "denied",
                    user_role = GetRandomUserRole(),
                    permission_set = new[] { "read", "write", "admin" },
                    policy_version = "rbac_policy_v2.1",
                    evaluation_time_ms = _deterministicRandom.Next(5, 25)
                });

            if (!isAuthorized)
            {
                _logger.LogWarning("Unauthorized access attempt detected",
                    new { 
                        attempted_action = action,
                        attempted_resource = resource,
                        security_impact = "privilege_escalation_attempt",
                        investigation_required = true,
                        incident_id = Guid.NewGuid().ToString("N")[..8]
                    });
            }

            await Task.Delay(_deterministicRandom.Next(5, 20));
        }
    }

    /// <summary>
    /// Simulate fraud detection logs (Stripe/PayPal scale: millions of transactions)
    /// </summary>
    private async Task SimulateFraudDetectionLogs()
    {
        var transactionId = Guid.NewGuid().ToString("N")[..16];
        var userId = _deterministicRandom.Next(1000000, 99999999);
        
        using (LogContext.PushProperty("TransactionId", transactionId))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("Component", "fraud-detection-ml"))
        {
            var transactionAmount = Math.Round(_deterministicRandom.NextDouble() * 1000, 2);
            var fraudScore = Math.Round(_deterministicRandom.NextDouble() * 100, 2);
            var isFraudulent = fraudScore > 75; // High-risk threshold
            
            _logger.LogInformation("Fraud detection analysis completed",
                new { 
                    transaction_amount_usd = transactionAmount,
                    fraud_risk_score = fraudScore,
                    ml_model_version = "fraud_detector_v4.3",
                    risk_factors = GetRandomFraudRiskFactors(),
                    device_analysis = new {
                        device_known = _deterministicRandom.NextDouble() > 0.2,
                        location_anomaly = _deterministicRandom.NextDouble() < 0.1,
                        velocity_check_passed = _deterministicRandom.NextDouble() > 0.05
                    }
                });

            if (isFraudulent)
            {
                _logger.LogError("Fraudulent transaction detected",
                    new { 
                        transaction_status = "blocked",
                        fraud_type = GetRandomFraudType(),
                        confidence_level = Math.Round(_deterministicRandom.NextDouble() * 0.3 + 0.7, 3), // 70-100%
                        investigation_priority = "high",
                        law_enforcement_notified = fraudScore > 90
                    });
            }

            await Task.Delay(_deterministicRandom.Next(20, 100)); // ML inference time
        }
    }

    /// <summary>
    /// Simulate compliance audit logs (SOC2, GDPR, HIPAA requirements)
    /// </summary>
    private async Task SimulateComplianceAuditLogs()
    {
        var auditId = Guid.NewGuid().ToString("N")[..12];
        
        using (LogContext.PushProperty("AuditId", auditId))
        using (LogContext.PushProperty("Component", "compliance-auditor"))
        {
            // Data access audit
            _logger.LogInformation("Data access audit completed",
                new { 
                    audit_type = "data_access",
                    compliance_framework = "GDPR",
                    user_count_audited = _deterministicRandom.Next(1000, 10000),
                    data_categories = new[] { "pii", "financial", "health", "behavioral" },
                    violations_detected = _deterministicRandom.Next(0, 5),
                    retention_policy_compliance_percent = Math.Round(95 + _deterministicRandom.NextDouble() * 5, 2)
                });

            await Task.Delay(10);

            // Security controls audit
            _logger.LogInformation("Security controls audit completed",
                new { 
                    audit_type = "security_controls",
                    compliance_framework = "SOC2",
                    controls_tested = 127,
                    controls_passed = 125,
                    critical_findings = _deterministicRandom.Next(0, 2),
                    remediation_required = _deterministicRandom.NextDouble() < 0.1
                });

            await Task.Delay(10);
        }
    }

    /// <summary>
    /// Simulate performance and diagnostic logs for system monitoring
    /// </summary>
    private async Task SimulatePerformanceAndDiagnosticLogs()
    {
        _logger.LogInformation("Simulating performance and diagnostic logs...");
        
        var performanceCorrelationId = Guid.NewGuid().ToString("N")[..16];
        
        using (LogContext.PushProperty("CorrelationId", performanceCorrelationId))
        using (LogContext.PushProperty("LogCategory", "performance_diagnostics"))
        {
            await SimulateSystemPerformanceLogs();
            await SimulateDatabasePerformanceLogs();
            await SimulateAPIPerformanceLogs();
            await SimulateInfrastructureHealthLogs();
        }
    }

    /// <summary>
    /// Simulate system performance logs (CPU, Memory, Network)
    /// </summary>
    private async Task SimulateSystemPerformanceLogs()
    {
        using (LogContext.PushProperty("Component", "system-monitor"))
        {
            for (int i = 0; i < 3; i++) // Multiple measurement intervals
            {
                _logger.LogDebug("System performance metrics collected",
                    new { 
                        cpu_utilization_percent = Math.Round(20 + _deterministicRandom.NextDouble() * 60, 1),
                        memory_utilization_percent = Math.Round(30 + _deterministicRandom.NextDouble() * 50, 1),
                        network_throughput_mbps = Math.Round(_deterministicRandom.NextDouble() * 1000, 1),
                        disk_io_ops_per_second = _deterministicRandom.Next(100, 5000),
                        load_average = Math.Round(_deterministicRandom.NextDouble() * 4, 2),
                        active_connections = _deterministicRandom.Next(500, 10000)
                    });

                await Task.Delay(100); // Simulate measurement interval
            }
        }
    }

    /// <summary>
    /// Simulate database performance logs
    /// </summary>
    private async Task SimulateDatabasePerformanceLogs()
    {
        using (LogContext.PushProperty("Component", "database-monitor"))
        {
            var queries = new[] { "SELECT", "INSERT", "UPDATE", "DELETE" };
            
            foreach (var queryType in queries)
            {
                _logger.LogDebug("Database query performance",
                    new { 
                        query_type = queryType,
                        execution_time_ms = _deterministicRandom.Next(5, 500),
                        rows_affected = _deterministicRandom.Next(1, 10000),
                        connection_pool_utilization_percent = Math.Round(_deterministicRandom.NextDouble() * 80, 1),
                        cache_hit_rate_percent = Math.Round(70 + _deterministicRandom.NextDouble() * 30, 1),
                        deadlocks_detected = _deterministicRandom.Next(0, 2)
                    });

                await Task.Delay(20);
            }
        }
    }

    /// <summary>
    /// Simulate API performance logs
    /// </summary>
    private async Task SimulateAPIPerformanceLogs()
    {
        using (LogContext.PushProperty("Component", "api-gateway"))
        {
            var endpoints = new[] { "/api/users", "/api/content", "/api/search", "/api/recommendations" };
            
            foreach (var endpoint in endpoints)
            {
                _logger.LogDebug("API endpoint performance",
                    new { 
                        endpoint = endpoint,
                        response_time_ms = _deterministicRandom.Next(10, 200),
                        status_code = _deterministicRandom.NextDouble() > 0.05 ? 200 : 500,
                        request_size_bytes = _deterministicRandom.Next(100, 5000),
                        response_size_bytes = _deterministicRandom.Next(500, 50000),
                        rate_limit_remaining = _deterministicRandom.Next(0, 1000)
                    });

                await Task.Delay(15);
            }
        }
    }

    /// <summary>
    /// Simulate infrastructure health logs
    /// </summary>
    private async Task SimulateInfrastructureHealthLogs()
    {
        using (LogContext.PushProperty("Component", "infrastructure-health"))
        {
            var services = new[] { "web-servers", "cache-cluster", "message-queue", "search-index" };
            
            foreach (var service in services)
            {
                var isHealthy = _deterministicRandom.NextDouble() > 0.1; // 90% healthy
                
                if (isHealthy)
                {
                    _logger.LogInformation("Service health check passed",
                        new { 
                            service_name = service,
                            health_status = "healthy",
                            response_time_ms = _deterministicRandom.Next(5, 50),
                            availability_percent = Math.Round(99.9 + _deterministicRandom.NextDouble() * 0.1, 3),
                            error_rate_percent = Math.Round(_deterministicRandom.NextDouble() * 0.1, 4)
                        });
                }
                else
                {
                    _logger.LogError("Service health check failed",
                        new { 
                            service_name = service,
                            health_status = "unhealthy",
                            failure_reason = GetRandomHealthFailureReason(),
                            incident_severity = "high",
                            auto_recovery_attempted = true
                        });
                }

                await Task.Delay(25);
            }
        }
    }

    /// <summary>
    /// Simulate error tracking and alerting logs
    /// </summary>
    private async Task SimulateErrorTrackingAndAlerting()
    {
        _logger.LogInformation("Simulating error tracking and alerting logs...");
        
        var errorCorrelationId = Guid.NewGuid().ToString("N")[..16];
        
        using (LogContext.PushProperty("CorrelationId", errorCorrelationId))
        using (LogContext.PushProperty("LogCategory", "error_tracking"))
        {
            await SimulateApplicationErrors();
            await SimulateSystemErrors();
            await SimulateAlertGeneration();
        }
    }

    /// <summary>
    /// Simulate application errors with detailed context
    /// </summary>
    private async Task SimulateApplicationErrors()
    {
        using (LogContext.PushProperty("Component", "application-error-tracker"))
        {
            // Simulate various application errors
            var errorTypes = new[] { "NullReferenceException", "TimeoutException", "ValidationException", "BusinessLogicException" };
            
            foreach (var errorType in errorTypes.Take(2)) // Limit for demo
            {
                var stackTrace = GenerateSimulatedStackTrace(errorType);
                
                _logger.LogError("Application error occurred",
                    new { 
                        error_type = errorType,
                        error_message = GetErrorMessage(errorType),
                        stack_trace = stackTrace,
                        user_id = _deterministicRandom.Next(1000000, 99999999),
                        request_id = Guid.NewGuid().ToString("N")[..16],
                        error_frequency_last_hour = _deterministicRandom.Next(1, 50),
                        impact_level = GetErrorImpactLevel(errorType)
                    });

                await Task.Delay(30);
            }
        }
    }

    /// <summary>
    /// Simulate system errors
    /// </summary>
    private async Task SimulateSystemErrors()
    {
        using (LogContext.PushProperty("Component", "system-error-tracker"))
        {
            var systemErrors = new[] { "OutOfMemoryException", "DiskSpaceException", "NetworkException", "DatabaseConnectionException" };
            
            foreach (var errorType in systemErrors.Take(1)) // Limit for demo
            {
                _logger.LogError("System error detected",
                    new { 
                        error_type = errorType,
                        error_message = GetSystemErrorMessage(errorType),
                        affected_services = GetAffectedServices(),
                        recovery_action = GetRecoveryAction(errorType),
                        estimated_recovery_time_minutes = _deterministicRandom.Next(5, 60),
                        customer_impact = GetCustomerImpact(errorType)
                    });

                await Task.Delay(40);
            }
        }
    }

    /// <summary>
    /// Simulate alert generation based on error patterns
    /// </summary>
    private async Task SimulateAlertGeneration()
    {
        using (LogContext.PushProperty("Component", "alerting-system"))
        {
            _logger.LogWarning("Alert threshold exceeded",
                new { 
                    alert_name = "error_rate_threshold_exceeded",
                    threshold_value = 5.0,
                    current_value = 7.3,
                    alert_severity = "medium",
                    notification_channels = new[] { "pagerduty", "slack", "email" },
                    escalation_policy = "engineering_team",
                    acknowledgment_required = true,
                    alert_id = Guid.NewGuid().ToString("N")[..8]
                });

            await Task.Delay(20);

            _logger.LogInformation("Alert resolved",
                new { 
                    alert_name = "error_rate_threshold_exceeded",
                    resolution_time_minutes = _deterministicRandom.Next(5, 30),
                    resolved_by = "automated_recovery",
                    root_cause = "temporary_database_slowdown",
                    preventive_actions_taken = new[] { "connection_pool_tuning", "query_optimization" }
                });
        }
    }

    // Helper methods for generating realistic test data
    private double GetEngagementWeight(string engagementType) => engagementType switch
    {
        "view" => 1.0,
        "like" => 2.5,
        "share" => 5.0,
        "reply" => 7.5,
        "retweet" => 4.0,
        "bookmark" => 3.5,
        _ => 1.0
    };

    private string GetRandomDeviceType() => 
        new[] { "mobile_ios", "mobile_android", "desktop_web", "tablet_ios", "tablet_android" }[_deterministicRandom.Next(5)];

    private string GetRandomCountry() => 
        new[] { "US", "CA", "UK", "DE", "FR", "JP", "AU", "BR", "IN", "SG" }[_deterministicRandom.Next(10)];

    private string GetRandomAcquisitionChannel() => 
        new[] { "organic_search", "social_media", "paid_ads", "referral", "direct", "email_marketing" }[_deterministicRandom.Next(6)];

    private string GenerateRandomIPAddress() => 
        $"{_deterministicRandom.Next(1, 255)}.{_deterministicRandom.Next(1, 255)}.{_deterministicRandom.Next(1, 255)}.{_deterministicRandom.Next(1, 255)}";

    private string GetRandomUserAgent() => 
        new[] { 
            "Mozilla/5.0 (iPhone; CPU iPhone OS 15_0 like Mac OS X) AppleWebKit/605.1.15",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/95.0.4638.69",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/95.0.4638.69"
        }[_deterministicRandom.Next(3)];

    private string GetRandomAuthFailureReason() => 
        new[] { "invalid_password", "account_locked", "too_many_attempts", "expired_token", "invalid_mfa_code" }[_deterministicRandom.Next(5)];

    private string GetRandomAction() => 
        new[] { "read", "write", "delete", "admin", "execute", "download" }[_deterministicRandom.Next(6)];

    private string GetRandomResource() => 
        new[] { "user_profile", "financial_data", "admin_panel", "analytics_dashboard", "api_keys", "audit_logs" }[_deterministicRandom.Next(6)];

    private string GetRandomUserRole() => 
        new[] { "standard_user", "premium_user", "moderator", "admin", "super_admin", "service_account" }[_deterministicRandom.Next(6)];

    private string[] GetRandomFraudRiskFactors() => 
        new[] { "high_velocity", "unknown_device", "location_anomaly", "suspicious_pattern" }.Where(x => _deterministicRandom.NextDouble() > 0.5).ToArray();

    private string GetRandomFraudType() => 
        new[] { "card_not_present", "account_takeover", "synthetic_identity", "payment_fraud", "chargeback_fraud" }[_deterministicRandom.Next(5)];

    private string GetRandomHealthFailureReason() => 
        new[] { "connection_timeout", "high_cpu_usage", "memory_exhaustion", "disk_full", "network_partition" }[_deterministicRandom.Next(5)];

    private string GetErrorMessage(string errorType) => errorType switch
    {
        "NullReferenceException" => "Object reference not set to an instance of an object at UserService.GetProfile()",
        "TimeoutException" => "The operation has timed out after 30 seconds waiting for database response",
        "ValidationException" => "Invalid input data: email format is incorrect for user registration",
        "BusinessLogicException" => "Cannot process payment: insufficient account balance",
        _ => "An unexpected error occurred during operation"
    };

    private string GenerateSimulatedStackTrace(string errorType) => 
        $"   at MyApp.Services.{errorType.Replace("Exception", "Service")}.ProcessRequest()\n" +
        $"   at MyApp.Controllers.ApiController.HandleRequest()\n" +
        $"   at Microsoft.AspNetCore.Mvc.Infrastructure.ActionMethodExecutor.ExecuteAsync()";

    private string GetErrorImpactLevel(string errorType) => errorType switch
    {
        "NullReferenceException" => "medium",
        "TimeoutException" => "high",
        "ValidationException" => "low",
        "BusinessLogicException" => "medium",
        _ => "unknown"
    };

    private string GetSystemErrorMessage(string errorType) => errorType switch
    {
        "OutOfMemoryException" => "System is running low on available memory (>95% utilization)",
        "DiskSpaceException" => "Disk space critically low on primary storage volume (<5% free)",
        "NetworkException" => "Network connectivity issues detected with external services",
        "DatabaseConnectionException" => "Unable to establish connection to primary database cluster",
        _ => "An unexpected system error occurred"
    };

    private string[] GetAffectedServices() => 
        _applicationComponents.Where(x => _deterministicRandom.NextDouble() > 0.7).Take(3).ToArray();

    private string GetRecoveryAction(string errorType) => errorType switch
    {
        "OutOfMemoryException" => "restart_service_instances",
        "DiskSpaceException" => "cleanup_old_logs_and_temp_files",
        "NetworkException" => "failover_to_backup_connection",
        "DatabaseConnectionException" => "switch_to_read_replica",
        _ => "investigate_and_manual_intervention"
    };

    private string GetCustomerImpact(string errorType) => errorType switch
    {
        "OutOfMemoryException" => "degraded_performance",
        "DiskSpaceException" => "service_unavailable",
        "NetworkException" => "intermittent_errors",
        "DatabaseConnectionException" => "read_only_mode",
        _ => "unknown_impact"
    };
}

/// <summary>
/// Console application entry point demonstrating enterprise log aggregation patterns
/// </summary>
class Program
{
    // === OBSERVABILITY ENDPOINTS - Environment Variable Pattern ===
    // These endpoints are configurable via environment variables for Aspire dynamic port mapping
    private static string LokiUrl =>
        Environment.GetEnvironmentVariable("LOKI_URL") ?? "http://localhost:18005";
    
    private static string GrafanaUrl =>
        Environment.GetEnvironmentVariable("GRAFANA_URL") ?? "http://localhost:18010";

    static async Task Main(string[] args)
    {
        // Configure comprehensive logging with multiple sinks and structured format
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithProcessId()
            .Enrich.WithThreadId()
            .WriteTo.Console(outputTemplate: 
                "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.File(new CompactJsonFormatter(), "logs/exercise43-log-aggregation-.json", 
                rollingInterval: RollingInterval.Day,
                buffered: true,
                flushToDiskInterval: TimeSpan.FromSeconds(1))
            .WriteTo.File("logs/exercise43-log-aggregation-.log", 
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext} {Message:lj} {Properties:j}{NewLine}{Exception}")
            .CreateLogger();

        Console.WriteLine("🚀 Day 4 Exercise 4.3: Enterprise Log Aggregation");
        Console.WriteLine("=============================================================");
        Console.WriteLine("📊 Enterprise-grade log aggregation and management");
        Console.WriteLine("🏢 ELK Stack patterns with structured logging");
        Console.WriteLine("📈 High-volume log processing simulation");
        Console.WriteLine("🔍 Correlation IDs and centralized collection");
        Console.WriteLine($"📋 Loki Logs: {LokiUrl}");
        Console.WriteLine($"📊 Grafana Dashboard: {GrafanaUrl}");
        Console.WriteLine("");

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddSingleton<EnterpriseLogAggregationService>();
            })
            .UseSerilog()
            .Build();

        try
        {
            Log.Information("Starting Exercise 4.3: Log Aggregation with enterprise patterns");
            
            var logAggregationService = host.Services.GetRequiredService<EnterpriseLogAggregationService>();
            await logAggregationService.RunLogAggregationDemo();
            
            Console.WriteLine("\n✅ Log aggregation demonstration completed successfully!");
            Console.WriteLine("📋 Key learning outcomes:");
            Console.WriteLine("   • Structured logging with correlation IDs");
            Console.WriteLine("   • High-volume log management patterns");
            Console.WriteLine("   • Security and compliance logging");
            Console.WriteLine("   • Performance and diagnostic logs");
            Console.WriteLine("   • Error tracking and alerting integration");
            Console.WriteLine("   • Enterprise ELK stack simulation");
            Console.WriteLine("\n📁 Log files generated:");
            Console.WriteLine("   • logs/exercise43-log-aggregation-*.json (Structured JSON)");
            Console.WriteLine("   • logs/exercise43-log-aggregation-*.log (Human-readable)");
            Console.WriteLine("");
            Console.WriteLine("🔍 View logs at:");
            Console.WriteLine($"   📋 Loki Logs: {LokiUrl}");
            Console.WriteLine($"   📊 Grafana Dashboard: {GrafanaUrl}");
            
            Log.Information("Exercise 4.3: Log Aggregation completed successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in Exercise 4.3: Log Aggregation");
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
