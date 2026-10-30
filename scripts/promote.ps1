param(
    [Parameter(Mandatory = $true)][int]$Attempt
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$src = Join-Path $root "src-attempts/attempt_$Attempt/DiscountPolicy.cs"
$dst = Join-Path $root 'src/DiscountPolicy/DiscountPolicy.cs'
if (-not (Test-Path $src)) { throw "attempt file not found: $src" }
Copy-Item $src $dst -Force
Write-Output "PROMOTED attempt_$Attempt -> src/DiscountPolicy/DiscountPolicy.cs"
