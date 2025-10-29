#!/bin/bash
# Bash script to run deployment and integration tests
# This script validates the deployment configuration and runs integration tests

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Default values
ENVIRONMENT="Development"
SKIP_DOCKER_TESTS=false
VERBOSE=false

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --environment)
            ENVIRONMENT="$2"
            shift 2
            ;;
        --skip-docker)
            SKIP_DOCKER_TESTS=true
            shift
            ;;
        --verbose)
            VERBOSE=true
            shift
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

echo -e "${CYAN}========================================${NC}"
echo -e "${CYAN}Deployment Tests - Meeting Management System${NC}"
echo -e "${CYAN}========================================${NC}"
echo -e "${YELLOW}Environment: $ENVIRONMENT${NC}"
echo ""

# Function to run tests
run_tests() {
    local test_filter=$1
    local test_name=$2
    
    echo -e "${GREEN}Running $test_name...${NC}"
    
    local test_args=(
        "test"
        "tests/MeetingManagementSystem.Tests/MeetingManagementSystem.Tests.csproj"
        "--filter" "$test_filter"
        "--logger" "console;verbosity=normal"
    )
    
    if [ "$VERBOSE" = true ]; then
        test_args+=("--verbosity" "detailed")
    fi
    
    if dotnet "${test_args[@]}"; then
        echo -e "${GREEN}✓ $test_name passed${NC}"
        return 0
    else
        echo -e "${RED}✗ $test_name failed${NC}"
        return 1
    fi
}

# Check prerequisites
echo -e "${YELLOW}Checking prerequisites...${NC}"

# Check .NET SDK
if command -v dotnet &> /dev/null; then
    DOTNET_VERSION=$(dotnet --version)
    echo -e "${GREEN}✓ .NET SDK version: $DOTNET_VERSION${NC}"
else
    echo -e "${RED}✗ .NET SDK not found. Please install .NET 9.0 SDK${NC}"
    exit 1
fi

# Check Docker (if not skipping Docker tests)
if [ "$SKIP_DOCKER_TESTS" = false ]; then
    if command -v docker &> /dev/null; then
        DOCKER_VERSION=$(docker --version)
        echo -e "${GREEN}✓ Docker version: $DOCKER_VERSION${NC}"
    else
        echo -e "${YELLOW}⚠ Docker not found. Skipping Docker tests${NC}"
        SKIP_DOCKER_TESTS=true
    fi
fi

echo ""

# Build the solution
echo -e "${YELLOW}Building solution...${NC}"
if dotnet build MeetingManagementSystem.sln --configuration Release; then
    echo -e "${GREEN}✓ Build successful${NC}"
else
    echo -e "${RED}✗ Build failed${NC}"
    exit 1
fi

echo ""

# Run tests
declare -A test_results

# 1. Environment Configuration Tests
if run_tests "FullyQualifiedName~EnvironmentConfigurationTests" "Environment Configuration Tests"; then
    test_results["Configuration"]=1
else
    test_results["Configuration"]=0
fi
echo ""

# 2. Database Migration Tests
if run_tests "FullyQualifiedName~DatabaseMigrationTests" "Database Migration Tests"; then
    test_results["Migration"]=1
else
    test_results["Migration"]=0
fi
echo ""

# 3. Health Check Tests
if run_tests "FullyQualifiedName~HealthCheckTests" "Health Check Tests"; then
    test_results["HealthCheck"]=1
else
    test_results["HealthCheck"]=0
fi
echo ""

# 4. Performance Tests
if run_tests "FullyQualifiedName~PerformanceTests" "Performance Tests"; then
    test_results["Performance"]=1
else
    test_results["Performance"]=0
fi
echo ""

# 5. Docker Deployment Tests (if not skipped)
if [ "$SKIP_DOCKER_TESTS" = false ]; then
    if run_tests "FullyQualifiedName~DockerDeploymentTests" "Docker Deployment Tests"; then
        test_results["Docker"]=1
    else
        test_results["Docker"]=0
    fi
    echo ""
fi

# Summary
echo -e "${CYAN}========================================${NC}"
echo -e "${CYAN}Test Summary${NC}"
echo -e "${CYAN}========================================${NC}"

passed_tests=0
failed_tests=0

for test in "${!test_results[@]}"; do
    if [ "${test_results[$test]}" -eq 1 ]; then
        echo -e "$test: ${GREEN}PASSED${NC}"
        ((passed_tests++))
    else
        echo -e "$test: ${RED}FAILED${NC}"
        ((failed_tests++))
    fi
done

echo ""
echo -e "${CYAN}Total: ${#test_results[@]} test suites${NC}"
echo -e "${GREEN}Passed: $passed_tests${NC}"
echo -e "${RED}Failed: $failed_tests${NC}"
echo ""

# Exit with appropriate code
if [ $failed_tests -gt 0 ]; then
    echo -e "${RED}Some tests failed. Please review the output above.${NC}"
    exit 1
else
    echo -e "${GREEN}All tests passed successfully!${NC}"
    exit 0
fi
