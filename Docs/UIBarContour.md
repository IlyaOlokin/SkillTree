# Bar contour rendering

[Home](Home.md) · [HUD and tooltips](Systems/HudAndTooltips.md)

Status: translated and reconciled with component, shader and material values on
**2026-09-28**. No new Unity render check was performed. Shader: UI/Bar Contour;
materials now live at `Assets/Materials/UIBarContourHallowed.mat` and
`Assets/Materials/UIBarContourProfaned.mat`, not the old Assets/Shaders paths.

## Setup and geometry

Add a decorative UI Image above the main bar, assign its bar sprite (the older
setup used bar_0 from bar.png), a contour material and UI > Effects > Bar Contour
Coordinates (UIBarContour). Disable its Raycast Target. For a whole-bar frame use
Simple; for the current fill use Horizontal Filled and synchronize fillAmount,
or resize the Image's RectTransform through its slider.

The component writes normalized current-geometry coordinates, aspect ratio and
fill-edge information to UV1, enabling the Canvas TexCoord1 channel. Reserve UV1
for it. Place it before Shadow/Outline mesh effects. Simple, Sliced and horizontal/
vertical Filled geometry are supported; radial fill is not an arc-contour implementation.

## Fade, corners and glow

Fade Start and Fade End are independent inward distances from left/right or
top/bottom edges as fractions of current width/height. For example, start 0.05
and end 0.25 keeps full alpha through 5%, fades through 25%, then becomes transparent.
The interval is their difference; changing start does not move end. End at/before
start produces a near-sharp boundary. Values are limited to 0.499 to leave a clear center.

Alpha Fade Curve Power shapes the already nonlinear smoothstep fade: one is the
ordinary curve, higher values fade faster, lower values retain alpha longer.
At the transition midpoint, power 2.5 gives roughly 18% instead of 50% alpha.
Power does not move the boundaries; reduce Fade Start to narrow the solid band.

Corner Radius is a fraction of the short side: zero is rectangular, 0.2 rounds
corners, 0.5 forms a capsule. Aspect data avoids stretched corner shapes; use uniform
Transform scale. Rounding removes pixels and cannot restore transparent sprite corners.
This is a rounded-rectangle contour, not distance to an arbitrary texture silhouette.

Bloom Color (HDR) and Bloom Intensity raise RGB brightness while preserving texture
and Image color. Zero intensity leaves ordinary color. The material provides an
HDR source; the camera's post-processing produces any surrounding halo. Use an
appropriate camera-rendered UI path, HDR and Bloom configuration. Overlay UI should
not be assumed to enter the camera's bloom path. The component does not configure
cameras or volumes. Older tuning recorded intensity 2 and radius 0.2; current
material values differ, as listed below.

## Half visibility and blending

Top Half Alpha and Bottom Half Alpha independently scale visibility. Half Split
Height locates the boundary from bottom to top (default 0.5; current shader range
0.01–0.99). Half Fade Distance fades inside the more visible half, leaving a disabled
half transparent including side edges. Equal half alphas produce no additional seam.
A split of 0.5 and fade distance 0.2 fades the upper half from 0.7 to 0.5 and the
lower half from 0.3 to 0.5. Bloom can spread beyond that visible half.

Align Image sizes/positions when combining halves. Coordinates follow local
geometry, so vertical flips swap visible top/bottom. Contour Blending selects
ordinary Alpha overlap or Additive light accumulation. Additive intersections become
brighter and cannot darken the background or maintain opaque color over a bright
background. Reduce intensity or Image alpha when too bright.

For additive contours to combine without an intervening fill covering earlier ones,
put their decorative layer after all fills/backgrounds in Canvas order. A material
switch does not reorder the hierarchy. Alpha also multiplies sprite and Image/material
alpha; transparent source pixels remain transparent.

## Masks and moving fill edges

Stencil Mask and RectMask2D, including softness, participate in rendering. RectMask2D
clips an already-computed contour; it does not create a new contour edge at its clip
boundary. Resize the Image or use Horizontal Filled for an enclosed moving-fill frame.

Fill Edge Softness fades inward at the moving horizontal/vertical Filled edge,
including its top/bottom contour bands. Fill Origin chooses the edge, and softness
is retained at full fill to avoid a final-step pop. Width is a fraction of the
current geometry's short side: 0.2 corresponds to about 14 UI units at height 70
while width is at least height, then shrinks with very short fills. Zero disables it.
Simple/Sliced, radial fill and an external mask's edge are unaffected. The shader
adds no geometry or independent glow outside the Image.

Without the component, only a Simple Image with a full-texture, non-atlased sprite
is suitable, with square-aspect rounding assumptions. Use the component for correct
proportions and duplicate materials for bars needing independent tuning.

## Current material snapshot

| Property | Hallowed | Profaned |
| --- | --- | --- |
| Top / Bottom alpha | 1 / 0 | 0 / 1 |
| Split / half fade distance | 0.5 / 0.2 | 0.5 / 0.2 |
| Fade Start X / End X | 0 / 0.094 | 0 / 0.094 |
| Fade Start Y / End Y | 0.04 / 0.378 | 0.134 / 0.378 |
| Fade Power | 2.5 | 1 |
| Corner Radius | 0 | 0 |
| Fill Edge Softness | 0.04 | 0.04 |
| Glow Intensity | 0.5 | 0.5 |
| Contour Blending | Alpha | Alpha |

These source-inspected values replace the older assertion that both materials had
Fill Edge Softness 0.2. They are current tuning, not mandatory future balance or style.

Sources: `Assets/Scripts/UI/UIBarContour.cs`, `Assets/Shaders/UIBarContour.shader`
and the material paths above. Verify actual rendering after any visual change.
