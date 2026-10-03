# Procedural Magic Root UI

[Home](../Home.md) · [HUD and tooltips](../Systems/HudAndTooltips.md)

Status: migrated into the vault on **2026-09-28**. Component/shader entry points
were spot-checked; the rendering checks below are historical, not rerun in this pass.

## Inspector controls

Add ProceduralMagicRootUI to a RawImage/Image, or assign an existing Graphic such as MysticBar/Fill. No branch texture or material asset is required.

Choose Light or Darkness and edit the displayed parameters directly. Each component serializes separate Light and Darkness settings. Switching styles or pressing the corresponding button restores your settings, not factory constants. The current style is saved automatically when edited. Existing component tuning is preserved when upgrading. Custom begins with the current values and does not overwrite either saved style.

Settings edited in Edit Mode persist in the scene/prefab. As with other Unity components, ordinary Play Mode changes are reverted on leaving Play Mode. Use prefab overrides/Apply as usual when changing a prefab instance.

Seed, Fill Amount and Fill Softness are shared across styles. Each observed transition of effective fill from 0 to a positive value automatically rolls a different seed, including Image.fillAmount. Positive fill changes and style switches keep that seed. Initial display and re-enabling do not reroll by themselves. If Mystic Colors Config is assigned, it supplies the Light/Darkness colors; edit colors in that asset or clear the reference to use the component's color.

## Glow

The Inspector Glow section shades each branch from a saturated colored rim to a bright HDR core. Glow Color and Glow Intensity control the core, Core Width controls its width, Core Sharpness shapes the center-to-edge gradient, Edge Brightness controls the colored rim, and Glow Falloff fades core emission toward the current fill endpoint. The shader evaluates paired outer/core distances in the existing segment loops, without an extra rendering pass. Each Light/Darkness preset remembers these values. Mystic Colors Config only overrides the base color. Set Glow Intensity to 0 to disable this layer. Emission stays within the existing silhouette and obeys fill, alpha and UI masks; the surrounding bloom is still provided by the camera post-processing path.

## Branches and shape

- Branch Count: number of deterministic primary branch candidates, default 64, range 8–96.
- Branch Density: probability that a candidate grows. Combine with Branch Count for denser patterns.
- Secondary Branch Density / Length: smaller offshoots.
- Thickness / End Thickness: root radii in bar-height units. The final tip always reaches zero.
- Main Path Amplitude / Frequency: vertical displacement and path variations.
- Branch Length / Width / Angle: length in bar heights, width relative to trunk, angle in degrees.
- Curvature / Jaggedness / Spike Amount: smooth bends, angular deviations and thorns.
- Taper Power / Thickness Falloff: thickness decrease toward the current fill endpoint.
- Length / Density Falloff: reduce branching detail toward the right.
- Style Blend: affects geometry and silhouette independently of color.
- Intensity / Left Intensity Boost: HDR RGB multipliers without a halo.

## Absorbed damage style

HealthBar selects the saved Light or Darkness style from current MysticHealth absorption, both at startup and when absorption changes. At zero absorption it retains the last style while the existing slider empties. Mystic Root is found on the same object or under the mystic slider; an explicit reference is also available. Inspector tuning remains stored per style. When the root is active, the Image is white so its tint does not multiply the shader color twice.

## Fill behavior

Fill now retapers the visible root instead of simply cutting a full-length shape. Its thickness reaches zero at the effective fill endpoint; nearby branches shorten to stay inside it. Trunk and branch anchor locations remain seed-based, but widths and branch lengths change with fill. Subpixel tips fade to prevent a bright endpoint dot.

For an existing Image with Type = Filled, Fill Method = Horizontal, Origin = Left, the component automatically reads Image.fillAmount. Leave the component Fill Amount at 1 to let the existing health UI control the length. If both values are lower than 1, the smaller value determines the endpoint. The component does not modify Image.fillAmount or health logic.

RawImage and Simple Image use the component Fill Amount directly. Fill 0 is empty; fill 0.5 ends in a tapered tip at the midpoint; fill 1 uses the full width. Right-origin, vertical and radial Image fills do not drive the automatic horizontal taper.

## Rendering

A single transparent HLSL pass evaluates tapered segments: 48 continuous trunk segments, up to 96 primary candidates with up to 6 segments each, optional secondary forks and 48 thorn candidates. Integer hashing combines seed, cell and branch depth. No time input, authored branch texture, mesh generation, compute pass or RenderTexture fallback is used.

The shader reconstructs Graphic-local coordinates through world space, supporting Canvas batching and transformed UI. Aspect ratio and the drawing rectangle update automatically, including Simple/Filled Image sprite padding and Preserve Aspect. Texture/sprite alpha and RawImage UV Rect are ignored. Image geometry still determines the drawable area; Simple/Filled are the intended Image types.

UI stencil Mask and RectMask2D participate in rendering. Every component owns one material, updates cached stencil visual properties, restores the original material on disable, and releases its own material. Assign only one root component to a target Graphic.

The serialized shader reference keeps the shader in configured scene/prefab builds. If every component is created exclusively at runtime, retain a configured prefab or include UI/Procedural Magic Root in Always Included Shaders. HDR/bloom requires an appropriate HDR camera/UI render path; ordinary Screen Space Overlay is normally after camera post-processing.

## Historical validation record

Unity 6000.4.6f1 / URP 17.4.0 / RTX 4070. Isolated real Canvas rendering checks cover compilation, RawImage, Simple/Filled Image, translated UI batching, masks, deterministic seeds, independent materials, edit-mode material lifecycle, style switching/serialization and current-fill tapering. Captured silhouettes are inspected visually. The original record placed tests in the temporary project Temp/MagicRootValidation; its continued availability and results were not verified in this pass.

Requires shader target 3.5. Mobile GPU performance, other graphics APIs and player builds have not been verified. Dense settings cost more than a textured UI quad; profile simultaneous bars on target hardware.

## Source entry points

- `Assets/Scripts/UI/ProceduralMagicRootUI.cs`
- `Assets/Scripts/UI/HealthBar.cs`
- `Assets/Scripts/UI/Editor/ProceduralMagicRootUIEditor.cs`
- `Assets/Shaders/ProceduralMagicRootUI.shader`

