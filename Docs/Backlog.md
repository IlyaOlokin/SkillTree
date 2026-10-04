# Issue and verification backlog

[Home](Home.md) · [Project map](ProjectMap.md)

Compiled **2026-09-28** from the findings already recorded in this vault. This is
a triage list, not a new code audit, a runtime reproduction report or an approved
redesign. No gameplay fixes or tests were performed while compiling it.

All entries are **Open**. IDs are stable; retain them when updating an entry.
Priorities are provisional: **P1** first investigation (lost rewards/progress,
economy integrity or combat correctness), **P2** normal investigation, **P3**
hardening, tooling or requirements clarification. Reachability in current content
must be checked before treating a source-level boundary as a player-facing bug.

Evidence types:

- **Observed gap**: implementation behavior explicitly recorded in the source review;
  the reported gameplay consequence still needs reproduction.
- **Risk**: a documented boundary with a conditional failure scenario.
- **Decision**: current behavior exists, but a different requirement is not established.
- **Verification gap**: missing tooling or validation, not evidence of a gameplay bug.

## First investigations

| ID | Priority / evidence | Finding and possible impact | Next step and closure criterion | Reference |
| --- | --- | --- | --- | --- |
| BL-001 | P1 / Observed gap | Direct and animated location rewards ignore failed `TryAddItem` and then record the claim. A full inventory can lose the reward permanently. | Reproduce both delivery paths with failed insertion; retain a recoverable unclaimed reward until delivery succeeds, and verify retry/reload cannot duplicate it. | [Reward delivery](Systems/EconomyAndLoot.md#items-and-location-rewards) |
| BL-002 | P1 / Observed gap | Wallet `Tick` adds 1,000 gold on Y without an editor-only guard. The debug path may ship in a player build. | Check the active player-build input path; remove it or restrict it to an explicitly approved development mode. Verify normal builds cannot grant gold this way. | [Wallet](Systems/EconomyAndLoot.md#wallet-and-persistence) |
| BL-003 | P1 / Risk | Chill duration reduction above one yields negative duration, treated as untimed by the controller. More duration reduction could create persistent Chill. | Trace attainable modifier values and reproduce below/at/above 100%; define boundary semantics and verify no accidental untimed effect. | [Chill boundary](Systems/AilmentsAndDebuffs.md#chill-and-freeze) |
| BL-004 | P1 / Risk | Armor helpers clamp negative inputs, but the actual mitigation path does not. Negative armor or a zero/invalid denominator may produce invalid combat damage. | Trace attainable inputs; test negative armor and denominator boundaries; agree on semantics and make runtime calculation and displayed prediction consistent. | [Armor](Systems/DefencesAndResources.md#evasion-and-armor) |
| BL-005 | P1 / Risk | Item effects apply before consumption, without general rollback; node use ignores failed `TryConsumeItem`. Inventory-changing callbacks could grant an effect without consuming the item. | Reproduce use with callbacks that remove/change selection or inventory; ensure one successful effect corresponds to the required consumption, or an explicitly handled failure. | [Item use](Systems/InventoryAndItems.md#using-items) |
| BL-006 | P1 / Risk | Definition catalogs silently keep the first duplicate ID; unresolved definitions are skipped on restore. Ambiguous IDs or unavailable definitions can restore the wrong item or omit it. | Audit authored IDs and player-build catalog coverage; test duplicate/unresolved IDs and define recovery behavior before changing content IDs. Close with deterministic resolution and visible actionable failure handling. | [Save IDs](Systems/SavesAndProfiles.md#ids-and-version-changes), [fallback IDs](Systems/InventoryAndItems.md#persistence-and-definition-identity) |

## Economy, inventory and persistence

| ID | Priority / evidence | Finding and possible impact | Next step and closure criterion | Reference |
| --- | --- | --- | --- | --- |
| BL-007 | P2 / Risk | Gold and animated item credit occur at flight completion. Saving/leaving during flight may interrupt delivery or its persistence. | Test scene exit, save/reload and interrupted animations; reconcile each resolved reward with exactly one delivered or recoverable reward. Coordinate with BL-001. | [Gold flights](Systems/EconomyAndLoot.md#enemy-gold), [item flights](Systems/EconomyAndLoot.md#items-and-location-rewards) |
| BL-008 | P2 / Risk | Shop rollback handles a failed return, not arbitrary listener exceptions; gem exchange commits slots before its socket callback without exception rollback. | Trace actual callbacks and test failure/reentrancy at these boundaries; avoid partial gold/item/socket state and duplicate observer actions. | [Shop transactions](Systems/EconomyAndLoot.md#shop-purchase-and-sale-contracts), [gem exchange](Systems/InventoryAndItems.md#gem-exchange-boundary) |
| BL-009 | P2 / Risk | Reducing inventory slot count removes excess slots. Existing contents need an explicit retention or migration policy. | Establish whether capacity can shrink through current configuration/load paths; test occupied excess slots and implement or document an approved recovery policy. | [Slots](Systems/InventoryAndItems.md#slots-and-stacking) |
| BL-010 | P3 / Decision | Oversized item creation clamps quantity to one stack; insertion does not split across partial stacks. A grant may truncate before insertion or fail despite sufficient aggregate space. | Audit reward/shop callers for oversized quantities and clarify stacking expectations; validate all delivered quantities against intended grants. Do not redesign stacking merely because it differs from another game. | [Item creation](Systems/InventoryAndItems.md#ownership), [stacking](Systems/InventoryAndItems.md#slots-and-stacking) |
| BL-011 | P3 / Decision | No caller of `ItemDropResolver` was found in the inspected scripts; `ItemDropPickup` alone does not transfer items. Ordinary enemy item-table loot is not established. | Confirm whether ordinary enemy item drops are intended; inspect event wiring before deciding to connect the system or mark it unused. | [Item drops](Systems/EconomyAndLoot.md#items-and-location-rewards) |
| BL-012 | P2 / Verification gap | WebGL flush is asynchronous and pending saves can be lost on forced termination. Editor checks cannot establish browser durability. | Test real-browser reload after completed flush, interruption during flush and recovery; record the actual durability boundary. Do not promise persistence of unflushed actions. | [WebGL persistence](Systems/SavesAndProfiles.md#webgl-boundary) |

## Combat mini-games, effects and modal flow

| ID | Priority / evidence | Finding and possible impact | Next step and closure criterion | Reference |
| --- | --- | --- | --- | --- |
| BL-013 | P2 / Observed gap | Rapid and Timing mini-games use local timing fields rather than `context.Duration`. Game-time bonuses may not affect these views. | Trace which authored bonuses target these games; compare baseline/bonus runs and define which timing parameters should scale. | [Mini-game timing](Systems/BattleMiniGames.md#views-and-timing) |
| BL-014 | P2 / Risk | Mini-game overlay writes CanvasGroup flags from activity and does not preserve previous values. Closing it could re-enable input disabled by another owner. | Open/close a mini-game while another modal owns the same group; preserve effective input blocking until all owners release it. | [Mini-game lifecycle](Systems/BattleMiniGames.md#run-lifecycle) |
| BL-015 | P2 / Risk | Low-level `TrySpawn` bypasses availability/cooldown checks and has no activator cap; random spawning continues during an active game. | Trace callers and run prolonged/rapid-trigger scenarios; confirm approved unlock, cooldown and concurrency behavior before adding restrictions. | [Spawning](Systems/BattleMiniGames.md#definition-availability-and-spawning), [lifecycle](Systems/BattleMiniGames.md#run-lifecycle) |
| BL-016 | P3 / Decision | Reviewed damage rewards ignore result score; same-type mini-game buffs replace strength and duration, potentially replacing a stronger buff with a weaker one. | Confirm intended scoring and replacement rules; test different strengths/scores and encode the approved behavior without assuming stacking is required. | [Mini-game rewards](Systems/BattleMiniGames.md#rules-and-rewards) |
| BL-017 | P2 / Risk | `EvasiveMomentum.MaxStacks` is three, but the effect class does not enforce insertion count. The cap may depend entirely on producers. | Trace every applying modifier and test repeated applications; confirm all producers enforce the intended cap or centralize enforcement if needed. | [Temporary effects](Systems/ReactiveCombatEffects.md#temporary-modifier-families) |
| BL-018 | P2 / Risk | Tutorial cleanup restores remembered time scale without shared ownership. Another modal changing time scale can be overwritten on tutorial close. | Test overlapping pause owners in both close orders; closing one owner must preserve the remaining owner's intended pause. | [Tutorial pause](Systems/Tutorials.md#presentation-and-pause-ownership) |
| BL-019 | P2 / Risk | Tutorial chains do not recheck blocking windows; the dimmer does not intercept raw hotkeys, and only configured input behaviours are disabled. | Test new blockers between chained topics and all active gameplay hotkeys; prevent modal overlap/input leakage according to intended UI behavior. | [Tutorial input and chains](Systems/Tutorials.md#presentation-and-pause-ownership) |
| BL-020 | P3 / Decision | Menu deallocation rejects disconnected dependents before optional branch collapse runs. Right-clicking a parent may not collapse its allocated branch. | Reproduce on the actual menu graph and decide intended UX; verify the chosen behavior preserves roots and protected selections. | [Menu deallocation](Systems/MenusAndScreenFlow.md#deallocation-and-single-selection-zones) |

## Localization, audio and HUD

| ID | Priority / evidence | Finding and possible impact | Next step and closure criterion | Reference |
| --- | --- | --- | --- | --- |
| BL-021 | P2 / Risk | Localization wrapper does not explicitly await initialization or catch every lookup error; unresolved keys may appear as raw text. | Test cold startup, absent/empty entries and invalid locale with actual table settings; ensure each affected consumer has an acceptable fallback and no startup failure. | [Lookup](Systems/Localization.md#lookup-and-fallback-contracts) |
| BL-022 | P2 / Observed gap | Visible tutorial text is not refreshed on locale change; other cached labels/tooltips require consumer-specific verification. | If switching language is reachable while these views are open, verify immediate refresh or define an explicit reopen policy. | [Locale refresh](Systems/Localization.md#refresh-is-the-consumers-responsibility) |
| BL-023 | P2 / Risk | Music startup sets target volume before fade-in; requesting the same clip kills the current fade before returning. Transitions may jump or remain at an interrupted volume. | Listen to switches and repeated same-clip requests at several fade positions; verify the intended ramp and final volume. | [Music transitions](Systems/Audio.md#music-transitions) |
| BL-024 | P2 / Risk | Null/empty audio clips can consume cue cooldown without sound; duplicate cue IDs silently overwrite earlier entries. | Validate libraries and exercise malformed cues; make silent failures actionable and confirm cooldown policy for unsuccessful playback. | [Cue resolution](Systems/Audio.md#cue-resolution-and-sound-effects), [lookup ownership](Systems/Audio.md#runtime-ownership) |
| BL-025 | P3 / Risk | SFX pool grows when busy and never shrinks; configured size is not a concurrency limit. Burst load can leave a larger pool resident. | Measure representative worst-case bursts; add a voice budget or reclamation only if measured cost or product requirements justify it. | [SFX pool](Systems/Audio.md#cue-resolution-and-sound-effects) |
| BL-026 | P2 / Risk | Missing mixer/parameters can silently prevent volume changes; startup depends on settings readiness. Graph constraints can also make volume nodes apply a count different from the saved value. | Test actual mixer names, initialization order, restart and constrained node graphs; verify audible volume and persisted settings agree with the selected control state. | [Mixer/settings](Systems/Audio.md#mixer-and-persistence-boundary), [volume nodes](Systems/Audio.md#menu-volume-nodes) |
| BL-027 | P3 / Risk | GSlider fill tween does not explicitly use unscaled time. A resource update during a zero-time-scale modal may leave an animated bar behind its numeric value. | Test updates while paused and decide whether HUD animations should continue; verify fill/text consistency under the chosen policy. | [HUD values](Systems/HudAndTooltips.md#values-and-presentation) |

## Tooling and remaining verification

BL-032 evidence update, 2026-10-04: a [live first-location balance playtest](Reference/BalancePlaytest20261004.md)
completed nine one-point starting-node comparisons and one Fire/barrier route
through the first boss. Background-focus stalls limited early wall-time data.
This adds bounded gameplay evidence; later locations, other boss builds, gems
and the broader integration/target-platform coverage remain unverified. BL-032
remains open.

Additional 2026-10-04 evidence: [120 isolated progression campaigns](Reference/BalanceCampaign20261004.md)
executed 5,010 waves with real XP, numeric-node allocation and simple boss reward
use. Seven locations were reached; stage 40 was attempted. This narrows the numeric
policy simulation gap, but does not verify later-location Play Mode/UI integration,
gems, special builds or target-platform behavior. BL-032 remains open.

Reporting/progression follow-up, 2026-10-04: 30 historical damage-build campaigns (raw artifacts subsequently deleted by the owner)
recorded 3,299 waves across four reached locations. Reports now include numeric
stat snapshots, active/allocated trees, Power and chronology. The five-second
export failure was addressed with direct on-disk per-campaign exports; the final
dataset was recovered from 29 saved records plus one rerun. Full-run speed metrics
and visual HTML interaction checks are unavailable for this run. This does not
close BL-032 or establish optimal-build balance.

| ID | Priority / evidence | Finding and possible impact | Next step and closure criterion | Reference |
| --- | --- | --- | --- | --- |
| BL-028 | P2 / Verification gap | Required `Tools/Locations/check_locations.py` was absent during the documentation survey. The documented content checks cannot currently be relied on. | Locate or restore the intended checker, confirm its rules against the owner guide, then run it for location-content work. Do not invent a substitute and label historical checks passed. | [Location validation](Locations/LocationCreationGuide.md), [tooling](Reference/EditorAndBuildTools.md#content-tools) |
| BL-029 | P3 / Risk | `CodexTwoClusters.cs` is an editor-update helper responding to Temp request files and can write scenes/assets. Its ongoing use is unknown. | Establish ownership and current consumers; retain, isolate or retire it deliberately. Confirm normal editor operation cannot trigger stale requests before considering this closed. | [Editor helper](Reference/EditorAndBuildTools.md#content-tools) |
| BL-030 | P2 / Verification gap | Build workflow has no regression-test step; existing .NET suites use stubs and cannot prove Unity integration. | Agree a proportionate release gate, run relevant suites and Unity checks, and record actual results. Building an artifact alone is not regression validation. | [Checks](Reference/EditorAndBuildTools.md#existing-automated-checks), [workflows](Reference/EditorAndBuildTools.md#repository-workflows) |
| BL-031 | P3 / Decision | Build script logs `-buildNumber` without applying a platform build-number setting. | Confirm whether consumers require a platform build number; implement and inspect output metadata if required, otherwise document the argument as informational. | [Build arguments](Reference/EditorAndBuildTools.md#build-entry-points) |
| BL-032 | P2 / Verification gap | Source review did not verify scene/prefab wiring, complete translations/fonts, audio routing/imports, target performance or current visual appearance. Historical render tests are not current results. | Split into scoped verification tasks when preparing a release or changing these areas; record platform, scene, steps and evidence. This umbrella entry is not a claim that all those areas are broken. | [Coverage limits](ProjectMap.md#coverage-limits-and-future-maintenance), [visual history](Reference/ProceduralMagicRootUI.md#historical-validation-record) |

Warning-maintenance note, 2026-10-03: the `CodexTwoClusters` UDR0001 diagnostics
refer to its intentional Editor-domain lifetime. They are locally suppressed with
an explanation; its polling and file-triggered behavior are unchanged. **BL-029
remains open**, because ownership and stale-request safety were not investigated.
See [compiler warning maintenance](Reference/EditorAndBuildTools.md#compiler-warning-maintenance-2026-10-03)
for the scoped API updates, runtime static resets and other inspected false positives.

## Performance capture findings, 2026-10-01

Evidence: [542-frame Editor Profiler capture analysis](Reference/PerformanceCapture20261001.md),
recording `SkillTree_2026-10-01_21-14-37.data`, Unity 6000.4.6f1. Entries below are
**Open**. Costs were measured in the saved capture; relevant visual/save source was
inspected. No optimization, project build or test suite was run. Priorities are
provisional. BL-032 remains open: this capture adds scoped Editor evidence, not
standalone-player verification. Inclusive marker times overlap and must not be summed.

| ID | Priority / evidence | Finding and possible impact | Next step and closure criterion | Reference |
| --- | --- | --- | --- | --- |
| BL-033 | P1 / Capture-confirmed cost; root cause unisolated | SkillTreeCamera CPU rendering preparation dominates heavy segments. QueuePrepareIntegrateMainThreadObjects totals 11.455 s, 61.9% of recorded main-thread duration; average 21.134 ms/frame, maximum per-frame sum 63.545 ms. Segment averages range from 2.189 to 56.889 ms. This can cause sustained low frame rate. SpriteMask sample volume is a lead, not a confirmed cause; GPU timing is unavailable. | Reproduce the same expensive view in a development player capture, isolate renderer population and sprite-mask participation, then optimize the confirmed cause. Close with a comparable before/after capture demonstrating reduced preparation time and preserved visuals. | [Rendering evidence](Reference/PerformanceCapture20261001.md#1-skill-tree-cpu-rendering-preparation-dominates-heavy-segments), [Skill tree](Systems/SkillTree.md) |
| BL-034 | P1 / Capture-confirmed hitches; synchronous source path | GameSaveCoordinator.Tick has nine 39.794–65.157 ms spikes and directly-parented GC.Alloc samples totaling 19.87 MiB. Inspected code synchronously captures the complete profile, writes storage and updates metadata on the caller thread. Periodic saving can visibly interrupt gameplay. Exact capture/encoding/disk contributions are not separated. | Measure these stages separately and reduce main-thread cost; consider ordered background encoding/writes after main-thread state capture. Preserve whole-profile inventory/tree consistency, backup recovery, retries and profile-switch/quit ordering. Close with reduced save spikes in a comparable capture and appropriate persistence verification. | [Autosave evidence](Reference/PerformanceCapture20261001.md#2-synchronous-autosave-causes-distinct-gameplay-hitches), [Save scheduling](Systems/SavesAndProfiles.md#autosave-and-profile-switching) |
| BL-035 | P2 / Capture + inspected source | NodeConnectionRenderer.Update costs 1.275 ms/frame on average, maximum 2.386 ms. It scans every connection each frame; settled entries still call finalization and texture-state/progress setters. Stable connections incur repeated work. Texture uploads are already conditional on animation changes. | Track active transitions and finalize each completed transition once, preserving allocation/refund direction, state textures and animation appearance. Close with comparable capture showing lower idle cost and verified transition behavior. | [Visual evidence](Reference/PerformanceCapture20261001.md#3-stable-per-frame-skill-tree-visual-overhead), [Skill tree](Systems/SkillTree.md) |
| BL-036 | P3 / Capture + inspected source | NodePowerVisual.Update runs 2,764 times/frame, with aggregate average cost 0.364 ms/frame. Settled instances already return early but still receive callbacks. Fog Update averages another 0.768 ms/frame; texture upload/visibility refresh are conditional. Idle visual work consumes CPU budget, below the dominant rendering/save costs. | Investigate scheduling updates only for active visual transitions and avoiding idle reveal-animation scans; preserve power/color transitions, fog discovery and presentation visibility. Close with lower measured idle overhead in the same scenario. | [Visual evidence](Reference/PerformanceCapture20261001.md#3-stable-per-frame-skill-tree-visual-overhead) |
| BL-037 | P2 / Capture-confirmed allocation; allocator unisolated | GC.Alloc samples directly parented by BattleTickSystem.Update total 9.28 MiB over 18.499 s. The callback's average CPU time is only 0.183 ms/frame, so this is allocation pressure rather than the main CPU bottleneck. The precise combat allocator is not identified; this recording does not show consequential GC pauses. | Capture allocation call stacks in the same combat scenario, identify actual allocating functions and reduce avoidable allocations while preserving combat semantics. Close with comparable allocated-bytes measurements; do not claim a GC hitch fix without measured collection pauses. | [Allocation evidence](Reference/PerformanceCapture20261001.md#4-allocation-pressure-exists-gc-pauses-do-not-explain-these-spikes), [Combat](Systems/CombatAndEffects.md) |

The two longest frames (306.606 and 365.234 ms) are primarily EditorLoop overhead.

**BL-033 update, 2026-10-02 — optimization implemented; performance verification pending.**
The owner reported approximately four times higher FPS after disabling node masks;
this is a user-reported comparison, not a newly analyzed capture. The five gameplay
node prefabs now use hard circular clipping in the icon shader, with shared,
adjustable-radius materials and disabled SpriteMasks. Unity shader diagnostics
reported no errors; a SkillTreeCamera image was inspected and all 2,812 NodeVisual
instances in the open MainScene used the replacement materials with no enabled
child masks. The new shader disables dynamic batching to preserve local clipping
coordinates. A comparable performance capture is still required before closing
BL-033 or claiming the reported FPS gain is retained by the replacement.
See [node clipping controls](Systems/SkillTree.md#node-icon-circular-clipping).

Memory growth during this short Editor recording is not proof of a leak. Neither
is registered here as a confirmed gameplay defect; the report retains the evidence
and the missing player/GPU/memory-retention coverage.

## Maintenance and exclusions
**BL-038 implementation update, 2026-10-02 — verification pending.**
NodeVisual now coalesces repeated events in LateUpdate, disables idle callbacks
and avoids unchanged labels/colors and availability checks on active nodes.
Queue positions use a dictionary and connection events use an incident-connection
index. Queue pruning, gem influence and fog retain existing behavior. No tests,
Unity checks or profiling were run, at the owner's request. BL-038 remains open.

### BL-038 — allocation queue frame-rate drop

**2026-10-03 — first optimization stage implemented; runtime verification pending.**
Queue views now receive node-specific notifications and use stable display numbers,
eliminating full-queue renumbering during execution. Planning/pruning use a shared
incremental reachability simulation; dependent refunds use one reverse graph pass.
Tree operations consolidate bonus notifications, and queue-only changes no longer
request stat/weapon updates. Operation cadence, actual queue order, refund exclusions,
combat Mods scheduling and the save format are retained. Targeted profiler markers
were added. Gameplay C# compiled with zero errors and 27 warnings against installed
Unity references; the active Play Mode was not restarted. No gameplay tests, player
build or new performance capture were run. BL-038 stays open until a comparable
recording and owner behavior checks confirm the result. See the
[implementation contract](Systems/SkillTree.md#allocation-optimization-implementation-2026-10-03).

**2026-10-03 — allocationTest2 analyzed; issue remains open.** The complete second
recording shows a 31.963 ms long-path input callback and 86 allocation-operation
frames averaging 32.181 ms total (12.618 ms PlayerLoop, 19.221 ms EditorLoop).
MainSkillTree.LateUpdate averages 4.297 ms and reaches 7.725 ms; queue-order changes
still broadcast to every NodeVisual for each changed queue entry. TMP.GenerateText
averages 0.953 ms during subsequent queue frames. Refund processing retains about
2.340 ms/frame in tree Update + LateUpdate. Source also shows repeated graph queries,
queue-only bonus invalidation and duplicate synchronous stat rebuilds when paused.
Their individual costs are not instrumented. First allocation includes a measured
4.976 ms audio load, 0.881 ms hierarchy expansion and Editor JIT overhead. Autosave
still has 11.7–17.1 ms completion-like spikes (BL-034). See the
[capture analysis and redesign proposals](Reference/PerformanceCaptureAllocationTest.md).
No gameplay implementation, tests, builds or new recordings were performed.

Third implementation update, 2026-10-02: queue processing skips repeated full graph
pruning while validated requests progress through active allocations. Live state
and custom predicates are still scanned; failures trigger full pruning. Queue
edits, deactivation/refund and topology changes invalidate validation. Head
allocation checks, cost, order and cadence remain unchanged. Built-in gem
influence reuses calculated power for ordinary-node changes; sockets, topology,
restore and unknown active influence rules retain full recalculation. No tests,
Editor checks or profiling were run; owner verification is pending. BL-038 remains open.

Owner comparison result, 2026-10-02: disabling connection transitions left the
reported queue-processing frame rate unchanged at approximately 28 FPS. This
comparison does not support transitions as the cause of the remaining drop.
Source inspection still shows full queue pruning before each queued allocation
and full gem influence recalculation on every MainSkillTree node-change event.
Their individual costs have not been measured in the updated implementation.

Second implementation update, 2026-10-02: removed unconditional full-tree visual
scheduling for queue and availability changes. MainSkillTree coalesces queue-order
differences and computes shared root reachability once per dirty frame. NodeVisual
schedules changed availability or queue labels; limited-zone notifications refresh
their own nodes. Verification and a comparable capture are pending; the owner
requested no checks. Queue pruning and gem influence costs are outside this
change. BL-038 remains open.

**Capture update, 2026-10-02:**
[NoAllocationEffect analysis](Reference/PerformanceCaptureNoAllocationEffect.md)
confirms a 69-frame expensive segment averaging 51.927 ms/frame, including
MainSkillTree.Update at 10.429 ms and all NodeVisual.LateUpdate calls at 10.400 ms.
There are exactly 2,812 NodeVisual callbacks on every selected frame despite earlier
event coalescing. Prioritize removing full-tree queue-label/availability callbacks
and repeated allocation work. EditorLoop contributes another 21.475 ms/frame.
The captured nested code does not isolate queue pruning from influence recalculation.
The flash was disabled; the reported slowdown persisted. No additional fix was
implemented during this capture analysis.

**Capture update, 2026-10-02:**
[NoAllocationEffect analysis](Reference/PerformanceCaptureNoAllocationEffect.md)
confirms a 69-frame expensive segment averaging 51.927 ms/frame, including
MainSkillTree.Update at 10.429 ms and all NodeVisual.LateUpdate calls at 10.400 ms.
There are exactly 2,812 NodeVisual callbacks on every selected frame despite earlier
event coalescing. Prioritize removing full-tree queue-label/availability callbacks
and repeated allocation work. EditorLoop contributes another 21.475 ms/frame.
The captured nested code does not isolate queue pruning from influence recalculation.
The flash was disabled; the reported slowdown persisted. No additional fix was
implemented during this capture analysis.

**P2 / Open / user-reported symptom with source-reviewed candidates, 2026-10-02.**
The owner reports lower FPS while many queued nodes are allocated. The service
already limits processing to one node operation per Tick. Each allocation causes
global NodeVisual refreshes through skill-point and node-allocation events, plus
queue-label refreshes; these can repeatedly traverse connectivity and linearly
search the queue for every visual. Further candidates are full queue pruning,
gem-power recalculation, fog updates and event-time connection scans. No capture
was taken, so their relative contribution is unknown. Prioritize coalesced visual
updates and queue/connection indexing, then conditional graph/influence invalidation
and fog work. Preserve custom conditions, limited zones, sockets, refunds and queue
ordering. Close after comparable queue processing shows improved frame times and
the owner verifies allocation behavior. No implementation or runtime verification
was performed in this investigation. See
[allocation queue investigation](Systems/SkillTree.md#allocation-queue-performance-investigation-2026-10-02).

**BL-034 update, 2026-10-02 — background autosave implemented; verification pending.**
Editor/non-WebGL autosave now captures and serializes the profile on the main thread,
then encodes, validates backups and writes on a single background worker. New dirty
events remain pending independently; failures restore dirty state and use the existing
retry delay. Explicit saves and profile/lifecycle transitions join the worker; storage
reads/writes/deletes also wait to avoid reset and backup races. Manifest updates remain
synchronous, and WebGL retains its previous path. The save format is unchanged.
No tests, Unity checks or measurements were run, at the owner's request; BL-034 remains
open until persistence behavior and reduced hitches are verified. See
[background autosave](Systems/SavesAndProfiles.md#background-autosave-2026-10-02).

**BL-035 / BL-036 update, 2026-10-02 — source changes implemented; verification pending.**
Connection updates now process only active transitions and finalize each completed
transition once. NodePowerVisual disables idle callbacks and wakes on new targets,
using an absolute 0.0001 completion tolerance. Node-change connection lookup remains
an event-time scan; fog was not changed. No Unity checks, tests or performance captures
were run for this change, at the owner's request. These entries remain open pending
owner verification and comparable timings. See
[visual transition scheduling](Systems/SkillTree.md#visual-transition-scheduling).

Start with BL-001 through BL-006; refine priority after checking reachability and
reproducing the scenario. For each investigation, record source revision, reproduction,
expected behavior and result. Update status to Investigating, Confirmed, Fixed,
Accepted behavior or Not reproducible, with evidence. A fix is closed only after
appropriate verification and an update to the affected system page.

Do not turn every documented implementation detail into a bug. Snapshot stat
semantics, next-hit versus attack-completion consumption, paused versus unscaled
clocks and specific stack replacement rules need their actual design context.
Level7's missing visual GUID references are explicitly intentional in the owner
guide. Freeze runtime support does not authorize new Freeze content, and historical
balance notes do not authorize rebalancing. Documentation corrections already made
(such as moved source paths) are not open gameplay defects.


Simulator maintenance, 2026-10-04: the external adapters/report pipeline were
reorganized and exercised with three fixed fights plus three bounded two-location
campaigns. Cached dispatch, direct atomic exports and full-publication work cleanup
were exercised. The retained 30-bot dataset was used for a same-data compression comparison
with JSON equality validation, preserving snapshots and graph data. Its original
uncompressed storage was restored at the owner's request. This adds narrow tooling evidence;
heavy-stage Pipeline timeouts, partial-bot resumption and visual viewer interaction
remain outside verified coverage. BL-032 remains open.

Balanced-campaign evidence, 2026-10-04: [50 five-policy runs](../Reports/BalanceSimulation/20261004-124706-435-campaign/verification.md)
recorded 8,071 waves, 6,268 snapshots and 12 reached locations. Archive/viewer payload
and final snapshot consistency were checked; temporary actors/modifiers were absent
after execution. Cold seeds 106/110 had no remaining supported numeric frontier
within the selected starter route and retained 3/7 points. This narrows numeric
policy coverage but does not close BL-032 or validate full cold/special/gem gameplay.

Adaptive policy evidence, 2026-10-04: [25 five-policy campaigns](../Reports/BalanceSimulation/20261004-135038-237-campaign/verification.md)
ran 3,535 normal and 2,683 probe waves. Frozen trial budgets/XP/seeds and progression
log isolation were checked; 156 changed layouts were committed, with 41 normal-stage
wins following changed-layout commits. On seeds 101–105, mean strength progression
improved 17→25 but dexterity regressed 51→42; elemental means stayed 14. The current
stage-only objective and persistent growth priorities can harm later progression;
this is a measured tool/policy limitation, not an established gameplay defect.
BL-032 remains open. No gameplay source, assets or profile-save contract changed.
