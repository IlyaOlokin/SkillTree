# Procedural unit death: 8B Curved Wisp

[Home](../Home.md) · [Project map](../ProjectMap.md) · [Unit rendering](UnitShapeRendering.md) · [Combat](../Systems/CombatAndEffects.md)

Status: **implemented, 2026-10-14**, after the owner authorized implementation.
The implementation record below describes the current code and scoped Unity
verification. The original design proposal follows it as historical planning;
where they differ, the implementation record takes precedence.

## Implementation record

Runtime code is isolated in `Assets/Scripts/Visual/UnitDeath/`:

- `UnitDeathAppearance.cs`: copies effective material/property-block values,
  the world matrix, geometry, colors, sorting and the decorative time phase.
  It reads the existing UnitShapeView API; no new getter or other change to the
  living body controller was necessary. Its geometry record explicitly removes
  the source Transform reference.
- `UnitDeathVisualEmitter.cs`: subscribes once in early Awake, captures immediately
  on Unit.OnDeath and guards duplicate requests per activation. Exceptions are
  logged without preventing the remaining death subscribers from running.
- `DeathVisualPlayer.cs`: prewarms four view entries, caps its pool at twelve,
  reclaims the oldest presentation when full and disables idle Update callbacks.
  Profiles select a view prefab through the shared UnitDeathView contract.
- `CurvedWispDeathView.cs`: generates an extended body/core quad and a combined
  24-segment ribbon/fleck mesh. There are **two renderers per active effect**,
  rather than the three submissions suggested in the original plan. The cubic
  trajectory uses eased progress, bounded camera-space control points and a
  single moving core. Gap positions, motion handedness and 6–12 flecks vary
  with a private cosmetic seed.
- `DeathVisualProfile.cs`: common view contract and reusable profile settings.

`Assets/Shaders/UnitDeath/CurvedWispSurface.shader` ports the existing body
appearance formulas and references `UnitShapeGeometry.hlsl` and
`PainterlyColor.hlsl`. The stationary core is replaced by the moving core in this
single surface. `CurvedWispRibbon.shader` uses the same painterly color functions
for the generated trail and angular flecks. Both copy the source's paint controls;
no bitmap, SpriteSheet, Animator or captured production texture is used. The living
`UnitShapeSdf.shader`, hit effects, ailments and stat-wisp code were not modified.
Keep the death-local appearance formulas in parity when changing the body shader.

Configured assets:

- `Assets/Resources/Visual/UnitDeath/CurvedWisp.asset`: default 0.5-second profile.
- `Assets/Resources/Visual/UnitDeath/CurvedWispSurface.mat`.
- `Assets/Resources/Visual/UnitDeath/CurvedWispRibbon.mat`.
- `Assets/Prefabs/Visual/UnitDeath/CurvedWispDeath.prefab`.

The common `Assets/Prefabs/Unit.prefab` is a visual/component base with no concrete
Unit component. It now contains the emitter and its shape/profile references;
PlayerUnit.prefab and EnemyUnit.prefab supply the concrete Unit reference through
prefab overrides. MainScene contains the pool owner under `BattleRoot`.

**Placement refinement:** BattleRoot is a UI RectTransform. Actual pooled views
are detached scene-root GameObjects with identity transforms, owned and destroyed
by the pool. They do not inherit canvas layout or scale changes. Disabling
BattleRoot clears active copies through the owner; scene teardown destroys the
pool entries. Merely placing world-coordinate mesh vertices beneath that UI root
caused disagreement during camera/canvas relayout in the first captures and was
corrected. No source unit remains referenced during playback.

Playback uses unscaled Unity frame time and continues while **battle ticks** are
paused, including the pause used by the existing death/completion windows. Unity
Editor Play Mode pause freezes frame updates; it is not the game's battle pause.
No gameplay system waits for completion. Unit death, rewards, attack resolution,
pool reuse, wave delays and saved profile contracts were not changed. No localization,
status icons or tooltip entries were needed. Accurate elemental cause selection
still requires a separate future death-context change; only the default 8B exists.

### Verification performed now

Unity 6000.4.6f1, MainScene, via Unity CLI with explicit project targeting:

- `recompile` and `recompile_status`: completed, failed=false after the final
  runtime changes. ShaderUtil reported no messages for either new shader.
