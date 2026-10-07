# Unity Project Development Guide for AI Agents

## Tests and verification scope

Do not write tests unless the user explicitly asks for them. Do not perform
excessive checks on your own initiative: keep verification limited to the minimum
needed for the requested change. Do not expand it into additional test suites,
builds, audits, or repeated checks unless the user explicitly requests them.
This rule takes precedence over broader verification guidance in this file and
referenced project guides.

## Tooltip authoring

Whenever creating or updating a `TooltipDescriptionData` asset, also update
`Assets/Scripts/TooltipSystem/TooltipTerms.asset` (the `TooltipTermDatabase`):
register new terms and keep existing term IDs and description references correct.
Verify that links in tooltip text resolve to the registered description asset.
Creating a tooltip includes its database registration; do not leave this as a
manual step when the user has requested the tooltip.

## Project documentation

Use [Docs/Home.md](Docs/Home.md) as the documentation entry point and
[Docs/ProjectMap.md](Docs/ProjectMap.md) to locate an unfamiliar subsystem.
Read only pages relevant to the task, not the entire vault before every edit.

Consult [Docs/Backlog.md](Docs/Backlog.md) for previously recorded issues and risks
in the area being changed. Recheck evidence before treating a risk as a confirmed
bug; update the matching entry and system page when investigated or fixed.

- For node allocation, queues, refunds, activation, power or tree persistence,
  read [Docs/Systems/SkillTree.md](Docs/Systems/SkillTree.md).
- For gems, sockets, influence or bridges, read
  [Docs/Systems/Gems.md](Docs/Systems/Gems.md) and follow its relevant references.
- Continue to use the content-authoring guides below for balance and content work.
  Runtime documentation does not override the owner's content requirements.
- For stat arithmetic, calculation phases, modifier power or runtime bindings,
  read [Docs/Systems/StatsAndModifiers.md](Docs/Systems/StatsAndModifiers.md).
- For combat ticks, attacks, damage receipt or effect lifecycle, read
  [Docs/Systems/CombatAndEffects.md](Docs/Systems/CombatAndEffects.md). For effect
  reactions, also read [Docs/Reference/EffectApplicationEvents.md](Docs/Reference/EffectApplicationEvents.md).
- For evasion, armor, resistance, block/parry, barriers, health or mystic absorption,
  read [Docs/Systems/DefencesAndResources.md](Docs/Systems/DefencesAndResources.md).
- For Bleed, Ignite, Chill/Freeze, Overcharge, Sunder, Distract, Expose or ailment
  absorption, read [Docs/Systems/AilmentsAndDebuffs.md](Docs/Systems/AilmentsAndDebuffs.md).
- For inventory slots, stacking, selection or item use, read
  [Docs/Systems/InventoryAndItems.md](Docs/Systems/InventoryAndItems.md).
- For persistence, profiles, recovery, reset, migrations or saved definition IDs,
  read [Docs/Systems/SavesAndProfiles.md](Docs/Systems/SavesAndProfiles.md).
- For location unlocks, stage progress, wave generation or boss completion,
  read [Docs/Systems/LocationsAndEnemies.md](Docs/Systems/LocationsAndEnemies.md).
- For wallet gold, shops, stock, drops or reward delivery, read
  [Docs/Systems/EconomyAndLoot.md](Docs/Systems/EconomyAndLoot.md).
- For menu graph actions, profile entry, screen modes or death/completion windows,
  read [Docs/Systems/MenusAndScreenFlow.md](Docs/Systems/MenusAndScreenFlow.md).
- For tutorial triggers, queues, pause/input ownership or saved progress, read
  [Docs/Systems/Tutorials.md](Docs/Systems/Tutorials.md). For authoring or wiring,
  also read [Docs/Tutorials/README.md](Docs/Tutorials/README.md).
- For localized strings, formatting, language selection or refresh behavior, read
  [Docs/Systems/Localization.md](Docs/Systems/Localization.md).
- For sound cues, source pooling, music, mixer volume parameters or volume nodes,
  read [Docs/Systems/Audio.md](Docs/Systems/Audio.md).
- For HUD values, tooltip ownership, Alt pinning, nested terms or bar rendering,
  read [Docs/Systems/HudAndTooltips.md](Docs/Systems/HudAndTooltips.md) and its visual references.
