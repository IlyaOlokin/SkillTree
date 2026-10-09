param([int]$Port=8877,[switch]$NoBrowser)
$ErrorActionPreference='Stop'
function Find-LabRuntime([string]$Name,[string[]]$Candidates){$command=Get-Command $Name -ErrorAction SilentlyContinue;if($command){return $command.Source};foreach($candidate in $Candidates){if(Test-Path -LiteralPath $candidate){return $candidate}};throw "Runtime $Name not found."}
$taskDependencies=Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies'
$taskNode=Find-LabRuntime 'node' @((Join-Path $taskDependencies 'node/bin/node.exe'))
$env:BALANCE_PWSH=Find-LabRuntime 'pwsh' @((Join-Path $taskDependencies 'native/powershell/pwsh.exe'),'C:/Program Files/PowerShell/7/pwsh.exe')
$env:BALANCE_UNITY=Find-LabRuntime 'unity' @((Join-Path $env:LOCALAPPDATA 'Unity/bin/unity.exe'))
$env:BALANCE_LAB_PORT=$Port.ToString()
$taskUrl="http://127.0.0.1:$Port"
Write-Host "Balance Lab: $taskUrl"
Write-Host 'Keep this terminal open. Open MainScene in Unity Edit Mode with Pipeline ready.'
$taskOpened=$false
& $taskNode (Join-Path $PSScriptRoot 'Lab/lab.mjs') | ForEach-Object {
    Write-Host $_
    if(-not $NoBrowser -and -not $taskOpened -and $_ -like 'Balance Lab:*') { Start-Process $taskUrl; $taskOpened=$true }
}
