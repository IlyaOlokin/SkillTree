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
if (request.ContainsKey("config") && ((System.Collections.Generic.Dictionary<string,object>)request["config"]).ContainsKey("controlledRun") &&
    (bool)((System.Collections.Generic.Dictionary<string,object>)request["config"])["controlledRun"] && sourcePlayer.gameObject.scene.isDirty)
    throw new System.InvalidOperationException("Save MainScene before a controlled experiment; unsaved scene inputs are not reproducible.");
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
bool KnownNode(SkillTree.Node n) => n != null;
var eligible = originals.Where(KnownNode).ToArray();
var progressType = typeof(Battle.EnemySpawner).Assembly.GetType("Battle.EnemyLocationProgressService", true);
object ProgressCall(object progress, string name, params object[] args) =>
    progressType.GetMethods().Single(m => m.Name == name && m.GetParameters().Length == args.Length &&
        (args.Length == 0 || args[0] == null || m.GetParameters()[0].ParameterType.IsByRef || m.GetParameters()[0].ParameterType.IsInstanceOfType(args[0])))
        .Invoke(progress, args);
int ProgressInt(object progress, string name) => (int)progressType.GetProperty(name).GetValue(progress);
var checkpoint = (System.Collections.Generic.Dictionary<string,object>)System.AppDomain.CurrentDomain.GetData(checkpointKey);
if (checkpoint == null) {
    var config = (System.Collections.Generic.Dictionary<string,object>)request["config"];
    var agentSettings = config.ContainsKey("agent") ? (System.Collections.Generic.Dictionary<string,object>)config["agent"] : null;
    var actorModel = agentSettings == null ? null : ReadAgentModel((string)agentSettings["modelPath"]);
    if (agentSettings != null && config.ContainsKey("adaptive") && (bool)config["adaptive"])
        throw new System.InvalidOperationException("Learning/frozen actor runs cannot use experimental adaptive respec.");
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
        if (!byId.ContainsKey(starter) || !KnownNode(byId[starter]) || !new[] { "damage", "defence", "balanced" }.Contains(style))
            throw new System.InvalidOperationException("Invalid starter/style in " + strategy["name"]);
        for (int run = 0; run < runs; run++) states.Add(new System.Collections.Generic.Dictionary<string,object> {
            { "strategy", strategy }, { "seed", unchecked(seed + run) }, { "status", "running" }, { "reason", "" },
            { "locationIndex", 0 }, { "attempt", 0 }, { "deaths", 0 }, { "waves", 0 }, { "kills", 0 }, { "gold", 0 },
            { "player", new SaveSystem.PlayerSaveData { level = sourcePlayer.UnitLevel.Level, currentExp = sourcePlayer.UnitLevel.CurrentExp, skillPoints = sourcePlayer.UnitLevel.SkillPoints } },
            { "progress", null }, { "allocated", new System.Collections.Generic.List<string>() },
            { "unlocked", new System.Collections.Generic.List<string>() }, { "powers", new System.Collections.Generic.Dictionary<string,float>() },
            { "inventory", new System.Collections.Generic.List<InventorySystem.InventoryItem>() },
            { "socketGems", new System.Collections.Generic.Dictionary<string,Gems.GemInstance>() },
            { "investments", new System.Collections.Generic.Dictionary<string,int>() },
            { "shopPurchases", new System.Collections.Generic.List<SaveSystem.ShopPurchaseSaveData>() },
            { "shopLog", new System.Collections.Generic.List<object>() },
            { "waveLog", new System.Collections.Generic.List<object>() }, { "allocationLog", new System.Collections.Generic.List<object>() },
            { "rewardLog", new System.Collections.Generic.List<object>() }, { "completedLocations", new System.Collections.Generic.List<string>() },
            { "snapshots", new System.Collections.Generic.List<object>() },
            { "search", null }, { "searchLog", new System.Collections.Generic.List<object>() },
            { "searchStage", "" }, { "searchRounds", 0 }, { "focus", "balanced" }, { "variant", 0 }, { "revision", 0 },
            { "trialWaves", 0 }, { "trialSeconds", 0d },
            { "replaySeedAttempt", null }, { "pendingConfirmation", null },
            { "highestCompletedStage", 0 }, { "simulatedSeconds", 0d }
            , { "agentLog", new System.Collections.Generic.List<object>() }, { "agentRewards", new System.Collections.Generic.List<object>() },
            { "agentRandom", new System.Random(unchecked(seed + run + strategy["name"].ToString().Aggregate(17, (h, c) => unchecked(h * 31 + c)))) }
        });
    }
    checkpoint = new System.Collections.Generic.Dictionary<string,object> { { "config", config }, { "states", states }, { "cursor", 0 }, { "computeSeconds", 0d }, { "actorModel", actorModel } };
    System.AppDomain.CurrentDomain.SetData(checkpointKey, checkpoint);
}
var campaignConfig = (System.Collections.Generic.Dictionary<string,object>)checkpoint["config"];
var campaignStates = (System.Collections.Generic.List<System.Collections.Generic.Dictionary<string,object>>)checkpoint["states"];
var campaignRoute = ((object[])campaignConfig["locationIds"]).Cast<string>().ToArray();
var agentConfig = campaignConfig.ContainsKey("agent") ? (System.Collections.Generic.Dictionary<string,object>)campaignConfig["agent"] : null;
var learnedModel = (System.Collections.Generic.Dictionary<string,object>)checkpoint["actorModel"];
var learnedWeights = learnedModel == null ? null : AgentWeights(learnedModel);
var learnedOffsets = learnedModel == null ? null : AgentOffsets(learnedModel);
double AgentSetting(string name, double fallback) => agentConfig != null && agentConfig.ContainsKey(name) ? System.Convert.ToDouble(agentConfig[name]) : fallback;
var savedRandom = UnityEngine.Random.state;
bool dirtyBefore = UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty;
var watch = System.Diagnostics.Stopwatch.StartNew();
var sourceTick = UnityEngine.Object.FindObjectsByType<Battle.BattleTickSystem>(UnityEngine.FindObjectsInactive.Include)
    .Where(t => t.gameObject.scene == sourcePlayer.gameObject.scene).ToArray();
