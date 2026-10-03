# One-time migration of this project's text-serialized scene. Default is read-only validation.
# Deliberately supports only root-node prefabs; unexpected layouts stop before writing.
param([switch]$Apply, [switch]$VerifyMigration)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$nodeGuids = @('ae1f51100ecafc144b31813065a5776c','856c9af5df3a9db499962c834fe1d0c1','1647e032a6deb814a9b070941d7f5f04')
function Blocks([string]$text) {
    foreach ($m in [regex]::Matches($text, '(?ms)^--- !u!(?<type>\d+) &(?<id>-?\d+)[^\n]*\n(?<body>.*?)(?=^--- !u!|\z)')) {
        [pscustomobject]@{ Type=$m.Groups['type'].Value; Id=$m.Groups['id'].Value; Body=$m.Groups['body'].Value; Text=$m.Value; Start=$m.Index; Length=$m.Length }
    }
}
function Value([string]$text, [string]$pattern) { [regex]::Match($text,$pattern).Groups[1].Value.TrimEnd("`r") }
function IsNode($b) { $b.Type -eq '114' -and $nodeGuids -contains (Value $b.Body 'm_Script:.*guid: ([a-f0-9]+)') }
function FileId([string]$body, [string]$field) { Value $body ($field + ': \{fileID: (-?\d+)') }
function DecodeName([string]$name) { if ($name.StartsWith('"')) { return ConvertFrom-Json $name }; return $name }
$prefabs = @{}
foreach ($file in Get-ChildItem (Join-Path $repo 'Assets/Prefabs/Nodes') -Filter '*.prefab') {
    $guid = Value ([IO.File]::ReadAllText($file.FullName+'.meta')) 'guid: ([a-f0-9]+)'
    $docs = @(Blocks ([IO.File]::ReadAllText($file.FullName)))
    $nodes = @($docs | Where-Object { IsNode $_ })
    if ($nodes.Count -eq 0) { continue }
    $root = @($docs | Where-Object { $_.Type -in @('4','224') -and (FileId $_.Body 'm_Father') -eq '0' })
    if ($root.Count -ne 1 -or $nodes.Count -ne 1) { throw "Unsupported node prefab $($file.Name)" }
    $gameObject = FileId $root[0].Body 'm_GameObject'
    if ((FileId $nodes[0].Body 'm_GameObject') -ne $gameObject) { throw 'Nested node prefab requires Unity migration.' }
    $go = $docs | Where-Object { $_.Id -eq $gameObject }
    $prefabs[$guid] = @{ Node=$nodes[0]; Transform=$root[0].Id; GameObject=$gameObject; Name=(DecodeName (Value $go.Body '(?m)^  m_Name: (.*)')) }
}
$scenePath = 'Assets/Scenes/MainScene.unity'
$path = Join-Path $repo $scenePath
$original = [IO.File]::ReadAllText($path)
$docs = @(Blocks $original)
$byId = @{}; $transforms = @{}; $rootTransforms = @{}
foreach ($b in $docs) { $byId[$b.Id] = $b }
function Override($instance, [string]$target, [string]$property) {
    $pattern = '(?ms)    - target: \{fileID: ' + [regex]::Escape($target) + ',[^\n]*\n      propertyPath: ' + [regex]::Escape($property) + '\r?\n      value: ([^\r\n]*)'
    return Value $instance.Body $pattern
}
foreach ($b in $docs) {
    if ($b.Type -notin @('4','224')) { continue }
    if ($b.Text -match '^---[^\n]* stripped') {
        $instanceId = FileId $b.Body 'm_PrefabInstance'
        $instance = $byId[$instanceId]
        $guid = Value $instance.Body 'm_SourcePrefab:.*guid: ([a-f0-9]+)'
        if (!$prefabs.ContainsKey($guid)) { continue }
        $prefab = $prefabs[$guid]
        if ((FileId $b.Body 'm_CorrespondingSourceObject') -ne $prefab.Transform) { continue }
        $parent = FileId $instance.Body 'm_TransformParent'
        $name = Override $instance $prefab.GameObject 'm_Name'
        if (!$name) { $name = $prefab.Name } else { $name = DecodeName $name }
        $rootTransforms[$instanceId] = $b.Id
    } else {
        $parent = FileId $b.Body 'm_Father'
        $go = $byId[(FileId $b.Body 'm_GameObject')]
        $name = DecodeName (Value $go.Body '(?m)^  m_Name: (.*)')
    }
    $transforms[$b.Id] = @{ Parent=$parent; Name=$name; Block=$b }
}
$children = @{}
foreach ($id in $transforms.Keys) {
    $body = $transforms[$id].Block.Body
    $list = Value $body '(?ms)^  m_Children:\r?\n((?:  -[^\n]*\n)*)'
    $children[$id] = @([regex]::Matches($list,'fileID: (-?\d+)') | ForEach-Object { $_.Groups[1].Value })
}
$sceneRoots = $docs | Where-Object { $_.Type -eq '1660057539' }
$children['0'] = @([regex]::Matches($sceneRoots.Body,'fileID: (-?\d+)') | ForEach-Object { $_.Groups[1].Value })
$paths = @{}
function Hierarchy([string]$id) {
    if ($paths.ContainsKey($id)) { return $paths[$id] }
    if (!$transforms.ContainsKey($id)) { throw "Cannot resolve transform $id; use Unity migration for this layout." }
    $t = $transforms[$id]; $siblings = $children[$t.Parent]
    if ($null -eq $siblings) { throw "Missing parent child list: transform=$id parent=$($t.Parent) name=$($t.Name)" }
    $index = [array]::IndexOf($siblings, $id)
    if ($index -lt 0) { throw "Transform $id not found in parent $($t.Parent) children." }
    $part = $t.Name + '[' + $index + ']'
    if ($t.Parent -ne '0') { $part = (Hierarchy $t.Parent) + '/' + $part }
    $paths[$id] = $part
    return $part
}
$records = [Collections.Generic.List[object]]::new()
foreach ($b in $docs) {
    if ($b.Type -eq '1001') {
        $guid = Value $b.Body 'm_SourcePrefab:.*guid: ([a-f0-9]+)'
        if (!$prefabs.ContainsKey($guid)) { continue }
        $p = $prefabs[$guid]; $target = $p.Node.Id
        $removed = Value $b.Body '(?ms)    m_RemovedComponents:\r?\n((?:    -[^\n]*\n)*)'
        if ($removed -match ('fileID: ' + $target + ',')) { continue }
        $id = Override $b $target 'saveId'
        if (!$id) { $id = Value $p.Node.Body '(?m)^  saveId: (.*)' }
        $transformId = $rootTransforms[$b.Id]
        $records.Add(@{ Block=$b; Id=$id; Guid=$guid; Target=$target; Transform=$transformId })
    } elseif ((IsNode $b) -and $b.Text -notmatch '^---[^\n]* stripped') {
        $goId = FileId $b.Body 'm_GameObject'
        $go = $byId[$goId]
        if ($go.Text -match '^---[^\n]* stripped') {
            $matching = @($rootTransforms[(FileId $go.Body 'm_PrefabInstance')])
        } else {
            $matching = @($transforms.Keys | Where-Object { (FileId $transforms[$_].Block.Body 'm_GameObject') -eq $goId })
        }
        if ($matching.Count -ne 1) { throw "Cannot resolve node transform for $($b.Id)" }
        $records.Add(@{ Block=$b; Id=(Value $b.Body '(?m)^  saveId: (.*)'); Transform=$matching[0] })
    }
}
$seen = @{}; $seenLegacy = @{}; $storedAliases = @{}; $edits = [Collections.Generic.List[object]]::new()
foreach ($r in $records) {
    $fallback = $scenePath + ':' + (Hierarchy $r.Transform)
    if ($seenLegacy.ContainsKey($fallback)) { throw "Duplicate fallback $fallback" }; $seenLegacy[$fallback] = $true
    if ($r.Id) {
        if ($seen.ContainsKey($r.Id)) { throw "Duplicate explicit ID $($r.Id)" }
        $seen[$r.Id] = $true
        $aliases = @()
        if ($r.Guid) {
            $size = Override $r.Block $r.Target 'legacySaveIds.Array.size'
            if ($size) {
                for ($i=0; $i -lt [int]$size; $i++) {
                    $aliases += DecodeName (Override $r.Block $r.Target ("legacySaveIds.Array.data[$i]"))
                }
            }
        } else {
            $list = Value $r.Block.Body '(?ms)^  legacySaveIds:\r?\n((?:  -[^\n]*\n)*)'
            foreach ($line in [regex]::Matches($list,'(?m)^  - (.*)')) { $aliases += DecodeName $line.Groups[1].Value }
        }
        foreach ($alias in $aliases) {
            if (!$alias -or $storedAliases.ContainsKey($alias)) { throw "Empty/duplicate migration alias: $alias" }
            $storedAliases[$alias] = $r.Id
        }
        if ($VerifyMigration -and $aliases.Count -gt 0 -and $aliases -notcontains $fallback) { throw "Migration path mismatch at $fallback" }
        continue
    }
    $r.Id = [guid]::NewGuid().ToString('N'); $seen[$r.Id] = $true
    $quoted = ConvertTo-Json -InputObject $fallback -Compress
    $text = $r.Block.Text
    if ($r.Guid) {
        if ($text -match 'propertyPath: legacySaveIds') { throw 'Unexpected partial migration' }
        $prefix = "    - target: {fileID: $($r.Target), guid: $($r.Guid), type: 3}`n      propertyPath: "
        $suffix = "`n      objectReference: {fileID: 0}`n"
        $extra = $prefix + "saveId`n      value: $($r.Id)" + $suffix +
            $prefix + "legacySaveIds.Array.size`n      value: 1" + $suffix +
            $prefix + "legacySaveIds.Array.data[0]`n      value: $quoted" + $suffix
        # Existing empty saveId overrides are replaced, never duplicated.
        $pattern = '(?ms)    - target: \{fileID: ' + $r.Target + ',[^\n]*\n      propertyPath: saveId\r?\n      value: [^\n]*\n      objectReference: [^\n]*\n'
        $text = [regex]::Replace($text, $pattern, '')
        $text = $text.Replace("    m_Modifications:`n", "    m_Modifications:`n" + $extra)
    } else {
        $text = [regex]::Replace($text, '(?m)^  saveId: [^\r\n]*\r?\n', "  saveId: $($r.Id)`n  legacySaveIds:`n  - $quoted`n")
    }
    if ($text -eq $r.Block.Text) { throw 'No replacement made' }
    $edits.Add(@{ Start=$r.Block.Start; Length=$r.Block.Length; Text=$text })
}
foreach ($alias in $storedAliases.Keys) {
    if ($seen.ContainsKey($alias) -and $alias -ne $storedAliases[$alias]) { throw "Migration alias conflicts with explicit ID: $alias" }
}
if ($Apply -and $edits.Count -gt 0) {
    $updated = [Text.StringBuilder]::new()
    $cursor = 0
    foreach ($e in ($edits | Sort-Object { [int]$_.Start })) {
        if ($e.Start -lt $cursor) { throw 'Overlapping scene edits' }
        [void]$updated.Append($original.Substring($cursor,$e.Start-$cursor))
        [void]$updated.Append($e.Text)
        $cursor = $e.Start + $e.Length
    }
    [void]$updated.Append($original.Substring($cursor))
    [IO.File]::WriteAllText($path,$updated.ToString(),[Text.UTF8Encoding]::new($false))
}
Write-Output ("Resolved {0} nodes; {1} need IDs; {2} stored migration aliases; applied={3}. All hierarchy paths and IDs checked." -f $records.Count,$edits.Count,$storedAliases.Count,$Apply)
if (!$Apply -and $edits.Count -gt 0) { exit 1 }
