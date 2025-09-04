# WI11: Chunk Large Instruction Files into Smaller Coherent Pieces

**File**: `WIs/WI11_chunk-instruction-files.md`
**Title**: Break down large instruction files into 80-120 line chunks  
**Description**: Break copilot-instructions.md (1007 lines) and default-rules.md (847 lines) into smaller, coherent chunks with proper overlap and semantic preservation
**Priority**: Medium
**Component**: Documentation
**Type**: Enhancement
**Assignee**: AI Agent
**Created**: 2025-01-07
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- WI8: Learned importance of maintaining documentation integrity while making surgical changes
- WI9: Learned about systematic testing and validation approaches

### Lessons Applied  
- Make minimal, surgical changes that preserve all valuable content
- Test changes thoroughly to ensure no functional regressions
- Maintain clear documentation structure and readability

### Problems Prevented
- Avoid breaking semantic integrity of instruction sections
- Prevent context loss between chunks through proper overlap
- Ensure each chunk remains usable as standalone reference

## Phase 1: Investigation
### Requirements
The task requires breaking down two large instruction files into smaller, manageable chunks:

1. **Target Files**:
   - `.github/copilot-instructions.md` (1007 lines)
   - `.roo/rules/default-rules.md` (847 lines)

2. **Key Discovery**: Files are NOT identical as originally stated:
   - Different content starting around line 790
   - copilot-instructions.md has .NET 9.0 environment rules
   - default-rules.md has Premium AI usage tracking rules

3. **Chunking Requirements**:
   - 80-120 lines per chunk
   - Preserve semantic boundaries (never cut sentences/rules/code blocks)
   - Add 2-3 line overlap between chunks for context
   - Numbered naming convention (e.g., `copilot-instructions-part1.md`)
   - Self-contained chunks with forward/backward references as needed

### Debug Information (MANDATORY - Update this section for every investigation)
- **File Analysis**: Used `wc -l` and `diff` to discover size and content differences
- **Structure Mapping**: Used `grep -n "^#"` to map section headers for both files
- **Content Verification**: Confirmed files have different sections and content after line 790
- **No Errors Found**: Files are readable and well-structured for chunking

### Findings
1. **copilot-instructions.md Structure** (1007 lines):
   - GitHub Copilot Guidelines (lines 1-456)
   - Work Item Enforcement Rule (lines 457-789)
   - .NET 9.0 Local Development Environment (lines 790-862)
   - AI Agent Build and Test Enforcement (lines 863-1007)

2. **default-rules.md Structure** (847 lines):
   - GitHub Copilot Guidelines (lines 1-456) - Similar to above
   - Work Item Enforcement Rule (lines 457-789) - Similar to above
   - Premium AI Usage Tracking (lines 790-847) - Different content

3. **Optimal Chunking Strategy**:
   - copilot-instructions.md: ~9-12 chunks of 80-120 lines each
   - default-rules.md: ~8-10 chunks of 80-120 lines each
   - Break at natural section boundaries
   - Include 2-3 line overlap for context

### Lessons Learned
- Files assumed to be identical were actually different
- Need separate chunking strategies for each file
- Section structure analysis is crucial for proper semantic boundaries

## Phase 2: Design  
### Requirements
- Develop chunking strategy that respects semantic boundaries
- Create naming convention for chunked files
- Plan overlap strategy for context preservation
- Design validation approach

### Architecture Decisions
**Chunking Strategy for copilot-instructions.md**:
- Part 1: Title + SOLID Principles (lines 1-120)
- Part 2: SOLID Principles continued (lines 118-238) 
- Part 3: .NET Best Practices (lines 236-356)
- Part 4: Code Review + Automatic Checks (lines 354-474)
- Part 5: Work Item Enforcement intro (lines 472-592)
- Part 6: Work Item Rules 1-5 (lines 590-710)
- Part 7: Work Item Rules 6-10 + Implementation (lines 708-828)
- Part 8: Architecture Documentation + TDD (lines 826-946)
- Part 9: .NET 9.0 Environment Rules (lines 944-1007)

**Chunking Strategy for default-rules.md**:
- Similar strategy but ending with Premium AI tracking instead

### Why This Approach
- Preserves semantic integrity by breaking at section boundaries
- Maintains context through 2-3 line overlaps
- Creates manageable 80-120 line chunks
- Each chunk remains self-contained and usable

### Alternatives Considered
- Fixed line count chunking: Rejected due to semantic breaking risk
- Single large chunk reduction: Rejected as doesn't meet 80-120 line requirement

## Phase 3: TDD/BDD
### Test Specifications
1. Each chunk must be 80-120 lines
2. No semantic breaks (sentences, rules, code blocks)
3. 2-3 line overlap between consecutive chunks
4. All chunks combined must equal original content
5. Each chunk must be standalone readable

### Behavior Definitions
- GIVEN large instruction files
- WHEN chunked into smaller files  
- THEN each chunk is semantically complete and contextually linked
- AND total content is preserved without loss

## Phase 4: Implementation
### Code Changes
[Implementation details will be added here]

### Challenges Encountered
[Challenges will be documented here]

### Solutions Applied
[Solutions will be documented here]

## Phase 5: Testing & Validation
### Test Results
[Test results will be added here]

### Performance Metrics
[Metrics will be added here]

## Phase 6: Owner Acceptance
### Demonstration
[Demonstration will be documented here]

### Owner Feedback
[Feedback will be documented here]

### Final Approval
[Approval status will be documented here]

## Lessons Learned & Future Reference (MANDATORY)
### What Worked Well
[Will be documented after completion]

### What Could Be Improved  
[Will be documented after completion]

### Key Insights for Similar Tasks
[Will be documented after completion]

### Specific Problems to Avoid in Future
[Will be documented after completion]

### Reference for Future WIs
[Will be documented after completion]