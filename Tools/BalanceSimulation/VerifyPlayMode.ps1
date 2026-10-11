param([string]$UnityCli = 'unity', [string]$Editor = '', [int]$TimeoutSeconds = 240)
$ErrorActionPreference = 'Stop'
$taskProject = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$taskVersion = [regex]::Match((Get-Content (Join-Path $taskProject 'ProjectSettings/ProjectVersion.txt') -Raw), 'm_EditorVersion: (\S+)').Groups[1].Value
if (-not $Editor) { $Editor = Join-Path $env:ProgramFiles "Unity/Hub/Editor/$taskVersion/Editor/Unity.exe" }
if (-not (Test-Path -LiteralPath $Editor)) { throw 'Pass -Editor with the installed matching Unity.exe.' }
$taskData = Join-Path (Split-Path $Editor) 'Data'
$taskEvidence = Join-Path $taskProject ('Reports/BalanceSimulation/' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff') + '-play-parity')
$taskIsolated = Join-Path $taskEvidence 'isolated-project'
$taskPlugins = Join-Path $taskIsolated 'Assets/Plugins'
$taskNative = Join-Path $taskEvidence 'native'
$taskEdit = Join-Path $taskEvidence 'edit'
foreach ($taskFolder in @($taskPlugins,$taskNative,$taskEdit,(Join-Path $taskIsolated 'Packages'),(Join-Path $taskIsolated 'ProjectSettings'),
    (Join-Path $taskIsolated 'Assets/Runtime'),(Join-Path $taskIsolated 'Assets/Editor'))) { New-Item -ItemType Directory -Path $taskFolder -Force | Out-Null }
# Only packages referenced by compiled gameplay and their dependencies are imported.
# Source packages remain read-only local dependencies; no source asset copy.
$taskDependencies = [ordered]@{}
$taskPackages = @{}; $taskAssemblyPackages = @{}
foreach ($taskPackage in (Get-ChildItem (Join-Path $taskProject 'Library/PackageCache') -Directory)) {
    $taskPackageJson = Join-Path $taskPackage.FullName 'package.json'
    if (Test-Path -LiteralPath $taskPackageJson) {
        $taskPackageInfo = Get-Content -LiteralPath $taskPackageJson -Raw | ConvertFrom-Json
        $taskPackages[$taskPackageInfo.name] = @{ path=$taskPackage.FullName; info=$taskPackageInfo }
        foreach ($taskDefinition in (Get-ChildItem $taskPackage.FullName -Filter '*.asmdef' -Recurse -File)) {
            $taskAsmInfo = Get-Content $taskDefinition.FullName -Raw | ConvertFrom-Json
            $taskAssemblyPackages[$taskAsmInfo.name] = $taskPackageInfo.name
        }
    }
}
$taskQueue = [Collections.Generic.Queue[string]]::new()
foreach ($taskDll in (Get-ChildItem (Join-Path $taskProject 'Library/ScriptAssemblies') -Filter 'Assembly-CSharp*.dll' | Where-Object Name -NotMatch 'Editor')) {
    foreach ($taskReference in ([Reflection.Assembly]::LoadFile($taskDll.FullName).GetReferencedAssemblies())) {
        if ($taskAssemblyPackages.ContainsKey($taskReference.Name)) { $taskQueue.Enqueue($taskAssemblyPackages[$taskReference.Name]) }
    }
}
while ($taskQueue.Count) {
    $taskName = $taskQueue.Dequeue()
    if ($taskDependencies.Contains($taskName) -or -not $taskPackages.ContainsKey($taskName)) { continue }
    $taskPackage = $taskPackages[$taskName]
    $taskDependencies[$taskName] = 'file:' + $taskPackage.path.Replace('\','/')
    if ($taskPackage.info.dependencies) { foreach ($taskDependency in $taskPackage.info.dependencies.PSObject.Properties.Name) { $taskQueue.Enqueue($taskDependency) } }
}
@{dependencies=$taskDependencies} | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $taskIsolated 'Packages/manifest.json') -Encoding utf8
Copy-Item -LiteralPath (Join-Path $taskProject 'ProjectSettings/ProjectVersion.txt') -Destination (Join-Path $taskIsolated 'ProjectSettings/ProjectVersion.txt')
$taskPlayerSettings = Get-Content (Join-Path $taskProject 'ProjectSettings/ProjectSettings.asset') -Raw
$taskPlayerSettings = [regex]::Replace($taskPlayerSettings, '(?m)^  companyName:.*$', '  companyName: BalanceParityIsolation')
$taskPlayerSettings = [regex]::Replace($taskPlayerSettings, '(?m)^  productName:.*$', '  productName: BalanceParity-' + [Guid]::NewGuid().ToString('N'))
$taskPlayerSettings | Set-Content (Join-Path $taskIsolated 'ProjectSettings/ProjectSettings.asset') -Encoding utf8
# Preserve compiled gameplay byte-for-byte. Native fixture scripts live only in the temporary project.
$taskGameAssemblies = @('Assembly-CSharp','Assembly-CSharp-firstpass')
foreach ($taskDefinition in (Get-ChildItem (Join-Path $taskProject 'Assets') -Filter '*.asmdef' -Recurse -File)) {
    $taskAsm = Get-Content $taskDefinition.FullName -Raw | ConvertFrom-Json
    if ($taskAsm.name -notmatch 'Editor|Tests|TestFramework|IntegrationTests' -and 'Editor' -notin @($taskAsm.includePlatforms)) { $taskGameAssemblies += $taskAsm.name }
}
foreach ($taskName in ($taskGameAssemblies | Select-Object -Unique)) {
    $taskDll = Join-Path $taskProject "Library/ScriptAssemblies/$taskName.dll"
    if (Test-Path -LiteralPath $taskDll) { Copy-Item -LiteralPath $taskDll -Destination $taskPlugins }
}
foreach ($taskDll in (Get-ChildItem (Join-Path $taskProject 'Assets') -Filter '*.dll' -File -Recurse | Where-Object FullName -NotMatch '[\\/]Editor[\\/]')) {
    try { $null = [Reflection.AssemblyName]::GetAssemblyName($taskDll.FullName) } catch { continue }
    $taskDestination = Join-Path $taskPlugins $taskDll.Name
    if (-not (Test-Path -LiteralPath $taskDestination)) { Copy-Item -LiteralPath $taskDll.FullName -Destination $taskDestination }
}
$taskFixtureSource = Join-Path $PSScriptRoot 'Runtime/ParityFixture.cs'
$taskFixtureName = 'BalanceParityFixture_' + (Get-FileHash $taskFixtureSource -Algorithm SHA256).Hash.Substring(0,12)
$taskFixture = Join-Path $taskPlugins ($taskFixtureName + '.dll')
$taskReferences = @(Get-ChildItem (Join-Path $taskData 'Managed') -Filter '*.dll' -Recurse; Get-ChildItem (Join-Path $taskProject 'Library/ScriptAssemblies') -Filter '*.dll';
    Get-ChildItem (Join-Path $taskData 'MonoBleedingEdge/lib/mono/4.8-api') -Filter '*.dll'; Get-ChildItem (Join-Path $taskData 'MonoBleedingEdge/lib/mono/4.8-api/Facades') -Filter '*.dll')
$taskRsp = Join-Path $taskEvidence 'fixture.rsp'
@('/nologo','/target:library','/langversion:latest','/nostdlib',('/out:"'+$taskFixture+'"')) + @($taskReferences|ForEach-Object {'/r:"'+$_.FullName+'"'}) + @('"'+$taskFixtureSource+'"') | Set-Content $taskRsp
& (Join-Path $taskData 'NetCoreRuntime/dotnet.exe') (Join-Path $taskData 'DotNetSdkRoslyn/csc.dll') "@$taskRsp"
if ($LASTEXITCODE -ne 0) { throw 'Parity fixture compilation failed.' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Runtime/ParityPlayHarness.cs') -Destination (Join-Path $taskIsolated 'Assets/Runtime/ParityPlayHarness.cs')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Runtime/ParityPlayBootstrap.cs') -Destination (Join-Path $taskIsolated 'Assets/Editor/ParityPlayBootstrap.cs')
@{name='BalanceParityNative';overrideReferences=$true;precompiledReferences=@(($taskFixtureName+'.dll'),'Assembly-CSharp.dll');autoReferenced=$true} | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $taskIsolated 'Assets/Runtime/BalanceParityNative.asmdef')
@{name='BalanceParityBootstrap';references=@('BalanceParityNative');includePlatforms=@('Editor');autoReferenced=$true} | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $taskIsolated 'Assets/Editor/BalanceParityBootstrap.asmdef')
# Execute the Edit Mode side in the existing Editor, with its normal effect ownership adapter.
$taskToolsRoot = $PSScriptRoot; $taskAdapterKind = 'parity'
$taskFixtureLiteral = $taskFixture.Replace('\','\\').Replace('"','\"')
$taskEditLiteral = $taskEdit.Replace('\','\\').Replace('"','\"')
$taskSource = @'
if (UnityEditor.EditorApplication.isPlaying) throw new System.InvalidOperationException("Keep the original project in Edit Mode for isolated parity verification.");
var fixture = System.Reflection.Assembly.LoadFrom("__FIXTURE__");
var type = fixture.GetType("BalanceParity.Scenario", true);
var names = (string[])type.GetField("Names").GetValue(null);
var savedRandom = UnityEngine.Random.state;
try {
 foreach (var name in names) {
  var scenario = System.Activator.CreateInstance(type, new object[] { name, 1f/60f, true, new System.Action<Battle.EffectController>(OwnEditorEffects) });
  try {
   type.GetMethod("Prepare").Invoke(scenario,null);
   while (!(bool)type.GetProperty("Complete").GetValue(scenario)) type.GetMethod("Step").Invoke(scenario,null);
   var trace = type.GetField("trace").GetValue(scenario);
   var converter = System.AppDomain.CurrentDomain.GetAssemblies().First(a=>a.GetName().Name=="Newtonsoft.Json").GetType("Newtonsoft.Json.JsonConvert",true);
   var json = (string)converter.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new[]{trace});
   System.IO.File.WriteAllText(System.IO.Path.Combine("__EDIT__",name+".json"),json);
  } finally { ((System.IDisposable)scenario).Dispose(); }
 }
} finally { UnityEngine.Random.state = savedRandom; }
return new { editModeCases=names.Length };
'@
$taskSource = $taskSource.Replace('__FIXTURE__',$taskFixtureLiteral).Replace('__EDIT__',$taskEditLiteral)
. (Join-Path $PSScriptRoot 'Internal/UnityBridge.ps1')
$null = Invoke-BalanceChunk @{}
$taskPreviousOutput = $env:BALANCE_PARITY_OUTPUT
$env:BALANCE_PARITY_OUTPUT = $taskNative
try {
    $taskProcess = Start-Process -FilePath $Editor -ArgumentList @('-batchmode','-nographics','-projectPath',('"'+$taskIsolated+'"'),'-logFile',('"'+(Join-Path $taskEvidence 'native-editor.log')+'"')) -WindowStyle Hidden -PassThru
} finally { $env:BALANCE_PARITY_OUTPUT = $taskPreviousOutput }
$taskWatch = [Diagnostics.Stopwatch]::StartNew()
while (-not $taskProcess.HasExited -and $taskWatch.Elapsed.TotalSeconds -lt $TimeoutSeconds) { Start-Sleep -Seconds 2; $taskProcess.Refresh() }
if (-not $taskProcess.HasExited) { $taskProcess.Kill(); throw "Isolated native Editor timed out. Evidence retained at $taskEvidence" }
if ($taskProcess.ExitCode -ne 0 -or -not (Test-Path (Join-Path $taskNative 'completed.json'))) { throw "Native parity run failed. Read $(Join-Path $taskEvidence 'native-editor.log')" }
& (Join-Path $PSScriptRoot 'ComparePlayTraces.ps1') -Evidence $taskEvidence
Write-Output "Native Play Mode / Edit Mode traces match. Evidence: $taskEvidence"
