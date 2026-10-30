# Tier 2 — PR gate (local mirror of the CI `pr-gate` job, minus --since):
# build -> architecture tests -> properties (fixed seed) -> snapshots ->
# Z3 boundary-oracle gate -> anti-gaming grep -> Stryker (break 80).
# Stryker runs only if everything cheaper passed (fast->slow).
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root

dotnet build --nologo -v q
if ($LASTEXITCODE -ne 0) { exit 1 }

dotnet test --no-build --nologo --filter "FullyQualifiedName~PipelineTests.ArchitectureTests"
if ($LASTEXITCODE -ne 0) { exit 1 }

dotnet test --no-build --nologo --filter "FullyQualifiedName~PipelineTests.Properties"
if ($LASTEXITCODE -ne 0) { exit 1 }

dotnet test --no-build --nologo --filter "FullyQualifiedName~PipelineTests.Snapshots"
if ($LASTEXITCODE -ne 0) { exit 1 }

dotnet run --project gates/z3-gate --no-restore
if ($LASTEXITCODE -ne 0) { exit 1 }

pwsh -NoProfile -File scripts/anti-gaming.ps1
if ($LASTEXITCODE -ne 0) { exit 1 }

dotnet tool restore --tool-manifest .config/dotnet-tools.json | Out-Null
dotnet stryker --config-file stryker-config.json
if ($LASTEXITCODE -ne 0) { exit 1 }

Write-Output 'TIER2: PASS'
