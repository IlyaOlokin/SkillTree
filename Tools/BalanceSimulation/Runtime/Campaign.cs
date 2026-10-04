// Unity CLI eval snippet: progression checkpoints contain data only, never live actors.
if (UnityEditor.EditorApplication.isPlaying)
    throw new System.InvalidOperationException("Stop Play Mode before running an isolated campaign.");
var request = (System.Collections.Generic.Dictionary<string,object>)System.AppDomain.CurrentDomain.GetData("balanceSimulation.config");
System.AppDomain.CurrentDomain.SetData("balanceSimulation.config", null);
string checkpointKey = (string)request["checkpointKey"];
if ((string)request["operation"] == "discard") {
    System.AppDomain.CurrentDomain.SetData(checkpointKey, null);
    return new { discarded = true };
}
var privateFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
object Field(object owner, System.Type type, string name) => type.GetField(name, privateFlags).GetValue(owner);
void FieldSet(object owner, System.Type type, string name, object value) => type.GetField(name, privateFlags).SetValue(owner, value);
int Number(System.Collections.Generic.Dictionary<string,object> values, string key, int fallback) =>
    values.ContainsKey(key) ? System.Convert.ToInt32(values[key]) : fallback;
var sourcePlayer = UnityEngine.Object.FindAnyObjectByType<Battle.PlayerUnit>(UnityEngine.FindObjectsInactive.Include);
var sourceSpawner = UnityEngine.Object.FindAnyObjectByType<Battle.EnemySpawner>(UnityEngine.FindObjectsInactive.Include);
var sourcePool = UnityEngine.Object.FindAnyObjectByType<Battle.EnemyPool>(UnityEngine.FindObjectsInactive.Include);
if (sourcePlayer == null || sourceSpawner == null || sourcePool == null) throw new System.InvalidOperationException("Open MainScene in Edit Mode.");
var catalog = (Battle.LocationCatalog)Field(sourceSpawner, typeof(Battle.EnemySpawner), "locationCatalog");
var locations = catalog.Locations.Where(l => l != null).ToDictionary(l => l.LocationId);
var sourceEnemy = (Battle.Unit)Field(sourcePool, typeof(Battle.EnemyPool), "enemyPrefab");
int poolSize = (int)Field(sourcePool, typeof(Battle.EnemyPool), "poolSize");
var originals = sourcePlayer.SkillTree.GetComponentsInChildren<SkillTree.Node>(true);
var byId = originals.ToDictionary(n => n.SaveId);
var originalLimits = sourcePlayer.SkillTree.GetComponentsInChildren<SkillTree.LimitedZone>(true);
var originalBonuses = sourcePlayer.SkillTree.GetComponentsInChildren<SkillTree.BonusZone>(true);
SkillTree.ModifierContainer Container(SkillTree.Modifier m) {
    if (m != null && m.GetType() == typeof(SkillTree.BaseModifier)) return ((SkillTree.BaseModifier)m).modifierContainer;
    if (m != null && m.GetType() == typeof(SkillTree.ModifierPerPlayerLevel))
        return (SkillTree.ModifierContainer)Field(m, typeof(SkillTree.ModifierPerPlayerLevel), "modifierContainer");
    return null;
}
bool Numeric(SkillTree.Node n) => n != null && !n.IsInfinite && !(n is SkillTree.SocketNode) && n.RuntimePower == 0f &&
    n.Modifiers != null && n.Modifiers.All(m => Container(m) != null && !Container(m).statType.ToString().Contains("Wisp"));
var eligible = originals.Where(Numeric).ToArray();
var progressType = typeof(Battle.EnemySpawner).Assembly.GetType("Battle.EnemyLocationProgressService", true);
object ProgressCall(object progress, string name, params object[] args) =>
    progressType.GetMethods().Single(m => m.Name == name && m.GetParameters().Length == args.Length &&
        (args.Length == 0 || args[0] == null || m.GetParameters()[0].ParameterType.IsByRef || m.GetParameters()[0].ParameterType.IsInstanceOfType(args[0])))
        .Invoke(progress, args);
