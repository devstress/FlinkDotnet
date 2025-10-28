# TODO Implementation Guide

**Purpose**: Step-by-step guide for implementing Apache Flink features from the TODO list.

**Last Updated**: 2025-10-28

---

## Quick Start: Implementing a TODO Feature

### Step 1: Choose a Feature

Review [TRACKING.md](TRACKING.md) and select a feature to implement:

```
Priority Levels:
- P0 (Critical): Must implement soon - highest business value
- P1 (High): Important features - significant value
- P2 (Medium): Nice to have - moderate value  
- P3 (Low): Optional enhancements - low priority
```

**Recommended Starting Points**:
- **First-time contributors**: Start with P2/P3 features (simpler, lower risk)
- **Experienced contributors**: P0/P1 features (critical path)
- **Uncertain?** Ask maintainers which feature would be most valuable

### Step 2: Review Feature Documentation

Each feature has detailed documentation:

**Feature Categories**:
- [AI/ML Integration](ai-ml-integration-features.md) - P0 priority
- [Table API & Advanced SQL](table-api-advanced-sql-features.md) - P1 priority
- [Performance & Format](performance-format-features.md) - P2 priority
- [All Versions Coverage](all-versions-coverage.md) - All features by Flink version
- [Prometheus Exporter](prometheus-exporter-future-design.md) - P3 priority

**What to Look For**:
- Feature description and Apache Flink capabilities
- Current FlinkDotNet gap
- Estimated effort (in weeks)
- Dependencies on other features
- Example code showing expected usage

### Step 3: Create Work Item from Template

1. **Copy the template**:
   ```bash
   cp TODO/.implementation-template.md WIs/WI[#]_[feature-name].md
   ```

2. **Determine WI number**: Check `WIs/` folder for highest number, add 1

3. **Fill in header section**:
   ```markdown
   # WI7: AI Model DDL Support
   
   **File**: `WIs/WI7_ai-model-ddl-support.md`
   **Title**: [TODO] AI/ML Integration - CREATE MODEL DDL
   **Description**: Implementation of CREATE MODEL DDL from Apache Flink 2.1
   **Priority**: P0 - Critical
   **Component**: FlinkDotNet Table API
   **Type**: Feature Implementation
   **Assignee**: Your Name
   **Created**: 2025-10-28
   **Status**: Investigation
   **TODO Reference**: TODO/ai-ml-integration-features.md#1-ai-model-ddl-support
   ```

4. **Link to TRACKING.md**: Update the corresponding checklist item with your WI reference

### Step 4: Investigation Phase

**Goal**: Fully understand the feature before coding.

**Tasks**:
- [ ] Read Apache Flink documentation for the feature
- [ ] Review Flink Java source code implementation
- [ ] Analyze FlinkDotNet architecture for integration points
- [ ] Identify required IR (Intermediate Representation) changes
- [ ] Document dependencies on other features
- [ ] Create investigation report in WI

**Debug-First Requirement**:
- Always investigate thoroughly before proposing solutions
- Document all evidence and findings
- Identify root requirements and constraints

**Deliverables**:
- Investigation section in WI fully completed
- Clear understanding of technical approach
- Risk assessment and mitigation strategies

### Step 5: Design Phase

**Goal**: Design the solution before writing code.

**Tasks**:
- [ ] Design IR schema changes (if needed)
- [ ] Design C# API surface
- [ ] Design Java IR Runner changes
- [ ] Document architecture decisions
- [ ] Consider alternatives and justify choices
- [ ] Get design feedback from maintainers

**Key Design Principles**:
- **Consistency**: Follow existing FlinkDotNet patterns
- **Simplicity**: Minimal changes to achieve the goal
- **Type Safety**: Leverage C# type system
- **Fluent API**: Match Apache Flink's fluent style

**Deliverables**:
- Design section in WI fully completed
- Architecture decisions documented with rationale
- Alternatives considered and rejected with reasons

### Step 6: TDD/BDD Phase

**Goal**: Write tests BEFORE implementing the feature.

**Test-First Development**:
```csharp
// 1. Write a failing test
[Fact]
public void CreateModel_ShouldGenerateCorrectIR()
{
    var tEnv = env.GetTableEnvironment();
    
    tEnv.CreateModel("my_model", ModelDescriptor
        .ForProvider("OPENAI")
        .Build());
    
    var ir = tEnv.GetExecutionPlan();
    // This test fails because CreateModel doesn't exist yet
}

// 2. Implement minimal code to make it pass
public void CreateModel(string name, ModelDescriptor descriptor)
{
    // Minimal implementation
}

// 3. Refactor while keeping tests green
```

