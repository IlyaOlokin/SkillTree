# Balance agent: stage-one simulator contract

[Home](../Home.md) · [Tooling](EditorAndBuildTools.md) · [Simulator](../../Tools/BalanceSimulation/README.md)

Review date: 2026-10-09. Stage one has passed its bounded readiness checks, not an assertion
that every mechanic matches an independently played game. Gameplay code, scenes,
prefabs, definitions and player saves were not edited by this work.

## Findings fixed in the external campaign adapter

The old adapter manually called Unit.CombatTick on actors with no injected
BattleTickSystem. Unit.RequestModRecalculation and RaiseOnModsChanged therefore
recalculated immediately, whereas registered live actors defer changes to the
Mods phase. The adapter now registers combat actors with a disposable production
tick driver and calls its actual RunTick delegate. The driver owns phase order
and registration queues. Initial/reset-boundary Mods flushes remain explicit.
Decision-preview actors are unregistered and are never included in combat ticks.

The old enemy adapter set Attacker's target but not EnemyUnit's injected
`_playerTarget`. EnemyUnit.Death consequently could not notify the simulated
player through NotifyEnemyKilled. The adapter now supplies that target before
initialization. Production death/XP methods remain unchanged.

The campaign now reads the scene's actual TickDuration; wave tick limits and
respawn delays use that duration rather than an assumed 60 ticks per second.
Actors are explicitly unregistered during cleanup. Failed registration is an
error rather than silently falling back to synchronous recalculation.

## Checks actually performed

- Live contract diagnostic: adding an outer HP modifier left cached HP unchanged
  and marked it pending; production Mods processing then applied it and cleared
  the pending flag.
- Live death diagnostic: lethal production Health damage on an isolated enemy
  produced exactly one player OnEnemyKilled notification.
- Two identical seed-31001 runs, three balanced strategies each, two initial
  locations, one attempt per stage, 60-second wave cap. All per-campaign gameplay
  JSON matched exactly: progression, waves, allocations, rewards and snapshots.
- After diagnostics: zero temporary campaign actors; active scene not dirty.
- Independent native Play Mode comparison in a disposable project: four
  representative reactive/effect scenarios matched all 750 per-tick snapshots.
  Native Awake/Start and deferred destruction ran in that project's actual Play
  Mode; its production tick driver advanced from native Update. The Edit Mode
  side used manual production ticks and the simulator's effect ownership adapter.
  Gameplay DLL SHA-256 values matched between source and native project.

Evidence: [repeatability record](../../Reports/BalanceSimulation/20261009-172550-791-fidelity/verification.md),
[contract diagnostic](../../Reports/BalanceSimulation/20261009-172550-791-fidelity/contract.json).
Native comparison: [record and scope](../../Reports/BalanceSimulation/20261009-175543-107-play-parity/verification.md),
[comparison results](../../Reports/BalanceSimulation/20261009-175543-107-play-parity/comparison.json),
[environment hashes](../../Reports/BalanceSimulation/20261009-175543-107-play-parity/environment.json).
The adapter was compiled/executed by live Unity CLI; no player build or new
regression suite was run. Re-run via `Tools/BalanceSimulation/VerifySimulator.ps1`.

## Honest remaining coverage

The native comparison covered Pain/Scar consumption and expiry, received-effect
repeat/no-recursion and unbinding, barrier loss/Surge/attack-progress reactions,
and Ignite stacking/expiry with the DoT-to-Pain exclusion. Other families and
combinations still need individual coverage before declaring exhaustive parity. So do
scene/service-dependent mechanics, influence/bridge gems, repeated infinite
investment, adaptive respec and inventory-full reward behavior. The fixed-battle
adapter still has its old manual tick scheduling and integer-cost assumptions;
do not use it as an independent parity oracle for the corrected campaign adapter.

The 3.25 major-node score is a policy estimate, not a combat rule. Admitting all
nodes does not prove every mechanic's integration. Historical campaign cohorts
used the older contract and remain historical; do not overwrite them or attribute
new results solely to learning or balance changes.

Stages two and three now have a [standalone local application and learning policy](BalanceLab.md),
including persisted experiments, immutable model versions and paired frozen
evaluations. Their bounded live checks establish operation and reproducibility;
they do not establish improved agent strength or expand the fidelity coverage above.
