param(
    [Parameter(Mandatory)][string]$WorkDirectory,
    [switch]$AllowPartial
)
$ErrorActionPreference = 'Stop'
$taskToolsRoot = $PSScriptRoot
$taskProject = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
. (Join-Path $PSScriptRoot 'Internal/Storage.ps1')
$taskWork = (Resolve-Path -LiteralPath $WorkDirectory).Path
$taskRoot = [IO.Path]::GetFullPath($taskReportRoot).TrimEnd('\','/') + [IO.Path]::DirectorySeparatorChar
if (-not $taskWork.StartsWith($taskRoot, [StringComparison]::OrdinalIgnoreCase) -or -not $taskWork.EndsWith('.json.work', [StringComparison]::OrdinalIgnoreCase)) { throw 'Choose a saved .json.work directory under Reports/BalanceSimulation.' }
$taskManifest = Read-BalanceReport (Join-Path $taskWork 'run.json')
$taskOutput = Resolve-BalanceReportPath $taskManifest.output 'campaign'
if ([IO.Path]::GetFullPath($taskOutput + '.work') -ne $taskWork) { throw 'Manifest output does not match its work directory.' }
$taskCatalog = Read-BalanceReport (Join-Path $taskWork 'catalog.json')
if (-not $taskCatalog.treeCatalog) { throw 'The saved tree catalog is missing.' }
$taskRows = @(Get-ChildItem -LiteralPath $taskWork -Filter 'campaign-*.json' -File | Sort-Object Name | ForEach-Object {
    $envelope = Read-BalanceReport $_.FullName
    if (@($envelope.campaigns).Count -ne 1) { throw 'Expected one campaign per saved export.' }
    $envelope.campaigns[0]
})
if (-not $taskRows.Count) { throw 'No completed campaigns were saved.' }
$taskExpected = @{}
foreach ($strategy in $taskManifest.preset.strategies) {
    for ($offset = 0; $offset -lt $taskManifest.runsPerStrategy; $offset++) {
        $key = ConvertTo-Json -InputObject @($strategy.name, [int]($taskManifest.seed + $offset)) -Compress
        if ($taskExpected.ContainsKey($key)) { throw 'Duplicate strategy/seed in manifest.' }
        $taskExpected[$key] = $false
    }
}
foreach ($row in $taskRows) {
    $key = ConvertTo-Json -InputObject @($row.strategy, [int]$row.seed) -Compress
    if (-not $taskExpected.ContainsKey($key) -or $taskExpected[$key] -or $row.status -notin @('completed','stopped','blocked') -or $null -eq $row.snapshots) { throw 'Unexpected, duplicate or unfinished saved campaign.' }
    $taskExpected[$key] = $true
}
$taskPartial = $taskRows.Count -ne $taskExpected.Count
if ($taskPartial -and -not $AllowPartial) { throw 'The run is incomplete. Use -AllowPartial to publish only saved campaigns, explicitly marked incomplete.' }
$taskCompute = if ($taskPartial) { -1 } else { $taskManifest.computeSeconds }
$taskWall = if ($taskPartial) { -1 } else { $taskManifest.wallSecondsIncludingCommands }
& (Join-Path $PSScriptRoot 'Internal/AssembleCampaignReport.ps1') -Campaigns $taskRows -Preset $taskManifest.preset `
    -TreeCatalog $taskCatalog.treeCatalog -Output $taskOutput -Seed $taskManifest.seed -RunsPerStrategy $taskManifest.runsPerStrategy `
    -AdvanceCommands $taskManifest.advanceCommands -Commands $taskManifest.commands -ComputeSeconds $taskCompute `
    -WallSeconds $taskWall -IsPartial:$taskPartial
if (-not $taskPartial) { Remove-BalanceWorkDirectory $taskOutput }