- Temporary `run_script` installation applied assets/prefabs and saved a previously
  clean MainScene. Existing runtime gameplay files were not edited.
- A paused Play Mode capture applied actual lethal Health.TakeDamage to one enemy.
  Unit.OnDeath launched one detached view and the source became inactive. Inspected
  callback order placed the emitter before the spawner and attack resolver.
- Saved BattleCamera frames at 0, 150, 320 and 500 ms, with manually selected visual
  progress for readable frame capture. The first frame retained the shader body,
  the intermediate frame showed a single rising core and broken contour, and the
  500 ms view was hidden. Camera target/viewport and asynchronous shader-preview
  settings were restored after capture.
- Circle, square and triangle captures used the actual view with snapshots of the
  source shape parameters. Three simultaneous requests were handled; source
  transform changes did not alter the copied appearance matrix.
- A separate live playback with BattleTickSystem paused returned its view to the
  pool by the observation at 0.703 seconds. This verifies cleanup during battle
  pause, not a frame-exact 500 ms measurement. Three more active requests were
  cleared on returning to the map: active count=0 and visible view count=0.
- Final Console ground truth: 0 errors and 0 warnings; no new captured errors after
  the final correction. Earlier iterations caught and fixed property-block creation
  in a MonoBehaviour field initializer and unstable parenting under the UI root.
  Editor was returned to Edit Mode.

No test suite, player build or target-device performance capture was run. The
player's actual game-over-window composition, full last-wave/instant-respawn path,
maximum pool load and scene-unload ordering were not separately exercised. BL-032
remains open for the wider visual/platform verification scope.

Real render evidence: [150 ms battlefield](../../output/death-concepts-2026-10-14/8b-development/implementation/battle-0150ms.png),
[320 ms battlefield](../../output/death-concepts-2026-10-14/8b-development/implementation/battle-0320ms.png),
[shape frames and report](../../output/death-concepts-2026-10-14/8b-development/implementation/README.md).

## Original design proposal

The unit itself is generated by a shader. The proposed death animation also uses
procedural rendering, with randomized parameters rather than animation frames,
captured images, baked textures or a sprite sheet. The generated preview is a
visual target only and must not become a production texture.

## Visual target

Develop the approved [8B reference](../../output/death-concepts-2026-10-14/variations/8B.png):
the original core leaves along a curved upward path while its hollow geometric
contour breaks and fades. Use the owner's battlefield screenshot rather than the
older filled Freeze concept. Preserve the unit's existing colors; the preview
shows red enemies, while the player would inherit its gold palette.

There is exactly one core. Its original position becomes empty as it moves.
Circle, square and triangle keep their silhouette, corners and tiny core symbol.
There is no conversion of polygon outlines into circular rings, no shockwave and
no permanent corpse. Marks and spikes reflect the actual dying unit, rather than
a fixed decorative pattern.

Suggested timing is a tuning target, not measured animation:

| Real elapsed time | Appearance |
| --- | --- |
| 0.00 s | Reproduce the dying body with its actual shader parameters and pose. The live unit, weapon and attached UI follow normal deactivation. |
| 0.00–0.08 s | Core starts moving; the contour loses continuity and its original accents begin fading. Avoid a separate large hit flash. |
| 0.08–0.22 s | One bright core exits upward-left or upward-right with a narrow tapered curved ribbon. The original center is empty; recognizable contour segments remain. |
| 0.22–0.36 s | Most body segments disappear. The moving core and short fading tail become the focal point, with a few dim local fragments. |
| 0.36–0.50 s | Core shrinks and dims, tail retracts and fragments disappear. At 0.50 s there is no visible residue. |

Start with 6–10 contour gaps and 6–12 small flecks. Gaps form on normalized
perimeter coordinates and fade in staggered order. Flecks start on the real
contour and travel a short distance outward along its normals. Square fragments
start on straight sides and corners; triangle fragments start on its three sides.
Do not turn the body into a radial explosion. The fill fades early without adding
new facets or a luminous solid interior.

Let D be the visible body's largest dimension. Initial motion ranges: rise
0.65–1.0 D, sideways displacement 0.15–0.35 D, fragment travel 0.03–0.12 D.
Use a bounded shallow S curve with one main bend, and clamp the trajectory to the
battle camera's playable region. Keep it away from the upper navigation UI.
Readability at the original gameplay scale takes precedence over a zoomed effect.

