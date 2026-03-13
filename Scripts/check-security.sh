#!/bin/bash
# check-security.sh - Check for vulnerable and deprecated packages
#
# This script runs security analysis on NuGet packages to identify
# known vulnerabilities. Use this before deploying to production.

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
SOLUTION_FILE="ShadowrunDiscordBot.sln"

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "🔒 Security Checker"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

cd "$PROJECT_ROOT"

# Check for solution file
if [ ! -f "$SOLUTION_FILE" ]; then
    echo "❌ ERROR: Solution file not found: $SOLUTION_FILE"
    exit 1
fi

echo "📦 Restoring NuGet packages..."
dotnet restore "$SOLUTION_FILE" > /dev/null 2>&1 || true

echo ""
echo "🔍 Checking for vulnerable packages..."
echo ""

# Check for vulnerabilities
VULN_OUTPUT=$(dotnet list package --vulnerable --include-transitive 2>&1 || true)
echo "$VULN_OUTPUT"

echo ""
echo "🔍 Checking for deprecated packages..."
echo ""

# Check for deprecated packages
DEPRECATION_OUTPUT=$(dotnet list package --deprecated --include-transitive 2>&1 || true)
echo "$DEPRECATION_OUTPUT"

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

if echo "$VULN_OUTPUT" | grep -q "has no vulnerable packages"; then
    echo "✅ No vulnerable packages found"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    exit 0
else
    echo "⚠️  Vulnerable packages detected - review the output above"
    echo ""
    echo "🔧 TO FIX:"
    echo "   1. Update vulnerable packages to patched versions"
    echo "   2. Check for breaking changes in release notes"
    echo "   3. Test thoroughly after updating"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    exit 0  # Exit 0 since this is advisory only
fi
