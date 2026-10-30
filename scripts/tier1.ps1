# Tier 1 — inner loop: build (warnings-as-errors via Directory.Build.props),
# architecture tests, property tests. Fast feedback per LLM attempt.
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root

dotnet build --nologo -v q
if ($LASTEXITCODE -ne 0) { exit 1 }

dotnet test --no-build --nologo --filter "FullyQualifiedName~PipelineTests.ArchitectureTests"
if ($LASTEXITCODE -ne 0) { exit 1 }

dotnet test --no-build --nologo --filter "FullyQualifiedName~PipelineTests.Properties"
if ($LASTEXITCODE -ne 0) { exit 1 }

Write-Output "TIER1: PASS"
