# E2E Test Setup and Runner Script
# This script helps set up and run E2E tests for the Meeting Management System

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("setup", "run", "run-workflows", "run-integration", "run-chromium", "run-firefox", "clean")]
    [string]$Action = "run",
    
    [Parameter(Mandatory=$false)]
    [switch]$DetailedOutput
)

$ErrorActionPreference = "Stop"
$ProjectRoot = $PSScriptRoot
$TestProject = Join-Path $ProjectRoot "tests\MeetingManagementSystem.E2ETests\MeetingManagementSystem.E2ETests.csproj"

function Write-Info {
    param([string]$Message)
    Write-Host "INFO: $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "SUCCESS: $Message" -ForegroundColor Green
}

function Write-Error-Custom {
    param([string]$Message)
    Write-Host "ERROR: $Message" -ForegroundColor Red
}

function Setup-E2ETests {
    Write-Info "Setting up E2E testing environment..."
    
    # Check Docker
    Write-Info "Checking Docker..."
    try {
        docker --version | Out-Null
        Write-Success "Docker is installed"
    }
    catch {
        Write-Error-Custom "Docker is not installed or not running. Please install Docker Desktop."
        exit 1
    }
    
    # Restore packages
    Write-Info "Restoring NuGet packages..."
    dotnet restore $TestProject
    if ($LASTEXITCODE -ne 0) {
        Write-Error-Custom "Failed to restore packages"
        exit 1
    }
    Write-Success "Packages restored"
    
    # Build test project
    Write-Info "Building E2E test project..."
    dotnet build $TestProject --configuration Debug
    if ($LASTEXITCODE -ne 0) {
        Write-Error-Custom "Failed to build test project"
        exit 1
    }
    Write-Success "Test project built"
    
    # Install Playwright browsers
    Write-Info "Installing Playwright browsers (Chromium and Firefox)..."
    $playwrightScript = Join-Path $ProjectRoot "tests\MeetingManagementSystem.E2ETests\bin\Debug\net9.0\playwright.ps1"
    & pwsh $playwrightScript install chromium firefox
    if ($LASTEXITCODE -ne 0) {
        Write-Error-Custom "Failed to install Playwright browsers"
        exit 1
    }
    Write-Success "Playwright browsers installed"
    
    Write-Success "E2E testing environment setup complete!"
    Write-Info ""
    Write-Info "You can now run tests with:"
    Write-Info "  .\run-e2e-tests.ps1 -Action run"
}

function Run-E2ETests {
    param(
        [string]$Filter = $null
    )
    
    $verbosity = if ($DetailedOutput) { "detailed" } else { "normal" }
    
    $buildArgs = @(
        "test"
        $TestProject
        "--configuration", "Debug"
        "--logger", "console;verbosity=$verbosity"
    )
    
    if ($Filter) {
        $buildArgs += "--filter"
        $buildArgs += $Filter
    }
    
    Write-Info "Running E2E tests..."
    & dotnet @buildArgs
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Tests passed!"
    }
    else {
        Write-Error-Custom "Tests failed!"
        exit 1
    }
}

function Clean-TestArtifacts {
    Write-Info "Cleaning test artifacts..."
    
    $testResultsPath = Join-Path $ProjectRoot "tests\MeetingManagementSystem.E2ETests\test-results"
    if (Test-Path $testResultsPath) {
        Remove-Item $testResultsPath -Recurse -Force
        Write-Success "Removed test results"
    }
    
    $binPath = Join-Path $ProjectRoot "tests\MeetingManagementSystem.E2ETests\bin"
    if (Test-Path $binPath) {
        Remove-Item $binPath -Recurse -Force
        Write-Success "Removed bin folder"
    }
    
    $objPath = Join-Path $ProjectRoot "tests\MeetingManagementSystem.E2ETests\obj"
    if (Test-Path $objPath) {
        Remove-Item $objPath -Recurse -Force
        Write-Success "Removed obj folder"
    }
    
    Write-Success "Cleanup complete!"
}

# Main script execution
Write-Host ""
Write-Host "============================================================" -ForegroundColor Magenta
Write-Host "   Meeting Management System - E2E Test Runner             " -ForegroundColor Magenta
Write-Host "============================================================" -ForegroundColor Magenta
Write-Host ""

switch ($Action) {
    "setup" {
        Setup-E2ETests
    }
    "run" {
        Run-E2ETests
    }
    "run-workflows" {
        Write-Info "Running Playwright workflow tests only..."
        Run-E2ETests -Filter "FullyQualifiedName~Workflows"
    }
    "run-integration" {
        Write-Info "Running HTTP integration tests only..."
        Run-E2ETests -Filter "FullyQualifiedName~Integration"
    }
    "run-chromium" {
        Write-Info "Running Chromium tests only..."
        Run-E2ETests -Filter "BrowserType=chromium"
    }
    "run-firefox" {
        Write-Info "Running Firefox tests only..."
        Run-E2ETests -Filter "BrowserType=firefox"
    }
    "clean" {
        Clean-TestArtifacts
    }
}

Write-Host ""
