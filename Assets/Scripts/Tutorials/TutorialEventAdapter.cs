using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Battle;
using Battle.MiniGames;
using SkillTree;
using Zenject;

namespace Tutorials
{
    public static class TutorialEvents
    {
        public const string PlayerLevel = "player.level";
        public const string LocationLevel = "location.level";
        public const string WaveCleared = "wave.cleared";
        public const string WorldMapOpened = "world_map.opened";
        public const string ElementalHit = "player.hit.elemental";
        public const string MysticHit = "player.hit.mystic";
        public const string AilmentReceived = "player.ailment.received";
        public const string TravelNodeAllocated = "tree.travel.allocated";
        public const string MysticNodeInvested = "tree.mystic.invested";
        public const string MiniGameSpawned = "minigame.spawned";
    }

    // New integrations belong here (or in another adapter), not in the presentation/service.
    public sealed class TutorialEventAdapter : IInitializable, ITickable, IDisposable
    {
        private readonly TutorialService tutorials;
        private readonly PlayerUnit player;
        private readonly UnitLevel level;
        private readonly EnemySpawner spawner;
        private bool observedBattle;
        private BattleMiniGameController miniGames;

        public TutorialEventAdapter(TutorialService tutorials, PlayerUnit player, UnitLevel level, EnemySpawner spawner)
        {
            this.tutorials = tutorials;
            this.player = player;
            this.level = level;
            this.spawner = spawner;
        }

        public void Initialize()
        {
            level.OnLevelUp += OnPlayerLevel;
            spawner.OnLevelChanged += OnLocationLevel;
            spawner.OnWaveClearedNumber += OnWave;
            player.OnGettingHit += OnHit;
            player.effectController.OnEffectAdded += OnEffectAdded;
            Node.OnAnyNodeAllocatedChanged += OnNodeAllocated;
            Node.OnAnyNodeInvested += OnNodeInvested;
            ObserveMiniGameController();
        }

        public void Dispose()
        {
            level.OnLevelUp -= OnPlayerLevel;
            spawner.OnLevelChanged -= OnLocationLevel;
            spawner.OnWaveClearedNumber -= OnWave;
            player.OnGettingHit -= OnHit;
            player.effectController.OnEffectAdded -= OnEffectAdded;
            Node.OnAnyNodeAllocatedChanged -= OnNodeAllocated;
            Node.OnAnyNodeInvested -= OnNodeInvested;
            if (miniGames != null) miniGames.EventSpawned -= OnMiniGameSpawned;
        }

        public void Tick()
        {
            ObserveMiniGameController();
            // Catch an already active battle after initialization/save loading, too.
            if (!tutorials.IsReady || !spawner.IsBattleActive)
            {
                observedBattle = false;
                return;
            }
            if (observedBattle) return;
            observedBattle = true;
            OnLocationLevel();
        }

        private void OnPlayerLevel(int value) => tutorials.Report(TutorialEvents.PlayerLevel, value, spawner.SelectedLocationId);
        private void ObserveMiniGameController()
        {
            var controller = BattleMiniGameController.For(player);
            if (ReferenceEquals(controller, miniGames)) return;
            if (miniGames != null) miniGames.EventSpawned -= OnMiniGameSpawned;
            miniGames = controller;
            if (miniGames != null) miniGames.EventSpawned += OnMiniGameSpawned;
        }

        private void OnMiniGameSpawned(BattleMiniGameEventDefinition definition) =>
            tutorials.Report(TutorialEvents.MiniGameSpawned, eventLocationId: spawner.SelectedLocationId);

        private void OnNodeAllocated(Node node)
        {
            if (node.IsAllocated && !node.IsApplyingSavedState && node.NodeType == NodeType.Travel)
                tutorials.Report(TutorialEvents.TravelNodeAllocated, eventLocationId: spawner.SelectedLocationId);
        }

        private void OnNodeInvested(Node node)
        {
            if (node.Modifiers == null) return;
            var visited = new HashSet<Modifier>();
            foreach (var modifier in node.Modifiers)
            {
                if (!HasMysticRule(modifier, visited)) continue;
                tutorials.Report(TutorialEvents.MysticNodeInvested, eventLocationId: spawner.SelectedLocationId);
                return;
            }
        }

        // Inspect authored data only: applying modifiers or creating effects here
        // would mutate combat state. Serialized fields also cover conditional containers.
        private static bool HasMysticRule(Modifier modifier, HashSet<Modifier> visited)
        {
            if (modifier == null || !visited.Add(modifier)) return false;
            if (modifier is AddedFireDamageConvertedToLightDamage
                || modifier is AddedColdDamageConvertedToDarknessDamage)
                return true;

            for (var type = modifier.GetType(); type != null && type != typeof(Modifier); type = type.BaseType)
            {
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public
                    | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (field.IsStatic || field.IsNotSerialized
                        || (!field.IsPublic && !field.IsDefined(typeof(UnityEngine.SerializeField), false)))
                        continue;
                    var value = field.GetValue(modifier);
                    if (value is ModifierContainer container && IsMysticContainer(container)) return true;
                    if (value is StatType stat && IsMysticStat(stat)) return true;
                    if (!(value is IList list)) continue;
                    foreach (var item in list)
                    {
                        if (item is ModifierContainer entry && IsMysticContainer(entry)) return true;
                        if (item is Modifier nested && HasMysticRule(nested, visited)) return true;
                    }
                }
            }
            return false;
        }

        private static bool IsMysticContainer(ModifierContainer container)
        {
            if (container == null) return false;
            if (IsMysticStat(container.statType)) return true;
            return container.statType == StatType.BarrierDamageTypeMask
                && (((DamageType)UnityEngine.Mathf.RoundToInt(container.value)
                    & (DamageType.Light | DamageType.Darkness)) != 0);
        }

        private static bool IsMysticStat(StatType stat)
        {
            return stat == StatType.MysticDamage || stat == StatType.LightDamage
                || stat == StatType.DarknessDamage || stat == StatType.MysticNegation
                || stat == StatType.ArmorAsMysticNegation
                || stat == StatType.MysticCleansePerSecond || stat == StatType.MysticDamageMitigation
                || stat == StatType.LightDamageMitigation || stat == StatType.DarknessDamageMitigation;
        }
        private void OnLocationLevel()
        {
            if (spawner.IsBattleActive)
                tutorials.Report(TutorialEvents.LocationLevel, spawner.SelectedLevel, spawner.SelectedLocationId);
        }
        private void OnWave(int number) => tutorials.Report(TutorialEvents.WaveCleared, number, spawner.SelectedLocationId);

        private void OnEffectAdded(BaseEffect effect)
        {
            if (effect is Bleed || effect is Ignite || effect is Chill || effect is Overcharge)
                tutorials.Report(TutorialEvents.AilmentReceived, eventLocationId: spawner.SelectedLocationId);
        }

        private void OnHit(DamageInfo hit)
        {
            if (!(hit.Owner is EnemyUnit)) return;
            bool elemental = false;
            bool mystic = false;
            foreach (var damage in hit.DamageInstance.Damage)
            {
                if (damage.Value <= 0f) continue;
                elemental |= (damage.Key & (DamageType.Fire | DamageType.Cold | DamageType.Lightning)) != 0;
                mystic |= (damage.Key & (DamageType.Light | DamageType.Darkness)) != 0;
            }
            if (elemental) tutorials.Report(TutorialEvents.ElementalHit, eventLocationId: spawner.SelectedLocationId);
            if (mystic) tutorials.Report(TutorialEvents.MysticHit, eventLocationId: spawner.SelectedLocationId);
        }
    }
}
