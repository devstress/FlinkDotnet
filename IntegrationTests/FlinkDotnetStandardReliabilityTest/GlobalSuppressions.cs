// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

// Test-specific suppressions for reliability testing
[assembly: SuppressMessage("Design", "S1144:Remove the unused private field", Justification = "Test diagnostic fields are essential for debugging and monitoring")]
[assembly: SuppressMessage("Performance", "S4487:Remove this unread private field", Justification = "Test diagnostic fields provide context for debugging")]
[assembly: SuppressMessage("Design", "S1172:Remove this unused method parameter", Justification = "Test parameters provide flexibility for future enhancements")]
[assembly: SuppressMessage("Maintainability", "S2325:Make static method", Justification = "Test methods often require instance context")]
[assembly: SuppressMessage("Maintainability", "S3776:Reduce Cognitive Complexity", Justification = "Test complexity is justified for comprehensive validation")]
[assembly: SuppressMessage("Performance", "S1481:Remove unused local variable", Justification = "Test variables provide debugging context")]
[assembly: SuppressMessage("Performance", "S1854:Remove useless assignment", Justification = "Test assignments provide debugging context")]
[assembly: SuppressMessage("Performance", "S6608:Use indexing instead of LINQ", Justification = "LINQ improves test readability")]
[assembly: SuppressMessage("Performance", "S6610:Use char overload", Justification = "String methods are clearer for test validation")]
[assembly: SuppressMessage("Design", "S927:Rename parameter", Justification = "Test parameter names are descriptive for clarity")]
[assembly: SuppressMessage("Design", "CS1998:Missing await operators", Justification = "Async test setup for future async operations")]
[assembly: SuppressMessage("Style", "IDE0005:Using directive is unnecessary", Justification = "Test usings may be needed for conditional compilation")]