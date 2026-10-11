# Ailment visual rendering

[Home](../Home.md) · [Project map](../ProjectMap.md) · [Unit rendering](UnitShapeRendering.md) · [Ailment mechanics](../Systems/AilmentsAndDebuffs.md) · [Original plan](AilmentVisualPlan.md)

Implemented 2026-10-11. This is presentation only: chance, damage, stacking, duration, absorption, saved profiles and effect gameplay were not changed. No localization, icons or tooltip entries were added.

## Ownership and integration

UnitVisual owns a serializable UnitVisualAilmentController, initialized with Unit, UnitShapeView and the existing injected BattleTickSystem. It subscribes on enable, reconciles in LateUpdate and clears on disable/destroy. A transient UnitAilmentView child beneath UnitShapeView owns two quad renderers and four manually simulated ParticleSystems (motes, drops, attached wounds and application splashes). No permanent GameObject is created per Bleed stack.

The shared Assets/Prefabs/Unit.prefab stores the enabled controller, palettes, timing/size/paint settings and references to:
- Assets/Resources/Visual/Ailments/Surface.mat
- Assets/Resources/Visual/Ailments/Particles.mat

Resources lookup is also a fallback for older serialized visuals missing those references, and keeps the shaders referenced in player builds. PlayerUnit and EnemyUnit inherit the shared configuration; existing derived overrides are preserved. The owner's pre-existing EnemyUnit.prefab changes were not edited by this implementation.

Surface/particle renderers use independent property blocks/material instances and do not modify the body colors or its cleared MaterialPropertyBlock. Hit flashes, attack/idle motion, stat wisps, weapon VFX and status icons retain their existing controllers.

## Geometry and shader style

UnitShapeGeometry.hlsl contains the existing shape SDF, contour and square/triangle anchor functions extracted without formula changes from UnitShapeSdf.shader. Both the body and ailment surfaces use it.

UnitShapeView.GetGeometry supplies actual shape, mesh size, material radius/line width, effective triangle size, shader rotation/mirrors, transform and sorting settings. The snapshot is read once per visual frame instead of introducing a geometry-change event; this also follows live material edits without a stale cached radius.

UnitShapeGeometry.cs samples normalized perimeter distance using matching formulas. Its world conversion inverts shader rotation before applying mirrors and the renderer transform. Overlay extents are padded to 2.1 times the body quad, while UV coordinates map back to the original shape coordinates. No circular carrier is used around polygonal units.

UnitAilmentSurface.shader draws analytic contour flashes, short flames, short frost feathers, a faceted ice pane and broken yellow arcs. AilmentParticle.shader draws procedural angular motes and teardrops. Both reuse PainterlyColor.hlsl. No concept-sheet image was imported as a production texture.

## Application and presence

| Effect | Rendering |
| --- | --- |
| Ignite | Sweeps from the physical bottom to top over 0.5 s, then settles to 12 persistent tongues. Application adds six extra tongues and a 0.45 size boost (previously 1.3); quiet fire size remains 0.21. Bases use irregular rounded caps rather than a flat step cutoff. Total duration remains 0.85 s. |
| Chill | Two local frost feathers grow over 0.35 s, swell and brighten, shed a few local flakes, then settle. There is no whole-contour wash. Peak feather scale is 1.45 times quiet size 0.3; total application remains 0.8 s. |
| Freeze | A bright inward freezing front covers the actual silhouette over 0.45 s, holds, and settles into the pane over the total 0.85 s. Existing pane remains under a reapplication front. Default pane opacity remains 0.25. |
| Overcharge | Three individual arcs begin longer and thicker, then shorten and narrow over 0.7 s. The broad corona is removed. At an offset of 0.1 shape units, reveal starts advancing after settling at 0.2 perimeter turns/s. The irregular path uses 48 fixed spatial vertices with a periodic join. Three visibility windows expose new path ahead and fade it behind; bends never translate or regenerate with time. There is no separate head object or ball. Runtime reveal travel is integrated from combat time and retains phase across reapplication; reveal holds its last phase while the removal tail catches up. |
| Bleed | Every stack adds an asymmetric faceted blood blotch matching the supplied wound reference, plus a brighter local splash lasting 0.65 s and six outward flecks lasting 0.5 s. Wound size is 0.14 and splash size 0.2 times transformed unit size. Wound stamps hold size between drip groups and fade only near expiry. Each drip emits three drops at jittered positions around its wound, with independently varied velocity, size and lifetime. Original drop size 0.075 and lifetime 0.55 s are retained; spawn spread defaults to 0.025 times transformed unit size. |

Application has separate growth, brief peak and fade phases. Current defaults above supersede the original immediate whole-contour flash, the intermediate bidirectional Ignite spread and the broad Chill/Overcharge application rings. Growth and total duration are separately exposed in the inspector. Accepted reapplications during an existing animation coalesce without resetting its progress; a later burst retains established presence underneath. OnEnable restores the current list immediately, so the first subsequently accepted application is no longer mistaken for restoration and skipped.

Attached geometry follows the unit. Blood particles detach and travel world-down; embers drift world-up. The dedicated System.Random instances do not consume combat UnityEngine.Random state.

