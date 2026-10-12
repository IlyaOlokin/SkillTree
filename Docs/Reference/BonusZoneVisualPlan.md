# BonusZone animated visual plan

[Home](../Home.md) · [Project map](../ProjectMap.md) · [Skill tree](../Systems/SkillTree.md)

Status: implemented on the existing MainScene BonusZone, 2026-10-12. Unity compilation and a Play Mode Game View capture were performed. Target-device performance remains unmeasured. The design notes below retain the original proposal; the implementation record defines what was actually delivered.

## Implementation record

- `BonusZoneAuraVisual` is attached to the existing BonusZone in `Assets/Scenes/MainScene.unity`. The original spline, node membership and localized description remain intact.
- `Assets/Shaders/BonusZoneFill.shader` renders transparent cyan energy with two moving procedural fractal-noise fields in local tree coordinates. Procedural noise replaces the proposed imported noise texture, avoiding a new texture dependency.
- SpriteShape material slot **0** is the fill; slot **1** remains the existing transparent edge material. Renderer tint is white so the component's opacity is not attenuated by the old tint. The new material assets are `Assets/Materials/BonusZone/BonusZoneFill.mat` and `BonusZoneBorder.mat`.
- `BonusZoneBorder.shader` draws a continuous bright core, soft halo and sparse drifting/shimmering dots on one generated closed ribbon. Integer spark count makes the perimeter seam periodic. Fill order remains -9; the generated contour inherits its sorting layer and uses order -8.
- The component exposes tint, fill opacity, pattern scale, flow velocity, contour/halo width and brightness, spark density/speed/intensity and phase. Initial fill opacity is 0.065. These are appearance settings, not gameplay balance.
- Refinement, 2026-10-12: the fill now contains sparse cyan dot particles computed in the shader, clipped by the SpriteShape fill mesh. Dots wander gently, shimmer independently and dim over the brightest wisps so they occupy the negative space. Inspector controls expose interior density (1.8 cells per tree unit), radius (0.018 tree units), intensity (0.5) and motion speed (0.12); no particle GameObjects or additional mesh rebuilds are needed. The transparency contrast exponent (2.6) remaps weak fill toward zero while preserving its strongest flows and the separate border. `_MainTex` is declared for SpriteShapeRenderer compatibility; procedural shading does not sample it.
- Spline hash changes (including editor edits/Undo) and `Refresh()` invalidate geometry. Static idle animation uses shader time without rebuilding meshes. The component hashes the small source spline in Update; it does not scan the skill tree. Generated mesh and child renderer use HideAndDontSave and are cleaned up on disable; disabling restores the prior fill property block.
- The supported outline is a simple closed **linear** spline, with either winding and concave shapes. Open, degenerate, intersecting or curved outlines log a warning and omit the contour. Joins use a capped miter (up to three halo widths), rather than the proposal's bevel fallback. Very acute corners/narrow gaps remain a visual limitation; no arbitrary-shape acceptance sweep was run.

### Verification performed

Gradient refinement, 2026-10-12: the narrow vein cores now have a broader Gaussian
opacity shoulder (`exp(-veinDistance² * 2.8) * 0.65 * (1 - wisps)`). This restores
intermediate opacity near each flow and slows its falloff into transparent space,
without increasing peak core brightness or changing the dots/border. The material
uses the updated shader directly; no scene reassignment is required.
The shader was imported in Unity and inspected in the composited Play Mode Game
View. The delivery screenshot was updated; the console contained no new errors.

Refinement verification: the updated component compiled with `failed:false`; dot settings were saved on the MainScene object and the composited Play Mode screenshot was replaced. Visual inspection caught derivative artifacts along procedural cell boundaries; antialiasing now uses continuous local coordinates instead of discontinuous randomized distances. The final screenshot shows interior dots and nearly transparent negative space. Console inspection reported no new effect errors (the earlier tooling timeout remains in history).

`unity command recompile` / `recompile_status` completed with `failed:false`, no compilation errors. The effect was applied through live Editor C# with Undo and scene saving, then inspected in Play Mode. A first capture exposed an incorrect material slot; the assignment was corrected and the delivered capture confirms transparent fill, contour and readable nodes/links on the existing nine-point zone.

The screenshot is the composited Game View (`capture_game_view --source screen`, 1920x1080), with the skill-tree camera temporarily framed on the zone in Play Mode. Camera framing was not saved to the scene. See [game screenshot](../../output/BonusZone/bonus-zone-game.png) and [delivery report](../../output/BonusZone/Report.md).

Console inspection found existing Input Manager, Zenject, DOTween, tutorial and ailment-authoring warnings. One Pipeline main-thread timeout occurred while transitioning into Play Mode; the framing command succeeded after the scene was ready. No aura compilation/runtime error was observed. No tests, player build, performance profiling, far-zoom or spline/Undo interaction check was run.

## Inspected baseline

