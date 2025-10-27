# Validation script for Meeting Management System setup

Write-Host "Validating Meeting Management System Setup..." -ForegroundColor Green

# Check if required files exist
$requiredFiles = @(
    "MeetingManagementSystem.sln",
    "docker-compose.yml", 
    "Dockerfile",
    "src/MeetingManagementSystem.Web/MeetingManagementSystem.Web.csproj",
    "src/MeetingManagementSystem.Core/MeetingManagementSystem.Core.csproj",
    "src/MeetingManagementSystem.Infrastructure/MeetingManagementSystem.Infrastructure.csproj"
)

$missingFiles = @()
foreach ($file in $requiredFiles) {
    if (-not (Test-Path $file)) {
        $missingFiles += $file
    }
}

if ($missingFiles.Count -eq 0) {
    Write-Host "All required files are present" -ForegroundColor Green
} else {
    Write-Host "Missing files:" -ForegroundColor Red
    foreach ($file in $missingFiles) {
        Write-Host "  - $file" -ForegroundColor Red
    }
}

# Check Docker Compose configuration
Write-Host "Validating Docker Compose configuration..." -ForegroundColor Yellow
$composeOutput = docker compose config 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "Docker Compose configuration is valid" -ForegroundColor Green
} else {
    Write-Host "Docker Compose configuration has errors:" -ForegroundColor Red
    Write-Host $composeOutput -ForegroundColor Red
}

# Check project structure
Write-Host "Checking project structure..." -ForegroundColor Yellow
$expectedDirs = @(
    "src/MeetingManagementSystem.Web/Pages",
    "src/MeetingManagementSystem.Core/Entities", 
    "src/MeetingManagementSystem.Core/Enums",
    "src/MeetingManagementSystem.Infrastructure/Data"
)

foreach ($dir in $expectedDirs) {
    if (Test-Path $dir) {
        Write-Host "$dir exists" -ForegroundColor Green
    } else {
        Write-Host "$dir missing" -ForegroundColor Red
    }
}

Write-Host "Setup validation complete!" -ForegroundColor Green
Write-Host "To start the application:" -ForegroundColor Cyan
Write-Host "  docker compose up -d" -ForegroundColor White
Write-Host "To access the application:" -ForegroundColor Cyan  
Write-Host "  Web App: http://localhost:5000" -ForegroundColor White
Write-Host "  MailHog: http://localhost:8025" -ForegroundColor White