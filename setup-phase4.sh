#!/bin/bash

# Phase 4 Session Management Setup Script
# This script helps set up and verify the Phase 4 implementation

echo "=========================================="
echo "Shadowrun Bot - Phase 4 Setup"
echo "Session Management System"
echo "=========================================="
echo ""

# Check if we're in the right directory
if [ ! -f "ShadowrunDiscordBot.csproj" ]; then
    echo "❌ Error: Please run this script from the project root directory"
    exit 1
fi

echo "✅ Found project file"
echo ""

# Step 1: Verify new files exist
echo "📁 Checking new files..."
FILES=(
    "Services/SessionManagementService.cs"
    "Services/DatabaseService.SessionManagement.cs"
    "PHASE4_SESSION_MANAGEMENT.md"
)

all_files_exist=true
for file in "${FILES[@]}"; do
    if [ -f "$file" ]; then
        echo "  ✅ $file"
    else
        echo "  ❌ $file (missing)"
        all_files_exist=false
    fi
done

if [ "$all_files_exist" = false ]; then
    echo ""
    echo "❌ Some files are missing. Please ensure all Phase 4 files are in place."
    exit 1
fi

echo ""

# Step 2: Verify model updates
echo "📦 Checking model updates..."
if grep -q "public class SessionBreak" Models/GameSessionModels.cs; then
    echo "  ✅ SessionBreak model found"
else
    echo "  ❌ SessionBreak model not found in GameSessionModels.cs"
    exit 1
fi

if grep -q "public class CompletedSession" Models/GameSessionModels.cs; then
    echo "  ✅ CompletedSession model found"
else
    echo "  ❌ CompletedSession model not found in GameSessionModels.cs"
    exit 1
fi

echo ""

# Step 3: Verify service registration
echo "⚙️  Checking service registration..."
if grep -q "SessionManagementService" Program.cs; then
    echo "  ✅ SessionManagementService registered in Program.cs"
else
    echo "  ❌ SessionManagementService not registered in Program.cs"
    exit 1
fi

echo ""

# Step 4: Verify database context updates
echo "💾 Checking database context..."
if grep -q "DbSet<SessionBreak>" Services/DatabaseService.cs; then
    echo "  ✅ SessionBreaks DbSet found"
else
    echo "  ❌ SessionBreaks DbSet not found in DatabaseService.cs"
    exit 1
fi

if grep -q "DbSet<CompletedSession>" Services/DatabaseService.cs; then
    echo "  ✅ CompletedSessions DbSet found"
else
    echo "  ❌ CompletedSessions DbSet not found in DatabaseService.cs"
    exit 1
fi

echo ""

# Step 5: Build the project
echo "🔨 Building project..."
if dotnet build; then
    echo "  ✅ Build successful"
else
    echo "  ❌ Build failed"
    exit 1
fi

echo ""

# Step 6: Check for database file
echo "🗄️  Checking database..."
if [ -f "shadowrun.db" ]; then
    echo "  ℹ️  Existing database found at shadowrun.db"
    echo "  ℹ️  New tables will be created automatically on next startup"
else
    echo "  ℹ️  No existing database found"
    echo "  ℹ️  Database will be created on first run with all tables"
fi

echo ""

# Summary
echo "=========================================="
echo "✅ Phase 4 Setup Complete!"
echo "=========================================="
echo ""
echo "Next steps:"
echo "  1. Run the bot: dotnet run"
echo "  2. EF Core will automatically create new tables"
echo "  3. Test the new commands in Discord:"
echo "     • /session break [reason]"
echo "     • /session notes [text]"
echo "     • /session tags action:add tag:example"
echo "     • /session complete [outcome]"
echo ""
echo "Optional: Create background service for auto-break detection"
echo "  See PHASE4_SESSION_MANAGEMENT.md for details"
echo ""
echo "📚 Documentation: PHASE4_SESSION_MANAGEMENT.md"
echo ""