**Test Coverage Requirements**:
- **Frontend**: 70% minimum line coverage
- **Backend**: 70% minimum line coverage
- **Integration**: End-to-end scenarios

**Tasks**:
- [ ] Write unit tests for C# API
- [ ] Write unit tests for IR generation
- [ ] Write integration tests for E2E scenarios
- [ ] Write BDD scenarios for behavior validation
- [ ] Ensure all tests are in "red" state (failing)

**Deliverables**:
- Test Specifications section in WI completed
- Comprehensive test suite written (all failing)
- BDD scenarios defined

### Step 7: Implementation Phase

**Goal**: Write minimal code to make tests pass.

**TDD Workflow**:
1. Run tests - verify they fail
2. Write minimal code to make ONE test pass
3. Run tests - verify that test passes
4. Refactor if needed
5. Repeat for next test

**Implementation Order**:
1. **IR Schema**: Define JSON structure for the feature
2. **C# API**: Implement SDK classes and methods
3. **Java IR Runner**: Implement Flink integration
4. **Integration**: Wire everything together

**Tasks**:
- [ ] Implement IR schema changes
- [ ] Implement C# API in FlinkDotNet.SDK
- [ ] Implement Java interpreter in FlinkIRRunner
- [ ] Update documentation
- [ ] Fix all failing tests

**Code Quality Requirements**:
- Follow SOLID principles
- No compiler warnings
- All tests passing
- Code coverage meets thresholds

**Deliverables**:
- Implementation section in WI completed
- All tests passing (green)
- Code reviewed and approved

### Step 8: Testing & Validation

**Goal**: Verify the feature works correctly in all scenarios.

**Validation Steps**:
1. **Unit Tests**: All passing with good coverage
2. **Integration Tests**: E2E scenarios validated
3. **Regression Tests**: Existing tests still pass
4. **Manual Testing**: Run real Flink jobs with the feature
5. **Performance Testing**: Meets performance requirements

**Build Validation**:
```bash
# Always validate before and after changes
./scripts/validate-build-and-tests.ps1

# If failures occur, fix immediately
```

**Tasks**:
- [ ] All unit tests passing
- [ ] All integration tests passing
- [ ] No regression in existing tests
- [ ] Manual validation completed
- [ ] Performance metrics documented

**Deliverables**:
- Testing & Validation section in WI completed
- CI/CD pipeline passing
- Performance metrics documented

### Step 9: Owner Acceptance

**Goal**: Get approval from maintainers/stakeholders.

**Tasks**:
- [ ] Demonstrate feature working
- [ ] Present test evidence
- [ ] Address feedback
- [ ] Get formal approval

**Deliverables**:
- Owner Acceptance section in WI completed
- Feature approved for merge

### Step 10: Update TODO Tracking

**Goal**: Update tracking documents to reflect completion.

**Files to Update**:

1. **TODO/TRACKING.md**: Mark feature as implemented
   ```markdown
   - [x] CREATE MODEL DDL (2-3 weeks) - **COMPLETED**
     - **WI**: WI7_ai-model-ddl-support.md
     - **Status**: Completed 2025-10-28
     - **Actual Effort**: 2.5 weeks
   ```

2. **TODO/README.md**: Update status overview
   ```markdown
   ### By Feature Category
   | Feature Category | Status | Priority | Estimated Effort |
   |------------------|--------|----------|------------------|
   | AI/ML Integration | ⚠️ Partial (1/5) | P0 | 8-13 weeks remaining |
   ```

3. **Your WI**: Add final lessons learned

**Tasks**:
- [ ] Update TODO/TRACKING.md checklist
- [ ] Update TODO/README.md status tables
- [ ] Document lessons learned in WI
- [ ] Archive WI (if older than 1 month, move learnings to AI-Learning/)

**Deliverables**:
- All tracking documents updated
- Feature marked as complete
- Lessons learned documented

---

## Best Practices

### Do's ✅

**Investigation**:
- ✅ Always debug-first to find root causes
- ✅ Document all evidence and findings
- ✅ Review similar features in FlinkDotNet
- ✅ Learn from previous WIs before starting

**Design**:
- ✅ Follow existing FlinkDotNet patterns
- ✅ Keep changes minimal and surgical
- ✅ Consider alternatives and document why rejected
- ✅ Get design feedback early

**Implementation**:
- ✅ Write tests first (TDD)
- ✅ Make smallest possible changes
- ✅ Validate builds frequently
- ✅ Fix all failing tests immediately

**Documentation**:
- ✅ Update API reference
- ✅ Update TODO tracking documents
- ✅ Document lessons learned
- ✅ Write clear code comments

### Don'ts ❌

