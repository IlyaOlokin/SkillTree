# Balance Lab

Standalone local experiment manager and a persisted learning policy for the
external balance simulator. Double-click [LaunchLab.cmd](../LaunchLab.cmd) with
`MainScene` open in Unity Edit Mode and Pipeline ready. Keep the launcher terminal open.

See [setup, modes, learning, comparison and coverage](../../../Docs/Reference/BalanceLab.md)
for the authoritative workflow. Generated jobs and models live in
[Reports/BalanceSimulation/Lab](../../../Reports/BalanceSimulation/Lab).

For scripting, `node Tools/BalanceSimulation/Lab/lab.mjs run <settings.json>`
creates and executes one job, provided Node, PowerShell 7 and Unity CLI are
available through PATH or the documented runtime environment variables. The
settings JSON uses the form's identifiers (`mode`, `policy`, `modelId`,
`strategies`, `runsPerStrategy`, `epochs`, `seed`, route limits and learning settings).
Use a job manifest's `config` as a concrete template. Missing fields receive the
same defaults as the UI. The process returns zero only for a completed job.
