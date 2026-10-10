param([Parameter(Mandatory)][string]$JobDirectory,[int]$Epoch=0,[string]$InputFile='',[string]$OutputFile='')
$ErrorActionPreference='Stop'
if(-not $InputFile){$InputFile=Join-Path $JobDirectory 'aggregate-input.json'}
if(-not $OutputFile){$OutputFile=Join-Path $JobDirectory 'report.json'}
$taskInput=Get-Content -LiteralPath $InputFile -Raw|ConvertFrom-Json
$taskJob=Get-Content (Join-Path $JobDirectory 'job.json') -Raw|ConvertFrom-Json
if($Epoch -eq 0){$Epoch=$taskJob.epochs.Count}
$taskEpoch=$taskJob.epochs[$Epoch-1]
& (Join-Path $PSScriptRoot '../Internal/AssembleCampaignReport.ps1') -Campaigns $taskInput.campaigns -TreeCatalog $taskInput.treeCatalog -Preset $taskInput.preset `
    -Output $OutputFile -Seed ($taskJob.config.seed+($Epoch-1)*$taskJob.config.runsPerStrategy) -RunsPerStrategy $taskJob.config.runsPerStrategy `
    -TimingNote 'Standalone experiment; timing remains in per-episode reports. Viewer contains this sampled epoch only.' `
    -Experiment ([pscustomobject]@{jobId=$taskJob.id; mode=$taskJob.config.mode; environment=$taskJob.environment; scenarioHash=$taskJob.scenarioHash; policy=$taskJob.config.policy; sampledModel=$taskEpoch.inputModel; outputModel=$(if($taskEpoch.outputModel){$taskEpoch.outputModel}else{$taskEpoch.inputModel}); epoch=$Epoch})
Remove-Item -LiteralPath $InputFile
