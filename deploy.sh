#!/bin/bash

# Shadowrun Discord Bot Deployment Script
# Usage: ./deploy.sh [environment]

set -e

ENVIRONMENT="${1:-production}"
COMPOSE_FILE="docker-compose.yml"

echo "=========================================="
echo "Shadowrun Discord Bot Deployment"
echo "Environment: $ENVIRONMENT"
echo "=========================================="

# Check for required tools
command -v docker >/dev/null 2>&1 || { echo "Error: Docker is required but not installed."; exit 1; }
command -v docker-compose >/dev/null 2>&1 || { echo "Error: docker-compose is required but not installed."; exit 1; }

# Check for .env file
if [ ! -f .env ]; then
    echo "Error: .env file not found. Please create one from .env.example"
    exit 1
fi

# Pull latest images
echo "Pulling latest images..."
docker-compose -f $COMPOSE_FILE pull

# Build the application
echo "Building application..."
docker-compose -f $COMPOSE_FILE build --no-cache

# Stop existing containers
echo "Stopping existing containers..."
docker-compose -f $COMPOSE_FILE down

# Start new containers
echo "Starting containers..."
docker-compose -f $COMPOSE_FILE up -d

# Wait for health check
echo "Waiting for application to be healthy..."
sleep 10

# Check status
if docker-compose -f $COMPOSE_FILE ps | grep -q "Up"; then
    echo "=========================================="
    echo "✅ Deployment successful!"
    echo "=========================================="
    echo ""
    echo "Application is running at: http://localhost:5000"
    echo "Redis Commander (dev mode): http://localhost:8081"
    echo ""
    echo "Useful commands:"
    echo "  docker-compose logs -f app     # View application logs"
    echo "  docker-compose logs -f redis   # View Redis logs"
    echo "  docker-compose ps              # View container status"
    echo "  docker-compose down            # Stop all containers"
else
    echo "=========================================="
    echo "❌ Deployment failed!"
    echo "=========================================="
    echo "Check logs with: docker-compose logs"
    exit 1
fi
