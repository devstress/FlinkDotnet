# Work Item Lifecycle Management

## Work Item Lifecycle

All work items must follow this mandatory progression:

### 1. Investigation Phase
- **Requirements**: Research problem scope, gather requirements, analyze dependencies
- **Deliverables**: Problem statement, scope definition, dependency analysis
- **Status**: WI marked as "Investigation"

### 2. Design Phase
- **Requirements**: Create technical design, architecture decisions, interface specifications
- **Deliverables**: Design document, API contracts, system architecture
- **Status**: WI marked as "Design"

### 3. Test-Driven Development (TDD/BDD) Phase
- **Requirements**: Write failing tests first, define behavior specifications
- **Deliverables**: Unit tests, integration tests, behavior specifications
- **Status**: WI marked as "Test Design"

### 4. Coding Phase
- **Requirements**: Implement solution to make tests pass
- **Deliverables**: Production code, code reviews completed
- **Status**: WI marked as "In Development"

### 5. Debug Phase
- **Requirements**: Fix issues, optimize performance, handle edge cases
- **Deliverables**: Bug fixes, performance improvements, edge case handling
- **Status**: WI marked as "Debugging"

### 6. Testing Validation Phase
- **Requirements**: All tests must pass (unit, integration, system, acceptance)
- **Deliverables**: Test execution reports, quality gates passed
- **Status**: WI marked as "Testing"

### 7. Commit Phase
- **Requirements**: Code review approved, all checks passed, ready for deployment
- **Deliverables**: Merged code, deployment artifacts
- **Status**: WI marked as "Done"

### 8. Owner Acceptance Phase
- **Requirements**: Present completed work to task owner for final approval
- **Deliverables**: Owner confirmation of satisfaction with deliverables
- **Status**: WI marked as "Pending Owner Review"

### 9. Work Item Closure Phase
- **Requirements**: Owner approval received, all acceptance criteria met
- **Deliverables**: Work Item deletion/archival, cleanup of related artifacts
- **Status**: WI marked as "Closed" then deleted

## Enforcement Rules

### Rule 1: One Functionality, One Work Item (MANDATORY)
- Each distinct functionality requires exactly ONE Work Item document
- All phases, iterations, and decisions must be documented within the same WI file
- The entire workflow from Investigation → Closure must be visible in one document
- NO separate WIs for different phases of the same functionality
- Sub-tasks may be tracked within sections but must remain in the same WI file
- This enables complete learning and traceability for future reference

### Rule 2: WIs Folder Structure (MANDATORY)
- All Work Items must be created as files in the `WIs/` folder
- File naming convention: `WI[#]_[brief-description].md`
- Example: `WIs/WI1_stress-test-fix.md`
- WI files must contain all phase documentation and progress tracking

### Rule 3: Single Document Lifecycle (MANDATORY)
- Work Items cannot skip phases within the same document
- Each phase must be completed before advancing to the next
- Phase completion requires explicit approval/verification
- ALL phase documentation, iterations, failures, and learnings must be recorded in the SAME WI file
- Include WHY decisions were made, what was tried, what failed, and lessons learned
- Never run into the same solutions and problems twice from the history of the WI.
- Document iterations and refinements within the same WI for complete context

### Rule 4: Traceability Requirements
- All code commits must reference the associated Work Item ID
- All design decisions must be linked to their Work Item
- All test cases must be traceable to their Work Item
- WI file path must be referenced in commit messages

### Rule 5: Status Accuracy
- Work Item status must reflect actual progress
- Status updates are mandatory at phase transitions
- Stale Work Items (>5 days without update) trigger automatic review
- WI file must be updated with each status change

## Work Item Creation Template

```markdown
# WI[#]: [Functionality Name]

**File**: `WIs/WI[#]_[brief-description].md`
**Title**: [Component] Brief description  
**Description**: Clear problem statement and acceptance criteria
**Priority**: [High|Medium|Low]
**Component**: [System component]
**Type**: [Investigation|Feature|Bug Fix|Enhancement]
**Assignee**: [Developer responsible]
**Created**: [Date]
**Status**: [Current phase]

## Lessons Applied from Previous WIs
### Previous WI References
- [List specific WI files reviewed]
### Lessons Applied  
- [Specific actions taken to avoid known problems]
### Problems Prevented
- [Specific issues avoided based on previous learnings]

## Phase 1: Investigation
### Requirements
### Debug Information (MANDATORY - Update this section for every investigation)
- **Error Messages**: [Exact error messages and stack traces]
- **Log Locations**: [Specific log files and key excerpts]
- **System State**: [Configuration, environment, running processes]
- **Reproduction Steps**: [Exact steps to reproduce the issue]
- **Evidence**: [Screenshots, command outputs, file contents]
### Findings
### Lessons Learned

## Phase 2: Design  
### Requirements
### Architecture Decisions
### Why This Approach
### Alternatives Considered

## Phase 3: TDD/BDD
### Test Specifications
### Behavior Definitions

## Phase 4: Implementation
### Code Changes
### Challenges Encountered
### Solutions Applied

## Phase 5: Testing & Validation
### Test Results
### Performance Metrics

## Phase 6: Owner Acceptance
### Demonstration
### Owner Feedback
### Final Approval

## Lessons Learned & Future Reference (MANDATORY)
### What Worked Well
- [Document successful approaches for reuse]
### What Could Be Improved  
- [Document specific improvements for next time]
### Key Insights for Similar Tasks
- [Actionable insights for similar future work]
### Specific Problems to Avoid in Future
- [Detailed list of problems and how to prevent them]
### Reference for Future WIs
- [What future developers should know before starting similar work]
```

## Commit Message Format
```
[WI#] Brief description of change

Detailed description of what was changed and why.

Work Item: WI#
Phase: [Investigation|Design|Test Design|Development|Debugging|Testing]
```