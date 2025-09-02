#!/bin/bash

# =============================================================================
# FlinkDotNet Learning Course Environment Setup Script
# Supports: Linux and macOS
# =============================================================================

set -e  # Exit on any error

echo "🚀 FlinkDotNet Learning Course Environment Setup"
echo "================================================="
echo ""

# Detect OS
OS="Unknown"
if [[ "$OSTYPE" == "linux-gnu"* ]]; then
    OS="Linux"
elif [[ "$OSTYPE" == "darwin"* ]]; then
    OS="macOS"
else
    echo "❌ Unsupported operating system: $OSTYPE"
    echo "   This script supports Linux and macOS only."
    echo "   For Windows, please use setup-environment-windows.ps1"
    exit 1
fi

echo "✅ Detected OS: $OS"
echo ""

# Function to check if command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Function to get available memory in GB
get_memory_gb() {
    if [[ "$OS" == "Linux" ]]; then
        mem_kb=$(grep MemTotal /proc/meminfo | awk '{print $2}')
        echo $((mem_kb / 1024 / 1024))
    elif [[ "$OS" == "macOS" ]]; then
        mem_bytes=$(sysctl -n hw.memsize)
        echo $((mem_bytes / 1024 / 1024 / 1024))
    fi
}

# Function to install .NET 9.0 SDK
install_dotnet() {
    echo "📦 Installing .NET 9.0 SDK..."
    
    if [[ "$OS" == "Linux" ]]; then
        # Install .NET 9.0 on Linux
        curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version 9.0.304
        export PATH="$HOME/.dotnet:$PATH"
        echo 'export PATH="$HOME/.dotnet:$PATH"' >> ~/.bashrc
        echo 'export PATH="$HOME/.dotnet:$PATH"' >> ~/.zshrc 2>/dev/null || true
    elif [[ "$OS" == "macOS" ]]; then
        # Check if Homebrew is available
        if command_exists brew; then
            echo "   Using Homebrew to install .NET..."
            brew install --cask dotnet
        else
            echo "   Using Microsoft installer..."
            curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version 9.0.304
            export PATH="$HOME/.dotnet:$PATH"
            echo 'export PATH="$HOME/.dotnet:$PATH"' >> ~/.zshrc
            echo 'export PATH="$HOME/.dotnet:$PATH"' >> ~/.bash_profile 2>/dev/null || true
        fi
    fi
    
    # Verify installation
    if command_exists dotnet; then
        local version=$(dotnet --version)
        echo "   ✅ .NET SDK installed: $version"
    else
        echo "   ❌ .NET SDK installation failed"
        exit 1
    fi
}

# Function to install Docker
install_docker() {
    echo "🐳 Installing Docker..."
    
    if [[ "$OS" == "Linux" ]]; then
        # Install Docker on Linux
        if command_exists apt-get; then
            # Ubuntu/Debian
            echo "   Installing Docker on Ubuntu/Debian..."
            sudo apt-get update
            sudo apt-get install -y apt-transport-https ca-certificates curl gnupg lsb-release
            curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg
            echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
            sudo apt-get update
            sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin
            sudo usermod -aG docker $USER
        elif command_exists yum; then
            # RHEL/CentOS/Fedora
            echo "   Installing Docker on RHEL/CentOS/Fedora..."
            sudo yum install -y docker
            sudo systemctl start docker
            sudo systemctl enable docker
            sudo usermod -aG docker $USER
        else
            echo "   ❌ Unsupported Linux distribution. Please install Docker manually."
            echo "      Visit: https://docs.docker.com/engine/install/"
            exit 1
        fi
    elif [[ "$OS" == "macOS" ]]; then
        # Install Docker on macOS
        if command_exists brew; then
            echo "   Using Homebrew to install Docker Desktop..."
            brew install --cask docker
        else
            echo "   ❌ Homebrew not found. Please install Docker Desktop manually."
            echo "      Visit: https://docs.docker.com/desktop/install/mac-install/"
            echo "      Or install Homebrew first: /bin/bash -c \"\$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)\""
            exit 1
        fi
    fi
    
    echo "   ✅ Docker installation completed"
    echo "   ⚠️  You may need to restart your terminal or log out/in for Docker permissions to take effect"
}

# Function to install Git
install_git() {
    echo "📝 Installing Git..."
    
    if [[ "$OS" == "Linux" ]]; then
        if command_exists apt-get; then
            sudo apt-get update && sudo apt-get install -y git
        elif command_exists yum; then
            sudo yum install -y git
        else
            echo "   ❌ Unable to install Git automatically. Please install manually."
            exit 1
        fi
    elif [[ "$OS" == "macOS" ]]; then
        if command_exists brew; then
            brew install git
        else
            # Git is usually pre-installed on macOS, try to trigger Xcode tools install
            git --version 2>/dev/null || xcode-select --install
        fi
    fi
    
    echo "   ✅ Git installed"
}

