#!/bin/bash

# Shadowrun Discord Bot Build Script
# Usage: ./build.sh

set -e

echo "=========================================="
echo "Shadowrun Discord Bot - Build"
echo "=========================================="
echo ""

# Check for .NET SDK
if ! command -v dotnet &> /dev/null; then
    echo "❌ Error: .NET SDK is not installed"
    echo ""
    echo "Please install .NET 8.0 SDK:"
    echo "  - macOS: https://dotnet.microsoft.com/download/dotnet/8.0"
    echo "  - Linux: https://dotnet.microsoft.com/download/dotnet/8.0"
    echo "  - Windows: https://dotnet.microsoft.com/download/dotnet/8.0"
    exit 1
fi

echo "✓ .NET SDK found: $(dotnet --version)"
echo ""

# Clean build
echo "Cleaning previous builds..."
dotnet clean
echo ""

# Restore packages
echo "Restoring NuGet packages..."
dotnet restore
echo ""

# Build project
echo "Building project..."
dotnet build --no-restore
echo ""

# Check build result
if [ $? -eq 0 ]; then
    echo "=========================================="
    echo "✅ Build successful!"
    echo "=========================================="
    echo ""
    echo "Output directory: bin/Debug/net8.0/"
    echo ""
    echo "To run the bot:"
    echo "  ./start.sh"
    echo "  OR"
    echo "  dotnet run"
    echo ""
    echo "To run tests:"
    echo "  dotnet test"
    echo ""
else
    echo "=========================================="
    echo "❌ Build failed!"
    echo "=========================================="
    exit 1
fi
