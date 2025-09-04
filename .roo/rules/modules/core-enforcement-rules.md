# Core Enforcement Rules - CRITICAL RULES FOR AI AGENTS

> **⚠️ CRITICAL**: These are the most important enforcement rules that must NEVER be missed or overlooked by AI agents. Violations of these rules result in immediate work stoppage and corrective action.

## MANDATORY Learning and Problem Prevention (Rule 6)

### CRITICAL Requirements
- **ALL learnings, failures, and solutions** must be documented in WIs with detailed explanations
- **BEFORE starting any new WI**, you MUST review existing WI files and AI-Learning files to learn from previous work
- **Search for similar problems** in the WIs folder or AI-Learning folder and apply lessons learned to avoid repetition
- Document in each new WI: "Lessons Applied from Previous WIs" section referencing specific WI files
- Include specific actions taken to prevent repeating known problems
- **Failure to learn from previous WIs and AI-Learning files and repeat solved problems is a MAJOR violation**
- Each WI must end with actionable lessons for future similar work

## MANDATORY Debug-First Investigation (Rule 7)

### CRITICAL Requirements
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

## MANDATORY User Action Prompting (Rule 8)

### CRITICAL Requirements
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

## CRITICAL Archiving & Learning Enforcement (Rule 10)

### CRITICAL Requirements
- **ALL Work Items older than 2 weeks must be reviewed, learned from, and archived**
- **Learnings from old WIs must be extracted and written to WIs/archived/ folder, grouped by topic**
- This ensures the agent and developers do not repeat mistakes and continuously improve
- AI agent should remove outdated WIs and enforce learning extraction
- **Failure to archive and write learnings after 2 weeks is a MAJOR violation**

## MAJOR VIOLATION Consequences

### What Triggers Immediate Work Stoppage
- **Repeating known problems without learning from previous WIs** → Complete task restart with mandatory review
- **Insufficient learning documentation** → Work rejection until proper lessons are documented
- **Proceeding to solutions without proper debugging** → Work rejection and mandatory investigation restart
- **Failure to prompt for user actions** → Task confusion and delayed completion
- **Not archiving WIs and extracting learnings within 2 weeks** → Knowledge loss and repeated mistakes

### Recovery Actions Required
1. **Stop all current work immediately**
2. **Review all related WIs and learnings files**
3. **Document what was learned and how to prevent repetition**
4. **Restart work with proper enforcement**
5. **Update WI with lessons learned section**

## Emergency Rule Reference

**When overwhelmed by instructions**: Focus on these 4 CRITICAL rules first:
1. **Learn from previous work before starting** (Rule 6)
2. **Debug first, solutions second** (Rule 7)  
3. **Ask user for actions needed** (Rule 8)
4. **Archive old work and extract learnings** (Rule 10)

**All other rules are important but these 4 are CRITICAL for success.**