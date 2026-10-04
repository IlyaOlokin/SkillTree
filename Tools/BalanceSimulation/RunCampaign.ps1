param(
    [string]$Preset = '',
    [ValidateRange(1, 1000)][int]$RunsPerStrategy = 5,
    [int]$Seed = 101,
    [ValidateRange(250, 2000)][int]$ComputeBudgetMs = 1500,
    [string]$UnityCli = 'unity',
    [string]$Output = ''
)
$ErrorActionPreference = 'Stop'
$taskToolsRoot = $PSScriptRoot
$taskProject = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
if (-not $Preset) { $Preset = Join-Path $PSScriptRoot 'Presets/damage-push.json' }
. (Join-Path $PSScriptRoot 'Internal/Reports.ps1')
$taskOutput = Resolve-BalanceReportPath $Output 'campaign'
$taskConfig = Get-Content -LiteralPath $Preset -Raw | ConvertFrom-Json
$taskSource = Get-Content (Join-Path $PSScriptRoot 'Runtime/Campaign.cs') -Raw
$taskAdapterKind = 'campaign'
. (Join-Path $PSScriptRoot 'Internal/UnityBridge.ps1')
$taskWork = New-BalanceWorkDirectory $taskOutput
$taskCheckpointKey = 'balanceSimulation.campaign.' + [Guid]::NewGuid().ToString('N')
$taskWatch = [Diagnostics.Stopwatch]::StartNew()
$taskCalls = 0
$taskCompleted = 0
$taskTotal = @($taskConfig.strategies).Count * $RunsPerStrategy
$taskCompute = 0
function Save-CampaignProgress {
    $progress = [ordered]@{ schemaVersion = 1; preset = $taskConfig; seed = $Seed; runsPerStrategy = $RunsPerStrategy
        output = $taskOutput; advanceCommands = $taskCalls; commands = $taskCommandCount
        computeSeconds = $taskCompute; wallSecondsIncludingCommands = $taskWatch.Elapsed.TotalSeconds
        savedRuns = $taskCompleted; totalRuns = $taskTotal }
    Write-BalanceText (Join-Path $taskWork 'run.json') ($progress | ConvertTo-Json -Depth 15 -Compress)
}
Save-CampaignProgress
try {
    # Save the graph before advancing: completed rows remain viewable after a later failure.
    $null = Invoke-BalanceExport @{ checkpointKey = $taskCheckpointKey; operation = 'catalog'; config = $taskConfig;
        runs = $RunsPerStrategy; seed = $Seed } (Join-Path $taskWork 'catalog.json')
    do {
        $taskCalls++
        if ($taskCalls -gt 10000) { throw 'Campaign command limit reached.' }
        $taskChunk = Invoke-BalanceChunk @{ checkpointKey = $taskCheckpointKey; operation = 'advance'; config = $taskConfig;
            runs = $RunsPerStrategy; seed = $Seed; computeBudgetMs = $ComputeBudgetMs }
        if ($null -eq $taskChunk.finished) { throw 'Campaign returned no progress checkpoint.' }
        $taskCompute = $taskChunk.computeSeconds
        Write-Output ('Campaigns finished: {0}/{1}; stage attempts this chunk: {2}' -f $taskChunk.completedRuns, $taskChunk.totalRuns, $taskChunk.stagesThisCommand)
        while ($taskCompleted -lt $taskChunk.completedRuns) {
            $taskExport = Invoke-BalanceExport @{ checkpointKey = $taskCheckpointKey; operation = 'export'; index = $taskCompleted } `
                (Join-Path $taskWork ('campaign-{0:D4}.json' -f $taskCompleted))
            if (@($taskExport.campaigns).Count -ne 1 -or $null -eq $taskExport.campaigns[0].snapshots) { throw 'Invalid campaign export.' }
            $taskCompleted++
            Save-CampaignProgress
        }
        Save-CampaignProgress
    } while (-not $taskChunk.finished)
} finally {
    try { $null = Invoke-BalanceChunk @{ checkpointKey = $taskCheckpointKey; operation = 'discard' } }
    catch { Write-Warning ('Could not discard checkpoint: ' + $taskCheckpointKey) }
    $taskWatch.Stop()
    Save-CampaignProgress
}
& (Join-Path $PSScriptRoot 'Recover.ps1') -WorkDirectory $taskWork
