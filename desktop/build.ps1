# Builds Budget Buddy.exe and puts it in the Budget Buddy folder (next to app\ and data\).
#   powershell -ExecutionPolicy Bypass -File desktop\build.ps1            build + copy the exe
#   powershell -ExecutionPolicy Bypass -File desktop\build.ps1 -Package   also make dist\BudgetBuddy-<version>-windows.zip
# Needs the .NET SDK (8 or newer). The exe itself only needs Windows 10/11.
param([switch]$Package)
$ErrorActionPreference = 'Stop'
$Desktop = $PSScriptRoot
$Root = Split-Path -Parent $Desktop

dotnet build "$Desktop\BudgetBuddy.csproj" -c Release -nologo -v quiet
if ($LASTEXITCODE) { throw 'Build failed.' }
$exe = Join-Path $Desktop 'bin\Release\net48\Budget Buddy.exe'
Copy-Item $exe $Root -Force
Write-Host "Built: $Root\Budget Buddy.exe" -ForegroundColor Green

if ($Package) {
    $version = ([xml](Get-Content "$Desktop\BudgetBuddy.csproj")).Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
    $dist = Join-Path $Root 'dist'
    $stage = Join-Path $dist 'Budget Buddy'
    if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
    New-Item -ItemType Directory -Force "$stage\app" | Out-Null
    # Only the app - never data\, backups or statements.
    Copy-Item $exe $stage
    Copy-Item "$Root\app\budget.html", "$Root\app\server.ps1" "$stage\app"
    Copy-Item "$Root\README.txt", "$Root\Start Budget Buddy.bat" $stage
    $zip = Join-Path $dist "BudgetBuddy-$version-windows.zip"
    if (Test-Path $zip) { Remove-Item $zip -Force }
    Compress-Archive -Path $stage -DestinationPath $zip
    Remove-Item $stage -Recurse -Force
    Write-Host "Packaged: $zip" -ForegroundColor Green
}