**Investigation**:
- ❌ Don't skip investigation phase
- ❌ Don't propose solutions without debugging
- ❌ Don't ignore previous WI learnings

**Design**:
- ❌ Don't violate SOLID principles
- ❌ Don't make unnecessary changes
- ❌ Don't skip design documentation
- ❌ Don't create alternative architectures without justification

**Implementation**:
- ❌ Don't skip writing tests first
- ❌ Don't leave failing tests
- ❌ Don't break existing functionality
- ❌ Don't introduce security vulnerabilities

**Documentation**:
- ❌ Don't forget to update TODO/TRACKING.md
- ❌ Don't skip lessons learned section
- ❌ Don't create IMPLEMENTATION_SUMMARY.md files

---

## Common Patterns

### Pattern 1: Adding a New DataStream Operation

**Example**: Adding a new transformation operator

**Steps**:
1. **IR Schema**: Add new operation type to IR
2. **C# API**: Add extension method to DataStream
3. **Java Runner**: Implement operation in FlinkIRRunner
4. **Tests**: Unit + integration tests

**Template**:
```csharp
// C# API
public static DataStream<TOut> NewOperation<TIn, TOut>(
    this DataStream<TIn> stream,
    Func<TIn, TOut> func)
{
    return stream.Transform(new NewOperationTransformation<TIn, TOut>(func));
}

// IR Generation
public class NewOperationTransformation<TIn, TOut> : ITransformation
{
    public JObject ToIR()
    {
        return new JObject
        {
            ["type"] = "new_operation",
            ["function"] = SerializeFunction(_func)
        };
    }
}
```

### Pattern 2: Adding a New Table API Feature

**Example**: Adding a new SQL DDL statement

**Steps**:
1. **SQL Parser**: Update SQL parsing (if needed)
2. **Table API**: Add method to TableEnvironment
3. **IR Schema**: Define DDL IR structure
4. **Java Runner**: Execute DDL on Flink TableEnvironment
5. **Tests**: SQL + Table API tests

**Template**:
```csharp
// C# Table API
public void ExecuteDDL(string ddlStatement)
{
    var ir = new JObject
    {
        ["type"] = "execute_ddl",
        ["statement"] = ddlStatement
    };
    SubmitOperation(ir);
}
```

### Pattern 3: Adding AI/ML Integration

**Example**: MODEL DDL + ML_PREDICT TVF

**Steps**:
1. **Model Descriptor**: Design C# model configuration API
2. **DDL Support**: Add CREATE/DROP MODEL statements
3. **TVF Support**: Add ML_PREDICT table-valued function
4. **Provider Integration**: Integrate AI providers (OpenAI, Azure)
5. **Tests**: E2E AI inference tests

**Template**:
```csharp
// Model Descriptor
public class ModelDescriptor
{
    public static ModelDescriptorBuilder ForProvider(string provider)
    {
        return new ModelDescriptorBuilder(provider);
    }
}

// Table Environment Integration
public void CreateModel(string name, ModelDescriptor descriptor)
{
    var ddl = $"CREATE MODEL {name} {descriptor.ToSQL()}";
    ExecuteDDL(ddl);
}
```

---

## Troubleshooting

### Issue: Build Failures

**Symptom**: `dotnet build` fails after changes

**Solution**:
1. Run `./scripts/validate-build-and-tests.ps1` to identify failures
2. Review compiler errors carefully
3. Ensure all dependencies are correct
4. Check for syntax errors in C# or Java code
5. Verify IR schema is valid JSON

### Issue: Test Failures

**Symptom**: Tests fail in CI or locally

**Solution**:
1. Run tests locally first: `dotnet test`
2. Check test output for specific failures
3. Debug failing tests individually
4. Verify test data and expectations
5. Ensure Flink cluster is properly configured for integration tests

### Issue: IR Not Recognized by FlinkIRRunner

**Symptom**: Java runner throws "Unknown operation type" error

**Solution**:
1. Verify IR JSON structure matches expected schema
2. Check FlinkIRRunner has corresponding handler
3. Ensure operation type string matches exactly
4. Validate IR serialization in C# produces correct JSON

### Issue: Integration Tests Timeout

**Symptom**: Integration tests hang or timeout

**Solution**:
1. Check Docker Desktop or Podman is running
2. Verify Flink cluster starts successfully
3. Check Aspire infrastructure is properly configured
4. Increase test timeout if legitimate long-running operation
5. Review Flink logs for errors

---

## Resources

