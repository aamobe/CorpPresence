param()
$ErrorActionPreference='Stop'
$checkDir=$PSScriptRoot
$projectDir=(Resolve-Path (Join-Path $checkDir '../..')).Path
$sdkVersion=(& dotnet --version).Trim()
$cscPath=Join-Path (Split-Path (Get-Command dotnet).Source) "sdk/$sdkVersion/Roslyn/bincore/csc.dll"
$frameworkDir='C:/Windows/Microsoft.NET/Framework64/v4.0.30319'
$harmonyPath='E:/steam/steamapps/common/Probably Stolen/Probably Stolen_Data/Managed/0Harmony.dll'
$outputDir=Join-Path $checkDir 'framework'
New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
& dotnet $cscPath /nologo /target:exe /optimize+ "/out:$outputDir/Checks.exe" "/reference:$frameworkDir/mscorlib.dll" "/reference:$frameworkDir/System.dll" "/reference:$frameworkDir/System.Core.dll" "/reference:$harmonyPath" "$checkDir/Program.cs" "$projectDir/src/CorpPresence/Integration/NeonCompatibility.cs"
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
Copy-Item -LiteralPath $harmonyPath -Destination "$outputDir/0Harmony.dll" -Force
& "$outputDir/Checks.exe"
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
& dotnet $cscPath /nologo /target:exe /optimize+ /define:NEON_ABSENT "/out:$outputDir/ChecksAbsent.exe" "/reference:$frameworkDir/mscorlib.dll" "/reference:$frameworkDir/System.dll" "/reference:$frameworkDir/System.Core.dll" "/reference:$harmonyPath" "$checkDir/Program.cs" "$projectDir/src/CorpPresence/Integration/NeonCompatibility.cs"
if($LASTEXITCODE -ne 0){exit $LASTEXITCODE}
& "$outputDir/ChecksAbsent.exe"
exit $LASTEXITCODE
