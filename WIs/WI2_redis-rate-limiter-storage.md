# WI2: Redis Rate Limiter Storage Implementation

**File**: `WIs/WI2_redis-rate-limiter-storage.md`
**Title**: [RateLimiter] Replace Kafka storage with Redis for fault tolerance and zero message loss
**Description**: Implement Redis-based storage for rate limiter with AOF configuration for fault tolerance and zero message loss after disk disruptions, backed by professional articles and scholars
**Priority**: High
**Component**: FlinkDotNet.Flink.JobBuilder.Backpressure
**Type**: Enhancement  
**Assignee**: AI Agent
**Created**: 2025-01-30
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- WI1: Credit control implementation with test integration
### Lessons Applied  
- Ensure all implementations are backed by real configuration and tests
- Reference actual working code rather than conceptual implementations
- Include professional backing with scholarly references
- Integrate with existing test infrastructure
### Problems Prevented
- Avoid conceptual implementations without real backing
- Ensure Redis configuration is production-ready with fault tolerance
- Document Redis persistence options thoroughly

## Phase 1: Investigation
### Requirements
- Replace current Kafka-based rate limiter storage with Redis
- Implement Redis AOF mode for fault tolerance
- Provide option for memory-only Redis with recalculation from source of truth
- Zero message loss after disk disruptions
- Professional articles and scholarly backing

### Debug Information (MANDATORY - Update this section for every investigation)
- **Current State**: Kafka storage implementation exists in KafkaRateLimiterStateStorage.cs
- **Redis Dependencies**: Redis is available in sample projects via Aspire.Hosting.Redis
- **Interface**: IRateLimiterStateStorage defines the contract for storage implementations
- **Factory Pattern**: RateLimiterFactory needs Redis methods added
- **Need to Add**: StackExchange.Redis dependency to main Flink.JobBuilder project

### Findings
Current implementation uses Kafka for distributed rate limiter state storage. User request is to switch to Redis for:
1. Better fault tolerance with AOF mode
2. Zero message loss after disk disruptions  
3. Option for memory-only with recalculation fallback
4. Professional backing with scholarly references

### Lessons Learned
Redis can provide better latency and fault tolerance characteristics for rate limiter storage compared to Kafka for this specific use case.

## Phase 2: Design  
### Requirements
Create Redis storage implementation following the same interface pattern
### Architecture Decisions
- Implement RedisRateLimiterStateStorage following IRateLimiterStateStorage interface
- Add Redis configuration options for AOF mode
- Provide memory-only Redis option with fallback to source recalculation
- Update RateLimiterFactory to support Redis storage creation
- Add Redis dependency to Flink.JobBuilder project
### Why This Approach
Redis provides sub-millisecond latency and excellent persistence options with AOF
### Alternatives Considered
Keep Kafka (rejected per user requirement), implement custom storage (unnecessary complexity)

## Phase 3: TDD/BDD
### Test Specifications
- Unit tests for RedisRateLimiterStateStorage
- Integration tests with Redis AOF mode
- Fault tolerance tests for disk disruption scenarios
### Behavior Definitions
- Redis storage should maintain rate limiter state across restarts
- AOF mode should prevent data loss during disk disruptions
- Fallback to recalculation should work when Redis data is lost

## Phase 4: Implementation
### Code Changes
- [ ] Add StackExchange.Redis dependency to Flink.JobBuilder.csproj
- [ ] Create RedisRateLimiterStateStorage.cs implementing IRateLimiterStateStorage
- [ ] Create RedisConfig.cs for Redis configuration options
- [ ] Update RateLimiterFactory.cs to include Redis storage methods
- [ ] Update documentation to include Redis configuration examples
### Challenges Encountered
TBD
### Solutions Applied
TBD

## Phase 5: Testing & Validation
### Test Results
TBD
### Performance Metrics
TBD

## Phase 6: Owner Acceptance
### Demonstration
TBD
### Owner Feedback
TBD
### Final Approval
TBD

## Lessons Learned & Future Reference (MANDATORY)
### What Worked Well
TBD
### What Could Be Improved  
TBD
### Key Insights for Similar Tasks
TBD
### Specific Problems to Avoid in Future
TBD
### Reference for Future WIs
TBD