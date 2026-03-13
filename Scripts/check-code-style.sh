#!/bin/bash
# check-code-style.sh - Check code style without making changes
#
# This script runs dotnet-format --check to verify all code follows
# the .editorconfig rules. Use this before committing to catch issues early.

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
SOLUTION_FILE="ShadowrunDiscordBot.sln"

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "📋 Code Style Checker"
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

echo "🔍 Checking code style..."
echo ""

# Run dotnet-format check
if dotnet-format --check --verbosity diagnostic; then
    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "✅ All code passes style checks!"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    exit 0
else
    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "❌ Code style violations found!"
    echo ""
    echo "🔧 TO FIX: Run ./scripts/fix-code-style.sh"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    exit 1
fi
