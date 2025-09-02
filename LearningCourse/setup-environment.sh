#!/bin/bash

# =============================================================================
# FlinkDotNet Learning Course Universal Environment Setup Script
# Detects platform and runs the appropriate setup script
# =============================================================================

set -e

echo "🚀 FlinkDotNet Learning Course Universal Setup"
echo "==============================================="
echo ""

# Function to detect platform
detect_platform() {
    local os=""
    local arch=""
    
    # Detect OS
    case "$(uname -s)" in
        Linux*)     os="Linux";;
        Darwin*)    os="macOS";;
        CYGWIN*)    os="Windows";;
        MINGW*)     os="Windows";;
        MSYS*)      os="Windows";;
        *)          os="Unknown";;
    esac
    
    # Detect architecture
    case "$(uname -m)" in
        x86_64|amd64)   arch="x64";;
        arm64|aarch64)  arch="arm64";;
        armv7l)         arch="arm";;
        i386|i686)      arch="x86";;
        *)              arch="Unknown";;
    esac
    
    echo "$os|$arch"
}

# Function to check prerequisites
check_prerequisites() {
    local platform=$1
    local os=$(echo $platform | cut -d'|' -f1)
    
    echo "🔍 Checking prerequisites for $os..."
    
    case $os in
        "Linux"|"macOS")
            # Check if running in a shell that supports the setup script
            if [[ -z "$BASH_VERSION" && -z "$ZSH_VERSION" ]]; then
                echo "❌ This script requires bash or zsh shell"
                echo "   Please run: bash setup-environment.sh"
                exit 1
            fi
            ;;
        "Windows")
            echo "❌ Windows detected in Unix environment"
            echo "   For Windows, please run: setup-environment-windows.ps1"
            echo "   You may need to run this in PowerShell or Git Bash"
            exit 1
            ;;
        *)
            echo "❌ Unsupported platform: $os"
            echo "   Supported platforms: Linux, macOS, Windows"
            exit 1
            ;;
    esac
}

# Function to run platform-specific setup
run_setup() {
    local platform=$1
    local os=$(echo $platform | cut -d'|' -f1)
    local arch=$(echo $platform | cut -d'|' -f2)
    local script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
    
    echo "🖥️  Detected Platform: $os ($arch)"
    echo ""
    
    case $os in
        "Linux"|"macOS")
            local setup_script="$script_dir/setup-environment-linux-macos.sh"
            if [[ -f "$setup_script" ]]; then
                echo "🚀 Running $os setup script..."
                chmod +x "$setup_script"
                bash "$setup_script"
            else
                echo "❌ Setup script not found: $setup_script"
                echo "   Please ensure you're running this from the LearningCourse directory"
                exit 1
            fi
            ;;
        *)
            echo "❌ Unsupported platform for this script: $os"
            echo "   For Windows, please run: setup-environment-windows.ps1"
            exit 1
            ;;
    esac
}

# Function to show help
show_help() {
    cat << 'EOF'
🚀 FlinkDotNet Learning Course Universal Setup

This script automatically detects your platform and runs the appropriate setup script.

Supported Platforms:
  • Linux (Ubuntu, Debian, RHEL, CentOS, Fedora)
  • macOS (Intel and Apple Silicon)
  • Windows (via PowerShell script)

Usage:
  ./setup-environment.sh          # Auto-detect and setup
  ./setup-environment.sh --help   # Show this help

Platform-Specific Scripts:
  Linux/macOS:  ./setup-environment-linux-macos.sh
  Windows:      ./setup-environment-windows.ps1

What This Script Installs:
  ✅ .NET 9.0 SDK
  ✅ Docker Desktop (or Docker Engine on Linux)
  ✅ Git
  ✅ Aspire workload for .NET
  ✅ FlinkDotNet repository
  ✅ Validates build environment

Requirements:
  • 8GB+ RAM recommended
  • Internet connection
  • Administrator/sudo privileges (for installations)

After Setup:
  1. Follow the STUDENT-GUIDE.md
  2. Start LocalTesting infrastructure
  3. Begin with Day 1 exercises

For Windows users:
  Please run setup-environment-windows.ps1 in PowerShell instead of this script.

EOF
}

# Main function
main() {
    # Check for help flag
    if [[ "$1" == "--help" || "$1" == "-h" ]]; then
        show_help
        exit 0
    fi
    
    # Detect platform
    local platform=$(detect_platform)
    
    # Check prerequisites
    check_prerequisites "$platform"
    
    # Run setup
    run_setup "$platform"
}

# Run main function with all arguments
main "$@"