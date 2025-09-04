# Architecture Documentation Maintenance

## System Architecture Documentation Updates (CRITICAL - Rule 11)

### ALWAYS Update Documentation for Architecture Changes
- **ALWAYS update system architecture documentation** when making architecture or system design changes
- **Required file updates for architecture changes**:
  - `docs/system-architecture-diagram.png` - Visual system architecture diagram
  - `docs/system-architecture.html` - Interactive HTML architecture documentation
  - `README.md` - System design section and architecture overview

### Architecture Change Triggers
- New API endpoints or protocols (REST, GraphQL, gRPC)
- Database schema changes or new database providers
- New infrastructure components (caching, message queues, search engines)
- Authentication/authorization mechanism changes
- New external integrations or client interfaces
- Performance optimization changes affecting system behavior
- Security enhancements that modify data flow
- Deployment or hosting configuration changes

### Enterprise-Level Documentation Standards
- Clear separation of concerns in layer descriptions
- Professional visual design and color schemes
- Comprehensive component descriptions with business value
- Technology stack specifications with version requirements
- Data flow diagrams with security considerations
- Scalability and performance characteristics
- Integration patterns and API design rationale

### Quality Requirements
- All documentation must reflect enterprise, world-class standards
- Visual elements must be professional and consistent
- Technical descriptions must be precise and comprehensive
- Documentation must be accessible to both technical and business stakeholders
- **Failure to update architecture documentation is a MAJOR violation** requiring immediate correction

### Documentation Workflow

1. **Before Architecture Changes**
   - Review current documentation to understand existing design
   - Plan documentation updates alongside code changes
   - Identify all files that need updates

2. **During Implementation**
   - Update documentation concurrently with code changes
   - Ensure visual diagrams reflect new architecture
   - Test that interactive documentation works correctly

3. **After Implementation**
   - Validate all documentation is accurate and complete
   - Review documentation for enterprise quality standards
   - Ensure cross-references between documents are correct

### Enforcement Actions
- Architecture changes without documentation updates → MAJOR violation
- Documentation that doesn't match implementation → Immediate correction required
- Sub-standard visual design → Must meet enterprise standards
- Missing component descriptions → Documentation incomplete until added