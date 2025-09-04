# .roo Rules - Modular System

> **This is the main index for .roo rules. All enforcement rules are organized into focused, manageable modules.**

## Critical Enforcement Rules (HIGHEST PRIORITY)
⚠️ **These rules must NEVER be missed or overlooked:**

📄 **[Core Enforcement Rules](modules/core-enforcement-rules.md)** - 4 CRITICAL rules that prevent major violations:
- Rule 6: Learning and Problem Prevention (CRITICAL)
- Rule 7: Debug-First Investigation (CRITICAL)  
- Rule 8: User Action Prompting (MANDATORY)
- Rule 10: Automatic Archiving & Learning Enforcement (CRITICAL)

## Work Management and Process Rules

📄 **[Work Item Lifecycle](modules/work-item-lifecycle.md)** - WI phases, templates, tracking, enforcement
📄 **[Learning and Archiving](modules/learning-and-archiving.md)** - Knowledge preservation and WI archiving system

## System and Development Rules

📄 **[Architecture Documentation](modules/architecture-documentation.md)** - System design documentation requirements
📄 **[Build and Validation](modules/build-validation.md)** - .NET 9.0 environment and validation requirements

## GitHub Copilot Code Quality Rules

📄 **[SOLID Principles](../../.github/copilot-instructions/solid-principles.md)** - Single responsibility, open/closed, etc.
📄 **[.NET Best Practices](../../.github/copilot-instructions/dotnet-best-practices.md)** - Naming, exception handling, async/await
📄 **[Testing Requirements](../../.github/copilot-instructions/testing-requirements.md)** - Coverage requirements, TDD/BDD
📄 **[AI Behavioral Enforcement](../../.github/copilot-instructions/ai-behavioral-enforcement.md)** - Reality filter rules

## Emergency Quick Reference

### When Overwhelmed by Rules
Focus on these 4 CRITICAL enforcement rules first:
1. **Learn from previous work before starting** (Rule 6) - Check WIs/archived/ for similar problems
2. **Debug first, solutions second** (Rule 7) - Evidence-based problem solving required  
3. **Ask user for actions needed** (Rule 8) - Never wait silently for user input
4. **Archive old work and extract learnings** (Rule 10) - Preserve knowledge, reduce clutter

### Major Violation Prevention
- ⚠️ **NEVER repeat solved problems** - Always review WIs/archived/ learnings first
- ⚠️ **NEVER proceed without debugging** - Evidence collection before solutions
- ⚠️ **NEVER wait silently** - Explicitly ask users for required actions
- ⚠️ **NEVER skip archiving** - WIs older than 2 weeks must be archived with learnings

## Modular System Benefits

✅ **Easily Discoverable**: Critical rules prominently positioned instead of buried
✅ **Focused Content**: Each module < 200 lines for better AI processing  
✅ **Maintainable**: Updates isolated to specific rule categories
✅ **Cross-Referenced**: Seamless integration with GitHub Copilot instructions

## New Archiving Rule Implementation

### Rule: Automatic WI Archiving for Files Older Than 2 Weeks

This rule has been implemented in the `modules/learning-and-archiving.md` file. Key points:

- **Automatic Process**: AI agents must identify and archive completed WIs older than 2 weeks
- **Knowledge Preservation**: Extract learnings and patterns before archiving
- **Archive Structure**: Organized by date in `WIs/archived/YYYY-MM/` folders
- **Learning Extraction**: Group insights by topic in learnings/ folders
- **Search Strategy**: Maintain searchable archive for future reference

**Implementation Status**: ✅ Active - Archive system created with WI1, WI6, WI8 examples

---

*This modular system ensures all critical enforcement rules remain discoverable while maintaining manageable file sizes for optimal AI agent processing. The original 847-line file has been broken down into focused modules, with the most critical rules easily accessible.*