$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$root = Join-Path $repoRoot 'Assets/Scripts'
$dotnet = Join-Path $env:ProgramFiles 'dotnet/dotnet.exe'
$sdk = & $dotnet --list-sdks | Where-Object { $_ -match '^9\.' } | Select-Object -Last 1
if (!$sdk -or $sdk -notmatch '^(\S+) \[(.+)\]$') { throw 'Install the .NET 9 SDK to run these checks.' }
$compiler = Join-Path $Matches[2] ($Matches[1] + '/Roslyn/bincore/csc.dll')
$refVersion = Get-ChildItem (Join-Path (Split-Path $dotnet) 'packs/Microsoft.NETCore.App.Ref') -Directory |
    Where-Object { $_.Name -like '9.*' } | Sort-Object { [version]$_.Name } | Select-Object -Last 1
$refs = Get-ChildItem (Join-Path $refVersion.FullName 'ref/net9.0') -Filter '*.dll' | ForEach-Object { '-r:' + $_.FullName }
$names = @('BaseEffect','ActiveEffect','EffectController','EffectVisualType','Bleed','BarrierSurge',
    'BleedPhysicalDamageBuff','CriticalMomentum','CriticalRegeneration','LightAbsorption',
    'DarknessAbsorption','EvasiveMomentum','RelentlessMomentum','Scar','Overcharge','Freeze',
    'NextAttackModifierEffect','Chill','Expose','Sunder','Distract')
$sources = @($names | ForEach-Object { Join-Path $root ('Battle/Effects/' + $_ + '.cs') })
$sources += @('Battle/EnemyUnit.cs','Battle/Attacker.cs','Battle/ITarget.cs','Battle/Barrier.cs',
    'Battle/MysticHealth.cs','StatType.cs','SkillTree/BonusZone.cs','Battle/EnemySystem/EnemySpawnData.cs',
    'Battle/MiniGames/Rewards/MiniGameMoreStatBuffEffect.cs') | ForEach-Object { Join-Path $root $_ }
$testOutput = Join-Path $env:TEMP ('SkillTreeCombatRegression-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $testOutput | Out-Null
Write-Output ('Build output: ' + $testOutput)
& $dotnet $compiler -nologo -nowarn:0067,0649 -target:exe ('-out:' + $testOutput + '/Regression.dll') @refs `
    (Join-Path $PSScriptRoot 'Program.cs') (Join-Path $PSScriptRoot 'Stubs.cs') @sources
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
'{"runtimeOptions":{"tfm":"net9.0","framework":{"name":"Microsoft.NETCore.App","version":"9.0.0"}}}' |
    Set-Content (Join-Path $testOutput 'Regression.runtimeconfig.json')
& $dotnet (Join-Path $testOutput 'Regression.dll')
exit $LASTEXITCODE