# Function to clone repository
clone_repository() {
    echo "📁 Setting up FlinkDotNet repository..."
    
    local repo_dir="$HOME/FlinkDotNet"
    
    if [[ -d "$repo_dir" ]]; then
        echo "   📁 Repository already exists at $repo_dir"
        echo "      Updating repository..."
        cd "$repo_dir"
        git pull origin main
    else
        echo "   📁 Cloning repository to $repo_dir..."
        git clone https://github.com/devstress/FlinkDotnet.git "$repo_dir"
        cd "$repo_dir"
    fi
    
    echo "   ✅ Repository ready at: $repo_dir"
    echo "export FLINK_DOTNET_PATH=\"$repo_dir\"" >> ~/.bashrc 2>/dev/null || true
    echo "export FLINK_DOTNET_PATH=\"$repo_dir\"" >> ~/.zshrc 2>/dev/null || true
}

# Main setup flow
main() {
    echo "🔍 Checking system requirements..."
    echo ""
    
    # Check memory
    local memory_gb=$(get_memory_gb)
    echo "💾 Available Memory: ${memory_gb}GB"
    if [[ $memory_gb -lt 8 ]]; then
        echo "   ⚠️  Warning: Recommended minimum is 8GB RAM"
        echo "      You may experience performance issues with LocalTesting infrastructure"
    else
        echo "   ✅ Memory requirements met"
    fi
    echo ""
    
    # Check and install .NET 9.0
    if command_exists dotnet; then
        local version=$(dotnet --version)
        if [[ $version == 9.* ]]; then
            echo "✅ .NET 9.0 SDK already installed: $version"
        else
            echo "❌ .NET version $version found, but need 9.0.x"
            install_dotnet
        fi
    else
        install_dotnet
    fi
    echo ""
    
    # Check and install Docker
    if command_exists docker; then
        echo "✅ Docker already installed"
        if docker version >/dev/null 2>&1; then
            echo "   ✅ Docker is running"
        else
            echo "   ⚠️  Docker is installed but not running"
            echo "      Please start Docker Desktop or the Docker service"
        fi
    else
        install_docker
    fi
    echo ""
    
    # Check and install Git
    if command_exists git; then
        echo "✅ Git already installed: $(git --version)"
    else
        install_git
    fi
    echo ""
    
    # Clone repository
    clone_repository
    echo ""
    
    # Verify .NET environment
    echo "🔧 Verifying .NET environment..."
    cd "$HOME/FlinkDotNet"
    export PATH="$HOME/.dotnet:$PATH"
    
    if dotnet --version >/dev/null 2>&1; then
        echo "   ✅ .NET SDK: $(dotnet --version)"
        
        # Check if Aspire workload is installed
        if dotnet workload list | grep -q aspire; then
            echo "   ✅ Aspire workload already installed"
        else
            echo "   📦 Installing Aspire workload..."
            dotnet workload install aspire
            echo "   ✅ Aspire workload installed"
        fi
    else
        echo "   ❌ .NET SDK not found in PATH"
        echo "      Please restart your terminal and run: export PATH=\"\$HOME/.dotnet:\$PATH\""
        exit 1
    fi
    echo ""
    
    # Test build
    echo "🔨 Testing build environment..."
    if ./validate-build-and-tests.ps1 -SkipTests >/dev/null 2>&1; then
        echo "   ✅ All solutions build successfully"
    else
        echo "   ⚠️  Build validation had issues - this may be normal for first run"
        echo "      You can run './validate-build-and-tests.ps1' manually later"
    fi
    echo ""
    
    # Final instructions
    echo "🎉 Setup Complete!"
    echo "=================="
    echo ""
    echo "📍 Repository location: $HOME/FlinkDotNet"
    echo ""
    echo "🚀 Quick Start:"
    echo "   1. Open a new terminal (to load new PATH)"
    echo "   2. cd $HOME/FlinkDotNet/LearningCourse"
    echo "   3. Read the STUDENT-GUIDE.md"
    echo "   4. Start with Day 1: cd Day01-Flink21-Fundamentals/Exercise-Solutions"
    echo ""
    echo "💡 To start the LocalTesting infrastructure:"
    echo "   cd $HOME/FlinkDotNet/LocalTesting"
    echo "   dotnet run --project LocalTesting.AppHost"
    echo ""
    echo "📚 Learning Course Path:"
    echo "   • Follow STUDENT-GUIDE.md for complete 14-day course"
    echo "   • Each day has Exercise-Solutions/README.md with step-by-step instructions"
    echo "   • All exercises are now compatible with .NET 9.0"
    echo ""
    
    if [[ "$OS" == "Linux" ]]; then
        echo "🐧 Linux Note:"
        echo "   • If Docker commands need sudo, log out and back in (or restart)"
        echo "   • You may need to start Docker service: sudo systemctl start docker"
    elif [[ "$OS" == "macOS" ]]; then
        echo "🍎 macOS Note:"
        echo "   • Make sure Docker Desktop is running before starting exercises"
        echo "   • You may need to restart your terminal for PATH changes"
    fi
    
    echo ""
    echo "✅ Environment setup complete! Happy learning! 🎓"
}

# Run main function
main "$@"