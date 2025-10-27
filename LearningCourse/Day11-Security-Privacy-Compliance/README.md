# Day 10: Security, Privacy, and Compliance in Stream Processing

## 🗺️ Course Navigation
**[← Day 9: Performance Optimization & Scaling](../Day09-Performance-Optimization-Scaling/)** | **[Course Overview](../README.md)** | **[Next: Day 11 - Disaster Recovery & Multi-Region →](../Day11-Disaster-Recovery-Multi-Region/)**

---

## Overview
Implement enterprise-grade security, data privacy, and regulatory compliance patterns for streaming applications handling sensitive data.

## Learning Objectives
- Design secure streaming architectures with end-to-end encryption
- Implement GDPR, CCPA, and financial compliance requirements
- Build data anonymization and pseudonymization pipelines
- Create secure multi-tenancy and access control systems
- Deploy audit logging and compliance monitoring

## Real-World Context
Financial services companies like Goldman Sachs must comply with strict regulations (SOX, PCI-DSS, Basel III) while processing millions of trading transactions in real-time. Their streaming platforms implement end-to-end encryption, field-level data masking, and comprehensive audit trails.

## Technical Deep Dive

### End-to-End Encryption Pipeline
```csharp
// Financial-grade encryption for sensitive data streams
public class EncryptedStreamProcessor : KeyedProcessFunction<string, SensitiveTransaction, EncryptedResult>
{
    private readonly IFieldLevelEncryption fieldEncryption;
    private readonly IKeyRotationService keyRotation;
    private readonly IAuditLogger auditLogger;
    
    public EncryptedStreamProcessor(
        IFieldLevelEncryption fieldEncryption,
        IKeyRotationService keyRotation,
        IAuditLogger auditLogger)
    {
        this.fieldEncryption = fieldEncryption;
        this.keyRotation = keyRotation;
        this.auditLogger = auditLogger;
    }
    
    public override void ProcessElement(
        SensitiveTransaction transaction, 
        Context context, 
        ICollector<EncryptedResult> output)
    {
        var processingId = Guid.NewGuid();
        
        try
        {
            // Audit access to sensitive data
            auditLogger.LogDataAccess(new DataAccessEvent
            {
                ProcessingId = processingId,
                UserId = context.GetCurrentKey(),
                DataType = "SensitiveTransaction",
                Timestamp = DateTimeOffset.UtcNow,
                AccessReason = "Stream Processing",
                LegalBasis = "Legitimate Interest"
            });
            
            // Get current encryption key (rotated regularly)
            var encryptionKey = keyRotation.GetCurrentKey("transaction-encryption");
            
            // Encrypt sensitive fields
            var encryptedTransaction = new EncryptedTransaction
            {
                TransactionId = transaction.TransactionId, // Unencrypted for processing
                EncryptedAccountNumber = fieldEncryption.Encrypt(transaction.AccountNumber, encryptionKey),
                EncryptedAmount = fieldEncryption.Encrypt(transaction.Amount.ToString(), encryptionKey),
                EncryptedPersonalData = fieldEncryption.Encrypt(
                    JsonSerializer.Serialize(transaction.PersonalData), encryptionKey),
                KeyVersion = encryptionKey.Version,
                ProcessedAt = DateTimeOffset.UtcNow
            };
            
            // Process business logic on encrypted data where possible
            var result = ProcessEncryptedTransaction(encryptedTransaction, context);
            
            output.Collect(result);
            
            // Audit successful processing
            auditLogger.LogProcessingSuccess(processingId, "Transaction encrypted and processed");
        }
        catch (Exception ex)
        {
            auditLogger.LogProcessingFailure(processingId, ex);
            
            // Security incident handling
            if (ex is SecurityException)
            {
                TriggerSecurityIncident(processingId, ex);
            }
            
            throw;
        }
    }
}

// Field-level encryption with format-preserving encryption for analytics
public class FormatPreservingEncryption : IFieldLevelEncryption
{
    public string EncryptAccountNumber(string accountNumber, EncryptionKey key)
    {
        // FPE maintains format (e.g., 1234-5678-9012 stays ####-####-####)
        var cipher = new FPECipher(key.KeyMaterial, accountNumber.Length);
        return cipher.Encrypt(accountNumber);
    }
    
    public decimal EncryptAmount(decimal amount, EncryptionKey key)
    {
        // Homomorphic encryption allows arithmetic operations on encrypted values
        var homomorphicCipher = new HomomorphicCipher(key.KeyMaterial);
        return homomorphicCipher.Encrypt(amount);
    }
}
```

