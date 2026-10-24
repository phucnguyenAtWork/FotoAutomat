$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$tempRoot = Join-Path ([IO.Path]::GetTempPath()) ('fotoautomat-inventory-' + [guid]::NewGuid().ToString('N'))
$inputPath = Join-Path $tempRoot 'input'
$outputPath = Join-Path $tempRoot 'report'
$null = New-Item -ItemType Directory -Path $inputPath
$image = [Drawing.Bitmap]::new(12, 8)
try { $image.Save((Join-Path $inputPath 'a.png'), [Drawing.Imaging.ImageFormat]::Png) }
finally { $image.Dispose() }
Copy-Item -LiteralPath (Join-Path $inputPath 'a.png') -Destination (Join-Path $inputPath 'b.png')
[IO.File]::WriteAllText((Join-Path $inputPath 'broken.jpg'), 'not an image')
[IO.File]::WriteAllText((Join-Path $inputPath 'sample.cr3'), 'placeholder raw, inventory only')
[IO.File]::WriteAllText((Join-Path $inputPath 'notes.txt'), 'not an image')
$before = (Get-FileHash (Join-Path $inputPath 'a.png')).Hash
& "$PSScriptRoot/Inventory.ps1" -InputDirectory $inputPath -OutputDirectory $outputPath
$result = Get-Content -Raw -LiteralPath (Join-Path $outputPath 'manifest.json') | ConvertFrom-Json
function Assert($condition, $message) { if (-not $condition) { throw $message } }
Assert ($result.assets.Count -eq 4) 'Asset count'
$a = $result.assets | Where-Object path -eq 'a.png'
Assert ($a.width -eq 12 -and $a.height -eq 8 -and $a.decode_status -eq 'decoded_first_frame') 'Raster decode'
Assert (($result.assets | Where-Object path -eq 'b.png').duplicate_of -eq 'a.png') 'Duplicate link'
Assert (($result.assets | Where-Object path -eq 'broken.jpg').decode_status -eq 'decode_failed') 'Corrupt file'
Assert (($result.assets | Where-Object path -eq 'sample.cr3').decode_status -eq 'unsupported_requires_decoder') 'RAW must not pass decode'
Assert (@($result.assets | Where-Object commercial_training_allowed).Count -eq 0) 'No implicit rights approval'
Assert ($before -eq (Get-FileHash (Join-Path $inputPath 'a.png')).Hash) 'Source must remain unchanged'
$refused = $false
try { & "$PSScriptRoot/Inventory.ps1" -InputDirectory $inputPath -OutputDirectory $outputPath } catch { $refused = $true }
Assert $refused 'Existing reports must not be overwritten'
$refused = $false
try { & "$PSScriptRoot/Inventory.ps1" -InputDirectory $inputPath -OutputDirectory (Join-Path $inputPath 'output') } catch { $refused = $true }
Assert $refused 'Output inside input must be refused'
Write-Output "PASS: decode, duplicate, corrupt, RAW, rights, original preservation and output guards. Synthetic fixtures: $tempRoot"
