// Unity CLI eval source, intentionally outside Assets. No assembly/domain reload.
// Fixed-level build presets in an isolated preview scene; no authored-state writes.
if (UnityEditor.EditorApplication.isPlaying)
    throw new System.InvalidOperationException("Stop Play Mode first. The simulator never changes the running game.");
var options = (System.Collections.Generic.Dictionary<string,object>)System.AppDomain.CurrentDomain.GetData("balanceSimulation.config");
// Consume the request even if later validation fails.
System.AppDomain.CurrentDomain.SetData("balanceSimulation.config", null);
var scenario = options != null && options.ContainsKey("scenario")
    ? (System.Collections.Generic.Dictionary<string,object>)options["scenario"]
    : new System.Collections.Generic.Dictionary<string,object>();
int IntOption(System.Collections.Generic.Dictionary<string,object> values, string key, int fallback) =>
    values.ContainsKey(key) ? System.Convert.ToInt32(values[key]) : fallback;
string StringOption(System.Collections.Generic.Dictionary<string,object> values, string key, string fallback) =>
    values.ContainsKey(key) ? (string)values[key] : fallback;
int repetitions = options != null ? IntOption(options, "runsPerBuild", 10) : 10;
int firstSeed = options != null ? IntOption(options, "seed", 101) : 101;
int playerLevel = IntOption(scenario, "playerLevel", 1);
int stage = IntOption(scenario, "stage", 1);
int waveNumber = IntOption(scenario, "waveNumber", 1);
int maxSeconds = IntOption(scenario, "maxSeconds", 180);
string scenarioName = StringOption(scenario, "name", "default");
string locationId = StringOption(scenario, "locationId", "level-1");
var sourcePlayer = UnityEngine.Object.FindAnyObjectByType<Battle.PlayerUnit>(UnityEngine.FindObjectsInactive.Include);
var sourceSpawner = UnityEngine.Object.FindAnyObjectByType<Battle.EnemySpawner>(UnityEngine.FindObjectsInactive.Include);
var sourcePool = UnityEngine.Object.FindAnyObjectByType<Battle.EnemyPool>(UnityEngine.FindObjectsInactive.Include);
if (sourcePlayer == null || sourceSpawner == null || sourcePool == null)
    throw new System.InvalidOperationException("Open MainScene in Edit Mode first.");
var privateFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
object GetField(object owner, System.Type type, string name) => type.GetField(name, privateFlags).GetValue(owner);
void SetField(object owner, System.Type type, string name, object value) => type.GetField(name, privateFlags).SetValue(owner, value);
var catalog = (Battle.LocationCatalog)GetField(sourceSpawner, typeof(Battle.EnemySpawner), "locationCatalog");
var locations = (System.Collections.Generic.List<Battle.LocationDefinition>)GetField(catalog, typeof(Battle.LocationCatalog), "locations");
var sourceNodes = sourcePlayer.SkillTree.GetComponentsInChildren<SkillTree.Node>(true);
var limitedZones = sourcePlayer.SkillTree.GetComponentsInChildren<SkillTree.LimitedZone>(true);
var bonusZones = sourcePlayer.SkillTree.GetComponentsInChildren<SkillTree.BonusZone>(true);
var nodeLookup = sourceNodes.ToDictionary(n => n.SaveId);
SkillTree.ModifierContainer ReportContainer(SkillTree.Modifier m) => m is SkillTree.BaseModifier b ? b.modifierContainer :
    m is SkillTree.ModifierPerPlayerLevel ? (SkillTree.ModifierContainer)GetField(m, typeof(SkillTree.ModifierPerPlayerLevel), "modifierContainer") : null;
bool SupportedContainer(SkillTree.ModifierContainer container) => container != null &&
    !container.statType.ToString().Contains("Wisp");
