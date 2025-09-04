# GitHub Copilot Instructions - Modular System

> **This is the main index for GitHub Copilot instructions. All enforcement rules are organized into focused, manageable modules.**

## Critical Enforcement Rules (READ FIRST)
These rules must NEVER be missed or overlooked:

📄 **[AI Behavioral Enforcement](copilot-instructions/ai-behavioral-enforcement.md)** - Reality filter and core behavioral requirements
📄 **[Core Enforcement Rules](../.roo/rules/modules/core-enforcement-rules.md)** - 4 CRITICAL rules that prevent major violations

## Code Quality and Standards

📄 **[SOLID Principles](copilot-instructions/solid-principles.md)** - Single responsibility, open/closed, etc.
📄 **[.NET Best Practices](copilot-instructions/dotnet-best-practices.md)** - Naming, exception handling, async/await, security
📄 **[Testing Requirements](copilot-instructions/testing-requirements.md)** - Coverage requirements, TDD/BDD enforcement

## Work Management and Process

📄 **[Work Item Lifecycle](../.roo/rules/modules/work-item-lifecycle.md)** - WI phases, templates, tracking
📄 **[Learning and Archiving](../.roo/rules/modules/learning-and-archiving.md)** - Knowledge preservation and WI archiving

## Specialized Rules

📄 **[Architecture Documentation](../.roo/rules/modules/architecture-documentation.md)** - System design documentation requirements
📄 **[Build and Validation](../.roo/rules/modules/build-validation.md)** - .NET 9.0 environment and validation requirements

## Quick Reference for AI Agents

### When Overwhelmed by Instructions
Focus on these 4 CRITICAL rules first:
1. **Learn from previous work before starting** (Rule 6)
2. **Debug first, solutions second** (Rule 7)  
3. **Ask user for actions needed** (Rule 8)
4. **Archive old work and extract learnings** (Rule 10)

### Emergency Priorities
- ⚠️ **NEVER repeat solved problems** - Always check WIs/archived/ first
- ⚠️ **Debug before solutions** - Evidence-based problem solving required
- ⚠️ **Ask user for actions** - Never wait silently for user input
- ⚠️ **Archive WIs older than 2 weeks** - Preserve learnings, reduce clutter

## Module Organization Benefits

✅ **Focused Rules**: Each module < 300 lines, easy to process
✅ **Quick Discovery**: Critical rules prominently positioned  
✅ **Maintainable**: Updates isolated to specific modules
✅ **Searchable**: Topic-based organization for faster reference

## New Archiving Rule for Instructions

### Rule: Automatic WI Archiving for Files Older Than 2 Weeks

**Requirement**: All Work Items older than 2 weeks from completion must be automatically reviewed, summarized, and archived by AI agents.

**Process**:
1. **Identify Completed WIs**: Scan WIs/ folder for items with "Done", "Completed", or "Closed" status
2. **Check Age**: Items completed > 2 weeks ago are candidates for archiving
3. **Extract Learnings**: Create summary with key patterns, solutions, and lessons learned
4. **Archive Structure**: Move to `WIs/archived/YYYY-MM/completed/` with summary format
5. **Learning Extraction**: Extract reusable patterns to `WIs/archived/YYYY-MM/learnings/`
6. **Clean Workspace**: Remove original WI files to reduce active workspace clutter

**Benefits**:
- **Preserved Knowledge**: All critical learnings retained in searchable format
- **Reduced Clutter**: Active WIs folder contains only current work
- **Better Performance**: Shorter instruction files improve AI processing
- **Institutional Memory**: Prevents repeating solved problems

**Implementation**: This rule is enforced in the [Learning and Archiving](../.roo/rules/modules/learning-and-archiving.md) module.

---

*This modular system ensures all critical enforcement rules remain discoverable while maintaining manageable file sizes for optimal AI agent processing.*