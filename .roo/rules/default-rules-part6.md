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

### Rule 6: Mandatory Learning and Problem Prevention (CRITICAL)
- **ALL learnings, failures, and solutions** must be documented in the WI with detailed explanations
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

> **Note**: This chunk covers Work Item enforcement rules 1-10 and violation consequences. For implementation guidelines and templates, see Part 7. For Work Item lifecycle, see Part 5.