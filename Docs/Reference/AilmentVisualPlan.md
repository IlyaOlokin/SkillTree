# Ailment visual integration plan

[Home](../Home.md) · [Project map](../ProjectMap.md) · [Rendering](UnitShapeRendering.md) · [Ailment rules](../Systems/AilmentsAndDebuffs.md)

Status: **proposed implementation, 2026-10-11**. Scoped source and serialized-prefab inspection only. No gameplay code, shader, material, prefab or scene changed; no Unity render, compilation, tests or player build performed. [Storyboards](../../output/ailment-concepts-2026-10-11/storyboards/README.md) illustrate visual intent rather than production frames. All timings below are starting hypotheses.

Implementation follow-up: [Ailment visual rendering](AilmentVisualRendering.md) records the implemented code/content, small departures from this original plan and verification limits. The status above describes the planning pass, not the later implementation.

Later owner feedback supersedes the immediate whole-contour Ignite flash in this plan: appearance now starts locally and expands around the unit over 0.5 s before fading to larger normal fire. Chill/Freeze application is more visible; Bleed uses splash/wound/grouped drops; Overcharge moves farther outside the outline and travels along the perimeter. Current defaults are in the implementation record.

## Accepted visual direction

- Ignite 1: application ignites the **entire actual contour simultaneously**, then recedes into smaller persistent fire pieces. This owner correction supersedes the left-to-right storyboard propagation.
- Chill 3: reduced branching, compact frost patterns rather than lightning. Application must be larger/more noticeable than the latest storyboard; quiet presence remains sparse.
- Freeze 3: a translucent faceted ice pane matching the shape.
- Overcharge 1: short broken **yellow** electric arcs.
- Bleed 1: one independent random source per active stack; drops fall, disappear and are emitted again.
- All five support circle, square and triangle, including authored shape rotation/mirroring and transform changes.

## Inspected current integration points

| Source | Current behavior and implication |
| --- | --- |
| [UnitVisual](../../Assets/Scripts/Visual/UnitVisual/UnitVisual.cs) | Coordinates serializable hit/attack/idle/icon controllers. Updates icons from active effects. New ailments should follow this controller ownership pattern. |
| [UnitShapeView](../../Assets/Scripts/Visual/UnitVisual/UnitShapeView.cs) | Generates a quad, renders through MeshRenderer, sends geometry/colors/stat decorations with a MaterialPropertyBlock. ApplyProperties clears the block. Only a few shape properties are publicly exposed. |
| [UnitShapeSdf.shader](../../Assets/Shaders/UnitShapeSdf.shader) | Computes signed distance and normalized contour coordinate for three shapes; square/triangle anchor helpers already exist. Mirrors and rotates shader coordinates; these are not necessarily transform rotations. Pulse currently affects brightness, not the contour's geometric radius. |
| [PainterlyColor.hlsl](../../Assets/Shaders/PainterlyColor.hlsl) | Shared angular patches and logarithmic brightness/alpha quantization. Reuse these functions in new rendering paths. |
| [Hit controller](../../Assets/Scripts/Visual/UnitVisual/UnitVisualHitEffectController.cs) | OnGettingHit drives unit color flash and transform tweens; weapon-specific SwordHit/StaffHit/HammerHit prefabs spawn at renderer bounds center. Damage flags/tint go to VFX and procedural shockwaves. |
| [AutoDestroyVisualEffect](../../Assets/Scripts/Visual/AutoDestroyVisualEffect.cs) | Short-lived hit effects use realtime fallback cleanup. Unsuitable as lifecycle owner for persistent ailments. |
| [EffectController](../../Assets/Scripts/Battle/Effects/EffectController.cs) | OnEffectAdded reports accepted additions and merged reapplications; OnEffectsChanged reports changes, removals and clear. Repeated bonus effects also reach these callbacks. |
| [ActiveEffect](../../Assets/Scripts/Battle/Effects/ActiveEffect.cs) | Holds effect reference and duration. Bleed has separate instances; Ignite/Chill/Freeze merge. Reference identity can key persistent visual sources. |
| [BattleTickSystem](../../Assets/Scripts/Battle/BattleTickSystem.cs) | Owns pause locks, fixed combat steps and speed multiplier; defaults to unscaled time. No exposed accumulated simulation timestamp currently exists. |
| [Unit.prefab](../../Assets/Prefabs/Unit.prefab), [EnemyUnit.prefab](../../Assets/Prefabs/EnemyUnit.prefab), [PlayerUnit.prefab](../../Assets/Prefabs/PlayerUnit.prefab) | Shared visual prefab, with explicit player/enemy Unit and renderer/transform overrides. VisualParent contains body, weapon and barrier. Base prefab alone contains null unit references, so preserve derived wiring. |
| [EnemyPool](../../Assets/Scripts/Battle/EnemySystem/EnemyPool.cs), [Unit](../../Assets/Scripts/Battle/Unit.cs) | Enemy objects are reused and death deactivates the object. Disable/reset lifecycle must clear new visual state and particles. |
| [Wisps](../../Assets/Scripts/Visual/UnitVisual/UnitShapeView.Wisps.cs) | Existing stat wisps use sprites/trails and separate cosmetic RNG. Ailments are separate from offensive stat wisps; do not replace or reinterpret them. |

