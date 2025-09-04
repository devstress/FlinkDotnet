# Aspire Testing Integration Patterns - Learning Repository

## Consolidated Learnings from WI9 and WI10

### Critical Problem Pattern: Repeated Aspire Integration Failures
**Source WIs**: WI9_fix-observability-tests-aspire-integration.md, WI10_observability-tests-aspire-framework-fix.md

### Root Cause Analysis
The same Aspire testing framework integration issues have occurred multiple times:
1. **Wrong Integration Approach**: Manual HttpClient creation instead of proper DistributedApplicationTestingBuilder
2. **Environment Dependency**: Tests failing due to .NET version mismatches
3. **Missing Step Definitions**: BDD scenarios incomplete due to missing step implementations

### Correct Implementation Pattern
```csharp
// CORRECT: Use DistributedApplicationTestingBuilder
public async Task InitializeAsync()
{
    _app = await DistributedApplicationTestingBuilder
        .CreateAsync<Projects.LocalTesting_AppHost>();
    _httpClient = _app.CreateHttpClient("localtesting-webapi");
}

// WRONG: Manual HttpClient creation
var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:18000") };
```

### Prevention Rules
1. **ALWAYS use DistributedApplicationTestingBuilder** for Aspire testing
2. **NEVER hardcode localhost URLs** in Aspire tests
3. **VERIFY .NET 9.0 environment** before writing Aspire tests
4. **IMPLEMENT ALL BDD step definitions** before running scenarios

### Future Reference Checklist
- [ ] Environment check: `dotnet --version` returns 9.0.x
- [ ] Use DistributedApplicationTestingBuilder.CreateAsync<Projects.LocalTesting_AppHost>()
- [ ] Create HttpClient via app.CreateHttpClient("service-name")
- [ ] Implement all step definitions matching .feature file scenarios
- [ ] Test with `dotnet test --filter "Category=observability"`

### Never Repeat These Mistakes
1. **Manual infrastructure setup** in Aspire tests
2. **Missing environment validation** before test development
3. **Incomplete BDD scenario implementations**
4. **Hardcoded service URLs** instead of service discovery