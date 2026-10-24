param(
    [Parameter(Mandatory=$true)][string]$InputDirectory,
    [Parameter(Mandatory=$true)][string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = (Resolve-Path -LiteralPath $InputDirectory).Path.TrimEnd('\')
if (-not (Test-Path -LiteralPath $root -PathType Container)) { throw 'Input must be a directory.' }
if ((Get-Item -LiteralPath $root).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Input must not be a link.' }
$output = [IO.Path]::GetFullPath($OutputDirectory)
if ($output.Equals($root, [StringComparison]::OrdinalIgnoreCase) -or $output.StartsWith($root + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Output must be outside input.' }
if (Test-Path -LiteralPath $output) { throw 'Output already exists. Use a new directory to preserve previous reports.' }

function Get-ImageProperty($image, [int]$id, [bool]$asText = $false) {
    if ($image.PropertyIdList -notcontains $id) { return $null }
    $value = $image.GetPropertyItem($id).Value
    if ($asText) { return [Text.Encoding]::ASCII.GetString($value).Trim([char]0).Trim() }
    if ($value.Length -ge 2) { return [int][BitConverter]::ToUInt16($value, 0) }
    return $null
}

# Traverse explicitly so junctions cannot silently pull unrelated files into a dataset.
$pending = [Collections.Generic.Stack[string]]::new()
$pending.Push($root)
$files = [Collections.Generic.List[object]]::new()
$skipped = [Collections.Generic.List[object]]::new()
while ($pending.Count -gt 0) {
    $dir = $pending.Pop()
    foreach ($item in Get-ChildItem -LiteralPath $dir -Force | Sort-Object Name) {
        if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) {
            $skipped.Add(@{ path = $item.FullName.Substring($root.Length + 1); reason = 'link_not_followed' })
        } elseif ($item.PSIsContainer) { $pending.Push($item.FullName) }
        else { $files.Add($item) }
    }
}
$raster = @('.jpg','.jpeg','.png','.bmp','.tif','.tiff','.gif')
$raw = @('.cr2','.cr3','.nef','.arw','.raf','.dng','.orf','.rw2','.pef','.heic','.webp')
$assets = [Collections.Generic.List[object]]::new()
$hashes = @{}
foreach ($file in $files | Sort-Object FullName) {
    $relative = $file.FullName.Substring($root.Length + 1).Replace('\','/')
    $ext = $file.Extension.ToLowerInvariant()
    if (($raster -notcontains $ext) -and ($raw -notcontains $ext)) {
        $skipped.Add(@{ path = $relative; reason = 'extension_not_in_inventory_scope' }); continue
    }
    $flags = [Collections.Generic.List[string]]::new()
    $flags.Add('rights_review_required'); $flags.Add('labels_and_split_required')
    $row = [ordered]@{
        path = $relative; bytes = $file.Length; sha256 = $null; duplicate_of = $null
        decode_status = 'not_attempted'; width = $null; height = $null
        orientation = $null; camera_make = $null; camera_model = $null
        exif_color_space = $null; icc_present = $null; pixel_format = $null
        domain = 'unassigned'; capture_group = $null; session_group = $null
        look_id = $null; split = 'unassigned'; review_status = 'quarantine'
        commercial_training_allowed = $false; rights_evidence = $null
        face_annotation_status = 'not_evaluated'; flags = $null
    }
    try {
        # Same read-only stream for hashing and validation prevents a stale hash/read pair.
        $stream = [IO.File]::Open($file.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
        try {
            $sha = [Security.Cryptography.SHA256]::Create()
            try { $row.sha256 = ([BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-','').ToLowerInvariant() }
            finally { $sha.Dispose() }
            if ($hashes.ContainsKey($row.sha256)) { $row.duplicate_of = $hashes[$row.sha256]; $flags.Add('exact_duplicate') }
            else { $hashes[$row.sha256] = $relative }
            if ($raw -contains $ext) {
                $row.decode_status = 'unsupported_requires_decoder'; $flags.Add('decoder_required')
            } else {
                $stream.Position = 0
                $image = $null; $bitmap = $null
                try {
                    $image = [Drawing.Image]::FromStream($stream, $false, $true)
                    $row.width = $image.Width; $row.height = $image.Height
                    $row.pixel_format = $image.PixelFormat.ToString()
                    $row.orientation = Get-ImageProperty $image 274
                    $row.camera_make = Get-ImageProperty $image 271 $true
                    $row.camera_model = Get-ImageProperty $image 272 $true
                    $row.exif_color_space = Get-ImageProperty $image 40961
                    $row.icc_present = $image.PropertyIdList -contains 34675
                    if (-not $row.icc_present -and $row.exif_color_space -ne 1) { $flags.Add('color_space_unverified') }
                    if ($row.orientation -and $row.orientation -ne 1) { $flags.Add('orientation_transform_required') }
                    $frames = 1
                    foreach ($guid in $image.FrameDimensionsList) {
                        $frames = [Math]::Max($frames, $image.GetFrameCount([Drawing.Imaging.FrameDimension]::new($guid)))
                    }
                    if ($frames -gt 1) { $flags.Add('multiple_frames_review') }
                    # Bound memory: large files remain pending, never pass as decoded.
                    if ([long]$image.Width * $image.Height -gt 40000000) {
                        $row.decode_status = 'header_only_pixel_limit'; $flags.Add('full_decode_pending')
                    } else {
                        $bitmap = [Drawing.Bitmap]::new($image)
                        $null = $bitmap.GetPixel($bitmap.Width - 1, $bitmap.Height - 1)
                        $row.decode_status = 'decoded_first_frame'
                    }
                } catch {
                    $row.decode_status = 'decode_failed'; $flags.Add('decode_failed')
                } finally {
                    if ($null -ne $bitmap) { $bitmap.Dispose() }
                    if ($null -ne $image) { $image.Dispose() }
                }
            }
        } finally { $stream.Dispose() }
    } catch { $row.decode_status = 'read_failed'; $flags.Add('read_failed') }
    $row.flags = @($flags.ToArray())
    $assets.Add([pscustomobject]$row)
}
$manifest = [ordered]@{
    schema_version = 'inventory-1'; created_utc = [DateTime]::UtcNow.ToString('o')
    tool = 'FotoAutomat.MatchLookData'; input_root = $root
    training_ready = $false; assets = @($assets.ToArray()); skipped = @($skipped.ToArray())
    limitations = @('No color conversion or image edits', 'No automatic rights approval or dataset splitting',
        'No face detection, skin masks, perceptual duplicate or blur scoring',
        'RAW and extra formats inventory only; first-frame validation of supported raster files',
        'ICC presence is not ICC validity; missing metadata stays unknown')
}
$null = New-Item -ItemType Directory -Path $output
$manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $output 'manifest.json') -Encoding UTF8
$assets | Select-Object path,sha256,duplicate_of,decode_status,width,height,camera_make,camera_model,domain,capture_group,session_group,look_id,split,review_status,commercial_training_allowed,rights_evidence,@{Name='flags';Expression={$_.flags -join ';'}} |
    Export-Csv -LiteralPath (Join-Path $output 'review.csv') -NoTypeInformation -Encoding UTF8
$summary = [ordered]@{ total_assets = $assets.Count; skipped = $skipped.Count; training_ready = $false; decode_status = @($assets | Group-Object decode_status | Select-Object Name,Count) }
$summary | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $output 'summary.json') -Encoding UTF8
Write-Output "Inventory complete: $($assets.Count) assets. Reports: $output"
