using UnityEngine;
using System.Collections.Generic;

namespace Battle.MiniGames
{
    public sealed class RandomBattleMiniGameSpawner : MonoBehaviour
    {
        [SerializeField] private BattleMiniGameController controller;

        private readonly Dictionary<BattleMiniGameEventDefinition, float> _timeLeftByEvent =
            new Dictionary<BattleMiniGameEventDefinition, float>();

        private void Awake()
        {
            if (controller == null)
            {
                controller = GetComponent<BattleMiniGameController>();
            }
        }

        private void OnEnable()
        {
            _timeLeftByEvent.Clear();
        }

        private void Update()
        {
            if (controller == null || !controller.IsBattleActive)
            {
                return;
            }

            List<BattleMiniGameEventDefinition> events = controller.GetAvailableEvents();
            PruneUnavailableTimers(events);

            for (int i = 0; i < events.Count; i++)
            {
                BattleMiniGameEventDefinition definition = events[i];
                if (definition == null || !definition.RandomSpawnEnabled)
                {
                    continue;
                }

                if (!_timeLeftByEvent.ContainsKey(definition))
                {
                    ResetTimer(definition);
                    continue;
                }

                _timeLeftByEvent[definition] -= Time.deltaTime;
                if (_timeLeftByEvent[definition] > 0f)
                {
                    continue;
                }

                controller.TrySpawnRandom(definition);
                ResetTimer(definition);
            }
        }

        private void PruneUnavailableTimers(List<BattleMiniGameEventDefinition> availableEvents)
        {
            List<BattleMiniGameEventDefinition> keysToRemove = null;
            foreach (BattleMiniGameEventDefinition definition in _timeLeftByEvent.Keys)
            {
                if (definition != null && definition.RandomSpawnEnabled && availableEvents.Contains(definition))
                {
                    continue;
                }

                keysToRemove ??= new List<BattleMiniGameEventDefinition>();
                keysToRemove.Add(definition);
            }

            if (keysToRemove == null)
            {
                return;
            }

            for (int i = 0; i < keysToRemove.Count; i++)
            {
                _timeLeftByEvent.Remove(keysToRemove[i]);
            }
        }

        private void ResetTimer(BattleMiniGameEventDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            _timeLeftByEvent[definition] = Random.Range(
                definition.RandomMinInterval,
                definition.RandomMaxInterval);
        }
    }
}
