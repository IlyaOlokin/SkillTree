. (Join-Path $PSScriptRoot 'Storage.ps1')
function Update-BalanceReportIndex {
    New-Item -ItemType Directory -Force -Path $taskReportRoot | Out-Null
    $entries = @(Get-ChildItem -LiteralPath $taskReportRoot -Filter '*.html' -File -Recurse |
        Where-Object Name -ne 'index.html' | Sort-Object LastWriteTime -Descending | ForEach-Object {
            $relative = $_.FullName.Substring($taskReportRoot.Length + 1).Replace('\','/')
            $labelPath = [IO.Path]::ChangeExtension($_.FullName, '.label.txt')
            $description = if (Test-Path -LiteralPath $labelPath) { (Get-Content -LiteralPath $labelPath -Raw).Trim() + ' · ' + $relative } else { $relative }
            '<li><a href="' + [Net.WebUtility]::HtmlEncode($relative) + '">' + [Net.WebUtility]::HtmlEncode($description) + '</a></li>'
        })
    $index = @('<!doctype html><html lang="ru"><meta charset="utf-8"><title>Отчёты баланса</title>',
        '<style>body{font:18px system-ui;background:#101722;color:#dce5ee;margin:40px}a{color:#7dd3fc}li{margin:16px}</style>',
        '<h1>Отчёты баланса</h1><p><a href="README.md">Формат отчётов</a></p><ul>',
        ($entries -join "`n"), '</ul></html>') -join "`n"
    Write-BalanceText (Join-Path $taskReportRoot 'index.html') $index
}
function Publish-BalanceReport([string]$Path, $Result) {
    $Path = Resolve-BalanceReportPath $Path 'report'
    $htmlPath = [IO.Path]::ChangeExtension($Path, '.html')
    $payload = ConvertTo-Json -InputObject $Result -Depth 30 -Compress
    $raw = [Text.UTF8Encoding]::new($false).GetBytes($payload)
    $memory = [IO.MemoryStream]::new()
    $gzip = [IO.Compression.GZipStream]::new($memory, [IO.Compression.CompressionLevel]::Optimal, $true)
    try { $gzip.Write($raw, 0, $raw.Length) }
    finally { $gzip.Dispose() }
    $compressed = $memory.ToArray(); $memory.Dispose()
    $template = Get-Content (Join-Path $taskToolsRoot 'Templates/Report.html') -Raw
    $strategySummary = Get-Content (Join-Path $taskToolsRoot 'Templates/StrategySummary.js') -Raw
    $template = $template.Replace('__STRATEGY_SUMMARY_JS__', $strategySummary)
    $indexLink = [IO.Path]::GetRelativePath((Split-Path $htmlPath), (Join-Path $taskReportRoot 'index.html')).Replace('\','/')
    $template = $template.Replace('href="../index.html"', 'href="' + [Net.WebUtility]::HtmlEncode($indexLink) + '"')
    $template = $template.Replace('href="report.md"', 'href="' + [Net.WebUtility]::HtmlEncode([IO.Path]::GetFileName([IO.Path]::ChangeExtension($Path, '.md'))) + '"')
    $template = $template.Replace('href="report.json.gz"', 'href="' + [Net.WebUtility]::HtmlEncode([IO.Path]::GetFileName($Path) + '.gz') + '"')
    # Encode once, reuse identical compressed bytes for offline HTML and the data archive.
    $html = $template.Replace('__REPORT_GZIP_BASE64__', [Convert]::ToBase64String($compressed))
    Write-BalanceBytes ($Path + '.gz') $compressed
    Write-BalanceText $htmlPath $html
    $label = if ($Result.campaigns) { 'Кампания: ' + @($Result.summary).Count + ' билда, ' + @($Result.campaigns).Count + ' ботов' } else { 'Фиксированные бои: ' + $Result.battles }
    if ($Result.preset.adaptive) { $label = 'Адаптивная ' + $label }
    if ($Result.isPartial) { $label += ' (неполный прогон)' }
    Write-BalanceText ([IO.Path]::ChangeExtension($Path, '.label.txt')) $label
    Update-BalanceReportIndex
    Write-Output ('Viewer: ' + $htmlPath)
}
function Add-BalanceCampaignDetails([string]$Path, $Result) {
    $lines = [Collections.Generic.List[string]]::new()
    $lines.Add('')
    $lines.Add('## Per-seed progression')
    $lines.Add('')
    $lines.Add('| Build | Seed | Stage cleared | Final level | Allocated nodes | Free points | Deaths | Stop reason |')
    $lines.Add('| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |')
    foreach ($row in $Result.campaigns) {
        $lines.Add(('| {0} | {1} | {2} | {3} | {4} | {5} | {6} | {7} |' -f
            $row.strategy, $row.seed, $row.highestCompletedStage, $row.player.level, $row.allocatedNodeIds.Count,
            $row.player.skillPoints, $row.deaths, $row.reason.Replace('|','\|')))
    }
    $lines.Add('')
    $lines.Add('## Best-reaching bot stats')
    $lines.Add('')
    $lines.Add('One bot per strategy, highest cleared stage then lowest seed. Values are the final reset-boundary production stats, after all retries and XP gains, rather than its stats on first entering that stage. Full tree, all stats and earlier boundaries are in the HTML viewer.')
    $lines.Add('')
    $lines.Add('| Build | Seed | Level | Strength | Dexterity | Intelligence | Maximum health | Physical damage | Lightning damage | Attack speed |')
    $lines.Add('| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |')
    foreach ($group in ($Result.campaigns | Group-Object strategy)) {
        $row = $group.Group | Sort-Object @{Expression='highestCompletedStage';Descending=$true},seed | Select-Object -First 1
        $stats = $row.snapshots[-1].stats
        $lines.Add(('| {0} | {1} | {2} | {3:F2} | {4:F2} | {5:F2} | {6:F2} | {7:F2} | {8:F2} | {9:F3} |' -f
            $row.strategy, $row.seed, $row.player.level, $stats.Strength, $stats.Dexterity, $stats.Intelligence,
            $stats.MaximumHealth, $stats.PhysicalDamage, $stats.LightningDamage, $stats.AttackSpeed))
    }
    $lines | Add-Content -LiteralPath ([IO.Path]::ChangeExtension($Path, '.md')) -Encoding utf8
}



