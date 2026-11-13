#requires -Version 7.0
param([Parameter(Mandatory)][string]$InputPath,[Parameter(Mandatory)][string]$EditsPath,[Parameter(Mandatory)][string]$OutputPath)
. (Join-Path $PSScriptRoot 'Initialize-SpriteTools.ps1')
$source=(Resolve-Path -LiteralPath $InputPath).Path
$destination=[IO.Path]::GetFullPath($OutputPath)
if($source -eq $destination -or (Test-Path -LiteralPath $destination)){throw 'Output must be new; input is never overwritten.'}
$spec=Get-Content -LiteralPath $EditsPath -Raw|ConvertFrom-Json
$bitmap=[Drawing.Bitmap]::new($source)
try{
 if($bitmap.Width -ne $spec.width -or $bitmap.Height -ne $spec.height){throw 'Input dimensions do not match edit specification.'}
 $seen=[Collections.Generic.HashSet[string]]::new()
 foreach($p in $spec.edits){
  if($p.x -lt 0 -or $p.x -ge $bitmap.Width -or $p.y -lt 0 -or $p.y -ge $bitmap.Height){throw 'Out-of-bounds edit.'}
  if(-not $seen.Add("$($p.x),$($p.y)")){throw 'Duplicate pixel edit.'}
  $c=$bitmap.GetPixel($p.x,$p.y)
  $hex='#{0:X2}{1:X2}{2:X2}{3:X2}' -f $c.R,$c.G,$c.B,$c.A
  if($hex -ne $p.before -or $p.color -notmatch '^#[0-9A-Fa-f]{8}$'){throw "Input/version mismatch at $($p.x),$($p.y)"}
 }
 foreach($p in $spec.edits){$h=$p.color.TrimStart('#');$bitmap.SetPixel($p.x,$p.y,[Drawing.Color]::FromArgb([Convert]::ToInt32($h.Substring(6,2),16),[Convert]::ToInt32($h.Substring(0,2),16),[Convert]::ToInt32($h.Substring(2,2),16),[Convert]::ToInt32($h.Substring(4,2),16)))}
 [IO.Directory]::CreateDirectory((Split-Path $destination))|Out-Null
 $bitmap.Save($destination,[Drawing.Imaging.ImageFormat]::Png)
}finally{$bitmap.Dispose()}
Write-Output $destination
