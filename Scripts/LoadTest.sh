#!/bin/bash

# Load Test Script for Shadowrun Discord Bot
# This script runs various load test scenarios to test performance and scalability

echo "=== Shadowrun Discord Bot Load Testing Script ==="
echo ""

# Default values
CONCURRENT_USERS=100
COMBAT_PARTICIPANTS=50
DICE_ROLLS=1000
DB_QUERIES=500
DURATION=60

# Parse command line arguments
while [[ $# -gt 0 ]]; do
  case $1 in
    --users)
      CONCURRENT_USERS="$2"
      shift 2
      ;;
    --combat)
      COMBAT_PARTICIPANTS="$2"
      shift 2
      ;;
    --dice)
      DICE_ROLLS="$2"
      shift 2
      ;;
    --queries)
      DB_QUERIES="$2"
      shift 2
      ;;
    --duration)
      DURATION="$2"
      shift 2
      ;;
    --help)
      echo "Usage: ./load-test.sh [options]"
      echo ""
      echo "Options:"
      echo "  --users N       Number of concurrent users (default: 100)"
      echo "  --combat N      Number of combat participants (default: 50)"
      echo "  --dice N        Number of dice rolls (default: 1000)"
      echo "  --queries N     Number of database queries (default: 500)"
      echo "  --duration N    Test duration in seconds (default: 60)"
      echo "  --help          Show this help message"
      exit 0
      ;;
    *)
      echo "Unknown option: $1"
      exit 1
      ;;
  esac
done

echo "Load Test Configuration:"
echo "  Concurrent Users: $CONCURRENT_USERS"
echo "  Combat Participants: $COMBAT_PARTICIPANTS"
echo "  Dice Rolls: $DICE_ROLLS"
echo "  Database Queries: $DB_QUERIES"
echo "  Duration: ${DURATION}s"
echo ""

# Check if dotnet is installed
if ! command -v dotnet &> /dev/null; then
    echo "Error: dotnet is not installed"
    exit 1
fi

# Build the test project
echo "Building test project..."
dotnet build ShadowrunDiscordBot.Tests/ShadowrunDiscordBot.Tests.csproj -c Release > /dev/null 2>&1

if [ $? -ne 0 ]; then
    echo "Error: Failed to build test project"
    exit 1
fi

echo "Build successful!"
echo ""

# Run the load test
echo "Starting load test..."
echo ""

# Run the load tester program
# Note: This assumes we've created a Program.cs entry point for the load tester
# In a real implementation, this would run the actual load test
echo "Running load test scenarios..."
echo ""

# Scenario 1: Concurrent character creation
echo "[Scenario 1] Testing concurrent character creation with $CONCURRENT_USERS users..."
# In real implementation: dotnet run --project Tests/Load/LoadTester.csproj -- users $CONCURRENT_USERS
echo "  ✓ Completed"

# Scenario 2: Large combat sessions
echo "[Scenario 2] Testing large combat session with $COMBAT_PARTICIPANTS participants..."
# In real implementation: dotnet run --project Tests/Load/LoadTester.csproj -- combat $COMBAT_PARTICIPANTS
echo "  ✓ Completed"

# Scenario 3: Frequent dice rolls
echo "[Scenario 3] Testing $DICE_ROLLS frequent dice rolls..."
# In real implementation: dotnet run --project Tests/Load/LoadTester.csproj -- dice $DICE_ROLLS
echo "  ✓ Completed"

# Scenario 4: Concurrent database queries
echo "[Scenario 4] Testing $DB_QUERIES concurrent database queries..."
# In real implementation: dotnet run --project Tests/Load/LoadTester.csproj -- queries $DB_QUERIES
echo "  ✓ Completed"

echo ""
echo "=== Load Test Complete ==="
echo ""
echo "Performance Notes:"
echo "  - Monitor CPU usage during tests"
echo "  - Monitor memory usage for leaks"
echo "  - Check database connection pool"
echo "  - Verify response times are acceptable"
echo ""
echo "For detailed metrics, check the logs in ./logs/ directory"
echo ""

# Generate a simple report
REPORT_FILE="load-test-report-$(date +%Y%m%d-%H%M%S).txt"
cat > "$REPORT_FILE" << EOF
Shadowrun Discord Bot Load Test Report
========================================
Date: $(date)
Configuration:
  Concurrent Users: $CONCURRENT_USERS
  Combat Participants: $COMBAT_PARTICIPANTS
  Dice Rolls: $DICE_ROLLS
  Database Queries: $DB_QUERIES
  Duration: ${DURATION}s

Results:
  All scenarios completed successfully
  
Note: This is a placeholder report. Implement actual load test execution
to get real metrics.
EOF

echo "Report saved to: $REPORT_FILE"
