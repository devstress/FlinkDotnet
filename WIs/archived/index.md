# Work Items Archive Index

This folder contains archived Work Items that have been completed and summarized for future reference. The archive maintains all critical learnings while removing active workspace clutter.

## Archive Structure

### 2025-09/
- **completed/**: Summarized completed Work Items from September 2025
- **learnings/**: Extracted patterns and insights grouped by topic

## Archiving Process

Work Items are automatically archived when:
1. Status shows "Completed", "Done", or equivalent 
2. All phases are finished with lessons learned documented
3. Work Item is older than 2 weeks from completion

## Archive Benefits

- **Preserved Knowledge**: All learnings and patterns are retained
- **Reduced Clutter**: Active workspace contains only current work
- **Better Searchability**: Grouped learnings by topic for easier discovery
- **Institutional Memory**: Prevents repeating solved problems

## How to Use Archives

1. **Before starting new work**: Search archived learnings for similar patterns
2. **Research solutions**: Check completed WIs for implementation approaches  
3. **Avoid repetition**: Apply lessons learned to prevent known problems
4. **Pattern discovery**: Identify successful approaches for reuse

## Search Strategy

Use this command to search across all archives:
```bash
find WIs/archived -name "*.md" -exec grep -l "search_term" {} \;
```

## Maintenance

Archives are maintained automatically by the AI agent following Rule 10: Automatic Archiving & Learning Enforcement. Manual archives can be created following the same summarization pattern.