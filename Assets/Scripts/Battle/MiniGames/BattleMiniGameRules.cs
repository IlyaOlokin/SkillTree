using System.Collections.Generic;
using UnityEngine;

namespace Battle.MiniGames
{
    public sealed class BattleMiniGameRules
    {
        private readonly Dictionary<BattleMiniGameEventDefinition, int> _unlockCounts =
            new Dictionary<BattleMiniGameEventDefinition, int>();

        private readonly Dictionary<string, float> _powerBonusByEventId =
            new Dictionary<string, float>();

        private float _globalPowerBonus;
        private float _activationTimeBonus;
        private float _miniGameTimeBonus;
        private float _ignoreRewardPercent;

        public float IgnoreRewardPercent => Mathf.Clamp01(_ignoreRewardPercent);

        public void Unlock(BattleMiniGameEventDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            _unlockCounts.TryGetValue(definition, out int count);
            _unlockCounts[definition] = count + 1;
        }

        public void Lock(BattleMiniGameEventDefinition definition)
        {
            if (definition == null || !_unlockCounts.TryGetValue(definition, out int count))
            {
                return;
            }

            count--;
            if (count <= 0)
            {
                _unlockCounts.Remove(definition);
                return;
            }

            _unlockCounts[definition] = count;
        }

        public bool IsUnlockedByModifier(BattleMiniGameEventDefinition definition)
        {
            return definition != null && _unlockCounts.ContainsKey(definition);
        }

        public void AppendUnlockedEventsTo(List<BattleMiniGameEventDefinition> target)
        {
            if (target == null)
            {
                return;
            }

            foreach (BattleMiniGameEventDefinition definition in _unlockCounts.Keys)
            {
                if (definition != null && !target.Contains(definition))
                {
                    target.Add(definition);
                }
            }
        }

        public void AddGlobalPowerBonus(float value)
        {
            _globalPowerBonus += value;
        }

        public void AddEventPowerBonus(string eventId, float value)
        {
            if (string.IsNullOrWhiteSpace(eventId))
            {
                return;
            }

            _powerBonusByEventId.TryGetValue(eventId, out float current);
            _powerBonusByEventId[eventId] = current + value;
        }

        public void AddActivationTimeBonus(float value)
        {
            _activationTimeBonus += value;
        }

        public void AddMiniGameTimeBonus(float value)
        {
            _miniGameTimeBonus += value;
        }

        public void AddIgnoreRewardPercent(float value)
        {
            _ignoreRewardPercent += value;
        }

        public float GetPower(BattleMiniGameEventDefinition definition)
        {
            if (definition == null)
            {
                return 0f;
            }

            float eventBonus = 0f;
            if (!string.IsNullOrWhiteSpace(definition.Id))
            {
                _powerBonusByEventId.TryGetValue(definition.Id, out eventBonus);
            }

            return definition.BasePower * Mathf.Max(0f, 1f + _globalPowerBonus + eventBonus);
        }

        public float GetActivationTime(BattleMiniGameEventDefinition definition)
        {
            return definition != null
                ? definition.ActivationTime * Mathf.Max(0f, 1f + _activationTimeBonus)
                : 0f;
        }

        public float GetMiniGameTime(BattleMiniGameEventDefinition definition)
        {
            return definition != null
                ? definition.MiniGameTime * Mathf.Max(0f, 1f + _miniGameTimeBonus)
                : 0f;
        }
    }
}