- For mini-game activators, timing, unlocks, results or rewards, read
  [Docs/Systems/BattleMiniGames.md](Docs/Systems/BattleMiniGames.md).
- For Pain, Vengeance, Scar, momentum buffs or next-hit/next-attack effects, read
  [Docs/Systems/ReactiveCombatEffects.md](Docs/Systems/ReactiveCombatEffects.md).
- For editor tools, regression runners, builds or deployment entry points, read
  [Docs/Reference/EditorAndBuildTools.md](Docs/Reference/EditorAndBuildTools.md).
- Check the implementation when a documented behavior matters to a change.
  Report discrepancies; do not silently turn existing bugs into design rules.
- Update affected documentation when a task changes behavior, ownership, save
  contracts or verification steps. Write new and revised documentation in English.
- Keep relevant documentation up to date as work progresses, including when
  investigation reveals that an existing description is outdated or incomplete.
- When you encounter undocumented behavior, rules, dependencies, or workflows
  that clearly need documentation, document them as part of the current work.
  Base descriptions on inspected evidence, mark uncertainty explicitly, and keep
  this effort focused on what you encounter rather than starting an unrelated audit.
- Use relative Markdown links, maintain the Home/project-map navigation, and group
  pages by broad purpose rather than creating one directory per mechanic.
- Preserve ongoing user edits and stable document paths. Translate older references
  incrementally when reviewing their area; preserve numeric values and rule meaning.
- Distinguish inspected source, historical checks and tests actually run now.
  Documentation-only work requires link/source checks, not a Unity build.

An explicitly requested project survey or documentation pass permits the necessary
read-only exploration beyond the routine 3–4-operation guideline below. Keep each
pass bounded to the agreed systems and mark remaining coverage honestly.

## Location and enemy content tasks

For creating, extending, balancing, or reviewing locations, enemy pools, bosses,
enemy affixes, or level power, first read
[Docs/Locations/LocationCreationGuide.md](Docs/Locations/LocationCreationGuide.md).
It records the owner's content rules, Level7 affix-roll baseline, branching
progression semantics, and intentional visual references. Run the read-only
`Tools/Locations/check_locations.py` checks described there after content edits.
The checker was absent on 2026-09-28; locate or restore it before relying on those
checks, and report the limitation if unavailable. Historical tuning is consolidated
in [Docs/Locations/ContentHistory.md](Docs/Locations/ContentHistory.md); it is not
an approved balance specification or a current validation report.
Existing assets are examples, not exceptions to those rules. The current user's
explicit request takes precedence. Do not automatically rebalance existing content
when asked only to document or inspect it.

## Skill tree content tasks

For requests to fill, rebalance, or review skill-tree node content, read
[Docs/SkillTree/SkillTreeFillingGuide.md](Docs/SkillTree/SkillTreeFillingGuide.md)
before selecting stats or editing nodes. It contains the owner's archetype,
branch consistency, large-node budget, allowed modifier types, and small-node
value rules, including a snapshot of the balance spreadsheet. Apply it only to
relevant skill-tree content tasks; the current user's explicit request takes precedence.

## Game-design tasks: modifiers, nodes and gems

### Owner's design direction

The core combat is automatic: the player does not directly control the character.
The player's agency is building the skill tree and placing gems. Design new content
to improve that experience through meaningful build decisions, experimentation and
readable combat outcomes. Do not assume manual dodging, aiming, movement, ability
buttons or player-timed combos. Existing mini-games are a separate system, not a
default requirement for a new modifier.

When asked to invent mechanics, act as a game designer and propose mechanics;
do not ask the owner to supply the very idea they requested. A design request does
not authorize implementation or serialized-content edits. For implementation of
an unspecified mechanic, the clarification rule below still applies. Distinguish
current behavior, a proposed design and implementation work explicitly.

### Fast orientation and current system boundaries

Start with this section and the relevant linked pages, then inspect only the source
analogues needed for the proposal. Do not repeat a whole-project survey per idea.

- Read [tree authoring](Docs/SkillTree/SkillTreeFillingGuide.md) for node themes,
  allowed stats, branch consistency and budgets; [tree runtime](Docs/Systems/SkillTree.md)
  for allocation, activity and power; [gems](Docs/Systems/Gems.md) for placement.
- Read [stats](Docs/Systems/StatsAndModifiers.md) and
  [combat](Docs/Systems/CombatAndEffects.md) for combat modifiers. Follow only the
  relevant defence, ailment, reactive-effect or effect-event references.