### GDPR Compliance Pipeline
```csharp
// GDPR-compliant data processing with consent management
public class GDPRCompliantProcessor : KeyedProcessFunction<string, UserEvent, ProcessedEvent>
{
    private ValueState<ConsentRecord> consentState;
    private ValueState<DataRetentionPolicy> retentionState;
    private MapState<string, PersonalDataRecord> personalDataState;
    
    public override void Open(Configuration parameters)
    {
        var consentDescriptor = new ValueStateDescriptor<ConsentRecord>(
            "user-consent", TypeInformation.Of<ConsentRecord>());
        consentState = GetRuntimeContext().GetState(consentDescriptor);
        
        var retentionDescriptor = new ValueStateDescriptor<DataRetentionPolicy>(
            "retention-policy", TypeInformation.Of<DataRetentionPolicy>());
        retentionState = GetRuntimeContext().GetState(retentionDescriptor);
        
        var personalDataDescriptor = new MapStateDescriptor<string, PersonalDataRecord>(
            "personal-data", TypeInformation.Of<string>(), TypeInformation.Of<PersonalDataRecord>());
        personalDataState = GetKeyedStateStore().GetMapState(personalDataDescriptor);
    }
    
    public override void ProcessElement(UserEvent userEvent, Context context, ICollector<ProcessedEvent> output)
    {
        var userId = context.GetCurrentKey();
        var currentConsent = consentState.Value();
        
        // Check consent validity
        if (currentConsent == null || !IsConsentValid(currentConsent, userEvent.DataType))
        {
            // Log GDPR compliance violation
            LogGDPRViolation(userId, userEvent.DataType, "No valid consent");
            
            // Don't process personal data without consent
            var anonymizedEvent = AnonymizeEvent(userEvent);
            output.Collect(CreateProcessedEvent(anonymizedEvent, "anonymized"));
            return;
        }
        
        // Check data retention policy
        var retentionPolicy = retentionState.Value();
        if (ShouldPurgeData(userEvent, retentionPolicy))
        {
            // Automatically purge expired personal data
            await PurgePersonalData(userId, userEvent.EventType);
            LogDataPurge(userId, userEvent.EventType, "Retention period expired");
            return;
        }
        
        // Process with full data (consent valid and within retention period)
        var processedEvent = ProcessWithPersonalData(userEvent, currentConsent);
        
        // Update personal data tracking for future right-to-be-forgotten requests
        personalDataState.Put(userEvent.EventId, new PersonalDataRecord
        {
            EventId = userEvent.EventId,
            DataTypes = ExtractPersonalDataTypes(userEvent),
            ProcessedAt = DateTimeOffset.UtcNow,
            LegalBasis = currentConsent.LegalBasis
        });
        
        output.Collect(processedEvent);
    }
    
    // Handle GDPR Article 17 - Right to be forgotten
    public async Task ProcessRightToBeForgotten(string userId, RightToBeForgottenRequest request)
    {
        var personalDataIterator = personalDataState.Iterator();
        var itemsToDelete = new List<string>();
        
        while (personalDataIterator.HasNext())
        {
            var entry = personalDataIterator.Next();
            var record = entry.Value;
            
            if (ShouldDeleteForRightToBeForgotten(record, request))
            {
                itemsToDelete.Add(entry.Key);
            }
        }
        
        // Delete personal data
        foreach (var eventId in itemsToDelete)
        {
            personalDataState.Remove(eventId);
            await NotifyDownstreamSystems(userId, eventId, "right-to-be-forgotten");
        }
        
        LogRightToBeForgottenCompliance(userId, itemsToDelete.Count);
    }
}
```