bool NumericModifier(SkillTree.Modifier modifier) {
    if (modifier == null) return false;
    if (modifier.GetType() == typeof(SkillTree.BaseModifier))
        return SupportedContainer(((SkillTree.BaseModifier)modifier).modifierContainer);
    if (modifier.GetType() == typeof(SkillTree.ModifierPerPlayerLevel))
        return SupportedContainer((SkillTree.ModifierContainer)GetField(modifier, typeof(SkillTree.ModifierPerPlayerLevel), "modifierContainer"));
    return false;
}
string UnsupportedReason(SkillTree.Node node) {
    if (node.IsInfinite || node is SkillTree.SocketNode) return "Infinite nodes and sockets are not supported yet.";
    if (node.PermanentPower != 0f || node.RuntimePower != 0f) return "Node power is not supported yet.";
    if ((bool)GetField(node, typeof(SkillTree.Node), "startsLocked")) return "Starts locked; unlock items are not simulated.";
    if (node.Modifiers == null || node.Modifiers.Any(m => !NumericModifier(m))) return "Requires exact numeric modifier types, valid containers and no wisp stats.";
    if (node.AdditionalAllocatedCondition != null || node.AdditionalActivationCondition != null) return "Unexpected live allocation/activation predicates in Edit Mode.";
    return null;
}
if (options != null && StringOption(options, "mode", "battle") == "catalog") {
    var catalogResponse = new { schemaVersion = 1,
        nodes = sourceNodes.Select(n => new {
            id = n.SaveId, name = n.name, type = n.GetType().Name, isRoot = n is SkillTree.RootNode, root = n is SkillTree.RootNode,
            x = sourcePlayer.SkillTree.transform.InverseTransformPoint(n.transform.position).x,
            y = sourcePlayer.SkillTree.transform.InverseTransformPoint(n.transform.position).y,
            cost = n is SkillTree.RootNode ? 0 : (int)GetField(n, typeof(SkillTree.Node), "nodeCost"),
            supported = UnsupportedReason(n) == null, unsupportedReason = UnsupportedReason(n),
            connectedIds = n.ConnectedNodes.Where(x => x != null).Select(x => x.SaveId).ToArray(),
            modifiers = n.Modifiers == null ? new object[0] : n.Modifiers.Select(m => (object)new {
                name = m != null ? m.name : "missing", type = m != null ? m.GetType().Name : "missing",
                serialized = m != null ? UnityEngine.JsonUtility.ToJson(m) : null,
                stat = ReportContainer(m)?.statType.ToString(), operation = ReportContainer(m)?.modifierType.ToString(), value = ReportContainer(m)?.value ?? 0f,
                levelsPerModifier = m is SkillTree.ModifierPerPlayerLevel ? (int)GetField(m, typeof(SkillTree.ModifierPerPlayerLevel), "levelsPerModifier") : 0 }).ToArray()
        }).ToArray(),
        limitedZones = limitedZones.Select(z => new { name = z.name, playerLevelDependent = z.UsesPlayerLevelLimit,
            serialized = UnityEngine.JsonUtility.ToJson(z),
            nodeIds = ((System.Collections.Generic.List<SkillTree.Node>)GetField(z, typeof(SkillTree.LimitedZone), "nodes")).Where(n => n != null).Select(n => n.SaveId).ToArray() }).ToArray(),
        locations = locations.Where(l => l != null && l.IsBattle && l.EnemyDatabase != null).Select(l => new {
            id = l.LocationId, minStage = l.EnemyDatabase.StartingLevel, maxStage = l.EnemyDatabase.MaxWaveLevel,
            wavesPerStage = l.EnemyDatabase.WavesToUnlockNextLevel }).ToArray() };
    return options.ContainsKey("exportPath") ? WriteEditorExport(catalogResponse, (string)options["exportPath"]) : (object)catalogResponse;
}
if (repetitions < 1 || repetitions > 1000 || playerLevel < 1 || playerLevel > 1000 || stage < 1 ||
    waveNumber < 1 || maxSeconds < 1 || maxSeconds > 3600)
    throw new System.ArgumentOutOfRangeException("Invalid simulation configuration");
var location = locations.FirstOrDefault(x => x != null && x.LocationId == locationId);
if (location == null || !location.IsBattle || location.EnemyDatabase == null)
    throw new System.InvalidOperationException("Unknown battle location: " + locationId);