- `Assets/Scenes/MainScene.unity` contains a BonusZone with a closed SpriteShape spline and linear control points. Its SpriteShapeRenderer currently has sorting order -9 and a low-alpha tint.
- `Assets/Scripts/SkillTree/BonusZone.cs` counts active nodes in its serialized `nodes` list. Polygon geometry does not determine membership. Moving the visual boundary does not update that list.
- `Assets/Scripts/Visual/SkillTree/BonusZoneVisual.cs` currently owns localized TMP description text, not shape rendering.
- Packages record URP 17.4.0 and SpriteShape 14.0.1. `Assets/Shaders/SkillTreeNodePowerAura.shader` is an inspected local example of animated aura shading, but its circular geometry and additive treatment are not the proposed zone implementation.
- Backlog BL-033 records historical skill-tree rendering preparation cost; this is a reason to keep renderer population and rebuilds small, not proof of a new BonusZone bottleneck.

## Selected approach

Keep the existing SpriteShapeController spline as the sole authored visual outline. Use its generated fill geometry with an unlit transparent animated fill shader. Generate a narrow closed border ribbon from the same spline into a child MeshRenderer, with a separate unlit border shader. The border is derived geometry, not a second editable polygon.

Suggested new files:

- `Assets/Scripts/Visual/SkillTree/BonusZoneAuraVisual.cs`: references the source SpriteShapeController, owns generated border resources and appearance settings, and invalidates geometry when shape changes.
- `Assets/Shaders/BonusZoneFill.shader`: two slowly scrolling samples of one small seamless noise texture, soft contrast, independent fill opacity and tint.
- `Assets/Shaders/BonusZoneBorder.shader`: continuous core, analytic soft halo and sparse animated spark dots using ribbon coordinates.

Keep existing text behavior separate. Materials and scene attachment belong to the later implementation/setup scope, not this planning pass.

## Fill and animation

Use local XY coordinates measured in tree units rather than normalized polygon bounds. Reshaping the spline then changes coverage without stretching the energy pattern. The shader does not need to know the polygon vertices: SpriteShape fill geometry limits coverage, including concave shapes.

Start with two noise samples moving in different directions, a low-contrast remap and independently adjustable opacity. Around 0.03–0.08 opacity is an initial tuning range, not a guarantee of matching the concept art. Respect renderer tint/alpha and avoid accidental double attenuation from the current tint. Keep fill transparency separate from contour brightness. Prefer ordinary transparent or correctly premultiplied blending for the fill; a local shader halo gives the border glow without requiring bloom.

Animate shader coordinates using time. Do not move spline points or rebuild meshes to animate the aura. An independent visual clock is only needed if existing pause/time-scale behavior must be overridden.

## Border geometry and readability

For straight segments, build a narrow ribbon with distance along the perimeter as U and signed distance across the ribbon as V. Use bounded miter joins with bevel fallback at sharp corners. Close the geometry and animation seam explicitly. Spark density is measured per tree unit and independent of vertex count. Use periodic perimeter coordinates or a seam fade so the closing edge does not pop.

The fragment shader draws a continuous line even when spark brightness varies. Dot sparks shimmer and drift gently along the contour; no individual particle GameObjects are needed. The ribbon includes enough width for the soft halo, since a shader cannot render beyond its geometry.

Support arbitrary simple closed polygons, including concave forms, and either winding direction. Reject/flag fewer than three distinct points, duplicate/zero-length edges and self-intersections. Holes and self-crossing polygons are outside the initial contract. Very narrow gaps or acute corners can still overlap the border halo and require scoped visual checking. Curved spline segments would require sampling their tangents; current content is linear, so curved support is optional later work.

Keep the fill behind links and nodes. Place the border above the fill but below links/nodes, preserving existing fog/visibility composition after a scoped scene check. Start with fixed width in tree units and antialiasing; inspect actual zoom limits. If the contour disappears at the farthest zoom, add a bounded camera-dependent minimum width. Camera-width changes require geometry invalidation if CPU ribbon width owns thickness; they are separate from shape edits.

## Shape synchronization and controls

Rebuild on enable, editor spline changes/Undo, and explicit runtime shape invalidation. No continuous runtime geometry rebuild is needed for static authored zones. If runtime edits can come from external code, expose a refresh method and add lightweight spline-change detection only if necessary.

Expose tint, fill opacity, noise scale, two flow velocities, contour width/brightness, halo width/brightness, spark density/speed/intensity and a phase seed. Keep density and texture scale independent of polygon bounds. Share base materials where practical and select the per-zone property mechanism with URP batching implications in mind. Clean up generated meshes/material instances on disable or destruction as appropriate to their ownership.

## Implementation sequence and minimum verification

1. Implement the fill shader and appearance component, then tune transparency against nodes and links.
2. Generate the spline-derived border and implement continuous core, halo and sparks.
3. Connect editor/runtime invalidation, expose parameters and configure one existing zone through the Unity Editor.
4. Run a scoped Unity compilation/import check and inspect one animated zone at near/far zoom, one concave shape, and an acute corner. Move/add/remove spline points and use Undo to confirm fill and border stay synchronized. Confirm idle animation does not rebuild geometry.

No tests or full player builds are proposed. Target-device performance remains unmeasured until specifically checked. Node-list membership and text removal are separate decisions; this visual plan does not alter them.

Concept reference: [simplified cyan crossflow](../../output/BonusZoneConcepts/v4/02-crossflow.png). These concept PNGs are visual references, not textures to import into the effect.