### Apache Flink Documentation
- [Flink 2.1 Documentation](https://nightlies.apache.org/flink/flink-docs-master/)
- [DataStream API Guide](https://nightlies.apache.org/flink/flink-docs-master/docs/dev/datastream/overview/)
- [Table API & SQL Guide](https://nightlies.apache.org/flink/flink-docs-master/docs/dev/table/overview/)
- [AI/ML Integration Guide](https://nightlies.apache.org/flink/flink-docs-master/docs/dev/table/ml/)

### FlinkDotNet Documentation
- [API Reference](../docs/api-reference.md)
- [Features Guide](../docs/features.md)
- [Architecture](../docs/architecture-and-usecases.md)
- [Contributing Guide](../CONTRIBUTING.md)

### TODO Documentation
- [Main TODO README](README.md) - Overview of all missing features
- [TRACKING.md](TRACKING.md) - Implementation progress tracking
- [AI/ML Features](ai-ml-integration-features.md)
- [Table API Features](table-api-advanced-sql-features.md)
- [Performance Features](performance-format-features.md)
- [All Versions Coverage](all-versions-coverage.md)

### Work Item Resources
- [Template](.implementation-template.md) - Standard WI template for TODO features
- [Enforcement Rules](../.github/copilot-instructions.md#work-item-enforcement-rule) - WI lifecycle and rules
- [Example WIs](../WIs/) - Learn from existing Work Items

---

## FAQ

### Q: Which feature should I implement first?

**A**: Start with features that:
1. Match your expertise (DataStream vs Table API)
2. Have minimal dependencies (check TRACKING.md)
3. Are marked P2/P3 for learning (or P0/P1 if experienced)
4. Interest you personally (motivation matters!)

### Q: Can I implement part of a feature?

**A**: No. Each TODO feature should be implemented completely in one WI. If a feature is too large, break it into logical sub-features first and update TRACKING.md accordingly.

### Q: What if estimated effort is too long?

**A**: 
- Long estimates (10+ weeks) often indicate features that should be broken down
- Consult with maintainers about feature decomposition
- Consider implementing a minimal viable version first

### Q: How do I test locally without a Flink cluster?

**A**: Use LocalTesting project with .NET Aspire:
```bash
cd LocalTesting/LocalTesting.FlinkSqlAppHost
dotnet run
```
This starts a complete Flink + Kafka environment locally.

### Q: What if Apache Flink documentation is unclear?

**A**: 
1. Review Flink Java source code on GitHub
2. Check Flink mailing list archives
3. Look for FLIP (Flink Improvement Proposal) documents
4. Ask in FlinkDotNet discussions

### Q: Can I skip phases in the WI template?

**A**: No. All phases are mandatory:
- Investigation: Understand the problem
- Design: Plan the solution
- TDD/BDD: Write tests first
- Implementation: Code the solution
- Testing: Validate it works
- Owner Acceptance: Get approval

Skipping phases leads to poor quality and rework.

### Q: How do I handle dependencies between features?

**A**: 
1. Check TRACKING.md "Dependencies" section for each feature
2. Implement dependencies first
3. If blocked, coordinate with other contributors
4. Document dependency status in your WI

### Q: What if I find a bug while implementing?

**A**: 
1. Create a separate WI for the bug
2. Fix the bug first if it blocks your feature
3. Document the bug and fix in your feature WI lessons learned
4. Don't mix bug fixes with feature implementation in commits

---

## Getting Help

### Where to Ask Questions

1. **GitHub Discussions**: General questions, architecture discussions
2. **GitHub Issues**: Bug reports, feature requests
3. **WI Comments**: Specific questions about your implementation
4. **Maintainers**: Tag maintainers in PR for review

### Before Asking

1. **Search existing WIs**: Someone may have solved your problem
2. **Review AI-Learning/ folder**: Documented learnings from previous work
3. **Check TODO documentation**: Often has detailed technical information
4. **Read Apache Flink docs**: Understand the official Flink implementation

### When Asking

Include:
- Which feature you're implementing (WI number)
- Current phase (Investigation, Design, etc.)
- Specific problem or question
- What you've already tried
- Relevant code or logs

---

## Success Criteria

You've successfully implemented a TODO feature when:

- [x] WI completed through all phases
- [x] All tests passing (unit + integration + regression)
- [x] Code coverage meets thresholds (70%+)
- [x] Code review approved
- [x] CI/CD pipeline green
- [x] TODO/TRACKING.md updated
- [x] TODO/README.md status updated
- [x] Documentation updated (API reference, features)
- [x] Lessons learned documented for future contributors

**Celebrate! 🎉** You've contributed a valuable feature to FlinkDotNet and helped bridge the gap between .NET and Apache Flink.

---

**Last Updated**: 2025-10-28
**Maintainers**: FlinkDotNet Core Team
**Questions?** Open a GitHub Discussion