### Multi-Tenant Security Architecture
```csharp
// Secure multi-tenancy with tenant isolation
public class SecureMultiTenantProcessor : KeyedProcessFunction<TenantKey, TenantEvent, TenantResult>
{
    private readonly ITenantSecurityContext securityContext;
    private readonly IResourceQuotaManager quotaManager;
    private readonly ITenantKeyManager keyManager;
    
    public override void ProcessElement(TenantEvent tenantEvent, Context context, ICollector<TenantResult> output)
    {
        var tenantKey = context.GetCurrentKey();
        
        // Verify tenant authentication and authorization
        if (!securityContext.IsAuthorized(tenantKey, tenantEvent.Operation))
        {
            LogSecurityViolation(tenantKey, tenantEvent, "Unauthorized operation");
            throw new UnauthorizedAccessException($"Tenant {tenantKey.TenantId} not authorized for {tenantEvent.Operation}");
        }
        
        // Check resource quotas to prevent tenant abuse
        var currentUsage = quotaManager.GetCurrentUsage(tenantKey.TenantId);
        if (quotaManager.ExceedsQuota(currentUsage, tenantEvent))
        {
            LogQuotaViolation(tenantKey, currentUsage, tenantEvent);
            throw new QuotaExceededException($"Tenant {tenantKey.TenantId} exceeded quota");
        }
        
        // Get tenant-specific encryption key
        var tenantKey = keyManager.GetTenantKey(tenantKey.TenantId);
        
        // Process with tenant isolation
        var isolatedContext = CreateTenantIsolatedContext(tenantKey, tenantKey);
        var result = ProcessInIsolation(tenantEvent, isolatedContext);
        
        // Update resource usage
        quotaManager.UpdateUsage(tenantKey.TenantId, tenantEvent, result);
        
        output.Collect(result);
    }
    
    private TenantIsolatedContext CreateTenantIsolatedContext(TenantKey tenantKey, EncryptionKey encryptionKey)
    {
        return new TenantIsolatedContext
        {
            TenantId = tenantKey.TenantId,
            EncryptionKey = encryptionKey,
            ResourceLimits = quotaManager.GetLimits(tenantKey.TenantId),
            SecurityPolicy = securityContext.GetPolicy(tenantKey.TenantId),
            AuditConfiguration = GetAuditConfig(tenantKey.TenantId)
        };
    }
}
```

