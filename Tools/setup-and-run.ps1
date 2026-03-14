# Task Manager MVC - Automated Setup and Run Script
# This script will check prerequisites, setup database, and run the application

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Task Manager MVC - Setup & Run Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Function to check if command exists
function Test-CommandExists {
    param($command)
    $null = Get-Command $command -ErrorAction SilentlyContinue
    return $?
}

# Step 1: Check .NET SDK
Write-Host "[1/7] Checking .NET SDK..." -ForegroundColor Yellow
if (Test-CommandExists dotnet) {
    $dotnetVersion = dotnet --version 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ .NET SDK found: $dotnetVersion" -ForegroundColor Green
    } else {
        Write-Host "✗ .NET SDK not found!" -ForegroundColor Red
        Write-Host "Please install .NET 8 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
        Write-Host "After installation, restart PowerShell and run this script again." -ForegroundColor Yellow
        exit 1
    }
} else {
    Write-Host "✗ .NET SDK not found!" -ForegroundColor Red
    Write-Host "Please install .NET 8 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
    exit 1
}

# Step 2: Check MySQL
Write-Host ""
Write-Host "[2/7] Checking MySQL..." -ForegroundColor Yellow
if (Test-CommandExists mysql) {
    Write-Host "✓ MySQL client found" -ForegroundColor Green
    
    # Check MySQL service
    $mysqlService = Get-Service -Name MySQL* -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($mysqlService) {
        if ($mysqlService.Status -eq "Running") {
            Write-Host "✓ MySQL service is running" -ForegroundColor Green
        } else {
            Write-Host "! MySQL service is not running. Attempting to start..." -ForegroundColor Yellow
            try {
                Start-Service $mysqlService.Name
                Write-Host "✓ MySQL service started" -ForegroundColor Green
            } catch {
                Write-Host "✗ Failed to start MySQL service. Please start it manually." -ForegroundColor Red
                exit 1
            }
        }
    } else {
        Write-Host "! MySQL service not found. Make sure MySQL is installed." -ForegroundColor Yellow
    }
} else {
    Write-Host "✗ MySQL client not found!" -ForegroundColor Red
    Write-Host "Please install MySQL and add it to PATH" -ForegroundColor Yellow
    exit 1
}

# Step 3: Restore NuGet Packages
Write-Host ""
Write-Host "[3/7] Restoring NuGet packages..." -ForegroundColor Yellow
Set-Location -Path "TaskManagerMVC"
$restoreOutput = dotnet restore 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Packages restored successfully" -ForegroundColor Green
} else {
    Write-Host "✗ Failed to restore packages" -ForegroundColor Red
    Write-Host $restoreOutput
    Set-Location -Path ".."
    exit 1
}
Set-Location -Path ".."

# Step 4: Check Database Connection
Write-Host ""
Write-Host "[4/7] Checking database connection..." -ForegroundColor Yellow
Write-Host "Please enter your MySQL root password (default: root):" -ForegroundColor Cyan
$mysqlPassword = Read-Host -AsSecureString
$mysqlPasswordPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($mysqlPassword))

# Test connection
$testQuery = "SELECT 1;"
$testResult = echo $testQuery | mysql -u root -p$mysqlPasswordPlain 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ MySQL connection successful" -ForegroundColor Green
} else {
    Write-Host "✗ MySQL connection failed. Please check your password." -ForegroundColor Red
    exit 1
}

# Step 5: Create Database
Write-Host ""
Write-Host "[5/7] Creating database..." -ForegroundColor Yellow
$createDbQuery = "CREATE DATABASE IF NOT EXISTS task_manager;"
echo $createDbQuery | mysql -u root -p$mysqlPasswordPlain 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Database 'task_manager' created/verified" -ForegroundColor Green
} else {
    Write-Host "✗ Failed to create database" -ForegroundColor Red
    exit 1
}

# Step 6: Execute Stored Procedures
Write-Host ""
Write-Host "[6/7] Creating stored procedures..." -ForegroundColor Yellow
if (Test-Path "database/all_stored_procedures.sql") {
    mysql -u root -p$mysqlPasswordPlain task_manager < database/all_stored_procedures.sql 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Stored procedures created successfully" -ForegroundColor Green
        
        # Verify procedures
        $countQuery = "SELECT COUNT(*) FROM information_schema.ROUTINES WHERE ROUTINE_SCHEMA = 'task_manager' AND ROUTINE_TYPE = 'PROCEDURE';"
        $procCount = echo $countQuery | mysql -u root -p$mysqlPasswordPlain -N 2>&1
        Write-Host "  → $procCount stored procedures created" -ForegroundColor Cyan
    } else {
        Write-Host "✗ Failed to create stored procedures" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "✗ Stored procedures SQL file not found!" -ForegroundColor Red
    exit 1
}

# Step 7: Build and Run
Write-Host ""
Write-Host "[7/7] Building and running the application..." -ForegroundColor Yellow
Set-Location -Path "TaskManagerMVC"

# Build
Write-Host "Building project..." -ForegroundColor Cyan
$buildOutput = dotnet build 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Build successful" -ForegroundColor Green
} else {
    Write-Host "✗ Build failed" -ForegroundColor Red
    Write-Host $buildOutput
    Set-Location -Path ".."
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Setup completed successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Starting the application..." -ForegroundColor Cyan
Write-Host ""
Write-Host "Once started, open your browser to:" -ForegroundColor Yellow
Write-Host "  → https://localhost:5001" -ForegroundColor Cyan
Write-Host "  → http://localhost:5000" -ForegroundColor Cyan
Write-Host ""
Write-Host "Default Admin Login:" -ForegroundColor Yellow
Write-Host "  Email: admin@taskmanager.com" -ForegroundColor Cyan
Write-Host "  Password: Admin@123" -ForegroundColor Cyan
Write-Host ""
Write-Host "Press Ctrl+C to stop the application" -ForegroundColor Yellow
Write-Host ""

# Run the application
dotnet run

Set-Location -Path ".."
