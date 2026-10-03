# Unit shape rendering

[Home](../Home.md) · [Project map](../ProjectMap.md)

Source-reviewed **2026-09-29**. Source:
`Assets/Shaders/UnitShapeSdf.shader` and
`Assets/Scripts/Visual/UnitVisual/UnitShapeView.cs`.

## Painterly color

`SkillTree/Unit Shape SDF` uses stepped glow/core energy and broad, angular
procedural patches. The patch pattern uses sheared, warped triangular cells in
shape-local coordinates; it follows the shape's rotation and mirroring without
scrolling with time. Existing pulse and contour animation still run.

Material controls in **Painterly Color**:

| Control | Default | Effect |
| --- | --- | --- |
| Paint Strength | 1 | Blend with the previous smooth shading; 0 restores it |
| Paint Tonal Steps | 5 | Rounded to an integer; fewer steps give stronger bands |
| Paint Patch Size | 0.14 | Larger values produce larger patches in local shape coordinates |
| Paint Patch Variation | 0.65 | Brightness differences between patches; 0 leaves only tonal bands |

Steps are logarithmic energy intervals, not a fixed total palette size. Glow and
core energies are rounded separately before mixing their authored colors; HDR
values above one remain available. The translucent halo is also stepped so its
alpha does not reintroduce a smooth gradient. Fill and geometric coverage keep
their original calculations, including SDF edge antialiasing and quad-edge fade.
Post-processing bloom can still soften the result.

These controls belong to the material. `UnitShapeView` continues to supply its
existing per-unit colors, shape and stat bindings through a property block; it
does not override the painterly controls. Existing materials inherit the shader
defaults until explicit values are saved. Set Paint Strength to 0 on a material
to disable the effect for its units.

## Barrier styling

Added on **2026-09-30** in `Assets/Shaders/BarrierRingsSdf.shader`.
The barrier uses the same shared `PainterlyColor.hlsl` functions and four material
controls/defaults listed above. Its Patch Size control permits values down to
**0.001** (other shaders
retain their existing ranges). After accumulating its active and inactive ring
layers, it quantizes color brightness and alpha using a single shape-local patch
sample. RGB channels scale together to preserve the authored hue; quad-edge fade
is applied afterward. Charge counts, segment layout and break animation retain
their original calculations. Paint Strength 0 bypasses the filter entirely.

`BarrierRingView` does not override these four material properties. Its generated
fallback material also inherits the shader defaults. Change the controls on the
renderer material, or assign a saved material to the view for persistent tuning.

Verification: Unity 6000.4.6f1 imported and rendered the shader with no ShaderUtil
messages. An isolated side-by-side render with Paint Strength 0 and 1 confirmed
the stepped color/alpha and angular patches. Temporary preview objects were
removed. No gameplay test or build was run.

## Hit-effect styling

Implemented on 2026-09-29. `Assets/Shaders/PainterlyColor.hlsl` holds the shared
patch and logarithmic energy functions used by the unit shader and both hit paths
under `Assets/Shaders/AttacksVFX`:

- `HammerShockwaveMasked.shader` samples two masks and animated noise, mixes
  damage-dependent layer colors, and outputs alpha-blended color. Each layer's
  color and spatial alpha are stylized using its deformed mask UV, before the
  existing lifetime fade. Its material has the same four Paint controls as units.
- `SwordHit.vfx`, `HammerHit.vfx`, and `StaffHit.vfx` contain respectively 7, 6,
  and 9 output contexts, migrated to Shader Graph outputs through Unity's VFX API.
  All use `PainterlyHit.shadergraph`, a URP unlit graph with Support VFX Graph.
  The graph processes texture RGB and alpha before multiplying explicit VFX
  `color` and `alpha` attribute inputs (`_ParticleTint`, `_ParticleOpacity`).
  Get Attribute operators connect these inputs on every output; do not replace
  them with mesh Vertex Color, which is white for these planar primitives.
  This retains damage tint and lifetime fading. Patch coordinates
  follow particle UVs. Each output exposes the same four Paint inputs; set
  `_PaintStrength` to 0 to disable the processing for that output.

