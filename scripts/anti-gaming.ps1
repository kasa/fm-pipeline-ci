# Anti-gaming content gate — UNCONDITIONAL grep (no diff logic).
# Attempts are untracked during generation and CODEOWNERS owns the review-gated
# paths, so there is no trustworthy diff to scope to: we scan content, always.
#
# Forbidden patterns (doc 08 §2.1, §5):
#   - `stryker disable` comments (any case) — suppresses mutants
#   - `#pragma warning disable`             — hides analyzer/diagnostic feedback
#   - `[SuppressMessage`                    — suppresses analyzers
#   - `[ExcludeFromCodeCoverage]`           — hides from coverage-guided tools
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root

$scopes = @('src', 'src-attempts') | Where-Object { Test-Path $_ }
if ($scopes.Count -eq 0) {
    Write-Output 'ANTI-GAMING: clean'
    exit 0
}

$hits = @(Select-String -Path (Get-ChildItem -Path $scopes -Recurse -Include *.cs -File |
        ForEach-Object { $_.FullName }) `
        -Pattern 'stryker disable', '#pragma warning disable', '\[SuppressMessage', '\[ExcludeFromCodeCoverage' `
        -SimpleMatch:$false -CaseSensitive:$false)

if ($hits.Count -gt 0) {
    foreach ($hit in $hits) {
        Write-Output ("ANTI-GAMING: VIOLATION {0}:{1}: {2}" -f $hit.Path.Replace((Get-Location).Path + '\', ''), $hit.LineNumber, $hit.Line.Trim())
    }
    exit 1
}

Write-Output 'ANTI-GAMING: clean'
exit 0