- Consult relevant [backlog](Docs/Backlog.md) entries. Known risks are not intended
  design features or reliable foundations for a synergy; recheck relevant evidence.
- Nodes contribute when active, not merely allocated. Ordinary modifier power is
  scaled by `max(0, 1 + PermanentPower + RuntimePower)`. Infinite nodes instead scale
  supported scalar Added/Increased modifiers by invested points; special mechanics
  are not currently supported there.
- Current gem kinds are LocalModifiers, NodeInfluence and Bridge. Local modifiers
  and influence require an active socket; pending bridge endpoints are inactive.
  Influence uses shortest authored-link distance through ConnectedNodes, even
  through unallocated nodes. Overlapping power bonuses add; sockets and infinite
  nodes cannot receive power. Bridges change allocation connectivity, not influence
  distance. Allocation-dependent influence, conversion, negative influence or a
  combination of gem kinds must be identified as requiring new support.
- Numeric stats use Added sum times (1 + Increased sum) times the product of More
  factors. Do not treat these as interchangeable or assume every stat is capped.
- Attacks advance automatically with AttackSpeed. Attack-start, landed-hit,
  critical, block, actual HP-loss and attack-completed triggers are different.
  OnHit precedes damage; ailments are attempted before block. DoT bypasses the
  ordinary attack pipeline. Define the precise trigger instead of just "on damage".
- Useful inspected analogues: DamageTakenAsPain listens to actual attack HP loss;
  PainConsumedGrantsScar turns consumed Pain into temporary defence with reduced
  healing. Their code is in `Assets/Scripts/SkillTree/Modifiers/ReactMods`; effects
  are in `Assets/Scripts/Battle/Effects`. They illustrate linked resources and
  tradeoffs, not an approved numeric balance template. Inspect both producer and
  effect before reusing any other named mechanic.

This orientation was checked against documentation and selected combat, reactive
modifier and gem-influence source on 2026-10-03. It is not a complete content catalog,
scene-wiring audit or playtest; recheck code when a specific boundary matters.

### Require a change in build decisions

For each major node, special modifier or new gem, complete this sentence concretely:
"Because of this mechanic, the player will invest in ___, give up ___, and choose
___ instead of ___." Explain why those choices help the mechanic function. A new
name, animation or conditional damage bonus is insufficient if the optimal tree
and gem placement remain the same. Simple numeric small nodes remain valid support
and progression rewards under the filling guide; do not force a special mechanic
onto every small node.

Prefer one clear central rule with several compatible ways to support it:

- Change the relative value of existing stats, such as attack frequency versus
  per-hit strength, ailment variety versus specialization, or defence versus healing.
- Connect a resource generator to a spender, with a meaningful constraint on how
  the player builds around their rates, capacity or consumption timing.
- Offer conversion, thresholds or specialization with an opportunity cost that
  changes allocation priorities. Describe conversion order and double-scaling risks.
- Make socket choice, graph distance, competing influence targets or access through
  a bridge matter. Specify the graph and eligible node states for any new tree rule.
- Give alternative routes distinct roles and allow cross-archetype synergy where
  access supports it, while respecting the owner's archetype and node-budget rules.

These are design tools, not claims that every option already exists in code.
Do not add forced drawbacks solely to make a mechanic look deep: point costs,
socket competition, narrower coverage and lost alternatives can supply real costs.
Avoid universal best picks, arbitrary mandatory pairings and builds that do nothing
until several rare pieces are collected. Identify a usable entry state and how
later investment changes the build. Keep the main rule easy to explain and its
activation visible enough for the player to learn from automatic combat.

### Proposal format and design review

Scale detail to the request. For each developed candidate provide:

1. **Role and experience:** modifier/node/gem, intended archetype or hybrid, the
   build fantasy and the player decision it introduces.
2. **Exact rule:** owner and target, trigger, condition, reward, cost, duration,
   stacking/refresh/consumption and limits. Mark provisional numbers as tuning
   hypotheses; do not present untested values as balanced.
3. **Build consequences:** concrete supporting stats/nodes or gem placement,
   what becomes less attractive, and at least two meaningfully different support
   plans when the mechanic permits them. Compare with the build without this item.
   Use inspected existing synergies and distinguish proposed partners.