## Random variation

Generate a cosmetic seed once per death with a private random generator. Never
consume UnityEngine.Random or combat generation randomness. Store the seed in
the request so a chosen appearance can be reproduced in a visual preview.

Randomize curve handedness and control points, limited rise distance, gap positions
and widths, disappearance order, fleck position/rotation/speed/size and small
tail-width/brightness variations. Sample once; evaluate from normalized elapsed
time thereafter. Do not re-roll each frame or introduce noisy jumping motion.

Keep total lifetime, one-core rule, original shape and original palette invariant.
Left and right arcs and a narrower S bend are variations of one animation, not
three separate profiles. At corners, keep a brief recognizable remnant rather
than rounding the polygon as it disappears.

## Best integration boundary

Use a small presentation adapter beside UnitVisual on the shared Unit prefab.
Its input is **Unit.OnDeath**. Do not place an animation wait in Unit.Death,
EnemyUnit.Death, EnemySpawner or EnemyPool.

Inspected source:

- `Assets/Scripts/Battle/Unit.cs`: Death invokes OnDeath and then immediately
  disables its GameObject. OnDisable unregisters combat ticking.
- `Assets/Scripts/Battle/EnemyUnit.cs`: base death precedes kill notification
  and experience delivery. Presentation must not alter that ordering.
- `Assets/Scripts/Battle/EnemySystem/EnemySpawner.cs`: subscribes during spawning,
  removes dying enemies, resolves gold and handles wave completion immediately.
  A zero respawn delay can execute the next spawn synchronously inside the event.
- `Assets/Scripts/Battle/EnemySystem/EnemyPool.cs`: reuses existing unit objects.
- `Assets/Scripts/Visual/UnitVisual/UnitVisual.cs`: already owns attack, idle,
  hit and ailment presentation; do not add the death playback loop to those systems.
- `Assets/Scripts/UI/PlayerDeathWindowController.cs`: death pauses battle ticks
  and starts opening the window immediately.

The adapter must synchronously copy appearance before later death handlers can
reset or reuse the unit. Bind during visual initialization before the spawner's
dynamic death subscriptions; verify that ordering during implementation. The
player window currently pauses/opens UI without resetting the player's shape.
Do not defer snapshot capture to LateUpdate or a coroutine. Subscribe once and
unsubscribe on destruction; use a per-activation guard against duplicate requests.
An ordinary disable, stage change or pool reset must not create a death effect.

The request goes to a scene-local DeathVisualPlayer, outside the unit/pool
hierarchy, with its own small pool of presentation objects. It owns playback and
cleanup after the source is disabled, destroyed or reused. It never holds a live
Unit, DamageInfo, material property block or source Transform as playback state.

Unit.OnDeath → synchronous appearance snapshot → DeathVisualPlayer → selected
profile's view → return to visual pool at 0.50 s.

No reward, attack target, wave, save, stat or effect lifecycle waits for playback.
No completion callback from presentation is required by gameplay. Missing visual
configuration may skip the presentation but must still allow normal death.

## Reproduce the shader-generated body

UnitShapeView.GetGeometry already supplies shape, size, radius, triangle size,
rotation, mirrors, line width and sorting settings. **Its struct retains a live
Transform and is not an independent snapshot.** Copy the world matrix and values.
That method also does not supply all decorative shader parameters.

Add one read-only appearance snapshot method to UnitShapeView during later
implementation. Capture effective material values and renderer property overrides,
including contour rings, fill/halo settings, core radius/glow/color, actual mark
and spike counts/widths/heights/intensity, armor/crit contributions, paint controls,
vertex tint, and the decorative time phase. Copy values, not shared mutable state.
Resolve active hit-flash overrides explicitly so the first death frame does not
unexpectedly change color. Freeze the captured decorative phase; death has its
own progress clock.

The independent death shader reuses `UnitShapeGeometry.hlsl` and
`PainterlyColor.hlsl`. The existing body shader also contains private mark/spike
and energy-composition helpers: geometry reuse alone cannot reproduce the complete
body. Initially port those inspected appearance formulas into a death-local
helper and validate the first frame against the live body. This keeps the live
UnitShapeSdf shader unchanged, at the cost of maintaining parity when body style
changes. A shared appearance helper extraction is a possible later refactor,
not a prerequisite and not currently authorized implementation.

