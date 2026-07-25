<#
.SYNOPSIS
    Linux canary: builds the cross-platform core assemblies and runs the
    net10.0 subset of GitCommands unit tests on a Linux runner.

.DESCRIPTION
    Counterpart of eng/Verify.ps1 for the Linux CI leg (change 0.4).
    Only the assemblies that target net10.0 are compiled, and
    GitCommands.Tests is run with its net10.0 leg (which excludes
    WinForms-dependent test infrastructure and ResourceManager).

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

# EnableWindowsTargeting is required on Linux because the multi-target
# test projects list net10.0-windows alongside net10.0.  MSBuild needs
# this flag to evaluate (not compile) Windows TFMs during restore.
$msbuildArgs = @(
    '-p:EnableWindowsTargeting=true'
)

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

# Core assemblies that MUST compile as net10.0.
$coreProjects = @(
    'src\app\GitExtensions.Extensibility\GitExtensions.Extensibility.csproj',
    'src\app\GitExtUtils\GitExtUtils.csproj',
    'src\app\GitCommands\GitCommands.csproj',
    'src\plugins\GitUIPluginInterfaces\GitUIPluginInterfaces.csproj',
    'src\app\GitExtensions.Avalonia\GitExtensions.Avalonia.csproj'
)

# -------------------------------------------------------------------
# 1. Build core assemblies
# -------------------------------------------------------------------
Write-Host ''
Write-Host "=== Verify-Linux: build core assemblies ===" -ForegroundColor Cyan
foreach ($proj in $coreProjects) {
    Write-Host "  $proj"
    dotnet build (Join-Path $repoRoot $proj) -c $Configuration @msbuildArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Host ''
        Write-Host "VERIFY-LINUX FAILED: build of $proj failed." -ForegroundColor Red
        exit 1
    }
}

# -------------------------------------------------------------------
# 2. Build and run GitCommands.Tests (net10.0 subset)
# -------------------------------------------------------------------
Write-Host ''
Write-Host "=== Verify-Linux: GitCommands.Tests (net10.0 subset) ===" -ForegroundColor Cyan

# The csproj has multi-target net10.0;net10.0-windows.  On net10.0 it:
#   - Sets UseWindowsForms=false
#   - Excludes ResourceManager dependency
#   - Excludes GitCommandHelpersTest.cs (needs ResourceManager.LocalizationHelpers)
#   - Excludes legacy XML serialisation tests (need System.IO.Packaging)
# CommonTestUtils (dependency) also multi-targets and excludes
# ConfigureJoinableTaskFactory + WinFormsTestHelper on net10.0.
# Tests that need JoinableTaskFactory will fail (~240 of ~3450) — expected.

$testsProj = Join-Path $repoRoot 'tests\app\UnitTests\GitCommands.Tests\GitCommands.Tests.csproj'
dotnet test $testsProj -f net10.0 -c $Configuration @msbuildArgs `
    --logger "trx;LogFileName=GitCommands.Tests.trx" `
    --results-directory $testResultsDir

$testPassed = ($LASTEXITCODE -eq 0)

$stopwatch.Stop()

Write-Host ''
Write-Host "=== Verify-Linux: summary ===" -ForegroundColor Cyan
if ($testPassed) {
    Write-Host "  PASS GitCommands.Tests (net10.0 subset)" -ForegroundColor Green
}
else {
    Write-Host "  FAIL GitCommands.Tests (net10.0 subset)" -ForegroundColor Red
}

Write-Host ''
Write-Host ("Elapsed: {0:mm\:ss}. TRX logs: {1}" -f $stopwatch.Elapsed, $testResultsDir)

if (-not $testPassed) {
    Write-Host "VERIFY-LINUX FAILED: some GitCommands.Tests failed." -ForegroundColor Red
    exit 1
}

Write-Host "VERIFY-LINUX OK: core assemblies build and GitCommands.Tests subset passes on Linux." -ForegroundColor Green
exit 0