4. **Progression and tradeoff:** minimum useful setup, further investment, point
   and socket opportunity costs, and a situation or build where another option wins.
   For a gem, state its kind, placement rule and how moving it changes the outcome;
   for a cluster, explain alternative routes and the shared large-node budget.
5. **Readability:** a short player-facing rule and the signal that communicates
   activation, stacks or resource use. Identify new UI needs as proposed work.
6. **Balance risks and feasibility:** existing analogues, reuse versus new hooks,
   power scaling, multiple copies, extreme speed/chance, feedback loops and relevant
   short encounters versus bosses. Include miss/block/DoT/reset boundaries only
   where they affect this mechanic. Do not assume kills or taking HP damage are
   equally available in every encounter.

Before recommending a candidate, ask whether it changes an actual investment or
placement decision, has a credible alternative at comparable cost, works in
automatic combat, and produces an understandable outcome. Revise weak candidates;
do not hide a plain numerical upgrade behind a complicated condition. When asked
for several ideas, vary the decisions and mechanical roles rather than reskinning
the same trigger. Recommend the strongest fit with a reason and state uncertainty.

Design reasoning and scoped source inspection are enough for an ideation task.
Suggest a small relevant playtest criterion if helpful, but do not run simulations,
write tests, build Unity or edit gameplay merely to validate a proposal. Implementation
follows the workflow below; content filling follows the existing authoring guide.

## Modifier implementation tasks

Apply this workflow when adding or changing a modifier. Start from the user's
actual mechanic description; if it is missing or still a template placeholder,
ask for it rather than inventing gameplay. Follow existing project patterns:
inspect analogues before implementation and avoid new architecture unless needed.
Existing analogues are a reason to reuse the established pattern, not to ask
whether to start fresh.

### Scope and serialized content boundary

- Implement the code portion: new or changed `.cs` files, battle logic, runtime
  bindings, events/hooks, enum values and localization keys as needed.
- Create and configure the modifier's own ScriptableObject `.asset` and `.meta`
  files as part of modifier implementation, using the requested settings and
  existing serialization patterns. Verify the script GUID and serialized values,
  and report the exact asset path in the final response. This does not authorize
  assigning the asset to nodes or changing other gameplay configuration.
- Leave node links, other gameplay/config `.asset`, `.prefab`, `.unity`,
  icon mappings, tooltip database entries and similar serialized configuration
  as **Manual Unity Editor Steps**. Do not create or configure them yourself,
  including through editor scripts or automation, unless the user explicitly
  overrides this boundary.
- Localization string tables are the exception: they may be edited manually.
  Write only English localization text. Never invent `ru`/`de` translations;
  add empty entries only if ID synchronization requires them, otherwise list
  exact missing keys as translator TODOs. Preserve existing translations.

### 1. Analyze the mechanic and inspect analogues

Read [Stats and modifiers](Docs/Systems/StatsAndModifiers.md), the relevant
backlog entries, and only the other system pages needed for this mechanic.
Inspect similar modifiers, effects and mechanics in the source. Briefly explain
which analogues were selected, why they fit, and the resulting code location and
implementation pattern.

Establish what the modifier does, when it triggers and who it affects. Classify
it as passive, reactive, conditional, special and/or effect-related. Decide
explicitly whether it needs `CreateRuntimeBinding`, a separate `BaseEffect`,
buff or debuff, new events/hooks, enum values or changes to battle flow. Reuse
existing lifecycle and event contracts wherever possible.

### 2. Present a checklist before editing

Mark each item as required or not applicable, with intended paths/identifiers
where known:

- Modifier `.cs` location and any separate effect `.cs`.
- Mechanic, binding, event/hook or battle-flow changes.
- `EffectVisualType`, status-icon visibility and effect display name.
- Localization table name, modifier-description key and English text.
- Effect-display-name key and English text, if needed.
- English table entries and any required empty `ru`/`de` entries or translator TODOs.
- Manual Unity Editor asset/configuration steps.
- Minimum compilation and behavior verification appropriate to the change.

This checklist is an implementation update, not an approval gate. Proceed with
authorized work unless essential gameplay requirements are unresolved.

### 3. Implement and review behavior, localization and UI

Implement the code using the selected analogues. Check stack/refresh/replace
rules, duration and expiry, event timing, affected targets, runtime binding
cleanup and relevant edge cases. Ensure the English description matches actual
code, including conditions, values and formatting placeholders.