var database = location.EnemyDatabase;
if (stage < database.StartingLevel || stage > database.MaxWaveLevel)
    throw new System.ArgumentOutOfRangeException("stage", "Stage is outside this location.");
if (waveNumber > database.WavesToUnlockNextLevel)
    throw new System.ArgumentOutOfRangeException("waveNumber", "Wave is outside this stage.");
var sourceEnemy = (Battle.Unit)GetField(sourcePool, typeof(Battle.EnemyPool), "enemyPrefab");
if (options == null || !options.ContainsKey("builds")) throw new System.InvalidOperationException("Build presets are required; use Run.ps1.");
var definitions = ((object[])options["builds"]).Select(value => {
    var build = (System.Collections.Generic.Dictionary<string,object>)value;
    string name = (string)build["name"];
    var ids = ((object[])build["nodeIds"]).Cast<string>().ToArray();
    if (ids.Length == 0 || ids.Distinct().Count() != ids.Length) throw new System.InvalidOperationException("Empty or duplicate node IDs: " + name);
    var chosen = ids.Select(id => nodeLookup.ContainsKey(id) ? nodeLookup[id] : throw new System.InvalidOperationException("Unknown node ID: " + id)).ToArray();
    if (chosen.Any(n => n is SkillTree.RootNode)) throw new System.InvalidOperationException("Roots are included automatically; omit them from nodeIds.");
    var all = sourceNodes.Where(n => n is SkillTree.RootNode).Concat(chosen).ToArray();
    foreach (var node in all) {
        string reason = UnsupportedReason(node);
        if (reason != null) throw new System.NotSupportedException(name + ": " + node.SaveId + ": " + reason);
    }
    int points = chosen.Sum(n => (int)GetField(n, typeof(SkillTree.Node), "nodeCost"));
    if (chosen.Any(n => (int)GetField(n, typeof(SkillTree.Node), "nodeCost") < 1))
        throw new System.NotSupportedException(name + ": nonpositive node costs are unsupported.");
    int budget = IntOption(build, "pointBudget", -1);
    if (budget < 0 || points > budget) throw new System.InvalidOperationException(name + ": point cost exceeds pointBudget (" + points + "/" + budget + ").");
    var included = new System.Collections.Generic.HashSet<SkillTree.Node>(all);
    // Preserve authored directed traversal: a selected candidate can reach a root through selected active nodes.
    foreach (var node in chosen) {
        var visited = new System.Collections.Generic.HashSet<SkillTree.Node>();
        var stack = new System.Collections.Generic.Stack<SkillTree.Node>(); stack.Push(node);
        bool connected = false;
        while (stack.Count > 0) {
            var current = stack.Pop(); if (!visited.Add(current)) continue;
            if (current is SkillTree.RootNode) { connected = true; break; }
            foreach (var next in current.ConnectedNodes) if (next != null && included.Contains(next)) stack.Push(next);
        }
        if (!connected) throw new System.InvalidOperationException(name + ": missing path to root for " + node.SaveId);
    }
    foreach (var zone in limitedZones) {
        var zoneNodes = (System.Collections.Generic.List<SkillTree.Node>)GetField(zone, typeof(SkillTree.LimitedZone), "nodes");
        int count = zoneNodes.Count(n => n != null && included.Contains(n));
        int limit = zone.MaxAllocatedNode;
        if (zone.UsesPlayerLevelLimit) {
            limit = 0;
            var rules = (System.Collections.IEnumerable)GetField(zone, typeof(SkillTree.LimitedZone), "playerLevelLimitRules");
            if (rules != null) foreach (var rule in rules) {
                if (rule == null) continue;
                int required = (int)rule.GetType().GetProperty("RequiredPlayerLevel").GetValue(rule);
                if (playerLevel >= required) limit = System.Math.Max(limit, (int)rule.GetType().GetProperty("MaxAllocatedNode").GetValue(rule));
            }
        }
        if (count > limit) throw new System.InvalidOperationException(name + ": active-node limit exceeded in " + zone.name + " (" + count + "/" + limit + ").");
    }
    foreach (var zone in bonusZones) {
        var zoneNodes = (System.Collections.Generic.List<SkillTree.Node>)GetField(zone, typeof(SkillTree.BonusZone), "nodes");
        if (zoneNodes.Any(n => n != null && included.Contains(n)) &&
            !SupportedContainer((SkillTree.ModifierContainer)GetField(zone, typeof(SkillTree.BonusZone), "modContainer")))
            throw new System.NotSupportedException(name + ": unsupported bonus-zone container: " + zone.name);
    }
    return new { name, nodes = all, nodeIds = ids, points, pointBudget = budget };
}).ToArray();
const float step = 1f / 60f;
int maxTicks = maxSeconds * 60;
var savedRandomState = UnityEngine.Random.state;
var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
bool sceneWasDirty = activeScene.isDirty;
var preview = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
var temporaryDefinitions = new System.Collections.Generic.List<UnityEngine.Object>();
var roots = new System.Collections.Generic.List<UnityEngine.GameObject>();
var rows = new System.Collections.Generic.List<object>();
var summary = new System.Collections.Generic.List<object>();
long totalTicks = 0;
double simulatedSeconds = 0;
var stopwatch = System.Diagnostics.Stopwatch.StartNew();

