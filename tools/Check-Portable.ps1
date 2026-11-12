#requires -Version 7.0
[CmdletBinding()]
param(
    [string]$Dotnet = 'dotnet',
    [string]$OutputRoot
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
try { $sdk = (Get-Command $Dotnet -ErrorAction Stop).Source }
catch { throw "SDK not found: $Dotnet" }
$version = & $sdk --version
if ($LASTEXITCODE -ne 0 -or $version -notmatch '^(\d+)\.' -or [int]$Matches[1] -lt 8) {
    throw '.NET SDK 8 or newer required'
}
if (!$OutputRoot) { $OutputRoot = Join-Path $repo 'artifacts/portable-checks' }
$runRoot = Join-Path ([IO.Path]::GetFullPath($OutputRoot)) ([Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $runRoot -Force | Out-Null
$pwsh = (Get-Process -Id $PID).Path

function Invoke-PortableCheck([string]$Name, [string]$Executable, [string[]]$Arguments) {
    $log = Join-Path $runRoot ($Name + '.log')
    & $Executable @Arguments *> $log
    $code = $LASTEXITCODE
    Get-Content -LiteralPath $log | Write-Output
    if ($code -ne 0) { throw "$Name failed: exit=$code; log=$log" }
    Write-Output "PASS $Name"
}

Push-Location $repo
try {
    Invoke-PortableCheck 'repository' $pwsh @('-NoProfile', '-File', (Join-Path $PSScriptRoot 'Check-Repository.ps1'))
    foreach ($name in @('ItemAuthoringChecks', 'ItemTagChecks', 'RuntimeLifetimeChecks', 'ExtensionChecks', 'HostChecks', 'SpacerModValuesChecks', 'WasteBatteryChecks')) {
        $checkRoot = Join-Path $runRoot $name
        Invoke-PortableCheck "$name-build" $sdk @('build', (Join-Path $PSScriptRoot "checks/$name/Checks.csproj"),
            '--artifacts-path', $checkRoot, '-p:UseSharedCompilation=false', '-warnaserror')
        Invoke-PortableCheck $name $sdk @((Join-Path $checkRoot 'bin/Checks/debug/Checks.dll'))
    }
    Write-Output "PORTABLE CHECKS PASSED; logs: $runRoot"
    Write-Output 'Static and substitute checks only; full game-SDK build, permission scan and gameplay checks remain separate.'
}
finally { Pop-Location }
