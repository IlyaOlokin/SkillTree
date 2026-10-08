# Saves and Profiles

[Home](../Home.md) · [Project map](../ProjectMap.md) · [Inventory](InventoryAndItems.md) · [Skill tree](SkillTree.md)

Status: **source-reviewed, 2026-09-27**. Scope: profile documents, autosave,
recovery, reset, identity and platform boundaries. No player save was modified,
and no regression run or Unity/browser playtest was performed for this pass.

## Storage layout and ownership

`SavePaths` roots storage at `Application.persistentDataPath/Saves`. The actual
absolute location depends on the runtime platform and product settings.

| Document | Role |
| --- | --- |
| `profiles_index.sav` | Profile list and selected profile ID |
| `Profiles/<profileId>/manifest.sav` | Display name, timestamps and save count |
| `Profiles/<profileId>/profile.sav` | One complete character-state snapshot |
| `Profiles/<profileId>/player.sav`, `progress.sav`, `skilltree.sav`, `inventory.sav` | Legacy subsystem files used for import |
| `Settings/cloud_settings.sav`, `local_settings.sav` | Separate settings documents |

`GameSaveCoordinator` captures/applies gameplay state. `SaveProfileManager` manages
profile selection and metadata. `SaveFileCodec` encodes documents; `SaveFileStorage`
handles writes, backups and read fallback. `CloudSettingsService` uses the same
local file storage: its name alone does not establish remote cloud synchronization.
For the consumers of these settings, read [language selection](Localization.md)
and [audio volumes](Audio.md); their in-memory changes and explicit saves are
separate from character autosave.

## One character snapshot

As of 2026-10-08, `PlayerSaveData.skillPoints` is a float so half-point travel-node
costs survive capture and restoration. Existing integer-valued JSON balances are
accepted by the same field. Infinite-node investment counts remain integers.
No historical allocation-cost reimbursement or profile rewrite is performed.

`ProfileSnapshotSaveData` contains player progression/gold, location progress/shop
purchases, tree state, inventory and tutorial progress. Any dirty subsystem causes
the coordinator to capture the complete current snapshot, rather than updating
one subsystem file in isolation.

This boundary matters for [gem transfers](Gems.md): recovery must not combine an
old inventory with a newer tree and duplicate or lose a committed gem. Snapshot
recovery selects a whole generation. Manifest metadata is updated afterward;
failure to update the manifest does not undo a committed character snapshot.

Snapshot validation requires the expected profile ID and non-null player,
progress, tree and inventory sections, unless it is a reset marker. Tutorial
progress is not required by this completeness predicate. Do not assume this check
validates every individual content ID or gameplay value.

## Autosave and profile switching

The [2026-10-01 Editor capture](../Reference/PerformanceCapture20261001.md)
measured nine synchronous coordinator Tick calls of approximately 40–65 ms.
Those measurements precede the background-autosave change of 2026-10-02.
Individual stage costs and the effect of that change remain unmeasured.

The coordinator loads settings and the selected profile, then subscribes to
progression, gold, tree, inventory, shop and tutorial changes. During restore,
dirty marking is suppressed so intermediate restore events are not saved.

- Normal autosave waits **0.75 seconds** after the latest change.
- Continuous changes are bounded by **5 seconds** since the first pending change.
- Failed writes remain dirty and retry after **5 seconds**, avoiding writes every frame.
- Timing uses unscaled time, so the debounce is not tied to combat speed.
- `SaveNow` requests a complete snapshot; creating/switching a profile first flushes
  pending changes through the coordinator.
- Disposal attempts a dirty save. Quit saves dirty state and settings; WebGL also
  handles loss of focus.

`GameSceneInstaller` binds `GameSaveCoordinator` through Zenject's lifecycle
interfaces. `Initialize` subscribes to `Application.quitting` (and WebGL
`focusChanged`); `Dispose` removes both subscriptions. This is a plain C# service,
so there is no `OnDisable`. The UDR0004 warning on those subscriptions is locally
suppressed with that justification as of 2026-10-03; save behavior is unchanged.

### Background autosave (2026-10-02)

In the Editor and non-WebGL players, ordinary autosave captures the complete profile
and serializes its payload to an immutable JSON string on the main thread. A single
`Task.Run` worker then compresses/encodes it, validates previous generations and
writes the snapshot with the existing backup/replacement protocol. Paths and the
expected profile ID are captured before dispatch; the worker does not read live
gameplay objects or resolve `Application.persistentDataPath`.

Only one snapshot is in flight. Dispatch transfers the current dirty generation to
the pending task; subsequent gameplay events create a new dirty window. Successful
completion does not clear those newer changes. A failed worker marks the profile
dirty again and the normal Tick failure path delays retry by five seconds. While a
write is running, additional autosave dispatch waits, so the five-second scheduling
bound is subject to completion of that write. There is no growing queue of snapshots.

`SaveNow`, scene disposal, quit and profile creation/switching join the pending task
before writing any newer state synchronously. Public storage reads, writes and
deletions also join the worker before touching files, preventing reset/backup
operations from racing an older write. Storage public APIs remain main-thread APIs;
background validators must be pure and capture immutable values only.

Manifest updates still run on the main thread after successful snapshot completion;
failure there remains a warning, not a failed character-state commit. State capture,
payload JSON serialization, metadata writes and explicit flush boundaries can still
cost main-thread time. This change does not promise allocation reduction or eliminate
all save hitches. WebGL retains synchronous saving and its asynchronous browser flush.

