# Representative native Play Mode / simulator parity, 2026-10-09

[Stage-one scope](../../../Docs/Reference/BalanceAgentStage1.md) · [Comparison](comparison.json) · [Environment hashes](environment.json)

Four controlled scenarios produced matching Play/Edit traces across 750 ticks:

| Scenario | Ticks | Transitions exercised |
| --- | ---: | --- |
| Pain → Scar | 10 | Actual attack HP loss grants Pain; attack consumes it; Scar applies temporary modifiers and expires |
| Received-effect repeat | 10 | One accepted source application produces two Scar instances; repeated copy emits no recursive receipt/source event; unbinding prevents the next repeat; expiry removes modifiers |
| Barrier reactions | 10 | A 40-damage hit spends two 30-capacity charges with no HP loss; no Pain is gained; each lost charge grants one Surge; getting hit advances attack progress; expired modifiers revert; removing the progress binding prevents further gain |
| Ignite pool | 720 | Two additions stack into one pool, drain exactly 60 HP within float tolerance, expire, and never trigger attack-HP-loss Pain generation |

The original Editor executed an externally compiled fixture using the simulator's
Edit Mode effect-ownership cleanup and manual production RunTick. A separate hidden
batch Editor entered actual Play Mode in an isolated project: native Awake/Start,
BattleTickSystem.Update, registration callbacks and deferred Object.Destroy ran.
The tick driver was accelerated by SpeedMultiplier without changing fixed delta.
The observer paused it after an exact tick count. Both used production Unit,
Attacker, AttackProcessor, effects and modifier bindings with controlled temporary
definitions. No policy special case or authored gameplay asset was changed.

Comparison includes complete frame arrays and exact counter/event counts. Float
serialization uses max(1e-5, 1e-6 × magnitude) tolerance; incomplete or empty trace
arrays are failures. Semantic checks ensure the intended reactions occurred rather
than accepting equal empty outputs. The Edit exporter uses Newtonsoft because
Unity JsonUtility omitted the frame-list field for a dynamically loaded DTO; native
JsonUtility output was complete. This was an export issue, not a gameplay mismatch.

The copied Assembly-CSharp and Assembly-CSharp-firstpass DLLs matched source
SHA-256 values. The isolated project used a unique company/product name to separate
persistent data and contained no main game scene, installer or save coordinator.
Main project Assets/Packages/ProjectSettings were unchanged. The temporary project
and its caches were removed after preserving traces, logs and hash evidence.

Initial setup attempts failed on a malformed fixture assembly-reference array and
an excessive cold package import timeout. Those infrastructure issues were fixed;
the successful native run exited with code 0 and wrote completed.json. Package
compiler warnings were unrelated to scenario behavior. No full player build or
whole-game profile/UI session was run.

This is representative lifecycle/reactive parity, not exhaustive coverage of all
modifier combinations, target platforms or actual gameplay scene/service wiring.
Remaining scope includes other families, influence/bridge gems, repeated infinite
investment, adaptive respec and inventory-full reward delivery. Earlier full cohorts
remain historical because their tick/kill wiring preceded the corrected contract.

Reproduce: `pwsh -NoProfile -File Tools/BalanceSimulation/VerifyPlayMode.ps1`.
The verifier retains its newly created isolated project for diagnosis; remove only
that recorded temporary directory if desired after reading its results. Offline
comparison: `ComparePlayTraces.ps1 -Evidence <run-directory>`.
