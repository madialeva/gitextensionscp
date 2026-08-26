<#
.SYNOPSIS
    Linux verification: builds the cross-platform solution and runs the
    GitCommands unit tests on a Linux runner.

.DESCRIPTION
    Counterpart of eng/Verify.ps1 for the Linux CI leg.
    Builds GitExtensions.slnx (the cross-platform solution: net10.0 assemblies
    plus the GitCommands.Tests test project) and runs GitCommands.Tests.

.PARAMETER Configuration
    Build configuration: Release (default) or Debug.

.EXAMPLE
    pwsh -File eng/Verify-Linux.ps1
    pwsh -File eng/Verify-Linux.ps1 -Configuration Debug
#>
[CmdletBinding()]
param(
    [ValidateSet('Release', 'Debug')]
    [string] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$testResultsDir = Join-Path $repoRoot "artifacts\$Configuration\TestResults"

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

# Cross-platform solution: the single source of truth for what compiles on
# Linux/macOS — the net10.0 assemblies plus the GitCommands.Tests test project.
$solution = Join-Path $repoRoot 'GitExtensions.slnx'

# -------------------------------------------------------------------
# 1. Build the cross-platform solution
# -------------------------------------------------------------------
Write-Host ''
Write-Host "=== Verify-Linux: build GitExtensions.slnx ===" -ForegroundColor Cyan
dotnet build $solution -c $Configuration
if ($LASTEXITCODE -ne 0) {
    Write-Host ''
    Write-Host "VERIFY-LINUX FAILED: build of the cross-platform solution failed." -ForegroundColor Red
    exit 1
}

# -------------------------------------------------------------------
# 2. Build and run GitCommands.Tests
# -------------------------------------------------------------------
Write-Host ''
Write-Host "=== Verify-Linux: GitCommands.Tests ===" -ForegroundColor Cyan

$testsProj = Join-Path $repoRoot 'tests\app\UnitTests\GitCommands.Tests\GitCommands.Tests.csproj'
dotnet test $testsProj -c $Configuration `
    --logger "trx;LogFileName=GitCommands.Tests.trx" `
    --results-directory $testResultsDir

$testPassed = ($LASTEXITCODE -eq 0)

$stopwatch.Stop()

Write-Host ''
Write-Host "=== Verify-Linux: summary ===" -ForegroundColor Cyan
if ($testPassed) {
    Write-Host "  PASS GitCommands.Tests" -ForegroundColor Green
}
else {
    Write-Host "  FAIL GitCommands.Tests" -ForegroundColor Red
}

Write-Host ''
Write-Host ("Elapsed: {0:mm\:ss}. TRX logs: {1}" -f $stopwatch.Elapsed, $testResultsDir)

if (-not $testPassed) {
    Write-Host "VERIFY-LINUX FAILED: some GitCommands.Tests failed." -ForegroundColor Red
    exit 1
}

Write-Host "VERIFY-LINUX OK: cross-platform solution builds and GitCommands.Tests passes on Linux." -ForegroundColor Green
exit 0
