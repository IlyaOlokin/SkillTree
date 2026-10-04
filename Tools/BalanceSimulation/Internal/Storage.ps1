# Shared storage for runners, offline report rebuilding and recovery.
$taskReportRoot = Join-Path $taskProject 'Reports/BalanceSimulation'
function Resolve-BalanceReportPath([string]$Requested, [string]$Kind) {
    if (-not $Requested) {
        return Join-Path $taskReportRoot ((Get-Date -Format 'yyyyMMdd-HHmmss-fff') + '-' + $Kind + '/report.json')
    }
    $resolved = [IO.Path]::GetFullPath($Requested)
    if ($resolved.EndsWith('.json.gz', [StringComparison]::OrdinalIgnoreCase)) { $resolved = $resolved.Substring(0, $resolved.Length - 3) }
    $root = [IO.Path]::GetFullPath($taskReportRoot).TrimEnd('\','/') + [IO.Path]::DirectorySeparatorChar
    if (-not $resolved.StartsWith($root, [StringComparison]::OrdinalIgnoreCase) -or -not $resolved.EndsWith('.json', [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Use a .json or .json.gz report path under Reports/BalanceSimulation.'
    }
    return $resolved
}
function Write-BalanceBytes([string]$Path, [byte[]]$Bytes) {
    New-Item -ItemType Directory -Force -Path (Split-Path $Path) | Out-Null
    $temporary = $Path + '.' + [Guid]::NewGuid().ToString('N') + '.tmp'
    try { [IO.File]::WriteAllBytes($temporary, $Bytes); [IO.File]::Move($temporary, $Path, $true) }
    finally { if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary } }
}
function Write-BalanceText([string]$Path, [string]$Text) {
    Write-BalanceBytes $Path ([Text.UTF8Encoding]::new($false).GetBytes($Text))
}
function Read-BalanceReport([string]$Path) {
    if (-not $Path.EndsWith('.gz', [StringComparison]::OrdinalIgnoreCase)) { return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json }
    $file = [IO.File]::OpenRead($Path)
    $gzip = [IO.Compression.GZipStream]::new($file, [IO.Compression.CompressionMode]::Decompress)
    $reader = [IO.StreamReader]::new($gzip, [Text.Encoding]::UTF8)
    try { return $reader.ReadToEnd() | ConvertFrom-Json }
    finally { $reader.Dispose(); $gzip.Dispose(); $file.Dispose() }
}
function New-BalanceWorkDirectory([string]$Output) {
    $work = $Output + '.work'
    if ((Test-Path -LiteralPath $Output) -or (Test-Path -LiteralPath ($Output + '.gz')) -or (Test-Path -LiteralPath ([IO.Path]::ChangeExtension($Output, '.html'))) -or (Test-Path -LiteralPath $work)) {
        throw 'This report/run already exists. Choose a new output folder, or use Recover.ps1 for the saved work.'
    }
    New-Item -ItemType Directory -Path $work -Force | Out-Null
    return $work
}
function Remove-BalanceWorkDirectory([string]$Output) {
    $work = [IO.Path]::GetFullPath($Output + '.work')
    $root = [IO.Path]::GetFullPath($taskReportRoot).TrimEnd('\','/') + [IO.Path]::DirectorySeparatorChar
    if (-not $work.StartsWith($root, [StringComparison]::OrdinalIgnoreCase) -or -not $work.EndsWith('.json.work', [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid work cleanup path.' }
    if (-not (Test-Path -LiteralPath ($Output + '.gz')) -or -not (Test-Path -LiteralPath ([IO.Path]::ChangeExtension($Output, '.html')))) { throw 'Publish both final artifacts before cleaning work.' }
    if (Test-Path -LiteralPath $work) {
        if ((Get-Item -LiteralPath $work).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw 'Cannot clean linked work directory.' }
        Remove-Item -LiteralPath $work -Recurse -Force
    }
}