### Financial Compliance (SOX, Basel III)
```csharp
// SOX-compliant financial transaction processing
public class SOXCompliantTradeProcessor : KeyedProcessFunction<string, TradeEvent, ComplianceValidatedTrade>
{
    private readonly IComplianceRuleEngine complianceEngine;
    private readonly IImmutableAuditLog auditLog;
    private readonly IRiskCalculator riskCalculator;
    
    public override void ProcessElement(TradeEvent trade, Context context, ICollector<ComplianceValidatedTrade> output)
    {
        var tradeId = Guid.NewGuid();
        var auditContext = new SOXAuditContext
        {
            TradeId = tradeId,
            OriginalTradeEvent = trade,
            ProcessingTimestamp = DateTimeOffset.UtcNow,
            ProcessorVersion = GetType().Assembly.GetName().Version.ToString()
        };
        
        try
        {
            // SOX Section 302: Management certification of controls
            var controlValidation = ValidateInternalControls(trade);
            auditLog.LogControlValidation(auditContext, controlValidation);
            
            // SOX Section 404: Management assessment of internal controls
            var complianceChecks = complianceEngine.ValidateCompliance(trade, new[]
            {
                ComplianceRule.AntiMoneyLaundering,
                ComplianceRule.KnowYourCustomer,
                ComplianceRule.MarketManipulation,
                ComplianceRule.InsiderTrading,
                ComplianceRule.PositionLimits
            });
            
            if (complianceChecks.Any(c => c.Violation))
            {
                var violations = complianceChecks.Where(c => c.Violation).ToList();
                auditLog.LogComplianceViolation(auditContext, violations);
                
                // Block trade and alert compliance team
                TriggerComplianceAlert(trade, violations);
                return; // Don't process non-compliant trades
            }
            
            // Basel III: Calculate risk-weighted assets
            var riskMetrics = riskCalculator.CalculateBaselIIIRisk(trade);
            auditLog.LogRiskCalculation(auditContext, riskMetrics);
            
            // Create compliance-validated trade
            var validatedTrade = new ComplianceValidatedTrade
            {
                TradeId = tradeId,
                OriginalTrade = trade,
                ComplianceTimestamp = DateTimeOffset.UtcNow,
                ComplianceChecks = complianceChecks,
                RiskMetrics = riskMetrics,
                AuditTrail = auditContext.AuditTrail,
                DigitalSignature = CreateDigitalSignature(trade, auditContext)
            };
            
            // Immutable audit logging for SOX compliance
            auditLog.LogTradeProcessing(auditContext, validatedTrade);
            
            output.Collect(validatedTrade);
        }
        catch (Exception ex)
        {
            auditLog.LogProcessingError(auditContext, ex);
            throw;
        }
    }
    
    private string CreateDigitalSignature(TradeEvent trade, SOXAuditContext auditContext)
    {
        // Create tamper-proof digital signature for audit trail
        var signatureData = $"{trade.TradeId}|{trade.Amount}|{trade.Timestamp}|{auditContext.ProcessingTimestamp}";
        return cryptographicService.SignData(signatureData);
    }
}
```

## Hands-On Exercises

### Exercise 1: Healthcare Data Privacy (HIPAA)
Build a HIPAA-compliant patient data stream that:
- Implements safe harbor de-identification
- Manages patient consent and access controls
- Provides audit trails for all data access
- Handles breach notification requirements

### Exercise 2: Payment Card Industry (PCI-DSS)
Create a PCI-DSS compliant payment processing system that:
- Tokenizes credit card numbers
- Implements network segmentation
- Provides real-time fraud detection
- Maintains compliance monitoring and reporting

### Exercise 3: European Banking Authority (EBA) Compliance
Design a banking transaction system that:
- Implements Strong Customer Authentication (SCA)
- Provides real-time risk scoring
- Handles cross-border transaction reporting
- Maintains operational resilience requirements

## Security Monitoring and Incident Response

### Real-time Security Monitoring
```csharp
// Security monitoring with ML-based anomaly detection
public class SecurityMonitor : KeyedProcessFunction<string, SecurityEvent, SecurityAlert>
{
    private readonly IAnomalyDetector anomalyDetector;
    private readonly IThreatIntelligence threatIntel;
    private MapState<string, UserBehaviorProfile> behaviorProfiles;
    
    public override void ProcessElement(SecurityEvent securityEvent, Context context, ICollector<SecurityAlert> output)
    {
        var userId = context.GetCurrentKey();
        var currentProfile = behaviorProfiles.Get(userId) ?? new UserBehaviorProfile(userId);
        
        // Real-time anomaly detection
        var anomalyScore = anomalyDetector.CalculateAnomalyScore(securityEvent, currentProfile);
        
        // Check against threat intelligence
        var threatScore = threatIntel.GetThreatScore(securityEvent);
        
        // Combined risk assessment
        var riskScore = CalculateOverallRisk(anomalyScore, threatScore, securityEvent);
        
        if (riskScore > GetSecurityThreshold())
        {
            var alert = new SecurityAlert
            {
                AlertId = Guid.NewGuid(),
                UserId = userId,
                EventType = securityEvent.Type,
                RiskScore = riskScore,
                AnomalyScore = anomalyScore,
                ThreatScore = threatScore,
                DetectedAt = DateTimeOffset.UtcNow,
                Severity = DetermineSeverity(riskScore),
                RecommendedActions = GenerateRecommendations(securityEvent, riskScore)
            };
            
            output.Collect(alert);
            
            // Immediate response for high-risk events
            if (alert.Severity >= SecuritySeverity.High)
            {
                TriggerImmediateResponse(alert);
            }
        }
        
        // Update user behavior profile
        currentProfile.UpdateWithEvent(securityEvent);
        behaviorProfiles.Put(userId, currentProfile);
    }
}
```

