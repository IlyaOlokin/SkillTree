function ConvertTo-CSharpLiteral($Value) {
    if ($null -eq $Value) { return 'null' }
    if ($Value -is [string]) { return '"' + $Value.Replace('\', '\\').Replace('"', '\"').Replace("`r", '\r').Replace("`n", '\n').Replace("`t", '\t') + '"' }
    if ($Value -is [bool]) { return $Value.ToString().ToLowerInvariant() }
    if ($Value -is [Collections.IDictionary]) {
        $entries = @($Value.Keys | ForEach-Object { '{' + (ConvertTo-CSharpLiteral ([string]$_)) + ',' + (ConvertTo-CSharpLiteral $Value[$_]) + '}' })
        return 'new System.Collections.Generic.Dictionary<string,object> {' + ($entries -join ',') + '}'
    }
    if ($Value -is [pscustomobject]) {
        $entries = @($Value.PSObject.Properties | ForEach-Object { '{' + (ConvertTo-CSharpLiteral $_.Name) + ',' + (ConvertTo-CSharpLiteral $_.Value) + '}' })
        return 'new System.Collections.Generic.Dictionary<string,object> {' + ($entries -join ',') + '}'
    }
    if ($Value -is [Collections.IEnumerable]) {
        return 'new object[] {' + (@($Value | ForEach-Object { ConvertTo-CSharpLiteral $_ }) -join ',') + '}'
    }
    if ($Value -is [int] -or $Value -is [long]) { return $Value.ToString([Globalization.CultureInfo]::InvariantCulture) }
    throw "Unsupported configuration value: $Value"
}
$taskEffectSource = Get-Content (Join-Path $taskToolsRoot 'Runtime/EditorEffectOwnership.cs') -Raw
$taskExportSource = Get-Content (Join-Path $taskToolsRoot 'Runtime/EditorExports.cs') -Raw
$taskAdapterBody = 'var requestExistingModifiers = new System.Collections.Generic.HashSet<SkillTree.Modifier>(UnityEngine.Resources.FindObjectsOfTypeAll<SkillTree.Modifier>());' +
    [Environment]::NewLine + $taskEffectSource + [Environment]::NewLine + $taskExportSource +
    [Environment]::NewLine + 'try {' + [Environment]::NewLine + $taskSource + [Environment]::NewLine +
    '} catch (System.Exception exception) { return new { adapterError = exception.ToString() }; } finally {' +
    ' DisposeEditorEffects(); System.AppDomain.CurrentDomain.SetData("balanceSimulation.config", null);' +
    ' foreach (var requestModifier in UnityEngine.Resources.FindObjectsOfTypeAll<SkillTree.Modifier>())' +
    ' if (!requestExistingModifiers.Contains(requestModifier) && !UnityEditor.AssetDatabase.Contains(requestModifier)) UnityEngine.Object.DestroyImmediate(requestModifier); }'
$taskHasher = [Security.Cryptography.SHA256]::Create()
try { $taskAdapterHash = [BitConverter]::ToString($taskHasher.ComputeHash([Text.Encoding]::UTF8.GetBytes($taskAdapterBody))).Replace('-','') }
finally { $taskHasher.Dispose() }
$taskAdapterKey = 'balanceSimulation.adapter.' + $taskAdapterKind
$taskCommandCount = 0
function Invoke-BalanceCode([string]$Code) {
    $taskRequestDirectory = Join-Path $taskToolsRoot 'Scratch'
    New-Item -ItemType Directory -Force -Path $taskRequestDirectory | Out-Null
    $taskRequestPath = Join-Path $taskRequestDirectory ('request-' + [Guid]::NewGuid().ToString('N') + '.cs')
    try {
        $Code | Set-Content -LiteralPath $taskRequestPath -Encoding utf8
        $script:taskCommandCount++
        $taskRaw = & $UnityCli command eval_file --caller plugin --skill unity-cli --project-path $taskProject --format json --timeout 120000 --file $taskRequestPath
        $taskExitCode = $LASTEXITCODE
    } finally {
        if (Test-Path -LiteralPath $taskRequestPath) { Remove-Item -LiteralPath $taskRequestPath }
    }
    if ($taskExitCode -ne 0) { throw ($taskRaw -join [Environment]::NewLine) }
    $taskEnvelope = ($taskRaw -join [Environment]::NewLine) | ConvertFrom-Json
    if (-not $taskEnvelope.success -or -not $taskEnvelope.data.result.success) { throw ($taskRaw -join [Environment]::NewLine) }
    $taskResult = $taskEnvelope.data.result.result
    if ($taskResult.adapterError) { throw $taskResult.adapterError }
    return $taskResult
}
function Invoke-BalanceChunk($Configuration) {
    # A cached delegate runs the full adapter; later chunks compile only this small dispatcher.
    $lookup = 'var cache = (System.Collections.Generic.Dictionary<string,object>)System.AppDomain.CurrentDomain.GetData("' + $taskAdapterKey + '");'
    $valid = 'cache != null && (string)cache["hash"] == "' + $taskAdapterHash + '"'
    $configurationCode = 'System.AppDomain.CurrentDomain.SetData("balanceSimulation.config", ' + (ConvertTo-CSharpLiteral $Configuration) + ');'
    $invoke = 'return ((System.Func<object>)cache["invoke"])();'
    $result = Invoke-BalanceCode ($lookup + ' if (!(' + $valid + ')) return new { adapterCacheMiss = true }; ' + $configurationCode + $invoke)
    if ($result.adapterCacheMiss) {
        $bootstrap = $lookup + ' if (!(' + $valid + ')) {' +
            ' var adapter = new System.Func<object>(() => {' + [Environment]::NewLine + $taskAdapterBody + [Environment]::NewLine + '});' +
            ' cache = new System.Collections.Generic.Dictionary<string,object> { {"hash","' + $taskAdapterHash + '"}, {"invoke",adapter} };' +
            ' System.AppDomain.CurrentDomain.SetData("' + $taskAdapterKey + '", cache); }' + $configurationCode + $invoke
        $result = Invoke-BalanceCode $bootstrap
    }
    return $result
}
function Invoke-BalanceExport($Configuration, [string]$Path) {
    # The work directory is new for this run. Never accept a stale export as a successful retry.
    if (Test-Path -LiteralPath $Path) { throw 'An export already exists at this path.' }
    $Configuration.exportPath = [IO.Path]::GetFullPath($Path)
    try { $null = Invoke-BalanceChunk $Configuration }
    catch { if (-not (Test-Path -LiteralPath $Path)) { throw }; Write-Warning 'CLI acknowledgement failed; using the atomic on-disk export.' }
    return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
}

