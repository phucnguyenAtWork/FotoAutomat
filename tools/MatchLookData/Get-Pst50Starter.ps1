param([string]$Destination = '.local/matchlook-data/pst50-starter')
$ErrorActionPreference = 'Stop'
$revision = '6ed86e34c37c558a0b2401d52f06603d619472e1'
$root = [IO.Path]::GetFullPath($Destination)
if (Test-Path -LiteralPath $root) { throw 'Use a new destination; existing data will not be overwritten.' }
$null = New-Item -ItemType Directory -Path $root
function Download([string]$url, [string]$path) {
    & curl.exe -sSL --fail --retry 2 --connect-timeout 20 --max-time 180 --output $path $url
    if ($LASTEXITCODE -ne 0) { throw "Download failed: $url" }
}
Download "https://huggingface.co/datasets/zrgong/PST50/raw/$revision/README.md" (Join-Path $root 'UPSTREAM-README.md')
Download 'https://creativecommons.org/licenses/by/4.0/legalcode.txt' (Join-Path $root 'LICENSE-CC-BY-4.0.txt')
$records = @()
foreach ($folder in @('content_709','paired_style','paired_gt')) {
    $metadataPath = Join-Path $root ($folder + '-upstream.json')
    Download "https://huggingface.co/api/datasets/zrgong/PST50/tree/$revision/$folder" $metadataPath
    $metadata = Get-Content -Raw -LiteralPath $metadataPath | ConvertFrom-Json
    $null = New-Item -ItemType Directory -Path (Join-Path $root "images/$folder")
    $prefix = @{content_709='in'; paired_style='tar'; paired_gt='gt'}[$folder]
    foreach ($id in 1..5) {
        $remote = "$folder/$prefix$id.png"
        $entry = @($metadata | Where-Object path -eq $remote)
        if ($entry.Count -ne 1 -or -not $entry[0].lfs.oid) { throw "Missing hash metadata: $remote" }
        $url = "https://huggingface.co/datasets/zrgong/PST50/resolve/$revision/$remote"
        $local = Join-Path $root "images/$remote"
        Download $url $local
        $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $local).Hash.ToLowerInvariant()
        if ($hash -ne $entry[0].lfs.oid -or (Get-Item -LiteralPath $local).Length -ne $entry[0].size) { throw "Integrity mismatch: $remote" }
        $records += [ordered]@{path="images/$remote"; source_url=$url; sha256=$hash; bytes=$entry[0].size; license='CC-BY-4.0'; role=$folder; case_id="pst50-$id"}
        Write-Output "Verified $remote"
    }
}
$cases = @(1..5 | ForEach-Object { [ordered]@{
    id="pst50-$_"; source="images/content_709/in$_.png"; reference="images/paired_style/tar$_.png"; target="images/paired_gt/gt$_.png"
    split='evaluation_only'; domain='pending_visual_review'; pairing='upstream_index_pending_visual_validation'; color_pipeline='pending_Rec709_transfer_verification'
}})
[ordered]@{dataset='zrgong/PST50'; revision=$revision; downloaded_utc=[DateTime]::UtcNow.ToString('o'); source='https://huggingface.co/datasets/zrgong/PST50'; attribution='PST50, zrgong and the SA-LUT authors; see upstream dataset card'; license='CC-BY-4.0'; modifications='Unmodified subset, cases 1 through 5'; commercial_training_allowed=$false; benchmark_ready=$false; assets=$records; cases=$cases} |
    ConvertTo-Json -Depth 8 | Set-Content -Encoding UTF8 -LiteralPath (Join-Path $root 'provenance.json')
Write-Output "Downloaded and verified $($records.Count) images to $root"