The death surface analytically masks perimeter segments, fill and decorations.
It omits the original stationary core after launch. A separately drawn small SDF
core uses the same shape symbol and its copied size/palette. Generate a ribbon
mesh along the sampled cubic curve, taper it by distance behind the head and
elapsed time, and bound its geometry so the ribbon cannot clip at the old body
quad. Flecks use generated quads with procedural angular coverage. No screen
capture, texture readback, whole-unit clone, physics fracture or Animator is needed.

Do not write death progress into the living renderer's property block. That block
is cleared by UnitShapeView.ApplyProperties and is also used by hit presentation.
Independent renderers and blocks avoid ownership conflicts with those paths and
with the ailment, barrier, wisp and weapon layers. Attached layers disappear with
the live unit; this first profile only redraws its body/core and death flecks.

## Proposed module and future profiles

Keep implementation under `Assets/Scripts/Visual/UnitDeath/` and its shaders under
`Assets/Shaders/UnitDeath/`. A small module is sufficient:

- **UnitDeathVisualEmitter**: lifecycle subscription and synchronous snapshot.
- **DeathVisualPlayer**: scene-local request handling, profile lookup and visual pool.
- **CurvedWispDeathView**: current procedural body/core/ribbon/fleck playback.
- **DeathVisualProfile**: visual prefab and bounded settings for a selected style.

Keep snapshot/request data and the minimal common view contract in this module.
A future fire/ice/lightning profile can provide a different view through the same
request and ownership lifecycle. Avoid an element switch inside Unit, EnemySpawner
or every living visual controller. Start with exactly one default 8B profile.

The request can reserve an optional visual cause identifier, with an unknown
fallback. Do not infer a lethal element from weapon type, active ailments or the
last remembered hit. OnDeath currently only carries Unit. Moreover,
Unit.ReceiveDamage calls health.TakeDamage before OnGettingHit; lethal OnDeath
occurs before that hit notification. DoT and mystic death thresholds add distinct
routes. Accurate elemental selection will therefore require a separate, scoped
damage/death-context change later; this first animation needs none of it.

## Clock, cleanup and cost boundaries

Proposed policy: use 0.50 seconds of unscaled real time, independently of battle
ticks and 1x/2x/4x speed. Already launched effects finish while a pause, tutorial
or death/completion window is open. This prevents terminal pauses freezing a
death remnant forever and requires no BattleTickSystem changes. Freeze-on-pause
would be a different product choice, not assumed here.

Own effects beneath a battle presentation root that is independent of unit
activation but disabled when leaving the battle screen. Root disable/unload
clears active effects and returns them to their pool. New spawns must never move
or recolor existing death copies. Keep camera culling/sorting compatible with
BattleCamera, beneath HUD/modal layers. Do not delay the player's death window;
it may naturally obscure part of the last effect.

Initial budget proposal: prewarm four visual entries, cap simultaneous playback
at twelve, then reclaim the oldest purely visual entry if necessary. A complete
entry uses one body surface, one ribbon and one combined generated core/fleck
mesh, roughly three draw submissions, with about 24 ribbon segments and at most
12 flecks. Share materials and reusable geometry buffers; maintain a list of
active effects instead of per-unit idle Update loops. These are bounded design
targets, not measured performance guarantees. The overall BL-032 visual validation
gap remains open; this proposal does not establish device performance.

## Later implementation verification

No tests or Unity build are required for this planning artifact. During separately
authorized implementation, perform scoped compilation and one visual scenario:
circle/square/triangle deaths, multiple overlapping deaths, immediate source
reuse, player death with paused ticks and exit to the map. Confirm exact first-frame
appearance, empty original core position, complete disappearance at 0.50 s and
unchanged gameplay death/reward timing. Verify subscription ordering and generated
ribbon bounds there. Do not add test suites without the owner's request.

## Generated preview

The [in-game concept sheet](../../output/death-concepts-2026-10-14/8b-development/in-game-preview.png)
shows four phases and three shape-aware randomized examples. It is generated
imagery rather than a Unity screenshot or verified animation. The
[generation prompt](../../output/death-concepts-2026-10-14/8b-development/Prompt.md)
records the references, visual constraints and single-core correction.