The codec uses Unity's supported background-thread JSON APIs for plain envelopes and
backup DTOs ([JSON serialization](https://docs.unity3d.com/Manual/json-serialization.html)).
The on-disk format and document versions are unchanged. No test suite, Unity check,
build or new performance capture was run for this change, at the owner's request.

These are scheduling rules, not a guarantee against loss of unsaved progress on a
crash. A successful in-memory action may still be within the pending-save window.

## Encoding, writes and recovery

The codec serializes the payload with Unity `JsonUtility`, compresses with GZip,
applies fixed-key XOR obfuscation and stores Base64 data in a JSON envelope. The
envelope includes type, version, transaction ID, timestamp and payload SHA-256.
This is obfuscation and corruption detection, not authenticated encryption.

Storage prepares a `.tmp` file before touching the primary document. Valid previous
generations rotate to `.bak1` and `.bak2`; a corrupt primary is not promoted over
a good recovery copy. Replacement uses `File.Replace` where supported, otherwise
delete/move after preparing the file and backups. Do not promise identical atomic
filesystem guarantees on every platform.

Read order is primary, `.bak1`, then `.bak2`, with decode and caller validation
for each candidate. It is not a timestamp search across all saves. Recovery logs
a warning when a backup is selected.

If snapshot files exist but none are readable, loading throws and saving remains
blocked. It does not silently import older subsystem files or overwrite the profile
with defaults. Unreadable existing profile index/manifest documents also raise
errors rather than silently replacing the user's profile.

## Load and legacy import

The coordinator first attempts the snapshot and its backups. Only when none exist
does it load legacy subsystem documents, validating their readability before
applying them. Missing legacy sections use defaults; unreadable existing sections
are an error. Successful import is saved as a new snapshot, leaving legacy files
available on disk.

Live state is applied in order: player/wallet, location/shop progress, tree,
inventory, then tutorials. Tutorial presentation is suspended during this work.
Tree loading also cancels transient placement. Failed application blocks saving;
it is not a general rollback mechanism for every already-applied live component.

## Reset protocol

`SaveProfileManager.ClearProfileSaveData` first commits a snapshot with the profile
ID and `resetRequested = true`. It then deletes snapshot backups and legacy
documents. On the next load the coordinator applies game defaults and writes a
normal snapshot.

Committing the marker before cleanup prevents an interrupted reset from importing
leftover legacy progress. Preserve this ordering; deleting arbitrary save files
is not equivalent to invoking the reset protocol. Reset keeps profile identity
and is distinct from deleting a profile or clearing global settings.

## IDs and version changes

- Tree state uses stable node IDs and legacy aliases; see [tree persistence](SkillTree.md).
- Item records use definition IDs; gem records additionally carry instance identity.
- Definition catalogs load built-in `Resources/Items` definitions and scan loaded
  definitions. An arbitrary asset existing in the project is not enough to make it
  resolvable in a player build.
- Catalog duplicate IDs keep the first encountered entry rather than reporting an
  error. Maintain uniqueness; do not rely on scan order.
- Unresolved definitions log warnings in the coordinator and return null. Inventory
  restore skips unresolved entries; bridge restore also validates its endpoints and
  gem. A structurally valid snapshot is not proof that all content restored.
- Current coordinator document versions are `1`, with empty registered migration
  lists. The generic pipeline rejects future versions and missing migration steps.
  Increasing a version requires an actual compatible migration strategy.

Do not regenerate IDs as routine cleanup or silently resolve ambiguous references.

## WebGL boundary

`WebGLPersistentStorageSync` invokes the project's JavaScript plugin after storage
operations. The plugin calls `FS.syncfs(false, callback)`, coalescing further flush
requests while a sync is running. Browser persistence is asynchronous; completion
of a C# file operation is not confirmation that the browser persisted the data.
Forced termination can lose the last pending flush. Editor-only checks cannot
establish IndexedDB durability.

## Verification when changing saves

Run the existing isolated harness from the repository root on Windows with .NET 9:

```powershell
& Tests/SaveRegression/Run.ps1
```

It writes disposable data under a unique `Temp/SaveRegression-*` directory and uses
production save code with Unity/gameplay stubs. Read `Tests/SaveRegression/README.md`
for coverage. It does not verify actual Unity lifecycle, real inventory UI or
browser persistence.

For relevant changes, additionally test equip/extract and reload, multiple profiles,
reset then reload, missing definition handling, and browser reload after a completed
flush. Use disposable test profiles for destructive reset/recovery scenarios.
Checks listed here were not executed during this documentation pass.

## Source entry points

Performance follow-up, 2026-10-03: [allocationTest2](../Reference/PerformanceCaptureAllocationTest.md#allocation-pressure-and-remaining-save-hitches)
still shows paired main-thread save spikes of about 4.6–7.0 ms and 11.7–17.1 ms.
The background implementation retains main-thread capture/JSON serialization and
synchronous manifest persistence after completion. The capture does not separately
time those stages; BL-034 remains open. No persistence behavior was changed or tested.

Repository-relative paths, outside this vault:

- `Assets/Scripts/SaveSystem/GameSaveCoordinator.cs`
- `Assets/Scripts/SaveSystem/SaveProfileManager.cs`
- `Assets/Scripts/SaveSystem/SaveDataModels.cs`
- `Assets/Scripts/SaveSystem/SavePaths.cs`
- `Assets/Scripts/SaveSystem/SaveFileCodec.cs`
- `Assets/Scripts/SaveSystem/SaveFileStorage.cs`
- `Assets/Scripts/SaveSystem/SaveMigrationPipeline.cs`
- `Assets/Scripts/SaveSystem/ItemDefinitionCatalog.cs`
- `Assets/Scripts/SaveSystem/SaveSettingsServices.cs`
- `Assets/Scripts/SaveSystem/WebGLPersistentStorageSync.cs`
- `Assets/Plugins/WebGL/SkillTreePersistentStorage.jslib`