Battle.Unit CreateActor(Battle.Unit source, SkillTree.BaseInnateModifiers innate, SkillTree.Node[] buildNodes) {
    var root = new UnityEngine.GameObject("BalanceSimulationActor");
    root.SetActive(false);
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, preview);
    root.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
    roots.Add(root);
    var health = root.AddComponent<Battle.Health>();
    var barrier = root.AddComponent<Battle.Barrier>();
    var mystic = root.AddComponent<Battle.MysticHealth>();
    var effects = root.AddComponent<Battle.EffectController>();
    OwnEditorEffects(effects);
    var attacker = root.AddComponent<Battle.Attacker>();
    var attributes = root.AddComponent<Battle.Attributes>();
    UnityEngine.JsonUtility.FromJsonOverwrite(UnityEngine.JsonUtility.ToJson(source.attributes), attributes);
    Battle.Unit actor;
    if (buildNodes != null) {
        var tree = root.AddComponent<SkillTree.MainSkillTree>();
        var clonedNodes = new System.Collections.Generic.Dictionary<SkillTree.Node, SkillTree.Node>();
        foreach (var original in buildNodes) {
            var copy = root.AddComponent<SkillTree.Node>();
            SetField(copy, typeof(SkillTree.Node), "<Modifiers>k__BackingField", original.Modifiers);
            SetField(copy, typeof(SkillTree.Node), "<IsAllocated>k__BackingField", true);
            SetField(copy, typeof(SkillTree.Node), "<IsActive>k__BackingField", true);
            clonedNodes.Add(original, copy);
        }
        var clonedZones = new System.Collections.Generic.List<SkillTree.BonusZone>();
        foreach (var original in bonusZones) {
            var zoneNodes = (System.Collections.Generic.List<SkillTree.Node>)GetField(original, typeof(SkillTree.BonusZone), "nodes");
            var relevant = zoneNodes.Where(n => n != null && clonedNodes.ContainsKey(n)).Select(n => clonedNodes[n]).ToList();
            if (relevant.Count == 0) continue;
            var copy = root.AddComponent<SkillTree.BonusZone>();
            SetField(copy, typeof(SkillTree.BonusZone), "nodes", relevant);
            var container = (SkillTree.ModifierContainer)GetField(original, typeof(SkillTree.BonusZone), "modContainer");
            SetField(copy, typeof(SkillTree.BonusZone), "modContainer", container == null ? null :
                new SkillTree.ModifierContainer(container.modifierType, container.statType, container.value));
            clonedZones.Add(copy);
        }
        SetField(tree, typeof(SkillTree.MainSkillTree), "bonusZones", clonedZones);
        SetField(tree, typeof(SkillTree.MainSkillTree), "_allocatedNodes", clonedNodes.Values.ToList());
        var level = root.AddComponent<Battle.UnitLevel>();
        level.ApplySaveData(new SaveSystem.PlayerSaveData { level = playerLevel, skillPoints = 0, currentExp = 0 });
        actor = root.AddComponent<Battle.PlayerUnit>();
        SetField(actor, typeof(Battle.PlayerUnit), "<SkillTree>k__BackingField", tree);
        SetField(actor, typeof(Battle.PlayerUnit), "<UnitLevel>k__BackingField", level);
    } else actor = root.AddComponent<Battle.Unit>();
    actor.health = health; actor.barrier = barrier; actor.mysticHealth = mystic;
    actor.effectController = effects; actor.attacker = attacker; actor.attributes = attributes;
    SetField(actor, typeof(Battle.Unit), "baseInnateModifiers", GetField(source, typeof(Battle.Unit), "baseInnateModifiers"));
    SetField(actor, typeof(Battle.Unit), "innateModifiers", innate ?? GetField(source, typeof(Battle.Unit), "innateModifiers"));
    actor.SetWeaponType(source.WeaponType);
    // Edit Mode does not run ordinary MonoBehaviour Awake. Wire only the combat actor.
    typeof(Battle.Unit).GetMethod("Awake", privateFlags).Invoke(actor, null);
    root.SetActive(true);
    actor.RequestModRecalculation();
    actor.ResetCombatState();
    return actor;
}

