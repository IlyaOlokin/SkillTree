$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$dotnet = Join-Path $env:ProgramFiles 'dotnet/dotnet.exe'
$sdk = & $dotnet --list-sdks | Where-Object { $_ -match '^9\.' } | Select-Object -Last 1
if (!$sdk -or $sdk -notmatch '^(\S+) \[(.+)\]$') { throw 'Install the .NET 9 SDK.' }
$compiler = Join-Path $Matches[2] ($Matches[1] + '/Roslyn/bincore/csc.dll')
$refVersion = Get-ChildItem (Join-Path (Split-Path $dotnet) 'packs/Microsoft.NETCore.App.Ref') -Directory |
    Where-Object { $_.Name -like '9.*' } | Sort-Object { [version]$_.Name } | Select-Object -Last 1
$refs = Get-ChildItem (Join-Path $refVersion.FullName 'ref/net9.0') -Filter '*.dll' | ForEach-Object { '-r:' + $_.FullName }
$names = @('SaveDataModels','SaveDocumentType','SaveEnvelope','SaveFileCodec','SaveFileStorage',
    'SaveMigrationPipeline','SavePaths','SaveProfileManager','GameSaveCoordinator','WebGLPersistentStorageSync')
$sources = @($names | ForEach-Object { Join-Path $repo ('Assets/Scripts/SaveSystem/' + $_ + '.cs') })
$sources += Join-Path $repo 'Assets/Scripts/SkillTree/SkillTreeSaveService.cs'
$output = Join-Path $repo ('Temp/SaveRegression-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $output | Out-Null
& $dotnet $compiler -nologo -nowarn:0067,0649 -target:exe ('-out:'+$output+'/Regression.dll') @refs `
    (Join-Path $PSScriptRoot 'Program.cs') (Join-Path $PSScriptRoot 'Stubs.cs') @sources
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
'{"runtimeOptions":{"tfm":"net9.0","framework":{"name":"Microsoft.NETCore.App","version":"9.0.0"}}}' |
    Set-Content (Join-Path $output 'Regression.runtimeconfig.json')
& $dotnet (Join-Path $output 'Regression.dll') (Join-Path $output 'SavesUnderTest')
exit $LASTEXITCODE
