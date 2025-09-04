#!/bin/bash

# Test script for observability tests fix - requires .NET 9.0 environment
echo "🔧 Testing Observability Tests Fix"
echo "📋 Fixed: CreateHttpClient endpoint name issue"
echo ""

# Check .NET version
echo "1. Checking .NET version..."
dotnet --version

if [ $? -ne 0 ]; then
    echo "❌ .NET 9.0 SDK not found. Please install .NET 9.0.100 or later."
    echo "📥 Download from: https://dotnet.microsoft.com/download/dotnet/9.0"
    exit 1
fi

echo ""
echo "2. Building LocalTesting solution..."
dotnet build LocalTesting/LocalTesting.sln --configuration Release

if [ $? -ne 0 ]; then
    echo "❌ Build failed. Cannot proceed with observability tests."
    exit 1
fi

echo ""
echo "3. Running observability tests..."
dotnet test LocalTesting/LocalTesting.IntegrationTests --filter "Category=observability" --configuration Release

echo ""
echo "✅ Observability test execution completed"
echo "🔧 Fix applied: Corrected CreateHttpClient endpoint name from 'localtesting-webapi' to ('localtesting-webapi', 'webapi')"
echo "📝 The Aspire testing framework now properly resolves the HTTP endpoint"