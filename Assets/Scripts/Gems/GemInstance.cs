using System;
using System.Collections.Generic;
using LocalizationSupport;
using SaveSystem;
using SkillTree;
using UnityEngine;

namespace Gems
{
    [Serializable]
    public class GemInstance
    {
        [SerializeField] private string instanceId;
        [SerializeField] private GemDefinition definition;
        [SerializeField] private List<float> rolledValues = new();

        public string InstanceId => instanceId;
        public GemDefinition Definition => definition;
        public string DisplayName => definition != null
            ? definition.DisplayName
            : GameLocalization.GetContent("content.gem.unknown", "Unknown Gem");
        public string Description => definition != null ? definition.Description : string.Empty;
        public Sprite Icon => definition != null ? definition.Icon : null;
        public GemKind Kind => definition != null ? definition.Kind : GemKind.LocalModifiers;
        public IReadOnlyList<float> RolledValues => rolledValues;

        public static GemInstance Create(GemDefinition definition)
        {
            GemInstance instance = new GemInstance();
            instance.Initialize(definition);
            return instance;
        }

        public static GemInstance Restore(GemDefinition definition, string savedInstanceId, IReadOnlyList<float> savedRolledValues)
        {
            GemInstance instance = new GemInstance
            {
                definition = definition,
                instanceId = string.IsNullOrWhiteSpace(savedInstanceId) ? Guid.NewGuid().ToString("N") : savedInstanceId,
                rolledValues = savedRolledValues != null ? new List<float>(savedRolledValues) : new List<float>()
            };

            instance.EnsureRollCount();
            return instance;
        }

        public void Reroll()
        {
            if (definition == null)
            {
                rolledValues.Clear();
                return;
            }

            rolledValues.Clear();
            IReadOnlyList<GemModifierRollDefinition> rollDefinitions = definition.ModifierRollDefinitions;
            for (int i = 0; i < rollDefinitions.Count; i++)
            {
                GemModifierRollDefinition rollDefinition = rollDefinitions[i];
                rolledValues.Add(rollDefinition != null ? rollDefinition.RollValue() : 0f);
            }
        }

        public List<Modifier> CreateRuntimeModifiers()
        {
            List<Modifier> modifiers = new();
            if (definition == null)
                return modifiers;

            EnsureRollCount();
            IReadOnlyList<GemModifierRollDefinition> rollDefinitions = definition.ModifierRollDefinitions;
            for (int i = 0; i < rollDefinitions.Count; i++)
            {
                GemModifierRollDefinition rollDefinition = rollDefinitions[i];
                if (rollDefinition == null)
                    continue;

                Modifier modifier = rollDefinition.CreateRolledModifier(rolledValues[i]);
                if (modifier != null)
                    modifiers.Add(modifier);
            }

            return modifiers;
        }

        public IReadOnlyList<string> GetTooltipDescriptions()
        {
            List<string> descriptions = new();

            if (!string.IsNullOrWhiteSpace(Description))
                descriptions.Add(Description);

            if (definition == null)
                return descriptions;

            EnsureRollCount();
            IReadOnlyList<GemModifierRollDefinition> rollDefinitions = definition.ModifierRollDefinitions;
            for (int i = 0; i < rollDefinitions.Count; i++)
            {
                GemModifierRollDefinition rollDefinition = rollDefinitions[i];
                if (rollDefinition == null)
                    continue;

                string description = rollDefinition.CreateRolledDescription(rolledValues[i]);
                if (!string.IsNullOrWhiteSpace(description))
                    descriptions.Add(description);
            }

            return descriptions;
        }

        private void Initialize(GemDefinition gemDefinition)
        {
            definition = gemDefinition;
            instanceId = Guid.NewGuid().ToString("N");
            Reroll();
        }

        private void EnsureRollCount()
        {
            if (definition == null)
            {
                rolledValues.Clear();
                return;
            }

            IReadOnlyList<GemModifierRollDefinition> rollDefinitions = definition.ModifierRollDefinitions;
            while (rolledValues.Count < rollDefinitions.Count)
            {
                GemModifierRollDefinition rollDefinition = rollDefinitions[rolledValues.Count];
                rolledValues.Add(rollDefinition != null ? rollDefinition.RollValue() : 0f);
            }

            if (rolledValues.Count > rollDefinitions.Count)
                rolledValues.RemoveRange(rollDefinitions.Count, rolledValues.Count - rollDefinitions.Count);
        }

        public GemInstanceSaveData CaptureSaveData()
        {
            return new GemInstanceSaveData
            {
                instanceId = instanceId,
                definitionId = definition != null ? definition.SaveDefinitionId : string.Empty,
                rolledValues = new List<float>(rolledValues)
            };
        }
    }
}
