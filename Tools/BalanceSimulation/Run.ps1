param(
    [ValidateRange(1, 1000)][int]$RunsPerBuild = 10,
    [int]$Seed = 101,
    [ValidateRange(1, 1000)][int]$PlayerLevel = 1,
    [ValidateRange(1, 1000)][int]$Stage = 1,
    [string]$LocationId = 'level-1',
    [ValidateRange(1, 25)][int]$BatchSize = 25,
    [string]$UnityCli = 'unity',
    [string]$Preset = '',
    [string]$ExportCatalog = '',
    [string]$Output = ''
)

$ErrorActionPreference = 'Stop'
$taskToolsRoot = $PSScriptRoot
$taskProject = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
. (Join-Path $PSScriptRoot 'Internal/Reports.ps1')
$taskOutput = Resolve-BalanceReportPath $Output 'fixed-battles'
$taskSource = Get-Content (Join-Path $PSScriptRoot 'Runtime/FixedBattle.cs') -Raw
$taskAdapterKind = 'fixed'
. (Join-Path $PSScriptRoot 'Internal/UnityBridge.ps1')
if ($ExportCatalog) {
    $taskCatalogPath = Resolve-BalanceReportPath $ExportCatalog 'catalog'
    $null = Invoke-BalanceExport @{ mode = 'catalog' } $taskCatalogPath



    Write-Output ('Catalog: ' + $taskCatalogPath)
    return
}
$taskBuilds = @(
    @{ name = 'strength'; nodeIds = @('8f571679d10f49299ae5a2f28c450ded'); pointBudget = 1 },
    @{ name = 'dexterity'; nodeIds = @('b2c819833f9b4ce5b68e1b4f072a3c81'); pointBudget = 1 },
    @{ name = 'intelligence-lightning'; nodeIds = @('4da909485ee14e01aec7a6a21a44d92c'); pointBudget = 1 }
)
$taskScenarios = @(@{ name = 'default'; locationId = $LocationId; stage = $Stage; playerLevel = $PlayerLevel; waveNumber = 1; maxSeconds = 180 })
if ($Preset) {
    $taskPreset = Get-Content -LiteralPath $Preset -Raw | ConvertFrom-Json
    if ($taskPreset.schemaVersion -ne 1 -or -not $taskPreset.builds -or -not $taskPreset.scenarios) {
        throw 'Preset requires schemaVersion 1 and nonempty builds/scenarios arrays.'
    }
    $taskBuilds = @($taskPreset.builds)
    $taskScenarios = @($taskPreset.scenarios)
}
if ($taskBuilds.Count -gt 75) { throw 'At most 75 builds per preset; split larger comparisons.' }
foreach ($taskNames in @(@{ kind = 'build'; entries = $taskBuilds }, @{ kind = 'scenario'; entries = $taskScenarios })) {
    if (@($taskNames.entries | Where-Object { [string]::IsNullOrWhiteSpace($_.name) }).Count -or
        @($taskNames.entries | Group-Object name | Where-Object Count -gt 1).Count) {
        throw ('Each ' + $taskNames.kind + ' must have a unique nonempty name.')
    }
}
$taskEffectiveBatchSize = [Math]::Min($BatchSize, [Math]::Floor(75 / $taskBuilds.Count))
$taskWork = New-BalanceWorkDirectory $taskOutput
$taskChunks = [Collections.Generic.List[object]]::new()
$taskWallClock = [Diagnostics.Stopwatch]::StartNew()
foreach ($taskScenario in $taskScenarios) {
for ($taskOffset = 0; $taskOffset -lt $RunsPerBuild; $taskOffset += $taskEffectiveBatchSize) {
    $taskCount = [Math]::Min($taskEffectiveBatchSize, $RunsPerBuild - $taskOffset)
    $taskSeed = $Seed + $taskOffset
    $taskChunk = Invoke-BalanceExport @{ runsPerBuild = [int]$taskCount; seed = $taskSeed; builds = $taskBuilds; scenario = $taskScenario } (Join-Path $taskWork ('batch-{0:D4}.json' -f $taskChunks.Count))
    if (-not $taskChunk -or -not $taskChunk.summary) { throw 'Simulator returned no report.' }
    $taskChunks.Add($taskChunk)
}
}
$taskCatalog = Invoke-BalanceExport @{ mode = 'catalog' } (Join-Path $taskWork 'catalog.json')
$taskWallClock.Stop()
$taskRows = @($taskChunks | ForEach-Object { $_.battlesDetail })
$taskSimSeconds = ($taskChunks.simulatedSeconds | Measure-Object -Sum).Sum
$taskComputeSeconds = ($taskChunks.elapsedSeconds | Measure-Object -Sum).Sum
$taskSummary = @($taskRows | Group-Object -Property { ConvertTo-Json -InputObject @($_.scenario, $_.build) -Compress } | ForEach-Object {
    $taskWins = @($_.Group | Where-Object outcome -eq 'won').Count
    [pscustomobject]@{
        scenario = $_.Group[0].scenario; build = $_.Group[0].build; points = $_.Group[0].points; runs = $_.Count; wins = $taskWins
        deaths = @($_.Group | Where-Object outcome -eq 'dead').Count
        timeouts = @($_.Group | Where-Object outcome -eq 'timeout').Count
        winRate = $taskWins / [double]$_.Count
        meanSimulatedSeconds = ($_.Group.simulatedSeconds | Measure-Object -Average).Average
        meanAttackHpLoss = ($_.Group.attackHpLoss | Measure-Object -Average).Average
        meanAttacks = ($_.Group.attacks | Measure-Object -Average).Average
        meanMinimumHpFraction = ($_.Group.minimumHpFraction | Measure-Object -Average).Average
        meanRemainingHpFraction = ($_.Group.remainingHpFraction | Measure-Object -Average).Average
    }
})
$taskResult = [pscustomobject]@{
    schemaVersion = 3; engine = $taskChunks[0].engine; scenarios = $taskScenarios; builds = $taskBuilds
    fixedLevel = $true; experienceAwards = $false; seed = $Seed; runsPerBuildPerScenario = $RunsPerBuild
    tickSeconds = $taskChunks[0].tickSeconds; battles = $taskRows.Count
    totalTicks = ($taskChunks.totalTicks | Measure-Object -Sum).Sum
    simulatedSeconds = $taskSimSeconds; elapsedSeconds = $taskComputeSeconds
    wallSecondsIncludingCommands = $taskWallClock.Elapsed.TotalSeconds
    speedup = $taskSimSeconds / [Math]::Max(0.000001, $taskComputeSeconds)
    endToEndSpeedup = $taskSimSeconds / [Math]::Max(0.000001, $taskWallClock.Elapsed.TotalSeconds)
    batches = $taskChunks.Count; sceneDirtyBefore = $taskChunks[0].sceneDirtyBefore
    sceneDirtyAfter = $taskChunks[$taskChunks.Count - 1].sceneDirtyAfter
    summary = $taskSummary; battlesDetail = $taskRows
    treeCatalog = $taskCatalog.nodes
    inspections = @($taskRows | ForEach-Object {
        $taskBattle = $_
        $taskBuild = $taskBuilds | Where-Object name -eq $taskBattle.build | Select-Object -First 1
        $taskScenario = $taskScenarios | Where-Object name -eq $taskBattle.scenario | Select-Object -First 1
        [pscustomobject]@{ strategy = $taskBattle.build; seed = $taskBattle.seed; highestCompletedStage = $null
            reason = 'Fixed battle: ' + $taskBattle.scenario + ' / ' + $taskBattle.outcome
            allocationLog = @(); rewardLog = @(); waveLog = @()
            snapshots = @([pscustomobject]@{ boundary = 'before-battle'; location = $taskScenario.locationId; stage = $taskScenario.stage; attempt = 1
                level = $taskScenario.playerLevel; freePoints = 0; stats = $taskBattle.initialStats; allocationCount = 0; rewardCount = 0; waveCount = 0
                nodes = @($taskCatalog.nodes | Where-Object { $_.isRoot -or $_.id -in $taskBuild.nodeIds } | ForEach-Object {
                    [pscustomobject]@{ id = $_.id; active = $true; power = 0; multiplier = 1; investedPoints = 1 }
                })
            })
        }
    })
}
New-Item -ItemType Directory -Force -Path (Split-Path $taskOutput) | Out-Null

