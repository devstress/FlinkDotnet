# GitHub Copilot Guidelines - Complete Index

This directory contains the GitHub Copilot Guidelines broken into manageable chunks for improved LLM agent usability.

## Navigation by Part

| Part | Content | Lines | Key Topics |
|------|---------|-------|------------|
| [Part 1](./default-rules-part-1.md) | SOLID Principles (Part 1) | ~110 | SRP, OCP, LSP, ISP Introduction |
| [Part 2](./default-rules-part-2.md) | SOLID Principles (Part 2) + .NET Practices | ~103 | ISP Examples, DIP, Naming Conventions |
| [Part 3](./default-rules-part-3.md) | .NET Best Practices + Code Review | ~110 | Exception Handling, Performance, Security, Code Review Checklist |
| [Part 4](./default-rules-part-4.md) | Review Guidelines + Test Coverage | ~114 | Common Patterns, Test Coverage Requirements, Reality Filter |
| [Part 5](./default-rules-part-5.md) | Work Item Enforcement (Part 1) | ~118 | Core Behavioral Enforcements, Work Item Lifecycle, Rules 1-6 |
| [Part 6](./default-rules-part-6.md) | Work Item Enforcement (Part 2) | ~120 | Learning Requirements, Debug-First, Implementation Guidelines |
| [Part 7](./default-rules-part-7.md) | Architecture + TDD Enforcement | ~112 | Architecture Documentation, TDD/BDD Requirements |
| [Part 8](./default-rules-part-8.md) | .NET 9.0 Environment | ~97 | Local Development, Environment Setup, Verification Commands |
| [Part 9](./default-rules-part-9.md) | Build + Test Enforcement | ~121 | Pre-Change Validation, Error Resolution, Recovery Procedures |

## Quick Reference by Topic

### SOLID Principles
- **Single Responsibility**: [Part 1](./default-rules-part-1.md#single-responsibility-principle-srp)
- **Open/Closed**: [Part 1](./default-rules-part-1.md#openclosed-principle-ocp)  
- **Liskov Substitution**: [Part 1](./default-rules-part-1.md#liskov-substitution-principle-lsp)
- **Interface Segregation**: [Part 1](./default-rules-part-1.md#interface-segregation-principle-isp) & [Part 2](./default-rules-part-2.md)
- **Dependency Inversion**: [Part 2](./default-rules-part-2.md#dependency-inversion-principle-dip)

### .NET Development
- **Naming Conventions**: [Part 2](./default-rules-part-2.md#naming-conventions)
- **Exception Handling**: [Part 3](./default-rules-part-3.md#exception-handling)
- **Async/Await**: [Part 3](./default-rules-part-3.md#asyncawait-best-practices)
- **Performance**: [Part 3](./default-rules-part-3.md#performance-considerations)
- **Security**: [Part 3](./default-rules-part-3.md#security-best-practices)
- **.NET 9.0 Setup**: [Part 8](./default-rules-part-8.md)

### Testing & Quality
- **Test Coverage**: [Part 4](./default-rules-part-4.md#test-coverage-requirements)
- **Code Review Checklist**: [Part 3](./default-rules-part-3.md#code-review-checklist)
- **TDD/BDD Enforcement**: [Part 7](./default-rules-part-7.md#test-driven-development-tdd-and-behavior-driven-development-bdd-enforcement-mandatory)
- **Build Validation**: [Part 9](./default-rules-part-9.md#rule-14-pre-change-validation-requirements-critical)

### Work Item Management
- **Core Rules**: [Part 5](./default-rules-part-5.md#work-item-enforcement-rule)
- **Learning Requirements**: [Part 6](./default-rules-part-6.md#rule-6-mandatory-learning-and-problem-prevention-critical)
- **Debug-First**: [Part 6](./default-rules-part-6.md#rule-7-mandatory-debug-first-investigation-critical)
- **Implementation Templates**: [Part 6](./default-rules-part-6.md#work-item-creation-template)

### Architecture & Documentation
- **Architecture Updates**: [Part 7](./default-rules-part-7.md#rule-11-system-architecture-documentation-updates-critical)
- **Reality Filter**: [Part 4](./default-rules-part-4.md#reality-filter---ai-agent-enforcement-rules)

## Usage Guidelines

### For LLM Agents
1. **Start with relevant part**: Use the topic index to find the appropriate chunk
2. **Follow cross-references**: Each part includes navigation to related content
3. **Apply overlap context**: 2-3 lines of overlap provide continuity between parts
4. **Use complete chunks**: Each part is designed to be independently useful

### For Developers
1. **Reference specific topics**: Use the quick reference table to find relevant guidelines
2. **Follow sequential reading**: Parts 1-9 provide comprehensive coverage when read in order
3. **Check cross-references**: Forward/backward references help understand related concepts

## Maintenance

This chunked structure is maintained to ensure:
- **Semantic integrity**: No rules or code blocks are split across chunks
- **Usability**: Each chunk contains complete, actionable guidance
- **Consistency**: Identical chunking across both `.github` and `.roo` locations
- **Navigation**: Clear cross-references and overlap for context preservation

---
**Source**: Generated from `/home/runner/work/FlinkDotnet/FlinkDotnet/.roo/rules/default-rules.md`  
**Last Updated**: 2025-01-07  
**Total Guidelines**: 1007 lines across 9 manageable chunks