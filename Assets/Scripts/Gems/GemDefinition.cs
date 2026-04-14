using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gems
{
    [CreateAssetMenu(menuName = "Gems/Gem Definition", fileName = "NewGemDefinition")]
    public class GemDefinition : ScriptableObject
    {
        [SerializeField] [HideInInspector] private string saveDefinitionId;
        [SerializeField] private string displayName;
        [SerializeField] [TextArea(2, 6)] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private GemKind kind = GemKind.LocalModifiers;
        [SerializeField] private List<GemModifierRollDefinition> modifierRollDefinitions = new();

        public string SaveDefinitionId => string.IsNullOrWhiteSpace(saveDefinitionId) ? $"name:{name}" : saveDefinitionId;
        public string ExplicitSaveDefinitionId => saveDefinitionId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public GemKind Kind => kind;
        public IReadOnlyList<GemModifierRollDefinition> ModifierRollDefinitions => modifierRollDefinitions;

        public GemInstance CreateInstance()
        {
            return GemInstance.Create(this);
        }

        public IReadOnlyList<string> GetBaseDescriptions()
        {
            List<string> descriptions = new();

            if (!string.IsNullOrWhiteSpace(description))
                descriptions.Add(description);

            for (int i = 0; i < modifierRollDefinitions.Count; i++)
            {
                GemModifierRollDefinition definition = modifierRollDefinitions[i];
                if (definition?.ModifierTemplate == null)
                    continue;

                descriptions.Add(definition.ModifierTemplate.GetDescription());
            }

            return descriptions;
        }

        public bool EnsureSaveDefinitionId()
        {
            if (!string.IsNullOrWhiteSpace(saveDefinitionId))
                return false;

            saveDefinitionId = Guid.NewGuid().ToString("N");
            return true;
        }

        public void RegenerateSaveDefinitionId()
        {
            saveDefinitionId = Guid.NewGuid().ToString("N");
        }

        private void OnValidate()
        {
            EnsureSaveDefinitionId();
        }
    }
}
