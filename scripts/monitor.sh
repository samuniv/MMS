#!/bin/bash
# Monitoring Script for Meeting Management System
# This script checks the health and status of all services

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
COMPOSE_FILE="docker-compose.prod.yml"
HEALTH_ENDPOINT="http://localhost:5000/health"
ALERT_EMAIL="${ALERT_EMAIL:-admin@gov.np}"

echo "========================================="
echo "Meeting Management System - Health Check"
echo "========================================="
echo "Timestamp: $(date)"
echo ""

# Function to check service health
check_service() {
    local service=$1
    local status=$(docker-compose -f ${COMPOSE_FILE} ps -q ${service} 2>/dev/null)
    
    if [ -z "$status" ]; then
        echo -e "${RED}✗${NC} ${service}: Not running"
        return 1
    fi
    
    local health=$(docker inspect --format='{{.State.Health.Status}}' $(docker-compose -f ${COMPOSE_FILE} ps -q ${service}) 2>/dev/null || echo "no-health-check")
    
    if [ "$health" = "healthy" ] || [ "$health" = "no-health-check" ]; then
        echo -e "${GREEN}✓${NC} ${service}: Running"
        return 0
    else
        echo -e "${RED}✗${NC} ${service}: Unhealthy (${health})"
        return 1
    fi
}

# Function to check HTTP endpoint
check_http() {
    local url=$1
    local response=$(curl -s -o /dev/null -w "%{http_code}" ${url} 2>/dev/null || echo "000")
    
    if [ "$response" = "200" ]; then
        echo -e "${GREEN}✓${NC} HTTP Health Check: OK (${response})"
        return 0
    else
        echo -e "${RED}✗${NC} HTTP Health Check: Failed (${response})"
        return 1
    fi
}

# Function to check database
check_database() {
    local result=$(docker-compose -f ${COMPOSE_FILE} exec -T postgres psql -U postgres -d meetingmanagement -c "SELECT 1" 2>/dev/null || echo "failed")
    
    if [[ "$result" == *"1 row"* ]]; then
        echo -e "${GREEN}✓${NC} Database: Connected"
        return 0
    else
        echo -e "${RED}✗${NC} Database: Connection failed"
        return 1
    fi
}

# Function to check disk space
check_disk_space() {
    local threshold=90
    local usage=$(df -h / | awk 'NR==2 {print $5}' | sed 's/%//')
    
    if [ "$usage" -lt "$threshold" ]; then
        echo -e "${GREEN}✓${NC} Disk Space: ${usage}% used"
        return 0
    else
        echo -e "${YELLOW}⚠${NC} Disk Space: ${usage}% used (Warning: >90%)"
        return 1
    fi
}

# Function to check memory usage
check_memory() {
    local app_memory=$(docker stats --no-stream --format "{{.MemPerc}}" meetingmanagement_app_prod 2>/dev/null | sed 's/%//' || echo "0")
    local db_memory=$(docker stats --no-stream --format "{{.MemPerc}}" meetingmanagement_postgres_prod 2>/dev/null | sed 's/%//' || echo "0")
    
    echo -e "${GREEN}✓${NC} Memory Usage:"
    echo "  - Application: ${app_memory}%"
    echo "  - Database: ${db_memory}%"
}

# Function to check logs for errors
check_logs() {
    local error_count=$(docker-compose -f ${COMPOSE_FILE} logs --tail=100 app 2>/dev/null | grep -i "error\|exception\|fatal" | wc -l)
    
    if [ "$error_count" -eq 0 ]; then
        echo -e "${GREEN}✓${NC} Recent Errors: None"
        return 0
    else
        echo -e "${YELLOW}⚠${NC} Recent Errors: ${error_count} found in last 100 log lines"
        return 1
    fi
}

# Run all checks
echo "Service Status:"
echo "---------------"
check_service "postgres"
check_service "app"
echo ""

echo "Health Checks:"
echo "--------------"
check_http ${HEALTH_ENDPOINT}
check_database
echo ""

echo "Resource Usage:"
echo "---------------"
check_disk_space
check_memory
echo ""

echo "Log Analysis:"
echo "-------------"
check_logs
echo ""

# Summary
echo "========================================="
echo "Monitoring completed at $(date)"
echo "========================================="
