# Task Manager - Database Reset Script
# PowerShell version

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   Task Manager - Database Reset" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "WARNING: This will DELETE ALL data!" -ForegroundColor Red
Write-Host ""

$confirm = Read-Host "Are you sure? (yes/no)"

if ($confirm -ne "yes") {
    Write-Host ""
    Write-Host "Reset cancelled." -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit
}

Write-Host ""
Write-Host "Step 1: Resetting database..." -ForegroundColor Yellow

# Get MySQL credentials
$mysqlUser = Read-Host "MySQL Username (default: root)"
if ([string]::IsNullOrWhiteSpace($mysqlUser)) {
    $mysqlUser = "root"
}

# Reset database
$resetResult = Get-Content database\reset_database.sql | & mysql -u $mysqlUser -p 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "ERROR: Failed to reset database!" -ForegroundColor Red
    Write-Host "Make sure MySQL is running and credentials are correct." -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "✓ Database reset successful" -ForegroundColor Green
Write-Host ""
Write-Host "Step 2: Creating tables and default data..." -ForegroundColor Yellow

# Create tables
$migrateResult = Get-Content database\migrate_simple.sql | & mysql -u $mysqlUser -p 2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "ERROR: Failed to create tables!" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "✓ Tables created successfully" -ForegroundColor Green
Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "   Database Reset Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Default Admin Account:" -ForegroundColor Cyan
Write-Host "  Email: admin@taskmanager.com" -ForegroundColor White
Write-Host "  Password: Admin@123" -ForegroundColor White
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$start = Read-Host "Start the application now? (yes/no)"

if ($start -eq "yes") {
    Write-Host ""
    Write-Host "Starting application..." -ForegroundColor Yellow
    
    Set-Location TaskManagerMVC
    Start-Process powershell -ArgumentList "-NoExit", "-Command", "dotnet run"
    
    Write-Host ""
    Write-Host "✓ Application starting in new window..." -ForegroundColor Green
    Write-Host "✓ Open browser: http://localhost:5000/Account/Login" -ForegroundColor Green
}

Write-Host ""
Read-Host "Press Enter to exit"