Original texture bindings and Alpha/Additive blend choices are retained.
Additive particles can still combine into brighter colors; this is per-particle
styling, not quantization of the final composited frame. The graph uses the shared
`ParticleTint_float` HLSL function to apply the VFX attributes after stylization.
It does not affect cameras/UI.

`UnitVisualHitEffectController` supplies damage-presence flags and
`DominantBaseDamageType` to the spawned Visual Effects and passes the dominant
type to `ProceduralShockwavePlayer`. Those inputs and the controller are unchanged.

## Wisp paint, trails and motion

`UnitShapeView.Wisps.cs` exposes separate Steel, Ash, Frost and Storm settings on
`UnitShapeView`, saved in `Assets/Prefabs/Unit.prefab` on `VisualParent/Visual`.
Each group contains motion ranges, trail lifetime/width/intensity
and four Paint controls shared by that type's sprite and trail. Property blocks
override `WispGlow.mat`, so edit these component settings rather than a runtime
material. Patch Size supports values down to 0.001.

`WispGlow.shader` uses the shared painterly functions on HDR color and spatial
alpha. Trail mode generates a thematic cross-section without a texture: a sharp
Steel blade, animated Ash flame edges, a Frost ribbon and a thin Storm core.
Lifetime alpha fades after quantization. Existing per-type colors remain intact.

Defaults: Steel travels uniformly at 210 degrees/second with a 0.22-second tapered
trail; Ash orbits at 55 with size/brightness pulsation and a 0.45-second flame
trail; Frost orbits at 25 with slow radius modulation and a 0.8-second trail;
Storm travels along straight legs between alternating inner/outer radius targets,
with jittered corners roughly every 0.085 seconds and a 0.32-second trail.
Speed/radius variation uses cosmetic randomness independent of gameplay RNG.
The one-time style migration preserves existing radius, size and colors while
assigning thematic motion defaults. Subsequent Inspector changes are retained.
Trails are transient children, follow unit visibility and are destroyed with wisps.

Storm trajectory is authored in the separate **Storm Trajectory** group on the
same component. Turn Duration and Angle Offset specify random min/max ranges
(X/Y, seconds/degrees). Radius Pattern selects alternating inner/outer corners,
random inward/outward corners, or a fixed radius offset. Radius Excursion defines
the amplitude and per-wisp spread; Excursion Multiplier defines its per-corner
min/max factor. Fixed Radius Offset applies only to Fixed mode. Travel Progress
is a normalized curve controlling movement along each leg (linear by default).
Orbit Radius and Orbit Speed remain in Storm Wisp. Storm does not use the shared
Flutter fields; the old amplitude and corner settings are migrated once into
Storm Trajectory to preserve authored values. Corner duration has a 0.02-second
minimum to bound cosmetic updates. No additional visual verification was run
for this Inspector change, per the owner's request.

## Verification record

Wisp update (2026-10-01): Unity loaded the revised C# and reported no shader
compiler messages. An isolated preview confirmed sprite colors and generated
trail geometry, but did not display the trails; in-game appearance and motion
remain unverified. Further visual checks are left to the owner as requested.

The initial unit shader revision failed D3D11 compilation because `triangle` was
used as a variable name. It was renamed to `triangleIndex`.

During the hit-effect implementation, Unity CLI was found outside the sandbox's
PATH and connected to Unity 6000.4.6f1. All three VFX assets were imported; Unity
ShaderUtil reported no messages for the unit shader, wave shader or particle
Shader Graph. Source comparison confirmed output counts, per-output block counts
and the texture GUID multisets were retained. No new shader errors were captured
during import/preview; tooling did report a command timeout and a temporary
serialized-object assertion during migration, so the console was not globally
error-free.

An isolated camera preview rendered the wave, but GPU particles remained culled
in that preview. Full in-game appearance, timing and color combinations remain
unverified. Temporary preview objects were cleaned up. No tests or build ran.

Color follow-up: the initial particle graph used mesh Vertex Color, and generated
VFX code supplied constant white instead of particle tint/alpha. Replaced that
input with explicit current-attribute connections on all 22 outputs. Unity
regenerated 7/6/9 output shaders. Inspected generated code confirms the final
color and alpha attributes, after output blocks, feed the fragment properties
and multiply the stylized texture. Shader Graph compilation reported no messages;
the in-game visual result still requires confirmation.
