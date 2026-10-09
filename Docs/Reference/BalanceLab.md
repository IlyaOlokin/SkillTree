# Balance Lab: standalone experiments and learning

[Home](../Home.md) · [Tooling](EditorAndBuildTools.md) · [Simulator contract](BalanceAgentStage1.md)

Implemented and reviewed on 2026-10-09. The tool runs locally outside `Assets`.
It does not edit gameplay source, content assets or the player's save. It requires
the existing Unity Editor and Pipeline connection; it is not a standalone game executable.

## Start without Codex

1. Open this project's `MainScene` in Unity, in Edit Mode, and save any intended changes.
   A controlled campaign refuses a dirty source scene. Do not edit game inputs during a job.
2. Double-click [LaunchLab.cmd](../../Tools/BalanceSimulation/LaunchLab.cmd).
   Keep its terminal open. It starts the app at `http://127.0.0.1:8877/` and opens a browser.
3. Choose **Проверка** for a frozen policy or **Обучение** for learning between batches.
   Choose the strategies, campaign count, seed range and route limits, then click **Запустить**.
4. Open a job's stats/heatmap report, inspect its manifest or select a saved model's memory.
   **Сохранить настройки** preserves the form for the next launch.

The launcher uses installed Node.js, PowerShell 7 and Unity CLI, with fallbacks to
the runtimes already available on this machine. No npm packages, Python trainer,
cloud service or Codex session are needed. `BALANCE_PWSH` and `BALANCE_UNITY` are the
worker's runtime paths; the launcher resolves them. A different HTTP port can be
selected with `pwsh -File Tools/BalanceSimulation/StartLab.ps1 -Port 8878`.

If **Проверить Unity** fails, confirm that this project's Pipeline is running.
The recovery used on this machine is **Window → Pipeline → Settings… → Restart**.
The job log reports connection, scene or campaign failures rather than accepting
them as poor gameplay outcomes.

## Experiment modes

The existing **Текущий бот v3** remains available for baseline evaluations.
**Обучаемая политика** chooses nodes, numeric local gems and shop purchases, with
the option to wait or retain gold. Legal actions obey actual allocation, socket,
shop stock, funds and inventory constraints. All legally available tree nodes
are candidates; there is no hardcoded node or special-modifier exclusion list.
Major-node direction/value remains an estimate of 3.25 ordinary small nodes,
not a change to gameplay power.

Training uses the same immutable input model for every campaign in a batch.
After the whole batch finishes, it writes a new model version; the next batch
uses that version and a new seed range. For example, three strategies × five
campaigns × three batches means **45 campaigns**, not 15. Within a batch, all
strategies share the configured seed range. Batch `e` starts at
`seed + e * runsPerStrategy`, where `e` starts at zero.

A frozen evaluation uses deterministic maximum-score actions and never updates
weights. Select a saved model explicitly; an empty model selection means an
untrained initial policy. Evaluation refuses strategy/seed pairs recorded in
that model's training history. Reserve a disjoint seed range for evaluation.
This guard prevents recorded overlap; it cannot prevent repeated human tuning
against the same evaluation cohort.

Training-job summary metrics combine campaigns from changing models and show
sampling progress. The aggregate stats/tree viewer contains the **last sampled
batch only**, played by its input model. Its `experiment.sampledModel` and
`experiment.outputModel` distinguish the sampled model from the version learned
after that batch. Evaluate that output version separately before judging improvement.

## Learning and contextual memory

The first learning implementation is a residual linear actor trained with
Monte Carlo REINFORCE, not a neural network. Its action score is
`log(1 + max(0, heuristicPrior)) + weights · features + contextualOffset`.
Sixteen bounded features describe action value/cost, node kind and direction,
offence/defence, threat type and build prerequisites. Context keys contain the
attribute, dominant enemy damage type, barrier/regen state and stage bucket.
Training samples a temperature-scaled softmax mixed with epsilon exploration.

