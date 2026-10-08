param(
    [Parameter(Mandatory)][object[]]$Campaigns,
    [Parameter(Mandatory)]$Preset,
    [Parameter(Mandatory)][object[]]$TreeCatalog,
    [Parameter(Mandatory)][string]$Output,
    [int]$Seed = 101,
    [int]$RunsPerStrategy = 10,
    [int]$AdvanceCommands = -1,
    [int]$Commands = -1,
    [switch]$IsPartial,
    [double]$ComputeSeconds = -1,
    [double]$WallSeconds = -1,
    [string]$TimingNote = ''
)
$ErrorActionPreference = 'Stop'
$taskToolsRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$taskProject = (Resolve-Path (Join-Path $taskToolsRoot '../..')).Path
. (Join-Path $PSScriptRoot 'Reports.ps1')
$taskOutput = Resolve-BalanceReportPath $Output 'campaign'
$taskRows = @($Campaigns)
$taskSummary = @($taskRows | Group-Object strategy | ForEach-Object {
    $taskRewardActions = @($_.Group | ForEach-Object rewardLog)
    [pscustomobject]@{
        strategy = $_.Name; runs = $_.Count
        routeCompleted = @($_.Group | Where-Object status -eq 'completed').Count
        stopped = @($_.Group | Where-Object status -eq 'stopped').Count
        blocked = @($_.Group | Where-Object status -eq 'blocked').Count
        meanHighestCompletedStage = ($_.Group.highestCompletedStage | Measure-Object -Average).Average
        maximumCompletedStage = ($_.Group.highestCompletedStage | Measure-Object -Maximum).Maximum
        meanFinalLevel = (@($_.Group | ForEach-Object { $_.player.level }) | Measure-Object -Average).Average
        meanAllocatedNodes = (@($_.Group | ForEach-Object { $_.allocatedNodeIds.Count }) | Measure-Object -Average).Average
        meanFreePoints = (@($_.Group | ForEach-Object { $_.player.skillPoints }) | Measure-Object -Average).Average
        deaths = ($_.Group.deaths | Measure-Object -Sum).Sum
        waves = ($_.Group.waves | Measure-Object -Sum).Sum
        trialWaves = (@($_.Group | ForEach-Object { if ($null -ne $_.trialWaves) { $_.trialWaves } else { 0 } }) | Measure-Object -Sum).Sum
        candidatesEvaluated = @($_.Group | ForEach-Object searchLog | Where-Object { $_.action -eq 'evaluate' -and $_.candidate -gt 0 -and -not $_.duplicate }).Count
        refitsCommitted = @($_.Group | ForEach-Object searchLog | Where-Object { $_.action -eq 'commit' -and $_.changedBuild }).Count
        confirmedStageWins = @($_.Group | ForEach-Object searchLog | Where-Object { $_.action -eq 'confirm' -and $_.won }).Count
        rewardsClaimed = @($taskRewardActions | Where-Object { $_.action -eq 'claim' -and $_.claimed }).Count
        rewardsUsed = @($taskRewardActions | Where-Object action -eq 'used').Count
        rewardsLostOnInsertion = @($taskRewardActions | Where-Object { $_.action -eq 'claim' -and $_.claimed -and -not $_.delivered }).Count
        gemsBought = @($_.Group | ForEach-Object shopLog | Where-Object { $_.action -eq 'buy' -and $_.success }).Count
        gemsInserted = @($_.Group | ForEach-Object shopLog | Where-Object action -eq 'insert').Count
    }
})
$taskLocationSummary = @($taskRows | ForEach-Object {
    $taskCampaign = $_
    foreach ($taskGroup in ($taskCampaign.waveLog | Group-Object location)) {
        [pscustomobject]@{ strategy = $taskCampaign.strategy; seed = $taskCampaign.seed; location = $taskGroup.Name
            completed = $taskGroup.Name -in $taskCampaign.completedLocations
            waves = $taskGroup.Count; deaths = @($taskGroup.Group | Where-Object outcome -eq 'dead').Count
            timeouts = @($taskGroup.Group | Where-Object outcome -eq 'timeout').Count
            bossWins = @($taskGroup.Group | Where-Object { $_.boss -and $_.outcome -eq 'won' }).Count
            bossAttempts = @($taskGroup.Group | Where-Object boss).Count
        }
    }
} | Group-Object -Property { ConvertTo-Json -InputObject @($_.strategy, $_.location) -Compress } | ForEach-Object {
    [pscustomobject]@{ strategy = $_.Group[0].strategy; location = $_.Group[0].location; runsReached = $_.Count
        runsCompleted = @($_.Group | Where-Object completed).Count
        waves = ($_.Group.waves | Measure-Object -Sum).Sum; deaths = ($_.Group.deaths | Measure-Object -Sum).Sum
        timeouts = ($_.Group.timeouts | Measure-Object -Sum).Sum
        bossWins = ($_.Group.bossWins | Measure-Object -Sum).Sum; bossAttempts = ($_.Group.bossAttempts | Measure-Object -Sum).Sum }
})
$taskSimulatedSeconds = ($taskRows.simulatedSeconds | Measure-Object -Sum).Sum
$taskTrialSeconds = (@($taskRows | ForEach-Object { if ($null -ne $_.trialSimulatedSeconds) { $_.trialSimulatedSeconds } else { 0 } }) | Measure-Object -Sum).Sum
$taskResult = [pscustomobject]@{
    schemaVersion = 2; mode = 'progression-campaign'; engine = 'production Unity combat, XP, allocation, inventory, items and location progress'
    generatedAt = (Get-Date).ToString('o'); treeCatalog = $TreeCatalog
    preset = $Preset; seed = $Seed; runsPerStrategy = $RunsPerStrategy
    advanceCommands = $(if ($AdvanceCommands -ge 0) { $AdvanceCommands } else { $null }); commands = $(if ($Commands -ge 0) { $Commands } else { $null })
    isPartial = [bool]$IsPartial; expectedCampaigns = @($Preset.strategies).Count * $RunsPerStrategy
    timingNote = $TimingNote
    computeSeconds = $(if ($ComputeSeconds -ge 0) { $ComputeSeconds } else { $null }); wallSecondsIncludingCommands = $(if ($WallSeconds -ge 0) { $WallSeconds } else { $null })
    simulatedCombatSeconds = $taskSimulatedSeconds
    trialSimulatedCombatSeconds = $taskTrialSeconds; totalSimulatedCombatSeconds = $taskSimulatedSeconds + $taskTrialSeconds
    computeSpeedup = $(if ($ComputeSeconds -ge 0) { ($taskSimulatedSeconds + $taskTrialSeconds) / [Math]::Max(0.000001, $ComputeSeconds) } else { $null })
    endToEndSpeedup = $(if ($WallSeconds -ge 0) { ($taskSimulatedSeconds + $taskTrialSeconds) / [Math]::Max(0.000001, $WallSeconds) } else { $null })
    summary = $taskSummary; locationSummary = $taskLocationSummary; campaigns = $taskRows
}
New-Item -ItemType Directory -Force -Path (Split-Path $taskOutput) | Out-Null
$taskMarkdown = [Collections.Generic.List[string]]::new()
$taskMarkdown.Add('# Progression campaign comparison')
$taskMarkdown.Add('')
if ($Preset.adaptive) {
    $taskMarkdown.Add('Adaptive search uses free experimental respecs on isolated copies. Probes freeze level, XP, point budget and wave seeds; they award no progression or items. Winning/better trees are committed and replayed in normal progression. Probe waves are reported separately.')
    $taskMarkdown.Add('')
    $taskMarkdown.Add('| Strategy | Probe waves | Distinct candidates evaluated | Changed trees committed | Confirmed stage wins after search |')
    $taskMarkdown.Add('| --- | ---: | ---: | ---: | ---: |')
    foreach ($taskEntry in $taskSummary) {
        $taskMarkdown.Add(('| {0} | {1} | {2} | {3} | {4} |' -f $taskEntry.strategy, $taskEntry.trialWaves, $taskEntry.candidatesEvaluated, $taskEntry.refitsCommitted, $taskEntry.confirmedStageWins))
    }
    $taskMarkdown.Add('')
}
if ($IsPartial) { $taskMarkdown.Add('INCOMPLETE RUN: only completed, saved campaigns are included.'); $taskMarkdown.Add('') }
$taskMarkdown.Add(('[Open interactive stats and skill tree]({0}). Select a campaign and a stage boundary. Build stats exclude temporary combat effects; all numeric values are raw production stat values.' -f [IO.Path]::GetFileName([IO.Path]::ChangeExtension($taskOutput, '.html'))))
$taskMarkdown.Add('')
$taskMarkdown.Add('Fresh authored player defaults. XP comes from actual enemy kills; nodes are allocated as points arrive. Boss rewards, including gold, are claimed and simple supported items are consumed. Campaigns with shopLog support numeric local gems: bots visit unlocked shops when an active empty socket needs a gem, buy affordable stock through ShopService and insert through InventorySocketService. Gold, stock and socket contents persist across stages. Repeated failures retain earned XP and investment. Influence/bridge gems and UI navigation remain outside coverage. Older reports without shopLog exclude shops and gems.')
$taskMarkdown.Add('')
$taskMarkdown.Add('| Strategy | Runs | Route completed | Mean stage cleared | Maximum stage | Mean final level | Mean nodes | Mean free points | Deaths | Rewards used |')
$taskMarkdown.Add('| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |')
foreach ($taskEntry in $taskSummary) {
    $taskMarkdown.Add(('| {0} | {1} | {2} | {3:F1} | {4} | {5:F1} | {6:F1} | {7:F1} | {8} | {9} |' -f
        $taskEntry.strategy.Replace('|','\|'), $taskEntry.runs, $taskEntry.routeCompleted, $taskEntry.meanHighestCompletedStage,
        $taskEntry.maximumCompletedStage, $taskEntry.meanFinalLevel, $taskEntry.meanAllocatedNodes, $taskEntry.meanFreePoints, $taskEntry.deaths, $taskEntry.rewardsUsed))
}
$taskMarkdown.Add('')
$taskMarkdown.Add('Location results count only normal-progression runs that reached that location. Boss attempts include normal retries; adaptive probes are separate.')
$taskMarkdown.Add('')
$taskMarkdown.Add('| Strategy | Location | Runs reached | Runs completed | Waves | Deaths | Timeouts | Boss wins / attempts |')
$taskMarkdown.Add('| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |')
foreach ($taskEntry in $taskLocationSummary) {
    $taskMarkdown.Add(('| {0} | {1} | {2} | {3} | {4} | {5} | {6} | {7} / {8} |' -f
        $taskEntry.strategy.Replace('|','\|'), $taskEntry.location.Replace('|','\|'), $taskEntry.runsReached, $taskEntry.runsCompleted,
        $taskEntry.waves, $taskEntry.deaths, $taskEntry.timeouts, $taskEntry.bossWins, $taskEntry.bossAttempts))
}
$taskMarkdown.Add('')
$taskMarkdown.Add(('Probe simulated combat: {0:F1} s. Throughput includes both normal and probe combat.' -f $taskTrialSeconds))
if ($ComputeSeconds -ge 0 -and $WallSeconds -ge 0) {
$taskMarkdown.Add(('Waves: {0}. Simulated combat: {1:F1} s. Compute: {2:F3} s. Full execution: {3:F3} s. Compute speedup: {4:F1}x; end-to-end: {5:F1}x.' -f
    ($taskRows.waves | Measure-Object -Sum).Sum, $taskSimulatedSeconds, $ComputeSeconds, $WallSeconds,
    $taskResult.computeSpeedup, $taskResult.endToEndSpeedup))
} else { $taskMarkdown.Add('Timing metrics are unavailable for this recovered dataset. ' + $TimingNote) }
$taskMarkdown | Set-Content -LiteralPath ([IO.Path]::ChangeExtension($taskOutput, '.md')) -Encoding utf8
Add-BalanceCampaignDetails $taskOutput $taskResult
Publish-BalanceReport $taskOutput $taskResult
$taskSummary | Select-Object strategy,runs,meanHighestCompletedStage,meanFinalLevel,deaths,rewardsUsed | Format-Table -AutoSize
Write-Output ('Report: ' + $taskOutput + '.gz')


