param([Parameter(Mandatory)][string]$Evidence)
$ErrorActionPreference = 'Stop'
function Compare-ParityValue($Left, $Right, [string]$Path) {
    if ($Left -is [ValueType] -and $Right -is [ValueType] -and $Left -isnot [bool] -and $Right -isnot [bool]) {
        $difference = [Math]::Abs([double]$Left-[double]$Right)
        $tolerance = [Math]::Max(0.00001, [Math]::Max([Math]::Abs([double]$Left),[Math]::Abs([double]$Right))*0.000001)
        if ($Path -match '\.(tick|painEffects|scarEffects|igniteEffects|surgeEffects|received|applied)$') { $tolerance=0 }
        if ($difference -gt $tolerance) { throw "Parity mismatch $Path : $Left versus $Right" }; return
    }
    if ($Left -is [array] -and $Right -is [array]) {
        if ($Left.Count -ne $Right.Count) { throw "Parity array-length mismatch $Path" }
        for ($i=0;$i -lt $Left.Count;$i++) { Compare-ParityValue $Left[$i] $Right[$i] "$Path[$i]" }; return
    }
    if ($Left -is [pscustomobject] -and $Right -is [pscustomobject]) {
        $leftKeys=@($Left.PSObject.Properties.Name|Sort-Object);$rightKeys=@($Right.PSObject.Properties.Name|Sort-Object)
        if (($leftKeys -join ',') -ne ($rightKeys -join ',')) { throw "Parity field mismatch $Path" }
        foreach ($key in $leftKeys) { Compare-ParityValue $Left.$key $Right.$key "$Path.$key" }; return
    }
    if ($Left -cne $Right) { throw "Parity mismatch $Path" }
}
$cases=@('pain-scar-consume-expire','repeat-effect-no-recursion','barrier-loss-and-hit-binding','ignite-pool-and-expiry')
$results=@()
foreach($case in $cases) {
    $left=Get-Content (Join-Path $Evidence "edit/$case.json") -Raw|ConvertFrom-Json
    $right=Get-Content (Join-Path $Evidence "native/$case.json") -Raw|ConvertFrom-Json
    $expected=if($case -eq 'ignite-pool-and-expiry'){720}else{10}
    if($left.frames.Count -ne $expected -or $right.frames.Count -ne $expected){throw "Incomplete trace $case"}
    Compare-ParityValue $left $right $case
    switch($case) {
        'pain-scar-consume-expire' {
            if($left.frames[0].pain -le 0 -or $left.frames[1].scarEffects -ne 1 -or $left.frames[-1].painEffects -ne 0 -or $left.frames[-1].scarEffects -ne 0){throw 'Pain/Scar lifecycle did not exercise expected transitions'}
        }
        'repeat-effect-no-recursion' {
            if($left.frames[0].scarEffects -ne 2 -or $left.frames[0].received -ne 1 -or $left.frames[0].applied -ne 1 -or $left.frames[7].scarEffects -ne 1 -or $left.frames[5].armor -ne 100 -or $left.frames[5].healing -ne 0){throw 'Repeat/no-recursion/unbind/expiry transitions failed'}
        }
        'barrier-loss-and-hit-binding' {
            if($left.frames[0].health -ne 500 -or $left.frames[0].barrier -ne 1 -or $left.frames[0].pain -ne 0 -or $left.frames[0].progress -ne 0.25 -or $left.frames[0].surgeEffects -ne 2 -or $left.frames[7].progress -ne 0 -or $left.frames[5].fire -ne 10 -or $left.frames[5].regeneration -ne 1 -or $left.frames[5].surgeEffects -ne 0){throw 'Barrier absorption/HP-loss gate/binding/expiry cleanup failed'}
        }
        'ignite-pool-and-expiry' {
            if($left.frames[0].igniteEffects -ne 1 -or $left.frames[-1].igniteEffects -ne 0 -or [Math]::Abs($left.frames[-1].health-440) -gt 0.01 -or @($left.frames|Where-Object pain -gt 0).Count){throw 'Ignite stacking/expiry/DoT gate failed'}
        }
    }
    $results += [pscustomobject]@{name=$case;frames=$expected;matched=$true}
}
$results|ConvertTo-Json -Depth 6|Set-Content (Join-Path $Evidence 'comparison.json') -Encoding utf8
$results|Format-Table -AutoSize
