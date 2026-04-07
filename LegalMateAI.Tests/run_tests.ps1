# run_tests.ps1

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   Running LegalMate Tests" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Restore packages
Write-Host "Restoring packages..." -ForegroundColor Yellow
dotnet restore

Write-Host ""

# Build the test project
Write-Host "Building test project..." -ForegroundColor Yellow
dotnet build --configuration Release --no-restore

Write-Host ""

# Run tests
Write-Host "Running tests..." -ForegroundColor Yellow
dotnet test --verbosity normal --no-build --configuration Release

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   Tests Completed!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan