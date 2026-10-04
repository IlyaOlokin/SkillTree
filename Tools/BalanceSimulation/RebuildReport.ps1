param([Parameter(Mandatory)][string]$Report)
$ErrorActionPreference = 'Stop'
$taskToolsRoot = $PSScriptRoot
$taskProject = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
. (Join-Path $PSScriptRoot 'Internal/Reports.ps1')
$taskOutput = Resolve-BalanceReportPath $Report 'rebuild'
$taskInput = if (Test-Path -LiteralPath ($taskOutput + '.gz')) { $taskOutput + '.gz' } else { $taskOutput }
$taskData = Read-BalanceReport $taskInput
if (-not $taskData.summary -or -not $taskData.treeCatalog) { throw 'Choose a final balance report, including summary and tree catalog.' }
Publish-BalanceReport $taskOutput $taskData
# Remove a legacy raw file only after the published archive retains identical JSON values.
if (Test-Path -LiteralPath $taskOutput) {
    $before = Read-BalanceReport $taskOutput | ConvertTo-Json -Depth 30 -Compress
    $after = Read-BalanceReport ($taskOutput + '.gz') | ConvertTo-Json -Depth 30 -Compress
    if ($before -cne $after) { throw 'Archive validation failed; legacy source retained.' }
    Remove-Item -LiteralPath $taskOutput
}
