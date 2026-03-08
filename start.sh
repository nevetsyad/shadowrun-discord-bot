#!/bin/bash

# Shadowrun Discord Bot - Quick Start Script

echo "═══════════════════════════════════════════════════════"
echo "   Shadowrun Discord Bot - .NET Edition Setup"
echo "═══════════════════════════════════════════════════════"
echo ""

# Check for .NET 8 SDK
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET SDK not found. Please install .NET 8 SDK:"
    echo "   https://dotnet.microsoft.com/download/dotnet/8.0"
    exit 1
fi

echo "✓ Found .NET SDK: $(dotnet --version)"
echo ""

# Check for environment file
if [ ! -f ".env" ]; then
    echo "📝 Creating .env file from template..."
    cp .env.example .env
    echo ""
    echo "⚠️  IMPORTANT: Edit .env file with your Discord credentials:"
    echo "   - DISCORD_TOKEN (from Discord Developer Portal)"
    echo "   - CLIENT_ID (from Discord Developer Portal)"
    echo "   - GUILD_ID (your Discord server ID)"
    echo "   - JWT_SECRET (generate a random 32+ character string)"
    echo ""
    read -p "Press Enter to open .env in your editor..."
    ${EDITOR:-nano} .env
fi

echo ""
echo "🔨 Building project..."
dotnet build

if [ $? -ne 0 ]; then
    echo "❌ Build failed. Please check the errors above."
    exit 1
fi

echo ""
echo "✓ Build successful!"
echo ""
echo "🚀 Starting Shadowrun Discord Bot..."
echo ""

dotnet run
