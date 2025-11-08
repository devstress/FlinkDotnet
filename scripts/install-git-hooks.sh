#!/bin/bash
# FlinkDotNet Git Hooks Installation Script
# Installs pre-commit hook for automatic code formatting

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}Installing FlinkDotNet Git hooks...${NC}"

# Check if we're in a git repository
if [ ! -d .git ]; then
    echo -e "${RED}Error: Not in a git repository root directory.${NC}"
    echo -e "${RED}Please run this script from the repository root.${NC}"
    exit 1
fi

# Check if dotnet is available
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}Warning: dotnet CLI not found.${NC}"
    echo -e "${YELLOW}Please install .NET SDK 9.0 or later.${NC}"
    echo -e "${YELLOW}The hook will be installed but won't work until .NET is available.${NC}"
fi

# Create hooks directory if it doesn't exist
mkdir -p .git/hooks

# Source hook file
SOURCE_HOOK="scripts/pre-commit"

# Destination hook file
DEST_HOOK=".git/hooks/pre-commit"

# Check if source hook exists
if [ ! -f "$SOURCE_HOOK" ]; then
    echo -e "${RED}Error: Source hook file not found: $SOURCE_HOOK${NC}"
    exit 1
fi

# Backup existing hook if it exists
if [ -f "$DEST_HOOK" ]; then
    BACKUP_FILE="$DEST_HOOK.backup.$(date +%Y%m%d_%H%M%S)"
    echo -e "${YELLOW}Backing up existing pre-commit hook to: $BACKUP_FILE${NC}"
    cp "$DEST_HOOK" "$BACKUP_FILE"
fi

# Copy the hook
echo -e "${YELLOW}Installing pre-commit hook...${NC}"
cp "$SOURCE_HOOK" "$DEST_HOOK"

# Make it executable
chmod +x "$DEST_HOOK"

# Verify installation
if [ -x "$DEST_HOOK" ]; then
    echo -e "${GREEN}✓ Pre-commit hook installed successfully!${NC}"
    echo ""
    echo -e "${GREEN}The hook will now automatically run 'dotnet format' before each commit.${NC}"
    echo ""
    echo -e "${YELLOW}To test the hook:${NC}"
    echo "  1. Make a change to a .cs file"
    echo "  2. Stage the file: git add <file>"
    echo "  3. Commit: git commit -m 'Test commit'"
    echo "  4. The hook will automatically format your code"
    echo ""
    echo -e "${YELLOW}To bypass the hook (not recommended):${NC}"
    echo "  git commit --no-verify -m 'Your message'"
    echo ""
else
    echo -e "${RED}✗ Failed to install pre-commit hook.${NC}"
    exit 1
fi

exit 0
