#!/bin/bash
# fix-code-style.sh - Auto-fix code style violations using dotnet-format
# 
# This script runs dotnet-format with --fix to automatically correct
# code style violations according to the .editorconfig rules.

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
SOLUTION_FILE="ShadowrunDiscordBot.sln"

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "🔧 Code Style Auto-Fixer"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

cd "$PROJECT_ROOT"

# Check for solution file
if [ ! -f "$SOLUTION_FILE" ]; then
    echo "❌ ERROR: Solution file not found: $SOLUTION_FILE"
    exit 1
fi

# Check for .editorconfig
if [ ! -f ".editorconfig" ]; then
    echo "⚠️  WARNING: No .editorconfig found. Using default style rules."
fi

echo "📦 Running dotnet-format to fix style violations..."
echo ""

# Run dotnet-format with --fix
dotnet-format --fix --verbosity diagnostic

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "✅ Code style fixes applied!"
echo ""
echo "📋 NEXT STEPS:"
echo "   1. Review the changes with: git diff"
echo "   2. Commit the fixes with: git add . && git commit -m 'style: apply code style fixes'"
echo ""
echo "💡 TIP: Run './scripts/check-code-style.sh' to verify all issues are fixed"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