void DestroyActors() {
    foreach (var root in roots) {
        if (root == null) continue;
        foreach (var actor in root.GetComponents<Battle.Unit>()) {
            actor.effectController.ClearAllEffects();
            typeof(Battle.Unit).GetMethod("UnbindModifierRuntimes", privateFlags).Invoke(actor, null);
        }
        // BonusZone.OnDestroy uses deferred Destroy; dispose its runtime asset explicitly in Edit Mode.
        foreach (var zone in root.GetComponents<SkillTree.BonusZone>()) {
            var runtime = (UnityEngine.Object)GetField(zone, typeof(SkillTree.BonusZone), "_runtimeModifier");
            SetField(zone, typeof(SkillTree.BonusZone), "_runtimeModifier", null);
            if (runtime != null) UnityEngine.Object.DestroyImmediate(runtime);
        }
        UnityEngine.Object.DestroyImmediate(root);
    }
    roots.Clear();
    foreach (var definition in temporaryDefinitions)
        if (definition != null) UnityEngine.Object.DestroyImmediate(definition);
    temporaryDefinitions.Clear();
}

try {
    for (int buildIndex = 0; buildIndex < definitions.Length; buildIndex++) {
        int wins = 0, deaths = 0, timeouts = 0;
        double secondsSum = 0, hpSum = 0, attacksSum = 0;
        for (int repetition = 0; repetition < repetitions; repetition++) {
            int seed = unchecked(firstSeed + repetition);
            UnityEngine.Random.InitState(seed);
            var context = new Battle.WaveContext(stage, waveNumber, database.WavesToUnlockNextLevel);
            if (database.BossBalance != null && database.BossBalance.TryGetRule(context, out var bossRule))
                context = new Battle.WaveContext(stage, waveNumber, database.WavesToUnlockNextLevel, true,
                    bossRule.BossCount, bossRule.TotalEnemiesInWave, bossRule.MaxBossAffixes);
            var packages = new Battle.WaveFactory(new Battle.EnemyFactory(database), database).CreateWave(context);
            if (packages.Count == 0) throw new System.InvalidOperationException("Wave generated no enemies.");
            foreach (var package in packages) temporaryDefinitions.Add(package.Modifiers);
            var player = CreateActor(sourcePlayer, null, definitions[buildIndex].nodes);
            var initialStats = System.Enum.GetValues(typeof(StatType)).Cast<StatType>().Where(s => s != StatType.Empty)
                .ToDictionary(s => s.ToString(), s => player.BaseUnitModifiers.GetStatValue(s));
            var enemies = packages.Select(package => {
                var enemy = CreateActor(sourceEnemy, package.Modifiers, null);
                enemy.SetWeaponType(package.Definition != null ? package.Definition.WeaponType : Battle.WeaponType.Sword);
                enemy.attacker.SetTarget(player);
                return enemy;
            }).ToArray();
            var resolverRoot = new UnityEngine.GameObject("BalanceSimulationTargets");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(resolverRoot, preview);
            resolverRoot.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
            roots.Add(resolverRoot);
            var resolver = resolverRoot.AddComponent<Battle.AttackResolver>();
            resolver.SetNewEnemies(enemies.ToList());
            player.attacker.SetTarget(resolver);
            int attacks = 0, misses = 0;
            double attackHpLoss = 0;
            float minimumHp = player.health.CurrentHealth;
            player.OnAttack += target => attacks++;
            player.OnMiss += target => misses++;
            player.OnHealthDamageTaken += (damage, amount) => attackHpLoss += amount;
            player.health.OnHealthChanged += () => minimumHp = UnityEngine.Mathf.Min(minimumHp, player.health.CurrentHealth);
            var actors = new[] { player }.Concat(enemies).ToArray();
            // Separate wave generation RNG from battle RNG. No visuals consume either.
            UnityEngine.Random.InitState(unchecked(seed ^ 0x5A17));
            int ticks = 0;
            for (; ticks < maxTicks && player.gameObject.activeSelf && enemies.Any(e => e.gameObject.activeSelf); ticks++) {
                for (int phase = 0; phase < 4; phase++)
                    foreach (var actor in actors)
                        if (actor.isActiveAndEnabled) actor.CombatTick(step, (Battle.CombatTickPhase)phase);
            }
            bool dead = !player.gameObject.activeSelf;
            bool won = !dead && enemies.All(e => !e.gameObject.activeSelf);
            string outcome = won ? "won" : dead ? "dead" : "timeout";
            if (won) wins++; else if (dead) deaths++; else timeouts++;
            double duration = ticks * (double)step;
            totalTicks += ticks; simulatedSeconds += duration;
            secondsSum += duration; hpSum += attackHpLoss; attacksSum += attacks;
            rows.Add(new { scenario = scenarioName, build = definitions[buildIndex].name, points = definitions[buildIndex].points,
                seed, outcome, ticks, simulatedSeconds = duration, initialStats,
                attacks, misses, minimumHp, maximumHp = player.health.MaxHealth, attackHpLoss,
                minimumHpFraction = minimumHp / player.health.MaxHealth,
                remainingHpFraction = player.health.CurrentHealth / player.health.MaxHealth,
                playerRemainingHp = player.health.CurrentHealth,
                enemyInitialHp = enemies.Select(e => e.health.MaxHealth).ToArray() });
            DestroyActors();
        }
        summary.Add(new { build = definitions[buildIndex].name, runs = repetitions, wins, deaths, timeouts,
            winRate = wins / (double)repetitions, meanSimulatedSeconds = secondsSum / repetitions,
            meanAttackHpLoss = hpSum / repetitions, meanAttacks = attacksSum / repetitions });
    }
} finally {
    DestroyActors();
    UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);
    UnityEngine.Random.state = savedRandomState;
    System.AppDomain.CurrentDomain.SetData("balanceSimulation.config", null);
}
stopwatch.Stop();
var battleResponse = new { schemaVersion = 2, engine = "production Unity combat / isolated preview scene", locationId, stage, playerLevel, waveNumber, scenarioName,
    fixedLevel = true, experienceAwards = false, tickSeconds = step, totalTicks, battles = rows.Count,
    simulatedSeconds, elapsedSeconds = stopwatch.Elapsed.TotalSeconds,
    speedup = simulatedSeconds / System.Math.Max(0.000001, stopwatch.Elapsed.TotalSeconds),
    sceneDirtyBefore = sceneWasDirty, sceneDirtyAfter = activeScene.isDirty, summary, battlesDetail = rows };
return options != null && options.ContainsKey("exportPath") ? WriteEditorExport(battleResponse, (string)options["exportPath"]) : (object)battleResponse;
