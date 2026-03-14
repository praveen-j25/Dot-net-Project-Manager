# Diagnose Login Issue for admin@taskmanager.com
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   Login Diagnostic Tool" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 1: Check if MySQL is running
Write-Host "Step 1: Checking MySQL service..." -ForegroundColor Yellow
$mysqlService = Get-Service -Name "MySQL*" -ErrorAction SilentlyContinue
if ($mysqlService) {
    Write-Host "✓ MySQL service found: $($mysqlService.Name) - Status: $($mysqlService.Status)" -ForegroundColor Green
} else {
    Write-Host "✗ MySQL service not found or not running" -ForegroundColor Red
}
Write-Host ""

# Step 2: Get MySQL credentials
Write-Host "Step 2: MySQL Connection" -ForegroundColor Yellow
$mysqlUser = Read-Host "MySQL Username (default: root)"
if ([string]::IsNullOrWhiteSpace($mysqlUser)) {
    $mysqlUser = "root"
}
Write-Host ""

# Step 3: Check admin user in database
Write-Host "Step 3: Checking admin user in database..." -ForegroundColor Yellow
Write-Host "Running SQL query..." -ForegroundColor Gray

$sqlQuery = @"
USE task_manager_db;
SELECT 
    id,
    email,
    first_name,
    last_name,
    role_id,
    is_active,
    is_verified,
    password as password_hash
FROM users 
WHERE email = 'admin@taskmanager.com';
"@

# Save query to temp file
$sqlQuery | Out-File -FilePath "temp_check.sql" -Encoding UTF8

# Run query
$result = & mysql -u $mysqlUser -p --table < temp_check.sql 2>&1

if ($LASTEXITCODE -eq 0) {
    Write-Host $result
    Write-Host ""
} else {
    Write-Host "✗ Failed to query database" -ForegroundColor Red
    Write-Host $result -ForegroundColor Red
    Remove-Item "temp_check.sql" -ErrorAction SilentlyContinue
    Read-Host "Press Enter to exit"
    exit 1
}

Remove-Item "temp_check.sql" -ErrorAction SilentlyContinue

# Step 4: Test BCrypt hash
Write-Host "Step 4: Testing BCrypt password hash..." -ForegroundColor Yellow
Write-Host "Running C# script to verify password..." -ForegroundColor Gray
Write-Host ""

if (Test-Path "test_bcrypt_hash.csx") {
    try {
        $bcryptResult = & dotnet script test_bcrypt_hash.csx 2>&1
        Write-Host $bcryptResult
    } catch {
        Write-Host "✗ Failed to run BCrypt test" -ForegroundColor Red
        Write-Host "Error: $_" -ForegroundColor Red
    }
} else {
    Write-Host "✗ test_bcrypt_hash.csx not found" -ForegroundColor Red
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   Diagnostic Summary" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Expected Credentials:" -ForegroundColor Yellow
Write-Host "  Email: admin@taskmanager.com" -ForegroundColor White
Write-Host "  Password: Admin@123" -ForegroundColor White
Write-Host ""
Write-Host "Common Issues:" -ForegroundColor Yellow
Write-Host "  1. Wrong password hash in database" -ForegroundColor White
Write-Host "  2. User is_active = 0 (inactive)" -ForegroundColor White
Write-Host "  3. User is_verified = 0 (not verified)" -ForegroundColor White
Write-Host "  4. Database connection string incorrect" -ForegroundColor White
Write-Host ""
Write-Host "Solutions:" -ForegroundColor Yellow
Write-Host "  1. Run: .\reset_and_start.bat" -ForegroundColor White
Write-Host "  2. Or run: .\Reset-Database.ps1" -ForegroundColor White
Write-Host ""

Read-Host "Press Enter to exit"
