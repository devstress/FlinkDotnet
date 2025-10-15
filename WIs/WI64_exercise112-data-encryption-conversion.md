# WI64: Exercise112 Data Encryption Conversion

**File**: `WIs/WI64_exercise112-data-encryption-conversion.md`
**Title**: Convert Exercise112 to Real Kafka/Flink Infrastructure - Field-Level Data Encryption
**Description**: Convert Exercise112 from template to production-ready field-level encryption implementation using real Kafka infrastructure and AES-256 encryption
**Priority**: High
**Component**: LearningCourse - Day11 Security & Compliance
**Type**: Feature - Real Infrastructure Conversion
**Assignee**: AI Agent
**Created**: 2025-01-14
**Status**: Implementation

## Lessons Applied from Previous WIs

### Previous WI References
- WI63: Exercise111 authentication conversion (JWT patterns)
- WI44-47: Day08 stress testing (real Kafka patterns)
- WI48-51: Day09 exactly-once semantics

### Lessons Applied
- Environment variable service discovery for Kafka
- Console application pattern with completion markers
- Real Kafka producer/consumer setup
- Proper infrastructure health checks
- Clear success indicators for test validation

### Problems Prevented
- No web service pattern (use console app)
- No hardcoded localhost addresses
- No simulation - use real encryption libraries
- Proper resource cleanup and disposal

## Phase 1: Investigation

### Requirements
Convert Exercise112 to demonstrate enterprise-grade field-level encryption using real Kafka infrastructure:

**Core Functionality**:
1. AES-256-GCM encryption for sensitive fields
2. Field-selective encryption (encrypt only PII fields)
3. Key rotation simulation
4. Encrypted data transmission via Kafka
5. Decryption with proper key management

**Architecture**:
- Data Generator → Field Encryption → Kafka → Decryption → Validation
- Sensitive fields: SSN, CreditCard, Email
- Non-sensitive fields: UserId, Timestamp (unencrypted for processing)

### Implementation Pattern
Based on Exercise111 success and production encryption patterns:

```csharp
// AES-256-GCM encryption
using var aes = Aes.Create();
aes.KeySize = 256;
aes.Mode = CipherMode.GCM;
var encrypted = aes.CreateEncryptor().TransformFinalBlock(plaintext, 0, plaintext.Length);

// Field-selective encryption
public class SensitiveCustomerData
{
    public string UserId { get; set; }  // Unencrypted for routing
    public string EncryptedSSN { get; set; }  // Encrypted
    public string EncryptedCreditCard { get; set; }  // Encrypted
    public string EncryptedEmail { get; set; }  // Encrypted
    public DateTimeOffset Timestamp { get; set; }  // Unencrypted for ordering
}
```

## Phase 2: Design

### Architecture Decisions
**Encryption Strategy**:
- AES-256-GCM (authenticated encryption)
- Per-field encryption (not whole-message)
- Base64 encoding for Kafka transmission
- Key rotation every N messages

**Why This Approach**:
- Field-level encryption allows processing on unencrypted fields
- GCM provides both confidentiality and authenticity
- Base64 encoding ensures safe Kafka transmission
- Key rotation demonstrates production key management

## Phase 3: Implementation

Implementation will include:
1. Field-level encryption service
2. Real Kafka producer for encrypted data
3. Real Kafka consumer with decryption
4. Key rotation demonstration
5. Encryption performance metrics

(Implementation in progress)