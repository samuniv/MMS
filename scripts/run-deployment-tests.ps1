# PowerShell script to run deployment and integration tests
# This script validates the deployment configuration and runs integration tests

param(
    [string]$Environment = "Development",
    [switch]$SkipDockerTests = $false,
    [switch]$Verbose = $false
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Deployment Tests - Meeting Management System" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Environment: $Environment" -ForegroundColor Yellow
Write-Host ""

# Set error action preference
$ErrorActionPreference = "Stop"

# Function to run tests
function Run-Tests {
    param(
        [string]$TestFilter,
        [string]$TestName
    )
    
    Write-Host "Running $TestName..." -ForegroundColor Green
    
    $testArgs = @(
        "test",
        "tests/MeetingManagementSystem.Tests/MeetingManagementSystem.Tests.csproj",
        "--filter", $TestFilter,
        "--logger", "console;verbosity=normal"
    )
    
    if ($Verbose) {
        $testArgs += "--verbosity", "detailed"
    }
    
    try {
        & dotnet $testArgs
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ $TestName passed" -ForegroundColor Green
            return $true
        } else {
            Write-Host "✗ $TestName failed" -ForegroundColor Red
            return $false
        }
    } catch {
        Write-Host "✗ $TestName failed with error: $_" -ForegroundColor Red
        return $false
    }
}

# Check prerequisites
Write-Host "Checking prerequisites..." -ForegroundColor Yellow

# Check .NET SDK
try {
    $dotnetVersion = & dotnet --version
    Write-Host "✓ .NET SDK version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ .NET SDK not found. Please install .NET 9.0 SDK" -ForegroundColor Red
    exit 1
}

# Check Docker (if not skipping Docker tests)
if (-not $SkipDockerTests) {
    try {
        $dockerVersion = & docker --version
        Write-Host "✓ Docker version: $dockerVersion" -ForegroundColor Green
    } catch {
        Write-Host "⚠ Docker not found. Skipping Docker tests" -ForegroundColor Yellow
        $SkipDockerTests = $true
    }
}

Write-Host ""

# Build the solution
Write-Host "Building solution..." -ForegroundColor Yellow
try {
    & dotnet build MeetingManagementSystem.sln --configuration Release
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Build successful" -ForegroundColor Green
    } else {
        Write-Host "✗ Build failed" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "✗ Build failed with error: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Run tests
$testResults = @{}

# 1. Environment Configuration Tests
$testResults["Configuration"] = Run-Tests `
    -TestFilter "FullyQualifiedName~EnvironmentConfigurationTests" `
    -TestName "Environment Configuration Tests"

Write-Host ""

# 2. Database Migration Tests
$testResults["Migration"] = Run-Tests `
    -TestFilter "FullyQualifiedName~DatabaseMigrationTests" `
    -TestName "Database Migration Tests"

Write-Host ""

# 3. Health Check Tests
$testResults["HealthCheck"] = Run-Tests `
    -TestFilter "FullyQualifiedName~HealthCheckTests" `
    -TestName "Health Check Tests"

Write-Host ""

# 4. Performance Tests
$testResults["Performance"] = Run-Tests `
    -TestFilter "FullyQualifiedName~PerformanceTests" `
    -TestName "Performance Tests"

Write-Host ""

# 5. Docker Deployment Tests (if not skipped)
if (-not $SkipDockerTests) {
    $testResults["Docker"] = Run-Tests `
        -TestFilter "FullyQualifiedName~DockerDeploymentTests" `
        -TestName "Docker Deployment Tests"
    Write-Host ""
}

# Summary
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Test Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

$passedTests = 0
$failedTests = 0

foreach ($test in $testResults.GetEnumerator()) {
    $status = if ($test.Value) { "PASSED" } else { "FAILED" }
    $color = if ($test.Value) { "Green" } else { "Red" }
    
    Write-Host "$($test.Key): $status" -ForegroundColor $color
    
    if ($test.Value) {
        $passedTests++
    } else {
        $failedTests++
    }
}

Write-Host ""
Write-Host "Total: $($testResults.Count) test suites" -ForegroundColor Cyan
Write-Host "Passed: $passedTests" -ForegroundColor Green
Write-Host "Failed: $failedTests" -ForegroundColor Red
Write-Host ""

# Exit with appropriate code
if ($failedTests -gt 0) {
    Write-Host "Some tests failed. Please review the output above." -ForegroundColor Red
    exit 1
} else {
    Write-Host "All tests passed successfully!" -ForegroundColor Green
    exit 0
}