## Data Anonymization Techniques

### Statistical Disclosure Control
```csharp
// Advanced anonymization techniques for privacy protection
public class DataAnonymizer
{
    public AnonymizedDataset ApplyKAnonymity(Dataset dataset, int k, string[] quasiIdentifiers)
    {
        // K-anonymity: Ensure each record is identical to at least k-1 others
        var equivalenceClasses = GroupByQuasiIdentifiers(dataset, quasiIdentifiers);
        
        foreach (var equivalenceClass in equivalenceClasses)
        {
            if (equivalenceClass.Count < k)
            {
                // Generalize or suppress records to achieve k-anonymity
                GeneralizeEquivalenceClass(equivalenceClass, quasiIdentifiers);
            }
        }
        
        return new AnonymizedDataset(equivalenceClasses.SelectMany(ec => ec).ToList());
    }
    
    public AnonymizedDataset ApplyDifferentialPrivacy(Dataset dataset, double epsilon)
    {
        // Add calibrated noise to protect individual privacy
        var noiseMechanism = new LaplaceMechanism(epsilon);
        
        var anonymizedRecords = dataset.Records.Select(record =>
        {
            var anonymizedRecord = record.Clone();
            
            // Add noise to numerical fields
            foreach (var field in record.NumericalFields)
            {
                var noise = noiseMechanism.AddNoise(field.Sensitivity);
                anonymizedRecord.SetField(field.Name, field.Value + noise);
            }
            
            return anonymizedRecord;
        }).ToList();
        
        return new AnonymizedDataset(anonymizedRecords);
    }
}
```

## Architecture Integration
- Configure encryption at rest and in transit
- Set up secure key management with hardware security modules
- Implement network security with VPCs and security groups
- Deploy compliance monitoring dashboards