Reward is given for a newly completed stage and, additionally, a completed
location; an unsuccessful attempt has a configurable penalty. Repeat farming,
kills, gold and increased stats have no separate reward. Rewards are assigned
to the decisions made before combat, excluding spending after its outcome.
Future reward events are discounted by gamma. A strategy/stage-bucket baseline,
bounded advantage and clipped batch gradient limit updates. Feature weights
and context/action offsets are saved after each batch.

The memory table also records how often an action was selected in a context
and its average subsequent return. These are observational statistics, not a
causal estimate of a node's strength: build history, route and future choices
also affect return. Global feature weights can transfer between similar actions;
offsets and recorded memory remain specific to their context and definition ID.

No automatic balance editing, unrestricted respec or gameplay code changes are
part of training. The existing bot supplies the simulator and initial heuristics.
The initial smoke model demonstrates that updates and persistence work; it has
not been shown to outperform the old bot.

## Persistence, interruption and comparison

Data lives in [Reports/BalanceSimulation/Lab](../../Reports/BalanceSimulation/Lab):

- `jobs/<id>/job.json`: immutable requested settings, input fingerprints, epochs,
  strategy/seed pairs, completed episode paths, model versions and current status.
- `jobs/<id>/epoch-N/episode-N/<attempt>/`: preset, compressed report and per-run artifacts.
- `models/<id>.json`: immutable content-addressed model, parent version and training history.
- `comparisons/<id>.json`: paired results. `last-settings.json` preserves the form.

Only one worker uses the project's Unity connection at a time. **Остановить**
requests cancellation at a campaign chunk/stage boundary; it does not interrupt
a combat tick. **Возобновить** keeps completed campaigns and repeats the unfinished
campaign from its initial seed. It does not restore a partial combat snapshot.
Completed reports admitted by the current worker carry SHA-256 checksums.
Model content IDs are checked before use. Earlier smoke reports lack the new
per-episode checksum field; they still undergo structural and strategy/seed checks.

The worker fingerprints authored source/serialized game inputs, project settings,
package source/configuration and simulation/learning code before and after each
campaign. Changing controlled inputs stops admission of the current result.
Resume requires the same fingerprints. Fingerprints are recorded identities,
not full copies: keep a separate Git revision or backup if old content must be restored.

**Парное сравнение** requires completed frozen evaluations, equal scenario settings,
equal simulator identity and matching strategy/seed pairs. Changing only the
policy compares agents; changing only game content with the same frozen policy
compares balance. Changing both is rejected. The report shows means, paired stage
differences, median differences and counts of better/worse/equal pairs. With at
least five seeds per strategy it also supplies a deterministic paired bootstrap
interval. Small cohorts remain exploratory; a positive mean alone is not proof
of an improvement. Identical inputs/policy also report full gameplay-record matches.

## Coverage and inspected implementation

The simulator's [stage-one limits](BalanceAgentStage1.md#honest-remaining-coverage)
still apply. Numeric `LocalModifiers` gems are supported; influence and bridge
gem decisions, all special-modifier combinations, adaptive respec and inventory-full
reward parity are not established. Admitting all nodes is not exhaustive fidelity.

Sources: [launcher](../../Tools/BalanceSimulation/StartLab.ps1),
[server](../../Tools/BalanceSimulation/Lab/lab.mjs),
[worker](../../Tools/BalanceSimulation/Lab/worker.mjs),
[model/input contracts](../../Tools/BalanceSimulation/Lab/core.mjs),
[trainer](../../Tools/BalanceSimulation/Lab/trainer.mjs),
[comparison](../../Tools/BalanceSimulation/Lab/compare.mjs),
[actor sampling](../../Tools/BalanceSimulation/Runtime/LearningPolicy.cs) and
[campaign adapter](../../Tools/BalanceSimulation/Runtime/Campaign.cs).
Live verification is recorded separately in
[the standalone check record](../../Reports/BalanceSimulation/Lab/verification-20261009.md).
