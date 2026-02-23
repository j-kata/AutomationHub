#!/bin/bash

# Script to stop the development environment

echo "Stopping AutomationHub development environment"
echo ""

# Kill the dotnet process if running
pkill -f "dotnet run" || true

docker-compose down

echo ""
echo "Docker Compose services stopped and removed"
