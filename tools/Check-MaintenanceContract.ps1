#requires -Version 7.0
[CmdletBinding()]
param([Parameter(Mandatory)][string]$GameManaged,[Parameter(Mandatory)][string]$Dotnet,[Parameter(Mandatory)][string]$Python)
$ErrorActionPreference='Stop'
$repo=Split-Path -Parent $PSScriptRoot
$root=Join-Path $repo ('artifacts/maintenance-contract-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $root | Out-Null
$runner=Join-Path $PSScriptRoot 'Check-Maintenance.ps1'
if(!(Test-Path -LiteralPath $runner)){throw 'Maintenance runner not implemented'}
$pwsh=(Get-Process -Id $PID).Path
function Reject([string]$Name,[string]$Sdk,[string]$Managed,[string]$Expected){
 $log=Join-Path $root ($Name+'.log')
 & $pwsh -NoProfile -File $runner -GameManaged $Managed -Dotnet $Sdk -Python $Python -OutputRoot (Join-Path $root $Name) *> $log
 $code=$LASTEXITCODE
 $text=Get-Content -LiteralPath $log -Raw
 if($code -eq 0 -or $text.Contains('ALL CHECKS PASSED') -or !$text.Contains($Expected)){throw "Wrong rejection $Name (exit=$code): $text"}
 Write-Output "PASS $Name exits nonzero without success message"
}
Reject 'missing-sdk' (Join-Path $root 'no-dotnet.exe') $GameManaged 'SDK'
Reject 'missing-managed' $Dotnet (Join-Path $root 'no-managed') 'GameManaged'
$fake=Join-Path $root 'fake-dotnet.cmd'
Set-Content -LiteralPath $fake -Encoding ascii -Value '@echo off
if "%~1"=="--version" (
 echo 8.0.426
 exit /b 0
)
exit /b 37'
Reject 'failed-child' $fake $GameManaged 'exit=37'
Write-Output 'PASS maintenance runner failure contract'