## References
- [GDPR Official Text](https://gdpr-info.eu/)
- [NIST Cybersecurity Framework](https://www.nist.gov/cyberframework)
- [SOX Compliance Requirements](https://www.sec.gov/spotlight/sarbanes-oxley.htm)
- [PCI DSS Standards](https://www.pcisecuritystandards.org/)
- [Basel III Capital Framework](https://www.bis.org/bcbs/basel3.htm)

## Next Steps
Day 11 focuses on disaster recovery, business continuity, and multi-region deployment strategies for mission-critical streaming applications.
---

## 🗺️ Course Navigation
**[← Day 9: Performance Optimization & Scaling](../Day09-Performance-Optimization-Scaling/)** | **[Course Overview](../README.md)** | **[Next: Day 11 - Disaster Recovery & Multi-Region →](../Day11-Disaster-Recovery-Multi-Region/)**

**Course Progress**: Day 10 of 14 Complete ✅

## Running Exercises Manually

The exercises can be run manually outside of the integration tests. This requires starting the infrastructure and setting environment variables that are normally discovered automatically by the test framework.

### Step 1: Start Infrastructure

From the repository root, start the LocalTesting infrastructure in LearningCourse mode:

```bash
# Linux/macOS
cd LocalTesting
./run-learningcourse.sh

# Windows (PowerShell)
cd LocalTesting
$env:LEARNINGCOURSE="true"
dotnet run --project LocalTesting.FlinkSqlAppHost --configuration Release
```

This starts:
- Apache Flink cluster (JobManager + TaskManager + SQL Gateway)
- Apache Kafka with JMX metrics
- FlinkDotNet Gateway (port 8086)
- Temporal workflow server (optional, for Day06+)
- Redis (for state management)
- Prometheus (metrics collection)
- Grafana (metrics visualization)

Wait approximately 60 seconds for all containers to be ready.

### Step 2: Discover Service Endpoints

The infrastructure uses dynamic port allocation. You need to discover the actual ports assigned:

1. **Open Aspire Dashboard**: The AppHost will display a URL like `http://localhost:15000`
2. **Find Kafka Port**: Look for "kafka" service, note the host port (e.g., `localhost:32785`)
3. **Find Flink JobManager Port**: Look for "flink-jobmanager-jm-http" service, note the port (e.g., `localhost:32787`)

### Step 3: Set Environment Variables

Before running an exercise, set these environment variables:

```bash
# Linux/macOS
export KAFKA_BOOTSTRAP_SERVERS="localhost:XXXXX"  # Replace XXXXX with discovered Kafka host port
export KAFKA_FLINK_BOOTSTRAP_SERVERS="kafka:9093"  # Fixed container-to-container address
export FLINK_JOB_GATEWAY_URL="http://localhost:8086/"  # Fixed JobGateway port
export FLINK_JOBMANAGER_URL="http://localhost:YYYYY"  # Replace YYYYY with discovered Flink port

# Windows (PowerShell)
$env:KAFKA_BOOTSTRAP_SERVERS="localhost:XXXXX"
$env:KAFKA_FLINK_BOOTSTRAP_SERVERS="kafka:9093"
$env:FLINK_JOB_GATEWAY_URL="http://localhost:8086/"
$env:FLINK_JOBMANAGER_URL="http://localhost:YYYYY"
```

**Optional environment variables** (depending on the exercise):
```bash
# For Day06 Temporal exercises
export TEMPORAL_ENDPOINT="localhost:ZZZZZ"  # Replace with discovered Temporal port

# For exercises using Redis
export REDIS_ENDPOINT="localhost:WWWWW"  # Replace with discovered Redis port
```

### Step 4: Run Exercise

Navigate to the exercise directory and run:

```bash
cd Day01-Kafka-Flink-Data-Pipeline/Exercise-Solutions/Exercise1-StringCapitalize
dotnet run --configuration Release
```

### Environment Variable Reference

| Variable | Purpose | Example Value |
|----------|---------|---------------|
| `KAFKA_BOOTSTRAP_SERVERS` | Kafka address for producer/consumer on host | `localhost:32785` |
| `KAFKA_FLINK_BOOTSTRAP_SERVERS` | Kafka address for Flink jobs (container-to-container) | `kafka:9093` |
| `FLINK_JOB_GATEWAY_URL` | FlinkDotNet Gateway endpoint for job submission | `http://localhost:8086/` |
| `FLINK_JOBMANAGER_URL` | Flink JobManager REST API for health checks | `http://localhost:32787` |
| `TEMPORAL_ENDPOINT` | Temporal server endpoint (Day06+) | `localhost:32789` |
| `REDIS_ENDPOINT` | Redis endpoint for state management | `localhost:32783` |

### Why Dynamic Ports?

The test infrastructure uses .NET Aspire which assigns dynamic ports to avoid conflicts. This is why you need to discover ports from the Aspire Dashboard rather than using hardcoded values.

### Alternative: Use Integration Tests

For automated testing with automatic port discovery, use the integration test framework:

```bash
# Run all Day01 tests
dotnet test LearningCourse/IntegrationTests.sln --filter "FullyQualifiedName~Day01Tests"
```

The integration tests automatically:
- Start the infrastructure
- Discover service endpoints
- Set environment variables
- Run exercises
- Validate results
- Clean up resources

