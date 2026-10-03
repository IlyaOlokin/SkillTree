# Battle mini-games

[Home](../Home.md) · [Project map](../ProjectMap.md) · [Screen flow](MenusAndScreenFlow.md)

Status: **source-reviewed, 2026-09-28**. Controller, rules, spawning, view result
paths and reward implementation inspected. Prefab wiring and gameplay were not tested.

## Definition, availability and spawning

BattleMiniGameEventDefinition supplies an ID, activator prefab, game prefab,
reward, base power, activation/game durations, placement, random intervals and
trigger cooldown. OnValidate keeps the maximum random interval at least the minimum.
Keep IDs unique for ID-based lookup and event-specific power bonuses.

BattleMiniGameController associates a player with its active controller registry.
Available events combine initialEvents with reference-counted modifier unlocks,
deduplicated by definition reference. Unlock/Lock contributions allow several
modifiers to keep an event available independently.

RandomBattleMiniGameSpawner maintains a separate timer for each available random
event. Timers use Time.deltaTime, advance only while battle is active, and reset
after a spawn attempt even if it fails. The controller's parameterless random
selection instead picks uniformly from available random-enabled definitions.
Triggered spawning uses a per-definition cooldown based on Time.time, started
only after successful spawn.

The low-level TrySpawn checks active battle, definition and activator prefab;
it does not enforce availability or trigger cooldown. Call the intended random,
ID or triggered entry point instead of assuming those restrictions are universal.
There is no activator-count cap in this method. Spawn creates/binds an activator,
optionally randomizes its position and emits EventSpawned.

## Run lifecycle

1. Activators tick with scaled delta time; their expiry countdown is paused while
   a mini-game view is active.
2. Activation refuses a second active view, instantiates the game prefab and
   consumes the activator. The prefab must contain an IBattleMiniGameView; a missing
   implementation logs an error and tears down the failed instance.
3. The run context snapshots definition, player, calculated power and duration.
4. Context completion resolves the result/reward once. View completion tears down
   the UI; result animations can therefore finish after the reward was applied.
5. Disabling the controller or leaving battle clears activators and cancels the
   active game without granting a new success reward.

The overlay toggles a configured root and sets the blocked CanvasGroup's
interactable/blocksRaycasts to the inverse of activity. It does not acquire a
BattleTickSystem pause or preserve an arbitrary previous CanvasGroup state.
Combat pause and mini-game timing are distinct; integrate with
[tutorial blockers](Tutorials.md#presentation-and-pause-ownership) deliberately.
The random spawner can continue creating activators during an active mini-game.

## Views and timing

| View | Result behavior |
| --- | --- |
| ManualCompleteMiniGameView | Explicit success/failure methods; scaled timer fails on expiry |
| RapidClickMiniGameView | Reach the configured required press count before its local timeLimitSeconds; score decreases with elapsed time |
| TimingClickMiniGameView | Click inside the configured window around perfectTimeSeconds; perfect-window clicks score one, outer-window clicks scale down; early/late clicks fail |

Rapid and Timing views use their own serialized timing parameters rather than
automatically deriving them from context.Duration. A rule that increases the
event's game-time value therefore does not necessarily lengthen these views.
Inspect the view implementation when adding time bonuses or authoring a new game.

## Rules and rewards

Power is `BasePower * max(0, 1 + globalBonus + eventBonus)`. Activation and game
time use their corresponding base duration multiplied by `max(0, 1 + bonus)`.
IgnoreRewardPercent is clamped to 0..1. These are runtime rule contributions,
not a separate saved progression system.

A successful resolved run applies the reward with the captured power and multiplier
one. Expiry of an unactivated icon can apply a partial reward using the current
rules and IgnoreRewardPercent. Clearing icons on exit is not this expiry path.
ApplyPartial scales the reward multiplier and delegates to Apply.

The damage reward uses `moreDamage * power * rewardMultiplier`; the attack-speed
reward follows its corresponding stat. The reviewed damage reward does not scale
by result score. MiniGameMoreStatBuffEffect replaces same-type magnitude and resets
duration to the incoming value on reapplication; it does not accumulate copies.
Removal detaches and releases its runtime modifier. See [effect ownership](CombatAndEffects.md).

## Verification and sources

Suggested checks: unlock reference counts, random versus triggered entry points,
expired versus cleared icons, duplicate completion signals, leave during result
animation, scaled-time pause, missing view, timing bonuses and buff replacement.
No runtime tests were run in this documentation pass.

- `Assets/Scripts/Battle/MiniGames/BattleMiniGameController.cs`
- `Assets/Scripts/Battle/MiniGames/RandomBattleMiniGameSpawner.cs`
- `Assets/Scripts/Battle/MiniGames/BattleMiniGameRules.cs`
- `Assets/Scripts/Battle/MiniGames/BattleMiniGameEventDefinition.cs`
- `Assets/Scripts/Battle/MiniGames/BattleMiniGameRunContext.cs`
- `Assets/Scripts/Battle/MiniGames/BattleMiniGameReward.cs`
- `Assets/Scripts/Battle/MiniGames/Views/ManualCompleteMiniGameView.cs`
- `Assets/Scripts/Battle/MiniGames/Views/RapidClickMiniGameView.cs`
- `Assets/Scripts/Battle/MiniGames/Views/TimingClickMiniGameView.cs`
- `Assets/Scripts/Battle/MiniGames/Rewards/MiniGameDamageBuffReward.cs`
- `Assets/Scripts/Battle/MiniGames/Rewards/MiniGameMoreStatBuffEffect.cs`
