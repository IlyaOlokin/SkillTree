# Standalone and learning verification — 2026-10-09

[Workflow and coverage](../../../Docs/Reference/BalanceLab.md) · [Machine-readable evidence](verification-20261009.json)

These are bounded live development checks of the requested tool. No new unit-test
suite, additional player build or exhaustive mechanics audit was performed.
Campaigns used two initial locations, one attempt per stage and a 60-second wave cap.
The initial training/evaluation runs predate the final worker checksum/provenance
enhancements; the final two-batch run exercised those enhancements live.

| Check | Stored job | Observed result |
| --- | --- | --- |
| Three-strategy learning, seed 41001 | [training manifest](jobs/20261009164332096-a2194dce/job.json) | Three completed episodes, 63 usable decisions, 13 nonzero feature weights, 65 context/action memory entries, immutable output model `71ba13117239d82f068184f29c678927` |
| Frozen evaluation, seed 51001 | [first manifest](jobs/20261009164618998-4c0ec484/job.json) | Three completed episodes, STR/DEX/INT-lightning stages 10/6/10 |
| Identical frozen repeat | [repeat manifest](jobs/20261009164833788-0300768a/job.json) | All three full gameplay records match, all paired stage differences zero; the frozen model's content checksum remains valid |
| Stop/resume, STR seeds 61001–61002 | [resume manifest](jobs/20261009165118826-e6ffe55c/job.json) | Stopped after one completed campaign, resumed to two; original completed report path and SHA-256 preserved |
| Sequential learning, STR seeds 71001–71002 | [two-batch manifest](jobs/20261009185800566-29ffcdfb/job.json) | Two batches completed; second input equals first output; final model has five accumulated episodes and three updates |

The sequential run saved `71ba… → 49ad… → 619696a1a6eac439288d0a9650f50afc`.
Its episode checksums were checked, and the aggregate compressed report records
the last sampled input (`49ad…`) separately from the final learned output (`6196…`).
Its [interactive report](jobs/20261009185800566-29ffcdfb/report.html) covers only that
last sampled batch. The learned output still needs a separate frozen evaluation
before any claim about strength.

The local application was started through `StartLab.ps1 -NoBrowser`, then used
through its browser interface to start the two-batch run, select a model's memory
and perform the paired repeat comparison. Memory rows show recorded context and
node modifier descriptions from the sampled catalog. Settings persistence and
Unity availability were checked through their UI controls. JavaScript syntax
checks and PowerShell parsing passed for the edited launch/publish paths.

Main gameplay source/assets were not edited. The worker's authored-input identity
was stable across the final campaigns. Earlier stage-one fidelity evidence remains
separate and bounded. These short samples verify operation and persistence, not
learning convergence, improved balance coverage or superiority to the heuristic bot.
