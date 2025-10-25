// This file is used to configure SonarAnalyzer code analysis suppressions for the entire assembly.

using System.Diagnostics.CodeAnalysis;

// S3776: Cognitive Complexity - Program.cs is the application entry point with infrastructure setup
// The complexity is acceptable for this bootstrapping code and difficult to refactor meaningfully
[assembly: SuppressMessage("Major Code Smell",
    "S3776:Refactor this method to reduce its Cognitive Complexity",
    Justification = "Infrastructure setup code in Program.cs requires sequential configuration steps",
    Scope = "member",
    Target = "~M:<Program>$")]