Verify the actual localization table name and exact keys, including an effect
display name if needed. Check that missing configuration does not unexpectedly
expose class names or other player-facing fallback text. Explain any remaining
fallback or missing-text behavior that depends on manual setup.

Determine whether the effect should appear in status icons, whether a new
`EffectVisualType` is necessary, and whether icon mapping, tooltip or display-name
configuration remains manual. State what the player will see before those steps
are completed. Do not imply the modifier is wired into gameplay when only its
code is implemented.

### 4. Provide an English icon-generation prompt

For each new modifier, write a ready-to-copy English prompt for another image
generation model. For an existing modifier being changed, provide or revise the
prompt to match its resulting mechanic. Describe a concrete visual symbol or
scene that communicates the mechanic, the focal subject, composition, palette,
lighting, background and style. Use available project icon references to guide
style; label a proposed style as a proposal if no reference was inspected.
Keep the silhouette clear and the composition readable at small skill-tree icon
sizes; avoid text, letters, numbers, logos and watermarks. Do not imply that an
image was generated, imported or mapped: this deliverable is the English prompt,
and asset setup remains a manual step unless separately requested.

### 5. Verify and report

For code changes, run the available scoped compilation/build check and report
the actual command or tool and result. Prefer compilation over a full player
build when sufficient. If unavailable, state the limitation and what remains
unverified. Separate unrelated warnings from errors caused by the change.
Respect the tests and verification scope above: do not write tests unless asked
or expand into extra suites, builds or audits. Documentation-only edits require
only relevant link/source checks, not Unity compilation.

The final response must include:

- What was implemented in code and the chosen analogues/pattern.
- Changed `.cs`, localization and documentation files.
- Exact localization table names, keys and English texts (including effect names
  when applicable); distinguish added entries from proposed/manual entries, and
  list any empty `ru`/`de` entries or translator TODOs. If none are needed, say so.
- **Icon Generation Prompt**: the complete English prompt.
- **Manual Unity Editor Steps**: concrete asset creation and wiring instructions,
  including modifier settings, node links, icons and tooltip entries as applicable.
- Verification actually performed, outstanding risks and remaining work.

## Project Context

You are working on a **Unity project** used to build games or interactive experiences. Unity is a cross-platform game engine that uses C# for scripting and organizes content into scenes, prefabs, and assets.

## Understanding Unity Projects

Unity projects are often **large, messy, and difficult to parse**:
- Thousands of files across Assets/, Library/, Packages/
- Abandoned prototypes and unused assets mixed with active code
- Auto-generated files, caches, and metadata everywhere
- No clear separation between "important" and "noise"

**As an AI agent, exhaustive exploration will fill your context with irrelevant information and slow you down.** Instead: get oriented quickly, ask when uncertain, and focus on what matters for the task.

## Project Structure

### Key Directories

#### `Assets/`
This is the **primary location** for project code, scripts, scenes, prefabs, materials, and other game content. 
Editor specific tools an extension will be in an `Editor` subfolder

**Always search here first** when exploring the codebase.

#### `Packages/`
Contains Unity packages that extend the project's functionality. Packages can be:
1. **Local packages**: Manually placed in the `Packages/` folder (editable)
2. **Embedded packages**: Custom packages with full source code in `Packages/`
3. **Registry packages**: Downloaded from Unity Registry or npm (cached elsewhere)

To understand package locations:
- Check `Packages/manifest.json` for package definitions
- Look for `file:` references for local/embedded packages with custom paths
- Local packages in `Packages/` are directly editable

#### `Library/PackageCache/`
Contains **cached, read-only** copies of registry packages downloaded from Unity Package Manager. This includes:
- Official Unity packages (e.g., `com.unity.textmeshpro`)
- Third-party packages from registries
- Git-based package dependencies

**Important**:
- These are cached copies - do not edit directly
- To modify a registry package, it must be embedded as a local package first
- When searching for package code, check `Packages/` first, then `Library/PackageCache/`

#### `Library/`
A cache and temporary data folder generated by Unity. **Generally ignore this folder** except for:
- `Library/PackageCache/` when searching for registry package source code
- The rest contains build artifacts, imported assets cache, and metadata

#### `ProjectSettings/`
Contains project configuration files (input, physics, quality settings, etc.). Rarely needs modification unless changing project-wide settings.