Particle count is bounded per system (default 96); drip scheduling visits sources fairly with a per-frame drop/splash emission budget of 32. Logical Bleed sources are not capped. At extreme stack counts individual sources emit less frequently under that shared budget; no high-stack performance claim has been made.

## Event/lifecycle boundaries

Accepted EffectController.OnEffectAdded callbacks, including direct and repeated effects, record pending requests. OnEffectsChanged marks state dirty. LateUpdate reconciles the final active list and drops requests for effects already gone, using effect reference identity for Bleed. No DamageInfo snapshot or effect factory is retained.

Concrete effect classes select channels; EffectVisualType alone is not used because indicator effects can borrow ailment icons.

Ignite/Chill/Freeze use one presence channel even on merged reapplication. Overcharge presence lasts while any actual instance remains. New Overcharge in the same attack that consumed older instances survives normally.

Freeze suppresses Chill appearance. Same-frame Chill replaced by Freeze does not show two full application effects. When the previous visible state was Freeze and Chill returns, it appears quietly; forced removal without returned Chill leaves no frost. This visual transition does not alter Freeze's saved Chill factory.

Bleed removals stop their source immediately; existing drops, splashes and wound stamps fade and are forcibly retired within the removal deadline. Merge replaces old visual sources with the new actual instance. Disabling the unit clears all sources, pending pulses, envelopes and particles. Re-enabling restores only current presence, without replaying application bursts.

BattleTickSystem.SimulationTime advances once per processed tick. Surface animation and manual particle simulation use this clock, so combat pause locks/speed changes are respected. No automatic particle playback runs in parallel. Existing hit tweens, stat wisps and body shader clocks are unchanged.

## Effect removal

Visuals keep a separate retiring state after the real effect has left EffectController. This never extends gameplay duration or status-icon ownership. Default Removal Duration is **0.3 combat seconds**, with an enforced **0.35-second maximum** even for serialized values outside the inspector range.

- Ignite dims its opacity and HDR brightness; geometry is retained during the fade.
- Chill scales both frost feathers down smoothly toward their anchors.
- Freeze thaws from the deepest interior toward the outside boundary, using the actual circle/square/triangle signed distance. On natural expiry, returned Chill remains quiet and becomes visible after the retiring ice layer finishes.
- Overcharge stops exposing new path: its invisible leading edge holds in place and its trailing visibility window contracts until the tail catches it. The stationary path is erased from back to front rather than uniformly dimmed. New sparks stop immediately; existing particles retire by the same deadline.
- Each removed Bleed instance stops spawning and fades only its own wound/drop/splash particles. Other stacks remain active. Merge retires the old sources and starts the new actual source.

Emission uses the real active state rather than the retiring render state. Particle seeds identify ailment channels or Bleed sources; reusable particle buffers fade existing alpha and cap remaining lifetime at the deadline. Completed retirement records are cleared. Fire temporal dimming is applied after painterly spatial quantization. Overcharge uses the same smooth exit easing to shorten its trailing window, retaining a fixed leading endpoint.

Reapplication cancels retirement for that channel and restores presence under the new application accent. Disable/destroy still clears visuals immediately for pooled/dead units. All timing follows the existing combat simulation clock and pauses with it.

## Authoring and bounded preview

Assets/Editor/AilmentVisualAuthoring.cs provides:
- Tools/Visual/Ailments/Create Materials
- Tools/Visual/Ailments/Configure Shared Unit Prefab
- Tools/Visual/Ailments/Render Preview

The preview creates an isolated temporary scene, renders all five effects across three shapes at 40, 150, 300, 500, 700 and 1050 ms, and removes its objects/resources. The phase evaluation is shared with the runtime controller, and particles advance between snapshots. It uses the authored enemy material and neutral HDR tonemapping for inspection; it is not the actual battle camera or a full playtest.

For this work, a one-shot Temp/AilmentVisualAuthoring.request marker drove requested authoring after import. The editor watcher does nothing when the marker is absent and never executes in Play Mode. Materials/prefab assignments use Unity editor APIs rather than hand-written YAML.

[Output and diagnostics](../../output/ailment-concepts-2026-10-11/implementation/README.md) distinguish actual checks from remaining in-game checks. Unity CLI discovery was unavailable despite a running local Pipeline listener, so authoring/preview ran through the Editor callback after the owner's Refresh.

## Verification limits

Unity imported the C# scripts with no reported C# errors. ShaderUtil diagnostics for UnitShapeSdf, UnitAilmentSurface and AilmentParticle were empty after fixing the new shader's reserved identifier. An isolated three-shape render was inspected; the visibility follow-up also rendered a six-moment growth sequence, including the inward Freeze front and grouped wound droplets. The later v3 pass inspected smaller top-down fire, local frost swelling, individual charge arcs and blood-clot splashes, including a Bleed application close-up. An initial black preview caused by missing camera.scene and a misleading default body material were corrected in the preview tool.

Actual battle playback, simultaneous weapon/barrier/wisp composition, rapid reapplication, Freeze transitions, pause and pooled reuse remain source-reviewed rather than playtested. No tests, player build or performance audit was run. These limits do not close BL-032.

