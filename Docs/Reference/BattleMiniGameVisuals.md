# Battle mini-game visual authoring

[Home](../Home.md) · [Mini-game runtime](../Systems/BattleMiniGames.md)

Implemented 2026-10-15. This is a presentation change, with the current precision
and rapid-click gameplay, power, rewards, timing and unlock configuration retained.

## Art direction and ownership

Revised after owner feedback on 2026-10-15: use a few large functional shapes.
Damage has a complete light approach ring and one broad amber target band. The
center explicitly says `WAIT` or `CLICK NOW`, synchronized with the real success
window. Attack speed retains thick mint charge segments and puts a large press
counter in the center, with a thin outer timer circle that disappears clockwise
starting at twelve o'clock. Segment fill uses integer counts to avoid the reproduced
first-press double highlight from floating-point boundary comparisons. Ornamental frames, small
ticks, runes, turbines and sparks were removed. Opaque beds and plain text plaques
separate the controls from battle VFX. Success and failure use a large check or X.

Existing skill-tree icons and the current Game View were inspected for the bronze,
stone, warm highlights and dark background direction. The new geometry is procedural;
the prompts below propose painterly icon counterparts, not imported artwork.

Changed runtime source is under `Assets/Scripts/Battle/MiniGames`:

- `Views/MiniGameFeedback.cs`: creates the overlay, localized labels, animation and
  fade handling; preserves the existing input listener. The presentation keeps the
  controller's spawn point and fixed Button hit area. Hover adds a 3% scale target;
  clicks add to a decaying scale envelope capped at 7%. A single `SmoothDamp` blends
  their sum without resetting scale or velocity. Only the artwork grows, so hover
  cannot move the input boundary. Appearance uses alpha, and no Canvas recentering
  is applied. Press feedback also includes a ring ripple.
- `Views/MiniGameSigilGraphic.cs`: shared UI mesh, functional rings, charge and result shapes.
- `Views/TimingClickMiniGameView.cs`, `Views/RapidClickMiniGameView.cs`: feed actual
  gameplay state to the overlay and retain it for the result presentation.
- `BattleMiniGameActivator.cs`: feeds the correct motif and lifetime to the shared overlay.

`MiniGameFeedback` is already attached to these prefabs, with the existing counter font:

- `Assets/Prefabs/UI/MiniGames/DamageBuffMiniGame.prefab`
- `Assets/Prefabs/UI/MiniGames/AttackSpeedBuffMiniGame.prefab`
- `Assets/Prefabs/UI/MiniGames/MiniGameActivator.prefab`

No manual setup is required to use the implemented geometry. Existing scene and event
working changes are preserved. Raster icons have not been generated or assigned.
To integrate future generated icons, import them as UI Sprites and explicitly add an
icon Image to the presentation; there is currently no raster-icon override field.

## Localization

Added to **GameUI**, English table `Assets/Localization/GameUI_en.asset`, with keys
registered in `Assets/Localization/GameUI Shared Data.asset`:

| Key | English text |
| --- | --- |
| minigame.damage | DAMAGE |
| minigame.speed | ATTACK SPEED |
| minigame.align | CLICK WHEN RINGS ALIGN |
| minigame.wait | WAIT |
| minigame.now | CLICK NOW |
| minigame.rapid | CLICK RAPIDLY |
| minigame.perfect | PERFECT |
| minigame.success | SUCCESS |
| minigame.early | TOO EARLY |
| minigame.late | TOO LATE |
| minigame.timeout | TIME'S UP |
| minigame.damageReward | DAMAGE BOOSTED |
| minigame.speedReward | ATTACK SPEED BOOSTED |
| minigame.noReward | NO BONUS |

Labels refresh when the locale changes. English fallbacks match the table entries.
Russian and German entries were not changed; all fourteen keys remain translator TODOs.
No tooltip descriptions, term registrations or modifier localization were added.
Results deliberately show no reward percentage or score-based strength claim.

## Icon Generation Prompts

### Damage — impact seal

Create a square dark-fantasy skill icon with one large ivory steel sword inside
one broad amber ring. Strong centered sword silhouette, simple bronze crossguard,
charcoal background within the emblem, warm directional lighting and restrained
material texture matching a stone-and-bronze RPG skill tree. Communicate a precise
powerful strike through the blade and ring alone. Bold shapes readable at 64 pixels,
painterly finish, crisp edges, controlled glow. Transparent outside the emblem.
No small ornaments, runes, sparks, extra rings, text, letters, numbers, logos or watermark.

### Attack speed — rapid strikes

Create a square dark-fantasy skill icon with two large forward-pointing mint
chevrons shaped like clean blade strokes. Surround them with one simple broken
circular rim made of five broad charge sections. Strong immediate sense of speed,
charcoal background inside the emblem, subtle bronze accents, pale mint highlights,
restrained painterly metal texture matching a stone-and-bronze RPG skill tree.
Centered bold silhouette readable at 64 pixels, crisp edges and controlled glow.
Transparent outside the emblem. No turbine, gears, tiny ornaments, runes, particles,
text, letters, numbers, logos or watermark.

## Authoring and verification

`Tools/MiniGameVisuals/Authoring.cs` provides explicit Pipeline `run_script` entries:

```powershell
unity command run_script --file Tools/MiniGameVisuals/Authoring.cs --entry MiniGameVisualAuthoring.Apply --project-path C:/UnityProjects/SkillTree
unity command run_script --file Tools/MiniGameVisuals/Authoring.cs --entry MiniGameVisualAuthoring.Preview --project-path C:/UnityProjects/SkillTree
```

`Apply` attaches the component and updates only these prefabs and the English GameUI
entries. It does not edit scenes or event settings. `Preview` renders runtime copies
of the actual prefabs in an isolated preview scene and writes `Temp/MiniGames/visual-preview.png`.
It shows damage before and during the success window, attack-speed first-press progress
and a timer with its first clockwise quarter removed,
both successful outcomes and an early failure.
Reflection only freezes presentation parameters for this visual board; it does not
establish gameplay timing or reward correctness.

Verification performed: Unity script compilation completed with `failed: false`
and no compiler errors; the final isolated prefab render was inspected and produced
no new Console errors. The preview clears the camera target before cleanup and uses
unconnected copies so runtime Button reparenting is permitted in Edit Mode.
The preview explicitly sets its fade alpha to one for the frozen visual board.
The saved [visual board](../../Reports/Visuals/BattleMiniGames-CircularTimer.png) shows
one filled segment after the first press and a clockwise-disappearing outer timer.
A scoped Editor button invocation confirmed one registered press while the former
floating comparison lit two segments; the integer fill is verified by the updated render.
A bounded Editor invocation of the current hover handlers, three rapid Button clicks
and scale stepping at 60 Hz produced no instantaneous input-driven scale change.
Peak artwork scale was 1.08317 and it returned to 1.0000006; artwork and Button
positions stayed fixed, and the Button scale remained one. This checks the blending
path on an isolated prefab copy, not end-to-end pointer raycasting in Play Mode.
No tests or player
build were created or run. Full battle overlap, pointer interaction, pause playback,
locale switching and target-device performance remain unverified.