These observations do not close BL-032's in-game wiring/render/performance gap. BL-003's Chill duration risk is unrelated to rendering and is not changed by this plan.
Docs/Home.md contains existing conflict markers in a balance/design section; this pass preserves that unrelated text.

## Recommended architecture

Add a serializable **UnitVisualAilmentController**, owned by UnitVisual, with a child **UnitAilmentView** beside the shape renderer. Controller owns subscriptions, accepted-application requests, state reconciliation, animation envelopes and per-Bleed anchors. View owns renderers, particles and reusable buffers. Do not use the status-icon controller as the world VFX owner.

Use **shape-aware shader surfaces plus a small number of ParticleSystems**:
- Two reusable overlay quads at most: interior frost/ice and contour/exterior fire/electric accents. A shared ailment-surface shader can support layer selection and independent channels. Disable unused renderers. This keeps shape coverage analytic and avoids one full-screen quad per stack.
- A small reused particle system for fire/embers/sparks and a separate blood-drop system for independent downward movement. Emit explicitly from sampled contour positions; disable automatic emission/playOnAwake and drive simulation from the chosen visual clock.
- A painterly unlit particle material for these systems reuses PainterlyColor.hlsl with particle tint/alpha. Do not directly assign PainterlyHit.shadergraph to ParticleSystem: its current tint contract is explicitly for VFX Graph inputs.
- Author a small reusable flame mask and frost-feather mask if procedural silhouettes do not match the approved art closely. These are production textures, not cropped storyboard frames. Faceted ice and short arcs can be generated procedurally.

Why this combination: shape-bound coverage is easy to express through existing SDF math, while moving drops need individual lifetimes and world-space trajectories. Current GPU hit VFX remain in place. A separate VFX Graph for every ailment/shape/stack adds authoring and input-binding work without solving shared geometry. VFX Graph remains an alternative if later measured particle counts justify it; no package change is required for the proposed approach.

New rendering uses shared materials and its own property blocks. It never writes to the base renderer's property block, glow/core color or hit-tween transforms. All absent ailment channels evaluate to zero.

## Shared geometry contract

Expose a read-only geometry snapshot/change signal from UnitShapeView: shape type, visual mesh size, effective triangle size, radius read from the authored material, rotation, mirrors, main contour width, renderer sorting/layer and transform.
Signal geometry changes after relevant existing setters/material updates; keep this separate from transient hit color changes.
Extract existing SDF/contour/anchor functions into UnitShapeGeometry.hlsl without changing their formulas; both base and ailment shaders consume that include.

CPU contour sampling uses the same dimensions/formulas for actual boundary positions and outward normals:
- Circle: arc-length normalized angle.
- Square: equal-length side segments and exact corner positions.
- Triangle: the authored vertices/radius scaling, not bounds-based circle sampling.
- Uniform perimeter sampling for Bleed makes each edge equally eligible by length. Optional slight inward offset places a fleck just inside the contour.
- Apply the inverse shader-coordinate rotation/mirror when converting sampled shape points back to mesh-local points. Then use the renderer transform to reach world space.
- Attached masks/anchors follow rotation, mirroring, breathing, hit movement and scale. Detached blood drops fall world-down and do not rotate with later wobble. Flames/embers drift world-up.
- Do not use renderer.bounds as the contour: those bounds describe the quad, not the shape.

