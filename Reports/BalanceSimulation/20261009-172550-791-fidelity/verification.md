# Simulator fidelity checkpoint

Two identical bounded three-strategy campaigns used the same seed, two initial
locations, one attempt per stage and a 60-second wave cap. Campaign gameplay data,
including waves, progression, allocations, rewards and snapshots, matched exactly.
Timings and generated timestamps are intentionally not compared.

This checks reproducibility, not full parity with a separately played Unity game.
The adapter now registers disposable combat actors with production BattleTickSystem,
runs its RunTick method, and binds each EnemyUnit's injected player target so
kill reactions receive production death notifications. Actor registration fails
explicitly if binding is unsuccessful. No gameplay source or scene is edited.

Remaining fidelity coverage: independent Play Mode comparisons for representative
reactive/effect families, inventory-full reward behavior, influence/bridge gems,
repeated infinite investment, adaptive respec and scene/service-specific mechanics.
These limits must remain visible before training or claiming exhaustive parity.
The [live contract diagnostic](contract.json) also passed: modifier changes were
deferred, applied in production Mods phase, and an enemy death notified its player
exactly once. Post-diagnostic inspection found no temporary actors and the active
scene was not dirty. See [stage-one scope](../../../Docs/Reference/BalanceAgentStage1.md).
