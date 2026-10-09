param([Parameter(Mandatory)][string]$JobDirectory)
$ErrorActionPreference='Stop'
$taskInput=Get-Content (Join-Path $JobDirectory 'aggregate-input.json') -Raw|ConvertFrom-Json
$taskJob=Get-Content (Join-Path $JobDirectory 'job.json') -Raw|ConvertFrom-Json
& (Join-Path $PSScriptRoot '../Internal/AssembleCampaignReport.ps1') -Campaigns $taskInput.campaigns -TreeCatalog $taskInput.treeCatalog -Preset $taskInput.preset `
    -Output (Join-Path $JobDirectory 'report.json') -Seed ($taskJob.config.seed+($taskJob.epochs.Count-1)*$taskJob.config.runsPerStrategy) -RunsPerStrategy $taskJob.config.runsPerStrategy `
    -TimingNote 'Standalone experiment; timing remains in per-episode reports. Aggregate viewer contains the final epoch only.' `
    -Experiment ([pscustomobject]@{jobId=$taskJob.id; mode=$taskJob.config.mode; environment=$taskJob.environment; scenarioHash=$taskJob.scenarioHash; policy=$taskJob.config.policy; sampledModel=$taskJob.epochs[-1].inputModel; outputModel=$taskJob.modelId; epoch=$taskJob.epochs.Count})
Remove-Item -LiteralPath (Join-Path $JobDirectory 'aggregate-input.json')
