using System;
using Battle;
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
    }

    // New integrations belong here (or in another adapter), not in the presentation/service.
    public sealed class TutorialEventAdapter : IInitializable, ITickable, IDisposable
    {
        private readonly TutorialService tutorials;
        private readonly PlayerUnit player;
        private readonly UnitLevel level;
        private readonly EnemySpawner spawner;
        private bool observedBattle;

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
        }

        public void Dispose()
        {
            level.OnLevelUp -= OnPlayerLevel;
            spawner.OnLevelChanged -= OnLocationLevel;
            spawner.OnWaveClearedNumber -= OnWave;
            player.OnGettingHit -= OnHit;
            player.effectController.OnEffectAdded -= OnEffectAdded;
        }

        public void Tick()
        {
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
