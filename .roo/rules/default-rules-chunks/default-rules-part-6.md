# GitHub Copilot Guidelines - Part 6 of 9
## Work Item Enforcement Rules (Part 2)

> **Navigation**: [Part 5](./default-rules-part-5.md) | [Part 7](./default-rules-part-7.md) | [All Parts Index](./README.md)

> **Context from Part 5**: Rule 6 introduction - Mandatory Learning and Problem Prevention requirements

- **BEFORE starting any new WI**, you MUST review existing WI files and existing AI-Learning files to learn from previous work
- **Search for similar problems** in the WIs folder or AI-Learning folder and apply lessons learned to avoid repetition
- Document in each new WI: "Lessons Applied from Previous WIs" section referencing specific WI files
- Include specific actions taken to prevent repeating known problems
- **Failure to learn from previous WIs and AI-Learning files and repeat solved problems is a MAJOR violation**
- Each WI must end with actionable lessons for future similar work

### Rule 7: Mandatory Debug-First Investigation (CRITICAL)
- **ALWAYS debug first** to find the root cause during the Investigation phase
- **Cannot proceed to solutions without proper debugging** and evidence collection
- **Must document debug findings** in the WI for future learning and reference
- **Debug section must be updated** for every investigation to save space and maintain consistency
- **Debug information required**:
  - Error messages and stack traces
  - Log file locations and key excerpts
  - System state at time of failure
  - Environment configuration details
  - Reproduction steps and conditions
- **Purpose**: Evidence-based problem solving and knowledge preservation for future debugging
- **Failure to debug first before proposing solutions is a MAJOR violation**

### Rule 8: User Action Prompting (MANDATORY)
- **NEVER wait silently** for user actions without explicit prompts
- **ALWAYS ask user directly** when their action is required to proceed
- **Script Design Philosophy**: Scripts should work standalone first, then fallback to manual instructions
- **Examples of required prompts**:
  - "Please restart Docker Desktop now and let me know when it's ready"
  - "Please run these commands manually: [commands]"
  - "Please check [status] and confirm when complete"
- **Password Prompting**: NEVER attempt interactive password prompting - use manual fallback instead
- **Clear instructions**: Provide specific steps the user needs to take
- **Explicit confirmation**: Ask user to confirm completion before proceeding
- **Purpose**: Prevent confusion about what the system is waiting for
- **Failure to prompt for user actions is a MAJOR violation**

### Rule 9: Prohibition of IMPLEMENTATION_SUMMARY.md Files (MANDATORY)
- **NEVER create IMPLEMENTATION_SUMMARY.md files** - this violates Work Item enforcement
- **ALL work must be tracked through WIs folder** using proper Work Item files (`WIs/WI[#]_[description].md`)
- **Cleanup workflow automatically removes WIs folders** after merge to maintain repository cleanliness
- **Implementation summaries belong in Work Items**, not as standalone files
- **Documentation belongs in the appropriate places**:
  - Technical details: In WI files during development
  - User documentation: In README.md or docs/ folder
  - API documentation: Inline code comments and generated docs
- **Purpose**: Enforce proper work tracking and prevent documentation pollution
- **Failure to follow this rule is a MAJOR violation** requiring immediate file removal and proper WI creation

### Rule 10: Automatic Archiving & Learning Enforcement (CRITICAL)
- ALL Work Items older than 1 month must be reviewed, learned from, and archived
- Learnings from these old WIs must be extracted and written to the AI-Learning/ folder, grouped by topic
- This ensures the agent and developers do not repeat mistakes and continuously improve
- AI agent should remove outdated WIs and enforce learning extraction
- Failure to archive and write learnings after 1 month is a MAJOR violation

## Violations and Consequences

### Minor Violations
- Missing WI references in commits → Automatic rejection
- Incorrect status updates → Warning and mandatory correction

### Major Violations  
- Skipping phases → Work rejection and rework requirement
- Multiple tasks in single WI → WI split mandate
- Untracked work → Immediate work stoppage until WI created
- **Repeating known problems without learning from previous WIs → Complete task restart with mandatory review**
- **Insufficient learning documentation → Work rejection until proper lessons are documented**
- **Failure to update CLAUDE.md when learning new project knowledge → Work rejection and mandatory documentation**
- **Proceeding to solutions without proper debugging → Work rejection and mandatory investigation restart**
- **Creating IMPLEMENTATION_SUMMARY.md files → Immediate file removal and WI creation mandate**

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

> **Continues in**: [Part 7](./default-rules-part-7.md) - Commit Messages, Tools Integration, and Architecture Documentation