Overlay quads need a larger padded extent than the body quad to avoid clipping flames/Chill appearance. Map overlay UVs back into the same unit coordinates. Update bounds accordingly and preserve transparent edge fade.
Avoid nearest-edge discontinuities at corners in application bands; anchor segments deliberately at corners where needed.

## Event and lifecycle contract

Subscribe to recipient EffectController.OnEffectAdded and OnEffectsChanged. Classify **concrete effect types**, not EffectVisualType alone: CyclicAilmentChanceIndicatorEffect can display an ailment icon without being that ailment. Do not hook outgoing OnAilmentApplied, raw damage flags or HP damage; those do not cover direct/repeated/redirected applications consistently.

OnEffectAdded records a pending pulse with the accepted effect reference and timestamp; do not render immediately inside battle callbacks. OnEffectsChanged marks state dirty. In LateUpdate reconcile once against the final active list using reusable collections. Remove stale sources and resolve pending pulses only for surviving effects. This also handles reentrant effect additions/removals without retaining mutable DamageInfo or invoking factories.

- Ignite: one persistent visual channel despite pooled damage additions. Accepted reapplication retriggers a controlled contour pulse.
- Chill: one channel; reapplication refreshes the compact growth pulse.
- Freeze: one channel; while active suppress Chill's large appearance and presence. When Chill was replaced in the same frame, prefer Freeze, discard that transient Chill pulse.
- Natural Freeze expiry restores current returned Chill as a gentle frost transition; distinguish it from a fresh incoming Chill by the previous Freeze state plus pending events. Forced removal with no Chill leaves no frost.
- Overcharge: one presence channel for all instances. Refresh the application pulse when accepted; disappearance follows actual removals, not the first hit callback. Newly added instances in the consuming attack remain visible. No unrequested consumption explosion.
- Bleed: map each surviving effect instance to its own stable anchor/seed/drip phase. Merge removes old sources and creates one new source for the actual merged instance. Do not equate grouped icon count with source identity.

Repeated pulses within one rendered frame combine into one bounded envelope per channel; do not queue seconds of delayed full-screen flashes. Later applications can raise the envelope without extending it indefinitely. This coalescing is cosmetic only.
For effects that appear and disappear within one frame, do not create false persistent presence. A separately retained tiny accepted-application accent can be considered later if needed for very short effects.

On enable, resolve Unit/ShapeView, subscribe once and restore existing presence without replaying application. On disable, unsubscribe, clear pending pulses/anchors/envelopes/particles and hide renderers. On destruction, dispose generated meshes/materials and buffers owned by the view. Re-enabling pooled units starts from the current effect list. No new save/profile contract.

## Animation proposal

| Effect | Application | Presence/removal |
| --- | --- | --- |
| Ignite | Entire perimeter flashes together in roughly 50–90 ms; larger short flame tongues appear around all edges; retreat over 200–350 ms. No travelling ignition front. | A few smaller flames flicker with upward embers. Fade fire when actual Ignite disappears. |
| Chill | Larger brief pale-cyan frost wash over several broad edge sectors, extending slightly outside contour; two compact short frost patterns grow inward. Peak around 120–200 ms, settle by 350–500 ms. Avoid electrical cores, long branches or complete ice casing. | Two sparse frosted patches and rare flakes. Freeze supersedes this appearance. |
| Freeze | Inward fronts meet over roughly 180–280 ms, briefly glint, then fade excess brightness. Pane matches actual silhouette. | Rigid translucent faceted pane; transparent center/contour readability. Natural expiry gently returns to Chill. |
| Overcharge | Brief yellow-white snap along broken outline sectors, peak around 80–160 ms, settle by 250–350 ms. | Short intermittent arcs with dark gaps, no smooth extra ring. Stops when actual instances disappear. |
| Bleed | New stack adds a small local fleck/splash at its own random anchor. | Each source emits staggered small falling drops; suggested drop lifetime 0.35–0.65 s, progressive shrink/fade. Removed stack stops emission; short existing tails expire. No ground pool. |

For Bleed random placement use a dedicated cosmetic RNG, never UnityEngine.Random's combat state. Each logical source remains independent even at high counts. Bound total particles and work, not logical source count; use fair scheduling to avoid permanently hiding newer stacks. Tune the particle budget only after visual observation. Avoid one GameObject/ParticleSystem per stack.