## Decision Making

### When to explore vs. when to ask

**For "build/create X" requests:**
1. If yes → Ask: "I see existing [X] code. Should I extend it or start fresh?"
2. If no → Proceed to implementation

**For "fix/modify X" requests:**
1. One targeted search to find the relevant code
2. If found → Read and fix it
3. If not found → Ask: "I couldn't find [X]. Can you point me to it?"

**General rule:** If you're doing more than 3-4 file operations before starting actual work, stop and ask the user for guidance instead. They know their project better than any amount of exploration will reveal.

### Avoid over-exploration

- Don't enumerate every folder looking for "relevant" files
- Don't read files "just in case" they might be useful
- The user can always provide more context if needed




## Unity Code Execution

---
When you need to **execute C# code in Unity** (creating objects, modifying scenes, adding components, changing materials, etc.), 

Unity C# code execution specialist. When the user wants to execute actions in Unity, modify the scene programmatically, create or manipulate GameObjects, or perform any Unity Editor operation that requires code execution. Use proactively for tasks like "create a cube", "add a component", "modify materials", etc.
---

You are a specialized Unity C# code generation and execution agent. Your can translate natural language requests into executable Unity C# code, validate it compiles correctly, and execute it in the Unity Editor.

## RunCommand Tool

Execute a C# script in the Unity Editor.

This is a powerful tool that allows you to programmatically control virtually every aspect of the game, including physics, input, graphics, gameplay logic, project setting and package management.

### The Golden Template
```csharp
using UnityEngine;
using UnityEditor;

internal class CommandScript : IRunCommand
{
    public void Execute(ExecutionResult result)
    {
        // 1. Your logic here
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

        // 2. Register changes for Undo/Redo and tracking
        result.RegisterObjectCreation(cube);

        // 3. Log the result
        result.Log("Created {0}", cube);
    }
}
```
### Rules for Success
1. **Class Name is Mandatory**: The class MUST be named `CommandScript`. Using any other name will cause a NullReferenceException or execution failure.
2. **Use `internal` Accessibility**: Always use `internal class CommandScript`. Using `public` will cause an "Inconsistent Accessibility" compilation error.
3. **Use the `result` Object**:
   - **Creation**: Use `result.RegisterObjectCreation(obj)` after creating objects.
   - **Modification**: Use `result.RegisterObjectModification(obj)` BEFORE changing properties.
   - **Deletion**: Use `result.DestroyObject(obj)` instead of `Object.DestroyImmediate`.
   - **Logging**:
     - `result.Log("Created {0}", obj)` - Log with object references using `{0}`, `{1}`, etc.
     - `result.LogWarning("Warning message")` - Log warnings
     - `result.LogError("Error message")` - Log errors
4. **Avoid Top-Level Statements**: Always wrap your code in the class structure above.

## Verifying Your Work

Unity work often has **no immediate feedback**. Code compiles, objects get created—but did it actually work? Is it positioned correctly? Is the color right?

**Always verify your work** unless you're mid-way through a multi-step task.

### Choose verification based on work type:
- **Object creation/modification** → Take a screenshot to confirm appearance matches request
- **Code execution or logic changes** → Check console for errors (`Unity.GetConsoleLogs`)
- **Both visual and code** → Check console first, then screenshot

For scene composition verification, use `Unity.SceneView.CaptureMultiAngleSceneView` (3D: multi-angle view) or `Unity.SceneView.Capture2DScene` (2D: region capture). 

### What to look for:
- Colors, positions, scales, and rotations match the request
- Objects exist and are visible in the scene
- No new console errors or warnings

### When verification reveals issues

**Fix simple issues immediately** without asking:
- Wrong color, position, scale, or rotation → Correct it
- Missing component or property → Add it
- Console error from your code → Fix it

**Ask before fixing** when:
- The fix requires significant additional work
- You need information you don't have (e.g., "which render pipeline are you using?")
- Multiple valid solutions exist and user preference matters

The goal of verification is a **completed task**, not a status report.

### Verification rhythm

Think of it like running lint after a coding session—not after every keystroke.

- **Simple request** ("create a blue sphere at 1,2,3") → Verify immediately after
- **Multi-step task** → Verify after each logical phase completes
- **Batch operations** (created 5 objects) → One screenshot at the end, not five
