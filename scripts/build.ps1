$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

dotnet restore WindowsDashboard.csproj
dotnet build WindowsDashboard.csproj -c Release -r win-x64
dotnet publish WindowsDashboard.csproj -c Release -r win-x64 --self-contained false -o publish

Write-Host ""
Write-Host "Build complete: $root\publish\WindowsDashboard.exe"
