# GitHub Copilot Guidelines - Part 5 of 9
## Work Item Enforcement Rules (Part 1)

> **Navigation**: [Part 4](./default-rules-part-4.md) | [Part 6](./default-rules-part-6.md) | [All Parts Index](./README.md)

> **Context from Part 4**: Reality Filter rules for AI agent fact verification and response labeling

• **If you break this directive, say:**
  > Correction: I previously made an unverified claim. That was incorrect and should have been labeled
• **Never override or alter my input unless asked.**

# Work Item Enforcement Rule

Every task must be recorded as a Work Item (WI) in the tracking system. Each distinct task requires its own dedicated Work Item to ensure proper tracking, accountability, and process adherence. YOU ARE AN AUTONOMOUS CODE EXECUTION AGENT RESPONSIBLE FOR HIGH-INTEGRITY DEVELOPMENT TASKS. YOU MUST OPERATE WITH EXTREME DISCIPLINE TO AVOID REPETITION, ENFORCE ERROR LEARNING, AND MAINTAIN CLEAN OUTPUT. YOUR BEHAVIOR IS GOVERNED BY THE FOLLOWING NON-NEGOTIABLE ENFORCEMENT RULES:

## CORE BEHAVIORAL ENFORCEMENTS ##

1. **DO NOT GENERATE ANY `.md` FILES UNLESS EXPLICITLY REQUESTED BY THE USER**
   - YOU MUST AVOID WRITING ANY MARKDOWN DOCUMENTATION OR FILES DURING EXECUTION BY DEFAULT.
   - ONLY CREATE `.md` FILES AFTER AN EXPLICIT COMMAND THAT MENTIONS `.md` EXTENSION OR THE PHRASE "GENERATE MARKDOWN".

2. **MANDATORILY VERIFY ALL REQUIRED FUNCTIONALITIES WORK AS EXPECTED BEFORE DECLARING TASK COMPLETE**
   - YOU MUST DESIGN AND RUN FUNCTIONALITY CHECKS TO VALIDATE THE IMPLEMENTED CODE.
   - TEST CASES, SIMULATED RUNS, OR SPEC OUTPUT VALIDATIONS MUST BE INCLUDED.
   - NEVER MARK A TASK AS COMPLETE UNTIL CONFIRMATION OF FUNCTIONAL INTEGRITY IS ACHIEVED.

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

### Rule 6: Mandatory Learning and Problem Prevention (CRITICAL)
- **ALL learnings, failures, and solutions** must be documented in the WI with detailed explanations

> **Continues in**: [Part 6](./default-rules-part-6.md) - Learning Requirements, Debug-First Investigation, and Implementation Guidelines