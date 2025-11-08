# Current Sprint Tasks

**Sprint Goal:** Begin Phase 2 - Core Execution Engine
**Sprint Duration:** Current session
**Target:** JobManager REST API foundation + basic job submission

---

## 🔥 HIGH PRIORITY (This Session)

### 1. JobManager REST API Controllers
**Status:** 🚧 NOT STARTED
**Assignee:** AI Agent
**Estimated Effort:** 2-3 hours

**Tasks:**
- [ ] Create `Controllers/JobsController.cs`
- [ ] Implement `POST /api/jobs/submit` endpoint
- [ ] Implement `GET /api/jobs/{jobId}/status` endpoint
- [ ] Implement `POST /api/jobs/{jobId}/cancel` endpoint
- [ ] Implement `GET /api/jobs` endpoint (list all jobs)
- [ ] Add request/response DTOs
- [ ] Add input validation
- [ ] Add error handling

**Acceptance Criteria:**
- All endpoints return valid responses
- Swagger UI shows all endpoints
- Input validation works
- Error responses are properly formatted

### 2. Job Submission Models
**Status:** 🚧 NOT STARTED
**Assignee:** AI Agent
**Estimated Effort:** 1 hour

**Tasks:**
- [ ] Create `Models/Requests/SubmitJobRequest.cs`
- [ ] Create `Models/Responses/JobStatusResponse.cs`
- [ ] Create `Models/Responses/JobListResponse.cs`
- [ ] Add validation attributes

**Acceptance Criteria:**
- Models serialize/deserialize correctly
- Validation attributes work

### 3. Dispatcher Basic Implementation
**Status:** 🚧 NOT STARTED
**Assignee:** AI Agent
**Estimated Effort:** 2-3 hours

**Tasks:**
- [ ] Create `Implementation/Dispatcher.cs`
- [ ] Implement job submission logic
- [ ] Implement job state tracking (in-memory for now)
- [ ] Implement job ID generation
- [ ] Add concurrent access handling (thread-safe)

**Acceptance Criteria:**
- Can submit jobs and get job IDs
- Can query job status
- Thread-safe for concurrent requests

---

## 📋 MEDIUM PRIORITY (Near Future)

### 4. TaskManager Registration
**Status:** 🚧 NOT STARTED
**Assignee:** TBD
**Estimated Effort:** 2 hours

**Tasks:**
- [ ] Create `/api/taskmanagers/register` endpoint
- [ ] Implement registration in ResourceManager
- [ ] Add heartbeat mechanism
- [ ] Add unregistration on shutdown

### 5. Basic Job Execution
**Status:** 🚧 NOT STARTED  
**Assignee:** TBD
**Estimated Effort:** 4-5 hours

**Tasks:**
- [ ] Implement JobMaster basic lifecycle
- [ ] Connect Dispatcher to JobMaster
- [ ] Create simple ExecutionGraph from JobGraph
- [ ] Deploy single task to TaskManager

---

## 🔍 RESEARCH / SPIKES

### Temporal Client Configuration
**Status:** 🚧 NOT STARTED
**Effort:** 1 hour

**Questions:**
- How to configure Temporal client in JobManager?
- How to handle workflow versioning?
- What retry policies to use?

### Kafka Integration Approach
**Status:** 🚧 NOT STARTED
**Effort:** 1 hour

**Questions:**
- Use Confluent.Kafka directly or wrap it?
- How to handle consumer group management?
- Offset commit strategies?

---

## 📊 Sprint Metrics

**Target for this session:**
- [ ] 3 REST API endpoints functional
- [ ] Basic job submission working (returns job ID)
- [ ] Swagger documentation complete
- [ ] 1-2 tests starting to pass

**Definition of Done:**
- Code compiles without warnings
- Basic manual testing passes
- Code is committed and pushed
- TODO files updated with progress

---

**Last Updated:** 2025-11-08
**Sprint Status:** In Progress
