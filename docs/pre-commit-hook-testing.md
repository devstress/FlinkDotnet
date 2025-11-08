# Pre-Commit Hook Testing Guide

This document demonstrates how the pre-commit hook works in FlinkDotNet.

## Test Scenario: Badly Formatted Code

### Setup

The pre-commit hook has been installed and is ready to use.

### Example: Before Formatting

Here's an example of badly formatted C# code:

```csharp
namespace FlinkDotNet.Demo;

public class DemoClass
{
    public void BadlyFormattedMethod()
    {
        var x=1+2;  // No spaces around operators
        var y    =   3   +   4;  // Too many spaces
        if(x>y){Console.WriteLine("Test");}  // No spaces, no line breaks
    }
}
```

### What Happens on Commit

When you run `git commit`, the pre-commit hook will:

1. **Detect changed C# files** - Identifies all staged `.cs` files
2. **Run dotnet format** - Automatically formats the files according to `.editorconfig` rules
3. **Re-stage formatted files** - Adds the properly formatted files back to the staging area
4. **Proceed with commit** - Commits the properly formatted code

### Expected Output

```
Running pre-commit checks...
Formatting changed C# files...
Formatting solution: FlinkDotNet/FlinkDotNet.sln
✓ Formatted: FlinkDotNet/FlinkDotNet.sln
  ↳ Restaging: FlinkDotNet/Some/File.cs
✓ Formatting complete. Formatted files have been restaged.
✓ Pre-commit checks passed!
```

### After Formatting

The code would be automatically formatted to:

```csharp
namespace FlinkDotNet.Demo;

public class DemoClass
{
    public void BadlyFormattedMethod()
    {
        var x = 1 + 2;  // Proper spacing
        var y = 3 + 4;  // Proper spacing
        if (x > y)
        {
            Console.WriteLine("Test");
        }  // Proper line breaks and braces
    }
}
```

## Testing the Hook

### Test 1: No C# Files Changed

If you commit changes that don't include C# files (e.g., markdown, config files):

```bash
git add README.md
git commit -m "Update README"
```

**Expected Output:**
```
Running pre-commit checks...
No C# files to format.
✓ Pre-commit checks passed!
```

### Test 2: Well-Formatted Code

If your C# code is already properly formatted:

```bash
git add WellFormattedFile.cs
git commit -m "Add new feature"
```

**Expected Output:**
```
Running pre-commit checks...
Formatting changed C# files...
Formatting solution: FlinkDotNet/FlinkDotNet.sln
✓ Formatted: FlinkDotNet/FlinkDotNet.sln
✓ All files already properly formatted.
✓ Pre-commit checks passed!
```

### Test 3: Badly Formatted Code

If your C# code needs formatting:

```bash
git add BadlyFormattedFile.cs
git commit -m "Add new feature"
```

**Expected Output:**
```
Running pre-commit checks...
Formatting changed C# files...
Formatting solution: FlinkDotNet/FlinkDotNet.sln
✓ Formatted: FlinkDotNet/FlinkDotNet.sln
  ↳ Restaging: BadlyFormattedFile.cs
✓ Formatting complete. Formatted files have been restaged.
✓ Pre-commit checks passed!
```

### Test 4: Syntax Errors

If your C# code has syntax errors that prevent formatting:

```bash
git add FileWithSyntaxError.cs
git commit -m "Add new feature"
```

**Expected Output:**
```
Running pre-commit checks...
Formatting changed C# files...
Formatting solution: FlinkDotNet/FlinkDotNet.sln
✗ Failed to format: FlinkDotNet/FlinkDotNet.sln
Please fix formatting errors and try again.
```

The commit will be **aborted** until you fix the syntax errors.

## Bypassing the Hook (Not Recommended)

In rare cases where you need to bypass the hook:

```bash
git commit --no-verify -m "Emergency commit"
```

⚠️ **Warning:** This should only be used in exceptional circumstances, as it may lead to:
- CI failures due to formatting issues
- Code review delays
- Inconsistent code style

## Troubleshooting

### Hook Not Running

**Problem:** The hook doesn't seem to execute on commit.

**Solution:**
```bash
# Verify the hook is installed
ls -la .git/hooks/pre-commit

# If not installed, run the installation script
./scripts/install-git-hooks.sh  # Linux/macOS
.\scripts\install-git-hooks.ps1  # Windows

# Verify it's executable
chmod +x .git/hooks/pre-commit
```

### Hook Takes Too Long

**Problem:** The hook seems to take a long time.

**Explanation:** The hook only formats changed files, not the entire solution. If it's taking long, it might be:
- First-time execution (NuGet package restore)
- Large number of files changed
- Slow disk I/O

**Temporary workaround:**
```bash
# Bypass for this commit only
git commit --no-verify -m "Your message"

# Then manually format later
dotnet format FlinkDotNet.sln
```

### dotnet CLI Not Found

**Problem:** Hook fails with "dotnet CLI not found"

**Solution:**
```bash
# Verify .NET is installed
dotnet --version

# If not installed, install .NET 9.0 SDK
# Download from: https://dotnet.microsoft.com/download/dotnet/9.0
```

## Verification

After installation, you can verify the hook works by:

1. Creating a test file with bad formatting
2. Staging the file
3. Committing and observing the hook output
4. Checking that the file was automatically formatted

## Benefits

✅ **Consistent Code Style** - All code follows the same formatting rules
✅ **No Manual Formatting** - Automatic formatting on every commit
✅ **No Merge Conflicts** - Formatting is consistent across all developers
✅ **Faster Code Reviews** - No discussions about code style
✅ **CI Success** - No formatting-related CI failures

---

For complete development guidelines, see [TODO/DEVELOPMENT_RULES.md](TODO/DEVELOPMENT_RULES.md).
