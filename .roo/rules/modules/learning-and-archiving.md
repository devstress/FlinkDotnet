# Learning and Archiving Enforcement

## Automatic Archiving & Learning Enforcement (CRITICAL - Rule 10)

### Core Requirements
- **ALL Work Items older than 2 weeks must be reviewed, learned from, and archived**
- **Learnings from old WIs must be extracted and written to WIs/archived/ folder, grouped by topic**
- This ensures the agent and developers do not repeat mistakes and continuously improve
- AI agent should remove outdated WIs and enforce learning extraction
- **Failure to archive and write learnings after 2 weeks is a MAJOR violation**

### Archive Structure
```
WIs/archived/
├── YYYY-MM/
│   ├── completed/
│   │   ├── WI#_brief-description_summary.md
│   │   └── WI#_another-task_summary.md
│   └── learnings/
│       ├── topic-specific-patterns.md
│       └── technology-specific-insights.md
└── index.md
```

### WI Summary Template
```markdown
# WI#: Task Name - ARCHIVED SUMMARY

**Original File**: WI#_original-filename.md  
**Completion Date**: YYYY-MM-DD  
**Status**: Completed/Done  
**Type**: Bug Fix/Feature/Enhancement  

## Problem Solved
[Brief description of what was accomplished]

## Key Learnings Applied
[Lessons from previous WIs that were applied]

## Solutions Implemented
[Technical solutions and approaches used]

## Critical Patterns for Reuse
[Reusable patterns and approaches]

## Problems Avoided
[Issues prevented through proper approach]

## Future Reference
[Actionable guidance for similar future work]

**Archive Reason**: [Why this WI was archived and what value it provides]
```

### Learning Extraction Template
```markdown
# [Topic] Patterns - Extracted Learnings

**Source WIs**: [List of source WI references]  
**Pattern Category**: [Technology/Process/Domain specific]  
**Last Updated**: YYYY-MM-DD  

## Core Principles
[Fundamental principles discovered]

## Reusable Patterns
[Specific patterns that can be applied elsewhere]

## Anti-Patterns to Avoid
[What NOT to do based on learnings]

## Implementation Guidelines
[Step-by-step guidance for applying patterns]

## Quality Gates
[Checklist for ensuring pattern is properly applied]
```

### Archiving Process Workflow

1. **Identification Phase**
   - Scan WIs/ folder for completed items older than 2 weeks
   - Check status indicators: "Done", "Completed", "Closed"
   - Verify all phases are complete with lessons learned

2. **Learning Extraction Phase**
   - Extract key technical solutions and approaches
   - Identify reusable patterns and anti-patterns  
   - Group related learnings by topic/technology
   - Document critical insights for future reference

3. **Summarization Phase**
   - Create concise summary using template
   - Preserve essential knowledge while removing clutter
   - Ensure searchable format with clear categorization
   - Link to related archived items where relevant

4. **Archive Organization Phase**
   - Move to appropriate WIs/archived/YYYY-MM/ structure
   - Update archive index with new entries
   - Create/update learning pattern files
   - Remove original WI from active workspace

5. **Validation Phase**
   - Verify all critical knowledge is preserved
   - Ensure archive structure is maintained
   - Test searchability of archived content
   - Confirm no broken references remain

### Search and Discovery

**Archive Search Commands**:
```bash
# Search for specific topics across archives
find WIs/archived -name "*.md" -exec grep -l "search_term" {} \;

# List all archived WIs by date
ls -la WIs/archived/*/completed/

# Search learning patterns by category
grep -r "pattern_name" WIs/archived/*/learnings/
```

**Discovery Strategies**:
- Check WIs/archived/index.md for overview
- Search by technology: "aspire", "documentation", "testing"
- Search by problem type: "integration", "configuration", "deployment"
- Review learning patterns before starting similar work

### Maintenance and Hygiene

**Weekly Archive Review**:
- Identify WIs ready for archiving (completed + 2 weeks old)
- Update learning pattern files with new insights
- Verify archive structure integrity
- Clean up any orphaned or duplicate content

**Monthly Archive Optimization**:
- Consolidate related learning patterns
- Update index with new categories
- Review and enhance search keywords
- Validate all archived content is still relevant

### Enforcement Actions

**For New WIs**: MUST review archived learnings first
**For Completed WIs**: MUST extract learnings before closing
**For Old WIs**: MUST archive within 2 weeks of completion
**For Repeated Problems**: MAJOR violation if archived solution exists

### Archive Benefits

✅ **Institutional Memory**: Prevents solving same problems repeatedly
✅ **Knowledge Preservation**: Critical insights never lost
✅ **Improved Performance**: Smaller active workspace for AI processing
✅ **Better Decision Making**: Historical context for new decisions
✅ **Team Learning**: Shared knowledge base for all developers
✅ **Quality Improvement**: Documented patterns improve consistency