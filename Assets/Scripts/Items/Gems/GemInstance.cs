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

        public string InstanceId => instanceId;
        public GemDefinition Definition => definition;
        public string DisplayName => definition != null
            ? definition.DisplayName
            : GameLocalization.GetContent("content.gem.unknown", "Unknown Gem");
        public string Description => definition != null ? definition.Description : string.Empty;
        public Sprite Icon => definition != null ? definition.Icon : null;
        public GemKind Kind => definition != null ? definition.Kind : GemKind.LocalModifiers;

        public static GemInstance Create(GemDefinition definition)
        {
            GemInstance instance = new GemInstance();
            instance.Initialize(definition);
            return instance;
        }

        public static GemInstance Restore(GemDefinition definition, string savedInstanceId)
        {
            return new GemInstance
            {
                definition = definition,
                instanceId = string.IsNullOrWhiteSpace(savedInstanceId) ? Guid.NewGuid().ToString("N") : savedInstanceId
            };
        }

        public List<Modifier> CreateRuntimeModifiers()
        {
            List<Modifier> modifiers = new();
            if (definition == null)
                return modifiers;

            IReadOnlyList<Modifier> modifierTemplates = definition.ModifierTemplates;
            for (int i = 0; i < modifierTemplates.Count; i++)
            {
                Modifier modifier = GemModifierUtility.CreateRuntimeModifier(modifierTemplates[i]);
                if (modifier == null)
                    continue;

                modifiers.Add(modifier);
            }

            return modifiers;
        }

        public IReadOnlyList<string> GetTooltipDescriptions()
        {
            return GetTooltipDescriptions(ModifierPowerContext.None);
        }

        public IReadOnlyList<string> GetTooltipDescriptions(ModifierPowerContext powerContext)
        {
            List<string> descriptions = new();

            if (!string.IsNullOrWhiteSpace(Description))
                descriptions.Add(Description);

            if (definition == null)
                return descriptions;

            IReadOnlyList<Modifier> modifierTemplates = definition.ModifierTemplates;
            for (int i = 0; i < modifierTemplates.Count; i++)
            {
                Modifier modifier = modifierTemplates[i];
                if (modifier == null)
                    continue;

                string description = modifier.GetDescription(powerContext);
                if (!string.IsNullOrWhiteSpace(description))
                    descriptions.Add(description);
            }

            return descriptions;
        }

        private void Initialize(GemDefinition gemDefinition)
        {
            definition = gemDefinition;
            instanceId = Guid.NewGuid().ToString("N");
        }

        public GemInstanceSaveData CaptureSaveData()
        {
            return new GemInstanceSaveData
            {
                instanceId = instanceId,
                definitionId = definition != null ? definition.SaveDefinitionId : string.Empty
            };
        }
    }
}
