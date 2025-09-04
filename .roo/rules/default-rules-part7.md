## Implementation Guidelines

### Work Item Creation Template
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

### Commit Message Format
```
[WI#] Brief description of change

Detailed description of what was changed and why.

Work Item: WI#
Phase: [Investigation|Design|Test Design|Development|Debugging|Testing]
```

## Tools and Integration
- Work Item tracking system integration required
- Automated phase transition notifications
- Commit hooks for WI reference validation
- Dashboard for WI lifecycle visibility

## Review and Compliance
- Weekly WI hygiene reviews
- Monthly process compliance audits
- Quarterly rule effectiveness assessment

## Architecture Documentation Maintenance (MANDATORY)

### Rule 11: System Architecture Documentation Updates (CRITICAL)
- **ALWAYS update system architecture documentation** when making architecture or system design changes
- **Required file updates for architecture changes**:
  - `docs/system-architecture-diagram.png` - Visual system architecture diagram
  - `docs/system-architecture.html` - Interactive HTML architecture documentation
  - `README.md` - System design section and architecture overview
- **Architecture change triggers** include:
  - New API endpoints or protocols (REST, GraphQL, gRPC)
  - Database schema changes or new database providers
  - New infrastructure components (caching, message queues, search engines)
  - Authentication/authorization mechanism changes
  - New external integrations or client interfaces
  - Performance optimization changes affecting system behavior
  - Security enhancements that modify data flow
  - Deployment or hosting configuration changes

> **Note**: This chunk covers Work Item implementation guidelines, templates, and architecture documentation. For Work Item enforcement rules, see Part 6. For TDD/BDD enforcement, see Part 8.