if (sourceTick.Length != 1) throw new System.InvalidOperationException("MainScene must have exactly one BattleTickSystem for controlled simulation.");
float step = sourceTick[0].TickDuration;
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
        var investments = isProbe ? new System.Collections.Generic.Dictionary<string,int>((System.Collections.Generic.Dictionary<string,int>)state["investments"]) :
            (System.Collections.Generic.Dictionary<string,int>)state["investments"];
        float NodeCost(SkillTree.Node n) => n.IsInfinite ? 1f : System.Convert.ToSingle(Field(n, typeof(SkillTree.Node), "nodeCost"));
        float Spent(string id) => NodeCost(byId[id]) * (byId[id].IsInfinite && investments.ContainsKey(id) ? investments[id] : 1);
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
                typeof(Battle.Unit).GetMethod("UnregisterFromBattleTickSystem", privateFlags).Invoke(actor, null);
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
            var tickRoot = NewRoot("BalanceSimulationCampaignTick");
            var simulatedTick = tickRoot.AddComponent<Battle.BattleTickSystem>(); simulatedTick.TickDuration = step;
            var runTick = (System.Action<float>)typeof(Battle.BattleTickSystem).GetMethod("RunTick", privateFlags).CreateDelegate(typeof(System.Action<float>), simulatedTick);
            var playerRoot = NewRoot("BalanceSimulationCampaignPlayer");
            var level = playerRoot.AddComponent<Battle.UnitLevel>();
            UnityEngine.JsonUtility.FromJsonOverwrite(UnityEngine.JsonUtility.ToJson(sourcePlayer.UnitLevel), level);
            var startData = (SaveSystem.PlayerSaveData)state["player"];
            level.ApplySaveData(isProbe ? new SaveSystem.PlayerSaveData { level = startData.level, currentExp = startData.currentExp,
                skillPoints = System.Convert.ToSingle(search["budget"]) - allocated.Sum(Spent) } : startData);
            var tree = playerRoot.AddComponent<SkillTree.MainSkillTree>();
            var inventory = playerRoot.AddComponent<InventorySystem.PlayerInventory>();
            var sourceInventory = UnityEngine.Object.FindAnyObjectByType<InventorySystem.PlayerInventory>(UnityEngine.FindObjectsInactive.Include);
            if (sourceInventory != null) FieldSet(inventory, typeof(InventorySystem.PlayerInventory), "slotCount", sourceInventory.SlotCount);
            foreach (var item in (System.Collections.Generic.List<InventorySystem.InventoryItem>)state["inventory"])
                if (!inventory.TryAddItem(item, out _)) throw new System.InvalidOperationException("Checkpoint inventory could not be restored.");
            var copies = new System.Collections.Generic.Dictionary<SkillTree.Node,SkillTree.Node>();
            SkillTree.Node Copy(SkillTree.Node original) {
                if (copies.TryGetValue(original, out var existing)) return existing;
                SkillTree.Node copy = (SkillTree.Node)playerRoot.AddComponent(original.GetType());
                UnityEngine.JsonUtility.FromJsonOverwrite(UnityEngine.JsonUtility.ToJson(original), copy);
                if (copy is SkillTree.SocketNode socket) {
                    var savedGems = (System.Collections.Generic.Dictionary<string,Gems.GemInstance>)state["socketGems"];
                    socket.SetSocketedGemFromSave(savedGems.ContainsKey(original.SaveId) ? savedGems[original.SaveId] : null);
                }
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
                FieldSet(copy, typeof(SkillTree.LimitedZone), "nodes", members.Where(KnownNode).Select(Copy).ToList());
                FieldSet(copy, typeof(SkillTree.LimitedZone), "_unitLevel", level);
                limitCopies.Add(copy);
            }
            var bonusCopies = new System.Collections.Generic.Dictionary<SkillTree.BonusZone,SkillTree.BonusZone>();
            foreach (var original in originalBonuses) {
                var container = (SkillTree.ModifierContainer)Field(original, typeof(SkillTree.BonusZone), "modContainer");
                if (container == null) continue;
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
            foreach (string id in allocated) copies[byId[id]].SetAllocatedFromSave(true, false, investments.ContainsKey(id) ? investments[id] : 1);
            foreach (var zone in limitCopies) typeof(SkillTree.LimitedZone).GetMethod("EndSaveDataRestoreInternal", privateFlags).Invoke(zone, null);
            RefreshTree();
            Battle.Unit Actor(UnityEngine.GameObject root, Battle.Unit source, bool isPlayer, bool combatActor = true) {
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
                if (combatActor) {
                    FieldSet(actor, typeof(Battle.Unit), "_battleTickSystem", simulatedTick);
                    typeof(Battle.Unit).GetMethod("RegisterToBattleTickSystem", privateFlags).Invoke(actor, null);
                    if (!(bool)Field(actor, typeof(Battle.Unit), "_isRegisteredInBattleTickSystem"))
                        throw new System.InvalidOperationException("Combat actor did not register with the production tick driver.");
                }
                return actor;
            }
            var player = (Battle.PlayerUnit)Actor(playerRoot, sourcePlayer, true);
            string style = (string)strategy["style"], starterId = (string)strategy["starterId"];
            string damageFamily = strategy.ContainsKey("damageFamily") ? (string)strategy["damageFamily"] : "Physical";
            string attribute = strategy.ContainsKey("attribute") ? (string)strategy["attribute"] : "";
            var decisionPlayer = (Battle.PlayerUnit)Actor(NewRoot("BalanceSimulationCampaignDecision"), sourcePlayer, true, false);
            var resetDecision = typeof(Battle.Unit).GetMethod("ResetUnit", privateFlags);
            var statTypes = System.Enum.GetValues(typeof(StatType)).Cast<StatType>().Where(s => s != StatType.Empty).ToArray();
            System.Collections.Generic.Dictionary<StatType,float> ReadStats(Battle.Unit unit) => statTypes.ToDictionary(s => s, s => unit.BaseUnitModifiers.GetStatValue(s));
            var opponentStats = new System.Collections.Generic.Dictionary<StatType,float>();
            var decisionRandom = UnityEngine.Random.state;
            try {
                UnityEngine.Random.InitState(unchecked((int)state["seed"] ^ stage * 104729 ^ 0x1D3C));
                var sampleContext = new Battle.WaveContext(stage, waveQuota, waveQuota);
                if (database.BossBalance != null && database.BossBalance.TryGetRule(sampleContext, out var sampleRule))
                    sampleContext = new Battle.WaveContext(stage, waveQuota, waveQuota, true, sampleRule.BossCount, sampleRule.TotalEnemiesInWave, sampleRule.MaxBossAffixes);
                var samplePackages = new Battle.WaveFactory(new Battle.EnemyFactory(database), database).CreateWave(sampleContext).Take(poolSize).ToArray();
                foreach (var package in samplePackages) {
                    definitionsToDispose.Add(package.Modifiers);
                    var sampleEnemy = (Battle.EnemyUnit)Actor(NewRoot("BalanceSimulationCampaignDecisionEnemy"), sourceEnemy, false, false);
                    sampleEnemy.Initialize(new Battle.EnemySpawnData(package.Definition, package.Rarity, package.Power, 0,
                        package.Modifiers, package.Affixes, package.BaseEvasion, false));
                    sampleEnemy.CombatTick(0f, Battle.CombatTickPhase.Mods);
                    foreach (var stat in statTypes) {
                        if (!opponentStats.ContainsKey(stat)) opponentStats[stat] = 0;
                        opponentStats[stat] += sampleEnemy.BaseUnitModifiers.GetStatValue(stat) / System.Math.Max(1, samplePackages.Length);
                    }
                    DisposeRoot(sampleEnemy.gameObject);
                    if (package.Modifiers != null) UnityEngine.Object.DestroyImmediate(package.Modifiers);
                }
                if (samplePackages.Length == 0) throw new System.InvalidOperationException("No opponents for decision profile.");
            } finally { UnityEngine.Random.state = decisionRandom; }
            var decisionZoneModifiers = new System.Collections.Generic.Dictionary<SkillTree.BonusZone,SkillTree.BaseModifier>();
            System.Collections.Generic.Dictionary<StatType,float> ProjectStats(System.Collections.Generic.IEnumerable<SkillTree.CollectedModifier> additions, SkillTree.Node[] path = null) {
                resetDecision.Invoke(decisionPlayer, null);
                var mods = decisionPlayer.GetAllModifiers(); mods.AddRange(additions);
                if (path != null) foreach (var pair in bonusCopies) {
                    var members = (System.Collections.Generic.List<SkillTree.Node>)Field(pair.Key, typeof(SkillTree.BonusZone), "nodes");
                    int extra = path.Count(n => members.Contains(n) && !copies[n].IsActive);
                    if (extra == 0) continue;
                    var originalBonus = pair.Value.CollectModifier();
                    if (!decisionZoneModifiers.TryGetValue(pair.Key, out var replacement)) {
                        replacement = UnityEngine.Object.Instantiate((SkillTree.BaseModifier)originalBonus);
                        definitionsToDispose.Add(replacement); decisionZoneModifiers[pair.Key] = replacement;
                    }
                    var container = (SkillTree.ModifierContainer)Field(pair.Key, typeof(SkillTree.BonusZone), "modContainer");
                    replacement.modifierContainer = new SkillTree.ModifierContainer(container.modifierType, container.statType,
                        container.value * (pair.Value.AllocatedNodesCount + extra));
                    // Replace rather than append: multiple More factors multiply.
                    mods.RemoveAll(m => m.Modifier == originalBonus);
                    mods.Add(SkillTree.CollectedModifier.WithoutPower(replacement));
                }
                StatCalculator.RecalculateStats(decisionPlayer, mods);
                return ReadStats(decisionPlayer);
            }
            var decisionBase = ProjectStats(System.Array.Empty<SkillTree.CollectedModifier>());
            var decisionMetrics = DecisionMetrics(decisionBase, opponentStats);
            var projectionCache = new System.Collections.Generic.Dictionary<string,double[]>();
            void RefreshDecision() {
                projectionCache.Clear(); decisionBase = ProjectStats(System.Array.Empty<SkillTree.CollectedModifier>());
                decisionMetrics = DecisionMetrics(decisionBase, opponentStats);
            }
            double[] ProjectGain(System.Collections.Generic.IEnumerable<SkillTree.CollectedModifier> additions, SkillTree.Node[] path = null) {
                var next = DecisionMetrics(ProjectStats(additions, path), opponentStats);
                return new[] { System.Math.Log(next[0] / decisionMetrics[0]), System.Math.Log(next[1] / decisionMetrics[1]) };
            }
            double GainScore(double[] gain) {
                double offenceWeight = style == "damage" ? 0.8 : style == "defence" ? 0.2 : 0.5;
                if (focus == "offence" || focus == "speed") offenceWeight = System.Math.Min(0.9, offenceWeight + 0.2);
                if (focus == "health" || focus == "barrier") offenceWeight = System.Math.Max(0.1, offenceWeight - 0.2);
                return 100 * (offenceWeight * gain[0] + (1 - offenceWeight) * gain[1]);
            }
            double[] NodeGain(SkillTree.Node[] path) {
                string key = string.Join("/", path.Select(n => n.SaveId));
                if (!projectionCache.TryGetValue(key, out var gain)) {
                    gain = ProjectGain(path.SelectMany(n => n.Modifiers.Where(m => !n.IsInfinite || SkillTree.InfiniteNode.Supports(m)).Select(m =>
                        new SkillTree.CollectedModifier(m, n.IsInfinite ? SkillTree.ModifierPowerContext.None : SkillTree.ModifierPowerContext.FromNode(Copy(n))))), path);
                    projectionCache[key] = gain;
                }
                return gain;
            }
            bool SupportedGem(Gems.GemDefinition gem) => gem != null && gem.Kind == Gems.GemKind.LocalModifiers &&
                gem.ModifierTemplates.Count > 0 && gem.ModifierTemplates.All(m => Container(m) != null && !Container(m).statType.ToString().Contains("Wisp"));
            double ModifierScore(System.Collections.Generic.IEnumerable<SkillTree.Modifier> modifiers) {
                double score = 0.01;
                foreach (var mod in modifiers) {
                    var c = Container(mod); if (c == null) continue; string stat = c.statType.ToString();
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
                return score;
            }
            double[] GemGain(Gems.GemDefinition gem) {
                string key = "gem/" + UnityEditor.AssetDatabase.GetAssetPath(gem);
                if (!projectionCache.TryGetValue(key, out var gain)) {
                    gain = ProjectGain(gem.ModifierTemplates.Select(SkillTree.CollectedModifier.WithoutPower)); projectionCache[key] = gain;
                }
                return gain;
            }
            double GemScore(Gems.GemDefinition gem) => SupportedGem(gem) ? System.Math.Max(0, GainScore(GemGain(gem))) : 0;
            var policyWallet = new CurrencySystem.PlayerWallet(); policyWallet.ApplySaveData((int)state["gold"]);
            var policyShop = new ShopSystem.ShopService(policyWallet, inventory);
            policyShop.ApplySaveData((System.Collections.Generic.List<SaveSystem.ShopPurchaseSaveData>)state["shopPurchases"]);
            var availableGems = inventory.Slots.Where(s => s.Item?.Gem != null).Select(s => s.Item.Gem.Definition)
                .Concat(locations.Values.Where(l => l.IsShop && l.ShopDefinition != null && (bool)ProgressCall(progress, "IsLocationUnlocked", l))
                    .SelectMany(l => l.ShopDefinition.Entries.Where(e => e.Price <= (int)state["gold"] && policyShop.GetRemainingStock(l.ShopDefinition, e) != 0))
                    .Select(e => e.ItemDefinition as Gems.GemDefinition)).Where(SupportedGem).Distinct().ToArray();
            double HeuristicScore(SkillTree.Node node) {
                double score = ModifierScore(node.Modifiers);
                if (node is SkillTree.SocketNode) {
                    score += availableGems.Select(GemScore).DefaultIfEmpty(0).Max();
                }
                uint hash = unchecked((uint)((int)state["seed"] + variant * 7919));
                foreach (char character in node.SaveId) hash = unchecked(hash * 16777619u ^ character);
                double diversity = variant == 0 ? 1 : 0.75 + (hash % 1000) / 2000d;
                return diversity * score / System.Math.Max(1, NodeCost(node));
            }
            double Score(SkillTree.Node node) => System.Math.Max(0, GainScore(NodeGain(new[] { node }))) /
                System.Math.Max(0.01, NodeCost(node));
            bool growing = false;
            double attackReference = 1, defenceReference = 1;
            bool MajorNode(SkillTree.Node n) => !n.IsInfinite && !(n is SkillTree.RootNode) && !(n is SkillTree.SocketNode) &&
                (n.name.IndexOf("BigNode", System.StringComparison.OrdinalIgnoreCase) >= 0 || n.name.IndexOf("SpecialNode", System.StringComparison.OrdinalIgnoreCase) >= 0 || n.Modifiers.Any(m => Container(m) == null));
            double MajorValue(SkillTree.Node n) {
                int offence = 0, defence = 0;
                foreach (var modifier in n.Modifiers.Where(m => m != null)) {
                    string text = (Container(modifier)?.statType.ToString() ?? modifier.GetType().Name) + " " + modifier.name;
                    if (new[] { "Health", "Armor", "Evasion", "Resistance", "Barrier", "Block", "Parry", "Mitigation", "Guard", "Cleanse", "Healing", "Defence", "DamageTaken", "Scar" }.Any(text.Contains)) defence++;
                    if (new[] { "Damage", "Attack", "Crit", "Accuracy", "Ignite", "Bleed", "Chill", "Overcharge", "Sunder", "Distract", "Expose", "Wisp" }.Any(text.Contains) && !text.Contains("DamageTaken")) offence++;
                }
                double share = offence + defence == 0 ? 0.5 : offence / (double)(offence + defence);
                return 3.25 * (share * attackReference + (1 - share) * defenceReference);
            }
            void CalibrateMajorValues(SkillTree.Node[] frontier) {
                var samples = frontier.Concat(frontier.SelectMany(n => n.ConnectedNodes)).Where(n => n != null && !MajorNode(n) &&
                    !(n is SkillTree.RootNode) && !(n is SkillTree.SocketNode) && !n.IsInfinite && n.Modifiers.All(m => Container(m) != null)).Distinct()
                    .OrderByDescending(HeuristicScore).Take(12).Select(n => NodeGain(new[] { n })).ToArray();
                double Median(double[] values) => values.Length == 0 ? 1 : values.OrderBy(v => v).ElementAt(values.Length / 2);
                attackReference = Median(samples.Select(g => 100 * System.Math.Max(0, g[0]) * (style == "damage" ? 0.8 : style == "defence" ? 0.2 : 0.5)).Where(v => v > 0.000001).ToArray());
                defenceReference = Median(samples.Select(g => 100 * System.Math.Max(0, g[1]) * (style == "defence" ? 0.8 : style == "damage" ? 0.2 : 0.5)).Where(v => v > 0.000001).ToArray());
            }
            bool probeCombatStarted = false;
            int currentWave = 0;
            int ChooseActor(string kind, string[] ids, string[] labels, System.Collections.Generic.IEnumerable<SkillTree.Modifier>[] modifiers,
                double[] priors, double[] costs, bool[] majors, bool[] sockets, bool[] infinites) {
                var damageNames = new[] { StatType.PhysicalDamage, StatType.FireDamage, StatType.ColdDamage, StatType.LightningDamage, StatType.LightDamage, StatType.DarknessDamage };
                int dominant = Enumerable.Range(0, damageNames.Length).OrderByDescending(i => opponentStats[damageNames[i]]).First();
                bool barrier = decisionBase[StatType.BarrierCount] >= 1;
                string context = attribute + "/" + dominant + "/" + (barrier ? "barrier" : "none") + "/" + (decisionBase[StatType.HealthRegenerationPerSecond] > 0 ? "regen" : "none") + "/" + (stage / 20);
                var features = Enumerable.Range(0, ids.Length).Select(i => {
                    string text = string.Join(" ", modifiers[i].Where(m => m != null).Select(m => (Container(m)?.statType.ToString() ?? m.GetType().Name) + " " + m.name));
                    bool offence = new[] { "Damage", "Attack", "Crit", "Accuracy", "Ignite", "Bleed", "Chill", "Overcharge", "Sunder", "Expose" }.Any(text.Contains);
                    bool defence = new[] { "Health", "Armor", "Evasion", "Resistance", "Barrier", "Block", "Parry", "Mitigation", "Guard", "Cleanse", "Healing" }.Any(text.Contains);
                    double attack = offence ? 1 : 0, guard = defence ? 1 : 0;
                    return new[] { 1d, System.Math.Tanh(System.Math.Log(1 + System.Math.Max(0, priors[i])) / 3), majors[i] ? 1d : 0,
                        sockets[i] ? 1d : 0, infinites[i] ? 1d : 0, -System.Math.Min(1, costs[i] / (kind == "node" ? 2 : 250)), attack, guard,
                        guard * (dominant == 0 ? 1 : 0), guard * (dominant >= 1 && dominant <= 3 ? 1 : 0), guard * (dominant >= 4 ? 1 : 0),
                        text.Contains("Barrier") ? (barrier ? 1d : -1d) : 0, text.Contains("Regeneration") ? 1d : 0,
                        attribute.Length > 0 && text.Contains(attribute) ? 1d : 0,
                        attack * System.Math.Min(1, System.Math.Max(0, decisionBase[StatType.CritChance])),
                        guard * System.Math.Tanh(System.Math.Max(0, decisionBase[StatType.Armor]) / System.Math.Max(1, opponentStats[StatType.PhysicalDamage])) };
                }).ToArray();
                var keys = ids.Select(id => context + "/" + id).ToArray();
                var logits = Enumerable.Range(0, ids.Length).Select(i => AgentLogit(priors[i], features[i], learnedWeights, keys[i], learnedOffsets)).ToArray();
                bool training = agentConfig.ContainsKey("training") && (bool)agentConfig["training"];
                double temperature = AgentSetting("temperature", 1), exploration = training ? AgentSetting("exploration", 0.1) : 0;
                int selected = AgentSelect(logits, (System.Random)state["agentRandom"], training, temperature, exploration, out var softmax, out var probabilities);
                ((System.Collections.Generic.List<object>)state["agentLog"]).Add(new { kind, location = location.LocationId, stage, wave = currentWave,
                    context, selected, temperature, exploration, modelId = learnedModel["id"].ToString(),
                    choices = Enumerable.Range(0, ids.Length).Select(i => new { id = ids[i], label = labels[i], key = keys[i], x = features[i],
                        prior = priors[i], probability = probabilities[i], softmax = softmax[i] }).ToArray() });
                return selected;
            }
            // A bounded three-node lookahead can pay for prerequisites and travel nodes.
            double HeuristicRouteScore(SkillTree.Node node) {
                double future = node.ConnectedNodes.Where(n => KnownNode(n) && !allocated.Contains(n.SaveId) && !(n is SkillTree.RootNode))
                    .Select(n => HeuristicScore(n) + 0.35 * n.ConnectedNodes.Where(x => KnownNode(x) && x != node && !allocated.Contains(x.SaveId) && !(x is SkillTree.RootNode))
                        .Select(HeuristicScore).DefaultIfEmpty(0).Max()).DefaultIfEmpty(0).Max();
                return HeuristicScore(node) + (strategy.ContainsKey("lookahead") && (bool)strategy["lookahead"] ? 0.5 * future : 0);
            }
            double RouteScore(SkillTree.Node node) {
                double PathScore(SkillTree.Node[] path) {
                    double cost = path.Sum(NodeCost);
                    double value = System.Math.Max(0, GainScore(NodeGain(path)));
                    // Owner's requested directional estimate for major/special mechanics.
                    value = System.Math.Max(value, path.Where(MajorNode).Sum(MajorValue));
                    // Empty sockets gain value only from a currently obtainable, useful gem.
                    if (path.Any(n => n is SkillTree.SocketNode)) value += availableGems.Select(GemScore).DefaultIfEmpty(0).Max();
                    return value / System.Math.Max(0.01, cost);
                }
                double best = PathScore(new[] { node });
                if (strategy.ContainsKey("lookahead") && (bool)strategy["lookahead"]) {
                    var next = node.ConnectedNodes.Where(n => KnownNode(n) && !(n is SkillTree.RootNode) && !allocated.Contains(n.SaveId) && !Copy(n).IsLocked)
                        .OrderByDescending(HeuristicScore).Take(2);
                    foreach (var second in next) {
                        best = System.Math.Max(best, 0.8 * PathScore(new[] { node, second }));
                        foreach (var third in second.ConnectedNodes.Where(n => n != node && KnownNode(n) && !(n is SkillTree.RootNode) && !allocated.Contains(n.SaveId) && !Copy(n).IsLocked)
                            .OrderByDescending(HeuristicScore).Take(1))
                            best = System.Math.Max(best, 0.6 * PathScore(new[] { node, second, third }));
                    }
                }
                return best;
            }
            void Grow() {
                if (isProbe && (probeIndex == 0 || probeCombatStarted)) return;
                if (growing) return; growing = true;
                try {
                    for (int safety = 0; safety < 1000 && level.SkillPoints > 0; safety++) {
                        var frontier = eligible.Where(n => !(n is SkillTree.RootNode) && (!allocated.Contains(n.SaveId) || n.IsInfinite) &&
                            n.ConnectedNodes.Any(next => next != null && copies.ContainsKey(next) && copies[next].IsActive)).ToArray();
                        foreach (var original in frontier) Copy(original);
                        RefreshTree();
                        var candidates = frontier.Where(n => copies[n].CanBeAllocated() && copies[n].HasEnoughSkillPoints()).ToArray();
                        if (allocated.Count == 0) candidates = candidates.Where(n => n.SaveId == starterId).ToArray();
                        RefreshDecision();
                        CalibrateMajorValues(candidates);
                        var ranked = candidates.OrderByDescending(HeuristicRouteScore).ThenBy(n => n.SaveId, System.StringComparer.Ordinal).Take(agentConfig == null ? 8 : 0)
                            .Concat(agentConfig == null ? candidates.Where(MajorNode) : Enumerable.Empty<SkillTree.Node>()).Distinct()
                            .Select(n => new { node = n, score = RouteScore(n) }).OrderByDescending(n => n.score).ThenBy(n => n.node.SaveId, System.StringComparer.Ordinal).ToArray();
                        var choice = ranked.FirstOrDefault();
                        var next = choice?.node;
                        if (agentConfig != null) {
                            bool canWait = allocated.Count > 0;
                            int count = candidates.Length + (canWait ? 1 : 0);
                            if (count == 0) break;
                            var priors = candidates.Select(n => System.Math.Max(0, HeuristicRouteScore(n) + (MajorNode(n) ? MajorValue(n) / System.Math.Max(0.01, NodeCost(n)) : 0))).Concat(canWait ? new[] { 0d } : new double[0]).ToArray();
                            int selected = ChooseActor("node", candidates.Select(n => "node:" + n.SaveId).Concat(canWait ? new[] { "wait" } : new string[0]).ToArray(),
                                candidates.Select(n => n.name).Concat(canWait ? new[] { "Keep skill points" } : new string[0]).ToArray(),
                                candidates.Select(n => (System.Collections.Generic.IEnumerable<SkillTree.Modifier>)n.Modifiers).Concat(canWait ? new[] { Enumerable.Empty<SkillTree.Modifier>() } : new System.Collections.Generic.IEnumerable<SkillTree.Modifier>[0]).ToArray(),
                                priors, candidates.Select(n => (double)NodeCost(n)).Concat(canWait ? new[] { 0d } : new double[0]).ToArray(),
                                candidates.Select(MajorNode).Concat(canWait ? new[] { false } : new bool[0]).ToArray(),
                                candidates.Select(n => n is SkillTree.SocketNode).Concat(canWait ? new[] { false } : new bool[0]).ToArray(),
                                candidates.Select(n => n.IsInfinite).Concat(canWait ? new[] { false } : new bool[0]).ToArray());
                            if (selected == candidates.Length) break;
                            next = candidates[selected]; choice = new { node = next, score = priors[selected] };
                        }
                        if (next == null) break;
                        // Keep points for a useful reachable route instead of buying a dead stat.
                        if (agentConfig == null && choice.score <= 0.000001 && allocated.Count > 0) break;
                        var estimatedGain = NodeGain(new[] { next });
                        float before = level.SkillPoints;
                        if (!copies[next].Allocate()) throw new System.InvalidOperationException("Allocation predicate changed unexpectedly.");
                        if (!allocated.Contains(next.SaveId)) allocated.Add(next.SaveId);
                        investments[next.SaveId] = copies[next].InvestedSkillPoints;
                        RefreshTree(); player.RequestModRecalculation();
                        if (!isProbe) allocations.Add(new { revision = (int)state["revision"], location = location.LocationId, stage, wave = currentWave, level = level.Level, nodeId = next.SaveId,
                            modifiers = next.Modifiers.Select(m => m.name).ToArray(), pointsSpent = before - level.SkillPoints, pointsRemaining = level.SkillPoints,
                            decisionScore = choice.score, estimatedOffenceGain = System.Math.Exp(estimatedGain[0]) - 1,
                            estimatedDefenceGain = System.Math.Exp(estimatedGain[1]) - 1, decisionPolicy = "counterfactual-v3-directional",
                            directionalMajorValue = MajorNode(next) ? MajorValue(next) : 0, majorNodeFactor = MajorNode(next) ? 3.25 : 0 });
                    }
                } finally { growing = false; }
            }
            level.OnSkillPointsChanged += unused => Grow();
            level.OnLevelUp += unused => Grow();
            Grow(); player.RequestModRecalculation(); player.CombatTick(0f, Battle.CombatTickPhase.Mods); player.ResetCombatState();
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
            void EquipAndShop() {
                if (isProbe) return;
                var socketService = new InventorySystem.InventorySocketService();
                var wallet = new CurrencySystem.PlayerWallet(); wallet.ApplySaveData((int)state["gold"]);
                var shopService = new ShopSystem.ShopService(wallet, inventory);
                shopService.ApplySaveData((System.Collections.Generic.List<SaveSystem.ShopPurchaseSaveData>)state["shopPurchases"]);
                var log = (System.Collections.Generic.List<object>)state["shopLog"];
                foreach (var socket in copies.Values.OfType<SkillTree.SocketNode>().Where(s => s.IsActive && !s.HasGem).OrderBy(s => s.SaveId)) {
                    RefreshDecision();
                    int bestSlot = Enumerable.Range(0, inventory.SlotCount)
                        .Where(i => SupportedGem(inventory.PeekItem(i)?.Gem?.Definition) && (agentConfig != null || GemScore(inventory.PeekItem(i).Gem.Definition) > 0.000001))
                        .OrderByDescending(i => GemScore(inventory.PeekItem(i).Gem.Definition)).ThenBy(i => i).DefaultIfEmpty(-1).First();
                    if (agentConfig != null && bestSlot >= 0) {
                        var slots = Enumerable.Range(0, inventory.SlotCount).Where(i => SupportedGem(inventory.PeekItem(i)?.Gem?.Definition)).ToArray();
                        var gems = slots.Select(i => inventory.PeekItem(i).Gem.Definition).ToArray();
                        int selected = ChooseActor("inventory-gem", gems.Select(g => "gem:" + g.SaveDefinitionId).Concat(new[] { "wait-gem" }).ToArray(),
                            gems.Select(g => g.name).Concat(new[] { "Keep socket empty" }).ToArray(),
                            gems.Select(g => (System.Collections.Generic.IEnumerable<SkillTree.Modifier>)g.ModifierTemplates).Concat(new[] { Enumerable.Empty<SkillTree.Modifier>() }).ToArray(),
                            gems.Select(GemScore).Concat(new[] { 0d }).ToArray(), new double[gems.Length + 1], new bool[gems.Length + 1],
                            Enumerable.Repeat(true, gems.Length + 1).ToArray(), new bool[gems.Length + 1]);
                        if (selected == gems.Length) continue;
                        bestSlot = slots[selected];
                    }
                    if (bestSlot < 0) {
                        var offers = locations.Values.Where(l => l.IsShop && l.ShopDefinition != null && (bool)ProgressCall(progress, "IsLocationUnlocked", l))
                            .SelectMany(l => l.ShopDefinition.Entries.Select(e => new { location = l, entry = e, gem = e.ItemDefinition as Gems.GemDefinition }))
                            .Where(o => SupportedGem(o.gem) && (agentConfig != null || GemScore(o.gem) > 0.000001) && o.entry.Price <= wallet.Gold && shopService.GetRemainingStock(o.location.ShopDefinition, o.entry) != 0)
                            .OrderByDescending(o => GemScore(o.gem) / System.Math.Max(1, o.entry.Price))
                            .ThenBy(o => o.location.LocationId, System.StringComparer.Ordinal).ThenBy(o => o.entry.EntryId, System.StringComparer.Ordinal).ToArray();
                        if (agentConfig != null) {
                            offers = offers.Where(o => (bool)typeof(ShopSystem.ShopService).GetMethod("CanAddToInventory", privateFlags).Invoke(shopService, new object[] { o.entry.CreateItem() })).ToArray();
                            if (offers.Length == 0) continue;
                            int selected = ChooseActor("shop-gem", offers.Select(o => "gem:" + o.gem.SaveDefinitionId).Concat(new[] { "wait-gem" }).ToArray(),
                                offers.Select(o => o.gem.name).Concat(new[] { "Save gold" }).ToArray(),
                                offers.Select(o => (System.Collections.Generic.IEnumerable<SkillTree.Modifier>)o.gem.ModifierTemplates).Concat(new[] { Enumerable.Empty<SkillTree.Modifier>() }).ToArray(),
                                offers.Select(o => GemScore(o.gem) / System.Math.Max(1, o.entry.Price)).Concat(new[] { 0d }).ToArray(),
                                offers.Select(o => (double)o.entry.Price).Concat(new[] { 0d }).ToArray(), new bool[offers.Length + 1], Enumerable.Repeat(true, offers.Length + 1).ToArray(), new bool[offers.Length + 1]);
                            if (selected == offers.Length) continue;
                            offers = offers.Skip(selected).Take(1).ToArray();
                        }
                        foreach (var offer in offers) {
                            if (!(bool)ProgressCall(progress, "TrySelectLocation", offer.location.LocationId)) continue;
                            log.Add(new { action = "visit", location = offer.location.LocationId, stage, gold = wallet.Gold });
                            var estimatedGain = GemGain(offer.gem);
                            bool bought = shopService.TryBuy(offer.location.ShopDefinition, offer.entry, out var transaction);
                            log.Add(new { action = "buy", location = offer.location.LocationId, stage, item = offer.gem.name,
                                price = offer.entry.Price, amount = offer.entry.Amount, success = bought, result = transaction.ToString(), gold = wallet.Gold,
                                decisionScore = GemScore(offer.gem), estimatedOffenceGain = System.Math.Exp(estimatedGain[0]) - 1,
                                estimatedDefenceGain = System.Math.Exp(estimatedGain[1]) - 1, decisionPolicy = "counterfactual-v2" });
                            if (!(bool)ProgressCall(progress, "TrySelectLocation", location.LocationId)) throw new System.InvalidOperationException("Could not return from shop.");
                            ProgressCall(progress, "SetSelectedLevel", stage);
                            if (!bought) continue;
                            bestSlot = Enumerable.Range(0, inventory.SlotCount).First(i => inventory.PeekItem(i)?.Gem?.Definition == offer.gem);
                            break;
                        }
                    }
                    if (bestSlot < 0) continue;
                    string gemName = inventory.PeekItem(bestSlot).Gem.Definition.name;
                    if (!socketService.TryInsertGem(inventory, bestSlot, socket)) throw new System.InvalidOperationException("Gem insertion failed: " + socketService.FailureReason);
                    log.Add(new { action = "insert", location = location.LocationId, stage, socketId = socket.SaveId, item = gemName });
                }
                state["gold"] = wallet.Gold; state["shopPurchases"] = shopService.CaptureSaveData();
                state["socketGems"] = copies.Values.OfType<SkillTree.SocketNode>().Where(s => s.HasGem).ToDictionary(s => s.SaveId, s => s.SavedSocketedGem);
                RefreshTree(); player.RequestModRecalculation();
            }
            EquipAndShop(); player.CombatTick(0f, Battle.CombatTickPhase.Mods); player.ResetCombatState();
            void Snapshot(string boundary) {
                // Only at existing reset boundaries. No combat effects contaminate the build stats.
                player.RequestModRecalculation(); player.CombatTick(0f, Battle.CombatTickPhase.Mods);
                var stats = System.Enum.GetValues(typeof(StatType)).Cast<StatType>().Where(s => s != StatType.Empty)
                    .ToDictionary(s => s.ToString(), s => player.BaseUnitModifiers.GetStatValue(s));
                ((System.Collections.Generic.List<object>)state["snapshots"]).Add(new {
                    boundary, location = location.LocationId, stage, attempt = stageAttempt,
                    level = level.Level, experience = level.CurrentExp, freePoints = level.SkillPoints, stats,
                    gold = (int)state["gold"], shopCount = ((System.Collections.Generic.List<object>)state["shopLog"]).Count,
                    socketGems = copies.Values.OfType<SkillTree.SocketNode>().Where(s => s.HasGem).Select(s => new { socketId = s.SaveId, item = s.SavedSocketedGem.Definition.name, active = s.IsGemActive }).ToArray(),
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
            if ((string)request["operation"] == "verify-contract") {
                float before = player.BaseUnitModifiers.GetStatValue(StatType.MaximumHealth);
                var diagnosticModifier = UnityEngine.ScriptableObject.CreateInstance<SkillTree.BaseModifier>();
                diagnosticModifier.modifierContainer = new SkillTree.ModifierContainer(ModifierType.Added, StatType.MaximumHealth, 17);
                definitionsToDispose.Add(diagnosticModifier);
                player.AddOuterModifier(diagnosticModifier);
                bool deferred = player.BaseUnitModifiers.GetStatValue(StatType.MaximumHealth) == before &&
                    (bool)Field(player, typeof(Battle.Unit), "_modsChangedPending");
                runTick(0f);
                bool appliedInMods = player.BaseUnitModifiers.GetStatValue(StatType.MaximumHealth) > before &&
                    !(bool)Field(player, typeof(Battle.Unit), "_modsChangedPending");
                player.RemoveOuterModifier(diagnosticModifier); runTick(0f);
                var diagnosticPackages = new Battle.WaveFactory(new Battle.EnemyFactory(database), database).CreateWave(new Battle.WaveContext(stage, 1, waveQuota));
                foreach (var package in diagnosticPackages) definitionsToDispose.Add(package.Modifiers);
                var diagnosticPackage = diagnosticPackages.First();
                var diagnosticEnemy = (Battle.EnemyUnit)Actor(NewRoot("BalanceSimulationCampaignDiagnosticEnemy"), sourceEnemy, false);
                FieldSet(diagnosticEnemy, typeof(Battle.EnemyUnit), "_playerTarget", player);
                diagnosticEnemy.Initialize(new Battle.EnemySpawnData(diagnosticPackage.Definition, diagnosticPackage.Rarity, diagnosticPackage.Power, 0,
                    diagnosticPackage.Modifiers, diagnosticPackage.Affixes, diagnosticPackage.BaseEvasion, false));
                diagnosticEnemy.CombatTick(0f, Battle.CombatTickPhase.Mods); diagnosticEnemy.ResetCombatState();
                int killNotifications = 0; player.OnEnemyKilled += unused => killNotifications++;
                var lethal = new Battle.DamageInstance(); lethal.Damage[Battle.DamageType.Physical] = diagnosticEnemy.health.MaxHealth + 1;
                diagnosticEnemy.health.TakeDamage(lethal, false);
                if (!deferred || !appliedInMods || killNotifications != 1)
                    throw new System.InvalidOperationException("Simulator contract diagnostic failed.");
                return new { deferredModifiers = deferred, appliedInProductionModsPhase = appliedInMods, killNotifications,
                    tickDuration = step, gameplayCodeModified = false };
            }
            probeCombatStarted = true;
            var resolverRoot = NewRoot("BalanceSimulationCampaignTargets"); resolverRoot.SetActive(true);
            var resolver = resolverRoot.AddComponent<Battle.AttackResolver>(); player.attacker.SetTarget(resolver);
            var goldResolver = new DropSystem.GoldDropResolver(database.GoldDropConfig);
            bool stageWon = true, wasBoss = false;
            int completedWaves = 0, probeKills = 0;
            double damagedFraction = 0, stageSeconds = 0;
            string proposalSignature = string.Join("|", allocated.OrderBy(id => id, System.StringComparer.Ordinal)
                .Select(id => id + ":" + (investments.ContainsKey(id) ? investments[id] : 1)));
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
                    FieldSet(enemy, typeof(Battle.EnemyUnit), "_playerTarget", player);
                    enemy.Initialize(new Battle.EnemySpawnData(package.Definition, package.Rarity, package.Power, isProbe ? 0 : package.ExperienceReward,
                        package.Modifiers, package.Affixes, package.BaseEvasion, false));
                    enemy.CombatTick(0f, Battle.CombatTickPhase.Mods); enemy.ResetCombatState();
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
                int ticks = 0;
                for (; ticks < System.Math.Ceiling(Number(campaignConfig, "maxWaveSeconds", 180) / step) && player.gameObject.activeSelf && enemies.Any(e => e.gameObject.activeSelf); ticks++)
                    runTick(step);
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
                        runTick(step);
                    if (!player.gameObject.activeSelf) { stageWon = false; if (!isProbe) { state["deaths"] = (int)state["deaths"] + 1; state["reason"] = "Death during respawn delay"; } break; }
                }
            }
            int combatDecisionCount = ((System.Collections.Generic.List<object>)state["agentLog"]).Count;
            if (isProbe) {
                if (level.Level != startData.level || level.CurrentExp != startData.currentExp) throw new System.InvalidOperationException("Probe changed progression.");
                double fitness = completedWaves + System.Math.Max(0, System.Math.Min(1, damagedFraction));
                bool improved = !duplicateProposal && (stageWon || fitness > (double)search["bestFitness"] + 0.000001);
                if (probeIndex == 0) {
                    search["baselineFitness"] = fitness; ((System.Collections.Generic.HashSet<string>)search["seen"]).Add(proposalSignature);
                }
                if (improved) {
                    search["bestFitness"] = fitness; search["bestNodes"] = new System.Collections.Generic.List<string>(allocated);
                    search["bestInvestments"] = new System.Collections.Generic.Dictionary<string,int>(investments);
                    search["bestFree"] = level.SkillPoints; search["bestFocus"] = focus; search["bestVariant"] = variant;
                }
                player.ResetCombatState(); Snapshot("after-probe");
                ((System.Collections.Generic.List<object>)state["searchLog"]).Add(new {
                    action = "evaluate", location = location.LocationId, stage, round = (int)search["round"], candidate = probeIndex,
                    focus, variant, level = startData.level, experience = startData.currentExp, budget = System.Convert.ToSingle(search["budget"]),
                    waveSeedAttempt = (int)search["seedAttempt"], duplicate = duplicateProposal, won = stageWon, completedWaves, probeKills,
                    fitness, baselineFitness = (double)search["baselineFitness"], delta = fitness - (double)search["baselineFitness"], improved,
                    simulatedSeconds = stageSeconds, freePoints = level.SkillPoints,
                    added = allocated.Except(baseNodes).ToArray(), removed = baseNodes.Except(allocated).ToArray(),
                    nodeIds = allocated.ToArray(), snapshotIndex = ((System.Collections.Generic.List<object>)state["snapshots"]).Count - 1
                });
                search["next"] = probeIndex + 1;
                if (stageWon || probeIndex >= Number(campaignConfig, "adaptiveCandidates", 4)) {
                    var bestNodes = (System.Collections.Generic.List<string>)search["bestNodes"];
                    var bestInvestments = (System.Collections.Generic.Dictionary<string,int>)search["bestInvestments"];
                    bool changedBuild = !new System.Collections.Generic.HashSet<string>(baseNodes).SetEquals(bestNodes) ||
                        bestNodes.Any(id => byId[id].IsInfinite && bestInvestments.ContainsKey(id) &&
                            (!((System.Collections.Generic.Dictionary<string,int>)state["investments"]).ContainsKey(id) ||
                            bestInvestments[id] != ((System.Collections.Generic.Dictionary<string,int>)state["investments"])[id]));
                    state["investments"] = bestInvestments;
                    state["allocated"] = bestNodes; state["focus"] = search["bestFocus"]; state["variant"] = search["bestVariant"];
                    startData.skillPoints = System.Convert.ToSingle(search["bestFree"]);
                    if (changedBuild) {
                        state["revision"] = (int)state["revision"] + 1;
                        foreach (string id in bestNodes) allocations.Add(new { revision = (int)state["revision"], location = location.LocationId, stage,
                            wave = 0, level = startData.level, nodeId = id, modifiers = byId[id].Modifiers.Select(m => m.name).ToArray(),
                            pointsSpent = System.Convert.ToSingle(Field(byId[id], typeof(SkillTree.Node), "nodeCost")), pointsRemaining = startData.skillPoints });
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
            bool newCompletion = false;
            if (stageWon) {
                object[] completedArgs = { 0 };
                bool firstCompletion = (bool)ProgressCall(progress, "RegisterCompletedLevel", completedArgs);
                newCompletion = firstCompletion;
                player.ResetCombatState(); ProgressCall(progress, "UnlockNextLevel");
                state["highestCompletedStage"] = System.Math.Max((int)state["highestCompletedStage"], stage);
                if (wasBoss && firstCompletion) {
                    var pending = (System.Collections.Generic.List<Battle.PendingLocationReward>)ProgressCall(progress, "GetPendingLocationRewards", stage);
                    foreach (var reward in pending) {
                        bool delivered = reward.IsGold || inventory.TryAddItem(reward.Item, out _);
                        // Match the currently inspected completion window, including BL-001 on insertion failure.
                        bool claimed = (bool)ProgressCall(progress, "TryClaimReward", reward);
                        if (reward.IsGold && claimed) state["gold"] = (int)state["gold"] + reward.GoldAmount;
                        rewards.Add(new { action = "claim", location = location.LocationId, stage, rewardId = reward.RewardId,
                            item = reward.IsGold ? "Gold" : reward.Item.ItemDefinition.name, amount = reward.IsGold ? reward.GoldAmount : reward.Item.StackCount, delivered, claimed });
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
            if (agentConfig != null) ((System.Collections.Generic.List<object>)state["agentRewards"]).Add(new {
                cursor = combatDecisionCount, location = location.LocationId, stage, won = stageWon, newCompletion,
                amount = (newCompletion ? AgentSetting("levelReward", 1) : 0) +
                    (newCompletion && stage >= database.MaxWaveLevel ? AgentSetting("locationReward", 5) : 0) -
                    (!stageWon ? AgentSetting("failurePenalty", 0.1) : 0) });
            if (agentConfig == null || (string)state["status"] == "running") EquipAndShop();
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
                    { "budget", level.SkillPoints + allocated.Sum(Spent) },
                    { "bestNodes", new System.Collections.Generic.List<string>(allocated) }, { "bestFree", level.SkillPoints },
                    { "bestInvestments", new System.Collections.Generic.Dictionary<string,int>(investments) },
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
    nodeInvestments = state["investments"],
    agentLog = state["agentLog"], agentRewards = state["agentRewards"],
    kills = (int)state["kills"], gold = (int)state["gold"], simulatedSeconds = (double)state["simulatedSeconds"],
    inventory = ((System.Collections.Generic.List<InventorySystem.InventoryItem>)state["inventory"]).Select(i => new { item = i.ItemDefinition.name, amount = i.StackCount }).ToArray(),
    waveLog = (System.Collections.Generic.List<object>)state["waveLog"], allocationLog = (System.Collections.Generic.List<object>)state["allocationLog"], rewardLog = (System.Collections.Generic.List<object>)state["rewardLog"],
    snapshots = (System.Collections.Generic.List<object>)state["snapshots"]
    , shopLog = (System.Collections.Generic.List<object>)state["shopLog"], shopPurchases = state["shopPurchases"],
    socketGems = ((System.Collections.Generic.Dictionary<string,Gems.GemInstance>)state["socketGems"]).Select(p => new { socketId = p.Key, item = p.Value.Definition.name }).ToArray()
    , searchLog = (System.Collections.Generic.List<object>)state["searchLog"], trialWaves = (int)state["trialWaves"],
    trialSimulatedSeconds = (double)state["trialSeconds"], allocationRevision = (int)state["revision"], learnedFocus = (string)state["focus"]
}).ToArray() : null;
// Keep data until the wrapper confirms per-campaign files were written, then discard.
var response = new { schemaVersion = 2, mode = "progression-campaign", finished, completedRuns = (int)checkpoint["cursor"], totalRuns = campaignStates.Count,
    stagesThisCommand, computeSeconds = (double)checkpoint["computeSeconds"], elapsedSeconds = watch.Elapsed.TotalSeconds,
    sceneDirtyBefore = dirtyBefore, sceneDirtyAfter = UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty, campaigns = results,
    simulationContract = new { version = "production-tick-v1", tickDuration = step, killTargetBound = true, manualCombatPhaseLoop = false },
    treeCatalog = exportCatalog ? originals.Select(n => new {
        id = n.SaveId, name = n.name, type = n.GetType().Name, supported = KnownNode(n), root = n is SkillTree.RootNode,
        x = sourcePlayer.SkillTree.transform.InverseTransformPoint(n.transform.position).x,
        y = sourcePlayer.SkillTree.transform.InverseTransformPoint(n.transform.position).y,
        cost = n is SkillTree.RootNode ? 0 : n.IsInfinite ? 1 : System.Convert.ToSingle(Field(n, typeof(SkillTree.Node), "nodeCost")),
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
