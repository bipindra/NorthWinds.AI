# PowerShell script to apply the AddProductDescription migration
# Make sure to stop the application first!

Write-Host "Applying migration: AddProductDescription..." -ForegroundColor Yellow

$projectPath = "src\Northwind.Portal.Web"
$dataProjectPath = "src\Northwind.Portal.Data\Northwind.Portal.Data.csproj"

Set-Location $projectPath

try {
    dotnet ef database update --project "..\$dataProjectPath" --context NorthwindDbContext
    Write-Host "Migration applied successfully!" -ForegroundColor Green
} catch {
    Write-Host "Error applying migration: $_" -ForegroundColor Red
    Write-Host "`nAlternative: Run the SQL script manually:" -ForegroundColor Yellow
    Write-Host "src\Northwind.Portal.Data\Migrations\NorthwindDb\AddDescriptionColumn.sql" -ForegroundColor Cyan
}

Set-Location ..\..
