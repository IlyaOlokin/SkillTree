param([string]$UnityCli = 'unity', [int]$Seed = 31001)
$ErrorActionPreference = 'Stop'
$taskProject = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$taskVerification = Join-Path $taskProject ('Reports/BalanceSimulation/' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff') + '-fidelity')
New-Item -ItemType Directory -Path $taskVerification | Out-Null
. (Join-Path $PSScriptRoot 'Internal/Storage.ps1')
$taskPreset = Get-Content (Join-Path $PSScriptRoot 'Presets/balanced-shops.json') -Raw | ConvertFrom-Json
$taskPreset.locationIds = @($taskPreset.locationIds | Select-Object -First 2)
$taskPreset.maxAttemptsPerStage = 1
$taskPreset.maxWaveSeconds = 60
$taskPresetPath = Join-Path $taskVerification 'preset.json'
Write-BalanceText $taskPresetPath ($taskPreset | ConvertTo-Json -Depth 15)
$taskToolsRoot = $PSScriptRoot
$taskSource = Get-Content (Join-Path $PSScriptRoot 'Runtime/Campaign.cs') -Raw
$taskAdapterKind = 'campaign'
. (Join-Path $PSScriptRoot 'Internal/UnityBridge.ps1')
$taskDiagnosticKey = 'balanceSimulation.contract.' + [Guid]::NewGuid().ToString('N')
try {
    $taskDiagnostic = Invoke-BalanceChunk @{ checkpointKey = $taskDiagnosticKey; operation = 'verify-contract'; config = $taskPreset; runs = 1; seed = $Seed }
    Write-BalanceText (Join-Path $taskVerification 'contract.json') ($taskDiagnostic | ConvertTo-Json -Depth 15)
} finally { $null = Invoke-BalanceChunk @{ checkpointKey = $taskDiagnosticKey; operation = 'discard' } }
foreach ($taskPass in @('first', 'repeat')) {
    & (Join-Path $PSScriptRoot 'RunCampaign.ps1') -Preset $taskPresetPath -RunsPerStrategy 1 -Seed $Seed -UnityCli $UnityCli -Output (Join-Path $taskVerification ($taskPass + '/report.json'))
}
$taskFirst = Read-BalanceReport (Join-Path $taskVerification 'first/report.json.gz')
$taskRepeat = Read-BalanceReport (Join-Path $taskVerification 'repeat/report.json.gz')
# Keep only gameplay data. Command timings/generated timestamps are intentionally different.
$taskFirstGameplay = $taskFirst.campaigns | ConvertTo-Json -Depth 30 -Compress
$taskRepeatGameplay = $taskRepeat.campaigns | ConvertTo-Json -Depth 30 -Compress
if ($taskFirstGameplay -cne $taskRepeatGameplay) { throw "Repeatability mismatch; both datasets retained in $taskVerification" }
$taskNote = @'
# Simulator fidelity checkpoint

Two identical bounded three-strategy campaigns used the same seed, two initial
locations, one attempt per stage and a 60-second wave cap. Campaign gameplay data,
including waves, progression, allocations, rewards and snapshots, matched exactly.
Timings and generated timestamps are intentionally not compared.

This checks reproducibility, not full parity with a separately played Unity game.
The adapter now registers disposable combat actors with production BattleTickSystem,
runs its RunTick method, and binds each EnemyUnit's injected player target so
kill reactions receive production death notifications. Actor registration fails
explicitly if binding is unsuccessful. No gameplay source or scene is edited.

Remaining fidelity coverage: independent Play Mode comparisons for representative
reactive/effect families, inventory-full reward behavior, influence/bridge gems,
repeated infinite investment, adaptive respec and scene/service-specific mechanics.
These limits must remain visible before training or claiming exhaustive parity.
'@
Write-BalanceText (Join-Path $taskVerification 'verification.md') $taskNote
Write-Output "Repeatability passed. Evidence: $taskVerification"
