<#
.SYNOPSIS
    Verifies the repository: builds the cross-platform solution and runs the
    cross-platform unit tests.

.DESCRIPTION
    Single source of truth for "the repo is verified" on Windows. Used locally
    (run it at any time, from any directory) and by CI
    (.github/workflows/fork-ci.yml), which is a thin wrapper around this script.

    Scope: builds GitExtensions.slnx (the cross-platform solution) and runs the
    cross-platform unit test projects on net10.0. The WinForms solution
    (GitExtensions.WinForms.slnx) and the Windows-only test projects
    (GitUI.Tests, ResourceManager.Tests, BugReporter.Tests, plugin tests) are
    out of scope: they are kept as reference for the Avalonia port and are not
    validated. Integration tests (tests/app/IntegrationTests) remain excluded.

    TRX result files are written to artifacts/<Configuration>/TestResults/.

.PARAMETER Configuration
    Build configuration: Release (default) or Debug. Use Debug locally to reuse
    your incremental development build.

.EXAMPLE
    .\eng\Verify.ps1
    .\eng\Verify.ps1 -Configuration Debug
#>
[CmdletBinding()]
param(
    [ValidateSet('Release', 'Debug')]
    [string] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

# Resolve paths relative to this script so it works from any current directory.
$repoRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repoRoot 'GitExtensions.slnx'
$testResultsDir = Join-Path $repoRoot "artifacts\$Configuration\TestResults"

# Cross-platform unit test projects (net10.0). Windows-only test projects and
# integration tests are excluded by design (see the header).
$testProjects = @(
    'tests\app\UnitTests\GitCommands.Tests\GitCommands.Tests.csproj'
)

$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

Write-Host ''
Write-Host "=== Verify: build ($Configuration) ===" -ForegroundColor Cyan
dotnet build $solution -c $Configuration
if ($LASTEXITCODE -ne 0) {
    Write-Host ''
    Write-Host "VERIFY FAILED: build failed (exit code $LASTEXITCODE). Tests were not run." -ForegroundColor Red
    exit 1
}

Write-Host ''
Write-Host "=== Verify: unit tests ($($testProjects.Count) projects) ===" -ForegroundColor Cyan

$results = @()
foreach ($project in $testProjects) {
    $name = [System.IO.Path]::GetFileNameWithoutExtension($project)
    Write-Host ''
    Write-Host "--- $name" -ForegroundColor Cyan
    # --no-build: the solution build above already compiled the test project.
    dotnet test (Join-Path $repoRoot $project) -c $Configuration --no-build `
        --logger "trx;LogFileName=$name.trx" `
        --results-directory $testResultsDir
    $results += [pscustomobject]@{
        Project = $name
        Passed  = ($LASTEXITCODE -eq 0)
    }
}

$stopwatch.Stop()
$failed = @($results | Where-Object { -not $_.Passed })

Write-Host ''
Write-Host '=== Verify: summary ===' -ForegroundColor Cyan
foreach ($result in $results) {
    if ($result.Passed) {
        Write-Host "  OK   $($result.Project)" -ForegroundColor Green
    }
    else {
        Write-Host "  FAIL $($result.Project)" -ForegroundColor Red
    }
}
Write-Host ''
Write-Host ("Elapsed: {0:mm\:ss}. TRX logs: {1}" -f $stopwatch.Elapsed, $testResultsDir)

if ($failed.Count -gt 0) {
    Write-Host "VERIFY FAILED: $($failed.Count) of $($results.Count) test projects failed." -ForegroundColor Red
    exit 1
}

Write-Host "VERIFY OK: build clean and all $($results.Count) unit test projects passed." -ForegroundColor Green
exit 0
