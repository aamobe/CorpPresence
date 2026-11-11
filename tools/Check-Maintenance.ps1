#requires -Version 7.0
[CmdletBinding()]
param(
 [Parameter(Mandatory)][string]$GameManaged,
 [Parameter(Mandatory)][string]$Dotnet,
 [Parameter(Mandatory)][string]$Python,
 [string]$OutputRoot
)
$ErrorActionPreference='Stop'
$repo=Split-Path -Parent $PSScriptRoot
try {$sdk=(Get-Command $Dotnet -ErrorAction Stop).Source} catch {throw "SDK not found: $Dotnet"}
$sdkVersion=& $sdk --version
if($LASTEXITCODE -ne 0 -or $sdkVersion -notmatch '^(\d+)\.' -or [int]$Matches[1] -lt 8){throw '.NET SDK 8 or newer required'}
if(!(Test-Path -LiteralPath $GameManaged -PathType Container)){throw 'GameManaged directory not found'}
$managed=(Resolve-Path -LiteralPath $GameManaged).Path
foreach($file in @('Assembly-CSharp.dll','0Harmony.dll','Newtonsoft.Json.dll')){
 if(!(Test-Path -LiteralPath (Join-Path $managed $file))){throw "GameManaged missing $file"}
}
if(!(Get-ChildItem -LiteralPath $managed -Filter 'Unity*.dll' -File | Select-Object -First 1)){throw 'GameManaged missing Unity references'}
try {$pythonExe=(Get-Command $Python -ErrorAction Stop).Source} catch {throw 'Python interpreter not found'}
if(!$OutputRoot){$OutputRoot=Join-Path $repo 'artifacts/maintenance-checks'}
$runRoot=Join-Path ([IO.Path]::GetFullPath($OutputRoot)) ([DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss')+'-'+[Guid]::NewGuid().ToString('N').Substring(0,8))
New-Item -ItemType Directory -Path $runRoot -Force | Out-Null
$results=[Collections.Generic.List[object]]::new()
$pwsh=(Get-Process -Id $PID).Path
function Invoke-Check([string]$Name,[string]$Layer,[string]$Executable,[string[]]$Arguments,[int]$Expected=0,[string]$ExpectedText=''){
 $log=Join-Path $runRoot ($Name+'.log')
 Write-Output "RUN [$Layer] $Name"
 $global:LASTEXITCODE=0
 & $Executable @Arguments *> $log
 $code=$LASTEXITCODE
 $result=[ordered]@{name=$Name;layer=$Layer;command=$Executable;arguments=$Arguments;exitCode=$code;expectedExit=$Expected;log=$log}
 $results.Add($result)
 $results | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $runRoot 'results.json') -Encoding utf8NoBOM
 if($code -ne $Expected -or ($ExpectedText -and !(Select-String -LiteralPath $log -Pattern $ExpectedText -SimpleMatch -Quiet))){
  Get-Content -LiteralPath $log -Tail 14 | Write-Output
  throw "$Name failed: exit=$code expected=$Expected"
 }
 Write-Output "PASS [$Layer] $Name exit=$code"
}
Push-Location $repo
try {
 Invoke-Check 'repository' 'static' $pwsh @('-NoProfile','-File',(Join-Path $PSScriptRoot 'Check-Repository.ps1'))
 $regularRoot=Join-Path $runRoot 'regular'
 $maintenanceRoot=Join-Path $runRoot 'maintenance'
 $project=Join-Path $repo 'src/CorpPresence.csproj'
 Invoke-Check 'release-build' 'compile' $sdk @('build',$project,'-c','Release','--artifacts-path',$regularRoot,"-p:GameManaged=$managed",'-p:Maintenance=false','-warnaserror')
 Invoke-Check 'maintenance-build' 'compile' $sdk @('build',$project,'-c','Release','--artifacts-path',$maintenanceRoot,"-p:GameManaged=$managed",'-p:Maintenance=true','-warnaserror')
 $regular=Join-Path $regularRoot 'bin/CorpPresence/release/CorpPresence.dll'
 $maintenance=Join-Path $maintenanceRoot 'bin/CorpPresence/release/CorpPresence.dll'
 foreach($name in @('MaintenanceChecks','ExtensionChecks','PaymentChecks','OrderChecks','RuntimeLifetimeChecks','SaveStateChecks','MangoGiftChecks','ItemChecks')){
  $checkRoot=Join-Path $runRoot $name
  Invoke-Check "$name-build" 'compile-test' $sdk @('build',(Join-Path $PSScriptRoot "$name/Checks.csproj"),'--artifacts-path',$checkRoot,"-p:GameManaged=$managed")
  $checkArgs=@((Join-Path $checkRoot 'bin/Checks/debug/Checks.dll'))
  if($name -eq 'ItemChecks'){$checkArgs+= $regular}
  Invoke-Check $name 'substitute' $sdk $checkArgs
 }
 $boundaryRoot=Join-Path $runRoot 'build-boundary'
 Invoke-Check 'build-boundary-build' 'compile-test' $sdk @('build',(Join-Path $PSScriptRoot 'MaintenanceBuildChecks/Checks.csproj'),'--artifacts-path',$boundaryRoot)
 Invoke-Check 'build-boundary' 'metadata' $sdk @((Join-Path $boundaryRoot 'bin/Checks/debug/Checks.dll'),$regular,$maintenance)
 foreach($flag in @('WaterTrace','LocalTestFunds')){
  Invoke-Check "conflict-$flag" 'compile-negative' $sdk @('build',$project,'-c','Release','--artifacts-path',(Join-Path $runRoot "conflict-$flag"),"-p:GameManaged=$managed",'-p:Maintenance=true',"-p:${flag}=true") 1 'Maintenance cannot be combined'
 }
 Invoke-Check 'restoration' 'substitute' $pythonExe @((Join-Path $PSScriptRoot 'RestorationChecks/State/run.py'),'--dotnet',$sdk,'--artifacts-path',(Join-Path $runRoot 'restoration'))
 Invoke-Check 'localization' 'substitute-and-native-parser' $pwsh @('-NoProfile','-File',(Join-Path $PSScriptRoot 'Check-Localization.ps1'),'-GameManaged',$managed,'-Dotnet',$sdk,'-CompiledAssembly',$regular)
 $scan=Join-Path $runRoot 'scan'
 New-Item -ItemType Directory -Path $scan | Out-Null
 Copy-Item -LiteralPath $regular -Destination (Join-Path $scan 'CorpPresence.dll')
 Copy-Item -LiteralPath (Join-Path $repo 'manifest.xml') -Destination $scan
 Invoke-Check 'permissions' 'native-scanner' $pwsh @('-NoProfile','-File',(Join-Path $PSScriptRoot 'Check-ModPermissions.ps1'),'-GameManaged',$managed,'-ModFolder',$scan,'-Dotnet',$sdk,'-RequireNone')
 Write-Output "ALL CHECKS PASSED: $($results.Count) checks; evidence: $runRoot"
 Write-Output 'No game launch, deployment, package, upload, merge or push. Gameplay remains user-manual.'
}
finally {Pop-Location}