$taskMarkdown = [Collections.Generic.List[string]]::new()
$taskMarkdown.Add('# Balance simulation comparison')
$taskMarkdown.Add('')
$taskMarkdown.Add('Fixed-level independent battles using production Unity combat. No XP, loot, gems or progression. HP loss counts attack HP loss; minimum/remaining HP also reflect DoT and regeneration.')
$taskMarkdown.Add('')
$taskMarkdown.Add('| Scenario | Build | Points | Runs | Win % | Deaths | Timeouts | Mean seconds (all outcomes) | Mean minimum HP % | Mean remaining HP % |')
$taskMarkdown.Add('| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |')
foreach ($taskEntry in $taskSummary) {
    $taskMarkdown.Add(('| {0} | {1} | {2} | {3} | {4:F1} | {5} | {6} | {7:F2} | {8:F1} | {9:F1} |' -f
        $taskEntry.scenario.Replace('|','\|').Replace("`n",' '), $taskEntry.build.Replace('|','\|').Replace("`n",' '),
        $taskEntry.points, $taskEntry.runs, (100*$taskEntry.winRate), $taskEntry.deaths, $taskEntry.timeouts,
        $taskEntry.meanSimulatedSeconds, (100*$taskEntry.meanMinimumHpFraction), (100*$taskEntry.meanRemainingHpFraction)))
}
$taskMarkdown.Add('')
$taskMarkdown.Add(('Battles: {0}. Compute: {1:F3} s. Full execution: {2:F3} s. Compute speedup: {3:F1}x; end-to-end: {4:F1}x.' -f
    $taskRows.Count, $taskComputeSeconds, $taskWallClock.Elapsed.TotalSeconds, $taskResult.speedup, $taskResult.endToEndSpeedup))
$taskMarkdownPath = [IO.Path]::ChangeExtension($taskOutput, '.md')
$taskMarkdown | Set-Content -LiteralPath $taskMarkdownPath -Encoding utf8
Publish-BalanceReport $taskOutput $taskResult
Remove-BalanceWorkDirectory $taskOutput
$taskResult.summary | Format-Table -AutoSize
Write-Output ('Report: ' + $taskOutput + '.gz')
Write-Output ('Measured speedup: ' + $taskResult.speedup.ToString('F1') + 'x')
Write-Output ('Including command overhead: ' + $taskResult.endToEndSpeedup.ToString('F1') + 'x')