int ProgressInt(object progress, string name) => (int)progressType.GetProperty(name).GetValue(progress);
var checkpoint = (System.Collections.Generic.Dictionary<string,object>)System.AppDomain.CurrentDomain.GetData(checkpointKey);
if (checkpoint == null) {
    var config = (System.Collections.Generic.Dictionary<string,object>)request["config"];
    if (Number(config, "schemaVersion", 0) != 1) throw new System.InvalidOperationException("Campaign preset schemaVersion must be 1.");
    var route = ((object[])config["locationIds"]).Cast<string>().ToArray();
    if (route.Length < 2 || route.Distinct().Count() != route.Length || route.Any(id => !locations.ContainsKey(id) || !locations[id].IsBattle))
        throw new System.InvalidOperationException("Campaign route requires distinct known battle locations (at least two).");
    int runs = Number(request, "runs", 1), seed = Number(request, "seed", 101);
    int attempts = Number(config, "maxAttemptsPerStage", 3), waveSeconds = Number(config, "maxWaveSeconds", 180);
    if (Number(config, "adaptiveCandidates", 4) < 1 || Number(config, "adaptiveCandidates", 4) > 12 ||
        Number(config, "adaptiveRoundsPerStage", 3) < 1 || Number(config, "adaptiveRoundsPerStage", 3) > 20)
        throw new System.ArgumentOutOfRangeException("Adaptive search limits");
    if (runs < 1 || runs > 1000 || attempts < 1 || attempts > 20 || waveSeconds < 1 || waveSeconds > 3600 || poolSize < 1)
        throw new System.ArgumentOutOfRangeException("Campaign limits");
    var strategies = ((object[])config["strategies"]).Cast<System.Collections.Generic.Dictionary<string,object>>().ToArray();
    if (strategies.Length == 0 || strategies.Select(s => (string)s["name"]).Distinct().Count() != strategies.Length)
        throw new System.InvalidOperationException("Strategies require unique names.");
    var states = new System.Collections.Generic.List<System.Collections.Generic.Dictionary<string,object>>();
    foreach (var strategy in strategies) {
        string starter = (string)strategy["starterId"], style = (string)strategy["style"];
        if (!byId.ContainsKey(starter) || !Numeric(byId[starter]) || !new[] { "damage", "defence", "balanced" }.Contains(style))
            throw new System.InvalidOperationException("Invalid starter/style in " + strategy["name"]);
        for (int run = 0; run < runs; run++) states.Add(new System.Collections.Generic.Dictionary<string,object> {
            { "strategy", strategy }, { "seed", unchecked(seed + run) }, { "status", "running" }, { "reason", "" },
            { "locationIndex", 0 }, { "attempt", 0 }, { "deaths", 0 }, { "waves", 0 }, { "kills", 0 }, { "gold", 0 },
            { "player", new SaveSystem.PlayerSaveData { level = sourcePlayer.UnitLevel.Level, currentExp = sourcePlayer.UnitLevel.CurrentExp, skillPoints = sourcePlayer.UnitLevel.SkillPoints } },
            { "progress", null }, { "allocated", new System.Collections.Generic.List<string>() },
            { "unlocked", new System.Collections.Generic.List<string>() }, { "powers", new System.Collections.Generic.Dictionary<string,float>() },
            { "inventory", new System.Collections.Generic.List<InventorySystem.InventoryItem>() },
            { "waveLog", new System.Collections.Generic.List<object>() }, { "allocationLog", new System.Collections.Generic.List<object>() },
            { "rewardLog", new System.Collections.Generic.List<object>() }, { "completedLocations", new System.Collections.Generic.List<string>() },
            { "snapshots", new System.Collections.Generic.List<object>() },
            { "search", null }, { "searchLog", new System.Collections.Generic.List<object>() },
            { "searchStage", "" }, { "searchRounds", 0 }, { "focus", "balanced" }, { "variant", 0 }, { "revision", 0 },
            { "trialWaves", 0 }, { "trialSeconds", 0d },
            { "replaySeedAttempt", null }, { "pendingConfirmation", null },
            { "highestCompletedStage", 0 }, { "simulatedSeconds", 0d }
        });
    }
    checkpoint = new System.Collections.Generic.Dictionary<string,object> { { "config", config }, { "states", states }, { "cursor", 0 }, { "computeSeconds", 0d } };
    System.AppDomain.CurrentDomain.SetData(checkpointKey, checkpoint);
}
var campaignConfig = (System.Collections.Generic.Dictionary<string,object>)checkpoint["config"];
var campaignStates = (System.Collections.Generic.List<System.Collections.Generic.Dictionary<string,object>>)checkpoint["states"];
var campaignRoute = ((object[])campaignConfig["locationIds"]).Cast<string>().ToArray();
var savedRandom = UnityEngine.Random.state;
bool dirtyBefore = UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty;
var watch = System.Diagnostics.Stopwatch.StartNew();
const float step = 1f / 60f;
int stagesThisCommand = 0;
bool exportCampaign = (string)request["operation"] == "export";
bool exportCatalog = (string)request["operation"] == "catalog";
try {
    // A checkpoint ends only at a stage/reset boundary; same-stage waves share one live player.
    do {
        int cursor = (int)checkpoint["cursor"];
        if (exportCampaign || exportCatalog) break;
        if (cursor >= campaignStates.Count) break;
        var state = campaignStates[cursor];
        var strategy = (System.Collections.Generic.Dictionary<string,object>)state["strategy"];
        var search = (System.Collections.Generic.Dictionary<string,object>)state["search"];
        bool isProbe = search != null;
        int probeIndex = isProbe ? (int)search["next"] : -1;
        string focus = isProbe && probeIndex > 0 ? new[] { "health", "offence", "barrier", "speed" }[(probeIndex + (int)search["round"] - 2) % 4] : (string)state["focus"];
        int variant = isProbe && probeIndex > 0 ? (int)search["round"] * 100 + probeIndex : (int)state["variant"];
        var baseNodes = isProbe ? (System.Collections.Generic.List<string>)search["baseNodes"] : (System.Collections.Generic.List<string>)state["allocated"];
        // Odd proposals replace the last third; even proposals rebuild from the same starter.
        var allocated = isProbe ? new System.Collections.Generic.List<string>(probeIndex == 0 ? baseNodes :
            probeIndex % 2 == 1 ? baseNodes.Take(System.Math.Max(1, baseNodes.Count - System.Math.Max(3, baseNodes.Count / 3))) : baseNodes.Take(1)) : baseNodes;
        var unlocked = (System.Collections.Generic.List<string>)state["unlocked"];
        var powers = (System.Collections.Generic.Dictionary<string,float>)state["powers"];
        var allocations = (System.Collections.Generic.List<object>)state["allocationLog"];
        var rewards = (System.Collections.Generic.List<object>)state["rewardLog"];
        int locationIndex = (int)state["locationIndex"];
        var location = locations[campaignRoute[locationIndex]];
        var database = location.EnemyDatabase;
        object progress = System.Activator.CreateInstance(progressType, new object[] { database, catalog });
        ProgressCall(progress, "Initialize");
        if (state["progress"] != null) ProgressCall(progress, "ApplySaveData", state["progress"]);
        if (!(bool)ProgressCall(progress, "TrySelectLocation", location.LocationId)) {
            state["status"] = "blocked"; state["reason"] = "Location prerequisites not completed: " + location.LocationId;
            checkpoint["cursor"] = cursor + 1; continue;
        }
        int stage = ProgressInt(progress, "SelectedLevel");
        int stageAttempt = (int)state["attempt"] + 1;
        int waveQuota = (int)ProgressCall(progress, "GetWavesToUnlockNextLevel", stage);
        var preview = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        var roots = new System.Collections.Generic.List<UnityEngine.GameObject>();
        var definitionsToDispose = new System.Collections.Generic.List<UnityEngine.Object>();
        UnityEngine.GameObject NewRoot(string name) {
            var root = new UnityEngine.GameObject(name); root.SetActive(false); root.hideFlags = UnityEngine.HideFlags.HideAndDontSave;
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, preview); roots.Add(root); return root;
        }
        void DisposeRoot(UnityEngine.GameObject root) {
            if (root == null) return;
            foreach (var actor in root.GetComponents<Battle.Unit>()) {
                actor.effectController.ClearAllEffects();
                typeof(Battle.Unit).GetMethod("UnbindModifierRuntimes", privateFlags).Invoke(actor, null);
            }
            foreach (var zone in root.GetComponents<SkillTree.BonusZone>()) {
                var runtime = (UnityEngine.Object)Field(zone, typeof(SkillTree.BonusZone), "_runtimeModifier");
                FieldSet(zone, typeof(SkillTree.BonusZone), "_runtimeModifier", null);
                if (runtime != null) UnityEngine.Object.DestroyImmediate(runtime);
            }
            UnityEngine.Object.DestroyImmediate(root);
        }
        try {
            var playerRoot = NewRoot("BalanceSimulationCampaignPlayer");
            var level = playerRoot.AddComponent<Battle.UnitLevel>();
            UnityEngine.JsonUtility.FromJsonOverwrite(UnityEngine.JsonUtility.ToJson(sourcePlayer.UnitLevel), level);
            var startData = (SaveSystem.PlayerSaveData)state["player"];
            level.ApplySaveData(isProbe ? new SaveSystem.PlayerSaveData { level = startData.level, currentExp = startData.currentExp,
                skillPoints = (int)search["budget"] - allocated.Sum(id => (int)Field(byId[id], typeof(SkillTree.Node), "nodeCost")) } : startData);
            var tree = playerRoot.AddComponent<SkillTree.MainSkillTree>();
            var inventory = playerRoot.AddComponent<InventorySystem.PlayerInventory>();
            var sourceInventory = UnityEngine.Object.FindAnyObjectByType<InventorySystem.PlayerInventory>(UnityEngine.FindObjectsInactive.Include);
            if (sourceInventory != null) FieldSet(inventory, typeof(InventorySystem.PlayerInventory), "slotCount", sourceInventory.SlotCount);
            foreach (var item in (System.Collections.Generic.List<InventorySystem.InventoryItem>)state["inventory"])
                if (!inventory.TryAddItem(item, out _)) throw new System.InvalidOperationException("Checkpoint inventory could not be restored.");
            var copies = new System.Collections.Generic.Dictionary<SkillTree.Node,SkillTree.Node>();
            SkillTree.Node Copy(SkillTree.Node original) {
                if (copies.TryGetValue(original, out var existing)) return existing;
                var copy = original is SkillTree.RootNode ? playerRoot.AddComponent<SkillTree.RootNode>() : playerRoot.AddComponent<SkillTree.Node>();
                UnityEngine.JsonUtility.FromJsonOverwrite(UnityEngine.JsonUtility.ToJson(original), copy);
                FieldSet(copy, typeof(SkillTree.Node), "connectedNodes", new System.Collections.Generic.List<SkillTree.Node>());
                FieldSet(copy, typeof(SkillTree.Node), "_unitLevel", level);
                FieldSet(copy, typeof(SkillTree.Node), "permanentPower", powers.ContainsKey(original.SaveId) ? powers[original.SaveId] : original.DefaultPermanentPower);
                copy.SetUnlockedFromSave(unlocked.Contains(original.SaveId));
                copies.Add(original, copy); return copy;
            }
            foreach (var root in originals.Where(n => n is SkillTree.RootNode)) Copy(root);
            foreach (string id in allocated) Copy(byId[id]);
            var limitCopies = new System.Collections.Generic.List<SkillTree.LimitedZone>();
            foreach (var original in originalLimits) {
                var members = (System.Collections.Generic.List<SkillTree.Node>)Field(original, typeof(SkillTree.LimitedZone), "nodes");
                var copy = playerRoot.AddComponent<SkillTree.LimitedZone>();
                UnityEngine.JsonUtility.FromJsonOverwrite(UnityEngine.JsonUtility.ToJson(original), copy);
                FieldSet(copy, typeof(SkillTree.LimitedZone), "nodes", members.Where(Numeric).Select(Copy).ToList());
                FieldSet(copy, typeof(SkillTree.LimitedZone), "_unitLevel", level);
                limitCopies.Add(copy);
            }
            var bonusCopies = new System.Collections.Generic.Dictionary<SkillTree.BonusZone,SkillTree.BonusZone>();
            foreach (var original in originalBonuses) {
                var container = (SkillTree.ModifierContainer)Field(original, typeof(SkillTree.BonusZone), "modContainer");
                if (container == null || container.statType.ToString().Contains("Wisp")) continue;
                var copy = playerRoot.AddComponent<SkillTree.BonusZone>();
                FieldSet(copy, typeof(SkillTree.BonusZone), "modContainer", new SkillTree.ModifierContainer(container.modifierType, container.statType, container.value));
                bonusCopies.Add(original, copy);
            }
            void RefreshTree() {
                foreach (var pair in copies) FieldSet(pair.Value, typeof(SkillTree.Node), "connectedNodes",
                    pair.Key.ConnectedNodes.Where(n => n != null && copies.ContainsKey(n)).Select(n => copies[n]).ToList());
                foreach (var pair in bonusCopies) {
                    var members = (System.Collections.Generic.List<SkillTree.Node>)Field(pair.Key, typeof(SkillTree.BonusZone), "nodes");
                    FieldSet(pair.Value, typeof(SkillTree.BonusZone), "nodes", members.Where(n => n != null && copies.ContainsKey(n)).Select(n => copies[n]).ToList());
                }
                FieldSet(tree, typeof(SkillTree.MainSkillTree), "_allocatedNodes", copies.Values.Where(n => n.IsActive).ToList());
            }
            FieldSet(tree, typeof(SkillTree.MainSkillTree), "bonusZones", bonusCopies.Values.ToList());
            RefreshTree();
            foreach (var zone in limitCopies) {
                typeof(SkillTree.LimitedZone).GetMethod("Awake", privateFlags).Invoke(zone, null);
                typeof(SkillTree.LimitedZone).GetMethod("BeginSaveDataRestoreInternal", privateFlags).Invoke(zone, null);
            }
            foreach (string id in allocated) copies[byId[id]].SetAllocatedFromSave(true);
            foreach (var zone in limitCopies) typeof(SkillTree.LimitedZone).GetMethod("EndSaveDataRestoreInternal", privateFlags).Invoke(zone, null);
            RefreshTree();
            Battle.Unit Actor(UnityEngine.GameObject root, Battle.Unit source, bool isPlayer) {
                var health = root.AddComponent<Battle.Health>(); var barrier = root.AddComponent<Battle.Barrier>();
                var mystic = root.AddComponent<Battle.MysticHealth>(); var effects = root.AddComponent<Battle.EffectController>();
                OwnEditorEffects(effects);
                var attacker = root.AddComponent<Battle.Attacker>(); var attributes = root.AddComponent<Battle.Attributes>();
                UnityEngine.JsonUtility.FromJsonOverwrite(UnityEngine.JsonUtility.ToJson(source.attributes), attributes);
                Battle.Unit actor;
                if (isPlayer) {
                    actor = root.AddComponent<Battle.PlayerUnit>();
                    FieldSet(actor, typeof(Battle.PlayerUnit), "<SkillTree>k__BackingField", tree);
                    FieldSet(actor, typeof(Battle.PlayerUnit), "<UnitLevel>k__BackingField", level);
                } else {
                    actor = root.AddComponent<Battle.EnemyUnit>();
                    FieldSet(actor, typeof(Battle.EnemyUnit), "_playerLevel", level);
                }
                actor.health = health; actor.barrier = barrier; actor.mysticHealth = mystic;
                actor.effectController = effects; actor.attacker = attacker; actor.attributes = attributes;
                FieldSet(actor, typeof(Battle.Unit), "baseInnateModifiers", Field(source, typeof(Battle.Unit), "baseInnateModifiers"));
                FieldSet(actor, typeof(Battle.Unit), "innateModifiers", Field(source, typeof(Battle.Unit), "innateModifiers"));
                actor.SetWeaponType(source.WeaponType);
                typeof(Battle.Unit).GetMethod("Awake", privateFlags).Invoke(actor, null);
                root.SetActive(true);
                // Enemy prefab innate data may be null; Initialize installs generated modifiers before recalculation.
                if (isPlayer) actor.RequestModRecalculation();
                return actor;
            }
            var player = (Battle.PlayerUnit)Actor(playerRoot, sourcePlayer, true);
            string style = (string)strategy["style"], starterId = (string)strategy["starterId"];
            string damageFamily = strategy.ContainsKey("damageFamily") ? (string)strategy["damageFamily"] : "Physical";
            string attribute = strategy.ContainsKey("attribute") ? (string)strategy["attribute"] : "";
            double Score(SkillTree.Node node) {
                double score = 0.01;
                foreach (var mod in node.Modifiers) {
                    var c = Container(mod); string stat = c.statType.ToString();
                    bool defence = stat.Contains("Health") || stat.Contains("Armor") || stat.Contains("Evasion") || stat.Contains("Barrier") ||
                        stat.Contains("Resistance") || stat.Contains("Mitigation") || stat.Contains("Block") || stat.Contains("Guard");
                    bool offence = !defence && (stat.Contains("Damage") || stat.Contains("AttackSpeed") || stat.Contains("Crit") || stat.Contains("Accuracy") ||
                        (damageFamily == "Fire" && stat.Contains("Ignite")) || (damageFamily == "Lightning" && stat.Contains("Overcharge")) || (damageFamily == "Physical" && stat.Contains("Bleed")));
                    bool elemental = damageFamily == "Fire" || damageFamily == "Cold" || damageFamily == "Lightning";
                    bool relevantDamage = !stat.Contains("Damage") || defence || stat == "Damage" || stat.Contains(damageFamily) || (elemental && stat.Contains("Elemental"));
                    if (!relevantDamage || c.value <= 0) continue;
                    double weight = defence ? (style == "defence" ? 8 : style == "balanced" ? 5 : 2) : offence ? (style == "damage" ? 8 : style == "balanced" ? 5 : 2) : 2;
                    if (attribute.Length > 0) {
                        if (stat == attribute || stat == "AllAttributes") weight = 7;
                        else if (stat == "Strength" || stat == "Dexterity" || stat == "Intelligence") weight = 0.5;
                    }
                    if (focus == "health" && (stat.Contains("Health") || stat.Contains("Armor") || stat.Contains("Resistance"))) weight *= 3;
                    if (focus == "offence" && offence) weight *= 3;
                    if (focus == "barrier" && (stat.Contains("Barrier") || stat.Contains("Resistance") || stat.Contains("Evasion"))) weight *= 3;
                    if (focus == "speed" && (stat.Contains("AttackSpeed") || stat.Contains("Crit") || stat.Contains("Accuracy"))) weight *= 3;
                    double scale = c.modifierType == ModifierType.Added ?
                        (stat.Contains("MaximumHealth") ? 20 : stat.Contains("Armor") || stat.Contains("Evasion") ? 6 : stat.Contains("BarrierCapacity") ? 12 :
                        stat.Contains("Chance") || stat.Contains("AttackSpeed") ? 0.05 : stat.Contains("Damage") ? 2 : 1) : 0.1;
                    score += weight * System.Math.Sqrt(c.value / scale);
                }
                uint hash = unchecked((uint)((int)state["seed"] + variant * 7919));
                foreach (char character in node.SaveId) hash = unchecked(hash * 16777619u ^ character);
                double diversity = variant == 0 ? 1 : 0.75 + (hash % 1000) / 2000d;
                return diversity * score / System.Math.Max(1, (int)Field(node, typeof(SkillTree.Node), "nodeCost"));
            }
            bool growing = false;
            bool probeCombatStarted = false;
            int currentWave = 0;
            // Two-edge lookahead can pay for travel nodes; it is an agent heuristic, not a game rule.
            double RouteScore(SkillTree.Node node) {
                double future = node.ConnectedNodes.Where(n => Numeric(n) && !allocated.Contains(n.SaveId) && !(n is SkillTree.RootNode))
                    .Select(n => Score(n) + 0.35 * n.ConnectedNodes.Where(x => Numeric(x) && x != node && !allocated.Contains(x.SaveId) && !(x is SkillTree.RootNode))
                        .Select(Score).DefaultIfEmpty(0).Max()).DefaultIfEmpty(0).Max();
                return Score(node) + (strategy.ContainsKey("lookahead") && (bool)strategy["lookahead"] ? 0.5 * future : 0);
            }
            void Grow() {
                if (isProbe && (probeIndex == 0 || probeCombatStarted)) return;
                if (growing) return; growing = true;
                try {
                    for (int safety = 0; safety < 1000 && level.SkillPoints > 0; safety++) {
                        var frontier = eligible.Where(n => !(n is SkillTree.RootNode) && !allocated.Contains(n.SaveId) &&
                            n.ConnectedNodes.Any(next => next != null && copies.ContainsKey(next) && copies[next].IsActive)).ToArray();
                        foreach (var original in frontier) Copy(original);
                        RefreshTree();
                        var candidates = frontier.Where(n => copies[n].CanBeAllocated() && copies[n].HasEnoughSkillPoints()).ToArray();
                        if (attribute.Length > 0) candidates = candidates.Where(n => n.SaveId == starterId || !n.ConnectedNodes.Any(x => x is SkillTree.RootNode)).ToArray();
                        if (allocated.Count == 0) candidates = candidates.Where(n => n.SaveId == starterId).ToArray();
                        var next = candidates.OrderByDescending(RouteScore).ThenBy(n => n.SaveId, System.StringComparer.Ordinal).FirstOrDefault();
                        if (next == null) break;
                        int before = level.SkillPoints;
                        if (!copies[next].Allocate()) throw new System.InvalidOperationException("Allocation predicate changed unexpectedly.");
                        allocated.Add(next.SaveId); RefreshTree(); player.RequestModRecalculation();
                        if (!isProbe) allocations.Add(new { revision = (int)state["revision"], location = location.LocationId, stage, wave = currentWave, level = level.Level, nodeId = next.SaveId,
                            modifiers = next.Modifiers.Select(m => m.name).ToArray(), pointsSpent = before - level.SkillPoints, pointsRemaining = level.SkillPoints });
                    }
                } finally { growing = false; }
            }
            level.OnSkillPointsChanged += unused => Grow();
            level.OnLevelUp += unused => Grow();
            Grow(); player.RequestModRecalculation(); player.ResetCombatState();
            if (!allocated.Contains(starterId)) throw new System.InvalidOperationException("Starter could not be allocated from authored initial points.");
            var useService = new InventorySystem.InventoryItemUseService(inventory, player);
            void UseSimpleItems() {
                for (int safety = 0; safety < 1000; safety++) {
                    bool used = false;
                    for (int slot = 0; slot < inventory.SlotCount; slot++) {
                        var item = inventory.PeekItem(slot); if (item == null || item.IsEmpty) continue;
                        var definition = item.ItemDefinition; string targetId = null;
                        if (definition is Items.GrantSkillPointsConsumableDefinition) used = useService.TryUseItem(slot);
                        else if (definition is Items.UnlockNodeItemDefinition || definition is Items.IncreaseNodePowerItemDefinition) {
                            var targets = definition is Items.UnlockNodeItemDefinition
                                ? eligible.Where(n => (bool)Field(n, typeof(SkillTree.Node), "startsLocked") && !unlocked.Contains(n.SaveId) &&
                                    n.ConnectedNodes.Any(x => x != null && copies.ContainsKey(x) && copies[x].IsActive))
                                : eligible.Where(n => allocated.Contains(n.SaveId));
                            foreach (var target in targets.OrderByDescending(Score).ThenBy(n => n.SaveId, System.StringComparer.Ordinal)) {
                                var copy = Copy(target); var context = new Items.ItemUseContext(player, inventory, slot, item);
                                if (!definition.TryUseOnNode(context, copy)) continue;
                                if (!inventory.TryConsumeItem(slot, 1)) throw new System.InvalidOperationException("Applied reward could not be consumed.");
                                targetId = target.SaveId; used = true;
                                if (definition is Items.UnlockNodeItemDefinition) unlocked.Add(targetId);
                                else powers[targetId] = copy.PermanentPower;
                                break;
                            }
                        }
                        if (used) {
                            rewards.Add(new { action = "used", location = location.LocationId, stage, level = level.Level, item = definition.name, itemType = definition.GetType().Name, targetId });
                            Grow(); RefreshTree(); player.RequestModRecalculation(); break;
                        }
                    }
                    if (!used) break;
                }
            }
            if (!isProbe) UseSimpleItems();
            void Snapshot(string boundary) {
                // Only at existing reset boundaries. No combat effects contaminate the build stats.
                player.RequestModRecalculation(); player.CombatTick(0f, Battle.CombatTickPhase.Mods);
                var stats = System.Enum.GetValues(typeof(StatType)).Cast<StatType>().Where(s => s != StatType.Empty)
                    .ToDictionary(s => s.ToString(), s => player.BaseUnitModifiers.GetStatValue(s));
                ((System.Collections.Generic.List<object>)state["snapshots"]).Add(new {
                    boundary, location = location.LocationId, stage, attempt = stageAttempt,
                    level = level.Level, experience = level.CurrentExp, freePoints = level.SkillPoints, stats,
                    allocationCount = allocations.Count, rewardCount = rewards.Count, waveCount = (int)state["waves"],
                    revision = (int)state["revision"], probe = isProbe, probeIndex, buildOrder = allocated.ToArray(),
                    searchCount = ((System.Collections.Generic.List<object>)state["searchLog"]).Count +
                        ((isProbe && boundary == "after-probe") || (!isProbe && state["pendingConfirmation"] != null && boundary.StartsWith("after-")) ? 1 : 0),
                    nodes = copies.Where(p => p.Value.IsAllocated || p.Key is SkillTree.RootNode).Select(p => new {
                        id = p.Key.SaveId, active = p.Value.IsActive, power = p.Value.Power, multiplier = p.Value.PowerMultiplier,
                        investedPoints = p.Value.InvestedSkillPoints
                    }).ToArray()
                });
            }
            Snapshot(isProbe ? "before-probe" : "before-stage");
            probeCombatStarted = true;
            var resolverRoot = NewRoot("BalanceSimulationCampaignTargets"); resolverRoot.SetActive(true);
            var resolver = resolverRoot.AddComponent<Battle.AttackResolver>(); player.attacker.SetTarget(resolver);
            var goldResolver = new DropSystem.GoldDropResolver(database.GoldDropConfig);
            bool stageWon = true, wasBoss = false;
            int completedWaves = 0, probeKills = 0;
            double damagedFraction = 0, stageSeconds = 0;
            string proposalSignature = string.Join("|", allocated.OrderBy(id => id, System.StringComparer.Ordinal));
            bool duplicateProposal = isProbe && probeIndex > 0 && !((System.Collections.Generic.HashSet<string>)search["seen"]).Add(proposalSignature);
            for (currentWave = 1; currentWave <= waveQuota; currentWave++) {
                if (duplicateProposal) { stageWon = false; break; }
                int waveSeed = unchecked((int)state["seed"] + stage * 100003 + currentWave * 7919 +
                    (isProbe ? (int)search["seedAttempt"] : state["replaySeedAttempt"] != null ? (int)state["replaySeedAttempt"] : (int)state["attempt"]) * 15485863);
                UnityEngine.Random.InitState(waveSeed);
                var context = new Battle.WaveContext(stage, currentWave, waveQuota);
                if (database.BossBalance != null && database.BossBalance.TryGetRule(context, out var rule))
                    context = new Battle.WaveContext(stage, currentWave, waveQuota, true, rule.BossCount, rule.TotalEnemiesInWave, rule.MaxBossAffixes);
                wasBoss = context.IsBossWave;
                var packages = new Battle.WaveFactory(new Battle.EnemyFactory(database), database).CreateWave(context);
                foreach (var package in packages) definitionsToDispose.Add(package.Modifiers);
                var enemies = packages.Take(poolSize).Select(package => {
                    var root = NewRoot("BalanceSimulationCampaignEnemy");
                    var enemy = (Battle.EnemyUnit)Actor(root, sourceEnemy, false);
                    enemy.Initialize(new Battle.EnemySpawnData(package.Definition, package.Rarity, package.Power, isProbe ? 0 : package.ExperienceReward,
                        package.Modifiers, package.Affixes, package.BaseEvasion, false));
                    enemy.attacker.SetTarget(player);
                    enemy.OnDeath += unused => {
                        if (isProbe) { probeKills++; return; }
                        state["kills"] = (int)state["kills"] + 1;
                        var gold = goldResolver.Resolve(package, stage); if (gold.HasGold) state["gold"] = (int)state["gold"] + gold.Amount;
                    };
                    return enemy;
                }).ToArray();
                if (enemies.Length == 0) throw new System.InvalidOperationException("Wave generated no active enemies.");
                resolver.SetNewEnemies(enemies.Cast<Battle.Unit>().ToList());
                UnityEngine.Random.InitState(unchecked(waveSeed ^ 0x5A17));
                int initialLevel = level.Level, attacks = 0;
                float initialHp = player.health.CurrentHealth, minimumHp = initialHp;
                double attackHpLoss = 0;
                System.Action<Battle.ITarget> countAttack = unused => attacks++;
                System.Action<Battle.DamageInfo,float> countLoss = (unused, amount) => attackHpLoss += amount;
                System.Action changed = () => minimumHp = UnityEngine.Mathf.Min(minimumHp, player.health.CurrentHealth);
                player.OnAttack += countAttack; player.OnHealthDamageTaken += countLoss; player.health.OnHealthChanged += changed;
                var actors = new Battle.Unit[] { player }.Concat(enemies).ToArray(); int ticks = 0;
                for (; ticks < Number(campaignConfig, "maxWaveSeconds", 180) * 60 && player.gameObject.activeSelf && enemies.Any(e => e.gameObject.activeSelf); ticks++)
                    for (int phase = 0; phase < 4; phase++) foreach (var actor in actors)
                        if (actor.isActiveAndEnabled) actor.CombatTick(step, (Battle.CombatTickPhase)phase);
                player.OnAttack -= countAttack; player.OnHealthDamageTaken -= countLoss; player.health.OnHealthChanged -= changed;
                bool dead = !player.gameObject.activeSelf;
                bool won = !dead && enemies.All(e => !e.gameObject.activeSelf);
                double seconds = ticks * (double)step;
                stageSeconds += seconds;
                if (won) { completedWaves++; damagedFraction = 0; }
                else damagedFraction = 1 - enemies.Sum(e => e.gameObject.activeSelf ? System.Math.Max(0, e.health.CurrentHealth) : 0) /
                    System.Math.Max(0.000001, enemies.Sum(e => e.health.MaxHealth));
                if (isProbe) { state["trialWaves"] = (int)state["trialWaves"] + 1; state["trialSeconds"] = (double)state["trialSeconds"] + seconds; }
                else {
                state["waves"] = (int)state["waves"] + 1; state["simulatedSeconds"] = (double)state["simulatedSeconds"] + seconds;
                ((System.Collections.Generic.List<object>)state["waveLog"]).Add(new { location = location.LocationId, stage, wave = currentWave,
                    attempt = (int)state["attempt"] + 1, waveSeed, boss = wasBoss, outcome = won ? "won" : dead ? "dead" : "timeout",
                    initialLevel, finalLevel = level.Level, allocatedNodes = allocated.Count, freePoints = level.SkillPoints,
                    initialHp, minimumHp, remainingHp = player.health.CurrentHealth, maximumHp = player.health.MaxHealth, attacks, attackHpLoss, ticks, simulatedSeconds = seconds });
                }
                foreach (var enemy in enemies) DisposeRoot(enemy.gameObject);
                foreach (var definition in definitionsToDispose) if (definition != null) UnityEngine.Object.DestroyImmediate(definition);
                definitionsToDispose.Clear();
                if (!won) { stageWon = false; if (dead && !isProbe) state["deaths"] = (int)state["deaths"] + 1; break; }
                if (currentWave < waveQuota) {
                    int delayTicks = (int)System.Math.Ceiling(database.RespawnDelay / step);
                    for (int delay = 0; delay < delayTicks && player.gameObject.activeSelf; delay++)
                        for (int phase = 0; phase < 4; phase++) player.CombatTick(step, (Battle.CombatTickPhase)phase);
                    if (!player.gameObject.activeSelf) { stageWon = false; if (!isProbe) { state["deaths"] = (int)state["deaths"] + 1; state["reason"] = "Death during respawn delay"; } break; }
                }
            }
            if (isProbe) {
                if (level.Level != startData.level || level.CurrentExp != startData.currentExp) throw new System.InvalidOperationException("Probe changed progression.");
                double fitness = completedWaves + System.Math.Max(0, System.Math.Min(1, damagedFraction));
                bool improved = !duplicateProposal && (stageWon || fitness > (double)search["bestFitness"] + 0.000001);
                if (probeIndex == 0) {
                    search["baselineFitness"] = fitness; ((System.Collections.Generic.HashSet<string>)search["seen"]).Add(proposalSignature);
                }
                if (improved) {
                    search["bestFitness"] = fitness; search["bestNodes"] = new System.Collections.Generic.List<string>(allocated);
                    search["bestFree"] = level.SkillPoints; search["bestFocus"] = focus; search["bestVariant"] = variant;
                }
                player.ResetCombatState(); Snapshot("after-probe");
                ((System.Collections.Generic.List<object>)state["searchLog"]).Add(new {
                    action = "evaluate", location = location.LocationId, stage, round = (int)search["round"], candidate = probeIndex,
                    focus, variant, level = startData.level, experience = startData.currentExp, budget = (int)search["budget"],
                    waveSeedAttempt = (int)search["seedAttempt"], duplicate = duplicateProposal, won = stageWon, completedWaves, probeKills,
                    fitness, baselineFitness = (double)search["baselineFitness"], delta = fitness - (double)search["baselineFitness"], improved,
                    simulatedSeconds = stageSeconds, freePoints = level.SkillPoints,
                    added = allocated.Except(baseNodes).ToArray(), removed = baseNodes.Except(allocated).ToArray(),
                    nodeIds = allocated.ToArray(), snapshotIndex = ((System.Collections.Generic.List<object>)state["snapshots"]).Count - 1
                });
                search["next"] = probeIndex + 1;
                if (stageWon || probeIndex >= Number(campaignConfig, "adaptiveCandidates", 4)) {
                    var bestNodes = (System.Collections.Generic.List<string>)search["bestNodes"];
                    bool changedBuild = !new System.Collections.Generic.HashSet<string>(baseNodes).SetEquals(bestNodes);
                    state["allocated"] = bestNodes; state["focus"] = search["bestFocus"]; state["variant"] = search["bestVariant"];
                    startData.skillPoints = (int)search["bestFree"];
                    if (changedBuild) {
                        state["revision"] = (int)state["revision"] + 1;
                        foreach (string id in bestNodes) allocations.Add(new { revision = (int)state["revision"], location = location.LocationId, stage,
                            wave = 0, level = startData.level, nodeId = id, modifiers = byId[id].Modifiers.Select(m => m.name).ToArray(),
                            pointsSpent = (int)Field(byId[id], typeof(SkillTree.Node), "nodeCost"), pointsRemaining = startData.skillPoints });
                    }
                    ((System.Collections.Generic.List<object>)state["searchLog"]).Add(new { action = "commit", location = location.LocationId,
                        stage, round = (int)search["round"], changedBuild, won = stageWon, focus = (string)state["focus"],
                        fitness = (double)search["bestFitness"], baselineFitness = (double)search["baselineFitness"],
                        delta = (double)search["bestFitness"] - (double)search["baselineFitness"], revision = (int)state["revision"], nodeIds = bestNodes.ToArray() });
                    state["search"] = null;
                    state["replaySeedAttempt"] = search["seedAttempt"];
                    state["pendingConfirmation"] = search["round"];
                }
            } else {
            if (stageWon) {
                object[] completedArgs = { 0 };
                bool firstCompletion = (bool)ProgressCall(progress, "RegisterCompletedLevel", completedArgs);
                player.ResetCombatState(); ProgressCall(progress, "UnlockNextLevel");
                state["highestCompletedStage"] = System.Math.Max((int)state["highestCompletedStage"], stage);
                if (wasBoss && firstCompletion) {
                    var pending = (System.Collections.Generic.List<Battle.PendingLocationReward>)ProgressCall(progress, "GetPendingLocationRewards", stage);
                    foreach (var reward in pending) {
                        bool delivered = inventory.TryAddItem(reward.Item, out _);
                        // Match the currently inspected completion window, including BL-001 on insertion failure.
                        bool claimed = (bool)ProgressCall(progress, "TryClaimReward", reward);
                        rewards.Add(new { action = "claim", location = location.LocationId, stage, rewardId = reward.RewardId,
                            item = reward.Item.ItemDefinition.name, amount = reward.Item.StackCount, delivered, claimed });
                    }
                    UseSimpleItems();
                }
                state["attempt"] = 0;
                if (stage >= database.MaxWaveLevel) {
                    ((System.Collections.Generic.List<string>)state["completedLocations"]).Add(location.LocationId);
                    state["locationIndex"] = locationIndex + 1;
                    if (locationIndex + 1 >= campaignRoute.Length) { state["status"] = "completed"; state["reason"] = "Route completed"; }
                } else ProgressCall(progress, "SetSelectedLevel", stage + 1);
            } else {
                state["attempt"] = (int)state["attempt"] + 1;
                if ((int)state["attempt"] >= Number(campaignConfig, "maxAttemptsPerStage", 3)) {
                    state["status"] = "stopped"; state["reason"] = "Stage retry limit: " + location.LocationId + "/" + stage;
                }
            }
            state["player"] = level.CaptureSaveData(); state["progress"] = ProgressCall(progress, "CaptureSaveData");
            state["inventory"] = inventory.Slots.Where(s => s.Item != null && !s.Item.IsEmpty).Select(s => s.Item.CreateCopy()).ToList();
            player.ResetCombatState(); Snapshot(stageWon ? "after-win" : "after-failure");
            if (state["pendingConfirmation"] != null) {
                ((System.Collections.Generic.List<object>)state["searchLog"]).Add(new { action = "confirm", location = location.LocationId,
                    stage, round = (int)state["pendingConfirmation"], won = stageWon, focus = (string)state["focus"], revision = (int)state["revision"],
                    snapshotIndex = ((System.Collections.Generic.List<object>)state["snapshots"]).Count - 1 });
                state["pendingConfirmation"] = null;
            }
            state["replaySeedAttempt"] = null;
            bool adaptive = campaignConfig.ContainsKey("adaptive") && (bool)campaignConfig["adaptive"];
            string searchStage = location.LocationId + "/" + stage;
            if ((string)state["searchStage"] != searchStage) { state["searchStage"] = searchStage; state["searchRounds"] = 0; }
            if (!stageWon && adaptive && (string)state["status"] == "running" && (int)state["searchRounds"] < Number(campaignConfig, "adaptiveRoundsPerStage", 3)) {
                state["searchRounds"] = (int)state["searchRounds"] + 1;
                state["search"] = new System.Collections.Generic.Dictionary<string,object> {
                    { "next", 0 }, { "round", (int)state["searchRounds"] }, { "seedAttempt", (int)state["attempt"] - 1 },
                    { "baseNodes", new System.Collections.Generic.List<string>(allocated) },
                    { "budget", level.SkillPoints + allocated.Sum(id => (int)Field(byId[id], typeof(SkillTree.Node), "nodeCost")) },
                    { "bestNodes", new System.Collections.Generic.List<string>(allocated) }, { "bestFree", level.SkillPoints },
                    { "bestFocus", state["focus"] }, { "bestVariant", state["variant"] }, { "bestFitness", -1d }, { "baselineFitness", 0d },
                    { "seen", new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal) }
                };
            }
            if ((string)state["status"] != "running") checkpoint["cursor"] = cursor + 1;
            }
        } finally {
            foreach (var root in roots) DisposeRoot(root);
            foreach (var definition in definitionsToDispose) if (definition != null) UnityEngine.Object.DestroyImmediate(definition);
            UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(preview);
        }
        stagesThisCommand++;
    } while (watch.ElapsedMilliseconds < Number(request, "computeBudgetMs", 1500));
} catch {
    // A failed request is not resumable: avoid accidental reuse of partially updated data.
    System.AppDomain.CurrentDomain.SetData(checkpointKey, null); throw;
} finally {
    UnityEngine.Random.state = savedRandom; watch.Stop();
}
checkpoint["computeSeconds"] = (double)checkpoint["computeSeconds"] + watch.Elapsed.TotalSeconds;
bool finished = (int)checkpoint["cursor"] >= campaignStates.Count;
int exportIndex = Number(request, "index", -1);
if (exportCampaign && (exportIndex < 0 || exportIndex >= (int)checkpoint["cursor"])) throw new System.InvalidOperationException("Only completed campaigns can be exported.");
var results = exportCampaign ? campaignStates.Skip(exportIndex).Take(1).Select(state => new {
    strategy = (string)((System.Collections.Generic.Dictionary<string,object>)state["strategy"])["name"], seed = (int)state["seed"],
    status = (string)state["status"], reason = (string)state["reason"], highestCompletedStage = (int)state["highestCompletedStage"],
    completedLocations = (System.Collections.Generic.List<string>)state["completedLocations"],
    player = (SaveSystem.PlayerSaveData)state["player"], allocatedNodeIds = (System.Collections.Generic.List<string>)state["allocated"],
    unlockedNodeIds = (System.Collections.Generic.List<string>)state["unlocked"], deaths = (int)state["deaths"], waves = (int)state["waves"],
    kills = (int)state["kills"], gold = (int)state["gold"], simulatedSeconds = (double)state["simulatedSeconds"],
    inventory = ((System.Collections.Generic.List<InventorySystem.InventoryItem>)state["inventory"]).Select(i => new { item = i.ItemDefinition.name, amount = i.StackCount }).ToArray(),
    waveLog = (System.Collections.Generic.List<object>)state["waveLog"], allocationLog = (System.Collections.Generic.List<object>)state["allocationLog"], rewardLog = (System.Collections.Generic.List<object>)state["rewardLog"],
    snapshots = (System.Collections.Generic.List<object>)state["snapshots"]
    , searchLog = (System.Collections.Generic.List<object>)state["searchLog"], trialWaves = (int)state["trialWaves"],
    trialSimulatedSeconds = (double)state["trialSeconds"], allocationRevision = (int)state["revision"], learnedFocus = (string)state["focus"]
}).ToArray() : null;
// Keep data until the wrapper confirms per-campaign files were written, then discard.
var response = new { schemaVersion = 2, mode = "progression-campaign", finished, completedRuns = (int)checkpoint["cursor"], totalRuns = campaignStates.Count,
    stagesThisCommand, computeSeconds = (double)checkpoint["computeSeconds"], elapsedSeconds = watch.Elapsed.TotalSeconds,
    sceneDirtyBefore = dirtyBefore, sceneDirtyAfter = UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty, campaigns = results,
    treeCatalog = exportCatalog ? originals.Select(n => new {
        id = n.SaveId, name = n.name, type = n.GetType().Name, supported = Numeric(n), root = n is SkillTree.RootNode,
        x = sourcePlayer.SkillTree.transform.InverseTransformPoint(n.transform.position).x,
        y = sourcePlayer.SkillTree.transform.InverseTransformPoint(n.transform.position).y,
        cost = n is SkillTree.RootNode ? 0 : (int)Field(n, typeof(SkillTree.Node), "nodeCost"),
        connectedIds = n.ConnectedNodes.Where(x => x != null).Select(x => x.SaveId).ToArray(),
        modifiers = n.Modifiers.Select(m => new { name = m == null ? "missing" : m.name, type = m == null ? "missing" : m.GetType().Name,
            serialized = m == null ? null : UnityEngine.JsonUtility.ToJson(m),
            stat = Container(m) == null ? null : Container(m).statType.ToString(),
            operation = Container(m) == null ? null : Container(m).modifierType.ToString(),
            value = Container(m) == null ? 0f : Container(m).value,
            levelsPerModifier = m is SkillTree.ModifierPerPlayerLevel ? (int)Field(m, typeof(SkillTree.ModifierPerPlayerLevel), "levelsPerModifier") : 0
        }).ToArray()
    }).ToArray() : null };
if (exportCampaign || exportCatalog) {
    return WriteEditorExport(response, (string)request["exportPath"]);
}
return response;

