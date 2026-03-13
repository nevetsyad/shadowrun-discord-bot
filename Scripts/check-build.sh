#!/bin/bash
# check-build.sh - Check for compilation errors without running tests
#
# This script performs a full build to catch compilation errors early.
# Use this before committing to ensure your code compiles correctly.

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
SOLUTION_FILE="ShadowrunDiscordBot.sln"

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "🔨 Build Checker"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

cd "$PROJECT_ROOT"

# Check for solution file
if [ ! -f "$SOLUTION_FILE" ]; then
    echo "❌ ERROR: Solution file not found: $SOLUTION_FILE"
    exit 1
fi

echo "📦 Restoring NuGet packages..."
dotnet restore "$SOLUTION_FILE"

echo ""
echo "🔨 Building solution (Release configuration)..."
echo ""

if dotnet build "$SOLUTION_FILE" --configuration Release --no-restore -warnaserror; then
    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "✅ Build succeeded! No compilation errors."
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    exit 0
else
    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "❌ Build failed! Fix the compilation errors above."
    echo ""
    echo "💡 COMMON FIXES:"
    echo "   - Missing using directives: Add required using statements"
    echo "   - Type errors: Check method signatures and return types"
    echo "   - Missing references: Ensure all projects are properly referenced"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    exit 1
fi
