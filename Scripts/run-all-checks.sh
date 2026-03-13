#!/bin/bash
# run-all-checks.sh - Run all CI checks locally before pushing
#
# This script runs all the checks that the CI pipeline will run,
# allowing you to catch issues before pushing to GitHub.

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "🚀 Running All CI Checks"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

FAILED=()

# Run code style check
echo "📋 Step 1/4: Code Style Check"
echo "──────────────────────────────────────────────────────────────────"
if "$SCRIPT_DIR/check-code-style.sh"; then
    echo ""
else
    FAILED+=("code-style")
fi

# Run build check
echo ""
echo "🔨 Step 2/4: Build Check"
echo "──────────────────────────────────────────────────────────────────"
if "$SCRIPT_DIR/check-build.sh"; then
    echo ""
else
    FAILED+=("build")
fi

# Run security check
echo ""
echo "🔒 Step 3/4: Security Check"
echo "──────────────────────────────────────────────────────────────────"
"$SCRIPT_DIR/check-security.sh"
echo ""

# Run tests
echo "🧪 Step 4/4: Run Tests"
echo "──────────────────────────────────────────────────────────────────"
cd "$(dirname "$SCRIPT_DIR")"
if dotnet test --configuration Release --verbosity normal; then
    echo ""
else
    FAILED+=("tests")
fi

# Summary
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "📊 CI Check Summary"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

if [ ${#FAILED[@]} -eq 0 ]; then
    echo "✅ All checks passed! Your code is ready to push."
    echo ""
    echo "Next steps:"
    echo "   git add ."
    echo "   git commit -m 'your commit message'"
    echo "   git push"
    exit 0
else
    echo "❌ The following checks failed:"
    for check in "${FAILED[@]}"; do
        echo "   - $check"
    done
    echo ""
    echo "Fix the issues above before pushing to GitHub."
    exit 1
fi
