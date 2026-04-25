using System.Collections.Generic;
using Items;
using UnityEngine;

namespace Gems
{
    [CreateAssetMenu(menuName = "Gems/Gem Definition", fileName = "NewGemDefinition")]
    public class GemDefinition : ItemDefinition
    {
        [SerializeField] private GemKind kind = GemKind.LocalModifiers;
        [SerializeField] private List<GemModifierRollDefinition> modifierRollDefinitions = new();

        public GemKind Kind => kind;
        public IReadOnlyList<GemModifierRollDefinition> ModifierRollDefinitions => modifierRollDefinitions;

        public GemInstance CreateInstance()
        {
            return GemInstance.Create(this);
        }

        public IReadOnlyList<string> GetBaseDescriptions()
        {
            List<string> descriptions = new();

            if (!string.IsNullOrWhiteSpace(Description))
                descriptions.Add(Description);

            for (int i = 0; i < modifierRollDefinitions.Count; i++)
            {
                GemModifierRollDefinition definition = modifierRollDefinitions[i];
                if (definition?.ModifierTemplate == null)
                    continue;

                descriptions.Add(definition.ModifierTemplate.GetDescription());
            }

            return descriptions;
        }
    }
}
