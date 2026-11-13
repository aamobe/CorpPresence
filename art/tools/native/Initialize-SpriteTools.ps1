#requires -Version 7.0
$ErrorActionPreference='Stop'
if(-not $IsWindows){throw 'These System.Drawing tools require Windows and PowerShell 7.'}
Add-Type -AssemblyName System.Drawing
$references=@(Get-ChildItem -LiteralPath (Join-Path $PSHOME 'ref') -Filter '*.dll' | ForEach-Object FullName)
$references+=@([Drawing.Bitmap].Assembly.Location)
$references+=@(Get-ChildItem -LiteralPath $PSHOME -Filter 'System.Private.Windows.*.dll' | ForEach-Object FullName)
foreach($name in @('SpriteProbe','SpriteRecover')){
 if(-not ($name -as [type])){Add-Type -ReferencedAssemblies $references -TypeDefinition ([IO.File]::ReadAllText((Join-Path $PSScriptRoot "$name.cs")))}
}
