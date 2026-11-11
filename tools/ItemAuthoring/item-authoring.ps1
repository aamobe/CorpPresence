#requires -Version 7.0
[CmdletBinding(DefaultParameterSetName='Preview')]
param(
    [Parameter(Mandatory=$true)][string]$Spec,
    [string]$Repository = (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)),
    [Parameter(ParameterSetName='Preview')][switch]$Preview,
    [Parameter(Mandatory=$true,ParameterSetName='Apply')][switch]$Apply,
    [Parameter(Mandatory=$true,ParameterSetName='Apply')][string]$ExpectedPlan,
    [Parameter(Mandatory=$true,ParameterSetName='Check')][switch]$Check,
    [Parameter(Mandatory=$true,ParameterSetName='Check')][string]$GameManaged,
    [string]$Dotnet='dotnet',
    [string]$ToolCache=(Join-Path (Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSScriptRoot))) '.corp-item-authoring-cache')
)
$ErrorActionPreference='Stop'
# Build the developer executable outside the target repository, so rejected requests do not touch it.
$buildRoot=[IO.Path]::GetFullPath($ToolCache)
$targetRoot=[IO.Path]::GetFullPath($Repository).TrimEnd([IO.Path]::DirectorySeparatorChar)
if($buildRoot -eq $targetRoot -or $buildRoot.StartsWith($targetRoot+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)) {
    throw 'ToolCache must be outside the target repository.'
}
$env:UseSharedCompilation='false'
& $Dotnet build (Join-Path $PSScriptRoot 'ItemAuthoring.csproj') --artifacts-path $buildRoot --nologo --verbosity quiet
if($LASTEXITCODE -ne 0) { throw 'Authoring tool failed to compile.' }
$mode=if($Apply){'apply'}elseif($Check){'check'}else{'preview'}
$toolArgs=@((Join-Path $buildRoot 'bin/ItemAuthoring/debug/ItemAuthoring.dll'),'--spec',$Spec,'--repo',$Repository,'--mode',$mode,'--dotnet',$Dotnet)
if($Apply){$toolArgs+=@('--expected-plan',$ExpectedPlan)}
if($Check){$toolArgs+=@('--game-managed',$GameManaged)}
& $Dotnet @toolArgs
exit $LASTEXITCODE