## Composition, clock and tuning

- Keep color families: orange Ignite, muted cyan Chill, blue-white Freeze, yellow Overcharge, dark crimson Bleed with restrained rose highlights.
- Interior ice/frost masks leave the core readable; contour overlay preserves the authored red/gold line; fire/electric exterior accents remain narrow. Layer order is relative to the body's actual sorting settings and must be checked with weapon, barrier, stat wisps, HP bars and hit VFX.
- Ignite and Freeze can coexist; no suppression except Freeze over Chill. Independent envelopes prevent one effect from resetting another.
- Keep small mobile-scale details coarse. Application is brighter/larger than presence but briefer than the total weapon-hit animation.
- Add inspector settings for palettes, size relative to contour, alpha/HDR brightness, envelopes, particle lifetime/rate and painterly controls. Use the existing serializable-settings pattern, not a new database unless reuse demands it.
- New visuals should use a read-only accumulated simulation timestamp exposed by BattleTickSystem, advanced once per processed combat tick. Animate from elapsed simulation time, not shader _Time or realtime cleanup. Pause locks and speed changes then match gameplay, including tick catch-up.
- Manually simulate ParticleSystems with elapsed visual simulation time; do not also allow automatic simulation. Defer visual emissions to avoid catch-up bursts after a stall.
- This proposal does not change existing hit DOTween/wisp/global shader time behavior. Their clocks differ today; synchronizing all presentation is outside this task.
- Do not map damage power to visual scale in v1; power rules differ among ailments and could destroy readability.

## Implementation sequence and proposed file scope

1. **Geometry and lifecycle skeleton.** UnitShapeView snapshot; shared HLSL geometry and CPU sampler; controller/view initialization and cleanup. Match all shapes before drawing effects.
2. **Surface effects.** Freeze pane, expanded Chill appearance and whole-contour Ignite flash; overlay extent/masks and painterly filter.
3. **Moving parts.** Smaller persistent fire, yellow arcs/sparks and independent per-stack Bleed drip scheduling.
4. **Composition and authoring.** Shared prefab settings/materials/textures; preserve PlayerUnit/EnemyUnit overrides; tune simultaneous effects at screenshot scale.
5. **Scoped verification.** One compilation/shader diagnostic pass plus one small play-mode preview covering the cases below; repair observed defects without adding test suites or a full player build.

Proposed new files:
- Assets/Scripts/Visual/UnitVisual/UnitVisualAilmentController.cs
- Assets/Scripts/Visual/UnitVisual/UnitAilmentView.cs
- Assets/Scripts/Visual/UnitVisual/UnitShapeGeometry.cs
- Assets/Shaders/UnitShapeGeometry.hlsl
- Assets/Shaders/UnitAilmentSurface.shader
- Assets/Shaders/AilmentParticle.shader

Expected small existing-code changes:
- UnitVisual.cs: initialize/enable/disable/dispose controller and call its LateUpdate.
- UnitShapeView.cs: read-only snapshot and change notification.
- UnitShapeSdf.shader: consume shared geometry include with identical behavior.
- BattleTickSystem.cs: expose read-only processed simulation time.
- EffectController.cs and ailment gameplay classes: **no required changes**.

Future authored content: ailment materials and small masks under Assets/Visual or the existing visual asset convention; shared Unit prefab child renderers/particle references and preserved derived overrides. These paths are proposals and will be finalized against actual asset conventions at implementation. No serialized content was created during planning.

## Minimum later acceptance checks

- Circle, square, triangle: boundary matching, authored rotation/mirroring, scaling and no quad clipping.
- Ignite whole-contour flash → small flames; Chill larger application → sparse non-electrical presence; Freeze correct pane; Overcharge yellow arcs.
- Bleed: multiple additions keep distinct anchors; drops fall/fade; removal and merge leave the correct number of sources.
- Repeat/direct/redirected application; Chill→Freeze in one attack and natural/forced expiry; Overcharge consumption plus new instance.
- Simultaneous effects with attack flash, weapon, wisps and barrier; paused combat and pooled enemy reuse.
- Scoped C# compilation and new/affected shader diagnostics. Record what was actually observed.

Planning checks are source/path/link checks only. No gameplay verification is claimed, and high-stack mobile performance remains unmeasured.

