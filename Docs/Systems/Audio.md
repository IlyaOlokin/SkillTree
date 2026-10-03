# Audio cues and volume settings

[Home](../Home.md) · [Project map](../ProjectMap.md) · [Menus](MenusAndScreenFlow.md)

Status: **source-reviewed, 2026-09-28**. Covers the project's cue player, source
pooling, music transitions and volume controls. Actual scene routing, cue-library
assignments, clip imports and audible results were not inspected in the Editor.

## Runtime ownership

GameAudio exposes a singleton Instance. Awake destroys a duplicate's GameObject;
the accepted instance optionally survives scene loads (default enabled), builds
its cue lookup, creates/configures sources and applies available saved volumes.
No claim is made here that every scene instantiates or correctly wires it.

AudioCueLibrary contains definitions with an ID, Sfx/Music bus, clip array, volume,
pitch range, minimum interval and spatial blend. IDs use ordinal, case-sensitive
lookup. Empty IDs are skipped; later duplicate IDs overwrite earlier definitions.
The lookup is built in Awake, so editing a library is not itself a runtime rebuild.

## Cue resolution and sound effects

Allocation-specific evidence, 2026-10-03: the
[allocationTest2 recording](../Reference/PerformanceCaptureAllocationTest.md#first-use-hitch-is-distinct-from-steady-allocation-cost)
contains a 4.976 ms SoundManager.LoadFMODSound sample under the first allocation.
The inspected cue library maps ui.nodeAllocation to NodeAllocation.wav; its import
metadata disables both preloadAudioData and loadInBackground. Preloading before
interaction is proposed, not implemented. This is a first-use CPU observation,
not a measured sustained audio bottleneck or an audible verification.

PlaySfx and PlaySfxAt only accept Sfx cues; PlayMusic only accepts Music cues.
Missing IDs, bus mismatch and calls inside the cue's minimum interval return
without playback. The interval uses unscaled time and is shared per cue ID across
callers. A successful lookup records the time before clip selection; an empty or
null-selected clip can therefore consume the cooldown without playing sound.
Random clip selection does not filter null entries from the array.

PlaySfx forces 2D playback. PlaySfxAt sets the source world position and uses the
cue's spatial blend, so a positioned cue can still be 2D when that value is zero.
SFX are non-looping and use pitch `1 + random(-range, range)` and cue volume.

A dedicated 2D source is preferred while idle. Other simultaneous sounds use a
pool, initially 12 sources by code default. The pool searches for an idle source
and creates another when all are busy. **The configured pool size is not a hard
concurrency limit**, and this implementation does not shrink the pool afterward.
Minimum cue intervals are the explicit throttling mechanism in this layer.

AudioCuePlayer exposes Play for event wiring and can use its transform position.
SfxPointerAudio plays configured cues on pointer enter/click. NodeAllocationEffectFactory
also calls GameAudio for allocation feedback. These callers tolerate a missing
singleton by skipping playback. Inspect serialized UnityEvents for additional
callers; a C# call search alone does not establish complete audio usage.

## Music transitions

Music uses one looping source at pitch one. A nonplaying source or a nonpositive
fade duration starts the selected clip immediately. Otherwise the old source
volume fades to zero, the clip switches, and another fade to cue volume is requested.
This is a sequential single-source transition, not an overlapping crossfade.
Fades use unscaled DOTween time and continue through a zero time scale.

**Implementation nuance:** StartMusicClip already sets source volume to the new
cue volume before the requested fade-in starts, so that second tween may have no
audible ramp. Also PlayMusic kills an existing fade before checking whether the
same clip is already playing; that early return can leave an interrupted volume.
These are source-observed boundaries to test, not verified listening results or
intentional sound-design rules. No audio code was changed in this pass.

StopMusic kills the current fade, then stops/clears the clip immediately or after
an unscaled fade-out. GameAudio does not subscribe to battle pause or tutorial
pause; combat pause should not be assumed to stop playback.

## Mixer and persistence boundary

Music and SFX sources are assigned their respective serialized mixer groups.
The mixer is expected to expose exactly MasterVolume, SfxVolume and MusicVolume.
Setters clamp linear values to 0..1 and convert them to `20 * log10(volume)` dB;
values at or below 0.0001 map to -80 dB. A missing mixer makes that operation a
no-op, and SetFloat's success result is not checked here.

The setters update an injected LocalSettingsService's in-memory fields but do not
save automatically. SaveVolumes explicitly saves that service. Initial values use
its Current object when available, otherwise all three volumes default to one;
GameAudio does not load a fallback settings service on its own. Initialization
order and injected settings readiness require a scene-level check.

Local settings are separate from character profiles and from the saved language
code. See [saves](SavesAndProfiles.md) and [localization](Localization.md).

## Menu volume nodes

MenuVolumeZone maps allocated nodes to volume in increments of 0.1, clamped to one.
Saved volume maps to `round(clamp01(volume) / 0.1)`, bounded to 0..10 nodes.
Synchronization deallocates from the end of its list and allocates from the start
through normal node/controller APIs, so graph and zone constraints can reject
changes. Startup then applies the **actual achieved node count**, not necessarily
the original saved value.

Node changes apply volume to GameAudio when present and update the settings
service; saving is controlled by saveOnAllocatedCountChanged (default true).
When injection is absent, the zone creates and loads a fallback LocalSettingsService.
Startup applies values without requesting a save. Avoid assuming that multiple
fallback service instances share one live Current object.

## Verification and sources

Recommended checks: missing/wrong-bus/duplicate cue; null clip; interval throttling;
many simultaneous SFX; positioned cue with zero spatial blend; scene persistence;
music switch/repeated request during a fade; mute/full-volume mixer parameters;
restart with saved volumes; node topology that prevents a desired count; tutorial
pause during music. No Unity playback or performance checks were run in this pass.

- `Assets/Scripts/AudioSystem/GameAudio.cs`
- `Assets/Scripts/AudioSystem/AudioCueLibrary.cs`
- `Assets/Scripts/AudioSystem/AudioCuePlayer.cs`
- `Assets/Scripts/AudioSystem/SfxPointerAudio.cs`
- `Assets/Scripts/AudioSystem/AudioBus.cs`
- `Assets/Scripts/MenuTree/MenuVolumeZone.cs`
- `Assets/Scripts/SaveSystem/SaveSettingsServices.cs`
- `Assets/Scripts/SaveSystem/SaveDataModels.cs`
- `Assets/Scripts/Visual/SkillTree/NodeAllocationEffectFactory.cs`
