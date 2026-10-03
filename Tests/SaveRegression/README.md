# Save regression checks

Run from the repository root on Windows with the .NET 9 SDK installed:

```powershell
& Tests/SaveRegression/Run.ps1
```

The runner compiles production save storage, codec, coordinator, profile manager,
data models and node migration against lightweight game/Unity stubs. Test saves
and compiler output go into a unique directory under `Temp`; player saves are
not accessed. The 13 groups cover whole-snapshot recovery, interrupted writes,
invalid payloads, node ID migration, legacy import, bounded debounce/retry,
gem transfers, reset interruption and damaged profile metadata.

Character state commits as one `profile.sav` envelope. Recovery selects a whole
generation from `.bak1` or `.bak2`, never individual character subsystems.
Legacy files are imported only when no snapshot or recovery copy exists.
Autosave waits 0.75 seconds after the latest change, capped at 5 seconds from
the first pending change; failed writes retry after 5 seconds. A crash can
lose pending progress, but cannot split a committed gem transfer across files.

Reset commits a `resetRequested` snapshot before removing legacy data and old
backups. On load, this marker applies the game's defaults and saves a normal
snapshot. Remaining legacy files after interrupted cleanup are ignored.
An unreadable existing profile index or manifest raises an error instead of
silently replacing the selected profile with a new one.

These tests substitute Unity JsonUtility and gameplay services. They do not
exercise Unity lifecycle ordering or WebGL IndexedDB durability. A Unity Play
Mode/browser smoke test is still useful: move a gem both ways, reload, switch
profiles, reset, and reload again. Browser persistence is asynchronous, so a
forced tab/process termination can lose the last pending flush.
