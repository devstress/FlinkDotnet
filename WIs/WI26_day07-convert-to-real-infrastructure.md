# WI26: Day07 Advanced Windows & Joins - Convert to Real Infrastructure

**File**: `WIs/WI26_day07-convert-to-real-infrastructure.md`
**Title**: [Day07] Convert Exercise71-74 advanced windowing and joins to real Kafka/Flink infrastructure
**Description**: Convert Day07 advanced windows & joins exercises from simulation to real LocalTesting Kafka/Flink with time-based operations
**Priority**: High
**Component**: LearningCourse Day07
**Type**: Infrastructure Implementation
**Assignee**: AI Agent
**Created**: 2025-01-14
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- WI24: Day09 Exercise91-94 real infrastructure ✅ Exactly-once semantics pattern
- WI23: Day08 Exercise71-74 real infrastructure ✅ High-performance load testing
- WI25: Master tracking for remaining 42 exercises

### Lessons Applied
- Follow Exercise71 (Day08) and Exercise91 (Day09) proven patterns
- Real Kafka producers/consumers with environment variable service discovery
- Real Flink job submission with IJobClient lifecycle management
- Infrastructure health checks before execution
- Time-based windowing requires proper event-time handling
- Stream joins need careful state management

### Problems Prevented
- No simulation of time windows (use real Flink windowing)
- No hardcoded addresses (environment variables)
- No missing cleanup (IJobClient.CancelAsync)
- Proper watermark configuration for late data handling
- Real Kafka timestamps for event-time processing

## Phase 1: Investigation
### Requirements
Convert Day07 Exercise71-74 from simulation to real Kafka/Flink infrastructure with advanced windowing and stream joins

**User Requirement**: "no simulation, only real LocalTesting connections"

### Current State Analysis
Checking Day07 exercise structure to understand current implementation...

### Debug Information (MANDATORY)
**Exercise Topics**:
- Exercise71: Time Window Aggregations (tumbling, sliding, session windows)
- Exercise72: Session Windows (gap-based window detection)
- Exercise73: Stream-Stream Joins (time-bounded joins)
- Exercise74: Interval Joins (temporal relationship joins)

**Key Requirements for Real Implementation**:
1. Real Kafka topics with proper timestamps
2. Flink event-time processing with watermarks
3. Window operators (TumblingWindow, SlidingWindow, SessionWindow)
4. Join operators (Window Join, Interval Join)
5. Late data handling with allowed lateness
6. State management for windowed operations

### Findings
Day07 focuses on time-based operations - requires real event-time semantics, not simulation

## Phase 2: Design
### Architecture Decisions

**Exercise71 - Time Window Aggregations**:
- Real Kafka topic with timestamped events
- Flink job with tumbling/sliding windows
- Watermark generation for event-time
- Real aggregation with state
- Output to verification topic

**Exercise72 - Session Windows**:
- Real Kafka topic with user activity events
- Session window with configurable gap (e.g., 5 minutes)
- Session aggregation (count events per session)
- Real Flink session window operator
- Output session summaries

**Exercise73 - Stream-Stream Joins**:
- Two real Kafka topics (e.g., orders + payments)
- Time-bounded window join
- Join condition on key + time window
- Real Flink window join operator
- Output joined results

**Exercise74 - Interval Joins**:
- Two real Kafka topics with temporal relationships
- Interval join with before/after bounds
- Real Flink interval join operator
- Complex temporal predicates
- Output matched events

### Why This Approach
- Real windowing validates actual Flink time semantics
- Real joins test state management and watermarking
- Production-ready patterns for time-based processing
- Critical for event-time processing understanding

### Alternatives Considered
- In-memory simulation (❌ Rejected - violates user requirement)
- Processing-time only (❌ Rejected - doesn't teach event-time)

## Phase 3: TDD/BDD
### Test Specifications
Each exercise must:
- Complete within 3 minutes
- Use real Kafka with proper timestamps
- Submit real Flink job with windowing
- Generate watermarks correctly
- Handle late data appropriately
- Output completion markers
- Pass integration test validation

### Behavior Definitions
**Given** LocalTesting infrastructure with Kafka/Flink
**When** exercise submits windowed job with real events
**Then** windows fire correctly, joins match, results verified

## Phase 4: Implementation
### Code Changes
Starting with Exercise71 conversion using Day08/Day09 pattern as template...

**To be updated as conversion progresses**

### Challenges Encountered
**To be documented during implementation**

### Solutions Applied
**To be documented during implementation**

## Phase 5: Testing & Validation
### Test Results
**To be updated after conversions complete**

### Performance Metrics
**To be tracked during testing**

## Phase 6: Owner Acceptance
### Demonstration
**To be completed after all 4 exercises converted**

### Owner Feedback
User requirement: "no simulation, only real LocalTesting connections" ✅

### Final Approval
**To be obtained after validation**

## Lessons Learned & Future Reference (MANDATORY)

### What Worked Well
**To be updated after completion**

### What Could Be Improved
**To be updated after completion**

### Key Insights for Similar Tasks
**To be updated after completion**

### Specific Problems to Avoid in Future
**To be updated after completion**

### Reference for Future WIs
**Gold Standard Patterns**:
- Day08 Exercise71: High-performance pattern
- Day09 Exercise91: Exactly-once pattern
- **Day07 Exercise71** (this WI): Time windowing pattern (to be established)

## Current Status
**Phase**: Investigation → Design → Implementation (starting)
**Next Action**: Check Day07 exercise structure, then convert Exercise71 first