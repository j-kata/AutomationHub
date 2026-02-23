#!/bin/bash

# Script to start the development environment with Docker Compose and the application

set -e

echo "Starting AutomationHub development environment"
echo ""

# Start Docker Compose services
echo "Starting Docker Compose services: Mosquitto, SMTP4Dev, PostgreSQL"
docker-compose up -d

# Wait for services to be healthy
echo "Waiting for services to be healthy"
sleep 5

# Check if services are running
docker-compose ps

echo ""
echo "SMTP4Dev web UI: http://localhost:1080"
echo "MQTT Broker: localhost:1883"
echo "PostgreSQL: localhost:5432 (user: postgres, password: postgres)"
echo ""
echo "Starting AutomationHub application"
cd AutomationHub
dotnet run
